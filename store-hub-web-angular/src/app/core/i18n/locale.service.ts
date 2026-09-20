import { DOCUMENT } from '@angular/common';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { Lang, TRANSLATIONS } from './translations';

const LS_KEY = 'storehub_lang';

@Injectable({ providedIn: 'root' })
export class LocaleService {
  private readonly document = inject(DOCUMENT);
  private readonly title = inject(Title);

  readonly lang = signal<Lang>(this.bootstrapLang());

  readonly isRtl = computed(() => this.lang() === 'ar');

  constructor() {
    this.applyToDocument();
  }

  setLang(next: Lang): void {
    if (this.lang() === next) return;
    this.lang.set(next);
    try {
      localStorage.setItem(LS_KEY, next);
    } catch {
      /* ignore */
    }
    this.applyToDocument();
  }

  toggleLang(): void {
    this.setLang(this.lang() === 'ar' ? 'en' : 'ar');
  }

  t(key: string, params?: Record<string, string | number>): string {
    this.lang();
    const table = TRANSLATIONS[this.lang()];
    let s = table[key] ?? TRANSLATIONS.en[key] ?? key;
    if (params) {
      for (const [k, v] of Object.entries(params)) {
        s = s.split(`{{${k}}}`).join(String(v));
      }
    }
    return s;
  }

  private bootstrapLang(): Lang {
    const stored = this.readStoredLang();
    return stored ?? 'ar';
  }

  private readStoredLang(): Lang | null {
    if (typeof localStorage === 'undefined') return null;
    try {
      const v = localStorage.getItem(LS_KEY);
      if (v === 'ar' || v === 'en') return v;
    } catch {
      /* ignore */
    }
    return null;
  }

  private applyToDocument(): void {
    const lang = this.lang();
    const html = this.document.documentElement;
    html.setAttribute('lang', lang);
    html.setAttribute('dir', lang === 'ar' ? 'rtl' : 'ltr');
    this.title.setTitle(TRANSLATIONS[lang]['app.title'] ?? 'StoreHub');
  }
}
