import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { LocaleService } from './core/i18n/locale.service';
import { ThemeService } from './core/theme/theme.service';
import { ErrorDisplayService } from './core/services/error-display.service';
import { LoadingService } from './core/services/loading.service';
import { SiTranslatePipe } from './shared/pipes/si-translate.pipe';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, SiTranslatePipe],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
})
export class AppComponent {
  private readonly _locale = inject(LocaleService);
  private readonly _theme = inject(ThemeService);
  readonly loading = inject(LoadingService);
  readonly errors = inject(ErrorDisplayService);
}
