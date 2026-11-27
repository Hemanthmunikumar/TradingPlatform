import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import type {
  PendingTradeApprovalDto,
  OrderDto,
  PositionDto,
  SellExecutionToastDto,
  NotificationDto,
  RiskAlertNotificationDto,
  SystemAlertNotificationDto,
} from '../types';

type NotificationListener = (notification: NotificationDto) => void;
type BuyApprovalListener = (approval: PendingTradeApprovalDto) => void;
type ApprovalDecisionListener = (decision: { approvalId: string; status: string; message: string }) => void;
type OrderListener = (order: OrderDto) => void;
type PositionListener = (position: PositionDto) => void;
type SellToastListener = (toast: SellExecutionToastDto) => void;
type RiskAlertListener = (alert: RiskAlertNotificationDto) => void;
type SystemAlertListener = (alert: SystemAlertNotificationDto) => void;

class SignalRClient {
  private connection: HubConnection | null = null;
  private isConnecting = false;
  private notificationListeners = new Set<NotificationListener>();
  private buyApprovalListeners = new Set<BuyApprovalListener>();
  private approvalDecisionListeners = new Set<ApprovalDecisionListener>();
  private orderSubmittedListeners = new Set<OrderListener>();
  private orderFilledListeners = new Set<OrderListener>();
  private positionClosedListeners = new Set<PositionListener>();
  private sellToastListeners = new Set<SellToastListener>();
  private stopLossToastListeners = new Set<SellToastListener>();
  private targetToastListeners = new Set<SellToastListener>();
  private riskAlertListeners = new Set<RiskAlertListener>();
  private systemAlertListeners = new Set<SystemAlertListener>();

  public async connect(): Promise<void> {
    if (this.connection && this.connection.state === HubConnectionState.Connected) {
      return;
    }
    if (this.isConnecting) return;

    this.isConnecting = true;
    const token = localStorage.getItem('access_token');
    const hubUrl = import.meta.env.VITE_HUB_URL || (import.meta.env.PROD ? '/hubs/trading' : 'http://localhost:5000/hubs/trading');

    this.connection = new HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => token || '',
      })
      .withAutomaticReconnect([0, 1000, 3000, 5000, 10000, 30000])
      .configureLogging(LogLevel.Warning)
      .build();

    this.connection.on('ReceiveNotification', (notif: NotificationDto) => {
      this.notificationListeners.forEach((l) => l(notif));
    });

    this.connection.on('ReceiveBuyApproval', (app: PendingTradeApprovalDto) => {
      this.buyApprovalListeners.forEach((l) => l(app));
    });

    this.connection.on('ReceiveApprovalDecision', (dec: { approvalId: string; status: string; message: string }) => {
      this.approvalDecisionListeners.forEach((l) => l(dec));
    });

    this.connection.on('ReceiveOrderSubmitted', (ord: OrderDto) => {
      this.orderSubmittedListeners.forEach((l) => l(ord));
    });

    this.connection.on('ReceiveOrderFilled', (ord: OrderDto) => {
      this.orderFilledListeners.forEach((l) => l(ord));
    });

    this.connection.on('ReceivePositionClosed', (pos: PositionDto) => {
      this.positionClosedListeners.forEach((l) => l(pos));
    });

    this.connection.on('ReceiveSellExecutionToast', (toast: SellExecutionToastDto) => {
      this.sellToastListeners.forEach((l) => l(toast));
    });

    this.connection.on('ReceiveStopLossHitToast', (toast: SellExecutionToastDto) => {
      this.stopLossToastListeners.forEach((l) => l(toast));
    });

    this.connection.on('ReceiveTargetHitToast', (toast: SellExecutionToastDto) => {
      this.targetToastListeners.forEach((l) => l(toast));
    });

    this.connection.on('ReceiveRiskAlert', (alert: RiskAlertNotificationDto) => {
      this.riskAlertListeners.forEach((l) => l(alert));
    });

    this.connection.on('ReceiveSystemAlert', (alert: SystemAlertNotificationDto) => {
      this.systemAlertListeners.forEach((l) => l(alert));
    });

    try {
      await this.connection.start();
      console.log('SignalR connected to /hubs/trading');
    } catch (err) {
      console.error('SignalR connection start failed:', err);
    } finally {
      this.isConnecting = false;
    }
  }

  public async disconnect(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
    }
  }

  public onNotification(listener: NotificationListener) {
    this.notificationListeners.add(listener);
    return () => this.notificationListeners.delete(listener);
  }

  public onBuyApproval(listener: BuyApprovalListener) {
    this.buyApprovalListeners.add(listener);
    return () => this.buyApprovalListeners.delete(listener);
  }

  public onApprovalDecision(listener: ApprovalDecisionListener) {
    this.approvalDecisionListeners.add(listener);
    return () => this.approvalDecisionListeners.delete(listener);
  }

  public onOrderSubmitted(listener: OrderListener) {
    this.orderSubmittedListeners.add(listener);
    return () => this.orderSubmittedListeners.delete(listener);
  }

  public onOrderFilled(listener: OrderListener) {
    this.orderFilledListeners.add(listener);
    return () => this.orderFilledListeners.delete(listener);
  }

  public onPositionClosed(listener: PositionListener) {
    this.positionClosedListeners.add(listener);
    return () => this.positionClosedListeners.delete(listener);
  }

  public onSellExecutionToast(listener: SellToastListener) {
    this.sellToastListeners.add(listener);
    return () => this.sellToastListeners.delete(listener);
  }

  public onStopLossHitToast(listener: SellToastListener) {
    this.stopLossToastListeners.add(listener);
    return () => this.stopLossToastListeners.delete(listener);
  }

  public onTargetHitToast(listener: SellToastListener) {
    this.targetToastListeners.add(listener);
    return () => this.targetToastListeners.delete(listener);
  }

  public onRiskAlert(listener: RiskAlertListener) {
    this.riskAlertListeners.add(listener);
    return () => this.riskAlertListeners.delete(listener);
  }

  public onSystemAlert(listener: SystemAlertListener) {
    this.systemAlertListeners.add(listener);
    return () => this.systemAlertListeners.delete(listener);
  }

  public async joinAccount(accountId: string) {
    if (this.connection && this.connection.state === HubConnectionState.Connected) {
      await this.connection.invoke('JoinAccountGroup', accountId);
    }
  }

  public async leaveAccount(accountId: string) {
    if (this.connection && this.connection.state === HubConnectionState.Connected) {
      await this.connection.invoke('LeaveAccountGroup', accountId);
    }
  }
}

export const signalRClient = new SignalRClient();
