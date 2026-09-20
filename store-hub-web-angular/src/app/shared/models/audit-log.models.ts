export interface AuditLogListItemDto {
  id: string;
  action: string;
  entityType: string;
  entityId: string | null;
  performedByUserId: string | null;
  performedByUserName: string | null;
  performedByNameAr: string | null;
  performedByNameEn: string | null;
  entityTargetUserName?: string | null;
  entityTargetNameAr?: string | null;
  entityTargetNameEn?: string | null;
  occurredOnUtc: string;
  detailsJson: string | null;
}

export interface AuditLogFilterParams {
  page: number;
  pageSize: number;
  search?: string;
  fromOccurredOnUtc?: string;
  toOccurredOnUtc?: string;
  entityType?: string;
  action?: string;
  performedByUserId?: string;
}
