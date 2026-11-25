import { Component, input } from '@angular/core';
import { TradeSummary } from '../models/trade';

@Component({
  selector: 'app-summary-cards',
  template: `
    @if (summary(); as s) {
      <section class="cards" aria-label="Settlement summary">
        <article class="card"><span class="label">Total trades</span><strong>{{ s.total }}</strong></article>
        <article class="card"><span class="label">Pending</span><strong>{{ s.pending }}</strong></article>
        <article class="card"><span class="label">Matched</span><strong>{{ s.matched }}</strong></article>
        <article class="card"><span class="label">Settled</span><strong>{{ s.settled }}</strong></article>
        <article class="card"><span class="label">Failed</span><strong>{{ s.failed }}</strong></article>
        <article class="card">
          <span class="label">Settlement rate</span><strong>{{ s.settlementRatePercent }}%</strong>
        </article>
        <article class="card"><span class="label">Due today</span><strong>{{ s.dueToday }}</strong></article>
        <article class="card" [class.alert]="s.overdue > 0">
          <span class="label">Overdue</span><strong>{{ s.overdue }}</strong>
        </article>
      </section>
    }
  `,
  styles: `
    .cards {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
      gap: 12px;
    }
    .card {
      background: var(--surface);
      border: 1px solid var(--border);
      border-radius: 12px;
      padding: 14px 16px;
      display: flex;
      flex-direction: column;
      gap: 6px;
    }
    .label {
      font-size: 0.8rem;
      color: var(--muted);
    }
    strong {
      font-size: 1.6rem;
    }
    .alert {
      border-color: var(--failed);
    }
    .alert strong {
      color: var(--failed);
    }
  `,
})
export class SummaryCards {
  readonly summary = input<TradeSummary | null>(null);
}