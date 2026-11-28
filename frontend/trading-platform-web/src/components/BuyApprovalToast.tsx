import React, { useEffect, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { BellRing, Clock, ShieldAlert, XCircle, ExternalLink } from 'lucide-react';
import { approvalsApi } from '../api/client';
import { signalRClient } from '../services/signalr';
import type { PendingTradeApprovalDto } from '../types';

function playAlertChime() {
  try {
    const AudioCtx = window.AudioContext || (window as any).webkitAudioContext;
    if (!AudioCtx) return;
    const ctx = new AudioCtx();
    const now = ctx.currentTime;
    [880, 1174.66].forEach((freq, idx) => {
      const osc = ctx.createOscillator();
      const gain = ctx.createGain();
      osc.type = 'sine';
      osc.frequency.setValueAtTime(freq, now + idx * 0.14);
      gain.gain.setValueAtTime(0.08, now + idx * 0.14);
      gain.gain.exponentialRampToValueAtTime(0.001, now + idx * 0.14 + 0.25);
      osc.connect(gain);
      gain.connect(ctx.destination);
      osc.start(now + idx * 0.14);
      osc.stop(now + idx * 0.14 + 0.26);
    });
  } catch {
    // Ignore autoplay restrictions before user interaction
  }
}

function sendDesktopNotification(app: PendingTradeApprovalDto, secs: number) {
  if (typeof window === 'undefined' || !('Notification' in window)) return;
  if (Notification.permission !== 'granted') return;
  try {
    const n = new Notification(`🔔 BUY ${app.symbol} (${secs}s to Reject)`, {
      body: `${app.quantity} shares @ ₹${app.indicativePrice.toFixed(2)}\n${app.reason}`,
      tag: `approval-${app.id}`,
      requireInteraction: true,
    });
    n.onclick = () => {
      window.focus();
      n.close();
    };
  } catch {
    // Ignore unsupported browser contexts
  }
}

export const BuyApprovalToast: React.FC = () => {
  const [pending, setPending] = useState<PendingTradeApprovalDto[]>([]);
  const [nowMs, setNowMs] = useState(() => Date.now());
  const [rejectingId, setRejectingId] = useState<string | null>(null);
  const [notifPerm, setNotifPerm] = useState<NotificationPermission>(
    typeof window !== 'undefined' && 'Notification' in window ? Notification.permission : 'denied'
  );
  const alertedIdsRef = useRef<Set<string>>(new Set());
  const baseTitleRef = useRef<string>(typeof document !== 'undefined' ? document.title : 'StrikeSutra');

  const upsertApproval = (app: PendingTradeApprovalDto) => {
    if (app.status !== 'Pending') return;
    const deadlineMs = new Date(app.approvalDeadlineUtc).getTime();
    if (deadlineMs <= Date.now()) return;

    setPending((prev) => {
      const exists = prev.some((x) => x.id === app.id);
      if (exists) return prev.map((x) => (x.id === app.id ? app : x));
      return [app, ...prev];
    });

    if (!alertedIdsRef.current.has(app.id)) {
      alertedIdsRef.current.add(app.id);
      const secs = Math.max(1, Math.ceil((deadlineMs - Date.now()) / 1000));
      playAlertChime();
      sendDesktopNotification(app, secs);
    }
  };

  useEffect(() => {
    const fetchPending = async () => {
      try {
        const list = await approvalsApi.getPending();
        const active = (list || []).filter(
          (a: PendingTradeApprovalDto) => a.status === 'Pending' && new Date(a.approvalDeadlineUtc).getTime() > Date.now()
        );
        setPending(active);
      } catch {
        // Ignore transient network errors
      }
    };

    fetchPending();
    const poll = setInterval(fetchPending, 15000);

    const unsubSignalR = signalRClient.onBuyApproval((app) => {
      upsertApproval(app);
    });

    const onLocalCreated = (e: Event) => {
      const custom = e as CustomEvent<PendingTradeApprovalDto>;
      if (custom.detail) upsertApproval(custom.detail);
    };
    window.addEventListener('approval-created', onLocalCreated);

    return () => {
      clearInterval(poll);
      unsubSignalR();
      window.removeEventListener('approval-created', onLocalCreated);
    };
  }, []);

  // Tick every 500ms for smooth countdown + prune expired items
  useEffect(() => {
    if (pending.length === 0) {
      document.title = baseTitleRef.current;
      return;
    }

    const tick = setInterval(() => {
      const current = Date.now();
      setNowMs(current);
      setPending((prev) => prev.filter((a) => new Date(a.approvalDeadlineUtc).getTime() > current));
    }, 500);

    return () => clearInterval(tick);
  }, [pending.length]);

  // Flash browser tab title while any approval is pending so user notices from other tabs
  useEffect(() => {
    if (pending.length === 0) {
      document.title = baseTitleRef.current;
      return;
    }

    let toggle = false;
    const flash = setInterval(() => {
      toggle = !toggle;
      const top = pending[0];
      const secs = Math.max(0, Math.ceil((new Date(top.approvalDeadlineUtc).getTime() - Date.now()) / 1000));
      document.title = toggle
        ? `🔔 [${secs}s] BUY ${top.symbol} — ACTION NEEDED`
        : baseTitleRef.current;
    }, 800);

    return () => {
      clearInterval(flash);
      document.title = baseTitleRef.current;
    };
  }, [pending]);

  const handleReject = async (id: string) => {
    setRejectingId(id);
    try {
      await approvalsApi.reject(id, 'Rejected by user from countdown alert');
      setPending((prev) => prev.filter((a) => a.id !== id));
      window.dispatchEvent(new Event('approval-updated'));
    } catch {
      // Refresh list on failure (e.g. already expired)
      setPending((prev) => prev.filter((a) => a.id !== id));
    } finally {
      setRejectingId(null);
    }
  };

  const requestDesktopPermission = async () => {
    if (!('Notification' in window)) return;
    const perm = await Notification.requestPermission();
    setNotifPerm(perm);
  };

  if (pending.length === 0) return null;

  return (
    <div className="fixed bottom-4 right-4 z-50 w-full max-w-sm space-y-3 pointer-events-auto">
      {notifPerm === 'default' && (
        <button
          onClick={requestDesktopPermission}
          className="w-full flex items-center justify-center gap-2 px-3 py-1.5 rounded-lg bg-amber-500/20 border border-amber-500/40 text-amber-200 text-xs font-semibold hover:bg-amber-500/30 transition shadow-lg"
        >
          <BellRing size={14} />
          Enable Browser Desktop Alerts for Background Tabs
        </button>
      )}

      {pending.slice(0, 3).map((app) => {
        const deadlineMs = new Date(app.approvalDeadlineUtc).getTime();
        const remainingSecs = Math.max(0, Math.ceil((deadlineMs - nowMs) / 1000));
        const totalSecs = Math.max(remainingSecs, app.remainingSeconds || 60);
        const pct = Math.min(100, Math.max(0, (remainingSecs / totalSecs) * 100));

        return (
          <div
            key={app.id}
            className="rounded-2xl bg-slate-900/95 border-2 border-amber-500/70 shadow-2xl shadow-amber-500/20 p-4 backdrop-blur-md space-y-3"
          >
            <div className="flex items-start justify-between gap-2">
              <div className="flex items-center gap-2">
                <div className="p-2 rounded-lg bg-amber-500/20 text-amber-400 animate-pulse">
                  <ShieldAlert size={18} />
                </div>
                <div>
                  <div className="text-xs font-bold uppercase tracking-wider text-amber-300">
                    Action Window • {app.side.toUpperCase()}
                  </div>
                  <div className="text-base font-extrabold text-white">
                    {app.symbol} × {app.quantity} @ ₹{app.indicativePrice.toFixed(2)}
                  </div>
                </div>
              </div>
              <div className="flex items-center gap-1 px-2.5 py-1 rounded-full bg-rose-500/20 border border-rose-500/40 text-rose-300 font-mono text-xs font-bold">
                <Clock size={12} />
                {remainingSecs}s
              </div>
            </div>

            <div className="w-full h-1.5 bg-slate-800 rounded-full overflow-hidden">
              <div
                className="h-full bg-gradient-to-r from-amber-500 to-rose-500 transition-all duration-500"
                style={{ width: `${pct}%` }}
              />
            </div>

            <p className="text-xs text-slate-300 line-clamp-2">{app.reason}</p>

            <div className="flex items-center justify-between text-[11px] text-slate-400">
              <span>SL: ₹{app.stopLoss?.toFixed(2) ?? '—'}</span>
              <span>Target: ₹{app.target?.toFixed(2) ?? '—'}</span>
              <span>Val: ₹{app.estimatedOrderValue.toLocaleString('en-IN')}</span>
            </div>

            <div className="flex items-center gap-2 pt-1">
              <button
                onClick={() => handleReject(app.id)}
                disabled={rejectingId === app.id}
                className="flex-1 flex items-center justify-center gap-1.5 px-3 py-2 rounded-lg bg-rose-600 hover:bg-rose-500 text-white text-xs font-bold shadow transition disabled:opacity-50"
              >
                <XCircle size={14} />
                {rejectingId === app.id ? 'Rejecting...' : `Reject (${remainingSecs}s)`}
              </button>
              <Link
                to="/approvals"
                className="px-3 py-2 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-semibold flex items-center gap-1 border border-slate-700"
              >
                <ExternalLink size={12} />
                Approvals
              </Link>
            </div>
          </div>
        );
      })}
    </div>
  );
};
