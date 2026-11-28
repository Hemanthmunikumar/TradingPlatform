import React, { useState, useEffect, useRef, useCallback } from 'react';
import { Clock, RefreshCw, ShieldAlert, XCircle } from 'lucide-react';
import { apiClient, approvalsApi } from '../api/client';
import { signalRClient } from '../services/signalr';
import type { ApiResponse, PendingTradeApprovalDto, TradingAccountDto } from '../types';

export const ApprovalsPage: React.FC = () => {
  const [pendingApprovals, setPendingApprovals] = useState<PendingTradeApprovalDto[]>([]);
  const [historyApprovals, setHistoryApprovals] = useState<PendingTradeApprovalDto[]>([]);
  const [configuredDuration, setConfiguredDuration] = useState<number>(60);
  const [isLiveMode, setIsLiveMode] = useState(false);
  const [nowMs, setNowMs] = useState(() => Date.now());
  const [rejectingId, setRejectingId] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // Paper/Mock local simulation state
  const [demoActive, setDemoActive] = useState(false);
  const [remainingTime, setRemainingTime] = useState(60.0);
  const timerRef = useRef<any>(null);

  const loadApprovals = useCallback(async () => {
    try {
      const [pendingList, historyList] = await Promise.all([
        approvalsApi.getPending(),
        approvalsApi.getHistory(undefined, 1, 50),
      ]);
      const now = Date.now();
      setPendingApprovals(
        (pendingList || []).filter((a: PendingTradeApprovalDto) => a.status === 'Pending' && new Date(a.approvalDeadlineUtc).getTime() > now)
      );
      setHistoryApprovals(historyList || []);
    } catch (err) {
      console.error('Failed to load trade approvals', err);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    const fetchDuration = async () => {
      try {
        const res = await apiClient.get<ApiResponse<TradingAccountDto[]>>('/trading-accounts');
        if (res.data.success && res.data.data) {
          const active = res.data.data.find((a) => a.isActive) || res.data.data[0];
          setIsLiveMode(active?.tradingMode === 'Live');
          if (active?.configuration?.approvalDurationSeconds) {
            setConfiguredDuration(active.configuration.approvalDurationSeconds);
            setRemainingTime(active.configuration.approvalDurationSeconds);
          }
        }
      } catch (err) {
        console.error('Failed to load approval duration', err);
      }
    };

    fetchDuration();
    loadApprovals();

    const unsubSignalR = signalRClient.onBuyApproval(() => {
      loadApprovals();
    });
    const onRefresh = () => loadApprovals();
    window.addEventListener('approval-created', onRefresh);
    window.addEventListener('approval-updated', onRefresh);

    const poll = setInterval(loadApprovals, 10000);

    return () => {
      unsubSignalR();
      window.removeEventListener('approval-created', onRefresh);
      window.removeEventListener('approval-updated', onRefresh);
      clearInterval(poll);
      if (timerRef.current) clearInterval(timerRef.current);
    };
  }, [loadApprovals]);

  useEffect(() => {
    if (pendingApprovals.length === 0) return;
    const tick = setInterval(() => {
      const current = Date.now();
      setNowMs(current);
      setPendingApprovals((prev) => prev.filter((a) => new Date(a.approvalDeadlineUtc).getTime() > current));
    }, 500);
    return () => clearInterval(tick);
  }, [pendingApprovals.length]);

  const triggerMockBuySignal = () => {
    if (timerRef.current) {
      clearInterval(timerRef.current);
      timerRef.current = null;
    }
    setDemoActive(true);
    setRemainingTime(configuredDuration);

    timerRef.current = setInterval(() => {
      setRemainingTime((prev) => {
        if (prev <= 0.1) {
          clearInterval(timerRef.current);
          timerRef.current = null;
          setDemoActive(false);
          return 0;
        }
        return Number((prev - 0.1).toFixed(1));
      });
    }, 100);
  };

  const handleReject = async (id: string) => {
    setRejectingId(id);
    try {
      await approvalsApi.reject(id, 'User rejected from Pending Approvals');
      setPendingApprovals((prev) => prev.filter((a) => a.id !== id));
      window.dispatchEvent(new Event('approval-updated'));
      await loadApprovals();
    } catch (err) {
      console.error('Failed to reject approval', err);
      await loadApprovals();
    } finally {
      setRejectingId(null);
    }
  };

  const formatIst = (iso?: string) => {
    if (!iso) return '—';
    return new Date(iso).toLocaleString('en-IN', {
      timeZone: 'Asia/Kolkata',
      day: '2-digit',
      month: 'short',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
    }) + ' IST';
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl sm:text-3xl font-extrabold text-white tracking-tight">
            BUY Approvals &amp; Fast-Action Center
          </h1>
          <p className="text-xs sm:text-sm text-slate-400 mt-0.5">
            Real-time server-enforced {configuredDuration}s rejection window for automated &amp; AI Co-Pilot BUY signals
          </p>
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={loadApprovals}
            className="flex items-center gap-1.5 px-3 py-2 bg-slate-800 hover:bg-slate-700 text-slate-200 rounded-lg text-xs font-semibold border border-slate-700 transition"
          >
            <RefreshCw size={14} className={isLoading ? 'animate-spin' : ''} />
            Refresh
          </button>
          {!isLiveMode && (
            <button
              onClick={triggerMockBuySignal}
              className="flex items-center gap-2 px-4 py-2 bg-gradient-to-r from-amber-500 to-rose-600 hover:from-amber-400 hover:to-rose-500 text-white rounded-lg text-xs font-semibold shadow-md shadow-amber-500/20 transition"
            >
              <Clock size={16} />
              Simulate {configuredDuration}s BUY Signal Alert
            </button>
          )}
        </div>
      </div>

      {/* Active Live Pending Approvals */}
      {pendingApprovals.map((app) => {
        const deadlineMs = new Date(app.approvalDeadlineUtc).getTime();
        const remainingSecs = Math.max(0, Math.ceil((deadlineMs - nowMs) / 1000));
        const totalSecs = Math.max(remainingSecs, configuredDuration || 60);
        const pct = Math.min(100, Math.max(0, (remainingSecs / totalSecs) * 100));

        return (
          <div key={app.id} className="p-5 rounded-xl bg-slate-900 border-2 border-amber-500 shadow-2xl space-y-3">
            <div className="flex flex-col sm:flex-row sm:items-start justify-between gap-4">
              <div className="space-y-1">
                <div className="flex flex-wrap items-center gap-2">
                  <span className="px-2 py-0.5 rounded bg-emerald-500/20 text-emerald-400 font-bold text-xs flex items-center gap-1">
                    <ShieldAlert size={12} /> {app.side.toUpperCase()} SIGNAL ACTIVE
                  </span>
                  <span className="text-xs font-mono text-amber-400 font-bold">
                    {remainingSecs}s remaining to reject
                  </span>
                </div>
                <h3 className="text-lg font-black text-white">
                  {app.symbol} • {app.companyName}
                </h3>
                <p className="text-xs text-slate-300">
                  {app.quantity} shares @ ₹{app.indicativePrice.toFixed(2)} (Est. ₹{app.estimatedOrderValue.toLocaleString('en-IN')})
                  {' • '}SL: ₹{app.stopLoss?.toFixed(2) ?? '—'} • Target: ₹{app.target?.toFixed(2) ?? '—'}
                </p>
                <p className="text-xs text-slate-400">{app.reason}</p>
              </div>

              <button
                onClick={() => handleReject(app.id)}
                disabled={rejectingId === app.id}
                className="flex items-center gap-1.5 px-5 py-2.5 bg-rose-600 hover:bg-rose-500 text-white text-xs font-extrabold rounded-lg shadow-lg shadow-rose-600/30 uppercase tracking-wider transition disabled:opacity-50 shrink-0"
              >
                <XCircle size={14} />
                {rejectingId === app.id ? 'Rejecting...' : 'REJECT ORDER NOW'}
              </button>
            </div>

            <div className="h-1.5 w-full bg-slate-800 rounded-full overflow-hidden">
              <div
                className="h-full bg-amber-400 transition-all duration-500 ease-linear"
                style={{ width: `${pct}%` }}
              />
            </div>
          </div>
        );
      })}

      {/* Paper/Mock Demo Simulation Toast */}
      {demoActive && (
        <div className="p-4 rounded-xl bg-slate-900 border-2 border-amber-500/70 shadow-2xl">
          <div className="flex items-start justify-between gap-4">
            <div className="space-y-1">
              <div className="flex items-center gap-2">
                <span className="px-2 py-0.5 rounded bg-emerald-500/20 text-emerald-400 font-bold text-xs">
                  SIMULATED BUY SIGNAL
                </span>
                <span className="text-xs font-mono text-amber-400 font-bold">
                  {remainingTime.toFixed(1)}s remaining to reject
                </span>
              </div>
              <h3 className="text-lg font-black text-white">INFY • Infosys Limited</h3>
              <p className="text-xs text-slate-300">
                Simulated 50 Qty @ ₹1,850.25 (Total: ₹92,512.50)
              </p>
            </div>
            <button
              onClick={() => {
                if (timerRef.current) clearInterval(timerRef.current);
                setDemoActive(false);
              }}
              className="px-5 py-2.5 bg-rose-600 hover:bg-rose-500 text-white text-xs font-extrabold rounded-lg uppercase"
            >
              REJECT SIMULATION
            </button>
          </div>
        </div>
      )}

      {/* Approvals History Table */}
      <div className="bg-slate-900 border border-slate-800 rounded-xl overflow-hidden shadow-sm">
        <div className="px-5 py-4 border-b border-slate-800">
          <h3 className="text-sm font-bold text-white tracking-wide uppercase">
            Signal Approval Audit Log
          </h3>
          <p className="text-xs text-slate-400 mt-0.5">
            Durable database records of all generated BUY/SELL signals, rejection windows, and outcomes
          </p>
        </div>

        {historyApprovals.length === 0 ? (
          <div className="p-12 text-center text-slate-400 space-y-2">
            <Clock size={36} className="mx-auto text-slate-600" />
            <p className="text-sm font-medium">No signals recorded yet for the active broker.</p>
            <p className="text-xs text-slate-500">
              Trigger a Buy from AI Co-Pilot or wait for live market signals to see them here.
            </p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm text-slate-300">
              <thead className="bg-slate-950/60 text-[11px] uppercase tracking-wider text-slate-400 border-b border-slate-800">
                <tr>
                  <th className="px-5 py-3">Symbol &amp; Reason</th>
                  <th className="px-5 py-3">Deadline (IST)</th>
                  <th className="px-5 py-3">Quantity &amp; Value</th>
                  <th className="px-5 py-3">Status</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-800/60 font-medium">
                {historyApprovals.map((a) => (
                  <tr key={a.id} className="hover:bg-slate-800/30">
                    <td className="px-5 py-3.5">
                      <div className="font-bold text-white">{a.symbol} ({a.side})</div>
                      <div className="text-xs text-slate-400 max-w-md truncate">{a.reason}</div>
                    </td>
                    <td className="px-5 py-3.5 text-xs text-slate-400 font-mono">
                      {formatIst(a.approvalDeadlineUtc)}
                    </td>
                    <td className="px-5 py-3.5">
                      <div className="font-mono text-white">{a.quantity} @ ₹{a.indicativePrice.toFixed(2)}</div>
                      <div className="text-xs text-slate-400 font-mono">₹{a.estimatedOrderValue.toLocaleString('en-IN')}</div>
                    </td>
                    <td className="px-5 py-3.5">
                      <span className={`px-2 py-0.5 rounded text-xs font-semibold ${
                        a.status === 'Approved'
                          ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/30'
                          : a.status === 'Rejected'
                          ? 'bg-rose-500/10 text-rose-400 border border-rose-500/30'
                          : a.status === 'Expired'
                          ? 'bg-slate-800 text-slate-400 border border-slate-700'
                          : 'bg-amber-500/10 text-amber-400 border border-amber-500/30'
                      }`}>
                        {a.status}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
};
