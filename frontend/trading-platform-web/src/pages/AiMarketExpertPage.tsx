import React, { useEffect, useState } from 'react';
import { 
  Sparkles, 
  TrendingUp, 
  TrendingDown, 
  ShieldCheck, 
  CheckCircle2, 
  Clock, 
  Plus, 
  Trash2, 
  RefreshCw, 
  Zap, 
  Eye,
  DollarSign
} from 'lucide-react';
import { apiClient } from '../api/client';
import type { ApiResponse } from '../types';

export interface AiAnalysisResultDto {
  symbol: string;
  companyName: string;
  action: 'STRONG_BUY' | 'BUY_THE_DIP' | 'HOLD' | 'BOOK_PROFIT_50' | 'EXIT_STOP_LOSS' | string;
  convictionScore: number;
  currentPrice: number;
  suggestedPrice: number;
  suggestedQuantity: number;
  stopLoss: number;
  target1: number;
  target2: number;
  summary: string;
  technicalRationale: string;
  riskNote: string;
  providerName: string;
  analyzedAtUtc: string;
}

export const AiMarketExpertPage: React.FC = () => {
  const [analyses, setAnalyses] = useState<AiAnalysisResultDto[]>([]);
  const [watchlist, setWatchlist] = useState<string[]>([]);
  const [newSymbol, setNewSymbol] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [isSimulating, setIsSimulating] = useState(false);
  const [simMessage, setSimMessage] = useState<string | null>(null);
  const [filterAction, setFilterAction] = useState<string>('ALL');

  const fetchData = async () => {
    setIsLoading(true);
    setSimMessage(null);
    try {
      const [overviewRes, watchlistRes] = await Promise.all([
        apiClient.get<ApiResponse<AiAnalysisResultDto[]>>('/ai/overview'),
        apiClient.get<ApiResponse<string[]>>('/ai/special-monitor'),
      ]);

      if (overviewRes.data.success && overviewRes.data.data) {
        setAnalyses(overviewRes.data.data);
      }
      if (watchlistRes.data.success && watchlistRes.data.data) {
        setWatchlist(watchlistRes.data.data);
      }
    } catch (err: any) {
      console.error('Failed to load AI market analysis', err);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, []);

  const handleAddWatchlist = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newSymbol.trim()) return;

    try {
      const res = await apiClient.post<ApiResponse<boolean>>('/ai/special-monitor/add', {
        symbol: newSymbol.trim().toUpperCase(),
      });
      if (res.data.success) {
        setNewSymbol('');
        await fetchData();
      }
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to add symbol to Special Monitor');
    }
  };

  const handleRemoveWatchlist = async (symbol: string) => {
    try {
      await apiClient.delete(`/ai/special-monitor/${symbol}`);
      await fetchData();
    } catch (err: any) {
      alert('Failed to remove symbol');
    }
  };

  const handleSimulateBuy = async (symbol: string = 'INFY') => {
    setIsSimulating(true);
    setSimMessage(null);
    try {
      const res = await apiClient.post<ApiResponse<any>>('/ai/simulate-buy-signal', {
        symbol,
        quantity: 10,
      });
      if (res.data.success) {
        setSimMessage(`⚡ 2-Second BUY Window Triggered for 10 shares of ${symbol} @ ₹${res.data.data?.indicativePrice}! Watch the persistent countdown toast.`);
      }
    } catch (err: any) {
      setSimMessage(`Simulation failed: ${err.response?.data?.message || err.message}`);
    } finally {
      setIsSimulating(false);
    }
  };

  const filteredAnalyses = filterAction === 'ALL'
    ? analyses
    : analyses.filter(a => a.action === filterAction);

  const getActionBadge = (action: string) => {
    switch (action) {
      case 'STRONG_BUY':
        return (
          <span className="px-2.5 py-1 rounded-full text-xs font-bold bg-emerald-500/20 text-emerald-400 border border-emerald-500/40 flex items-center gap-1">
            <TrendingUp size={13} /> STRONG BUY
          </span>
        );
      case 'BUY_THE_DIP':
        return (
          <span className="px-2.5 py-1 rounded-full text-xs font-bold bg-sky-500/20 text-sky-400 border border-sky-500/40 flex items-center gap-1">
            <Zap size={13} /> BUY THE DIP
          </span>
        );
      case 'BOOK_PROFIT_50':
        return (
          <span className="px-2.5 py-1 rounded-full text-xs font-bold bg-amber-500/20 text-amber-300 border border-amber-500/40 flex items-center gap-1">
            <DollarSign size={13} /> BOOK 50% PROFIT
          </span>
        );
      case 'EXIT_STOP_LOSS':
        return (
          <span className="px-2.5 py-1 rounded-full text-xs font-bold bg-rose-500/20 text-rose-400 border border-rose-500/40 flex items-center gap-1">
            <TrendingDown size={13} /> EXIT / STOP LOSS
          </span>
        );
      default:
        return (
          <span className="px-2.5 py-1 rounded-full text-xs font-semibold bg-slate-800 text-slate-300 border border-slate-700">
            HOLD / WATCH
          </span>
        );
    }
  };

  return (
    <div className="space-y-6 max-w-7xl mx-auto">
      {/* Header */}
      <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-4">
        <div>
          <div className="flex items-center gap-2">
            <span className="p-1.5 rounded-lg bg-gradient-to-tr from-sky-500 to-indigo-600 text-white shadow-md shadow-sky-500/20">
              <Sparkles size={20} />
            </span>
            <h1 className="text-2xl sm:text-3xl font-extrabold text-white tracking-tight">
              AI Market Co-Pilot &amp; Special Monitor
            </h1>
          </div>
          <p className="text-xs sm:text-sm text-slate-400 mt-1">
            Autonomous multi-timeframe market analyst powered by <strong>Gemini Pro (Configurable)</strong> with dip accumulation and profit booking advice.
          </p>
        </div>

        {/* Action Controls */}
        <div className="flex flex-wrap items-center gap-2">
          <button
            onClick={fetchData}
            disabled={isLoading}
            className="flex items-center gap-1.5 px-3 py-2 bg-slate-800 hover:bg-slate-700 text-slate-300 rounded-lg text-xs font-semibold border border-slate-700 transition"
          >
            <RefreshCw size={14} className={isLoading ? 'animate-spin' : ''} />
            Refresh Scan
          </button>

          {/* 1-Click Interactive Test Trial Button */}
          <button
            onClick={() => handleSimulateBuy('INFY')}
            disabled={isSimulating}
            className="flex items-center gap-2 px-4 py-2 bg-gradient-to-r from-emerald-500 to-teal-600 hover:from-emerald-400 hover:to-teal-500 text-white rounded-lg text-xs font-bold shadow-lg shadow-emerald-500/20 transition active:scale-95"
            title="Triggers instant 2-second BUY rejection window countdown"
          >
            <Zap size={15} className={isSimulating ? 'animate-spin' : ''} />
            ⚡ Test 2s BUY Rejection Window
          </button>
        </div>
      </div>

      {/* Simulation Feedback Alert */}
      {simMessage && (
        <div className="p-4 rounded-xl bg-emerald-950/60 border border-emerald-500/40 text-emerald-300 text-xs flex items-center justify-between shadow-lg">
          <div className="flex items-center gap-2.5">
            <CheckCircle2 size={18} className="text-emerald-400 shrink-0" />
            <span className="font-medium">{simMessage}</span>
          </div>
          <button
            onClick={() => setSimMessage(null)}
            className="text-slate-400 hover:text-white text-xs underline ml-2"
          >
            Dismiss
          </button>
        </div>
      )}

      {/* KPI Overview Cards */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <div className="p-4 rounded-xl bg-slate-900 border border-slate-800 space-y-1">
          <div className="flex items-center justify-between text-slate-400 text-xs font-medium">
            <span>Special Watchlist</span>
            <Eye size={15} className="text-sky-400" />
          </div>
          <p className="text-2xl font-extrabold text-white">{watchlist.length} Stocks</p>
          <p className="text-[11px] text-slate-500">Live multi-timeframe monitoring</p>
        </div>

        <div className="p-4 rounded-xl bg-slate-900 border border-slate-800 space-y-1">
          <div className="flex items-center justify-between text-slate-400 text-xs font-medium">
            <span>Actionable Signals</span>
            <TrendingUp size={15} className="text-emerald-400" />
          </div>
          <p className="text-2xl font-extrabold text-emerald-400">
            {analyses.filter(a => a.action === 'STRONG_BUY' || a.action === 'BUY_THE_DIP').length} Active
          </p>
          <p className="text-[11px] text-slate-500">High-conviction entry setups</p>
        </div>

        <div className="p-4 rounded-xl bg-slate-900 border border-slate-800 space-y-1">
          <div className="flex items-center justify-between text-slate-400 text-xs font-medium">
            <span>AI Conviction Index</span>
            <Sparkles size={15} className="text-purple-400" />
          </div>
          <p className="text-2xl font-extrabold text-purple-400">
            {analyses.length > 0 
              ? `${Math.round((analyses.reduce((acc, a) => acc + a.convictionScore, 0) / analyses.length) * 100)}%`
              : '88%'}
          </p>
          <p className="text-[11px] text-slate-500">Confidence average</p>
        </div>

        <div className="p-4 rounded-xl bg-slate-900 border border-slate-800 space-y-1">
          <div className="flex items-center justify-between text-slate-400 text-xs font-medium">
            <span>Safety Gate</span>
            <ShieldCheck size={15} className="text-sky-400" />
          </div>
          <p className="text-2xl font-extrabold text-sky-400">19-Point Guard</p>
          <p className="text-[11px] text-slate-500">2s Rejection Window active</p>
        </div>
      </div>

      {/* Special Monitor Input & Chips */}
      <div className="p-4 rounded-xl bg-slate-900 border border-slate-800 space-y-3">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div>
            <h2 className="text-sm font-bold text-white flex items-center gap-1.5">
              <Eye size={16} className="text-sky-400" />
              Special Monitor (Priority Stocks)
            </h2>
            <p className="text-xs text-slate-400">
              Add any NSE stock ticker to prioritize for Dip Accumulation &amp; Breakout Entry analysis.
            </p>
          </div>

          <form onSubmit={handleAddWatchlist} className="flex items-center gap-2">
            <input
              type="text"
              placeholder="e.g. ZOMATO, BEL, HAL"
              value={newSymbol}
              onChange={(e) => setNewSymbol(e.target.value.toUpperCase())}
              className="px-3 py-1.5 bg-slate-950 border border-slate-700 rounded-lg text-slate-100 text-xs uppercase focus:outline-none focus:ring-2 focus:ring-sky-500 font-mono w-44"
            />
            <button
              type="submit"
              className="flex items-center gap-1 px-3 py-1.5 bg-sky-600 hover:bg-sky-500 text-white rounded-lg text-xs font-semibold shadow-md transition"
            >
              <Plus size={14} /> Add
            </button>
          </form>
        </div>

        <div className="flex flex-wrap gap-2 pt-1">
          {watchlist.map((sym) => (
            <span
              key={sym}
              className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg bg-slate-950 border border-slate-700/80 text-xs font-mono font-bold text-slate-200"
            >
              {sym}
              <button
                onClick={() => handleRemoveWatchlist(sym)}
                className="text-slate-500 hover:text-rose-400 transition"
                title={`Remove ${sym}`}
              >
                <Trash2 size={12} />
              </button>
            </span>
          ))}
        </div>
      </div>

      {/* Filter Tabs */}
      <div className="flex flex-wrap items-center gap-2 pt-2">
        <span className="text-xs font-semibold text-slate-400 mr-1">Filter Signals:</span>
        {['ALL', 'STRONG_BUY', 'BUY_THE_DIP', 'BOOK_PROFIT_50', 'HOLD', 'EXIT_STOP_LOSS'].map((filter) => (
          <button
            key={filter}
            onClick={() => setFilterAction(filter)}
            className={`px-3 py-1 rounded-lg text-xs font-semibold transition ${
              filterAction === filter
                ? 'bg-sky-500 text-white shadow-md shadow-sky-500/20'
                : 'bg-slate-900 text-slate-400 hover:text-white border border-slate-800'
            }`}
          >
            {filter.replace(/_/g, ' ')}
          </button>
        ))}
      </div>

      {/* AI Market Expert Recommendations Grid */}
      <div className="space-y-4">
        {filteredAnalyses.length === 0 ? (
          <div className="p-8 rounded-xl bg-slate-900 border border-slate-800 text-center text-slate-400 text-sm">
            No stocks matched the selected filter. Try "ALL" or click "Refresh Scan".
          </div>
        ) : (
          filteredAnalyses.map((item) => (
            <div
              key={item.symbol}
              className="p-5 rounded-xl bg-slate-900/90 border border-slate-800 shadow-md hover:border-slate-700 transition space-y-3"
            >
              {/* Card Top Row */}
              <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-3 border-b border-slate-800/80">
                <div className="flex items-center gap-3">
                  <div>
                    <div className="flex items-center gap-2">
                      <h3 className="text-base font-extrabold text-white font-mono tracking-tight">
                        {item.symbol}
                      </h3>
                      <span className="text-xs text-slate-400">{item.companyName}</span>
                      <span className="px-1.5 py-0.5 rounded text-[10px] font-mono bg-slate-950 text-slate-400 border border-slate-800">
                        NSE
                      </span>
                    </div>
                    <p className="text-xs text-slate-300 font-semibold mt-0.5">
                      LTP: ₹{item.currentPrice.toLocaleString('en-IN', { minimumFractionDigits: 2 })}
                    </p>
                  </div>
                </div>

                <div className="flex items-center gap-3">
                  {getActionBadge(item.action)}
                  <div className="text-right">
                    <p className="text-[10px] text-slate-400 uppercase font-semibold">AI Conviction</p>
                    <p className="text-sm font-extrabold text-purple-400">
                      {Math.round(item.convictionScore * 100)}%
                    </p>
                  </div>
                </div>
              </div>

              {/* Targets, Stop Loss & Quant Levels */}
              <div className="grid grid-cols-2 sm:grid-cols-4 gap-2.5 p-3 rounded-lg bg-slate-950/80 border border-slate-800/80 text-xs">
                <div>
                  <span className="text-[10px] text-slate-500 uppercase font-bold">Suggested Entry</span>
                  <p className="font-bold text-slate-200">₹{item.suggestedPrice.toFixed(2)}</p>
                </div>
                <div>
                  <span className="text-[10px] text-rose-400 uppercase font-bold">Stop Loss</span>
                  <p className="font-bold text-rose-400">₹{item.stopLoss.toFixed(2)}</p>
                </div>
                <div>
                  <span className="text-[10px] text-emerald-400 uppercase font-bold">Target 1</span>
                  <p className="font-bold text-emerald-400">₹{item.target1.toFixed(2)}</p>
                </div>
                <div>
                  <span className="text-[10px] text-teal-400 uppercase font-bold">Target 2</span>
                  <p className="font-bold text-teal-400">₹{item.target2.toFixed(2)}</p>
                </div>
              </div>

              {/* Rationale and Strategy Explanation */}
              <div className="space-y-1.5 text-xs">
                <p className="text-slate-200 font-semibold leading-relaxed">
                  {item.summary}
                </p>
                <p className="text-slate-400 leading-relaxed text-[11px]">
                  💡 <strong>Technical Rationale:</strong> {item.technicalRationale}
                </p>
                {item.riskNote && (
                  <p className="text-amber-400/90 text-[11px]">
                    ⚠️ <strong>Risk Management:</strong> {item.riskNote}
                  </p>
                )}
              </div>

              {/* Bottom Quick Trigger */}
              {(item.action === 'STRONG_BUY' || item.action === 'BUY_THE_DIP') && (
                <div className="pt-2 flex items-center justify-between border-t border-slate-800/60">
                  <span className="text-[11px] text-slate-500 flex items-center gap-1">
                    <Clock size={12} />
                    Will activate 2-second BUY rejection window upon trigger
                  </span>
                  <button
                    onClick={() => handleSimulateBuy(item.symbol)}
                    className="flex items-center gap-1.5 px-3 py-1.5 bg-gradient-to-r from-emerald-600 to-teal-600 hover:from-emerald-500 hover:to-teal-500 text-white rounded-lg text-xs font-bold shadow-md transition"
                  >
                    <Zap size={13} />
                    Trigger Buy Trial
                  </button>
                </div>
              )}
            </div>
          ))
        )}
      </div>
    </div>
  );
};
