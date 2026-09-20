import { Component, inject, signal } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';
import { LocaleService } from '../../core/i18n/locale.service';
import { ActiveStoreService } from '../../core/services/active-store.service';
import { DataImportApiService } from '../../core/services/data-import-api.service';
import { SiTranslatePipe } from '../../shared/pipes/si-translate.pipe';
import { PermissionCodes } from '../../shared/models/permission-codes';
import { DataImportKind, DataImportResultDto, downloadBlob } from '../../shared/models/data-import.models';

interface ImportCard {
  kind: DataImportKind;
  titleKey: string;
  descKey: string;
  needsStore: boolean;
  canAccess: () => boolean;
}

@Component({
  selector: 'app-data-import',
  imports: [SiTranslatePipe],
  templateUrl: './data-import.component.html',
  styleUrl: './data-import.component.scss',
})
export class DataImportComponent {
  private readonly api = inject(DataImportApiService);
  private readonly auth = inject(AuthService);
  readonly activeStore = inject(ActiveStoreService);
  readonly locale = inject(LocaleService);

  readonly busyKind = signal<DataImportKind | null>(null);
  readonly message = signal<string | null>(null);
  readonly result = signal<DataImportResultDto | null>(null);
  readonly error = signal<string | null>(null);

  readonly cards: ImportCard[] = [
    {
      kind: 'Stores',
      titleKey: 'dataImport.stores',
      descKey: 'dataImport.storesDesc',
      needsStore: false,
      canAccess: () => this.auth.hasPermission(PermissionCodes.StoreManage),
    },
    {
      kind: 'Categories',
      titleKey: 'dataImport.categories',
      descKey: 'dataImport.categoriesDesc',
      needsStore: true,
      canAccess: () =>
        this.auth.hasPermission(PermissionCodes.PosCategoryManage) ||
        this.auth.hasPermission(PermissionCodes.StoreManage),
    },
    {
      kind: 'Products',
      titleKey: 'dataImport.products',
      descKey: 'dataImport.productsDesc',
      needsStore: true,
      canAccess: () =>
        this.auth.hasPermission(PermissionCodes.PosProductCreate) ||
        this.auth.hasPermission(PermissionCodes.StoreManage),
    },
    {
      kind: 'Users',
      titleKey: 'dataImport.users',
      descKey: 'dataImport.usersDesc',
      needsStore: false,
      canAccess: () => this.auth.hasPermission(PermissionCodes.UserManage),
    },
  ];

  canUseAll(): boolean {
    return this.visibleCards().length > 0;
  }

  visibleCards(): ImportCard[] {
    return this.cards.filter((c) => c.canAccess());
  }

  storeLabel(): string {
    const s = this.activeStore.activeStore();
    if (!s) return '';
    return this.locale.lang() === 'ar' ? s.nameAr : s.nameEn;
  }

  download(kind: DataImportKind, sample: boolean): void {
    this.error.set(null);
    this.busyKind.set(kind);
    this.api.downloadTemplate(kind, sample).subscribe({
      next: (blob) => {
        downloadBlob(blob, `${kind.toLowerCase()}-${sample ? 'sample' : 'template'}.xlsx`);
        this.busyKind.set(null);
      },
      error: () => {
        this.error.set(this.locale.t('dataImport.downloadError'));
        this.busyKind.set(null);
      },
    });
  }

  onFile(kind: DataImportKind, needsStore: boolean, event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;

    // All can resolve stores from StoreNameAr; store fallback still helps
    if (needsStore && kind !== 'All' && !this.activeStore.activeStoreId()) {
      this.error.set(this.locale.t('dashboard.noStore'));
      return;
    }

    this.error.set(null);
    this.message.set(null);
    this.result.set(null);
    this.busyKind.set(kind);

    this.api.import(kind, file, this.activeStore.activeStoreId()).subscribe({
      next: (r) => {
        this.result.set(r);
        this.message.set(
          this.locale.t('dataImport.done', {
            created: r.created,
            skipped: r.skipped,
            failed: r.failed,
          }),
        );
        this.busyKind.set(null);
      },
      error: (err) => {
        const msg =
          err?.error?.errors?.[0] ||
          err?.error?.message ||
          this.locale.t('dataImport.uploadError');
        this.error.set(typeof msg === 'string' ? msg : this.locale.t('dataImport.uploadError'));
        this.busyKind.set(null);
      },
    });
  }
}
