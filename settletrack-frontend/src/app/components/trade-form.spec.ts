import { TestBed } from '@angular/core/testing';
import { CreateTradeRequest } from '../models/trade';
import { TradeForm } from './trade-form';

describe('TradeForm', () => {
  function setup() {
    const fixture = TestBed.createComponent(TradeForm);
    fixture.detectChanges();

    const emitted: CreateTradeRequest[] = [];
    fixture.componentInstance.submitted.subscribe((value) => emitted.push(value));

    const root: HTMLElement = fixture.nativeElement;
    const type = (name: string, value: string) => {
      const input = root.querySelector<HTMLInputElement>(`[formControlName="${name}"]`)!;
      input.value = value;
      input.dispatchEvent(new Event('input'));
    };
    const submit = () => {
      root.querySelector('form')!.dispatchEvent(new Event('submit'));
      fixture.detectChanges();
    };

    return { fixture, root, emitted, type, submit };
  }

  it('does not emit and shows errors when the form is empty', () => {
    const { root, emitted, submit } = setup();

    submit();

    expect(emitted.length).toBe(0);
    expect(root.querySelectorAll('.err').length).toBeGreaterThan(0);
  });

  it('rejects an invalid ISIN', () => {
    const { root, emitted, type, submit } = setup();

    type('isin', 'ABC');
    type('counterparty', 'Test Capital');
    submit();

    expect(emitted.length).toBe(0);
    expect(root.textContent).toContain('12 characters');
  });

  it('emits a normalized request when the form is valid', () => {
    const { emitted, type, submit } = setup();

    type('isin', 'us88160r1014');
    type('counterparty', '  Test Capital  ');
    type('currency', 'eur');
    submit();

    expect(emitted.length).toBe(1);
    expect(emitted[0].isin).toBe('US88160R1014');
    expect(emitted[0].counterparty).toBe('Test Capital');
    expect(emitted[0].currency).toBe('EUR');
    expect(emitted[0].side).toBe('Buy');
    expect(emitted[0].quantity).toBe(100);
  });

  it('rejects a settlement date in the past', () => {
    const { root, emitted, type, submit } = setup();

    type('isin', 'US88160R1014');
    type('counterparty', 'Test Capital');
    type('settlementDate', '2000-01-01');
    submit();

    expect(emitted.length).toBe(0);
    expect(root.textContent).toContain('Cannot be in the past');
  });
});