import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../core/i18n/locale.service';
import { AuditLogsApiService } from '../../core/services/audit-logs-api.service';
import { SiTranslatePipe } from '../../shared/pipes/si-translate.pipe';
import { AuditLogFilterParams, AuditLogListItemDto } from '../../shared/models/audit-log.models';
import { PagedResult } from '../../shared/models/api.types';
import {
  formatAuditAction,
  formatAuditPerformer,
  formatAuditTarget,
} from '../../shared/utils/audit-display.util';
import { ListViewMode } from '../../shared/models/list-view-mode';

@Component({
  selector: 'app-audit-logs',
  imports: [FormsModule, DatePipe, SiTranslatePipe],
  templateUrl: './audit-logs.component.html',
  styleUrl: './audit-logs.component.scss',
})
export class AuditLogsComponent {
  private readonly api = inject(AuditLogsApiService);
  readonly locale = inject(LocaleService);

  readonly page = signal(1);
  readonly pageSize = signal(12);
  readonly result = signal<PagedResult<AuditLogListItemDto> | null>(null);
  readonly viewMode = signal<ListViewMode>('table');

  search = '';
  entityType = '';
  action = '';
  performedByUserId = '';
  fromLocal = '';
  toLocal = '';

  readonly selected = signal<AuditLogListItemDto | null>(null);
  readonly detailLoading = signal(false);

  constructor() {
    this.load();
  }

  load(): void {
    const params: AuditLogFilterParams = {
      page: this.page(),
      pageSize: this.pageSize(),
      search: this.search.trim() || undefined,
      entityType: this.entityType.trim() || undefined,
      action: this.action.trim() || undefined,
      performedByUserId: this.performedByUserId.trim() || undefined,
      fromOccurredOnUtc: this.fromLocal ? new Date(this.fromLocal).toISOString() : undefined,
      toOccurredOnUtc: this.toLocal ? new Date(this.toLocal).toISOString() : undefined,
    };
    this.api.getPaged(params).subscribe((r) => this.result.set(r));
  }

  applyFilters(): void {
    this.page.set(1);
    this.load();
  }

  setView(mode: ListViewMode): void {
    this.viewMode.set(mode);
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

  openRow(row: AuditLogListItemDto): void {
    this.selected.set(row);
    this.detailLoading.set(true);
    this.api.getById(row.id).subscribe({
      next: (full) => {
        this.selected.set(full);
        this.detailLoading.set(false);
      },
      error: () => this.detailLoading.set(false),
    });
  }

  closeDetail(): void {
    this.selected.set(null);
  }

  dash(): string {
    return this.locale.t('users.dash');
  }

  actionLabel(row: AuditLogListItemDto): string {
    return formatAuditAction(row, this.locale);
  }

  targetLabel(row: AuditLogListItemDto): string {
    return formatAuditTarget(row, this.locale);
  }

  performerLabel(row: AuditLogListItemDto): string {
    return formatAuditPerformer(row, this.locale);
  }

  isHttpRow(row: AuditLogListItemDto): boolean {
    return row.action === 'Http.Request';
  }
}
