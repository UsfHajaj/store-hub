import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../core/i18n/locale.service';
import { ActiveStoreService } from '../../core/services/active-store.service';
import { InventoryApiService } from '../../core/services/inventory-api.service';
import { SiTranslatePipe } from '../../shared/pipes/si-translate.pipe';
import { PagedResult } from '../../shared/models/api.types';
import { StocktakeDto, StocktakeLineDto, StocktakeListItemDto } from '../../shared/models/inventory.models';

@Component({
  selector: 'app-stocktake',
  imports: [FormsModule, DatePipe, SiTranslatePipe],
  templateUrl: './stocktake.component.html',
  styleUrl: './stocktake.component.scss',
})
export class StocktakeComponent {
  private readonly api = inject(InventoryApiService);
  private readonly activeStore = inject(ActiveStoreService);
  readonly locale = inject(LocaleService);

  readonly page = signal(1);
  readonly pageSize = signal(12);
  readonly result = signal<PagedResult<StocktakeListItemDto> | null>(null);
  readonly detail = signal<StocktakeDto | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly counts = signal<Record<string, number>>({});

  constructor() {
    this.reload();
  }

  statusLabel(status: number): string {
    if (status === 1) return this.locale.t('stocktake.draft');
    if (status === 2) return this.locale.t('stocktake.completed');
    return this.locale.t('stocktake.cancelled');
  }

  lineName(l: StocktakeLineDto): string {
    return this.locale.lang() === 'ar' ? l.productNameAr : l.productNameEn;
  }

  reload(): void {
    const storeId = this.activeStore.activeStoreId();
    if (!storeId) {
      this.result.set(null);
      return;
    }
    this.loading.set(true);
    this.api
      .getStocktakesPaged(storeId, { page: this.page(), pageSize: this.pageSize() })
      .subscribe({
        next: (r) => {
          this.result.set(r);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
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

  create(): void {
    const storeId = this.activeStore.activeStoreId();
    if (!storeId) return;
    this.saving.set(true);
    this.api.createStocktake(storeId, {}).subscribe({
      next: (d) => {
        this.saving.set(false);
        this.openDetail(d);
        this.reload();
      },
      error: () => this.saving.set(false),
    });
  }

  open(id: string): void {
    const storeId = this.activeStore.activeStoreId();
    if (!storeId) return;
    this.api.getStocktake(storeId, id).subscribe({ next: (d) => this.openDetail(d) });
  }

  private openDetail(d: StocktakeDto): void {
    this.detail.set(d);
    const map: Record<string, number> = {};
    for (const l of d.lines) {
      map[l.productId] = l.countedQuantity ?? l.systemQuantity;
    }
    this.counts.set(map);
  }

  close(): void {
    if (this.saving()) return;
    this.detail.set(null);
  }

  setCount(productId: string, value: string | number): void {
    const n = Number(value);
    this.counts.update((m) => ({ ...m, [productId]: Number.isFinite(n) ? n : 0 }));
  }

  saveLines(): void {
    const storeId = this.activeStore.activeStoreId();
    const d = this.detail();
    if (!storeId || !d || d.status !== 1) return;
    this.saving.set(true);
    const lines = d.lines.map((l) => ({
      productId: l.productId,
      countedQuantity: this.counts()[l.productId] ?? l.systemQuantity,
    }));
    this.api.updateStocktakeLines(storeId, d.id, { lines }).subscribe({
      next: (updated) => {
        this.saving.set(false);
        this.openDetail(updated);
      },
      error: () => this.saving.set(false),
    });
  }

  complete(): void {
    const storeId = this.activeStore.activeStoreId();
    const d = this.detail();
    if (!storeId || !d || d.status !== 1) return;
    this.saving.set(true);
    const lines = d.lines.map((l) => ({
      productId: l.productId,
      countedQuantity: this.counts()[l.productId] ?? l.systemQuantity,
    }));
    this.api.updateStocktakeLines(storeId, d.id, { lines }).subscribe({
      next: () => {
        this.api.completeStocktake(storeId, d.id).subscribe({
          next: (updated) => {
            this.saving.set(false);
            this.openDetail(updated);
            this.reload();
          },
          error: () => this.saving.set(false),
        });
      },
      error: () => this.saving.set(false),
    });
  }
}
