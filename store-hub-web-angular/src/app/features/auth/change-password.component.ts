import { animate, style, transition, trigger } from '@angular/animations';
import { Component, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { LocaleService } from '../../core/i18n/locale.service';
import { ThemeService } from '../../core/theme/theme.service';
import { SiTranslatePipe } from '../../shared/pipes/si-translate.pipe';
import { AuthVisualComponent } from '../../shared/ui/auth-visual/auth-visual.component';
import { BrandMarkComponent } from '../../shared/ui/brand-mark/brand-mark.component';
import { PasswordToggleBtnComponent } from '../../shared/ui/password-toggle-btn/password-toggle-btn.component';

function matchPasswords(group: AbstractControl): ValidationErrors | null {
  const password = group.get('password')?.value;
  const confirm = group.get('confirmPassword')?.value;
  if (!password || !confirm) return null;
  return password === confirm ? null : { mismatch: true };
}

@Component({
  selector: 'app-change-password',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    SiTranslatePipe,
    BrandMarkComponent,
    AuthVisualComponent,
    PasswordToggleBtnComponent,
  ],
  templateUrl: './change-password.component.html',
  styleUrl: './change-password.component.scss',
  animations: [
    trigger('panelIn', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateY(10px)' }),
        animate('400ms cubic-bezier(0.22, 1, 0.36, 1)', style({ opacity: 1, transform: 'translateY(0)' })),
      ]),
    ]),
  ],
})
export class ChangePasswordComponent {
  private readonly fb = inject(FormBuilder);
  readonly locale = inject(LocaleService);
  readonly theme = inject(ThemeService);

  readonly submitting = signal(false);
  readonly message = signal<string | null>(null);
  readonly showPassword = signal(false);
  readonly showConfirm = signal(false);

  readonly form = this.fb.nonNullable.group(
    {
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', [Validators.required]],
    },
    { validators: matchPasswords },
  );

  togglePasswordVisibility(): void {
    this.showPassword.update((v) => !v);
  }

  toggleConfirmVisibility(): void {
    this.showConfirm.update((v) => !v);
  }

  submit(): void {
    this.message.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    setTimeout(() => {
      this.submitting.set(false);
      this.message.set(this.locale.t('changePassword.soon'));
    }, 450);
  }
}
