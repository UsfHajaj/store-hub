import { LocaleService } from '../../core/i18n/locale.service';
import { AuditLogListItemDto } from '../models/audit-log.models';

interface HttpAuditDetails {
  method?: string;
  path?: string;
  userName?: string;
}

const DOMAIN_ACTION_KEYS: Record<string, string> = {
  'Http.Request': 'audit.action.httpRequest',
  Inserted: 'audit.action.inserted',
  Updated: 'audit.action.updated',
  Deleted: 'audit.action.deleted',
  SoftDeleted: 'audit.action.softDeleted',
};

const ENTITY_KEYS: Record<string, string> = {
  User: 'audit.entity.user',
  Role: 'audit.entity.role',
  Permission: 'audit.entity.permission',
  Notification: 'audit.entity.notification',
  Store: 'audit.entity.store',
  ProductCategory: 'audit.entity.productCategory',
  Product: 'audit.entity.product',
  ProductDiscount: 'audit.entity.productDiscount',
};

export function formatAuditPerformer(row: AuditLogListItemDto, locale: LocaleService): string {
  const isAr = locale.lang() === 'ar';
  const localized = isAr ? row.performedByNameAr?.trim() : row.performedByNameEn?.trim();
  if (localized) {
    return localized;
  }
  const fallback = isAr ? row.performedByNameEn?.trim() : row.performedByNameAr?.trim();
  if (fallback) {
    return fallback;
  }
  if (row.performedByUserName?.trim()) {
    return row.performedByUserName.trim();
  }
  return locale.t('audit.systemUser');
}

export function formatAuditAction(row: AuditLogListItemDto, locale: LocaleService): string {
  if (row.action === 'Http.Request' || row.entityType.includes('/api')) {
    const human = resolveHttpHumanLabel(row, locale);
    if (human) {
      return human;
    }
    return locale.t('audit.action.httpRequest');
  }

  const key = DOMAIN_ACTION_KEYS[row.action];
  if (key) {
    return locale.t(key);
  }

  if (/^\s*(GET|POST|PUT|PATCH|DELETE)\s+\/api\//i.test(row.action) || row.action.includes('/api/')) {
    return locale.t('audit.action.httpRequest');
  }

  return row.action;
}

export function formatAuditTarget(row: AuditLogListItemDto, locale: LocaleService): string {
  if (row.action === 'Http.Request' || row.entityType.includes('/api')) {
    // Action already carries the human description for HTTP rows.
    return locale.t('audit.target.systemActivity');
  }

  if (row.entityType === 'User') {
    const name = pickEntityUserName(row, locale);
    if (name) {
      return locale.t('audit.target.userNamed', { name });
    }
  }

  const entityLabel = ENTITY_KEYS[row.entityType]
    ? locale.t(ENTITY_KEYS[row.entityType])
    : row.entityType;

  if (row.entityId) {
    return `${entityLabel} · ${shortId(row.entityId)}`;
  }

  return entityLabel;
}

function resolveHttpHumanLabel(row: AuditLogListItemDto, locale: LocaleService): string | null {
  const details = parseHttpDetails(row.detailsJson);
  const method = (details.method ?? extractMethodFromEntityType(row.entityType) ?? 'GET').toUpperCase();
  const path = normalizePath(details.path ?? extractPathFromEntityType(row.entityType));
  const pathKey = resolvePathKey(path, method);
  return pathKey ? locale.t(pathKey) : null;
}

function pickEntityUserName(row: AuditLogListItemDto, locale: LocaleService): string | null {
  const isAr = locale.lang() === 'ar';
  const localized = isAr ? row.entityTargetNameAr?.trim() : row.entityTargetNameEn?.trim();
  if (localized) {
    return localized;
  }
  const fallback = isAr ? row.entityTargetNameEn?.trim() : row.entityTargetNameAr?.trim();
  if (fallback) {
    return fallback;
  }
  if (row.entityTargetUserName?.trim()) {
    return row.entityTargetUserName.trim();
  }
  return null;
}

function parseHttpDetails(json: string | null | undefined): HttpAuditDetails {
  if (!json?.trim()) {
    return {};
  }
  try {
    return JSON.parse(json) as HttpAuditDetails;
  } catch {
    return {};
  }
}

function extractMethodFromEntityType(entityType: string): string | undefined {
  const match = /^(\w+)\s+\S+/.exec(entityType.trim());
  return match?.[1];
}

function extractPathFromEntityType(entityType: string): string | undefined {
  const match = /^\w+\s+(\S+)/.exec(entityType.trim());
  return match?.[1];
}

function normalizePath(path: string | undefined): string {
  if (!path?.trim()) {
    return '';
  }
  let p = path.trim();
  const q = p.indexOf('?');
  if (q >= 0) {
    p = p.slice(0, q);
  }
  return p.replace(/\/[0-9a-f-]{36}/gi, '/{id}').replace(/\/\d+/g, '/{id}');
}

function resolvePathKey(path: string, method: string): string | null {
  const m = method.toUpperCase();
  const rules: { pattern: RegExp; key: string; methods?: string[] }[] = [
    { pattern: /^\/api\/auth\/login$/i, key: 'audit.path.login', methods: ['POST'] },
    { pattern: /^\/api\/auth\/change-password$/i, key: 'audit.path.changePassword', methods: ['POST'] },
    { pattern: /^\/api\/users$/i, key: m === 'GET' ? 'audit.path.usersList' : 'audit.path.usersManage' },
    { pattern: /^\/api\/users\/role-options$/i, key: 'audit.path.usersList', methods: ['GET'] },
    { pattern: /^\/api\/users\/\{id\}$/i, key: m === 'GET' ? 'audit.path.userDetail' : m === 'DELETE' ? 'audit.path.userDelete' : 'audit.path.usersManage' },
    { pattern: /^\/api\/users\/\{id\}\/activate$/i, key: 'audit.path.userActivate', methods: ['POST'] },
    { pattern: /^\/api\/users\/\{id\}\/deactivate$/i, key: 'audit.path.userDeactivate', methods: ['POST'] },
    { pattern: /^\/api\/users\/\{id\}\/roles$/i, key: 'audit.path.userRoles', methods: ['PUT'] },
    { pattern: /^\/api\/roles$/i, key: m === 'GET' ? 'audit.path.rolesList' : 'audit.path.rolesManage' },
    { pattern: /^\/api\/roles\/\{id\}$/i, key: m === 'GET' ? 'audit.path.roleDetail' : m === 'DELETE' ? 'audit.path.roleDelete' : 'audit.path.rolesManage' },
    { pattern: /^\/api\/roles\/\{id\}\/permissions$/i, key: 'audit.path.rolePermissions', methods: ['PUT'] },
    { pattern: /^\/api\/permissions$/i, key: 'audit.path.permissionsList', methods: ['GET'] },
    { pattern: /^\/api\/stores$/i, key: m === 'GET' ? 'audit.path.storesList' : 'audit.path.storesManage' },
    { pattern: /^\/api\/stores\/mine$/i, key: 'audit.path.storesMine', methods: ['GET'] },
    { pattern: /^\/api\/stores\/\{id\}$/i, key: m === 'GET' ? 'audit.path.storeDetail' : 'audit.path.storesManage' },
    { pattern: /^\/api\/stores\/\{id\}\/activate$/i, key: 'audit.path.storeActivate', methods: ['POST'] },
    { pattern: /^\/api\/stores\/\{id\}\/deactivate$/i, key: 'audit.path.storeDeactivate', methods: ['POST'] },
    { pattern: /^\/api\/stores\/\{id\}\/members$/i, key: m === 'GET' ? 'audit.path.storeMembers' : 'audit.path.storeMembersSet' },
    {
      pattern: /^\/api\/stores\/\{id\}\/categories$/i,
      key: m === 'GET' ? 'audit.path.categoriesList' : 'audit.path.categoriesManage',
    },
    {
      pattern: /^\/api\/stores\/\{id\}\/categories\/\{id\}$/i,
      key: m === 'DELETE' ? 'audit.path.categoryDelete' : 'audit.path.categoriesManage',
    },
    {
      pattern: /^\/api\/stores\/\{id\}\/products$/i,
      key: m === 'GET' ? 'audit.path.productsList' : 'audit.path.productsManage',
    },
    {
      pattern: /^\/api\/stores\/\{id\}\/products\/\{id\}$/i,
      key: m === 'GET' ? 'audit.path.productDetail' : m === 'DELETE' ? 'audit.path.productDelete' : 'audit.path.productsManage',
    },
    {
      pattern: /^\/api\/stores\/\{id\}\/discounts$/i,
      key: m === 'GET' ? 'audit.path.discountsList' : 'audit.path.discountsManage',
    },
    {
      pattern: /^\/api\/stores\/\{id\}\/discounts\/\{id\}$/i,
      key: m === 'GET' ? 'audit.path.discountDetail' : m === 'DELETE' ? 'audit.path.discountDelete' : 'audit.path.discountsManage',
    },
    { pattern: /^\/api\/audit-logs$/i, key: 'audit.path.auditLogs', methods: ['GET'] },
    { pattern: /^\/api\/audit-logs\/\{id\}$/i, key: 'audit.path.auditLogDetail', methods: ['GET'] },
    { pattern: /^\/api\/notifications$/i, key: 'audit.path.notifications', methods: ['GET'] },
    { pattern: /^\/api\/notifications\/my$/i, key: 'audit.path.notificationsMy', methods: ['GET'] },
    { pattern: /^\/api\/notifications\/my\/unread-count$/i, key: 'audit.path.notificationsUnreadCount', methods: ['GET'] },
    { pattern: /^\/api\/notifications\/mark-all-read$/i, key: 'audit.path.notificationsMarkAll', methods: ['POST'] },
    { pattern: /^\/api\/notifications\/\{id\}\/read$/i, key: 'audit.path.notificationRead', methods: ['POST'] },
  ];

  for (const rule of rules) {
    if (!rule.pattern.test(path)) {
      continue;
    }
    if (rule.methods && !rule.methods.includes(m)) {
      continue;
    }
    return rule.key;
  }

  return null;
}

function shortId(id: string): string {
  return id.length > 8 ? `${id.slice(0, 8)}…` : id;
}
