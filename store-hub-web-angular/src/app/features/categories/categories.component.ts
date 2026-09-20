import { Component, HostListener, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/auth/auth.service';
import { LocaleService } from '../../core/i18n/locale.service';
import { ActiveStoreService } from '../../core/services/active-store.service';
import { CategoriesApiService } from '../../core/services/categories-api.service';
import { SiTranslatePipe } from '../../shared/pipes/si-translate.pipe';
import { PagedResult } from '../../shared/models/api.types';
import { CategoryDto } from '../../shared/models/catalog.models';
import { PermissionCodes } from '../../shared/models/permission-codes';

type ModalMode = 'create' | 'edit';

@Component({
  selector: 'app-categories',
  imports: [FormsModule, SiTranslatePipe],
  templateUrl: './categories.component.html',
  styleUrl: './categories.component.scss',
})
export class CategoriesComponent {
  private readonly api = inject(CategoriesApiService);
  private readonly activeStore = inject(ActiveStoreService);
  private readonly auth = inject(AuthService);
  readonly locale = inject(LocaleService);

  readonly PermissionCodes = PermissionCodes;
  readonly page = signal(1);
  readonly pageSize = signal(12);
  readonly result = signal<PagedResult<CategoryDto> | null>(null);
  readonly allForParents = signal<CategoryDto[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly modalOpen = signal(false);
  readonly modalMode = signal<ModalMode>('create');
  readonly editingId = signal<string | null>(null);
  readonly deleteTarget = signal<CategoryDto | null>(null);

  search = '';
  nameAr = '';
  nameEn = '';
  isActive = true;
  parentCategoryId: string | null = null;

  readonly canManage = computed(() => this.auth.hasPermission(PermissionCodes.PosCategoryManage));

  readonly parentOptions = computed(() => {
    const editing = this.editingId();
    const all = this.allForParents();
    const blocked = editing ? this.collectSelfAndDescendants(editing, all) : new Set<string>();
    const eligible = all.filter((c) => !blocked.has(c.id));
    const roots = eligible.filter((c) => !c.parentCategoryId);
    return roots.length > 0 ? roots : eligible;
  });

  constructor() {
    this.reload();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.saving()) return;
    if (this.deleteTarget()) {
      this.closeDeleteConfirm();
      return;
    }
    if (this.modalOpen()) this.closeModal();
  }

  reload(): void {
    const storeId = this.activeStore.activeStoreId();
    if (!storeId) {
      this.result.set(null);
      this.loading.set(false);
      return;
    }
    this.loading.set(true);
    this.api
      .getPaged(storeId, {
        page: this.page(),
        pageSize: this.pageSize(),
        search: this.search.trim() || undefined,
      })
      .subscribe({
        next: (r) => {
          this.result.set(r);
          this.loading.set(false);
        },
        error: () => {
          this.result.set(null);
          this.loading.set(false);
        },
      });
  }

  apply(): void {
    this.page.set(1);
    this.reload();
  }

  prev(): void {
    const r = this.result();
    if (r?.hasPreviousPage) {
      this.page.update((p) => p - 1);
      this.reload();
    }
  }

  next(): void {
    const r = this.result();
    if (r?.hasNextPage) {
      this.page.update((p) => p + 1);
      this.reload();
    }
  }

  categoryName(c: CategoryDto): string {
    return this.locale.lang() === 'ar' ? c.nameAr : c.nameEn;
  }

  parentBadge(c: CategoryDto): string {
    if (!c.parentCategoryId) return this.locale.t('categories.rootBadge');
    const name =
      this.locale.lang() === 'ar' ? (c.parentNameAr ?? '') : (c.parentNameEn ?? '');
    return this.locale.t('categories.subOf', { name: name.trim() || '—' });
  }

  private loadParentOptions(done?: () => void): void {
    const storeId = this.activeStore.activeStoreId();
    if (!storeId) {
      this.allForParents.set([]);
      done?.();
      return;
    }
    this.api.getList(storeId, false).subscribe({
      next: (list) => {
        this.allForParents.set(list as CategoryDto[]);
        done?.();
      },
      error: () => {
        this.allForParents.set([]);
        done?.();
      },
    });
  }

  private collectSelfAndDescendants(id: string, all: CategoryDto[]): Set<string> {
    const blocked = new Set<string>([id]);
    let changed = true;
    while (changed) {
      changed = false;
      for (const c of all) {
        if (c.parentCategoryId && blocked.has(c.parentCategoryId) && !blocked.has(c.id)) {
          blocked.add(c.id);
          changed = true;
        }
      }
    }
    return blocked;
  }

  openCreate(): void {
    this.modalMode.set('create');
    this.editingId.set(null);
    this.nameAr = '';
    this.nameEn = '';
    this.isActive = true;
    this.parentCategoryId = null;
    this.loadParentOptions(() => this.modalOpen.set(true));
  }

  openEdit(c: CategoryDto): void {
    this.modalMode.set('edit');
    this.editingId.set(c.id);
    this.nameAr = c.nameAr;
    this.nameEn = c.nameEn;
    this.isActive = c.isActive;
    this.parentCategoryId = c.parentCategoryId;
    this.loadParentOptions(() => this.modalOpen.set(true));
  }

  closeModal(): void {
    if (this.saving()) return;
    this.modalOpen.set(false);
    this.editingId.set(null);
  }

  save(): void {
    const storeId = this.activeStore.activeStoreId();
    if (!storeId) return;
    this.saving.set(true);
    const payload = {
      parentCategoryId: this.parentCategoryId || null,
      nameAr: this.nameAr.trim(),
      nameEn: this.nameEn.trim(),
      isActive: this.isActive,
    };
    const req =
      this.modalMode() === 'create'
        ? this.api.create(storeId, payload)
        : this.api.update(storeId, this.editingId()!, payload);
    req.subscribe({
      next: () => {
        this.saving.set(false);
        this.closeModal();
        this.reload();
      },
      error: () => this.saving.set(false),
    });
  }

  remove(c: CategoryDto, event: Event): void {
    event.stopPropagation();
    if (!this.canManage() || this.saving()) return;
    this.deleteTarget.set(c);
  }

  closeDeleteConfirm(): void {
    if (this.saving()) return;
    this.deleteTarget.set(null);
  }

  confirmDelete(): void {
    const c = this.deleteTarget();
    const storeId = this.activeStore.activeStoreId();
    if (!c || !storeId || this.saving()) return;
    this.saving.set(true);
    this.api.delete(storeId, c.id).subscribe({
      next: () => {
        this.saving.set(false);
        this.deleteTarget.set(null);
        if (this.editingId() === c.id) this.closeModal();
        this.reload();
      },
      error: () => this.saving.set(false),
    });
  }
}
