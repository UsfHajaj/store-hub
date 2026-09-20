import { Component, HostListener, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { LocaleService } from '../../core/i18n/locale.service';
import { StoresApiService } from '../../core/services/stores-api.service';
import { UsersApiService } from '../../core/services/users-api.service';
import { ActiveStoreService } from '../../core/services/active-store.service';
import { SiTranslatePipe } from '../../shared/pipes/si-translate.pipe';
import { PagedResult } from '../../shared/models/api.types';
import {
  STORE_TYPE_OPTIONS,
  StoreListItemDto,
  StoreType,
} from '../../shared/models/store.models';
import { UserListItemDto } from '../../shared/models/user.models';
import { formatUserDisplayName } from '../../shared/utils/user-display.util';

type ModalMode = 'create' | 'edit' | 'members';

@Component({
  selector: 'app-stores',
  imports: [FormsModule, SiTranslatePipe],
  templateUrl: './stores.component.html',
  styleUrl: './stores.component.scss',
})
export class StoresComponent {
  private readonly api = inject(StoresApiService);
  private readonly usersApi = inject(UsersApiService);
  private readonly activeStore = inject(ActiveStoreService);
  readonly locale = inject(LocaleService);

  readonly page = signal(1);
  readonly pageSize = signal(12);
  readonly result = signal<PagedResult<StoreListItemDto> | null>(null);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly modalOpen = signal(false);
  readonly modalMode = signal<ModalMode>('create');
  readonly editingId = signal<string | null>(null);
  readonly modalLoading = signal(false);
  readonly users = signal<UserListItemDto[]>([]);
  readonly selectedMemberIds = signal<Set<string>>(new Set());

  readonly typeOptions = STORE_TYPE_OPTIONS;

  search = '';
  nameAr = '';
  nameEn = '';
  descriptionAr = '';
  descriptionEn = '';
  storeType: StoreType = 99;

  constructor() {
    this.reload();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.modalOpen() && !this.saving()) this.closeModal();
  }

  reload(): void {
    this.loading.set(true);
    this.api
      .getPaged({
        page: this.page(),
        pageSize: this.pageSize(),
        search: this.search.trim() || undefined,
      })
      .subscribe({
        next: (r) => {
          this.result.set(r);
          this.loading.set(false);
          this.activeStore.refresh();
        },
        error: () => this.loading.set(false),
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

  storeName(s: StoreListItemDto): string {
    return this.locale.lang() === 'ar' ? s.nameAr : s.nameEn;
  }

  storeDesc(s: StoreListItemDto): string {
    const d = this.locale.lang() === 'ar' ? s.descriptionAr : s.descriptionEn;
    return d?.trim() || this.locale.t('stores.noDesc');
  }

  typeLabel(type: StoreType): string {
    const opt = this.typeOptions.find((o) => o.value === type);
    return opt ? this.locale.t(opt.labelKey) : String(type);
  }

  openCreate(): void {
    this.modalMode.set('create');
    this.editingId.set(null);
    this.nameAr = '';
    this.nameEn = '';
    this.descriptionAr = '';
    this.descriptionEn = '';
    this.storeType = 99;
    this.selectedMemberIds.set(new Set());
    this.modalOpen.set(true);
    this.loadUsers();
  }

  openEdit(store: StoreListItemDto): void {
    this.modalMode.set('edit');
    this.editingId.set(store.id);
    this.modalOpen.set(true);
    this.modalLoading.set(true);
    this.api.getById(store.id).subscribe({
      next: (detail) => {
        this.nameAr = detail.nameAr;
        this.nameEn = detail.nameEn;
        this.descriptionAr = detail.descriptionAr ?? '';
        this.descriptionEn = detail.descriptionEn ?? '';
        this.storeType = detail.storeType;
        this.modalLoading.set(false);
      },
      error: () => {
        this.modalLoading.set(false);
        this.closeModal();
      },
    });
  }

  openMembers(store: StoreListItemDto): void {
    this.modalMode.set('members');
    this.editingId.set(store.id);
    this.modalOpen.set(true);
    this.modalLoading.set(true);
    forkJoin({
      users: this.usersApi.getPaged({ page: 1, pageSize: 100 }),
      members: this.api.getMembers(store.id),
    }).subscribe({
      next: ({ users, members }) => {
        this.users.set([...users.items]);
        this.selectedMemberIds.set(new Set(members.map((m) => m.userId)));
        this.modalLoading.set(false);
      },
      error: () => {
        this.modalLoading.set(false);
        this.closeModal();
      },
    });
  }

  closeModal(): void {
    if (this.saving()) return;
    this.modalOpen.set(false);
    this.editingId.set(null);
  }

  toggleMember(userId: string): void {
    const next = new Set(this.selectedMemberIds());
    if (next.has(userId)) next.delete(userId);
    else next.add(userId);
    this.selectedMemberIds.set(next);
  }

  isMember(userId: string): boolean {
    return this.selectedMemberIds().has(userId);
  }

  userLabel(u: UserListItemDto): string {
    return formatUserDisplayName(u, this.locale);
  }

  save(): void {
    if (this.saving() || this.modalLoading()) return;
    const mode = this.modalMode();

    if (mode === 'members') {
      const id = this.editingId();
      if (!id) return;
      this.saving.set(true);
      this.api.setMembers(id, [...this.selectedMemberIds()]).subscribe({
        next: () => {
          this.saving.set(false);
          this.modalOpen.set(false);
          this.reload();
        },
        error: () => this.saving.set(false),
      });
      return;
    }

    const nameAr = this.nameAr.trim();
    const nameEn = this.nameEn.trim();
    if (!nameAr || !nameEn) return;

    this.saving.set(true);

    if (mode === 'create') {
      this.api
        .create({
          nameAr,
          nameEn,
          descriptionAr: null,
          descriptionEn: null,
          storeType: 99,
          memberUserIds: [...this.selectedMemberIds()],
        })
        .subscribe({
          next: () => {
            this.saving.set(false);
            this.modalOpen.set(false);
            this.reload();
          },
          error: () => this.saving.set(false),
        });
      return;
    }

    const id = this.editingId();
    if (!id) {
      this.saving.set(false);
      return;
    }

    this.api
      .update(id, {
        nameAr,
        nameEn,
        descriptionAr: null,
        descriptionEn: null,
        storeType: this.storeType || 99,
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.modalOpen.set(false);
          this.reload();
        },
        error: () => this.saving.set(false),
      });
  }

  toggleActive(store: StoreListItemDto): void {
    const call = store.isActive ? this.api.deactivate(store.id) : this.api.activate(store.id);
    call.subscribe(() => this.reload());
  }

  private loadUsers(): void {
    this.usersApi.getPaged({ page: 1, pageSize: 100 }).subscribe({
      next: (r) => this.users.set([...r.items]),
    });
  }
}
