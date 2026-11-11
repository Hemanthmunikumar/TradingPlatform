import React, { useState, useEffect, useRef } from 'react';
import { Bell, Check, CheckCheck, AlertCircle, Info, TrendingUp, ShieldAlert } from 'lucide-react';
import { notificationsApi } from '../api/client';
import { signalRClient } from '../services/signalr';
import type { NotificationDto, RiskAlertNotificationDto, SystemAlertNotificationDto } from '../types';

export const NotificationCenter: React.FC = () => {
  const [isOpen, setIsOpen] = useState(false);
  const [unreadCount, setUnreadCount] = useState(0);
  const [notifications, setNotifications] = useState<NotificationDto[]>([]);
  const [loading, setLoading] = useState(false);
  const panelRef = useRef<HTMLDivElement>(null);

  const fetchUnread = async () => {
    try {
      const count = await notificationsApi.getUnreadCount();
      setUnreadCount(count);
    } catch {
      // Ignored if offline
    }
  };

  const loadNotifications = async () => {
    setLoading(true);
    try {
      const data = await notificationsApi.getAll({ pageSize: 30 });
      setNotifications(data);
    } catch (err) {
      console.error('Failed to load notifications:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    signalRClient.connect();
    fetchUnread();

    const unsubscribeNotif = signalRClient.onNotification((notif) => {
      setNotifications((prev) => [notif, ...prev]);
      setUnreadCount((c) => c + 1);
    });

    const unsubscribeRisk = signalRClient.onRiskAlert((alert: RiskAlertNotificationDto) => {
      const notif: NotificationDto = {
        id: crypto.randomUUID(),
        userId: '',
        type: 'RISK_ALERT',
        title: `Risk Alert: ${alert.ruleName}`,
        message: alert.message,
        isRead: false,
        createdAtUtc: alert.timestampUtc,
      };
      setNotifications((prev) => [notif, ...prev]);
      setUnreadCount((c) => c + 1);
    });

    const unsubscribeSys = signalRClient.onSystemAlert((alert: SystemAlertNotificationDto) => {
      const notif: NotificationDto = {
        id: crypto.randomUUID(),
        userId: '',
        type: 'SYSTEM_ALERT',
        title: alert.title,
        message: alert.message,
        isRead: false,
        createdAtUtc: alert.timestampUtc,
      };
      setNotifications((prev) => [notif, ...prev]);
      setUnreadCount((c) => c + 1);
    });

    // Close panel on outside click
    const handleClickOutside = (event: MouseEvent) => {
      if (panelRef.current && !panelRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);

    return () => {
      unsubscribeNotif();
      unsubscribeRisk();
      unsubscribeSys();
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, []);

  const handleToggle = () => {
    if (!isOpen) {
      loadNotifications();
    }
    setIsOpen(!isOpen);
  };

  const handleMarkAsRead = async (id: string) => {
    try {
      await notificationsApi.markAsRead(id);
      setNotifications((prev) =>
        prev.map((n) => (n.id === id ? { ...n, isRead: true } : n))
      );
      setUnreadCount((c) => Math.max(0, c - 1));
    } catch (err) {
      console.error(err);
    }
  };

  const handleMarkAllAsRead = async () => {
    try {
      await notificationsApi.markAllAsRead();
      setNotifications((prev) => prev.map((n) => ({ ...n, isRead: true })));
      setUnreadCount(0);
    } catch (err) {
      console.error(err);
    }
  };

  const getIcon = (type: string) => {
    switch (type) {
      case 'RISK_ALERT':
        return <ShieldAlert size={14} className="text-rose-400" />;
      case 'SELL_FILLED':
      case 'TARGET_HIT':
        return <TrendingUp size={14} className="text-emerald-400" />;
      case 'SL_HIT':
        return <AlertCircle size={14} className="text-amber-400" />;
      default:
        return <Info size={14} className="text-sky-400" />;
    }
  };

  return (
    <div className="relative" ref={panelRef}>
      <button
        onClick={handleToggle}
        className="relative p-2 rounded-lg bg-slate-800/80 text-slate-400 hover:text-white transition"
        title="Notifications"
      >
        <Bell size={18} />
        {unreadCount > 0 && (
          <span className="absolute -top-1 -right-1 flex h-4 w-4 items-center justify-center rounded-full bg-rose-600 text-[9px] font-bold text-white shadow">
            {unreadCount > 9 ? '9+' : unreadCount}
          </span>
        )}
      </button>

      {isOpen && (
        <div className="absolute right-0 mt-2 w-80 sm:w-96 rounded-xl bg-slate-900 border border-slate-800 shadow-2xl z-50 overflow-hidden animate-in fade-in slide-in-from-top-2 duration-150">
          <div className="flex items-center justify-between border-b border-slate-800 px-4 py-3 bg-slate-950/60">
            <div className="flex items-center gap-2">
              <span className="text-xs font-bold uppercase tracking-wider text-white">Notifications</span>
              {unreadCount > 0 && (
                <span className="px-1.5 py-0.5 rounded text-[10px] font-semibold bg-sky-500/20 text-sky-400 border border-sky-500/30">
                  {unreadCount} unread
                </span>
              )}
            </div>
            {unreadCount > 0 && (
              <button
                onClick={handleMarkAllAsRead}
                className="flex items-center gap-1 text-[11px] text-slate-400 hover:text-sky-400 transition"
              >
                <CheckCheck size={13} /> Mark all read
              </button>
            )}
          </div>

          <div className="max-h-80 overflow-y-auto divide-y divide-slate-800/60">
            {loading ? (
              <div className="py-8 text-center text-xs text-slate-500">Loading notifications...</div>
            ) : notifications.length === 0 ? (
              <div className="py-8 text-center text-xs text-slate-500">No notifications yet.</div>
            ) : (
              notifications.map((notif) => (
                <div
                  key={notif.id}
                  className={`p-3 text-xs transition flex gap-3 items-start ${
                    notif.isRead ? 'bg-transparent text-slate-400' : 'bg-slate-800/30 text-slate-200'
                  }`}
                >
                  <div className="mt-0.5">{getIcon(notif.type)}</div>
                  <div className="flex-1 min-w-0">
                    <p className={`font-semibold truncate ${notif.isRead ? 'text-slate-300' : 'text-white'}`}>
                      {notif.title}
                    </p>
                    <p className="text-[11px] text-slate-400 mt-0.5 line-clamp-2">{notif.message}</p>
                    <p className="text-[10px] text-slate-500 mt-1 font-mono">
                      {new Date(notif.createdAtUtc).toLocaleTimeString()}
                    </p>
                  </div>
                  {!notif.isRead && (
                    <button
                      onClick={() => handleMarkAsRead(notif.id)}
                      className="text-slate-500 hover:text-emerald-400 p-1"
                      title="Mark as read"
                    >
                      <Check size={14} />
                    </button>
                  )}
                </div>
              ))
            )}
          </div>
        </div>
      )}
    </div>
  );
};
