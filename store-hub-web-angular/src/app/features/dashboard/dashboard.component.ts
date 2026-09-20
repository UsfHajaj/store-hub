import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { LocaleService } from '../../core/i18n/locale.service';
import { ActiveStoreService } from '../../core/services/active-store.service';
import { NotificationsApiService } from '../../core/services/notifications-api.service';
import { ReportsApiService } from '../../core/services/reports-api.service';
import { SiTranslatePipe } from '../../shared/pipes/si-translate.pipe';
import { PermissionCodes } from '../../shared/models/permission-codes';
import {
  AdminReportSummaryDto,
  CashierPerformanceDto,
  InventoryStockItemDto,
  NamedAmountDto,
  RecentOrderDto,
  StoreReportSummaryDto,
} from '../../shared/models/reports.models';
import { SiChartComponent, SiChartDataset } from '../../shared/ui/si-chart/si-chart.component';
import { dailyLabels, namedAmounts, namedLabels, singleDataset } from '../../shared/utils/report-charts';

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink, CurrencyPipe, SiTranslatePipe, SiChartComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit {
  readonly auth = inject(AuthService);
  readonly activeStore = inject(ActiveStoreService);
  readonly locale = inject(LocaleService);
  private readonly notificationsApi = inject(NotificationsApiService);
  private readonly reportsApi = inject(ReportsApiService);
  readonly PermissionCodes = PermissionCodes;

  readonly unread = signal(0);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly storeSummary = signal<StoreReportSummaryDto | null>(null);
  readonly adminSummary = signal<AdminReportSummaryDto | null>(null);

  readonly lang = computed(() => this.locale.lang());

  ngOnInit(): void {
    this.notificationsApi.getMyUnreadCount().subscribe({
      next: (n) => this.unread.set(n),
      error: () => this.unread.set(0),
    });
    this.loadReports();
  }

  isAdmin(): boolean {
    return this.auth.hasPermission(PermissionCodes.StoreManage);
  }

  name(): string {
    this.locale.lang();
    const s = this.auth.sessionSnapshot();
    return s?.userName || s?.email || this.locale.t('dashboard.visitor');
  }

  private label(ar: string, en: string): string {
    return this.lang() === 'ar' ? ar || en : en || ar;
  }

  labels(items: readonly NamedAmountDto[]): string[] {
    return namedLabels(items, this.lang());
  }

  amounts(items: readonly NamedAmountDto[]): number[] {
    return namedAmounts(items);
  }

  ds(labelKey: string, data: number[]): SiChartDataset[] {
    return singleDataset(this.locale.t(labelKey), data);
  }

  dayLabels(s: StoreReportSummaryDto | AdminReportSummaryDto): string[] {
    return dailyLabels(s.dailySales);
  }

  daySales(s: StoreReportSummaryDto | AdminReportSummaryDto): SiChartDataset[] {
    return [
      {
        label: this.locale.t('reports.chartSales'),
        data: s.dailySales.map((d) => Number(d.salesTotal) || 0),
        fill: true,
        tension: 0.35,
      },
    ];
  }

  dayInvoices(s: StoreReportSummaryDto | AdminReportSummaryDto): SiChartDataset[] {
    return [
      {
        label: this.locale.t('reports.chartInvoices'),
        data: s.dailySales.map((d) => Number(d.invoiceCount) || 0),
        fill: true,
        tension: 0.35,
      },
    ];
  }

  /** Full 0–23 axis so the hourly line reads as a day curve. */
  hourLabels(_: StoreReportSummaryDto | AdminReportSummaryDto): string[] {
    return Array.from({ length: 24 }, (_, h) => `${h}`);
  }

  hourSales(s: StoreReportSummaryDto | AdminReportSummaryDto): SiChartDataset[] {
    const map = new Map((s.hourlySales ?? []).map((h) => [h.hour, Number(h.salesTotal) || 0]));
    return [
      {
        label: this.locale.t('reports.chartSales'),
        data: Array.from({ length: 24 }, (_, h) => map.get(h) ?? 0),
        fill: true,
        tension: 0.4,
      },
    ];
  }

  storeLabels(stores: readonly StoreReportSummaryDto[]): string[] {
    return stores.map((st) => this.label(st.storeNameAr, st.storeNameEn));
  }

  storeSalesDs(stores: readonly StoreReportSummaryDto[]): SiChartDataset[] {
    return this.ds(
      'reports.chartSales',
      stores.map((st) => Number(st.salesTotal) || 0),
    );
  }

  storeCompareDs(stores: readonly StoreReportSummaryDto[]): SiChartDataset[] {
    return [
      {
        label: this.locale.t('reports.sales'),
        data: stores.map((st) => Number(st.salesTotal) || 0),
      },
      {
        label: this.locale.t('reports.returns'),
        data: stores.map((st) => Number(st.returnsTotal) || 0),
      },
    ];
  }

  cashierLabels(cashiers: readonly CashierPerformanceDto[]): string[] {
    return cashiers.slice(0, 12).map((c) => {
      const name = this.label(c.nameAr, c.nameEn);
      const store = this.label(c.storeNameAr, c.storeNameEn);
      return store && this.isAdmin() ? `${name} · ${store}` : name;
    });
  }

  cashierSalesDs(cashiers: readonly CashierPerformanceDto[]): SiChartDataset[] {
    return this.ds(
      'reports.chartSales',
      cashiers.slice(0, 12).map((c) => Number(c.salesTotal) || 0),
    );
  }

  cashierInvoicesDs(cashiers: readonly CashierPerformanceDto[]): SiChartDataset[] {
    return this.ds(
      'reports.chartInvoices',
      cashiers.slice(0, 12).map((c) => Number(c.invoiceCount) || 0),
    );
  }

  inventoryLabels(items: readonly InventoryStockItemDto[]): string[] {
    return items
      .filter((i) => i.isLowStock)
      .slice(0, 12)
      .map((i) => {
        const name = this.label(i.nameAr, i.nameEn);
        const store = this.label(i.storeNameAr, i.storeNameEn);
        return this.isAdmin() && store ? `${name} · ${store}` : name;
      });
  }

  inventoryStockDs(items: readonly InventoryStockItemDto[]): SiChartDataset[] {
    return this.ds(
      'reports.chartStock',
      items
        .filter((i) => i.isLowStock)
        .slice(0, 12)
        .map((i) => Number(i.stockQuantity) || 0),
    );
  }

  inventoryAllLabels(items: readonly InventoryStockItemDto[]): string[] {
    return items.slice(0, 12).map((i) => this.label(i.nameAr, i.nameEn));
  }

  inventoryAllDs(items: readonly InventoryStockItemDto[]): SiChartDataset[] {
    return this.ds(
      'reports.chartStock',
      items.slice(0, 12).map((i) => Number(i.stockQuantity) || 0),
    );
  }

  orderLabels(orders: readonly RecentOrderDto[]): string[] {
    return [...orders]
      .slice()
      .reverse()
      .slice(-12)
      .map((o) => `#${o.invoiceNumber}`);
  }

  orderTotalsDs(orders: readonly RecentOrderDto[]): SiChartDataset[] {
    return [
      {
        label: this.locale.t('reports.chartSales'),
        data: [...orders]
          .slice()
          .reverse()
          .slice(-12)
          .map((o) => Number(o.grandTotal) || 0),
        fill: true,
        tension: 0.35,
      },
    ];
  }

  private loadReports(): void {
    this.loading.set(true);
    this.error.set(null);

    const canViewReports =
      this.auth.hasPermission(PermissionCodes.ReportView) ||
      this.auth.hasPermission(PermissionCodes.StoreManage);

    if (!canViewReports) {
      this.adminSummary.set(null);
      this.storeSummary.set(null);
      this.loading.set(false);
      return;
    }

    if (this.isAdmin()) {
      this.reportsApi.getAdminSummary({}).subscribe({
        next: (s) => {
          this.adminSummary.set(this.normalizeAdmin(s ?? this.emptyAdmin()));
          this.storeSummary.set(null);
          this.loading.set(false);
        },
        error: () => {
          this.adminSummary.set(this.emptyAdmin());
          this.storeSummary.set(null);
          this.error.set(this.locale.t('dashboard.loadError'));
          this.loading.set(false);
        },
      });
      return;
    }

    const storeId = this.activeStore.activeStoreId();
    if (!storeId) {
      this.storeSummary.set(null);
      this.adminSummary.set(null);
      this.error.set(this.locale.t('dashboard.noStore'));
      this.loading.set(false);
      return;
    }

    this.reportsApi.getStoreSummary(storeId, {}).subscribe({
      next: (s) => {
        this.storeSummary.set(this.normalizeStore(s));
        this.adminSummary.set(null);
        this.loading.set(false);
      },
      error: () => {
        this.storeSummary.set(null);
        this.adminSummary.set(null);
        this.error.set(this.locale.t('dashboard.loadError'));
        this.loading.set(false);
      },
    });
  }

  private normalizeStore(s: StoreReportSummaryDto | null): StoreReportSummaryDto | null {
    if (!s) return null;
    return {
      ...s,
      hourlySales: s.hourlySales ?? [],
      topProductsByUnits: s.topProductsByUnits ?? [],
      inventoryByCategory: s.inventoryByCategory ?? [],
      cashiers: s.cashiers ?? [],
      inventoryItems: s.inventoryItems ?? [],
      recentOrders: s.recentOrders ?? [],
    };
  }

  private normalizeAdmin(a: AdminReportSummaryDto): AdminReportSummaryDto {
    return {
      ...a,
      discountTotal: a.discountTotal ?? 0,
      productCount: a.productCount ?? 0,
      hourlySales: a.hourlySales ?? [],
      topProductsByUnits: a.topProductsByUnits ?? [],
      salesByCategory: a.salesByCategory ?? [],
      inventoryByCategory: a.inventoryByCategory ?? [],
      lowStockProducts: a.lowStockProducts ?? [],
      cashiers: a.cashiers ?? [],
      inventoryItems: a.inventoryItems ?? [],
      recentOrders: a.recentOrders ?? [],
      stores: a.stores ?? [],
    };
  }

  private emptyAdmin(): AdminReportSummaryDto {
    return {
      salesTotal: 0,
      discountTotal: 0,
      returnsTotal: 0,
      invoiceCount: 0,
      returnCount: 0,
      activeStoreCount: 0,
      lowStockCount: 0,
      productCount: 0,
      salesByStore: [],
      dailySales: [],
      hourlySales: [],
      topProducts: [],
      topProductsByUnits: [],
      salesByCategory: [],
      paymentBreakdown: [],
      inventoryByCategory: [],
      lowStockProducts: [],
      cashiers: [],
      inventoryItems: [],
      recentOrders: [],
      stores: [],
    };
  }
}
