import { Component, inject, input, output } from '@angular/core';
import {
  AbstractControl,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { CreateTradeRequest, TradeSide } from '../models/trade';

function todayIso(): string {
  const now = new Date();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  return `${now.getFullYear()}-${month}-${day}`;
}

function notInPast(control: AbstractControl<string>): ValidationErrors | null {
  const value = control.value;
  // ISO dates (yyyy-MM-dd) can be compared as plain strings.
  return value && value < todayIso() ? { pastDate: true } : null;
}

@Component({
  selector: 'app-trade-form',
  imports: [ReactiveFormsModule],
  template: `
    <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
      <div class="grid">
        <label>
          ISIN
          <input formControlName="isin" placeholder="US0378331005" maxlength="12" />
          @if (invalid('isin')) {
            <small class="err">12 characters: 2 letters, 9 letters or digits, 1 digit.</small>
          }
        </label>

        <label>
          Counterparty
          <input formControlName="counterparty" placeholder="Northbridge Capital" />
          @if (invalid('counterparty')) {
            <small class="err">Required (2 to 100 characters).</small>
          }
        </label>

        <label>
          Side
          <select formControlName="side">
            <option value="Buy">Buy</option>
            <option value="Sell">Sell</option>
          </select>
        </label>

        <label>
          Quantity
          <input type="number" formControlName="quantity" min="1" />
          @if (invalid('quantity')) {
            <small class="err">Must be at least 1.</small>
          }
        </label>

        <label>
          Price
          <input type="number" formControlName="price" step="0.01" min="0" />
          @if (invalid('price')) {
            <small class="err">Must be greater than 0.</small>
          }
        </label>

        <label>
          Currency
          <input formControlName="currency" placeholder="EUR" maxlength="3" />
          @if (invalid('currency')) {
            <small class="err">3-letter code, for example EUR.</small>
          }
        </label>

        <label>
          Settlement date
          <input type="date" formControlName="settlementDate" [min]="today" />
          @if (invalid('settlementDate')) {
            <small class="err">
              {{ form.controls.settlementDate.hasError('pastDate') ? 'Cannot be in the past.' : 'Required.' }}
            </small>
          }
        </label>
      </div>

      <button type="submit" [disabled]="busy()">Add trade</button>
    </form>
  `,
  styles: `
    .grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(190px, 1fr));
      gap: 14px;
      margin: 12px 0 16px;
    }
    label {
      display: flex;
      flex-direction: column;
      gap: 4px;
      font-size: 0.85rem;
      color: var(--muted);
    }
    input,
    select {
      font: inherit;
      padding: 8px 10px;
      border: 1px solid var(--border);
      border-radius: 8px;
      background: var(--surface);
      color: var(--text);
    }
    input.ng-invalid.ng-touched {
      border-color: var(--failed);
    }
    .err {
      color: var(--failed);
    }
    button {
      font: inherit;
      cursor: pointer;
      border: 1px solid var(--primary);
      background: var(--primary);
      color: #fff;
      border-radius: 8px;
      padding: 8px 16px;
    }
    button:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }
  `,
})
export class TradeForm {
  private readonly fb = inject(NonNullableFormBuilder);

  readonly busy = input(false);
  readonly submitted = output<CreateTradeRequest>();

  protected readonly today = todayIso();

  protected readonly form = this.fb.group({
    isin: ['', [Validators.required, Validators.pattern(/^[A-Za-z]{2}[A-Za-z0-9]{9}[0-9]$/)]],
    counterparty: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
    side: ['Buy' as TradeSide, Validators.required],
    quantity: [100, [Validators.required, Validators.min(1)]],
    price: [10, [Validators.required, Validators.min(0.0001)]],
    currency: ['EUR', [Validators.required, Validators.pattern(/^[A-Za-z]{3}$/)]],
    settlementDate: [todayIso(), [Validators.required, notInPast]],
  });

  protected invalid(name: string): boolean {
    const control = this.form.get(name);
    return !!control && control.invalid && (control.touched || control.dirty);
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.submitted.emit({
      isin: value.isin.trim().toUpperCase(),
      counterparty: value.counterparty.trim(),
      side: value.side,
      quantity: value.quantity,
      price: value.price,
      currency: value.currency.trim().toUpperCase(),
      settlementDate: value.settlementDate,
    });
  }

  reset(): void {
    this.form.reset({
      isin: '',
      counterparty: '',
      side: 'Buy',
      quantity: 100,
      price: 10,
      currency: 'EUR',
      settlementDate: todayIso(),
    });
  }
}