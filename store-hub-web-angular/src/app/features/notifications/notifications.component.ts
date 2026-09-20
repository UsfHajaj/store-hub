import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NotificationsApiService } from '../../core/services/notifications-api.service';
import { SiTranslatePipe } from '../../shared/pipes/si-translate.pipe';
import { PagedResult } from '../../shared/models/api.types';
import { NotificationListItemDto } from '../../shared/models/notification.models';

@Component({
  selector: 'app-notifications',
  imports: [FormsModule, DatePipe, SiTranslatePipe],
  templateUrl: './notifications.component.html',
  styleUrl: './notifications.component.scss',
})
export class NotificationsComponent {
  private readonly api = inject(NotificationsApiService);

  readonly page = signal(1);
  readonly pageSize = signal(10);
  readonly result = signal<PagedResult<NotificationListItemDto> | null>(null);
  unreadOnly = false;

  constructor() {
    this.load();
  }

  load(): void {
    this.api
      .getMy({
        page: this.page(),
        pageSize: this.pageSize(),
        unreadOnly: this.unreadOnly ? true : undefined,
      })
      .subscribe((r) => this.result.set(r));
  }

  apply(): void {
    this.page.set(1);
    this.load();
  }

  prev(): void {
    const r = this.result();
    if (r?.hasPreviousPage) {
      this.page.update((p) => p - 1);
      this.load();
    }
  }

  next(): void {
    const r = this.result();
    if (r?.hasNextPage) {
      this.page.update((p) => p + 1);
      this.load();
    }
  }

  markOne(id: string): void {
    this.api.markRead(id).subscribe(() => this.load());
  }

  markAll(): void {
    this.api.markAllMyRead().subscribe(() => this.load());
  }
}
