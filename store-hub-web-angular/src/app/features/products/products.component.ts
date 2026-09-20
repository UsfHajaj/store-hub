import { CurrencyPipe } from '@angular/common';
import { Component, HostListener, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/auth/auth.service';
import { LocaleService } from '../../core/i18n/locale.service';
import { ActiveStoreService } from '../../core/services/active-store.service';
import { CategoriesApiService } from '../../core/services/categories-api.service';
import { DiscountsApiService } from '../../core/services/discounts-api.service';
import { ProductsApiService } from '../../core/services/products-api.service';
import { SiTranslatePipe } from '../../shared/pipes/si-translate.pipe';
import { PagedResult } from '../../shared/models/api.types';
import { CategoryDto, ProductListItemDto } from '../../shared/models/catalog.models';
import { PermissionCodes } from '../../shared/models/permission-codes';
import { environment } from '../../../environments/environment';

type ModalMode = 'create' | 'edit';

interface CategoryNavItem {
  id: string;
  label: string;
  selectLabel: string;
  isChild: boolean;
  productCount: number;
}

@Component({
  selector: 'app-products',
  imports: [FormsModule, CurrencyPipe, SiTranslatePipe],
  templateUrl: './products.component.html',
  styleUrl: './products.component.scss',
})
export class ProductsComponent {
  private readonly api = inject(ProductsApiService);
  private readonly categoriesApi = inject(CategoriesApiService);
  private readonly discountsApi = inject(DiscountsApiService);
  private readonly activeStore = inject(ActiveStoreService);
  private readonly auth = inject(AuthService);
  readonly locale = inject(LocaleService);

  readonly page = signal(1);
  readonly pageSize = signal(12);
  readonly result = signal<PagedResult<ProductListItemDto> | null>(null);
  readonly categories = signal<CategoryDto[]>([]);
  readonly loading = signal(true);
  readonly modalOpen = signal(false);
  readonly modalMode = signal<ModalMode>('create');
  readonly editingId = signal<string | null>(null);
  readonly saving = signal(false);
  readonly uploading = signal(false);
  readonly discountOpen = signal(false);
  readonly discountTarget = signal<ProductListItemDto | null>(null);
  readonly deleteTarget = signal<ProductListItemDto | null>(null);

  search = '';
  filterCategoryId = '';
  discountPercent = 10;

  nameAr = '';
  nameEn = '';
  descriptionAr = '';
  descriptionEn = '';
  categoryId = '';
  barcode = '';
  price = 0;
  imageUrl = '';
  stockQuantity = 0;
  tracksInventory = true;
  isActive = true;

  readonly canCreate = computed(() => this.auth.hasPermission(PermissionCodes.PosProductCreate));
  readonly canUpdate = computed(() => this.auth.hasPermission(PermissionCodes.PosProductUpdate));
  readonly canDiscount = computed(() => this.auth.hasPermission(PermissionCodes.PosDiscountManage));

  readonly categoryNav = computed((): CategoryNavItem[] => {
    const list = this.categories();
    const ar = this.locale.lang() === 'ar';
    const name = (c: CategoryDto) => (ar ? c.nameAr : c.nameEn);
    const roots = list.filter((c) => !c.parentCategoryId);
    const childrenOf = (id: string) => list.filter((c) => c.parentCategoryId === id);
    const items: CategoryNavItem[] = [];

    const push = (c: CategoryDto, isChild: boolean, parentLabel?: string) => {
      const label = name(c);
      items.push({
        id: c.id,
        label,
        selectLabel: isChild && parentLabel ? `${parentLabel} › ${label}` : label,
        isChild,
        productCount: c.productCount ?? 0,
      });
    };

    if (roots.length === 0) {
      for (const c of list) push(c, false);
      return items;
    }

    for (const root of roots) {
      push(root, false);
      for (const child of childrenOf(root.id)) {
        push(child, true, name(root));
      }
    }

    const seen = new Set(items.map((i) => i.id));
    for (const c of list) {
      if (!seen.has(c.id)) push(c, !!c.parentCategoryId);
    }
    return items;
  });

  constructor() {
    this.reloadCategories();
    this.load();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.saving() || this.uploading()) return;
    if (this.deleteTarget()) {
      this.closeDeleteConfirm();
      return;
    }
    if (this.discountOpen()) {
      this.closeDiscount();
      return;
    }
    if (this.modalOpen()) this.closeModal();
  }

  productName(p: ProductListItemDto): string {
    return this.locale.lang() === 'ar' ? p.nameAr : p.nameEn;
  }

  categoryName(c: CategoryDto): string {
    return this.locale.lang() === 'ar' ? c.nameAr : c.nameEn;
  }

  resolveImage(url: string | null | undefined): string | null {
    if (!url?.trim()) return null;
    if (url.startsWith('http://') || url.startsWith('https://') || url.startsWith('data:')) return url;
    const base = (environment.apiBaseUrl || '').replace(/\/$/, '');
    return `${base}${url.startsWith('/') ? url : `/${url}`}`;
  }

  setCategoryFilter(id: string): void {
    this.filterCategoryId = id;
    this.apply();
  }

  reloadCategories(): void {
    const storeId = this.activeStore.activeStoreId();
    if (!storeId) {
      this.categories.set([]);
      return;
    }
    this.categoriesApi.getList(storeId, false).subscribe({
      next: (list) => this.categories.set(list as CategoryDto[]),
      error: () => this.categories.set([]),
    });
  }

  load(): void {
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
        categoryId: this.filterCategoryId || undefined,
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
    this.load();
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
    this.resetForm();
    if (this.filterCategoryId) this.categoryId = this.filterCategoryId;
    this.modalOpen.set(true);
  }

  openEdit(p: ProductListItemDto): void {
    const storeId = this.activeStore.activeStoreId();
    if (!storeId) return;
    this.modalMode.set('edit');
    this.editingId.set(p.id);
    this.modalOpen.set(true);
    this.api.getById(storeId, p.id).subscribe({
      next: (detail) => {
        this.nameAr = detail.nameAr;
        this.nameEn = detail.nameEn;
        this.descriptionAr = detail.descriptionAr ?? '';
        this.descriptionEn = detail.descriptionEn ?? '';
        this.categoryId = detail.categoryId;
        this.barcode = detail.barcode ?? '';
        this.price = detail.price;
        this.imageUrl = detail.imageUrl ?? '';
        this.stockQuantity = detail.stockQuantity;
        this.tracksInventory = detail.tracksInventory !== false;
        this.isActive = detail.isActive;
      },
      error: () => this.closeModal(),
    });
  }

  closeModal(): void {
    if (this.saving() || this.uploading()) return;
    this.modalOpen.set(false);
    this.editingId.set(null);
  }

  openProductDiscount(row: ProductListItemDto): void {
    this.discountTarget.set(row);
    this.discountPercent = 10;
    this.discountOpen.set(true);
  }

  closeDiscount(): void {
    if (this.saving()) return;
    this.discountOpen.set(false);
    this.discountTarget.set(null);
  }

  saveProductDiscount(): void {
    const storeId = this.activeStore.activeStoreId();
    const target = this.discountTarget();
    const pct = Number(this.discountPercent);
    if (!storeId || !target || !(pct > 0 && pct <= 100)) return;

    const label = this.productName(target);
    this.saving.set(true);
    this.discountsApi
      .create(storeId, {
        nameAr: `خصم ${pct}% — ${label}`,
        nameEn: `${pct}% off — ${label}`,
        discountPercent: pct,
        appliesToAllProducts: false,
        categoryId: null,
        isActive: true,
        productIds: [target.id],
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.closeDiscount();
        },
        error: () => this.saving.set(false),
      });
  }

  onImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;
    const storeId = this.activeStore.activeStoreId();
    if (!storeId) return;
    this.uploading.set(true);
    this.api.uploadImage(storeId, file).subscribe({
      next: (res) => {
        this.imageUrl = res.imageUrl;
        this.uploading.set(false);
      },
      error: () => this.uploading.set(false),
    });
  }

  clearImage(): void {
    this.imageUrl = '';
  }

  save(): void {
    const storeId = this.activeStore.activeStoreId();
    if (!storeId || !this.categoryId) return;
    this.saving.set(true);
    const payload = {
      categoryId: this.categoryId,
      nameAr: this.nameAr.trim(),
      nameEn: this.nameEn.trim(),
      descriptionAr: this.descriptionAr.trim() || null,
      descriptionEn: this.descriptionEn.trim() || null,
      barcode: this.barcode.trim() || null,
      sku: null,
      price: Number(this.price),
      imageUrl: this.imageUrl.trim() || null,
      stockQuantity: this.tracksInventory ? Number(this.stockQuantity) : 0,
      tracksInventory: this.tracksInventory,
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
        this.load();
        this.reloadCategories();
      },
      error: () => this.saving.set(false),
    });
  }

  remove(p: ProductListItemDto, event: Event): void {
    event.stopPropagation();
    if (!this.canUpdate() || this.saving()) return;
    this.deleteTarget.set(p);
  }

  closeDeleteConfirm(): void {
    if (this.saving()) return;
    this.deleteTarget.set(null);
  }

  confirmDelete(): void {
    const p = this.deleteTarget();
    const storeId = this.activeStore.activeStoreId();
    if (!p || !storeId || this.saving()) return;
    this.saving.set(true);
    this.api.delete(storeId, p.id).subscribe({
      next: () => {
        this.saving.set(false);
        this.deleteTarget.set(null);
        if (this.editingId() === p.id) this.closeModal();
        this.load();
        this.reloadCategories();
      },
      error: () => this.saving.set(false),
    });
  }

  private resetForm(): void {
    this.nameAr = '';
    this.nameEn = '';
    this.descriptionAr = '';
    this.descriptionEn = '';
    this.categoryId = this.categoryNav()[0]?.id ?? this.categories()[0]?.id ?? '';
    this.barcode = '';
    this.price = 0;
    this.imageUrl = '';
    this.stockQuantity = 0;
    this.tracksInventory = true;
    this.isActive = true;
  }
}
