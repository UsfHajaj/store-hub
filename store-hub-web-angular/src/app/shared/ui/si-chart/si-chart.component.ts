import {
  AfterViewInit,
  Component,
  ElementRef,
  OnDestroy,
  effect,
  input,
  viewChild,
} from '@angular/core';
import {
  ArcElement,
  BarController,
  BarElement,
  CategoryScale,
  Chart,
  ChartConfiguration,
  ChartType,
  DoughnutController,
  Filler,
  Legend,
  LineController,
  LineElement,
  LinearScale,
  PieController,
  PointElement,
  PolarAreaController,
  RadialLinearScale,
  Tooltip,
} from 'chart.js';

Chart.register(
  CategoryScale,
  LinearScale,
  RadialLinearScale,
  BarElement,
  BarController,
  LineElement,
  LineController,
  PointElement,
  ArcElement,
  DoughnutController,
  PieController,
  PolarAreaController,
  Legend,
  Tooltip,
  Filler,
);

export interface SiChartDataset {
  label: string;
  data: number[];
  backgroundColor?: string | string[];
  borderColor?: string | string[];
  fill?: boolean;
  tension?: number;
  borderWidth?: number;
}

@Component({
  selector: 'app-si-chart',
  standalone: true,
  template: `<div class="si-chart"><canvas #canvas></canvas></div>`,
  styles: [
    `
      :host {
        display: block;
        width: 100%;
        height: 100%;
        min-height: 0;
      }
      .si-chart {
        position: relative;
        width: 100%;
        height: 100%;
        min-height: 0;
      }
      canvas {
        display: block;
        width: 100% !important;
        height: 100% !important;
        max-height: 100%;
      }
    `,
  ],
})
export class SiChartComponent implements AfterViewInit, OnDestroy {
  readonly type = input.required<ChartType>();
  readonly labels = input<string[]>([]);
  readonly datasets = input<SiChartDataset[]>([]);
  readonly horizontal = input(false);
  readonly currency = input(false);
  readonly legend = input(true);
  readonly empty = input(false);

  private readonly canvas = viewChild<ElementRef<HTMLCanvasElement>>('canvas');
  private chart: Chart | null = null;
  private ready = false;
  private lastSig = '';

  constructor() {
    effect(() => {
      this.type();
      this.labels();
      this.datasets();
      this.horizontal();
      this.currency();
      this.legend();
      this.empty();
      if (this.ready) {
        this.render();
      }
    });
  }

  ngAfterViewInit(): void {
    this.ready = true;
    this.render();
  }

  ngOnDestroy(): void {
    this.chart?.destroy();
    this.chart = null;
  }

  private render(): void {
    const el = this.canvas()?.nativeElement;
    if (!el) return;

    const labels = this.labels().map((l) => (l ?? '').toString().trim() || '—');
    const datasetsInput = this.datasets();
    const sig = JSON.stringify({
      type: this.type(),
      labels,
      datasets: datasetsInput,
      horizontal: this.horizontal(),
      currency: this.currency(),
      legend: this.legend(),
      empty: this.empty(),
    });

    if (sig === this.lastSig && this.chart) {
      return;
    }
    this.lastSig = sig;

    this.chart?.destroy();
    this.chart = null;

    if (this.empty() || !labels.length || !datasetsInput.some((d) => d.data.length)) {
      return;
    }

    const isRtl = typeof document !== 'undefined' && document.documentElement.dir === 'rtl';
    const muted = this.cssVar('--si-muted', '#6b7280');
    const grid = this.cssVar('--si-border', 'rgba(0,0,0,0.08)');
    const palette = this.palette();
    const pointColors = (count: number) =>
      Array.from({ length: count }, (_, i) => palette[i % palette.length]);

    const datasets = datasetsInput.map((d, i) => {
      // Bars/arcs: full brand mix. Lines: skip pale coral-first so fill isn't milky peach.
      const colorIndex = this.type() === 'line' ? (i + 2) % palette.length : i % palette.length;
      const base = palette[colorIndex];
      const perPoint = this.type() === 'bar' || this.isArc();
      const colors = perPoint ? pointColors(d.data.length) : base;
      const softFill =
        this.type() === 'line' ? this.withAlpha(String(base), 0.14) : colors;

      return {
        ...d,
        backgroundColor: d.backgroundColor ?? softFill,
        borderColor:
          d.borderColor ??
          (this.isArc() ? '#fff' : perPoint ? pointColors(d.data.length) : base),
        borderWidth: d.borderWidth ?? (this.isArc() ? 2 : this.type() === 'line' ? 2.5 : 0),
        fill: d.fill ?? this.type() === 'line',
        tension: d.tension ?? 0.35,
        hoverBackgroundColor: perPoint
          ? pointColors(d.data.length).map((c) => this.withAlpha(String(c), 0.85))
          : this.withAlpha(String(base), 0.85),
      };
    });

    const truncate = (value: string, max = 16) =>
      value.length > max ? `${value.slice(0, max - 1)}…` : value;

    const config: ChartConfiguration = {
      type: this.type(),
      data: {
        labels,
        datasets,
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        layout: { padding: { top: 4, right: 6, bottom: 2, left: 4 } },
        indexAxis: this.horizontal() ? 'y' : 'x',
        interaction: {
          mode: this.isArc() ? 'nearest' : 'index',
          intersect: false,
          axis: this.horizontal() ? 'y' : 'x',
        },
        events: ['mousemove', 'mouseout', 'click', 'touchstart', 'touchmove'],
        plugins: {
          legend: {
            display: this.legend() && (datasets.length > 1 || this.isArc()),
            position: 'bottom',
            rtl: isRtl,
            labels: {
              color: muted,
              boxWidth: 8,
              boxHeight: 8,
              padding: 8,
              font: { size: 10, weight: 600 },
            },
          },
          tooltip: {
            enabled: true,
            external: undefined,
            rtl: isRtl,
            backgroundColor: 'rgba(17, 24, 28, 0.94)',
            titleColor: '#fff',
            bodyColor: '#f3f4f6',
            titleFont: { size: 12, weight: 700 },
            bodyFont: { size: 12, weight: 500 },
            padding: 10,
            cornerRadius: 8,
            displayColors: true,
            caretPadding: 8,
            callbacks: {
              title: (items) => {
                const idx = items[0]?.dataIndex ?? 0;
                return labels[idx] ?? '';
              },
              label: (ctx) => {
                const parsed = ctx.parsed as number | { x?: number; y?: number; r?: number };
                const raw =
                  typeof parsed === 'number'
                    ? parsed
                    : Number(
                        this.horizontal()
                          ? (parsed?.x ?? ctx.raw ?? 0)
                          : (parsed?.y ?? parsed?.r ?? parsed?.x ?? ctx.raw ?? 0),
                      );
                const prefix = ctx.dataset.label ? `${ctx.dataset.label}: ` : '';
                return prefix + (this.currency() ? this.formatMoney(raw) : this.formatNum(raw));
              },
            },
          },
        },
        scales: this.isArc()
          ? undefined
          : {
              x: {
                reverse: isRtl && !this.horizontal(),
                beginAtZero: this.horizontal(),
                grid: { color: grid, drawTicks: false },
                ticks: {
                  color: muted,
                  font: { size: 9 },
                  maxRotation: 0,
                  autoSkipPadding: 6,
                  callback: (value, index) => {
                    if (this.horizontal()) {
                      const n = Number(value);
                      return this.currency() ? this.formatMoney(n) : this.formatNum(n);
                    }
                    const label = labels[index] ?? String(value);
                    return truncate(label, 12);
                  },
                },
                border: { color: grid },
              },
              y: {
                reverse: isRtl && this.horizontal(),
                beginAtZero: !this.horizontal(),
                grid: { color: grid, drawTicks: false },
                ticks: {
                  color: muted,
                  font: { size: 9 },
                  maxTicksLimit: this.horizontal() ? 10 : 5,
                  callback: (value, index) => {
                    if (this.horizontal()) {
                      const label = labels[index] ?? String(value);
                      return truncate(label, 18);
                    }
                    const n = Number(value);
                    return this.currency() ? this.formatMoney(n) : this.formatNum(n);
                  },
                },
                border: { color: grid },
              },
            },
        elements: {
          bar: { borderRadius: 4 },
          point: { radius: this.type() === 'line' ? 2.5 : 0, hoverRadius: 5, hitRadius: 8 },
        },
      },
    };

    this.chart = new Chart(el, config);
  }

  private isArc(): boolean {
    return this.type() === 'doughnut' || this.type() === 'pie' || this.type() === 'polarArea';
  }

  private palette(): string[] {
    const keys = [
      '--si-chart-1',
      '--si-chart-2',
      '--si-chart-3',
      '--si-chart-4',
      '--si-chart-5',
      '--si-chart-6',
      '--si-chart-7',
      '--si-chart-8',
      '--si-chart-9',
      '--si-chart-10',
    ];
    // Saturated brand-adjacent fallbacks (no cream/white washes)
    const fallbacks = [
      '#EC5B38', // coral
      '#5f7a68', // sage-green
      '#524646', // ink
      '#3d8f6e', // success
      '#c48a2a', // warning
      '#4d7373', // sage-teal
      '#a04a35', // deep coral
      '#6b6358', // sage-ink
      '#d4783a', // amber-coral
      '#2f6b52', // deep success
    ];
    return keys.map((k, i) => this.cssVar(k, fallbacks[i]));
  }

  private cssVar(name: string, fallback: string): string {
    if (typeof getComputedStyle === 'undefined') return fallback;
    const v = getComputedStyle(document.documentElement).getPropertyValue(name).trim();
    return v || fallback;
  }

  private withAlpha(color: string, a: number): string {
    if (color.startsWith('#') && color.length === 7) {
      const r = parseInt(color.slice(1, 3), 16);
      const g = parseInt(color.slice(3, 5), 16);
      const b = parseInt(color.slice(5, 7), 16);
      return `rgba(${r},${g},${b},${a})`;
    }
    return color;
  }

  private formatMoney(n: number): string {
    return `LE ${n.toLocaleString(undefined, { maximumFractionDigits: 0 })}`;
  }

  private formatNum(n: number): string {
    return n.toLocaleString(undefined, { maximumFractionDigits: 1 });
  }
}
