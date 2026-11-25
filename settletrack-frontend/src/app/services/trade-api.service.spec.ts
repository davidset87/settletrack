import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../environments/environment';
import { CreateTradeRequest } from '../models/trade';
import { TradeApiService } from './trade-api.service';

const baseUrl = `${environment.apiBaseUrl}/trades`;

describe('TradeApiService', () => {
  let service: TradeApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(TradeApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('gets all trades without query parameters', () => {
    service.getTrades().subscribe();

    const req = http.expectOne((r) => r.url === baseUrl);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.keys().length).toBe(0);
    req.flush([]);
  });

  it('sends the status and a trimmed ISIN as filters', () => {
    service.getTrades('Pending', '  US03 ').subscribe();

    const req = http.expectOne((r) => r.url === baseUrl);
    expect(req.request.params.get('status')).toBe('Pending');
    expect(req.request.params.get('isin')).toBe('US03');
    req.flush([]);
  });

  it('gets the summary', () => {
    service.getSummary().subscribe();

    const req = http.expectOne(`${baseUrl}/summary`);
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  it('creates a trade with POST', () => {
    const body: CreateTradeRequest = {
      isin: 'US88160R1014',
      counterparty: 'Test Capital',
      side: 'Buy',
      quantity: 100,
      price: 10,
      currency: 'EUR',
      settlementDate: '2030-01-01',
    };

    service.createTrade(body).subscribe();

    const req = http.expectOne(baseUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(body);
    req.flush({});
  });

  it('changes the status with PATCH', () => {
    service.updateStatus(7, 'Matched').subscribe();

    const req = http.expectOne(`${baseUrl}/7/status`);
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ status: 'Matched' });
    req.flush({});
  });
});