import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/auth/auth.service';
import { LocaleService } from '../../core/i18n/locale.service';
import { ActiveStoreService } from '../../core/services/active-store.service';
import { SalesApiService } from '../../core/services/sales-api.service';
import { SiTranslatePipe } from '../../shared/pipes/si-translate.pipe';
import { PagedResult } from '../../shared/models/api.types';
import { PermissionCodes } from '../../shared/models/permission-codes';
import {
  PAYMENT_METHOD_LABEL_KEYS,
  SALE_STATUS_LABEL_KEYS,
  SaleDto,
  SaleLineDto,
  SaleListItemDto,
} from '../../shared/models/sales.models';

@Component({
  selector: 'app-orders',
  imports: [FormsModule, CurrencyPipe, DatePipe, SiTranslatePipe],
  templateUrl: './orders.component.html',
  styleUrl: './orders.component.scss',
})
export class OrdersComponent {
  private readonly api = inject(SalesApiService);
  private readonly activeStore = inject(ActiveStoreService);
  private readonly auth = inject(AuthService);
  readonly locale = inject(LocaleService);

  readonly result = signal<PagedResult<SaleListItemDto> | null>(null);
  readonly detail = signal<SaleDto | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly page = signal(1);
  readonly returnQtys = signal<Record<string, number>>({});

  search = '';

  readonly canReturn = computed(() => this.auth.hasPermission(PermissionCodes.PosReturnCreate));

  constructor() {
    this.reload();
  }

  paymentLabel(m: 1 | 2): string {
    return this.locale.t(PAYMENT_METHOD_LABEL_KEYS[m]);
  }

  statusLabel(s: 1 | 2 | 3): string {
    return this.locale.t(SALE_STATUS_LABEL_KEYS[s]);
  }

  lineName(l: SaleLineDto): string {
    return this.locale.lang() === 'ar' ? l.productNameAr : l.productNameEn;
  }

  reload(): void {
    const storeId = this.activeStore.activeStoreId();
    if (!storeId) {
      this.result.set(null);
      return;
    }
    this.loading.set(true);
    this.api.getPaged(storeId, { page: this.page(), pageSize: 15, search: this.search.trim() || undefined }).subscribe({
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

  open(row: SaleListItemDto): void {
    const storeId = this.activeStore.activeStoreId();
    if (!storeId) return;
    this.api.getById(storeId, row.id).subscribe({
      next: (d) => {
        this.detail.set(d);
        const qtys: Record<string, number> = {};
        for (const l of d.lines) {
          qtys[l.id] = 0;
        }
        this.returnQtys.set(qtys);
      },
    });
  }

  closeDetail(): void {
    if (this.saving()) return;
    this.detail.set(null);
  }

  print(saleId: string): void {
    const storeId = this.activeStore.activeStoreId();
    if (!storeId) return;
    this.api.downloadInvoicePdf(storeId, saleId).subscribe((blob) => {
      const url = URL.createObjectURL(blob);
      window.open(url, '_blank');
      setTimeout(() => URL.revokeObjectURL(url), 60_000);
    });
  }

  setReturnQty(lineId: string, value: string | number): void {
    const n = Number(value);
    this.returnQtys.update((m) => ({ ...m, [lineId]: Number.isFinite(n) ? n : 0 }));
  }

  submitReturn(): void {
    const storeId = this.activeStore.activeStoreId();
    const sale = this.detail();
    if (!storeId || !sale || !this.canReturn()) return;
    const lines = sale.lines
      .map((l) => ({ saleLineId: l.id, quantity: this.returnQtys()[l.id] || 0 }))
      .filter((l) => l.quantity > 0);
    if (lines.length === 0) return;
    this.saving.set(true);
    this.api.createReturn(storeId, sale.id, { lines }).subscribe({
      next: () => {
        this.saving.set(false);
        this.closeDetail();
        this.reload();
      },
      error: () => this.saving.set(false),
    });
  }

  remaining(l: SaleLineDto): number {
    return Math.max(0, l.quantity - l.returnedQuantity);
  }
}
