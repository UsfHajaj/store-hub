import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../core/i18n/locale.service';
import { ActiveStoreService } from '../../core/services/active-store.service';
import { InventoryApiService } from '../../core/services/inventory-api.service';
import { SiTranslatePipe } from '../../shared/pipes/si-translate.pipe';
import { PagedResult } from '../../shared/models/api.types';
import { InventoryItemDto } from '../../shared/models/inventory.models';

@Component({
  selector: 'app-inventory',
  imports: [FormsModule, SiTranslatePipe],
  templateUrl: './inventory.component.html',
  styleUrl: './inventory.component.scss',
})
export class InventoryComponent {
  private readonly api = inject(InventoryApiService);
  private readonly activeStore = inject(ActiveStoreService);
  readonly locale = inject(LocaleService);

  readonly result = signal<PagedResult<InventoryItemDto> | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly page = signal(1);
  readonly adjustTarget = signal<InventoryItemDto | null>(null);

  search = '';
  lowStockOnly = false;
  quantityChange = 0;
  notes = '';

  constructor() {
    this.reload();
  }

  name(i: InventoryItemDto): string {
    return this.locale.lang() === 'ar' ? i.nameAr : i.nameEn;
  }

  cat(i: InventoryItemDto): string {
    return this.locale.lang() === 'ar' ? i.categoryNameAr : i.categoryNameEn;
  }

  reload(): void {
    const storeId = this.activeStore.activeStoreId();
    if (!storeId) return;
    this.loading.set(true);
    this.api
      .getPaged(storeId, {
        page: this.page(),
        pageSize: 20,
        search: this.search.trim() || undefined,
        lowStockOnly: this.lowStockOnly || undefined,
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

  openAdjust(item: InventoryItemDto): void {
    this.adjustTarget.set(item);
    this.quantityChange = 0;
    this.notes = '';
  }

  closeAdjust(): void {
    if (this.saving()) return;
    this.adjustTarget.set(null);
  }

  saveAdjust(): void {
    const storeId = this.activeStore.activeStoreId();
    const target = this.adjustTarget();
    if (!storeId || !target || this.quantityChange === 0) return;
    this.saving.set(true);
    this.api
      .adjust(storeId, {
        productId: target.productId,
        quantityChange: this.quantityChange,
        notes: this.notes.trim() || null,
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.closeAdjust();
          this.reload();
        },
        error: () => this.saving.set(false),
      });
  }
}
