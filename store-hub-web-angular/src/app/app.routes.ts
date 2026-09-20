import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { guestGuard } from './core/guards/guest.guard';
import { permissionGuard } from './core/guards/permission.guard';
import { storeContextGuard } from './core/guards/store-context.guard';
import { PermissionCodes } from './shared/models/permission-codes';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login.component').then((m) => m.LoginComponent),
    canActivate: [guestGuard],
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/register.component').then((m) => m.RegisterComponent),
    canActivate: [guestGuard],
  },
  {
    path: 'change-password',
    loadComponent: () =>
      import('./features/auth/change-password.component').then((m) => m.ChangePasswordComponent),
    canActivate: [guestGuard],
  },
  {
    path: 'select-store',
    loadComponent: () =>
      import('./features/stores/store-picker.component').then((m) => m.StorePickerComponent),
    canActivate: [authGuard],
  },
  {
    path: 'profile/change-password',
    loadComponent: () =>
      import('./features/profile/change-password-page.component').then((m) => m.ChangePasswordPageComponent),
    canActivate: [authGuard],
  },
  {
    path: '',
    loadComponent: () => import('./layout/app-shell.component').then((m) => m.AppShellComponent),
    canActivate: [authGuard, storeContextGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadComponent: () => import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent),
      },
      {
        path: 'stores',
        loadComponent: () => import('./features/stores/stores.component').then((m) => m.StoresComponent),
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.StoreManage] },
      },
      {
        path: 'categories',
        loadComponent: () => import('./features/categories/categories.component').then((m) => m.CategoriesComponent),
        canActivate: [permissionGuard],
        data: {
          permissions: [
            PermissionCodes.PosCatalogView,
            PermissionCodes.PosCategoryManage,
            PermissionCodes.PosProductCreate,
            PermissionCodes.PosSaleCreate,
          ],
        },
      },
      {
        path: 'products',
        loadComponent: () => import('./features/products/products.component').then((m) => m.ProductsComponent),
        canActivate: [permissionGuard],
        data: {
          permissions: [
            PermissionCodes.PosCatalogView,
            PermissionCodes.PosCategoryManage,
            PermissionCodes.PosProductCreate,
            PermissionCodes.PosProductUpdate,
            PermissionCodes.PosSaleCreate,
          ],
        },
      },
      {
        path: 'discounts',
        loadComponent: () => import('./features/discounts/discounts.component').then((m) => m.DiscountsComponent),
        canActivate: [permissionGuard],
        data: {
          permissions: [
            PermissionCodes.PosCatalogView,
            PermissionCodes.PosDiscountManage,
            PermissionCodes.PosDiscountApply,
          ],
        },
      },
      {
        path: 'pos',
        loadComponent: () => import('./features/pos/pos.component').then((m) => m.PosComponent),
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.PosSaleCreate, PermissionCodes.StoreManage] },
      },
      {
        path: 'orders',
        loadComponent: () => import('./features/orders/orders.component').then((m) => m.OrdersComponent),
        canActivate: [permissionGuard],
        data: {
          permissions: [PermissionCodes.PosSaleView, PermissionCodes.PosSaleCreate, PermissionCodes.StoreManage],
        },
      },
      {
        path: 'inventory',
        loadComponent: () => import('./features/inventory/inventory.component').then((m) => m.InventoryComponent),
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.PosInventoryManage] },
      },
      {
        path: 'stocktake',
        loadComponent: () => import('./features/stocktake/stocktake.component').then((m) => m.StocktakeComponent),
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.PosStocktakeManage] },
      },
      {
        path: 'reports',
        loadComponent: () => import('./features/reports/reports.component').then((m) => m.ReportsComponent),
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.ReportView, PermissionCodes.StoreManage] },
      },
      {
        path: 'data-import',
        loadComponent: () =>
          import('./features/data-import/data-import.component').then((m) => m.DataImportComponent),
        canActivate: [permissionGuard],
        data: {
          permissions: [
            PermissionCodes.StoreManage,
            PermissionCodes.UserManage,
            PermissionCodes.PosCategoryManage,
            PermissionCodes.PosProductCreate,
          ],
        },
      },
      {
        path: 'invoice-settings',
        loadComponent: () =>
          import('./features/invoice-settings/invoice-settings.component').then((m) => m.InvoiceSettingsComponent),
        canActivate: [permissionGuard],
        data: {
          permissions: [
            PermissionCodes.StoreManage,
            PermissionCodes.StoreView,
            PermissionCodes.PosInventoryManage,
          ],
        },
      },
      {
        path: 'users',
        loadComponent: () => import('./features/users/users.component').then((m) => m.UsersComponent),
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.UserManage] },
      },
      {
        path: 'roles',
        loadComponent: () => import('./features/roles/roles.component').then((m) => m.RolesComponent),
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.RoleManage] },
      },
      {
        path: 'notifications',
        loadComponent: () =>
          import('./features/notifications/notifications.component').then((m) => m.NotificationsComponent),
      },
      {
        path: 'profile',
        loadComponent: () => import('./features/profile/profile.component').then((m) => m.ProfileComponent),
      },
    ],
  },
  {
    path: '403',
    loadComponent: () => import('./features/forbidden/forbidden.component').then((m) => m.ForbiddenComponent),
  },
  { path: '**', redirectTo: 'dashboard' },
];
