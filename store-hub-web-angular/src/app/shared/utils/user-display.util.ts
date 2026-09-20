import { LocaleService } from '../../core/i18n/locale.service';
import { UserListItemDto } from '../models/user.models';

export function formatUserDisplayName(row: UserListItemDto, locale: LocaleService): string {
  const isAr = locale.lang() === 'ar';
  const primary = isAr ? row.nameAr?.trim() : row.nameEn?.trim();
  if (primary) {
    return primary;
  }
  const secondary = isAr ? row.nameEn?.trim() : row.nameAr?.trim();
  if (secondary) {
    return secondary;
  }
  return row.userName;
}

export function userInitials(row: UserListItemDto, locale: LocaleService): string {
  const name = formatUserDisplayName(row, locale);
  const parts = name.split(/\s+/).filter(Boolean);
  if (parts.length >= 2) {
    return (parts[0][0] + parts[1][0]).toUpperCase();
  }
  return name.slice(0, 2).toUpperCase();
}
