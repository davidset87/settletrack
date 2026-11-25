export type TradeStatus = 'Pending' | 'Matched' | 'Settled' | 'Failed';
export type TradeSide = 'Buy' | 'Sell';

export const TRADE_STATUSES: TradeStatus[] = ['Pending', 'Matched', 'Settled', 'Failed'];

export interface Trade {
  id: number;
  isin: string;
  counterparty: string;
  side: TradeSide;
  quantity: number;
  price: number;
  currency: string;
  settlementDate: string;
  status: TradeStatus;
  allowedTransitions: TradeStatus[];
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface TradeSummary {
  total: number;
  pending: number;
  matched: number;
  settled: number;
  failed: number;
  settlementRatePercent: number;
  dueToday: number;
  overdue: number;
}