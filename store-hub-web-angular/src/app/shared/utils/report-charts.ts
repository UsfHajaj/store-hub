import { DailySalesPointDto, NamedAmountDto } from '../models/reports.models';
import { SiChartDataset } from '../ui/si-chart/si-chart.component';

const GUID_RE =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

function isReadableName(value: string | null | undefined): value is string {
  const t = value?.trim();
  if (!t) return false;
  if (GUID_RE.test(t)) return false;
  return true;
}

/** Prefer a human product/category name; avoid raw ids / empty values. */
export function displayName(item: NamedAmountDto, lang: string, fallback = '—'): string {
  const primary = lang === 'ar' ? item.nameAr : item.nameEn;
  const secondary = lang === 'ar' ? item.nameEn : item.nameAr;
  if (isReadableName(primary)) return primary.trim();
  if (isReadableName(secondary)) return secondary.trim();
  return fallback;
}

export function namedLabels(items: readonly NamedAmountDto[], lang: string): string[] {
  return items.map((n) => displayName(n, lang));
}

export function namedAmounts(items: readonly NamedAmountDto[]): number[] {
  return items.map((n) => Number(n.amount) || 0);
}

export function namedCounts(items: readonly NamedAmountDto[]): number[] {
  return items.map((n) => Number(n.count) || 0);
}

export function singleDataset(label: string, data: number[]): SiChartDataset[] {
  return [{ label, data }];
}

export function dailyLabels(points: readonly DailySalesPointDto[]): string[] {
  return points.map((p) => {
    const d = p.date?.slice(5) || p.date;
    return d;
  });
}

export function compositionItems(
  sales: number,
  returns: number,
  discounts: number,
  labels: { sales: string; returns: string; discounts: string },
): NamedAmountDto[] {
  return [
    { nameAr: labels.sales, nameEn: labels.sales, amount: Math.max(0, sales), count: 0 },
    { nameAr: labels.returns, nameEn: labels.returns, amount: Math.max(0, returns), count: 0 },
    { nameAr: labels.discounts, nameEn: labels.discounts, amount: Math.max(0, discounts), count: 0 },
  ].filter((x) => x.amount > 0);
}
