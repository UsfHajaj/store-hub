export interface NotificationListItemDto {
  id: string;
  notificationType: number;
  title: string;
  message: string;
  channel: number;
  isRead: boolean;
  relatedEntityId: string | null;
  relatedEntityType: string | null;
  createdOnUtc: string;
}

export interface NotificationFilterParams {
  page: number;
  pageSize: number;
  unreadOnly?: boolean;
}
