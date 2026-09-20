export interface ApiResponse<T = unknown> {
  success: boolean;
  data?: T;
  errors?: readonly string[] | null;
  traceId?: string | null;
}

export interface PagedResult<T> {
  items: readonly T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}
