import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { 
  Wallet, 
  TrendingUp, 
  ShieldAlert, 
  Layers, 
  Clock, 
  ArrowUpRight, 
  RefreshCw, 
  AlertTriangle,
  Activity
} from 'lucide-react';
import { apiClient } from '../api/client';
import type { ApiResponse, TradingAccountDto, BrokerConnectionDto } from '../types';

export const DashboardPage: React.FC = () => {
  const [accounts, setAccounts] = useState<TradingAccountDto[]>([]);
  const [connections, setConnections] = useState<BrokerConnectionDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchData = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const [accRes, connRes] = await Promise.all([
        apiClient.get<ApiResponse<TradingAccountDto[]>>('/trading-accounts'),
        apiClient.get<ApiResponse<BrokerConnectionDto[]>>('/brokers/connections'),
      ]);

      if (accRes.data.success && accRes.data.data) {
        setAccounts(accRes.data.data);
      }
      if (connRes.data.success && connRes.data.data) {
        setConnections(connRes.data.data);
      }
    } catch (err: any) {
      setError('Could not fetch portfolio accounts. Check backend connection.');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, []);

  // Aggregated KPI calculations across individual accounts (displaying per-account separation as well)
  const totalAllocated = accounts.reduce((acc, a) => acc + (a.allocatedCapital || 0), 0);
  const totalAvailable = accounts.reduce((acc, a) => acc + (a.availableFunds || 0), 0);
  const activeAccountsCount = accounts.filter((a) => a.isActive).length;
  const liveAccountsCount = accounts.filter((a) => a.tradingMode === 'Live').length;
  const activeAccount = accounts.find((a) => a.isActive) || accounts[0];
  const rejectionWindowSeconds = activeAccount?.configuration?.approvalDurationSeconds || 60;

  const formatINR = (val: number) => {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      maximumFractionDigits: 2,
    }).format(val);
  };

  return (
    <div className="space-y-6">
      {/* Top Banner / Greetings */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl sm:text-3xl font-extrabold text-white tracking-tight">
            Trading Dashboard
          </h1>
          <p className="text-xs sm:text-sm text-slate-400 mt-0.5">
            Real-time multi-broker Indian equity management • IST 09:15 - 15:30
          </p>
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={fetchData}
            disabled={isLoading}
            className="flex items-center gap-1.5 px-3 py-2 bg-slate-800 hover:bg-slate-700 text-slate-300 rounded-lg text-xs font-semibold border border-slate-700 transition"
          >
            <RefreshCw size={14} className={isLoading ? 'animate-spin' : ''} />
            Refresh
          </button>
          <Link
            to="/brokers"
            className="flex items-center gap-1.5 px-3 py-2 bg-sky-600 hover:bg-sky-500 text-white rounded-lg text-xs font-semibold shadow-md shadow-sky-600/20 transition"
          >
            <Layers size={14} />
            Manage Brokers
          </Link>
        </div>
      </div>

      {error && (
        <div className="p-4 rounded-xl bg-rose-500/10 border border-rose-500/30 text-rose-400 text-sm flex items-center justify-between">
          <div className="flex items-center gap-2">
            <AlertTriangle size={18} />
            <span>{error}</span>
          </div>
          <button onClick={fetchData} className="underline text-xs hover:text-white">Retry</button>
        </div>
      )}

      {/* KPI Cards Grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {/* Total Allocated Capital */}
        <div className="p-4 rounded-xl bg-slate-900 border border-slate-800 flex flex-col justify-between">
          <div className="flex items-center justify-between text-slate-400">
            <span className="text-xs font-semibold uppercase tracking-wider">Allocated Capital</span>
            <div className="p-2 rounded-lg bg-sky-500/10 text-sky-400">
              <Wallet size={18} />
            </div>
          </div>
          <div className="mt-3">
            <div className="text-2xl font-bold text-white tracking-tight">
              {formatINR(totalAllocated)}
            </div>
            <p className="text-[11px] text-slate-400 mt-1">
              Across {accounts.length} linked account{accounts.length === 1 ? '' : 's'}
            </p>
          </div>
        </div>

        {/* Available Cash / Margin */}
        <div className="p-4 rounded-xl bg-slate-900 border border-slate-800 flex flex-col justify-between">
          <div className="flex items-center justify-between text-slate-400">
            <span className="text-xs font-semibold uppercase tracking-wider">Available Funds</span>
            <div className="p-2 rounded-lg bg-emerald-500/10 text-emerald-400">
              <Activity size={18} />
            </div>
          </div>
          <div className="mt-3">
            <div className="text-2xl font-bold text-emerald-400 tracking-tight">
              {formatINR(totalAvailable)}
            </div>
            <p className="text-[11px] text-slate-400 mt-1">
              Ready for algorithmic deployment
            </p>
          </div>
        </div>

        {/* Realized P&L (Today) */}
        <div className="p-4 rounded-xl bg-slate-900 border border-slate-800 flex flex-col justify-between">
          <div className="flex items-center justify-between text-slate-400">
            <span className="text-xs font-semibold uppercase tracking-wider">Today&apos;s Realized P&L</span>
            <div className="p-2 rounded-lg bg-emerald-500/10 text-emerald-400">
              <TrendingUp size={18} />
            </div>
          </div>
          <div className="mt-3">
            <div className="text-2xl font-bold text-emerald-400 tracking-tight">
              +₹0.00
            </div>
            <p className="text-[11px] text-slate-400 mt-1">
              Net of taxes &amp; broker charges
            </p>
          </div>
        </div>

        {/* Risk & Kill Switch Status */}
        <div className="p-4 rounded-xl bg-slate-900 border border-slate-800 flex flex-col justify-between">
          <div className="flex items-center justify-between text-slate-400">
            <span className="text-xs font-semibold uppercase tracking-wider">Engine Safety Gate</span>
            <div className="p-2 rounded-lg bg-indigo-500/10 text-indigo-400">
              <ShieldAlert size={18} />
            </div>
          </div>
          <div className="mt-3">
            <div className="flex items-center gap-2">
              <span className="w-2.5 h-2.5 rounded-full bg-emerald-400 animate-pulse" />
              <span className="text-xl font-bold text-white">Active &amp; Guarded</span>
            </div>
            <p className="text-[11px] text-slate-400 mt-1">
              19-point risk verification ready
            </p>
          </div>
        </div>
      </div>

      {/* 2-Second BUY Approval Window Alert Banner */}
      <div className="p-4 rounded-xl bg-gradient-to-r from-amber-500/10 via-sky-500/5 to-transparent border border-amber-500/30 flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div className="flex items-center gap-3">
          <div className="p-2.5 rounded-xl bg-amber-500/20 text-amber-400 shrink-0">
            <Clock size={20} />
          </div>
          <div>
            <h4 className="text-sm font-bold text-white flex items-center gap-2">
              Fast-Action {rejectionWindowSeconds}-Second BUY Rejection Window
              <span className="text-[10px] uppercase font-mono px-1.5 py-0.5 rounded bg-amber-500/20 text-amber-300">
                Active Policy ({rejectionWindowSeconds}s)
              </span>
            </h4>
            <p className="text-xs text-slate-300 mt-0.5">
              BUY signals trigger persistent instant toasts with interactive {rejectionWindowSeconds}s reject countdown. Zero pending signals right now.
            </p>
          </div>
        </div>
        <Link
          to="/approvals"
          className="inline-flex items-center gap-1 text-xs font-semibold text-amber-400 hover:text-amber-300 whitespace-nowrap self-start sm:self-auto"
        >
          View Approvals Center <ArrowUpRight size={14} />
        </Link>
      </div>

      {/* Broker Accounts Overview Table */}
      <div className="bg-slate-900 border border-slate-800 rounded-xl overflow-hidden shadow-sm">
        <div className="px-5 py-4 border-b border-slate-800 flex items-center justify-between">
          <div>
            <h3 className="text-sm font-bold text-white tracking-wide uppercase">
              Linked Broker Accounts
            </h3>
            <p className="text-xs text-slate-400 mt-0.5">
              Isolated funds, individual risk limits and credentials
            </p>
          </div>
          <span className="text-xs font-mono px-2 py-0.5 rounded bg-slate-800 text-slate-300 border border-slate-700">
            {activeAccountsCount} Active / {liveAccountsCount} Live
          </span>
        </div>

        {accounts.length === 0 ? (
          <div className="p-8 text-center text-slate-400 space-y-3">
            <Layers size={36} className="mx-auto text-slate-600" />
            <p className="text-sm">No broker accounts linked yet.</p>
            <Link
              to="/brokers"
              className="inline-flex items-center gap-2 px-4 py-2 bg-sky-600 hover:bg-sky-500 text-white rounded-lg text-xs font-semibold transition"
            >
              Connect Upstox, Zerodha or Mock Engine
            </Link>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm text-slate-300">
              <thead className="bg-slate-950/60 text-[11px] uppercase tracking-wider text-slate-400 border-b border-slate-800">
                <tr>
                  <th className="px-5 py-3">Account &amp; Broker</th>
                  <th className="px-5 py-3">Trading Mode</th>
                  <th className="px-5 py-3">Allocated Capital</th>
                  <th className="px-5 py-3">Available Cash</th>
                  <th className="px-5 py-3">Status</th>
                  <th className="px-5 py-3 text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-800/60 font-medium">
                {accounts.map((acc) => {
                  const conn = connections.find((c) => c.id === acc.brokerConnectionId);
                  return (
                    <tr key={acc.id} className="hover:bg-slate-800/30 transition">
                      <td className="px-5 py-3.5">
                        <div className="font-semibold text-white">{acc.accountName}</div>
                        <div className="text-xs text-slate-500 font-mono">
                          {conn?.providerCode || 'Broker'} • ID: {acc.accountId}
                        </div>
                      </td>
                      <td className="px-5 py-3.5">
                        <span className={`px-2 py-0.5 rounded text-xs font-semibold ${
                          acc.tradingMode === 'Paper' 
                            ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/30' 
                            : 'bg-rose-500/10 text-rose-400 border border-rose-500/30'
                        }`}>
                          {acc.tradingMode}
                        </span>
                      </td>
                      <td className="px-5 py-3.5 text-white font-mono">
                        {formatINR(acc.allocatedCapital)}
                      </td>
                      <td className="px-5 py-3.5 text-emerald-400 font-mono">
                        {formatINR(acc.availableFunds)}
                      </td>
                      <td className="px-5 py-3.5">
                        <span className={`inline-flex items-center gap-1.5 text-xs ${
                          acc.tradingEnabled ? 'text-emerald-400' : 'text-slate-500'
                        }`}>
                          <span className={`w-1.5 h-1.5 rounded-full ${
                            acc.tradingEnabled ? 'bg-emerald-400' : 'bg-slate-500'
                          }`} />
                          {acc.tradingEnabled ? 'Enabled' : 'Paused'}
                        </span>
                      </td>
                      <td className="px-5 py-3.5 text-right">
                        <Link
                          to={`/settings?accountId=${acc.id}`}
                          className="text-xs font-semibold text-sky-400 hover:text-sky-300 transition"
                        >
                          Configure Risk →
                        </Link>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
};
