import { Component, HostListener, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged, switchMap, of, catchError } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { LocaleService } from '../../core/i18n/locale.service';
import { ActiveStoreService } from '../../core/services/active-store.service';
import { CategoriesApiService } from '../../core/services/categories-api.service';
import { DiscountsApiService } from '../../core/services/discounts-api.service';
import { ProductsApiService } from '../../core/services/products-api.service';
import { SiTranslatePipe } from '../../shared/pipes/si-translate.pipe';
import { PagedResult } from '../../shared/models/api.types';
import { CategoryDto, DiscountDto, ProductListItemDto } from '../../shared/models/catalog.models';
import { PermissionCodes } from '../../shared/models/permission-codes';

type ModalMode = 'create' | 'edit';
type ScopeMode = 'all' | 'category' | 'product';

@Component({
  selector: 'app-discounts',
  imports: [FormsModule, SiTranslatePipe],
  templateUrl: './discounts.component.html',
  styleUrl: './discounts.component.scss',
})
export class DiscountsComponent {
  private readonly api = inject(DiscountsApiService);
  private readonly productsApi = inject(ProductsApiService);
  private readonly categoriesApi = inject(CategoriesApiService);
  private readonly activeStore = inject(ActiveStoreService);
  private readonly auth = inject(AuthService);
  readonly locale = inject(LocaleService);

  private readonly productSearch$ = new Subject<string>();

  readonly page = signal(1);
  readonly pageSize = signal(12);
  readonly result = signal<PagedResult<DiscountDto> | null>(null);
  readonly categories = signal<CategoryDto[]>([]);
  readonly productHits = signal<ProductListItemDto[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly searching = signal(false);
  readonly modalOpen = signal(false);
  readonly modalMode = signal<ModalMode>('create');
  readonly editingId = signal<string | null>(null);
  readonly selectedProduct = signal<ProductListItemDto | null>(null);
  readonly deleteTarget = signal<DiscountDto | null>(null);

  search = '';
  nameAr = '';
  nameEn = '';
  discountPercent = 10;
  startsAtUtc = '';
  endsAtUtc = '';
  isActive = true;
  scopeMode: ScopeMode = 'all';
  categoryId = '';
  productQuery = '';

  readonly canManage = computed(() => this.auth.hasPermission(PermissionCodes.PosDiscountManage));

  constructor() {
    this.reload();
    this.productSearch$
      .pipe(
        debounceTime(280),
        distinctUntilChanged(),
        switchMap((q) => {
          const storeId = this.activeStore.activeStoreId();
          const term = q.trim();
          if (!storeId || term.length < 1) {
            this.searching.set(false);
            return of([] as ProductListItemDto[]);
          }
          this.searching.set(true);
          return this.productsApi.getPaged(storeId, { page: 1, pageSize: 8, search: term }).pipe(
            switchMap((r) => of([...r.items])),
            catchError(() => of([] as ProductListItemDto[])),
          );
        }),
      )
      .subscribe((items) => {
        this.productHits.set(items);
        this.searching.set(false);
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

  discountName(d: DiscountDto): string {
    return this.locale.lang() === 'ar' ? d.nameAr : d.nameEn;
  }

  categoryName(c: CategoryDto): string {
    return this.locale.lang() === 'ar' ? c.nameAr : c.nameEn;
  }

  scopeLabel(d: DiscountDto): string {
    if (d.appliesToAllProducts) return this.locale.t('discounts.allProducts');
    if (d.categoryId) {
      const name = this.locale.lang() === 'ar' ? d.categoryNameAr : d.categoryNameEn;
      return this.locale.t('discounts.scopeCategory', { name: name || '—' });
    }
    return this.locale.t('discounts.scopeProducts', { count: d.productCount || d.productIds.length });
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
    this.categoriesApi.getList(storeId, false).subscribe({
      next: (list) => this.categories.set(list as CategoryDto[]),
      error: () => this.categories.set([]),
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
    this.nameAr = '';
    this.nameEn = '';
    this.discountPercent = 10;
    this.startsAtUtc = '';
    this.endsAtUtc = '';
    this.isActive = true;
    this.scopeMode = 'all';
    this.categoryId = '';
    this.productQuery = '';
    this.selectedProduct.set(null);
    this.productHits.set([]);
    this.modalOpen.set(true);
  }

  openEdit(d: DiscountDto): void {
    this.modalMode.set('edit');
    this.editingId.set(d.id);
    this.nameAr = d.nameAr;
    this.nameEn = d.nameEn;
    this.discountPercent = d.discountPercent;
    this.startsAtUtc = d.startsAtUtc ? d.startsAtUtc.slice(0, 16) : '';
    this.endsAtUtc = d.endsAtUtc ? d.endsAtUtc.slice(0, 16) : '';
    this.isActive = d.isActive;
    if (d.appliesToAllProducts) {
      this.scopeMode = 'all';
      this.categoryId = '';
      this.selectedProduct.set(null);
    } else if (d.categoryId) {
      this.scopeMode = 'category';
      this.categoryId = d.categoryId;
      this.selectedProduct.set(null);
    } else {
      this.scopeMode = 'product';
      this.categoryId = '';
      const pid = d.productIds[0];
      this.selectedProduct.set(
        pid
          ? {
              id: pid,
              categoryId: '',
              categoryNameAr: '',
              categoryNameEn: '',
              nameAr: this.locale.t('discounts.selectedOne'),
              nameEn: this.locale.t('discounts.selectedOne'),
              barcode: null,
              sku: null,
              price: 0,
              imageUrl: null,
              stockQuantity: 0,
              isActive: true,
            }
          : null,
      );
    }
    this.productQuery = '';
    this.productHits.set([]);
    this.modalOpen.set(true);
  }

  closeModal(): void {
    if (this.saving()) return;
    this.modalOpen.set(false);
    this.editingId.set(null);
  }

  onProductQuery(): void {
    this.productSearch$.next(this.productQuery);
  }

  pickProduct(p: ProductListItemDto): void {
    this.selectedProduct.set(p);
    this.productQuery = '';
    this.productHits.set([]);
  }

  clearPickedProduct(): void {
    this.selectedProduct.set(null);
  }

  productLabel(p: ProductListItemDto): string {
    return this.locale.lang() === 'ar' ? p.nameAr : p.nameEn;
  }

  save(): void {
    const storeId = this.activeStore.activeStoreId();
    if (!storeId) return;

    let nameAr = this.nameAr.trim();
    let nameEn = this.nameEn.trim();
    const pct = Number(this.discountPercent);

    if (!nameAr || !nameEn) {
      if (this.scopeMode === 'all') {
        nameAr = nameAr || `خصم ${pct}% على الكل`;
        nameEn = nameEn || `${pct}% off all`;
      } else if (this.scopeMode === 'category') {
        const cat = this.categories().find((c) => c.id === this.categoryId);
        const cn = cat ? this.categoryName(cat) : '';
        nameAr = nameAr || `خصم ${pct}% — ${cn}`;
        nameEn = nameEn || `${pct}% off — ${cn}`;
      } else if (this.selectedProduct()) {
        const pn = this.productLabel(this.selectedProduct()!);
        nameAr = nameAr || `خصم ${pct}% — ${pn}`;
        nameEn = nameEn || `${pct}% off — ${pn}`;
      }
    }

    if (!nameAr || !nameEn) return;

    const appliesToAllProducts = this.scopeMode === 'all';
    const categoryId = this.scopeMode === 'category' ? this.categoryId || null : null;
    const productIds =
      this.scopeMode === 'product' && this.selectedProduct() ? [this.selectedProduct()!.id] : null;

    if (this.scopeMode === 'category' && !categoryId) return;
    if (this.scopeMode === 'product' && !productIds?.length) return;

    this.saving.set(true);
    const payload = {
      nameAr,
      nameEn,
      discountPercent: pct,
      startsAtUtc: this.startsAtUtc ? new Date(this.startsAtUtc).toISOString() : null,
      endsAtUtc: this.endsAtUtc ? new Date(this.endsAtUtc).toISOString() : null,
      appliesToAllProducts,
      categoryId,
      isActive: this.isActive,
      productIds,
    };

    const req =
      this.modalMode() === 'create'
        ? this.api.create(storeId, payload)
        : this.api.update(storeId, this.editingId()!, { ...payload, isActive: this.isActive });

    req.subscribe({
      next: () => {
        this.saving.set(false);
        this.closeModal();
        this.reload();
      },
      error: () => this.saving.set(false),
    });
  }

  remove(d: DiscountDto, event: Event): void {
    event.stopPropagation();
    if (!this.canManage() || this.saving()) return;
    this.deleteTarget.set(d);
  }

  closeDeleteConfirm(): void {
    if (this.saving()) return;
    this.deleteTarget.set(null);
  }

  confirmDelete(): void {
    const d = this.deleteTarget();
    const storeId = this.activeStore.activeStoreId();
    if (!d || !storeId || this.saving()) return;
    this.saving.set(true);
    this.api.delete(storeId, d.id).subscribe({
      next: () => {
        this.saving.set(false);
        this.deleteTarget.set(null);
        if (this.editingId() === d.id) this.closeModal();
        this.reload();
      },
      error: () => this.saving.set(false),
    });
  }
}
