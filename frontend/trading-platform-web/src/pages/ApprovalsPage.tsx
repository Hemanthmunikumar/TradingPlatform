import React, { useState, useEffect, useRef } from 'react';
import { Clock } from 'lucide-react';
import { apiClient } from '../api/client';
import type { ApiResponse, TradingAccountDto } from '../types';

interface MockApproval {
  id: string;
  symbol: string;
  companyName: string;
  broker: string;
  account: string;
  strategy: string;
  side: 'BUY';
  quantity: number;
  price: number;
  orderValue: number;
  deadlineSeconds: number;
  status: 'Pending' | 'Rejected' | 'Approved' | 'Executed';
}

export const ApprovalsPage: React.FC = () => {
  const [approvals, setApprovals] = useState<MockApproval[]>([]);
  const [demoActive, setDemoActive] = useState(false);
  const [configuredDuration, setConfiguredDuration] = useState<number>(60);
  const [remainingTime, setRemainingTime] = useState(60.0);
  const [isLiveMode, setIsLiveMode] = useState(false);
  const timerRef = useRef<any>(null);

  useEffect(() => {
    const fetchDuration = async () => {
      try {
        const res = await apiClient.get<ApiResponse<TradingAccountDto[]>>('/trading-accounts');
        if (res.data.success && res.data.data) {
          const active = res.data.data.find(a => a.isActive) || res.data.data[0];
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

    return () => {
      if (timerRef.current) clearInterval(timerRef.current);
    };
  }, []);

  const triggerMockBuySignal = () => {
    // Clear any existing active timer
    if (timerRef.current) {
      clearInterval(timerRef.current);
      timerRef.current = null;
    }

    const id = Math.random().toString(36).substring(7);
    const mock: MockApproval = {
      id,
      symbol: 'INFY',
      companyName: 'Infosys Limited',
      broker: 'Upstox',
      account: 'Upstox Intraday Primary',
      strategy: 'Intraday EMA Breakout',
      side: 'BUY',
      quantity: 50,
      price: 1850.25,
      orderValue: 92512.50,
      deadlineSeconds: configuredDuration,
      status: 'Pending',
    };

    setApprovals((prev) => [mock, ...prev]);
    setDemoActive(true);
    setRemainingTime(configuredDuration);

    timerRef.current = setInterval(() => {
      setRemainingTime((prev) => {
        if (prev <= 0.1) {
          if (timerRef.current) {
            clearInterval(timerRef.current);
            timerRef.current = null;
          }
          setDemoActive(false);
          // Only execute if still in Pending state (never overwrite Rejected status)
          setApprovals((current) =>
            current.map((a) => (a.id === id && a.status === 'Pending' ? { ...a, status: 'Executed' } : a))
          );
          return 0;
        }
        return Number((prev - 0.1).toFixed(1));
      });
    }, 100);
  };

  const handleReject = async (id: string) => {
    // 1. Immediately cancel the countdown interval
    if (timerRef.current) {
      clearInterval(timerRef.current);
      timerRef.current = null;
    }
    setDemoActive(false);

    // 2. Permanently update status to Rejected
    setApprovals((current) =>
      current.map((a) => (a.id === id ? { ...a, status: 'Rejected' } : a))
    );

    // 3. If real GUID approval from backend, dispatch to backend
    if (id && id.length > 20) {
      try {
        await apiClient.post(`/trade-approvals/${id}/reject`, { reason: 'User rejected manually' });
      } catch (err) {
        console.error('Failed to notify backend of rejection', err);
      }
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl sm:text-3xl font-extrabold text-white tracking-tight">
            BUY Approvals &amp; Fast-Action Center
          </h1>
          <p className="text-xs sm:text-sm text-slate-400 mt-0.5">
            Real-time server-enforced rejection window for automated BUY signals
          </p>
        </div>

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

      {/* Floating Demo Toast */}
      {demoActive && (
        <div className="p-4 rounded-xl bg-slate-900 border-2 border-amber-500 shadow-2xl relative overflow-hidden animate-pulse">
          <div className="flex items-start justify-between gap-4">
            <div className="space-y-1">
              <div className="flex items-center gap-2">
                <span className="px-2 py-0.5 rounded bg-emerald-500/20 text-emerald-400 font-bold text-xs">
                  BUY SIGNAL GENERATED
                </span>
                <span className="text-xs font-mono text-amber-400 font-bold">
                  {remainingTime.toFixed(1)}s remaining to reject
                </span>
              </div>
              <h3 className="text-lg font-black text-white">INFY • Infosys Limited</h3>
              <p className="text-xs text-slate-300">
                Proposed 50 Qty @ ₹1,850.25 (Total: ₹92,512.50) via Intraday EMA Breakout
              </p>
            </div>

            <button
              onClick={() => handleReject(approvals[0]?.id)}
              className="px-5 py-2.5 bg-rose-600 hover:bg-rose-500 text-white text-xs font-extrabold rounded-lg shadow-lg shadow-rose-600/30 uppercase tracking-wider transition"
            >
              REJECT ORDER NOW
            </button>
          </div>

          {/* Countdown Progress Bar */}
          <div className="mt-3 h-1.5 w-full bg-slate-800 rounded-full overflow-hidden">
            <div
              className="h-full bg-amber-400 transition-all duration-100 ease-linear"
              style={{ width: `${(remainingTime / configuredDuration) * 100}%` }}
            />
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
            Durable records of all generated BUY signals, rejection times, and order dispatches
          </p>
        </div>

        {approvals.length === 0 ? (
          <div className="p-12 text-center text-slate-400 space-y-2">
            <Clock size={36} className="mx-auto text-slate-600" />
            <p className="text-sm font-medium">No signals recorded during this active session.</p>
            <p className="text-xs text-slate-500">
              Click &quot;Simulate 2s BUY Signal Alert&quot; to test the real-time rejection workflow.
            </p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm text-slate-300">
              <thead className="bg-slate-950/60 text-[11px] uppercase tracking-wider text-slate-400 border-b border-slate-800">
                <tr>
                  <th className="px-5 py-3">Symbol &amp; Company</th>
                  <th className="px-5 py-3">Strategy</th>
                  <th className="px-5 py-3">Broker Account</th>
                  <th className="px-5 py-3">Quantity &amp; Value</th>
                  <th className="px-5 py-3">Status</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-800/60 font-medium">
                {approvals.map((a) => (
                  <tr key={a.id} className="hover:bg-slate-800/30">
                    <td className="px-5 py-3.5">
                      <div className="font-bold text-white">{a.symbol}</div>
                      <div className="text-xs text-slate-500">{a.companyName}</div>
                    </td>
                    <td className="px-5 py-3.5 text-xs text-slate-300">{a.strategy}</td>
                    <td className="px-5 py-3.5 text-xs text-slate-400">{a.account}</td>
                    <td className="px-5 py-3.5">
                      <div className="font-mono text-white">{a.quantity} shares</div>
                      <div className="text-xs text-slate-400 font-mono">₹{a.orderValue.toLocaleString('en-IN')}</div>
                    </td>
                    <td className="px-5 py-3.5">
                      <span className={`px-2 py-0.5 rounded text-xs font-semibold ${
                        a.status === 'Executed'
                          ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/30'
                          : a.status === 'Rejected'
                          ? 'bg-rose-500/10 text-rose-400 border border-rose-500/30'
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
