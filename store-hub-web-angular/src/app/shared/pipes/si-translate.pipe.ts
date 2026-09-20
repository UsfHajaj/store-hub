import { Pipe, PipeTransform, inject } from '@angular/core';
import { LocaleService } from '../../core/i18n/locale.service';

@Pipe({
  name: 'siTranslate',
  standalone: true,
  pure: false,
})
export class SiTranslatePipe implements PipeTransform {
  private readonly locale = inject(LocaleService);

  transform(key: string, params?: Record<string, string | number> | null): string {
    return this.locale.t(key, params ?? undefined);
  }
}
