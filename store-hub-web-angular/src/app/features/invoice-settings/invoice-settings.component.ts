import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/auth/auth.service';
import { LocaleService } from '../../core/i18n/locale.service';
import { ActiveStoreService } from '../../core/services/active-store.service';
import { StoresApiService } from '../../core/services/stores-api.service';
import { SiTranslatePipe } from '../../shared/pipes/si-translate.pipe';
import { PermissionCodes } from '../../shared/models/permission-codes';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-invoice-settings',
  imports: [FormsModule, SiTranslatePipe],
  templateUrl: './invoice-settings.component.html',
  styleUrl: './invoice-settings.component.scss',
})
export class InvoiceSettingsComponent {
  private readonly api = inject(StoresApiService);
  private readonly activeStore = inject(ActiveStoreService);
  private readonly auth = inject(AuthService);
  readonly locale = inject(LocaleService);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly uploading = signal(false);
  readonly saved = signal(false);

  invoiceDisplayNameAr = '';
  invoiceDisplayNameEn = '';
  taxNumber = '';
  invoiceFooter = '';
  logoUrl = '';

  readonly canManage = computed(
    () =>
      this.auth.hasPermission(PermissionCodes.StoreManage) ||
      this.auth.hasPermission(PermissionCodes.StoreView) ||
      this.auth.hasPermission(PermissionCodes.PosInventoryManage),
  );

  constructor() {
    this.load();
  }

  storeLabel(): string {
    this.locale.lang();
    const s = this.activeStore.activeStore();
    if (!s) return '';
    return this.locale.lang() === 'ar' ? s.nameAr : s.nameEn;
  }

  resolveImage(url: string | null | undefined): string | null {
    if (!url?.trim()) return null;
    if (url.startsWith('http://') || url.startsWith('https://') || url.startsWith('data:')) return url;
    const base = (environment.apiBaseUrl || '').replace(/\/$/, '');
    return `${base}${url.startsWith('/') ? url : `/${url}`}`;
  }

  load(): void {
    const storeId = this.activeStore.activeStoreId();
    if (!storeId) {
      this.loading.set(false);
      return;
    }
    this.loading.set(true);
    this.api.getInvoiceSettings(storeId).subscribe({
      next: (store) => {
        this.invoiceDisplayNameAr = store.invoiceDisplayNameAr ?? '';
        this.invoiceDisplayNameEn = store.invoiceDisplayNameEn ?? '';
        this.taxNumber = store.taxNumber ?? '';
        this.invoiceFooter = store.invoiceFooter ?? '';
        this.logoUrl = store.logoUrl ?? '';
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  onLogoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    const storeId = this.activeStore.activeStoreId();
    if (!file || !storeId) return;
    this.uploading.set(true);
    this.api.uploadLogo(storeId, file).subscribe({
      next: (res) => {
        this.logoUrl = res.imageUrl;
        this.uploading.set(false);
      },
      error: () => this.uploading.set(false),
    });
  }

  clearLogo(): void {
    this.logoUrl = '';
  }

  save(): void {
    const storeId = this.activeStore.activeStoreId();
    if (!storeId || this.saving() || this.uploading()) return;
    this.saving.set(true);
    this.saved.set(false);
    this.api
      .updateInvoiceSettings(storeId, {
        invoiceDisplayNameAr: this.invoiceDisplayNameAr.trim() || null,
        invoiceDisplayNameEn: this.invoiceDisplayNameEn.trim() || null,
        taxNumber: this.taxNumber.trim() || null,
        invoiceFooter: this.invoiceFooter.trim() || null,
        logoUrl: this.logoUrl.trim() || null,
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.saved.set(true);
        },
        error: () => this.saving.set(false),
      });
  }
}
