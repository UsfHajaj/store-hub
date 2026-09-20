import { Component, computed, HostListener, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { LocaleService } from '../../core/i18n/locale.service';
import { RolesApiService } from '../../core/services/roles-api.service';
import { SiTranslatePipe } from '../../shared/pipes/si-translate.pipe';
import { PagedResult } from '../../shared/models/api.types';
import { PermissionDto, RoleDto, RoleListItemDto } from '../../shared/models/role.models';

type ModalMode = 'create' | 'edit';

@Component({
  selector: 'app-roles',
  imports: [FormsModule, SiTranslatePipe],
  templateUrl: './roles.component.html',
  styleUrl: './roles.component.scss',
})
export class RolesComponent {
  private readonly api = inject(RolesApiService);
  readonly locale = inject(LocaleService);

  readonly page = signal(1);
  readonly pageSize = signal(12);
  readonly result = signal<PagedResult<RoleListItemDto> | null>(null);
  readonly permissions = signal<PermissionDto[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly modalOpen = signal(false);
  readonly modalMode = signal<ModalMode>('create');
  readonly editingId = signal<string | null>(null);
  readonly modalLoading = signal(false);
  readonly deleteTarget = signal<RoleListItemDto | null>(null);

  search = '';
  nameAr = '';
  nameEn = '';
  descriptionAr = '';
  descriptionEn = '';
  selectedPermissionIds = signal<Set<string>>(new Set());
  isAdminRole = false;

  readonly permissionModules = computed(() => {
    const map = new Map<string, PermissionDto[]>();
    for (const p of this.permissions()) {
      const list = map.get(p.module) ?? [];
      list.push(p);
      map.set(p.module, list);
    }
    return [...map.entries()].map(([module, items]) => ({ module, items }));
  });

  constructor() {
    this.api.getPermissions().subscribe({
      next: (permissions) => this.permissions.set(permissions),
      error: () => this.permissions.set([]),
    });
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

  openCreate(): void {
    this.modalMode.set('create');
    this.editingId.set(null);
    this.isAdminRole = false;
    this.nameAr = '';
    this.nameEn = '';
    this.descriptionAr = '';
    this.descriptionEn = '';
    this.selectedPermissionIds.set(new Set());
    this.modalOpen.set(true);
  }

  openEdit(role: RoleListItemDto): void {
    this.modalMode.set('edit');
    this.editingId.set(role.id);
    this.modalOpen.set(true);
    this.modalLoading.set(true);
    this.api.getRole(role.id).subscribe({
      next: (detail) => {
        this.applyRole(detail);
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
    this.modalLoading.set(false);
  }

  canDelete(role: RoleListItemDto): boolean {
    return role.nameEn !== 'Admin';
  }

  roleName(role: RoleListItemDto | RoleDto): string {
    return this.locale.lang() === 'ar' ? role.nameAr : role.nameEn;
  }

  roleDesc(role: RoleListItemDto): string {
    const d = this.locale.lang() === 'ar' ? role.descriptionAr : role.descriptionEn;
    return d?.trim() || this.locale.t('roles.noDesc');
  }

  permissionName(p: PermissionDto): string {
    return this.locale.lang() === 'ar' ? p.nameAr : p.nameEn;
  }

  permissionDesc(p: PermissionDto): string {
    const d = this.locale.lang() === 'ar' ? p.descriptionAr : p.descriptionEn;
    return d?.trim() || '';
  }

  moduleLabel(module: string): string {
    const key = `roles.module.${module}`;
    const t = this.locale.t(key);
    return t === key ? module : t;
  }

  togglePermission(id: string): void {
    const next = new Set(this.selectedPermissionIds());
    if (next.has(id)) next.delete(id);
    else next.add(id);
    this.selectedPermissionIds.set(next);
  }

  isChecked(id: string): boolean {
    return this.selectedPermissionIds().has(id);
  }

  save(): void {
    if (this.saving() || this.modalLoading()) return;
    const nameAr = this.nameAr.trim();
    const nameEn = this.nameEn.trim();
    if (!nameAr || !nameEn) return;

    this.saving.set(true);
    const permissionIds = [...this.selectedPermissionIds()];
    const descAr = this.descriptionAr.trim() || null;
    const descEn = this.descriptionEn.trim() || null;

    if (this.modalMode() === 'create') {
      this.api
        .createRole({
          nameAr,
          nameEn,
          descriptionAr: descAr,
          descriptionEn: descEn,
          permissionIds,
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

    forkJoin({
      role: this.api.updateRole(id, {
        nameAr,
        nameEn,
        descriptionAr: descAr,
        descriptionEn: descEn,
      }),
      perms: this.api.setPermissions(id, permissionIds),
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.modalOpen.set(false);
        this.reload();
      },
      error: () => this.saving.set(false),
    });
  }

  remove(role: RoleListItemDto, event?: Event): void {
    event?.stopPropagation();
    if (!this.canDelete(role) || this.saving()) return;
    this.deleteTarget.set(role);
  }

  closeDeleteConfirm(): void {
    if (this.saving()) return;
    this.deleteTarget.set(null);
  }

  confirmDelete(): void {
    const role = this.deleteTarget();
    if (!role || !this.canDelete(role) || this.saving()) return;
    this.saving.set(true);
    this.api.deleteRole(role.id).subscribe({
      next: () => {
        this.saving.set(false);
        this.deleteTarget.set(null);
        if (this.editingId() === role.id) this.closeModal();
        this.reload();
      },
      error: () => this.saving.set(false),
    });
  }

  private applyRole(role: RoleDto): void {
    this.nameAr = role.nameAr;
    this.nameEn = role.nameEn;
    this.descriptionAr = role.descriptionAr ?? '';
    this.descriptionEn = role.descriptionEn ?? '';
    this.isAdminRole = role.nameEn === 'Admin';
    this.selectedPermissionIds.set(new Set(role.permissionIds));
  }
}
