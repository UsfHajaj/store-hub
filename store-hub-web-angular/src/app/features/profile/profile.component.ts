import { CurrencyPipe } from '@angular/common';
import { Component, OnDestroy, OnInit, computed, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { LocaleService } from '../../core/i18n/locale.service';
import { ActiveStoreService } from '../../core/services/active-store.service';
import { ProfileApiService } from '../../core/services/profile-api.service';
import { SiTranslatePipe } from '../../shared/pipes/si-translate.pipe';
import { MyPerformanceDto } from '../../shared/models/profile.models';
import { UserDto } from '../../shared/models/user.models';

interface CalendarCell {
  day: number;
  inMonth: boolean;
  isToday: boolean;
}

@Component({
  selector: 'app-profile',
  imports: [SiTranslatePipe, RouterLink, CurrencyPipe],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss',
})
export class ProfileComponent implements OnInit, OnDestroy {
  readonly auth = inject(AuthService);
  readonly locale = inject(LocaleService);
  readonly activeStore = inject(ActiveStoreService);
  private readonly profileApi = inject(ProfileApiService);

  readonly me = signal<UserDto | null>(null);
  readonly performance = signal<MyPerformanceDto | null>(null);
  readonly loading = signal(true);
  readonly perfLoading = signal(false);

  readonly now = signal(new Date());
  private readonly timer = setInterval(() => this.now.set(new Date()), 1000);

  private readonly today = new Date();
  readonly viewYear = signal(this.today.getFullYear());
  readonly viewMonth = signal(this.today.getMonth());

  private readonly localeTag = computed(() => (this.locale.lang() === 'ar' ? 'ar-EG' : 'en-US'));

  readonly displayName = computed(() => {
    const lang = this.locale.lang();
    const m = this.me();
    if (m) {
      const named = lang === 'ar' ? m.nameAr || m.nameEn : m.nameEn || m.nameAr;
      if (named?.trim()) return named.trim();
      return m.userName || m.email;
    }
    const s = this.auth.sessionSnapshot();
    return s?.userName || s?.email || this.locale.t('common.guest');
  });

  readonly email = computed(() => this.me()?.email || this.auth.sessionSnapshot()?.email || '—');
  readonly userName = computed(() => this.me()?.userName || this.auth.sessionSnapshot()?.userName || '—');

  readonly roleLabels = computed(() => {
    const m = this.me();
    if (!m) return [] as string[];
    const lang = this.locale.lang();
    if (lang === 'en' && m.roleNamesEn?.length) {
      return [...m.roleNamesEn];
    }
    return [...(m.roleNames ?? [])];
  });

  readonly storeLabel = computed(() => {
    const p = this.performance();
    if (p) {
      return this.locale.lang() === 'ar' ? p.storeNameAr || p.storeNameEn : p.storeNameEn || p.storeNameAr;
    }
    const s = this.activeStore.activeStore();
    if (!s) return '';
    return this.locale.lang() === 'ar' ? s.nameAr : s.nameEn;
  });

  readonly initials = computed(() => {
    const name = this.displayName().trim();
    const parts = name.split(/\s+/).filter(Boolean);
    if (parts.length >= 2) return (parts[0][0] + parts[1][0]).toUpperCase();
    return name.slice(0, 2).toUpperCase() || 'U';
  });

  readonly lastLogin = computed(() => {
    const raw = this.me()?.lastLoginUtc;
    if (!raw) return '—';
    const d = new Date(raw);
    if (Number.isNaN(d.getTime())) return '—';
    return new Intl.DateTimeFormat(this.localeTag(), { dateStyle: 'medium', timeStyle: 'short' }).format(d);
  });

  readonly statusLabel = computed(() => {
    const m = this.me();
    if (!m) return '—';
    return m.isActive ? this.locale.t('profile.statusActive') : this.locale.t('profile.statusInactive');
  });

  readonly clock = computed(() =>
    new Intl.DateTimeFormat(this.localeTag(), {
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
    }).format(this.now()),
  );

  readonly fullDate = computed(() =>
    new Intl.DateTimeFormat(this.localeTag(), {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    }).format(this.now()),
  );

  readonly monthLabel = computed(() =>
    new Intl.DateTimeFormat(this.localeTag(), { month: 'long', year: 'numeric' }).format(
      new Date(this.viewYear(), this.viewMonth(), 1),
    ),
  );

  readonly weekdays = computed(() => {
    const fmt = new Intl.DateTimeFormat(this.localeTag(), { weekday: 'short' });
    return Array.from({ length: 7 }, (_, i) => fmt.format(new Date(2023, 0, 1 + i)));
  });

  readonly calendar = computed<CalendarCell[]>(() => {
    const year = this.viewYear();
    const month = this.viewMonth();
    const firstDay = new Date(year, month, 1);
    const startOffset = firstDay.getDay();
    const daysInMonth = new Date(year, month + 1, 0).getDate();
    const daysInPrev = new Date(year, month, 0).getDate();
    const t = this.today;
    const cells: CalendarCell[] = [];
    for (let i = 0; i < 42; i++) {
      const dayNumber = i - startOffset + 1;
      if (dayNumber < 1) {
        cells.push({ day: daysInPrev + dayNumber, inMonth: false, isToday: false });
      } else if (dayNumber > daysInMonth) {
        cells.push({ day: dayNumber - daysInMonth, inMonth: false, isToday: false });
      } else {
        const isToday =
          dayNumber === t.getDate() && month === t.getMonth() && year === t.getFullYear();
        cells.push({ day: dayNumber, inMonth: true, isToday });
      }
    }
    return cells;
  });

  constructor() {
    effect(() => {
      const storeId = this.activeStore.activeStoreId();
      if (storeId) {
        this.loadPerformance(storeId);
      } else {
        this.performance.set(null);
      }
    });
  }

  ngOnInit(): void {
    this.profileApi.getMe().subscribe({
      next: (u) => {
        this.me.set(u);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  private loadPerformance(storeId: string): void {
    this.perfLoading.set(true);
    this.profileApi.getMyPerformance(storeId).subscribe({
      next: (p) => {
        this.performance.set(p);
        this.perfLoading.set(false);
      },
      error: () => {
        this.performance.set(null);
        this.perfLoading.set(false);
      },
    });
  }

  prevMonth(): void {
    const m = this.viewMonth();
    if (m === 0) {
      this.viewMonth.set(11);
      this.viewYear.update((y) => y - 1);
    } else {
      this.viewMonth.set(m - 1);
    }
  }

  nextMonth(): void {
    const m = this.viewMonth();
    if (m === 11) {
      this.viewMonth.set(0);
      this.viewYear.update((y) => y + 1);
    } else {
      this.viewMonth.set(m + 1);
    }
  }

  goToday(): void {
    this.viewYear.set(this.today.getFullYear());
    this.viewMonth.set(this.today.getMonth());
  }

  ngOnDestroy(): void {
    clearInterval(this.timer);
  }
}
