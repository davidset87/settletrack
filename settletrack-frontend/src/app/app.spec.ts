import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../environments/environment';
import { App } from './app';
import { TradeSummary } from './models/trade';

const tradesUrl = `${environment.apiBaseUrl}/trades`;
const summaryUrl = `${environment.apiBaseUrl}/trades/summary`;

const summary: TradeSummary = {
  total: 0,
  pending: 0,
  matched: 0,
  settled: 0,
  failed: 0,
  settlementRatePercent: 0,
  dueToday: 0,
  overdue: 0,
};

describe('App', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads trades and summary when it starts', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    http.expectOne((r) => r.method === 'GET' && r.url === tradesUrl).flush([]);
    http.expectOne((r) => r.method === 'GET' && r.url === summaryUrl).flush(summary);
    await fixture.whenStable();

    const element: HTMLElement = fixture.nativeElement;
    expect(element.querySelector('h1')?.textContent).toContain('SettleTrack');
    expect(element.querySelector('[role="alert"]')).toBeNull();
  });

  it('shows a clear message when the API is unreachable', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    http
      .expectOne((r) => r.method === 'GET' && r.url === tradesUrl)
      .error(new ProgressEvent('error'));
    http.expectOne((r) => r.method === 'GET' && r.url === summaryUrl).flush(summary);
    await fixture.whenStable();
    fixture.detectChanges();

    const alert: HTMLElement | null = fixture.nativeElement.querySelector('[role="alert"]');
    expect(alert?.textContent).toContain('Cannot reach the API');
  });
});