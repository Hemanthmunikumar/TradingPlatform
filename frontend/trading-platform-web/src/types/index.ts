export type OrderSide = 'Buy' | 'Sell';
export type OrderType = 'Market' | 'Limit' | 'StopLoss' | 'StopLossMarket';
export type ProductType = 'CNC' | 'MIS' | 'NRML';
export type OrderStatus = 'Created' | 'Submitted' | 'Accepted' | 'Open' | 'PartiallyFilled' | 'Filled' | 'Cancelled' | 'Rejected' | 'Error';
export type TradingMode = 'Paper' | 'Live';
export type BrokerCode = 'Mock' | 'Upstox' | 'Zerodha' | 'Dhan' | 'AngelOne';

export interface UserDto {
  id: string;
  email: string;
  fullName: string;
  timezone: string;
  roles: string[];
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAtUtc: string;
  user: UserDto;
}

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  errors?: string[];
  timestampUtc: string;
}

export interface BrokerProviderDto {
  id: string;
  name: string;
  code: BrokerCode;
  apiBaseUrl: string;
  isActive: boolean;
}

export interface BrokerConnectionDto {
  id: string;
  brokerProviderId: string;
  providerName: string;
  providerCode: BrokerCode;
  displayName: string;
  isAuthenticated: boolean;
  isActive: boolean;
  tradingEnabled: boolean;
  tradingMode: TradingMode;
  tokenExpiresAtUtc?: string;
  maskedApiKey: string;
  createdAtUtc: string;
}

export interface AccountConfigurationDto {
  maxCapitalAllocation: number;
  maxOrderValue: number;
  maxPositions: number;
  maxRiskPerTradePercent: number;
  dailyLossLimit: number;
  maxExposurePerStockPercent: number;
  maxTradesPerDay: number;
  stopLossRequired: boolean;
  approvalDurationSeconds: number;
  autoExecuteOnTimeout: boolean;
  killSwitchActive: boolean;
  tradingScheduleStart: string;
  tradingScheduleEnd: string;
  intradaySquareOffTime: string;
}

export interface TradingAccountDto {
  id: string;
  brokerConnectionId: string;
  accountId: string;
  accountName: string;
  accountType: string;
  isActive: boolean;
  tradingEnabled: boolean;
  tradingMode: TradingMode;
  availableFunds: number;
  allocatedCapital: number;
  configuration?: AccountConfigurationDto;
}

export interface PendingTradeApprovalDto {
  id: string;
  tradingAccountId: string;
  tradingSignalId?: string;
  symbol: string;
  companyName: string;
  side: OrderSide;
  quantity: number;
  indicativePrice: number;
  estimatedOrderValue: number;
  stopLoss?: number;
  target?: number;
  riskAmount?: number;
  reason: string;
  approvalDeadlineUtc: string;
  status: 'Pending' | 'Approved' | 'Rejected' | 'Expired';
  decidedAtUtc?: string;
  decisionReason?: string;
  remainingSeconds: number;
  isExpired: boolean;
}

export interface IndianBrokerageChargesDto {
  brokerage: number;
  stt: number;
  exchangeCharges: number;
  gst: number;
  sebiFees: number;
  stampDuty: number;
  dpCharges: number;
  totalCharges: number;
}

export interface PositionDto {
  id: string;
  tradingAccountId: string;
  instrumentId: string;
  symbol: string;
  companyName: string;
  side: string;
  productType: string;
  quantity: number;
  averageBuyPrice: number;
  currentPrice: number;
  stopLoss?: number;
  target?: number;
  unrealizedPnL: number;
  realizedGrossPnL: number;
  realizedNetPnL: number;
  totalCharges: number;
  isOpen: boolean;
  openedAtUtc: string;
  closedAtUtc?: string;
  exitReason?: string;
}

export interface OrderDto {
  id: string;
  tradingAccountId: string;
  instrumentId: string;
  symbol: string;
  companyName: string;
  side: string;
  orderType: string;
  productType: string;
  quantity: number;
  filledQuantity: number;
  price: number;
  triggerPrice?: number;
  averagePrice: number;
  status: OrderStatus;
  statusMessage?: string;
  brokerOrderId?: string;
  createdAtUtc: string;
  submittedAtUtc?: string;
}

export interface TradeHistoryDto {
  executionId: string;
  orderId: string;
  tradingAccountId: string;
  instrumentId: string;
  symbol: string;
  companyName: string;
  side: string;
  productType: string;
  quantity: number;
  price: number;
  totalValue: number;
  entryPrice?: number;
  realizedPnL?: number;
  exchange: string;
  executedAtUtc: string;
}

export interface SellExecutionToastDto {
  tradeExecutionId: string;
  orderId: string;
  positionId?: string;
  symbol: string;
  companyName: string;
  quantity: number;
  entryPrice: number;
  exitPrice: number;
  grossPnL: number;
  returnPercent: number;
  charges: IndianBrokerageChargesDto;
  netRealizedPnL: number;
  holdDuration: string;
  exitTrigger: string;
  executedAtUtc: string;
}

export interface NotificationDto {
  id: string;
  userId: string;
  tradingAccountId?: string;
  type: string;
  title: string;
  message: string;
  dataJson?: string;
  isRead: boolean;
  readAtUtc?: string;
  createdAtUtc: string;
}

export interface RiskAlertNotificationDto {
  tradingAccountId?: string;
  severity: string;
  ruleName: string;
  message: string;
  timestampUtc: string;
}

export interface SystemAlertNotificationDto {
  level: string;
  title: string;
  message: string;
  timestampUtc: string;
}

export interface DailyPnLDataPointDto {
  date: string;
  realizedPnL: number;
  cumulativePnL: number;
}

export interface SymbolPnLSummaryDto {
  symbol: string;
  tradesCount: number;
  totalQuantity: number;
  realizedPnL: number;
  totalCharges: number;
  netPnL: number;
}

export interface StrategyPerformanceDto {
  strategyName: string;
  totalTrades: number;
  winningTrades: number;
  losingTrades: number;
  winRatePercentage: number;
  realizedNetPnL: number;
  profitFactor: number;
}

export interface PortfolioAnalyticsDto {
  userId: string;
  tradingAccountId?: string;
  totalPortfolioValue: number;
  totalRealizedPnL: number;
  totalUnrealizedPnL: number;
  netTotalPnL: number;
  todayPnL: number;
  weekPnL: number;
  monthPnL: number;
  totalTrades: number;
  winningTrades: number;
  losingTrades: number;
  winRatePercentage: number;
  averageWinAmount: number;
  averageLossAmount: number;
  profitFactor: number;
  maxDrawdownPercentage: number;
  dailyPnLTrend: DailyPnLDataPointDto[];
  symbolBreakdown: SymbolPnLSummaryDto[];
  strategyBreakdown: StrategyPerformanceDto[];
}
