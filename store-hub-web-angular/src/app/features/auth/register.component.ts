import { animate, style, transition, trigger } from '@angular/animations';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { LocaleService } from '../../core/i18n/locale.service';
import { ThemeService } from '../../core/theme/theme.service';
import { SiTranslatePipe } from '../../shared/pipes/si-translate.pipe';
import { AuthVisualComponent } from '../../shared/ui/auth-visual/auth-visual.component';
import { BrandMarkComponent } from '../../shared/ui/brand-mark/brand-mark.component';
import { PasswordToggleBtnComponent } from '../../shared/ui/password-toggle-btn/password-toggle-btn.component';

@Component({
  selector: 'app-register',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    SiTranslatePipe,
    BrandMarkComponent,
    AuthVisualComponent,
    PasswordToggleBtnComponent,
  ],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss',
  animations: [
    trigger('panelIn', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateY(10px)' }),
        animate('400ms cubic-bezier(0.22, 1, 0.36, 1)', style({ opacity: 1, transform: 'translateY(0)' })),
      ]),
    ]),
  ],
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  readonly locale = inject(LocaleService);
  readonly theme = inject(ThemeService);

  readonly submitting = signal(false);
  readonly message = signal<string | null>(null);
  readonly showPassword = signal(false);

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
  });

  togglePasswordVisibility(): void {
    this.showPassword.update((v) => !v);
  }

  submit(): void {
    this.message.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    // UI ready — API wiring comes with the store/auth feature work.
    setTimeout(() => {
      this.submitting.set(false);
      this.message.set(this.locale.t('register.soon'));
    }, 450);
  }
}
