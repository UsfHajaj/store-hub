import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LocaleService } from '../../core/i18n/locale.service';
import { SiTranslatePipe } from '../../shared/pipes/si-translate.pipe';

@Component({
  selector: 'app-forbidden',
  imports: [RouterLink, SiTranslatePipe],
  template: `
    <div class="wrap">
      <div class="card">
        <div class="code">{{ 'forbidden.code' | siTranslate }}</div>
        <h1>{{ 'forbidden.title' | siTranslate }}</h1>
        <p>{{ 'forbidden.text' | siTranslate }}</p>
        <a routerLink="/dashboard" class="si-btn si-btn--primary">{{ 'forbidden.back' | siTranslate }}</a>
      </div>
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
        min-height: 100vh;
        background: var(--si-surface);
        color: var(--si-text);
      }
      .wrap {
        min-height: 100vh;
        display: grid;
        place-items: center;
        padding: 1.5rem;
      }
      .card {
        max-width: 440px;
        text-align: center;
        padding: 2.25rem 1.85rem;
        border-radius: var(--si-radius-lg);
        border: 1px solid var(--si-border);
        background: var(--si-surface-elevated);
        box-shadow: var(--si-shadow-lg);
        animation: pop 0.55s cubic-bezier(0.22, 1, 0.36, 1) both;
      }
      .code {
        font-size: 3rem;
        font-weight: 800;
        letter-spacing: -0.06em;
        background: linear-gradient(120deg, var(--si-primary), var(--si-accent));
        -webkit-background-clip: text;
        background-clip: text;
        color: transparent;
      }
      h1 {
        margin: 0.5rem 0 0.75rem;
        font-size: 1.3rem;
        font-weight: 800;
      }
      p {
        margin: 0 0 1.35rem;
        color: var(--si-muted);
        line-height: 1.55;
      }
      a.si-btn {
        text-decoration: none;
        display: inline-flex;
        justify-content: center;
      }
      @keyframes pop {
        from {
          opacity: 0;
          transform: translateY(16px) scale(0.96);
        }
        to {
          opacity: 1;
          transform: translateY(0) scale(1);
        }
      }
    `,
  ],
})
export class ForbiddenComponent {
  private readonly _locale = inject(LocaleService);
}
