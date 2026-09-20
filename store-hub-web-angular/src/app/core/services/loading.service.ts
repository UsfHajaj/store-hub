import { Injectable, computed, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class LoadingService {
  private readonly depth = signal(0);

  readonly active = computed(() => this.depth() > 0);

  begin(): void {
    this.depth.update((d) => d + 1);
  }

  end(): void {
    this.depth.update((d) => Math.max(0, d - 1));
  }
}
