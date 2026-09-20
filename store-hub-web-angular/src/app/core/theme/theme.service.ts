import { DOCUMENT } from '@angular/common';
import { Injectable, computed, inject, signal } from '@angular/core';

export type ThemeMode = 'light' | 'dark';

const LS_KEY = 'storehub_theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);

  readonly theme = signal<ThemeMode>(this.bootstrapTheme());
  readonly isDark = computed(() => this.theme() === 'dark');

  constructor() {
    this.applyToDocument();
  }

  setTheme(next: ThemeMode): void {
    if (this.theme() === next) return;
    this.theme.set(next);
    try {
      localStorage.setItem(LS_KEY, next);
    } catch {
      /* ignore */
    }
    this.applyToDocument();
  }

  toggleTheme(): void {
    this.setTheme(this.theme() === 'dark' ? 'light' : 'dark');
  }

  private bootstrapTheme(): ThemeMode {
    const stored = this.readStored();
    if (stored) return stored;
    if (typeof window !== 'undefined' && window.matchMedia?.('(prefers-color-scheme: dark)').matches) {
      return 'dark';
    }
    return 'light';
  }

  private readStored(): ThemeMode | null {
    if (typeof localStorage === 'undefined') return null;
    try {
      const v = localStorage.getItem(LS_KEY);
      if (v === 'light' || v === 'dark') return v;
    } catch {
      /* ignore */
    }
    return null;
  }

  private applyToDocument(): void {
    const theme = this.theme();
    const html = this.document.documentElement;
    html.setAttribute('data-theme', theme);
    const meta = this.document.querySelector('meta[name="theme-color"]');
    if (meta) {
      meta.setAttribute('content', theme === 'dark' ? '#171312' : '#EC5B38');
    }
  }
}
