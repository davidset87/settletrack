import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal, viewChild } from '@angular/core';
import { SummaryCards } from './components/summary-cards';
import { TradeForm } from './components/trade-form';
import { StatusChange, TradeTable } from './components/trade-table';
import {
  CreateTradeRequest,
  TRADE_STATUSES,
  Trade,
  TradeStatus,
  TradeSummary,
} from './models/trade';
import { TradeApiService } from './services/trade-api.service';

@Component({
  selector: 'app-root',
  imports: [SummaryCards, TradeTable, TradeForm],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App implements OnInit {
  private readonly api = inject(TradeApiService);
  private readonly tradeForm = viewChild(TradeForm);

  protected readonly statuses = TRADE_STATUSES;
  protected readonly trades = signal<Trade[]>([]);
  protected readonly summary = signal<TradeSummary | null>(null);
  protected readonly loading = signal(false);
  protected readonly creating = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly busyId = signal<number | null>(null);
  protected readonly statusFilter = signal<TradeStatus | ''>('');
  protected readonly isinFilter = signal('');

  ngOnInit(): void {
    this.refresh();
  }

  protected refresh(): void {
    this.loadTrades();
    this.loadSummary();
  }

  protected setStatusFilter(value: string): void {
    this.statusFilter.set(value as TradeStatus | '');
    this.loadTrades();
  }

  protected setIsinFilter(value: string): void {
    this.isinFilter.set(value);
    this.loadTrades();
  }

  protected createTrade(request: CreateTradeRequest): void {
    this.creating.set(true);
    this.error.set(null);

    this.api.createTrade(request).subscribe({
      next: () => {
        this.creating.set(false);
        this.tradeForm()?.reset();
        this.refresh();
      },
      error: (err: unknown) => {
        this.creating.set(false);
        this.error.set(this.describe(err));
      },
    });
  }

  protected changeStatus(change: StatusChange): void {
    this.busyId.set(change.id);
    this.error.set(null);

    this.api.updateStatus(change.id, change.status).subscribe({
      next: () => {
        this.busyId.set(null);
        this.refresh();
      },
      error: (err: unknown) => {
        this.busyId.set(null);
        this.error.set(this.describe(err));
      },
    });
  }

  private loadTrades(): void {
    this.loading.set(true);
    this.error.set(null);

    this.api.getTrades(this.statusFilter(), this.isinFilter()).subscribe({
      next: (trades) => {
        this.trades.set(trades);
        this.loading.set(false);
      },
      error: (err: unknown) => {
        this.loading.set(false);
        this.error.set(this.describe(err));
      },
    });
  }

  private loadSummary(): void {
    this.api.getSummary().subscribe({
      next: (summary) => this.summary.set(summary),
      error: (err: unknown) => this.error.set(this.describe(err)),
    });
  }

  private describe(err: unknown): string {
    if (err instanceof HttpErrorResponse) {
      if (err.status === 0) {
        return 'Cannot reach the API. Is it running?';
      }
      const validationErrors = err.error?.errors as Record<string, string[]> | undefined;
      if (validationErrors) {
        return Object.values(validationErrors).flat().join(' ');
      }
      return err.error?.detail ?? err.error?.title ?? `Request failed (${err.status}).`;
    }
    return 'Unexpected error.';
  }
}