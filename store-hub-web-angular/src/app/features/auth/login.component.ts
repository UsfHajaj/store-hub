import { animate, style, transition, trigger } from '@angular/animations';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { LocaleService } from '../../core/i18n/locale.service';
import { ThemeService } from '../../core/theme/theme.service';
import { ActiveStoreService } from '../../core/services/active-store.service';
import { StoresApiService } from '../../core/services/stores-api.service';
import { SiTranslatePipe } from '../../shared/pipes/si-translate.pipe';
import { AuthVisualComponent } from '../../shared/ui/auth-visual/auth-visual.component';
import { BrandMarkComponent } from '../../shared/ui/brand-mark/brand-mark.component';
import { PasswordToggleBtnComponent } from '../../shared/ui/password-toggle-btn/password-toggle-btn.component';
import { resolveUserFacingError } from '../../shared/utils/http-error.util';
import { safeReturnUrl } from '../../core/auth/auth-navigation.util';

@Component({
  selector: 'app-login',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    SiTranslatePipe,
    BrandMarkComponent,
    AuthVisualComponent,
    PasswordToggleBtnComponent,
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
  animations: [
    trigger('panelIn', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateY(10px)' }),
        animate('400ms cubic-bezier(0.22, 1, 0.36, 1)', style({ opacity: 1, transform: 'translateY(0)' })),
      ]),
    ]),
  ],
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly storesApi = inject(StoresApiService);
  private readonly activeStore = inject(ActiveStoreService);
  readonly locale = inject(LocaleService);
  readonly theme = inject(ThemeService);

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly showPassword = signal(false);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(1)]],
  });

  togglePasswordVisibility(): void {
    this.showPassword.update((v) => !v);
  }

  private resolveErrorMessage(err: unknown): string {
    const msg = resolveUserFacingError(err, (key) => this.locale.t(key));
    if (msg === this.locale.t('errors.unauthorized') || msg === this.locale.t('errors.forbidden')) {
      return this.locale.t('login.errorInvalid');
    }
    if (msg === this.locale.t('errors.network')) return this.locale.t('login.errorNetwork');
    if (msg === this.locale.t('errors.server')) return this.locale.t('login.errorServer');
    if (msg === this.locale.t('errors.generic')) return this.locale.t('login.errorGeneric');
    return msg;
  }

  submit(): void {
    this.errorMessage.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    const { email, password } = this.form.getRawValue();
    this.auth.login({ email, password }).subscribe({
      next: () => {
        this.storesApi.getMine().subscribe({
          next: (list) => {
            this.activeStore.stores.set(list);
            this.submitting.set(false);
            const returnUrl = safeReturnUrl(this.route.snapshot.queryParamMap.get('returnUrl'));
            if (returnUrl) {
              void this.router.navigateByUrl(returnUrl);
              return;
            }
            if (list.length > 1) {
              const current = this.activeStore.activeStoreId();
              if (!current || !list.some((s) => s.id === current)) {
                void this.router.navigateByUrl('/select-store');
                return;
              }
            } else if (list.length === 1) {
              this.activeStore.selectStore(list[0].id);
            }
            void this.router.navigateByUrl('/dashboard');
          },
          error: () => {
            this.submitting.set(false);
            void this.router.navigateByUrl('/dashboard');
          },
        });
      },
      error: (err: unknown) => {
        this.submitting.set(false);
        this.errorMessage.set(this.resolveErrorMessage(err));
      },
    });
  }
}
