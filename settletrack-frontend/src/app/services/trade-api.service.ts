import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { CreateTradeRequest, Trade, TradeStatus, TradeSummary } from '../models/trade';

@Injectable({ providedIn: 'root' })
export class TradeApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/trades`;

  getTrades(status: TradeStatus | '' = '', isin = ''): Observable<Trade[]> {
    let params = new HttpParams();
    if (status) {
      params = params.set('status', status);
    }
    if (isin.trim()) {
      params = params.set('isin', isin.trim());
    }
    return this.http.get<Trade[]>(this.baseUrl, { params });
  }

  getSummary(): Observable<TradeSummary> {
    return this.http.get<TradeSummary>(`${this.baseUrl}/summary`);
  }

  createTrade(request: CreateTradeRequest): Observable<Trade> {
    return this.http.post<Trade>(this.baseUrl, request);
  }

  updateStatus(id: number, status: TradeStatus): Observable<Trade> {
    return this.http.patch<Trade>(`${this.baseUrl}/${id}/status`, { status });
  }
}