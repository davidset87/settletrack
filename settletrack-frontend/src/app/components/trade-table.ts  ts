import { DecimalPipe } from '@angular/common';
import { Component, input, output } from '@angular/core';
import { Trade, TradeStatus } from '../models/trade';

export interface StatusChange {
  id: number;
  status: TradeStatus;
}

@Component({
  selector: 'app-trade-table',
  imports: [DecimalPipe],
  template: `
    @if (trades().length === 0) {
      <p class="empty">No trades match your filters.</p>
    } @else {
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>ISIN</th>
              <th>Counterparty</th>
              <th>Side</th>
              <th class="num">Quantity</th>
              <th class="num">Price</th>
              <th>Settlement</th>
              <th>Status</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            @for (trade of trades(); track trade.id) {
              <tr>
                <td class="mono">{{ trade.isin }}</td>
                <td>{{ trade.counterparty }}</td>
                <td>{{ trade.side }}</td>
                <td class="num">{{ trade.quantity | number }}</td>
                <td class="num">{{ trade.price | number: '1.2-4' }} {{ trade.currency }}</td>
                <td>{{ trade.settlementDate }}</td>
                <td>
                  <span class="badge" [attr.data-status]="trade.status">{{ trade.status }}</span>
                </td>
                <td class="actions">
                  @for (next of trade.allowedTransitions; track next) {
                    <button
                      type="button"
                      [disabled]="busyId() === trade.id"
                      (click)="statusChange.emit({ id: trade.id, status: next })"
                    >
                      Mark {{ next }}
                    </button>
                  } @empty {
                    <span class="muted">Final</span>
                  }
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    }
  `,
  styles: `
    .table-wrap {
      overflow-x: auto;
      background: var(--surface);
      border: 1px solid var(--border);
      border-radius: 12px;
    }
    table {
      width: 100%;
      border-collapse: collapse;
      font-size: 0.92rem;
    }
    th,
    td {
      padding: 10px 14px;
      text-align: left;
      border-bottom: 1px solid var(--border);
      white-space: nowrap;
    }
    th {
      font-size: 0.78rem;
      text-transform: uppercase;
      letter-spacing: 0.04em;
      color: var(--muted);
    }
    tbody tr:last-child td {
      border-bottom: none;
    }
    .num {
      text-align: right;
    }
    .mono {
      font-family: ui-monospace, 'Cascadia Mono', Consolas, monospace;
    }
    .badge {
      display: inline-block;
      padding: 2px 10px;
      border-radius: 999px;
      font-size: 0.78rem;
      font-weight: 600;
      color: #fff;
    }
    .badge[data-status='Pending'] { background: var(--pending); }
    .badge[data-status='Matched'] { background: var(--matched); }
    .badge[data-status='Settled'] { background: var(--settled); }
    .badge[data-status='Failed'] { background: var(--failed); }
    .actions {
      display: flex;
      gap: 6px;
    }
    button {
      font: inherit;
      font-size: 0.82rem;
      cursor: pointer;
      background: transparent;
      color: var(--primary);
      border: 1px solid var(--primary);
      border-radius: 8px;
      padding: 4px 10px;
    }
    button:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }
    .muted {
      color: var(--muted);
    }
    .empty {
      padding: 24px;
      text-align: center;
      color: var(--muted);
    }
  `,
})
export class TradeTable {
  readonly trades = input.required<Trade[]>();
  readonly busyId = input<number | null>(null);
  readonly statusChange = output<StatusChange>();
}