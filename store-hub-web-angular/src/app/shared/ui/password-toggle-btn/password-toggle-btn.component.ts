import { Component, input, output } from '@angular/core';
import { SiTranslatePipe } from '../../pipes/si-translate.pipe';

@Component({
  selector: 'app-password-toggle-btn',
  imports: [SiTranslatePipe],
  template: `
    <button
      type="button"
      class="si-pw-toggle"
      (click)="toggled.emit()"
      [attr.aria-label]="(visible() ? 'login.hidePassword' : 'login.showPassword') | siTranslate"
      [attr.aria-pressed]="visible()"
      [attr.title]="(visible() ? 'login.hidePassword' : 'login.showPassword') | siTranslate"
    >
      @if (visible()) {
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
          <path d="M17.94 17.94A10.94 10.94 0 0 1 12 20c-5 0-9.27-3.11-11-8 1.02-2.87 2.93-5.1 5.24-6.44" />
          <path d="M9.9 4.24A10.94 10.94 0 0 1 12 4c5 0 9.27 3.11 11 8a11.5 11.5 0 0 1-2.16 3.19" />
          <path d="M14.12 14.12a3 3 0 1 1-4.24-4.24" />
          <path d="M1 1l22 22" />
        </svg>
      } @else {
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
          <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8S1 12 1 12z" />
          <circle cx="12" cy="12" r="3" />
        </svg>
      }
    </button>
  `,
  styles: [
    `
      :host {
        display: contents;
      }
    `,
  ],
})
export class PasswordToggleBtnComponent {
  readonly visible = input(false);
  readonly toggled = output<void>();
}
