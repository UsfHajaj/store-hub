import { DatePipe } from '@angular/common';
import { Component, HostListener, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs/operators';
import { AuthService } from '../core/auth/auth.service';
import { LocaleService } from '../core/i18n/locale.service';
import { ThemeService } from '../core/theme/theme.service';
import { NotificationsApiService } from '../core/services/notifications-api.service';
import { ActiveStoreService } from '../core/services/active-store.service';
import { SiTranslatePipe } from '../shared/pipes/si-translate.pipe';
import { PermissionCodes } from '../shared/models/permission-codes';
import { NotificationListItemDto } from '../shared/models/notification.models';
import { BrandMarkComponent } from '../shared/ui/brand-mark/brand-mark.component';

const SIDEBAR_LS_KEY = 'si_sidebar_collapsed';

const ROUTE_TITLE_KEYS: Record<string, string> = {
  '/dashboard': 'nav.dashboard',
  '/stores': 'nav.stores',
  '/categories': 'nav.categories',
  '/products': 'nav.products',
  '/discounts': 'nav.discounts',
  '/pos': 'nav.pos',
  '/orders': 'nav.orders',
  '/inventory': 'nav.inventory',
  '/stocktake': 'nav.stocktake',
  '/reports': 'nav.reports',
  '/data-import': 'nav.dataImport',
  '/invoice-settings': 'nav.invoiceSettings',
  '/users': 'nav.users',
  '/roles': 'nav.roles',
  '/notifications': 'nav.notifications',
  '/profile': 'nav.profile',
  '/profile/change-password': 'changePassword.heading',
};

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, DatePipe, SiTranslatePipe, BrandMarkComponent],
  templateUrl: './app-shell.component.html',
  styleUrl: './app-shell.component.scss',
})
export class AppShellComponent {
  readonly auth = inject(AuthService);
  readonly locale = inject(LocaleService);
  readonly theme = inject(ThemeService);
  readonly activeStore = inject(ActiveStoreService);
  private readonly router = inject(Router);
  private readonly notificationsApi = inject(NotificationsApiService);

  readonly PermissionCodes = PermissionCodes;
  readonly catalogViewPermissions = [
    PermissionCodes.PosCatalogView,
    PermissionCodes.PosCategoryManage,
    PermissionCodes.PosProductCreate,
    PermissionCodes.PosSaleCreate,
  ] as const;
  readonly productNavPermissions = [
    PermissionCodes.PosCatalogView,
    PermissionCodes.PosCategoryManage,
    PermissionCodes.PosProductCreate,
    PermissionCodes.PosProductUpdate,
    PermissionCodes.PosSaleCreate,
  ] as const;
  readonly discountNavPermissions = [
    PermissionCodes.PosCatalogView,
    PermissionCodes.PosDiscountManage,
    PermissionCodes.PosDiscountApply,
  ] as const;
  readonly orderNavPermissions = [
    PermissionCodes.PosSaleView,
    PermissionCodes.PosSaleCreate,
    PermissionCodes.StoreManage,
  ] as const;
  readonly invoiceSettingsPermissions = [
    PermissionCodes.StoreManage,
    PermissionCodes.StoreView,
    PermissionCodes.PosInventoryManage,
  ] as const;
  readonly dataImportPermissions = [
    PermissionCodes.StoreManage,
    PermissionCodes.UserManage,
    PermissionCodes.PosCategoryManage,
    PermissionCodes.PosProductCreate,
  ] as const;
  readonly unreadCount = signal(0);
  readonly sidebarCollapsed = signal(this.readSidebarCollapsed());
  readonly pageTitleKey = signal('nav.dashboard');
  readonly userMenuOpen = signal(false);
  readonly mobileSidebarOpen = signal(false);
  readonly sidebarTip = signal<{ text: string; x: number; y: number; rtl: boolean } | null>(null);

  readonly notifOpen = signal(false);
  readonly notifLoading = signal(false);
  readonly notifItems = signal<readonly NotificationListItemDto[]>([]);

  constructor() {
    this.syncPageTitle(this.router.url);
    this.refreshUnread();

    this.router.events.pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd)).subscribe((e) => {
      this.syncPageTitle(e.urlAfterRedirects);
      this.refreshUnread();
      this.mobileSidebarOpen.set(false);
    });
  }

  toggleSidebar(): void {
    if (this.isMobileViewport()) {
      this.mobileSidebarOpen.update((v) => !v);
      this.hideSidebarTip();
      return;
    }
    this.sidebarCollapsed.update((v) => !v);
    this.hideSidebarTip();
    try {
      localStorage.setItem(SIDEBAR_LS_KEY, String(this.sidebarCollapsed()));
    } catch {
      /* ignore */
    }
  }

  closeMobileSidebar(): void {
    this.mobileSidebarOpen.set(false);
    this.hideSidebarTip();
  }

  onSidebarTip(event: Event): void {
    if (!this.sidebarCollapsed() || this.mobileSidebarOpen()) {
      this.hideSidebarTip();
      return;
    }

    const target = event.target as HTMLElement | null;
    const host = target?.closest?.('[data-tooltip]') as HTMLElement | null;
    if (!host) {
      this.hideSidebarTip();
      return;
    }

    const text = host.getAttribute('data-tooltip')?.trim();
    if (!text) {
      this.hideSidebarTip();
      return;
    }

    const rect = host.getBoundingClientRect();
    const rtl = document.documentElement.getAttribute('dir') === 'rtl';
    const gap = 12;
    this.sidebarTip.set({
      text,
      x: rtl ? rect.left - gap : rect.right + gap,
      y: rect.top + rect.height / 2,
      rtl,
    });
  }

  hideSidebarTip(): void {
    this.sidebarTip.set(null);
  }

  toggleUserMenu(event: Event): void {
    event.stopPropagation();
    this.userMenuOpen.update((v) => !v);
  }

  @HostListener('document:click')
  closeUserMenu(): void {
    this.userMenuOpen.set(false);
  }

  @HostListener('window:resize')
  onResize(): void {
    if (!this.isMobileViewport()) {
      this.mobileSidebarOpen.set(false);
    }
  }

  logout(): void {
    this.activeStore.clear();
    this.unreadCount.set(0);
    this.auth.logout(true);
  }

  hasAnyPermission(codes: readonly string[]): boolean {
    return this.auth.hasAnyPermission(codes);
  }

  displayName(): string {
    this.locale.lang();
    const s = this.auth.sessionSnapshot();
    return s?.userName || s?.email || this.locale.t('common.guest');
  }

  userEmail(): string {
    return this.auth.sessionSnapshot()?.email ?? '';
  }

  userInitials(): string {
    const name = this.displayName().trim();
    const parts = name.split(/\s+/).filter(Boolean);
    if (parts.length >= 2) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }
    return name.slice(0, 2).toUpperCase() || 'U';
  }

  pageTitle(): string {
    return this.locale.t(this.pageTitleKey());
  }

  activeStoreLabel(): string {
    this.locale.lang();
    const s = this.activeStore.activeStore();
    if (!s) return '';
    return this.locale.lang() === 'ar' ? s.nameAr : s.nameEn;
  }

  switchStore(): void {
    void this.router.navigateByUrl('/select-store');
  }

  openNotifications(): void {
    this.notifOpen.set(true);
    this.loadRecentNotifications();
  }

  closeNotifications(): void {
    this.notifOpen.set(false);
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.notifOpen.set(false);
    this.userMenuOpen.set(false);
  }

  markNotif(id: string): void {
    this.notificationsApi.markRead(id).subscribe(() => {
      this.loadRecentNotifications();
      this.refreshUnread();
    });
  }

  markAllNotifications(): void {
    this.notificationsApi.markAllMyRead().subscribe(() => {
      this.loadRecentNotifications();
      this.refreshUnread();
    });
  }

  private loadRecentNotifications(): void {
    this.notifLoading.set(true);
    this.notificationsApi.getMy({ page: 1, pageSize: 6 }).subscribe({
      next: (r) => {
        this.notifItems.set(r.items);
        this.notifLoading.set(false);
      },
      error: () => {
        this.notifItems.set([]);
        this.notifLoading.set(false);
      },
    });
  }

  private syncPageTitle(url: string): void {
    const path = url.split('?')[0].split('#')[0];
    this.pageTitleKey.set(ROUTE_TITLE_KEYS[path] ?? 'nav.dashboard');
  }

  private refreshUnread(): void {
    if (!this.auth.isAuthenticated()) {
      this.unreadCount.set(0);
      return;
    }
    this.notificationsApi.getMyUnreadCount().subscribe({
      next: (n) => this.unreadCount.set(n),
      error: () => this.unreadCount.set(0),
    });
  }

  private readSidebarCollapsed(): boolean {
    try {
      return localStorage.getItem(SIDEBAR_LS_KEY) === 'true';
    } catch {
      return false;
    }
  }

  private isMobileViewport(): boolean {
    return typeof window !== 'undefined' && window.innerWidth < 768;
  }
}
