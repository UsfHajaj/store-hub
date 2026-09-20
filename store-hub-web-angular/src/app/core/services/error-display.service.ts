import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ErrorDisplayService {
  readonly message = signal<string | null>(null);
  private clearTimer: ReturnType<typeof setTimeout> | null = null;

  show(text: string): void {
    const trimmed = text?.trim();
    if (!trimmed) return;
    this.message.set(trimmed);
    if (this.clearTimer) clearTimeout(this.clearTimer);
    this.clearTimer = setTimeout(() => this.clear(), 6500);
  }

  clear(): void {
    if (this.clearTimer) {
      clearTimeout(this.clearTimer);
      this.clearTimer = null;
    }
    this.message.set(null);
  }
}
