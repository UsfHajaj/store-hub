import { CurrencyPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/auth/auth.service';
import { LocaleService } from '../../core/i18n/locale.service';
import { ActiveStoreService } from '../../core/services/active-store.service';
import { CategoriesApiService } from '../../core/services/categories-api.service';
import { DiscountsApiService } from '../../core/services/discounts-api.service';
import { ProductsApiService } from '../../core/services/products-api.service';
import { SalesApiService } from '../../core/services/sales-api.service';
import { SiTranslatePipe } from '../../shared/pipes/si-translate.pipe';
import { PagedResult } from '../../shared/models/api.types';
import { CategoryDto, DiscountDto, ProductListItemDto } from '../../shared/models/catalog.models';
import { PermissionCodes } from '../../shared/models/permission-codes';
import { PAYMENT_METHOD_OPTIONS, PaymentMethod, SaleDto } from '../../shared/models/sales.models';
import { environment } from '../../../environments/environment';

interface CartLine {
  productId: string;
  categoryId: string;
  nameAr: string;
  nameEn: string;
  unitPrice: number;
  quantity: number;
}

@Component({
  selector: 'app-pos',
  imports: [FormsModule, CurrencyPipe, SiTranslatePipe],
  templateUrl: './pos.component.html',
  styleUrl: './pos.component.scss',
})
export class PosComponent {
  private readonly productsApi = inject(ProductsApiService);
  private readonly categoriesApi = inject(CategoriesApiService);
  private readonly discountsApi = inject(DiscountsApiService);
  private readonly salesApi = inject(SalesApiService);
  private readonly activeStore = inject(ActiveStoreService);
  private readonly auth = inject(AuthService);
  readonly locale = inject(LocaleService);

  readonly paymentOptions = PAYMENT_METHOD_OPTIONS;

  readonly result = signal<PagedResult<ProductListItemDto> | null>(null);
  readonly categories = signal<CategoryDto[]>([]);
  readonly discounts = signal<readonly DiscountDto[]>([]);
  readonly cart = signal<CartLine[]>([]);
  readonly loading = signal(false);
  readonly checkingOut = signal(false);
  readonly lastSale = signal<SaleDto | null>(null);
  readonly page = signal(1);

  search = '';
  filterCategoryId = '';
  paymentMethod: PaymentMethod = 1;
  notes = '';

  readonly canCheckout = computed(
    () =>
      this.auth.hasPermission(PermissionCodes.PosSaleCreate) ||
      this.auth.hasPermission(PermissionCodes.StoreManage),
  );

  readonly estimatedSubtotal = computed(() =>
    this.cart().reduce((sum, l) => sum + l.unitPrice * l.quantity, 0),
  );

  readonly estimatedDiscount = computed(() =>
    this.cart().reduce((sum, l) => {
      const pct = this.discountPercentFor(l.productId, l.categoryId);
      if (pct <= 0) return sum;
      return sum + Math.round(l.unitPrice * l.quantity * (pct / 100) * 100) / 100;
    }, 0),
  );

  readonly estimatedTotal = computed(() =>
    Math.max(0, Math.round((this.estimatedSubtotal() - this.estimatedDiscount()) * 100) / 100),
  );

  readonly itemCount = computed(() => this.cart().reduce((sum, l) => sum + l.quantity, 0));

  constructor() {
    this.loadCategories();
    this.loadDiscounts();
    this.load();
  }

  productName(p: ProductListItemDto): string {
    return this.locale.lang() === 'ar' ? p.nameAr : p.nameEn;
  }

  lineName(l: CartLine): string {
    return this.locale.lang() === 'ar' ? l.nameAr : l.nameEn;
  }

  categoryName(c: CategoryDto): string {
    return this.locale.lang() === 'ar' ? c.nameAr : c.nameEn;
  }

  discountPercentFor(productId: string, categoryId: string): number {
    let best = 0;
    const now = Date.now();
    for (const d of this.discounts()) {
      if (!d.isActive) continue;
      if (d.startsAtUtc && Date.parse(d.startsAtUtc) > now) continue;
      if (d.endsAtUtc && Date.parse(d.endsAtUtc) < now) continue;
      const applies =
        d.appliesToAllProducts ||
        (!!d.categoryId && d.categoryId === categoryId) ||
        d.productIds.includes(productId);
      if (applies && d.discountPercent > best) best = d.discountPercent;
    }
    return best;
  }

  lineDiscountedUnit(l: CartLine): number {
    const pct = this.discountPercentFor(l.productId, l.categoryId);
    if (pct <= 0) return l.unitPrice;
    return Math.round(l.unitPrice * (1 - pct / 100) * 100) / 100;
  }

  lineTotal(l: CartLine): number {
    return Math.round(this.lineDiscountedUnit(l) * l.quantity * 100) / 100;
  }

  resolveImage(url: string | null | undefined): string | null {
    if (!url?.trim()) return null;
    if (url.startsWith('http://') || url.startsWith('https://') || url.startsWith('data:')) return url;
    const base = (environment.apiBaseUrl || '').replace(/\/$/, '');
    return `${base}${url.startsWith('/') ? url : `/${url}`}`;
  }

  loadCategories(): void {
    const storeId = this.activeStore.activeStoreId();
    if (!storeId) return;
    this.categoriesApi.getList(storeId, false).subscribe({
      next: (list) => this.categories.set(list as CategoryDto[]),
      error: () => this.categories.set([]),
    });
  }

  loadDiscounts(): void {
    const storeId = this.activeStore.activeStoreId();
    if (!storeId) return;
    this.discountsApi.getAll(storeId).subscribe({
      next: (list) => this.discounts.set(list ?? []),
      error: () => this.discounts.set([]),
    });
  }

  load(): void {
    const storeId = this.activeStore.activeStoreId();
    if (!storeId) {
      this.result.set(null);
      return;
    }
    this.loading.set(true);
    this.productsApi
      .getPaged(storeId, {
        page: this.page(),
        pageSize: 18,
        search: this.search.trim() || undefined,
        categoryId: this.filterCategoryId || undefined,
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
    this.load();
  }

  setCategoryFilter(id: string): void {
    this.filterCategoryId = id;
    this.apply();
  }

  onSearchEnter(): void {
    const storeId = this.activeStore.activeStoreId();
    const term = this.search.trim();
    if (!storeId || !term) {
      this.apply();
      return;
    }
    this.loading.set(true);
    this.productsApi.getPaged(storeId, { page: 1, pageSize: 18, search: term }).subscribe({
      next: (r) => {
        this.loading.set(false);
        this.page.set(1);
        this.result.set(r);
        if (r.items.length === 1) {
          this.addToCart(r.items[0]);
          this.search = '';
          this.load();
        }
      },
      error: () => this.loading.set(false),
    });
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

  addToCart(p: ProductListItemDto): void {
    this.lastSale.set(null);
    this.cart.update((lines) => {
      const existing = lines.find((l) => l.productId === p.id);
      if (existing) {
        return lines.map((l) => (l.productId === p.id ? { ...l, quantity: l.quantity + 1 } : l));
      }
      return [
        ...lines,
        {
          productId: p.id,
          categoryId: p.categoryId,
          nameAr: p.nameAr,
          nameEn: p.nameEn,
          unitPrice: p.price,
          quantity: 1,
        },
      ];
    });
  }

  changeQty(productId: string, delta: number): void {
    this.cart.update((lines) =>
      lines
        .map((l) => (l.productId === productId ? { ...l, quantity: l.quantity + delta } : l))
        .filter((l) => l.quantity > 0),
    );
  }

  setQty(productId: string, value: string): void {
    const qty = Number(value);
    if (!Number.isFinite(qty)) return;
    this.cart.update((lines) =>
      lines.map((l) => (l.productId === productId ? { ...l, quantity: qty } : l)).filter((l) => l.quantity > 0),
    );
  }

  removeLine(productId: string): void {
    this.cart.update((lines) => lines.filter((l) => l.productId !== productId));
  }

  clearCart(): void {
    this.cart.set([]);
    this.notes = '';
  }

  checkout(): void {
    const storeId = this.activeStore.activeStoreId();
    const lines = this.cart();
    if (!storeId || lines.length === 0 || this.checkingOut() || !this.canCheckout()) return;
    this.checkingOut.set(true);
    this.salesApi
      .create(storeId, {
        paymentMethod: this.paymentMethod,
        notes: this.notes.trim() || null,
        lines: lines.map((l) => ({ productId: l.productId, quantity: l.quantity })),
      })
      .subscribe({
        next: (sale) => {
          this.checkingOut.set(false);
          this.lastSale.set(sale);
          this.cart.set([]);
          this.notes = '';
          this.load();
          this.loadDiscounts();
        },
        error: () => this.checkingOut.set(false),
      });
  }

  printLastInvoice(): void {
    const sale = this.lastSale();
    const storeId = this.activeStore.activeStoreId();
    if (!sale || !storeId) return;
    this.salesApi.downloadInvoicePdf(storeId, sale.id).subscribe((blob) => {
      const url = URL.createObjectURL(blob);
      window.open(url, '_blank');
      setTimeout(() => URL.revokeObjectURL(url), 60_000);
    });
  }

  startNewSale(): void {
    this.lastSale.set(null);
  }
}
