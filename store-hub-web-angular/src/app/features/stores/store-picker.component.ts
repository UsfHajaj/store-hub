import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { LocaleService } from '../../core/i18n/locale.service';
import { ActiveStoreService } from '../../core/services/active-store.service';
import { SiTranslatePipe } from '../../shared/pipes/si-translate.pipe';
import { StoreListItemDto } from '../../shared/models/store.models';

@Component({
  selector: 'app-store-picker',
  imports: [SiTranslatePipe],
  templateUrl: './store-picker.component.html',
  styleUrl: './store-picker.component.scss',
})
export class StorePickerComponent {
  readonly activeStore = inject(ActiveStoreService);
  readonly locale = inject(LocaleService);
  private readonly router = inject(Router);

  constructor() {
    this.activeStore.refresh();
  }

  storeName(s: StoreListItemDto): string {
    return this.locale.lang() === 'ar' ? s.nameAr : s.nameEn;
  }

  pick(store: StoreListItemDto): void {
    this.activeStore.selectStore(store.id);
    void this.router.navigateByUrl('/dashboard');
  }
}
