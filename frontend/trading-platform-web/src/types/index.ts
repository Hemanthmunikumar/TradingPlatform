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
