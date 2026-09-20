import { DatePipe } from '@angular/common';
import { Component, HostListener, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { LocaleService } from '../../core/i18n/locale.service';
import { UsersApiService } from '../../core/services/users-api.service';
import { SiTranslatePipe } from '../../shared/pipes/si-translate.pipe';
import { PasswordToggleBtnComponent } from '../../shared/ui/password-toggle-btn/password-toggle-btn.component';
import { PagedResult } from '../../shared/models/api.types';
import { UserListItemDto } from '../../shared/models/user.models';
import { RoleListItemDto } from '../../shared/models/role.models';
import { ListViewMode } from '../../shared/models/list-view-mode';
import { formatUserDisplayName, userInitials } from '../../shared/utils/user-display.util';

type ModalMode = 'create' | 'edit';

@Component({
  selector: 'app-users',
  imports: [FormsModule, DatePipe, SiTranslatePipe, PasswordToggleBtnComponent],
  templateUrl: './users.component.html',
  styleUrl: './users.component.scss',
})
export class UsersComponent {
  private readonly api = inject(UsersApiService);
  readonly locale = inject(LocaleService);

  readonly page = signal(1);
  readonly pageSize = signal(12);
  readonly result = signal<PagedResult<UserListItemDto> | null>(null);
  readonly viewMode = signal<ListViewMode>('card');
  readonly roles = signal<RoleListItemDto[]>([]);
  readonly selectedRoleIds = signal<Set<string>>(new Set());
  readonly modalOpen = signal(false);
  readonly modalMode = signal<ModalMode>('create');
  readonly editingId = signal<string | null>(null);
  readonly modalLoading = signal(false);
  readonly saving = signal(false);
  readonly showPassword = signal(false);
  readonly deleteTarget = signal<UserListItemDto | null>(null);

  search = '';
  userName = '';
  nameAr = '';
  nameEn = '';
  email = '';
  password = '';

  constructor() {
    this.load();
    this.api.getRoleOptions().subscribe({
      next: (r) => this.roles.set(r),
      error: () => this.roles.set([]),
    });
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

  togglePasswordVisibility(): void {
    this.showPassword.update((v) => !v);
  }

  load(): void {
    this.api
      .getPaged({
        page: this.page(),
        pageSize: this.pageSize(),
        search: this.search.trim() || undefined,
      })
      .subscribe((r) => this.result.set(r));
  }

  apply(): void {
    this.page.set(1);
    this.load();
  }

  setView(mode: ListViewMode): void {
    this.viewMode.set(mode);
  }

  prev(): void {
    const r = this.result();
    if (r?.hasPreviousPage) {
      this.page.update((p) => p - 1);
      this.load();
    }
  }

  next(): void {
    const r = this.result();
    if (r?.hasNextPage) {
      this.page.update((p) => p + 1);
      this.load();
    }
  }

  openCreate(): void {
    this.modalMode.set('create');
    this.editingId.set(null);
    this.userName = '';
    this.nameAr = '';
    this.nameEn = '';
    this.email = '';
    this.password = '';
    this.showPassword.set(false);
    this.selectedRoleIds.set(new Set());
    this.modalOpen.set(true);
  }

  openEdit(row: UserListItemDto): void {
    this.modalMode.set('edit');
    this.editingId.set(row.id);
    this.showPassword.set(false);
    this.modalOpen.set(true);
    this.modalLoading.set(true);
    this.password = '';
    this.api.getById(row.id).subscribe({
      next: (u) => {
        this.userName = u.userName;
        this.nameAr = u.nameAr ?? '';
        this.nameEn = u.nameEn ?? '';
        this.email = u.email;
        this.selectedRoleIds.set(new Set(u.roleIds ?? []));
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

  toggleRole(id: string): void {
    const next = new Set(this.selectedRoleIds());
    if (next.has(id)) next.delete(id);
    else next.add(id);
    this.selectedRoleIds.set(next);
  }

  isRoleChecked(id: string): boolean {
    return this.selectedRoleIds().has(id);
  }

  roleLabel(role: RoleListItemDto): string {
    return this.locale.lang() === 'ar' ? role.nameAr : role.nameEn;
  }

  save(): void {
    if (this.saving() || this.modalLoading()) return;
    const userName = this.userName.trim();
    const email = this.email.trim();
    if (!userName || !email) return;

    const roleIds = [...this.selectedRoleIds()];
    this.saving.set(true);

    if (this.modalMode() === 'create') {
      if (this.password.trim().length < 8) {
        this.saving.set(false);
        return;
      }
      this.api
        .create({
          userName,
          email,
          nameAr: this.nameAr.trim() || null,
          nameEn: this.nameEn.trim() || null,
          password: this.password,
          roleIds,
        })
        .subscribe({
          next: () => {
            this.saving.set(false);
            this.modalOpen.set(false);
            this.load();
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

    const newPassword = this.password.trim() || null;
    forkJoin({
      user: this.api.update(id, {
        userName,
        email,
        nameAr: this.nameAr.trim() || null,
        nameEn: this.nameEn.trim() || null,
        newPassword,
      }),
      roles: this.api.assignRoles(id, roleIds),
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.modalOpen.set(false);
        this.load();
      },
      error: () => this.saving.set(false),
    });
  }

  remove(row: UserListItemDto, event?: Event): void {
    event?.stopPropagation();
    if (this.saving()) return;
    this.deleteTarget.set(row);
  }

  closeDeleteConfirm(): void {
    if (this.saving()) return;
    this.deleteTarget.set(null);
  }

  confirmDelete(): void {
    const row = this.deleteTarget();
    if (!row || this.saving()) return;
    this.saving.set(true);
    this.api.delete(row.id).subscribe({
      next: () => {
        this.saving.set(false);
        this.deleteTarget.set(null);
        if (this.editingId() === row.id) this.closeModal();
        this.load();
      },
      error: () => this.saving.set(false),
    });
  }

  toggleActive(row: UserListItemDto): void {
    const call = row.isActive ? this.api.deactivate(row.id) : this.api.activate(row.id);
    call.subscribe(() => this.load());
  }

  dash(): string {
    return this.locale.t('users.dash');
  }

  displayName(row: UserListItemDto): string {
    return formatUserDisplayName(row, this.locale);
  }

  initials(row: UserListItemDto): string {
    return userInitials(row, this.locale);
  }
}
