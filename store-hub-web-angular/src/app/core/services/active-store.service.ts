import { Injectable, computed, inject, signal } from '@angular/core';
import { AuthService } from '../auth/auth.service';
import { PermissionCodes } from '../../shared/models/permission-codes';
import { StoreListItemDto } from '../../shared/models/store.models';
import { StoresApiService } from './stores-api.service';

const LS_ACTIVE_STORE = 'storehub_active_store_id';

@Injectable({ providedIn: 'root' })
export class ActiveStoreService {
  private readonly api = inject(StoresApiService);
  private readonly auth = inject(AuthService);

  readonly stores = signal<StoreListItemDto[]>([]);
  readonly activeStoreId = signal<string | null>(this.readStoredId());
  readonly loading = signal(false);

  readonly activeStore = computed(() => {
    const id = this.activeStoreId();
    return this.stores().find((s) => s.id === id) ?? null;
  });

  readonly needsPicker = computed(() => {
    if (!this.auth.isAuthenticated()) return false;
    if (this.loading()) return false;
    const list = this.stores();
    if (list.length === 0) return false;
    if (list.length === 1) return false;
    return !this.activeStoreId() || !list.some((s) => s.id === this.activeStoreId());
  });

  refresh(): void {
    if (!this.auth.isAuthenticated()) {
      this.stores.set([]);
      return;
    }
    this.loading.set(true);
    this.api.getMine().subscribe({
      next: (list) => {
        this.stores.set(list);
        this.loading.set(false);
        this.ensureSelection(list);
      },
      error: () => {
        this.stores.set([]);
        this.loading.set(false);
      },
    });
  }

  selectStore(id: string): void {
    this.activeStoreId.set(id);
    try {
      localStorage.setItem(LS_ACTIVE_STORE, id);
    } catch {
      /* ignore */
    }
  }

  clear(): void {
    this.stores.set([]);
    this.activeStoreId.set(null);
    try {
      localStorage.removeItem(LS_ACTIVE_STORE);
    } catch {
      /* ignore */
    }
  }

  canManageStores(): boolean {
    return this.auth.hasPermission(PermissionCodes.StoreManage);
  }

  private ensureSelection(list: StoreListItemDto[]): void {
    if (list.length === 0) {
      this.activeStoreId.set(null);
      return;
    }
    const current = this.activeStoreId();
    if (current && list.some((s) => s.id === current)) {
      return;
    }
    if (list.length === 1) {
      this.selectStore(list[0].id);
    }
  }

  private readStoredId(): string | null {
    try {
      return localStorage.getItem(LS_ACTIVE_STORE);
    } catch {
      return null;
    }
  }
}
