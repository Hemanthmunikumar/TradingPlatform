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
  DollarSign, 
  Sliders, 
  X, 
  Check, 
  RotateCcw, 
  Target, 
  BarChart3, 
  Flame, 
  Award,
  Building2,
  ArrowUpRight,
  ArrowDownRight,
  ChevronDown,
  Search,
  AlertCircle
} from 'lucide-react';
import { apiClient } from '../api/client';
import type { 
  ApiResponse, 
  PreMarketScanResponse, 
  PreMarket21EmaSignalDto,
  IndiaOrbHitDto,
  IndiaInsiderDealDto,
  BrokerSyncResponse,
  BrokerHoldingItemDto,
  SpecialMonitorItemDto
} from '../types';

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

export interface AiAdvisorConfig {
  provider: string;
  apiKey?: string;
  hasApiKey: boolean;
  model: string;
  minConfidenceThreshold: number;
  enableDipAccumulation: boolean;
  enableSpecialWatchlist: boolean;
  activeSource: string;
}

export const AiMarketExpertPage: React.FC = () => {
  const [activeStrategyTab, setActiveStrategyTab] = useState<'brokerSync' | 'specialMonitor' | 'premarket' | 'orb' | 'insiders' | 'holdings'>('brokerSync');
  
  // Data States
  const [brokerSyncData, setBrokerSyncData] = useState<BrokerSyncResponse | null>(null);
  const [premarketData, setPremarketData] = useState<PreMarketScanResponse | null>(null);
  const [orbHits, setOrbHits] = useState<IndiaOrbHitDto[]>([]);
  const [insiderDeals, setInsiderDeals] = useState<IndiaInsiderDealDto[]>([]);
  const [analyses, setAnalyses] = useState<AiAnalysisResultDto[]>([]);
  const [watchlist, setWatchlist] = useState<string[]>([]);
  const [lastScannedAt, setLastScannedAt] = useState<string>(new Date().toISOString());
  const [slPercent, setSlPercent] = useState<number>(2.5);
  const [tgtPercent, setTgtPercent] = useState<number>(10.0);

  const formatIstTimestamp = (iso?: string) => {
    const dt = iso ? new Date(iso) : new Date();
    return (
      dt.toLocaleString('en-IN', {
        timeZone: 'Asia/Kolkata',
        day: '2-digit',
        month: 'short',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      }) + ' IST'
    );
  };

  // 4-Stage Skill Inspector Modal State
  const [inspectSymbol, setInspectSymbol] = useState<string | null>(null);
  const [isInspectLoading, setIsInspectLoading] = useState(false);
  const [inspectData, setInspectData] = useState<{
    news?: any;
    analyst?: any;
    entry?: any;
    timemachine?: any;
  } | null>(null);

  const handleInspectSkills = async (symbol: string, entry: number, stop: number, target: number, level?: number) => {
    setInspectSymbol(symbol);
    setIsInspectLoading(true);
    setInspectData(null);
    try {
      const [newsRes, analystRes, entryRes, tmRes] = await Promise.allSettled([
        apiClient.get<ApiResponse<any>>(`/ai/skills/news?symbol=${encodeURIComponent(symbol)}&hours=48`),
        apiClient.get<ApiResponse<any>>(`/ai/skills/analyst?symbol=${encodeURIComponent(symbol)}&quarters=2`),
        apiClient.get<ApiResponse<any>>(`/ai/skills/entry-check?symbol=${encodeURIComponent(symbol)}${level ? `&level=${level}` : ''}`),
        apiClient.get<ApiResponse<any>>(`/ai/skills/timemachine?symbol=${encodeURIComponent(symbol)}&entry=${entry}&stop=${stop}&target=${target}`),
      ]);
      setInspectData({
        news: newsRes.status === 'fulfilled' ? newsRes.value.data?.data : null,
        analyst: analystRes.status === 'fulfilled' ? analystRes.value.data?.data : null,
        entry: entryRes.status === 'fulfilled' ? entryRes.value.data?.data : null,
        timemachine: tmRes.status === 'fulfilled' ? tmRes.value.data?.data : null,
      });
    } finally {
      setIsInspectLoading(false);
    }
  };
  const [specialMonitorItems, setSpecialMonitorItems] = useState<SpecialMonitorItemDto[]>([]);
  
  // Interactive UI States
  const [newSymbol, setNewSymbol] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [isSyncingBroker, setIsSyncingBroker] = useState(false);
  const [isSimulating, setIsSimulating] = useState(false);
  const [simMessage, setSimMessage] = useState<string | null>(null);
  const [filterAction, setFilterAction] = useState<string>('ALL');
  const [approvalDurationSeconds, setApprovalDurationSeconds] = useState<number>(60);
  const [isScannersDropdownOpen, setIsScannersDropdownOpen] = useState(false);
  const [specialMonitorError, setSpecialMonitorError] = useState<string | null>(null);
  const [isAddingToMonitor, setIsAddingToMonitor] = useState(false);

  // Pre-market filter state
  const [minGain, setMinGain] = useState<number>(1.0);
  const [maxGain, setMaxGain] = useState<number>(2.5);
  const [minVolume, setMinVolume] = useState<number>(20000);

  // AI Configuration State
  const [isConfigOpen, setIsConfigOpen] = useState(false);
  const [config, setConfig] = useState<AiAdvisorConfig>({
    provider: 'GeminiPro',
    apiKey: '',
    hasApiKey: false,
    model: 'gemini-1.5-pro',
    minConfidenceThreshold: 0.80,
    enableDipAccumulation: true,
    enableSpecialWatchlist: true,
    activeSource: 'AppSettings'
  });
  const [savingConfig, setSavingConfig] = useState(false);
  const [configSuccessMsg, setConfigSuccessMsg] = useState<string | null>(null);

  const fetchConfig = async () => {
    try {
      const res = await apiClient.get<ApiResponse<AiAdvisorConfig>>('/ai/config');
      if (res.data.success && res.data.data) {
        setConfig(res.data.data);
      }
    } catch (err) {
      console.error('Failed to load AI advisor configuration', err);
    }
  };

  const handleSyncBroker = async () => {
    setIsSyncingBroker(true);
    try {
      const res = await apiClient.get<ApiResponse<BrokerSyncResponse>>('/ai/broker-sync');
      if (res.data.success && res.data.data) {
        setBrokerSyncData(res.data.data);
        setSimMessage(`✅ Synchronized ${res.data.data.holdings.length} holdings and ${res.data.data.orders.length} orders from broker ${res.data.data.brokerName} (${res.data.data.tradingMode} mode).`);
      }
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to fetch broker orders & holdings');
    } finally {
      setIsSyncingBroker(false);
    }
  };

  const fetchData = async () => {
    setIsLoading(true);
    setSimMessage(null);
    try {
      const [syncRes, pmRes, orbRes, insiderRes, overviewRes, watchlistRes, specialDetailsRes, accountsRes] = await Promise.all([
        apiClient.get<ApiResponse<BrokerSyncResponse>>('/ai/broker-sync'),
        apiClient.get<ApiResponse<PreMarketScanResponse>>(`/ai/skills/premarket-21ema?minGain=${minGain}&maxGain=${maxGain}&minVolume=${minVolume}`),
        apiClient.get<ApiResponse<IndiaOrbHitDto[]>>('/ai/skills/orb-breakouts'),
        apiClient.get<ApiResponse<IndiaInsiderDealDto[]>>('/ai/skills/insider-deals?limit=15'),
        apiClient.get<ApiResponse<AiAnalysisResultDto[]>>('/ai/overview'),
        apiClient.get<ApiResponse<string[]>>('/ai/special-monitor'),
        apiClient.get<ApiResponse<SpecialMonitorItemDto[]>>('/ai/special-monitor/details'),
        apiClient.get<ApiResponse<any[]>>('/trading-accounts'),
      ]);

      if (syncRes.data.success && syncRes.data.data) {
        setBrokerSyncData(syncRes.data.data);
      }
      if (pmRes.data.success && pmRes.data.data) {
        setPremarketData(pmRes.data.data);
        if (pmRes.data.data.stopLossPercent) setSlPercent(pmRes.data.data.stopLossPercent);
        if (pmRes.data.data.targetPercent) setTgtPercent(pmRes.data.data.targetPercent);
      }
      setLastScannedAt(new Date().toISOString());
      if (orbRes.data.success && orbRes.data.data) {
        setOrbHits(orbRes.data.data);
      }
      if (insiderRes.data.success && insiderRes.data.data) {
        setInsiderDeals(insiderRes.data.data);
      }
      if (overviewRes.data.success && overviewRes.data.data) {
        setAnalyses(overviewRes.data.data);
      }
      if (watchlistRes.data.success && watchlistRes.data.data) {
        setWatchlist(watchlistRes.data.data);
      }
      if (specialDetailsRes.data.success && specialDetailsRes.data.data) {
        setSpecialMonitorItems(specialDetailsRes.data.data);
      }
      if (accountsRes.data.success && accountsRes.data.data) {
        const active = accountsRes.data.data.find((a: any) => a.isActive) || accountsRes.data.data[0];
        if (active?.configuration?.approvalDurationSeconds) {
          setApprovalDurationSeconds(active.configuration.approvalDurationSeconds);
        }
        if (active?.configuration?.intradayMinVolume) {
          setMinVolume(active.configuration.intradayMinVolume);
        }
      }
    } catch (err: any) {
      console.error('Failed to load market strategy scans', err);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
    fetchConfig();
  }, []);

  const handleSaveConfig = async (e: React.FormEvent) => {
    e.preventDefault();
    setSavingConfig(true);
    setConfigSuccessMsg(null);
    try {
      const res = await apiClient.post<ApiResponse<AiAdvisorConfig>>('/ai/config', config);
      if (res.data.success && res.data.data) {
        setConfig(res.data.data);
        setConfigSuccessMsg('Settings saved! Using ' + res.data.data.provider + ' (' + res.data.data.activeSource + ')');
        setTimeout(() => {
          setIsConfigOpen(false);
          setConfigSuccessMsg(null);
        }, 1500);
        await fetchData();
      }
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to update AI configuration');
    } finally {
      setSavingConfig(false);
    }
  };

  const handleResetConfig = async () => {
    if (!confirm('Revert AI Advisor settings to appsettings.json defaults?')) return;
    setSavingConfig(true);
    try {
      const res = await apiClient.post<ApiResponse<AiAdvisorConfig>>('/ai/config/reset');
      if (res.data.success && res.data.data) {
        setConfig(res.data.data);
        setConfigSuccessMsg('Restored to appsettings.json defaults!');
        setTimeout(() => {
          setIsConfigOpen(false);
          setConfigSuccessMsg(null);
        }, 1500);
        await fetchData();
      }
    } catch (err: any) {
      alert('Failed to reset configuration');
    } finally {
      setSavingConfig(false);
    }
  };

  const handleAddWatchlist = async (symbolToAdd?: string) => {
    const sym = (symbolToAdd || newSymbol).trim().toUpperCase();
    if (!sym) return;

    setIsAddingToMonitor(true);
    setSpecialMonitorError(null);
    try {
      const res = await apiClient.post<ApiResponse<boolean>>('/ai/special-monitor/add', {
        symbol: sym,
      });
      if (res.data.success) {
        setNewSymbol('');
        await fetchData();
        setSimMessage(`✅ Successfully verified & added ${sym} to Special Monitor.`);
      } else {
        setSpecialMonitorError(res.data.message || `Failed to add ${sym}`);
      }
    } catch (err: any) {
      setSpecialMonitorError(
        err.response?.data?.message || 
        err.response?.data?.errors?.[0] || 
        `Stock symbol '${sym}' not found on NSE or BSE. Please verify the stock ticker spelling.`
      );
    } finally {
      setIsAddingToMonitor(false);
    }
  };

  const handleRemoveWatchlist = async (symbol: string) => {
    try {
      await apiClient.delete(`/ai/special-monitor/${symbol}`);
      await fetchData();
      setSimMessage(`Removed ${symbol} from Special Monitor.`);
    } catch (err: any) {
      alert('Failed to remove symbol');
    }
  };

  const handleTriggerSpecialMonitorBuy = async (item: SpecialMonitorItemDto) => {
    setIsSimulating(true);
    setSimMessage(null);
    try {
      const res = await apiClient.post<ApiResponse<any>>('/ai/trigger-premarket-order', {
        symbol: item.symbol,
        price: item.lastPrice,
        stopLoss: item.stopLoss,
        targetPrice: item.targetPrice,
        suggestedAtmOption: item.suggestedAtmOption,
        quantity: 10
      });
      if (res.data.success) {
        if (res.data.data) window.dispatchEvent(new CustomEvent('approval-created', { detail: res.data.data }));
        setSimMessage(`⚡ ${approvalDurationSeconds}s BUY Window Triggered for 10 shares of ${item.symbol} @ ₹${item.lastPrice}! ATM Option Strike: ${item.suggestedAtmOption}. Watch countdown toast.`);
      }
    } catch (err: any) {
      setSimMessage(`Failed to trigger buy: ${err.response?.data?.message || err.message}`);
    } finally {
      setIsSimulating(false);
    }
  };

  const handleHoldingAction = async (holding: BrokerHoldingItemDto, action: 'ADD_QUANTITY' | 'BOOK_PROFIT' | 'EXIT') => {
    setIsSimulating(true);
    setSimMessage(null);
    const qty = action === 'ADD_QUANTITY' ? holding.suggestedAddQuantity : Math.max(1, Math.floor(holding.quantity / 2));
    try {
      const res = await apiClient.post<ApiResponse<any>>('/ai/trigger-holding-action', {
        positionId: holding.positionId,
        action,
        quantity: qty
      });
      if (res.data.success) {
        if (res.data.data) window.dispatchEvent(new CustomEvent('approval-created', { detail: res.data.data }));
        setSimMessage(`⚡ ${approvalDurationSeconds}s ${res.data.data?.side} Window Triggered for ${res.data.data?.quantity} shares of ${holding.symbol} @ ₹${res.data.data?.indicativePrice}! Watch countdown toast.`);
        await handleSyncBroker();
      }
    } catch (err: any) {
      setSimMessage(`Action failed: ${err.response?.data?.message || err.message}`);
    } finally {
      setIsSimulating(false);
    }
  };

  const handleTriggerPreMarketBuy = async (signal: PreMarket21EmaSignalDto) => {
    setIsSimulating(true);
    setSimMessage(null);
    try {
      const res = await apiClient.post<ApiResponse<any>>('/ai/trigger-premarket-order', {
        symbol: signal.symbol,
        price: signal.spotPrice,
        stopLoss: signal.stopLoss,
        targetPrice: signal.targetPrice,
        suggestedAtmOption: signal.suggestedAtmOption,
        quantity: 10
      });
      if (res.data.success) {
        if (res.data.data) window.dispatchEvent(new CustomEvent('approval-created', { detail: res.data.data }));
        setSimMessage(`⚡ ${approvalDurationSeconds}s BUY Window Triggered for 10 shares of ${signal.symbol} @ ₹${signal.spotPrice}! ATM Option Strike: ${signal.suggestedAtmOption}. Observe countdown toast.`);
      }
    } catch (err: any) {
      setSimMessage(`Failed to trigger buy: ${err.response?.data?.message || err.message}`);
    } finally {
      setIsSimulating(false);
    }
  };

  const handleTriggerOrbBuy = async (hit: IndiaOrbHitDto) => {
    setIsSimulating(true);
    setSimMessage(null);
    const slPrice = Number((hit.price * (1 - slPercent / 100)).toFixed(2));
    const tgtPrice = Number((hit.price * (1 + tgtPercent / 100)).toFixed(2));
    try {
      const res = await apiClient.post<ApiResponse<any>>('/ai/trigger-premarket-order', {
        symbol: hit.symbol,
        price: hit.price,
        stopLoss: slPrice,
        targetPrice: tgtPrice,
        suggestedAtmOption: 'ORB 15m Breakout',
        quantity: 10
      });
      if (res.data.success) {
        if (res.data.data) window.dispatchEvent(new CustomEvent('approval-created', { detail: res.data.data }));
        setSimMessage(`⚡ ${approvalDurationSeconds}s ORB BUY Window Triggered for 10 shares of ${hit.symbol} @ ₹${hit.price}! Watch countdown toast.`);
      }
    } catch (err: any) {
      setSimMessage(`Failed to trigger ORB buy: ${err.response?.data?.message || err.message}`);
    } finally {
      setIsSimulating(false);
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
        if (res.data.data) window.dispatchEvent(new CustomEvent('approval-created', { detail: res.data.data }));
        setSimMessage(`⚡ ${approvalDurationSeconds}s BUY Window Triggered for 10 shares of ${symbol} @ ₹${res.data.data?.indicativePrice}! Watch the persistent countdown toast.`);
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
    <div className="space-y-6 max-w-7xl mx-auto pb-12">
      {/* Header */}
      <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-4">
        <div>
          <div className="flex items-center gap-2">
            <span className="p-1.5 rounded-lg bg-gradient-to-tr from-sky-500 to-indigo-600 text-white shadow-md shadow-sky-500/20">
              <Sparkles size={20} />
            </span>
            <h1 className="text-2xl sm:text-3xl font-extrabold text-white tracking-tight">
              AI Market Co-Pilot &amp; Broker Synchronizer
            </h1>
          </div>
          <p className="text-xs sm:text-sm text-slate-400 mt-1">
            Real-time monitoring of <strong>Zerodha broker holdings &amp; orders</strong>, Pre-Market 21-EMA Pullbacks, and AI Dip Accumulation
          </p>
        </div>

        {/* Action Controls */}
        <div className="flex flex-wrap items-center gap-2">
          {/* AI Settings Modal Toggle */}
          <button
            onClick={() => setIsConfigOpen(true)}
            className="flex items-center gap-1.5 px-3 py-2 bg-indigo-600/20 hover:bg-indigo-600/30 text-indigo-300 rounded-lg text-xs font-semibold border border-indigo-500/30 transition shadow-sm"
          >
            <Sliders size={14} />
            AI Settings
            <span className="px-1.5 py-0.5 rounded text-[10px] font-bold bg-indigo-500/30 text-indigo-200">
              {config.provider}
            </span>
          </button>

          <button
            onClick={fetchData}
            disabled={isLoading}
            className="flex items-center gap-1.5 px-3 py-2 bg-slate-800 hover:bg-slate-700 text-slate-300 rounded-lg text-xs font-semibold border border-slate-700 transition"
          >
            <RefreshCw size={14} className={isLoading ? 'animate-spin' : ''} />
            Refresh Scans
          </button>

          {/* Sync Broker Button */}
          <button
            onClick={handleSyncBroker}
            disabled={isSyncingBroker}
            className="flex items-center gap-1.5 px-3.5 py-2 bg-emerald-600/20 hover:bg-emerald-600/30 text-emerald-300 rounded-lg text-xs font-bold border border-emerald-500/30 transition shadow-sm"
          >
            <Building2 size={14} className={isSyncingBroker ? 'animate-spin' : ''} />
            {isSyncingBroker ? 'Syncing Zerodha...' : 'Sync Broker (Orders & Holdings)'}
          </button>

          {/* 1-Click Interactive Test Trial Button (Paper / Mock only) */}
          {brokerSyncData?.tradingMode !== 'Live' && (
            <button
              onClick={() => handleSimulateBuy('TATAMOTORS')}
              disabled={isSimulating}
              className="flex items-center gap-2 px-4 py-2 bg-gradient-to-r from-emerald-500 to-teal-600 hover:from-emerald-400 hover:to-teal-500 text-white rounded-lg text-xs font-bold shadow-lg shadow-emerald-500/20 transition active:scale-95"
              title={`Triggers instant ${approvalDurationSeconds}-second BUY rejection window countdown`}
            >
              <Zap size={15} className={isSimulating ? 'animate-spin' : ''} />
              ⚡ Test {approvalDurationSeconds}s BUY Window
            </button>
          )}
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
            <span>Broker Holdings (Zerodha)</span>
            <Building2 size={15} className="text-emerald-400" />
          </div>
          <p className="text-2xl font-extrabold text-white">
            {brokerSyncData?.holdings.length ?? 0} Stocks
          </p>
          <p className="text-[11px] text-slate-500">
            Mode: <strong className="text-emerald-400">{brokerSyncData?.tradingMode ?? 'Paper'}</strong>
          </p>
        </div>

        <div className="p-4 rounded-xl bg-slate-900 border border-slate-800 space-y-1">
          <div className="flex items-center justify-between text-slate-400 text-xs font-medium">
            <span>Pre-Market 21-EMA Signals</span>
            <Flame size={15} className="text-amber-400" />
          </div>
          <p className="text-2xl font-extrabold text-amber-400">
            {premarketData?.signals.length ?? 0} Opportunities
          </p>
          <p className="text-[11px] text-slate-500">1.0% - 2.5% Gainers at 21-EMA</p>
        </div>

        <div className="p-4 rounded-xl bg-slate-900 border border-slate-800 space-y-1">
          <div className="flex items-center justify-between text-slate-400 text-xs font-medium">
            <span>Broker Orders Tracked</span>
            <CheckCircle2 size={15} className="text-sky-400" />
          </div>
          <p className="text-2xl font-extrabold text-sky-400">
            {brokerSyncData?.orders.length ?? 0} Orders
          </p>
          <p className="text-[11px] text-slate-500">Filled &amp; Pending executions</p>
        </div>

        <div className="p-4 rounded-xl bg-slate-900 border border-slate-800 space-y-1">
          <div className="flex items-center justify-between text-slate-400 text-xs font-medium">
            <span>Safety Gate</span>
            <ShieldCheck size={15} className="text-sky-400" />
          </div>
          <p className="text-2xl font-extrabold text-sky-400">19-Point Guard</p>
          <p className="text-[11px] text-slate-500">{approvalDurationSeconds}s Rejection Window active</p>
        </div>
      </div>

      {/* Sleek, Non-Scrolling Segmented Navigation */}
      <div className="relative z-40 flex flex-wrap items-center justify-between gap-3 p-1.5 bg-slate-900/90 rounded-2xl border border-slate-800 shadow-md backdrop-blur-md">
        <div className="flex flex-wrap items-center gap-1.5">
          {/* Tab 1: Broker Portfolio */}
          <button
            onClick={() => { setActiveStrategyTab('brokerSync'); setIsScannersDropdownOpen(false); }}
            className={`px-4 py-2 rounded-xl text-xs sm:text-sm font-bold transition flex items-center gap-2 ${
              activeStrategyTab === 'brokerSync'
                ? 'bg-emerald-500/20 text-emerald-300 border border-emerald-500/40 shadow-sm'
                : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/60'
            }`}
          >
            <Building2 size={16} className={activeStrategyTab === 'brokerSync' ? 'text-emerald-400' : 'text-slate-500'} />
            <span>Broker Portfolio (Zerodha)</span>
            <span className="px-1.5 py-0.5 rounded-full text-[10px] bg-emerald-950 text-emerald-400 font-mono border border-emerald-800/60">
              {brokerSyncData?.holdings.length ?? 0}
            </span>
          </button>

          {/* Tab 2: Special Monitor (Watchlist) */}
          <button
            onClick={() => { setActiveStrategyTab('specialMonitor'); setIsScannersDropdownOpen(false); }}
            className={`px-4 py-2 rounded-xl text-xs sm:text-sm font-bold transition flex items-center gap-2 ${
              activeStrategyTab === 'specialMonitor'
                ? 'bg-sky-500/20 text-sky-300 border border-sky-500/40 shadow-sm'
                : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/60'
            }`}
          >
            <Eye size={16} className={activeStrategyTab === 'specialMonitor' ? 'text-sky-400' : 'text-slate-500'} />
            <span>Special Monitor &amp; Watchlist</span>
            <span className="px-1.5 py-0.5 rounded-full text-[10px] bg-sky-950 text-sky-400 font-mono border border-sky-800/60">
              {specialMonitorItems.length}
            </span>
          </button>

          {/* Tab 3: Market Scanners with Dropdown */}
          <div className="relative">
            <button
              onClick={() => setIsScannersDropdownOpen(!isScannersDropdownOpen)}
              className={`px-4 py-2 rounded-xl text-xs sm:text-sm font-bold transition flex items-center gap-2 ${
                ['premarket', 'orb', 'insiders', 'holdings'].includes(activeStrategyTab)
                  ? 'bg-amber-500/20 text-amber-300 border border-amber-500/40 shadow-sm'
                  : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/60'
              }`}
            >
              <Zap size={16} className={['premarket', 'orb', 'insiders', 'holdings'].includes(activeStrategyTab) ? 'text-amber-400' : 'text-slate-500'} />
              <span>
                {activeStrategyTab === 'premarket' && 'Scanner: Pre-Market 21-EMA'}
                {activeStrategyTab === 'orb' && 'Scanner: 15m ORB Breakouts'}
                {activeStrategyTab === 'insiders' && 'Scanner: SEBI Whale Deals'}
                {activeStrategyTab === 'holdings' && 'Scanner: AI Multi-Model'}
                {!['premarket', 'orb', 'insiders', 'holdings'].includes(activeStrategyTab) && 'Market Scanners'}
              </span>
              <ChevronDown size={14} className={`transition-transform duration-200 ${isScannersDropdownOpen ? 'rotate-180' : ''}`} />
            </button>

            {/* Invisible backdrop for click-outside */}
            {isScannersDropdownOpen && (
              <div 
                className="fixed inset-0 z-40 cursor-default" 
                onClick={() => setIsScannersDropdownOpen(false)} 
              />
            )}

            {/* Flyout Menu with High z-index & Scroll Safety */}
            {isScannersDropdownOpen && (
              <div 
                className="absolute left-0 mt-2 w-80 max-h-[380px] overflow-y-auto rounded-2xl bg-slate-900 border border-slate-700 shadow-2xl p-2 z-50 backdrop-blur-xl animate-in fade-in slide-in-from-top-2 duration-150"
              >
                <div className="px-3 py-1.5 text-[10px] font-bold uppercase tracking-wider text-slate-400 border-b border-slate-800 mb-1">
                  Select Active Market Scanner
                </div>

                <button
                  onClick={() => { setActiveStrategyTab('premarket'); setIsScannersDropdownOpen(false); }}
                  className={`w-full text-left p-2.5 rounded-xl flex items-center justify-between text-xs transition ${
                    activeStrategyTab === 'premarket' ? 'bg-amber-500/20 text-amber-300 font-bold border border-amber-500/30' : 'text-slate-300 hover:bg-slate-800/80'
                  }`}
                >
                  <div className="flex items-center gap-2.5">
                    <Flame size={16} className="text-amber-400 shrink-0" />
                    <div>
                      <p className="font-semibold text-white">Pre-Market 21-EMA</p>
                      <p className="text-[10px] text-slate-400">1-2.5% Gainers + ATM Options</p>
                    </div>
                  </div>
                  <span className="font-mono text-[10px] px-1.5 py-0.5 rounded bg-slate-950 text-amber-400 border border-slate-800">
                    {premarketData?.signals.length ?? 0}
                  </span>
                </button>

                <button
                  onClick={() => { setActiveStrategyTab('orb'); setIsScannersDropdownOpen(false); }}
                  className={`w-full text-left p-2.5 rounded-xl flex items-center justify-between text-xs transition ${
                    activeStrategyTab === 'orb' ? 'bg-sky-500/20 text-sky-300 font-bold border border-sky-500/30' : 'text-slate-300 hover:bg-slate-800/80'
                  }`}
                >
                  <div className="flex items-center gap-2.5">
                    <BarChart3 size={16} className="text-sky-400 shrink-0" />
                    <div>
                      <p className="font-semibold text-white">15m ORB Breakouts</p>
                      <p className="text-[10px] text-slate-400">Opening-Range NSE Hits</p>
                    </div>
                  </div>
                  <span className="font-mono text-[10px] px-1.5 py-0.5 rounded bg-slate-950 text-sky-400 border border-slate-800">
                    {orbHits.length}
                  </span>
                </button>

                <button
                  onClick={() => { setActiveStrategyTab('insiders'); setIsScannersDropdownOpen(false); }}
                  className={`w-full text-left p-2.5 rounded-xl flex items-center justify-between text-xs transition ${
                    activeStrategyTab === 'insiders' ? 'bg-purple-500/20 text-purple-300 font-bold border border-purple-500/30' : 'text-slate-300 hover:bg-slate-800/80'
                  }`}
                >
                  <div className="flex items-center gap-2.5">
                    <Award size={16} className="text-purple-400 shrink-0" />
                    <div>
                      <p className="font-semibold text-white">SEBI Whale Deals</p>
                      <p className="text-[10px] text-slate-400">Bulk &amp; Insider Block Trades</p>
                    </div>
                  </div>
                  <span className="font-mono text-[10px] px-1.5 py-0.5 rounded bg-slate-950 text-purple-400 border border-slate-800">
                    {insiderDeals.length}
                  </span>
                </button>

                <button
                  onClick={() => { setActiveStrategyTab('holdings'); setIsScannersDropdownOpen(false); }}
                  className={`w-full text-left p-2.5 rounded-xl flex items-center justify-between text-xs transition ${
                    activeStrategyTab === 'holdings' ? 'bg-indigo-500/20 text-indigo-300 font-bold border border-indigo-500/30' : 'text-slate-300 hover:bg-slate-800/80'
                  }`}
                >
                  <div className="flex items-center gap-2.5">
                    <Sparkles size={16} className="text-indigo-400 shrink-0" />
                    <div>
                      <p className="font-semibold text-white">AI Multi-Model Consensus</p>
                      <p className="text-[10px] text-slate-400">Gemini + DeepSeek Analysis</p>
                    </div>
                  </div>
                  <span className="font-mono text-[10px] px-1.5 py-0.5 rounded bg-slate-950 text-indigo-400 border border-slate-800">
                    {analyses.length}
                  </span>
                </button>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Sub-Navigation Pill Bar for Scanners (Zero-Overlap & Instant Switching) */}
      {['premarket', 'orb', 'insiders', 'holdings'].includes(activeStrategyTab) && (
        <div className="flex flex-wrap items-center gap-2 p-2 bg-slate-900/70 rounded-xl border border-slate-800 shadow-sm animate-in fade-in duration-150">
          <span className="text-[11px] font-bold uppercase tracking-wider text-slate-400 pl-2">
            Scanners:
          </span>
          <button
            onClick={() => setActiveStrategyTab('premarket')}
            className={`px-3 py-1.5 rounded-lg text-xs font-semibold flex items-center gap-1.5 transition ${
              activeStrategyTab === 'premarket'
                ? 'bg-amber-500/20 text-amber-300 border border-amber-500/40 shadow-sm'
                : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800'
            }`}
          >
            <Flame size={13} className="text-amber-400" />
            <span>Pre-Market 21-EMA</span>
            <span className="font-mono text-[10px] px-1 rounded bg-slate-950 text-amber-400 border border-slate-800">
              {premarketData?.signals.length ?? 0}
            </span>
          </button>

          <button
            onClick={() => setActiveStrategyTab('orb')}
            className={`px-3 py-1.5 rounded-lg text-xs font-semibold flex items-center gap-1.5 transition ${
              activeStrategyTab === 'orb'
                ? 'bg-sky-500/20 text-sky-300 border border-sky-500/40 shadow-sm'
                : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800'
            }`}
          >
            <BarChart3 size={13} className="text-sky-400" />
            <span>15m ORB Breakouts</span>
            <span className="font-mono text-[10px] px-1 rounded bg-slate-950 text-sky-400 border border-slate-800">
              {orbHits.length}
            </span>
          </button>

          <button
            onClick={() => setActiveStrategyTab('insiders')}
            className={`px-3 py-1.5 rounded-lg text-xs font-semibold flex items-center gap-1.5 transition ${
              activeStrategyTab === 'insiders'
                ? 'bg-purple-500/20 text-purple-300 border border-purple-500/40 shadow-sm'
                : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800'
            }`}
          >
            <Award size={13} className="text-purple-400" />
            <span>SEBI Whale Deals</span>
            <span className="font-mono text-[10px] px-1 rounded bg-slate-950 text-purple-400 border border-slate-800">
              {insiderDeals.length}
            </span>
          </button>

          <button
            onClick={() => setActiveStrategyTab('holdings')}
            className={`px-3 py-1.5 rounded-lg text-xs font-semibold flex items-center gap-1.5 transition ${
              activeStrategyTab === 'holdings'
                ? 'bg-indigo-500/20 text-indigo-300 border border-indigo-500/40 shadow-sm'
                : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800'
            }`}
          >
            <Sparkles size={13} className="text-indigo-400" />
            <span>AI Multi-Model Consensus</span>
            <span className="font-mono text-[10px] px-1 rounded bg-slate-950 text-indigo-400 border border-slate-800">
              {analyses.length}
            </span>
          </button>
        </div>
      )}

      {/* TAB 0: BROKER ORDERS & HOLDINGS (ZERODHA) */}
      {activeStrategyTab === 'brokerSync' && (
        <div className="space-y-6">
          {/* Sync Status Banner */}
          <div className="p-4 rounded-xl bg-slate-900 border border-slate-800 flex flex-col sm:flex-row sm:items-center justify-between gap-3 shadow-sm">
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-lg bg-emerald-500/20 text-emerald-400 border border-emerald-500/30">
                <Building2 size={20} />
              </div>
              <div>
                <div className="flex items-center gap-2">
                  <h3 className="text-sm font-extrabold text-white">
                    Broker: {brokerSyncData?.brokerName ?? 'Zerodha Kite'}
                  </h3>
                  <span className={`px-2 py-0.5 rounded-full text-[10px] font-bold ${
                    brokerSyncData?.isPaperTrading
                      ? 'bg-emerald-500/20 text-emerald-300 border border-emerald-500/40'
                      : 'bg-rose-500/20 text-rose-300 border border-rose-500/40'
                  }`}>
                    {brokerSyncData?.isPaperTrading ? 'Paper Trading (Zero Risk Simulation)' : 'Live Trading'}
                  </span>
                </div>
                <p className="text-xs text-slate-400 mt-0.5">
                  Account: <strong>{brokerSyncData?.accountName}</strong> • Available Capital: <strong>₹{brokerSyncData?.availableFunds?.toLocaleString('en-IN', { minimumFractionDigits: 2 })}</strong>
                </p>
              </div>
            </div>

            <button
              onClick={handleSyncBroker}
              disabled={isSyncingBroker}
              className="flex items-center gap-1.5 px-4 py-2 bg-emerald-600 hover:bg-emerald-500 text-white rounded-lg text-xs font-bold shadow-md transition self-start sm:self-center"
            >
              <RefreshCw size={14} className={isSyncingBroker ? 'animate-spin' : ''} />
              {isSyncingBroker ? 'Syncing...' : '🔄 Refresh from Broker'}
            </button>
          </div>

          {/* Section A: Existing Broker Holdings with AI Recommendations */}
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <div>
                <h3 className="text-sm font-bold text-white uppercase tracking-wider flex items-center gap-2">
                  <ShieldCheck size={16} className="text-emerald-400" />
                  Existing Broker Holdings &amp; AI Diagnostic
                </h3>
                <p className="text-xs text-slate-400">
                  Continuous monitoring of your existing stocks. If a stock is dipping to key 21-EMA support, AI suggests adding quantity; if target is reached, AI suggests profit booking.
                </p>
              </div>
              <span className="text-xs font-mono font-bold text-slate-400">
                {brokerSyncData?.holdings.length ?? 0} Holdings Active
              </span>
            </div>

            {(!brokerSyncData || brokerSyncData.holdings.length === 0) ? (
              <div className="p-8 rounded-xl bg-slate-900 border border-slate-800 text-center text-slate-400 text-sm">
                No active holdings found. Click "Refresh from Broker" to load your holdings.
              </div>
            ) : (
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                {brokerSyncData.holdings.map((h) => (
                  <div
                    key={h.positionId}
                    className="p-5 rounded-xl bg-slate-900 border border-slate-800 hover:border-slate-700 shadow-md transition space-y-3 flex flex-col justify-between"
                  >
                    <div>
                      {/* Top Row: Symbol & Verdict */}
                      <div className="flex items-start justify-between pb-3 border-b border-slate-800">
                        <div>
                          <div className="flex items-center gap-1.5">
                            <h4 className="text-base font-extrabold text-white font-mono">{h.symbol}</h4>
                            <span className="px-1.5 py-0.5 rounded text-[10px] font-mono bg-slate-950 text-slate-400 border border-slate-800">
                              {h.productType}
                            </span>
                          </div>
                          <span className="text-xs text-slate-400 truncate block max-w-[180px]">{h.companyName}</span>
                        </div>

                        {/* Verdict Badge */}
                        <div className="text-right">
                          <span className={`px-2 py-0.5 rounded-full text-[10px] font-extrabold border ${
                            h.aiVerdict === 'DIP_ACCUMULATION'
                              ? 'bg-sky-500/20 text-sky-300 border-sky-500/40 animate-pulse'
                              : h.aiVerdict === 'BOOK_PROFIT'
                              ? 'bg-amber-500/20 text-amber-300 border-amber-500/40'
                              : h.aiVerdict === 'EXIT_STOP_LOSS'
                              ? 'bg-rose-500/20 text-rose-300 border-rose-500/40'
                              : 'bg-slate-800 text-slate-300 border-slate-700'
                          }`}>
                            {h.aiVerdict.replace(/_/g, ' ')}
                          </span>
                          <span className="block text-[10px] text-purple-400 font-mono mt-0.5">
                            {Math.round(h.convictionScore * 100)}% Conviction
                          </span>
                        </div>
                      </div>

                      {/* Position Numbers Grid */}
                      <div className="grid grid-cols-2 gap-2 p-3 my-3 rounded-lg bg-slate-950 border border-slate-800/80 text-xs">
                        <div>
                          <span className="text-[10px] text-slate-500 block uppercase font-bold">Qty &amp; Avg Price</span>
                          <span className="font-mono font-bold text-slate-200">{h.quantity} @ ₹{h.averageBuyPrice.toFixed(2)}</span>
                        </div>
                        <div>
                          <span className="text-[10px] text-slate-500 block uppercase font-bold">Current LTP</span>
                          <span className="font-mono font-bold text-white">₹{h.currentPrice.toFixed(2)}</span>
                        </div>
                        <div>
                          <span className="text-[10px] text-slate-500 block uppercase font-bold">Total Value</span>
                          <span className="font-mono font-bold text-slate-300">₹{h.totalValue.toLocaleString('en-IN', { minimumFractionDigits: 2 })}</span>
                        </div>
                        <div>
                          <span className="text-[10px] text-slate-500 block uppercase font-bold">Unrealized P&amp;L</span>
                          <span className={`font-mono font-extrabold flex items-center gap-0.5 ${
                            h.unrealizedPnL >= 0 ? 'text-emerald-400' : 'text-rose-400'
                          }`}>
                            {h.unrealizedPnL >= 0 ? <ArrowUpRight size={13} /> : <ArrowDownRight size={13} />}
                            {h.unrealizedPnL >= 0 ? '+' : ''}₹{h.unrealizedPnL.toFixed(2)} ({h.unrealizedPnLPercent}%)
                          </span>
                        </div>
                      </div>

                      {/* AI Diagnostic Commentary */}
                      <div className="p-2.5 rounded-lg bg-slate-950/60 border border-slate-800/60 text-xs text-slate-300 leading-relaxed mb-3">
                        <p>{h.aiRecommendationSummary}</p>
                      </div>
                    </div>

                    {/* Bottom Action Controls */}
                    <div className="pt-2 border-t border-slate-800 flex items-center justify-between gap-2">
                      {h.aiVerdict === 'DIP_ACCUMULATION' ? (
                        <button
                          onClick={() => handleHoldingAction(h, 'ADD_QUANTITY')}
                          disabled={isSimulating}
                          className="w-full flex items-center justify-center gap-1.5 py-2 px-3 bg-gradient-to-r from-sky-600 to-emerald-600 hover:from-sky-500 hover:to-emerald-500 text-white rounded-lg text-xs font-bold shadow-md transition"
                        >
                          <Plus size={13} />
                          Add +{h.suggestedAddQuantity} Shares (Average Down)
                        </button>
                      ) : h.aiVerdict === 'BOOK_PROFIT' ? (
                        <button
                          onClick={() => handleHoldingAction(h, 'BOOK_PROFIT')}
                          disabled={isSimulating}
                          className="w-full flex items-center justify-center gap-1.5 py-2 px-3 bg-gradient-to-r from-amber-600 to-orange-600 hover:from-amber-500 hover:to-orange-500 text-white rounded-lg text-xs font-bold shadow-md transition"
                        >
                          <DollarSign size={13} />
                          Book 50% Profit (Sell {Math.floor(h.quantity / 2)} Qty)
                        </button>
                      ) : (
                        <div className="w-full flex items-center justify-between">
                          <button
                            onClick={() => handleHoldingAction(h, 'ADD_QUANTITY')}
                            disabled={isSimulating}
                            className="px-3 py-1.5 bg-slate-800 hover:bg-slate-700 text-slate-200 rounded-lg text-xs font-semibold"
                          >
                            + Buy More
                          </button>
                          <button
                            onClick={() => handleHoldingAction(h, 'EXIT')}
                            disabled={isSimulating}
                            className="px-3 py-1.5 bg-rose-950/60 hover:bg-rose-900/60 text-rose-300 border border-rose-800/40 rounded-lg text-xs font-semibold"
                          >
                            Exit / Sell
                          </button>
                        </div>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Section B: Existing Orders from Broker */}
          <div className="space-y-4 pt-4 border-t border-slate-800">
            <div className="flex items-center justify-between">
              <div>
                <h3 className="text-sm font-bold text-white uppercase tracking-wider flex items-center gap-2">
                  <Clock size={16} className="text-sky-400" />
                  Existing Broker Orders Log
                </h3>
                <p className="text-xs text-slate-400">
                  Actual order history received from broker Kite / execution engine
                </p>
              </div>
              <span className="text-xs font-mono text-slate-400">
                {brokerSyncData?.orders.length ?? 0} Orders Recorded
              </span>
            </div>

            {(!brokerSyncData || brokerSyncData.orders.length === 0) ? (
              <div className="p-8 rounded-xl bg-slate-900 border border-slate-800 text-center text-slate-400 text-sm">
                No orders recorded yet. Place an order or trigger an AI setup to see it logged here.
              </div>
            ) : (
              <div className="overflow-x-auto rounded-xl border border-slate-800">
                <table className="w-full text-xs text-left">
                  <thead className="bg-slate-950 text-slate-400 uppercase text-[10px] font-semibold">
                    <tr>
                      <th className="px-4 py-3">Time</th>
                      <th className="px-4 py-3">Broker Order ID</th>
                      <th className="px-4 py-3">Symbol</th>
                      <th className="px-4 py-3">Side</th>
                      <th className="px-4 py-3">Type / Product</th>
                      <th className="px-4 py-3 text-right">Qty / Filled</th>
                      <th className="px-4 py-3 text-right">Price</th>
                      <th className="px-4 py-3">Status</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-800 bg-slate-900">
                    {brokerSyncData.orders.map((o) => (
                      <tr key={o.orderId} className="hover:bg-slate-800/50">
                        <td className="px-4 py-2.5 font-mono text-slate-400">
                          {new Date(o.createdAtUtc).toLocaleTimeString()}
                        </td>
                        <td className="px-4 py-2.5 font-mono text-slate-300">
                          {o.brokerOrderId ?? o.orderId.substring(0, 8)}
                        </td>
                        <td className="px-4 py-2.5 font-mono font-bold text-white">{o.symbol}</td>
                        <td className="px-4 py-2.5">
                          <span className={`px-2 py-0.5 rounded text-[10px] font-bold ${
                            o.side === 'Buy' ? 'bg-emerald-500/20 text-emerald-300' : 'bg-rose-500/20 text-rose-300'
                          }`}>
                            {o.side.toUpperCase()}
                          </span>
                        </td>
                        <td className="px-4 py-2.5 text-slate-300">
                          {o.orderType} • {o.productType}
                        </td>
                        <td className="px-4 py-2.5 text-right font-mono text-slate-200">
                          {o.filledQuantity} / {o.quantity}
                        </td>
                        <td className="px-4 py-2.5 text-right font-mono text-slate-200">
                          ₹{o.price > 0 ? o.price.toFixed(2) : o.averagePrice.toFixed(2)}
                        </td>
                        <td className="px-4 py-2.5">
                          <span className={`px-2 py-0.5 rounded text-[10px] font-bold ${
                            o.status === 'Filled'
                              ? 'bg-emerald-500/20 text-emerald-300 border border-emerald-500/30'
                              : o.status === 'Rejected'
                              ? 'bg-rose-500/20 text-rose-300 border border-rose-500/30'
                              : 'bg-amber-500/20 text-amber-300 border border-amber-500/30'
                          }`}>
                            {o.status}
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
      )}

      {/* TAB: SPECIAL MONITOR & WATCHLIST (DEDICATED VIEW) */}
      {activeStrategyTab === 'specialMonitor' && (
        <div className="space-y-6">
          {/* Header & Add Stock Card with Live Exchange Validation */}
          <div className="p-5 rounded-2xl bg-slate-900 border border-slate-800 shadow-md space-y-4">
            <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
              <div>
                <div className="flex items-center gap-2">
                  <span className="p-1.5 rounded-lg bg-sky-500/20 text-sky-400 border border-sky-500/30">
                    <Eye size={18} />
                  </span>
                  <h2 className="text-lg font-bold text-white tracking-tight">
                    Special Monitor &amp; Priority Watchlist
                  </h2>
                </div>
                <p className="text-xs text-slate-400 mt-1">
                  Track custom NSE/BSE stocks with live 21-EMA pullback proximity, ATM Option Strikes, AI Buy/Sell verdicts, and 1-click execution.
                </p>
              </div>

              {/* Add Symbol Input with Live Exchange Validation */}
              <form onSubmit={(e) => { e.preventDefault(); handleAddWatchlist(); }} className="flex items-center gap-2">
                <div className="relative">
                  <input
                    type="text"
                    placeholder="e.g. ZOMATO, BEL, HAL, TCS"
                    value={newSymbol}
                    onChange={(e) => { setNewSymbol(e.target.value.toUpperCase()); setSpecialMonitorError(null); }}
                    className="px-3.5 py-2 pl-8 bg-slate-950 border border-slate-700 rounded-xl text-slate-100 text-xs uppercase focus:outline-none focus:ring-2 focus:ring-sky-500 font-mono w-56 placeholder:normal-case placeholder:font-sans"
                  />
                  <Search size={14} className="absolute left-2.5 top-2.5 text-slate-500" />
                </div>
                <button
                  type="submit"
                  disabled={isAddingToMonitor || !newSymbol.trim()}
                  className="flex items-center gap-1.5 px-4 py-2 bg-gradient-to-r from-sky-500 to-indigo-600 hover:from-sky-400 hover:to-indigo-500 disabled:opacity-50 text-white rounded-xl text-xs font-bold shadow-md shadow-sky-500/20 transition active:scale-95"
                >
                  <Plus size={15} />
                  {isAddingToMonitor ? 'Validating...' : 'Add Stock'}
                </button>
              </form>
            </div>

            {/* Validation Error Alert if Symbol Doesn't Exist */}
            {specialMonitorError && (
              <div className="p-3.5 rounded-xl bg-rose-500/10 border border-rose-500/30 text-rose-300 text-xs flex items-center justify-between animate-in fade-in">
                <div className="flex items-center gap-2">
                  <AlertCircle size={16} className="text-rose-400 shrink-0" />
                  <span>{specialMonitorError}</span>
                </div>
                <button onClick={() => setSpecialMonitorError(null)} className="text-rose-400 hover:text-white">
                  <X size={14} />
                </button>
              </div>
            )}

            {/* Quick 1-Click Suggestions */}
            <div className="flex flex-wrap items-center gap-2 pt-1 border-t border-slate-800/80">
              <span className="text-[11px] font-semibold text-slate-400">Quick Add:</span>
              {['RELIANCE', 'TCS', 'INFY', 'BEL', 'HAL', 'HDFCBANK', 'ICICIBANK', 'SBIN', 'TATAMOTORS', 'ZOMATO'].map((quickSym) => {
                const isAlreadyAdded = watchlist.includes(quickSym);
                return (
                  <button
                    key={quickSym}
                    onClick={() => !isAlreadyAdded && handleAddWatchlist(quickSym)}
                    disabled={isAlreadyAdded}
                    className={`px-2.5 py-1 rounded-lg text-[11px] font-mono font-bold transition flex items-center gap-1 ${
                      isAlreadyAdded
                        ? 'bg-slate-800/40 text-slate-600 border border-slate-800 cursor-not-allowed'
                        : 'bg-slate-800/80 hover:bg-slate-700 text-slate-300 hover:text-white border border-slate-700'
                    }`}
                  >
                    {isAlreadyAdded ? <Check size={11} className="text-emerald-500" /> : <Plus size={11} className="text-sky-400" />}
                    {quickSym}
                  </button>
                );
              })}
            </div>
          </div>

          {/* Monitored Stocks List / Cards */}
          <div className="space-y-4">
            <div className="flex items-center justify-between text-xs text-slate-400">
              <span>Showing <strong>{specialMonitorItems.length}</strong> Priority Stocks with Real-Time Strategy Signals</span>
              <span className="flex items-center gap-1 text-emerald-400 font-semibold">
                <CheckCircle2 size={13} /> Live NSE/BSE Pricing &amp; 21-EMA Active
              </span>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
              {specialMonitorItems.map((item) => {
                const isGain = item.dayChangePercent >= 0;
                return (
                  <div
                    key={item.symbol}
                    className="p-5 rounded-2xl bg-slate-900 border border-slate-800 hover:border-slate-700 transition flex flex-col justify-between shadow-lg relative overflow-hidden group"
                  >
                    {/* Top Row: Symbol, Company & Delete */}
                    <div>
                      <div className="flex items-start justify-between gap-3">
                        <div>
                          <div className="flex items-center gap-2">
                            <h3 className="text-xl font-extrabold text-white tracking-tight font-mono">
                              {item.symbol}
                            </h3>
                            <span className="px-2 py-0.5 rounded text-[10px] font-bold bg-slate-800 text-slate-300 border border-slate-700">
                              {item.exchange}
                            </span>
                            {/* AI Signal Badge */}
                            {item.aiSignal === 'STRONG_BUY' && (
                              <span className="px-2.5 py-0.5 rounded-full text-[10px] font-extrabold bg-emerald-500/20 text-emerald-300 border border-emerald-500/40 flex items-center gap-1">
                                <Sparkles size={11} /> STRONG BUY
                              </span>
                            )}
                            {item.aiSignal === 'BUY_THE_DIP' && (
                              <span className="px-2.5 py-0.5 rounded-full text-[10px] font-extrabold bg-sky-500/20 text-sky-300 border border-sky-500/40 flex items-center gap-1">
                                <Zap size={11} /> BUY THE DIP
                              </span>
                            )}
                            {item.aiSignal === 'WAIT_FOR_PULLBACK' && (
                              <span className="px-2.5 py-0.5 rounded-full text-[10px] font-extrabold bg-amber-500/20 text-amber-300 border border-amber-500/40 flex items-center gap-1">
                                <Clock size={11} /> WAIT FOR PULLBACK
                              </span>
                            )}
                            {item.aiSignal === 'ACCUMULATE' && (
                              <span className="px-2.5 py-0.5 rounded-full text-[10px] font-extrabold bg-teal-500/20 text-teal-300 border border-teal-500/40 flex items-center gap-1">
                                <Plus size={11} /> ACCUMULATE (CNC)
                              </span>
                            )}
                          </div>
                          <p className="text-xs text-slate-400 mt-0.5 font-medium">{item.companyName}</p>
                        </div>

                        <div className="flex items-center gap-1">
                          <button
                            onClick={() => handleRemoveWatchlist(item.symbol)}
                            title={`Remove ${item.symbol} from Special Monitor`}
                            className="p-1.5 rounded-lg text-slate-500 hover:text-rose-400 hover:bg-slate-800 transition"
                          >
                            <Trash2 size={15} />
                          </button>
                        </div>
                      </div>

                      {/* Live Price & Day Change Banner */}
                      <div className="mt-4 p-3.5 rounded-xl bg-slate-950/80 border border-slate-800/80 flex items-center justify-between">
                        <div>
                          <p className="text-[11px] text-slate-500 font-medium">Last Traded Price</p>
                          <p className="text-2xl font-black text-white font-mono mt-0.5">
                            ₹{item.lastPrice.toLocaleString('en-IN', { minimumFractionDigits: 2 })}
                          </p>
                        </div>

                        <div className="text-right">
                          <p className="text-[11px] text-slate-500 font-medium">Day Change</p>
                          <span
                            className={`inline-flex items-center gap-1 text-sm font-bold font-mono px-2 py-0.5 rounded-lg mt-0.5 ${
                              isGain
                                ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/30'
                                : 'bg-rose-500/10 text-rose-400 border border-rose-500/30'
                            }`}
                          >
                            {isGain ? <ArrowUpRight size={14} /> : <ArrowDownRight size={14} />}
                            {isGain ? '+' : ''}{item.dayChangePercent.toFixed(2)}% (₹{item.dayChange.toFixed(2)})
                          </span>
                        </div>
                      </div>

                      {/* Technical Diagnostics Badges: 21-EMA, ATM Option, Range */}
                      <div className="mt-3 grid grid-cols-2 sm:grid-cols-3 gap-2 text-xs">
                        {/* 21-EMA Proximity */}
                        <div className="p-2.5 rounded-xl bg-slate-950/60 border border-slate-800">
                          <span className="text-[10px] text-slate-500 block uppercase font-semibold">21 EMA Support</span>
                          <span className="text-slate-200 font-bold font-mono">₹{item.ema21.toFixed(2)}</span>
                          <span className={`block text-[10px] font-semibold mt-0.5 ${
                            Math.abs(item.distanceToEmaPercent) <= 1.0 ? 'text-emerald-400' : item.distanceToEmaPercent > 0 ? 'text-sky-400' : 'text-amber-400'
                          }`}>
                            {item.distanceToEmaPercent >= 0 ? '+' : ''}{item.distanceToEmaPercent.toFixed(2)}% from EMA
                          </span>
                        </div>

                        {/* ATM Option Strike */}
                        <div className="p-2.5 rounded-xl bg-purple-950/30 border border-purple-800/40">
                          <span className="text-[10px] text-purple-400 block uppercase font-bold">ATM Option Strike</span>
                          <span className="text-purple-200 font-extrabold font-mono text-sm">{item.suggestedAtmOption}</span>
                          <span className="block text-[10px] text-purple-400/80 mt-0.5">At-The-Money Call</span>
                        </div>

                        {/* Confidence Score */}
                        <div className="p-2.5 rounded-xl bg-slate-950/60 border border-slate-800 col-span-2 sm:col-span-1">
                          <span className="text-[10px] text-slate-500 block uppercase font-semibold">AI Confidence</span>
                          <span className="text-sky-400 font-extrabold font-mono text-sm">{item.confidenceScore}%</span>
                          <span className="block text-[10px] text-slate-500 mt-0.5">R:R 1 : {item.riskRewardRatio}</span>
                        </div>
                      </div>

                      {/* AI Strategic Reasoning */}
                      <div className="mt-3.5 p-3 rounded-xl bg-slate-950/90 border border-slate-800 text-xs text-slate-300">
                        <div className="flex items-center gap-1.5 text-sky-400 font-semibold mb-1">
                          <Sparkles size={13} />
                          <span>AI Strategy Verdict:</span>
                        </div>
                        <p className="text-[11px] text-slate-300 leading-relaxed">{item.aiReason}</p>
                      </div>

                      {/* Trade Targets: Entry / StopLoss / Target */}
                      <div className="mt-3 grid grid-cols-3 gap-2 text-center text-xs p-2.5 bg-slate-950/50 rounded-xl border border-slate-800">
                        <div>
                          <span className="text-[10px] text-slate-500 uppercase block">Entry</span>
                          <span className="font-mono font-bold text-slate-200">₹{item.suggestedEntry.toFixed(2)}</span>
                        </div>
                        <div>
                          <span className="text-[10px] text-rose-400 uppercase block">Stop Loss (-2%)</span>
                          <span className="font-mono font-bold text-rose-300">₹{item.stopLoss.toFixed(2)}</span>
                        </div>
                        <div>
                          <span className="text-[10px] text-emerald-400 uppercase block">Target (+5%)</span>
                          <span className="font-mono font-bold text-emerald-300">₹{item.targetPrice.toFixed(2)}</span>
                        </div>
                      </div>
                    </div>

                    {/* Bottom Action: 1-Click Trigger Buy Ticket */}
                    <div className="mt-4 pt-3.5 border-t border-slate-800/80 flex flex-wrap items-center justify-between gap-3">
                      <span className="text-[11px] text-slate-400 flex items-center gap-1">
                        <Clock size={11} className="text-sky-400" />
                        {formatIstTimestamp(lastScannedAt)} • {approvalDurationSeconds}s gate
                      </span>
                      <button
                        onClick={() => handleTriggerSpecialMonitorBuy(item)}
                        disabled={isSimulating}
                        className="flex items-center gap-2 px-4 py-2 bg-gradient-to-r from-emerald-500 to-teal-600 hover:from-emerald-400 hover:to-teal-500 text-white rounded-xl text-xs font-extrabold shadow-md shadow-emerald-500/20 transition active:scale-95"
                      >
                        <Zap size={14} className={isSimulating ? 'animate-spin' : ''} />
                        Trigger Buy Ticket (10 Qty)
                      </button>
                    </div>
                  </div>
                );
              })}

              {specialMonitorItems.length === 0 && !isLoading && (
                <div className="col-span-full p-12 text-center bg-slate-900/40 rounded-2xl border border-dashed border-slate-800 space-y-3">
                  <Eye size={40} className="mx-auto text-slate-600" />
                  <p className="text-slate-300 font-semibold text-sm">No stocks added to Special Monitor yet</p>
                  <p className="text-xs text-slate-500 max-w-md mx-auto">
                    Type any NSE/BSE stock ticker (e.g. RELIANCE, TCS, BEL, HAL, ZOMATO) above to track its live 21-EMA pullbacks and ATM option strikes.
                  </p>
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      {/* TAB 1: PRE-MARKET 21-EMA PULLBACK STRATEGY */}
      {activeStrategyTab === 'premarket' && (
        <div className="space-y-4">
          {/* Strategy Briefing Banner */}
          <div className="p-4 rounded-xl bg-amber-950/20 border border-amber-600/30 text-xs space-y-3">
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
              <span className="font-bold text-amber-300 text-sm flex items-center gap-1.5">
                <Target size={16} /> Pre-Market 1.0%–2.5% Gainers with 21-EMA Pullback Strategy
              </span>
              <div className="flex items-center gap-2">
                <span className="text-slate-400">Min Gain:</span>
                <input
                  type="number"
                  step="0.1"
                  value={minGain}
                  onChange={(e) => setMinGain(parseFloat(e.target.value) || 0)}
                  className="w-14 px-1.5 py-0.5 bg-slate-900 border border-slate-700 rounded text-center text-amber-300 font-mono"
                />
                <span className="text-slate-400">Max Gain:</span>
                <input
                  type="number"
                  step="0.1"
                  value={maxGain}
                  onChange={(e) => setMaxGain(parseFloat(e.target.value) || 0)}
                  className="w-14 px-1.5 py-0.5 bg-slate-900 border border-slate-700 rounded text-center text-amber-300 font-mono"
                />
                <button
                  type="button"
                  onClick={fetchData}
                  className="px-2 py-0.5 bg-amber-600 hover:bg-amber-500 text-white font-bold rounded"
                >
                  Apply
                </button>
              </div>
            </div>
            <p className="text-slate-300 leading-relaxed">
              Scans <strong>F&amp;O segment</strong> pre-market gainers up <strong>{minGain}% to {maxGain}%</strong> in a confirmed <strong>uptrend</strong> with positive news support and volume (≥ {minVolume.toLocaleString()} shares). After open, waits for a pullback touching the <strong>21 EMA</strong>, auto-selects the <strong>ATM Call Option</strong>, and sets <strong>Option SL (-2.5% below 21 EMA)</strong> &amp; <strong>Target (+10%)</strong>.
            </p>
          </div>

          {/* Cards Grid */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {(!premarketData || premarketData.signals.length === 0) ? (
              <div className="col-span-full p-8 rounded-xl bg-slate-900 border border-slate-800 text-center text-slate-400 text-sm">
                No stocks currently in the 1.0% - 2.5% gain window with 21-EMA pullback. Click "Refresh Scans".
              </div>
            ) : (
              premarketData.signals.map((sig) => (
                <div
                  key={sig.symbol}
                  className="p-5 rounded-xl bg-slate-900 border border-slate-800 hover:border-slate-700 shadow-md transition space-y-3"
                >
                  {/* Top Row */}
                  <div className="flex items-center justify-between pb-2 border-b border-slate-800">
                    <div>
                      <div className="flex items-center gap-2">
                        <h3 className="text-base font-extrabold text-white font-mono">{sig.symbol}</h3>
                        <span className="text-xs text-slate-400">{sig.name}</span>
                      </div>
                      <p className="text-xs text-emerald-400 font-bold mt-0.5">
                        LTP: ₹{sig.spotPrice.toFixed(2)} (+{sig.gainPercent}%)
                      </p>
                    </div>

                    <div className="text-right">
                      <span className={`px-2.5 py-1 rounded-full text-xs font-bold border ${
                        sig.status === 'ACTIVE_BUY_AT_21EMA'
                          ? 'bg-emerald-500/20 text-emerald-300 border-emerald-500/40 animate-pulse'
                          : 'bg-amber-500/20 text-amber-300 border-amber-500/40'
                      }`}>
                        {sig.status === 'ACTIVE_BUY_AT_21EMA' ? '⚡ TOUCHED 21-EMA (ACTIVE BUY)' : 'MONITORING PULLBACK'}
                      </span>
                      <p className="text-[10px] text-slate-400 font-mono mt-1">
                        Dist: {sig.distanceToEmaPercent > 0 ? `+${sig.distanceToEmaPercent}` : sig.distanceToEmaPercent}%
                      </p>
                    </div>
                  </div>

                  {/* Quantitative Metrics & Option Strike */}
                  <div className="grid grid-cols-3 gap-2 p-3 rounded-lg bg-slate-950 border border-slate-800/80 text-xs">
                    <div>
                      <span className="text-[10px] text-slate-500 uppercase font-bold">21 EMA</span>
                      <p className="font-mono font-bold text-amber-400">₹{sig.ema21.toFixed(2)}</p>
                    </div>
                    <div>
                      <span className="text-[10px] text-slate-500 uppercase font-bold">Volume</span>
                      <p className="font-mono font-bold text-slate-200">{sig.volume.toLocaleString()}</p>
                    </div>
                    <div>
                      <span className="text-[10px] text-sky-400 uppercase font-bold">ATM Option Strike</span>
                      <p className="font-mono font-extrabold text-sky-300 bg-sky-500/20 px-1.5 py-0.5 rounded text-center">
                        {sig.suggestedAtmOption}
                      </p>
                    </div>
                  </div>

                  {/* Uptrend & News Badges */}
                  <div className="flex flex-wrap items-center gap-2 text-[11px]">
                    <span className="px-2 py-0.5 rounded bg-emerald-500/15 text-emerald-300 border border-emerald-500/30 font-semibold">
                      📈 Uptrend Confirmed
                    </span>
                    <span className={`px-2 py-0.5 rounded border font-semibold ${
                      sig.newsSentiment === 'POSITIVE'
                        ? 'bg-sky-500/15 text-sky-300 border-sky-500/30'
                        : 'bg-slate-800 text-slate-300 border-slate-700'
                    }`}>
                      {sig.newsSentiment === 'POSITIVE' ? '📰 Positive News' : '📰 Neutral News'}
                    </span>
                    {sig.optionSymbol && (
                      <span className="px-2 py-0.5 rounded bg-indigo-500/15 text-indigo-300 border border-indigo-500/30 font-mono font-bold">
                        {sig.optionSymbol}
                      </span>
                    )}
                  </div>

                  {/* Risk & Target (Configured from Account Risk Settings) */}
                  <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-1 text-xs px-1">
                    <span className="text-rose-400 font-semibold">
                      SL (-{slPercent}%): ₹{(sig.optionStopLoss && sig.optionStopLoss > 0 ? sig.optionStopLoss : sig.stopLoss).toFixed(2)}
                    </span>
                    <span className="text-emerald-400 font-semibold">
                      Target (+{tgtPercent}%): ₹{(sig.optionTarget && sig.optionTarget > 0 ? sig.optionTarget : sig.targetPrice).toFixed(2)}
                    </span>
                  </div>
                  <div className="text-[11px] text-slate-400 px-1 flex items-center gap-1">
                    <Clock size={11} className="text-sky-400" />
                    Condition Matched: {formatIstTimestamp(sig.timestamp)}
                  </div>

                  {/* 1-Click Trigger & 4-Stage Skill Check */}
                  <div className="pt-2 flex flex-wrap items-center justify-between gap-2 border-t border-slate-800">
                    <span className="text-[11px] text-slate-500">
                      Routes via {approvalDurationSeconds}s countdown gate
                    </span>
                    <div className="flex items-center gap-2">
                      <button
                        onClick={() => handleInspectSkills(sig.symbol, sig.spotPrice, sig.stopLoss, sig.targetPrice, sig.ema21)}
                        className="flex items-center gap-1 px-2.5 py-1.5 bg-slate-800 hover:bg-slate-700 text-sky-300 border border-slate-700 rounded-lg text-xs font-semibold transition"
                      >
                        <Search size={12} />
                        4-Stage Check
                      </button>
                      <button
                        onClick={() => handleTriggerPreMarketBuy(sig)}
                        disabled={isSimulating}
                        className="flex items-center gap-1.5 px-3.5 py-1.5 bg-gradient-to-r from-amber-500 to-emerald-600 hover:from-amber-400 hover:to-emerald-500 text-white rounded-lg text-xs font-bold shadow-md transition"
                      >
                        <Zap size={13} />
                        Trigger Buy (ATM Option + Equity)
                      </button>
                    </div>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      )}

      {/* TAB 2: NSE OPENING RANGE BREAKOUTS (ORB) */}
      {activeStrategyTab === 'orb' && (
        <div className="space-y-4">
          <div className="p-4 rounded-xl bg-sky-950/20 border border-sky-600/30 text-xs">
            <span className="font-bold text-sky-300 text-sm block mb-1">
              Opening-Range Breakouts (ORB 9:15 - 9:30 AM IST)
            </span>
            Identifies stocks breaking above the 15-minute opening high with high turnover, Wilder's RSI confirmation, and CAS 15:12 square-off enforcement.
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {orbHits.length === 0 ? (
              <div className="col-span-full p-8 rounded-xl bg-slate-900 border border-slate-800 text-center text-slate-400 text-sm">
                No active ORB breakouts detected at this moment.
              </div>
            ) : (
              orbHits.map((h) => (
                <div key={h.symbol} className="p-4 rounded-xl bg-slate-900 border border-slate-800 space-y-2.5">
                  <div className="flex items-center justify-between">
                    <div>
                      <h3 className="font-extrabold text-white font-mono text-base">{h.symbol}</h3>
                      <span className="text-xs text-slate-400">Price: ₹{h.price.toFixed(2)}</span>
                    </div>
                    <div className="flex flex-wrap gap-1">
                      {h.setups.map(s => (
                        <span key={s} className="px-2 py-0.5 rounded text-[10px] font-bold bg-sky-500/20 text-sky-300 border border-sky-500/30">
                          {s}
                        </span>
                      ))}
                    </div>
                  </div>

                  <div className="grid grid-cols-3 gap-2 p-2.5 rounded-lg bg-slate-950 text-xs font-mono">
                    <div>
                      <span className="text-[10px] text-slate-500 block">RSI 14</span>
                      <span className="text-slate-200 font-bold">{h.rsi.toFixed(1)}</span>
                    </div>
                    <div>
                      <span className="text-[10px] text-slate-500 block">Volume</span>
                      <span className="text-slate-200 font-bold">{h.volume.toLocaleString()}</span>
                    </div>
                    <div>
                      <span className="text-[10px] text-slate-500 block">Square-Off</span>
                      <span className="text-amber-400 font-bold">{h.squareOff} IST</span>
                    </div>
                  </div>

                  <div className="flex items-center justify-between text-xs px-1">
                    <span className="text-rose-400 font-semibold">
                      SL (-{slPercent}%): ₹{(h.price * (1 - slPercent / 100)).toFixed(2)}
                    </span>
                    <span className="text-emerald-400 font-semibold">
                      Target (+{tgtPercent}%): ₹{(h.price * (1 + tgtPercent / 100)).toFixed(2)}
                    </span>
                  </div>

                  <div className="pt-2 flex flex-wrap items-center justify-between gap-2 border-t border-slate-800">
                    <span className="text-[11px] text-slate-400 flex items-center gap-1">
                      <Clock size={11} className="text-sky-400" />
                      Suggested Entry Time: {formatIstTimestamp(h.timestamp)}
                    </span>
                    <div className="flex items-center gap-2">
                      <button
                        onClick={() => handleInspectSkills(h.symbol, h.price, Number((h.price * (1 - slPercent / 100)).toFixed(2)), Number((h.price * (1 + tgtPercent / 100)).toFixed(2)))}
                        className="flex items-center gap-1 px-2.5 py-1.5 bg-slate-800 hover:bg-slate-700 text-sky-300 border border-slate-700 rounded-lg text-xs font-semibold transition"
                      >
                        <Search size={12} />
                        4-Stage Check
                      </button>
                      <button
                        onClick={() => handleTriggerOrbBuy(h)}
                        disabled={isSimulating}
                        className="flex items-center gap-1.5 px-3 py-1.5 bg-gradient-to-r from-sky-500 to-emerald-600 hover:from-sky-400 hover:to-emerald-500 text-white rounded-lg text-xs font-bold shadow-md transition"
                      >
                        <Zap size={13} />
                        Trigger Buy
                      </button>
                    </div>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      )}

      {/* TAB 3: SEBI WHALE DEALS */}
      {activeStrategyTab === 'insiders' && (
        <div className="space-y-4">
          <div className="p-4 rounded-xl bg-purple-950/20 border border-purple-600/30 text-xs">
            <span className="font-bold text-purple-300 text-sm block mb-1">
              SEBI PIT &amp; NSE Whale Deals (Bulk &amp; Block Transactions)
            </span>
            Real-time feed of institutional and high-net-worth investor transactions (Quant, Graviton, HRTI, Arihant Capital) reported to the exchange.
          </div>

          <div className="overflow-x-auto rounded-xl border border-slate-800">
            <table className="w-full text-xs text-left">
              <thead className="bg-slate-950 text-slate-400 uppercase text-[10px] font-semibold">
                <tr>
                  <th className="px-4 py-3">Date</th>
                  <th className="px-4 py-3">Symbol</th>
                  <th className="px-4 py-3">Investor / Client</th>
                  <th className="px-4 py-3">Side</th>
                  <th className="px-4 py-3 text-right">Shares</th>
                  <th className="px-4 py-3 text-right">Price</th>
                  <th className="px-4 py-3 text-right">Value (₹)</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-800 bg-slate-900">
                {insiderDeals.map((d) => (
                  <tr key={d.id} className="hover:bg-slate-800/50">
                    <td className="px-4 py-2.5 font-mono text-slate-400">{d.date}</td>
                    <td className="px-4 py-2.5 font-mono font-bold text-white">{d.symbol}</td>
                    <td className="px-4 py-2.5 text-slate-300 max-w-xs truncate">{d.client}</td>
                    <td className="px-4 py-2.5">
                      <span className={`px-2 py-0.5 rounded text-[10px] font-bold ${
                        d.side === 'BUY'
                          ? 'bg-emerald-500/20 text-emerald-300'
                          : 'bg-rose-500/20 text-rose-300'
                      }`}>
                        {d.side}
                      </span>
                    </td>
                    <td className="px-4 py-2.5 text-right font-mono text-slate-200">{d.shares.toLocaleString()}</td>
                    <td className="px-4 py-2.5 text-right font-mono text-slate-200">₹{d.price.toFixed(2)}</td>
                    <td className="px-4 py-2.5 text-right font-mono font-bold text-purple-300">
                      ₹{(d.valueInr / 10000000).toFixed(2)} Cr
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* TAB 4: HOLDINGS & AI CO-PILOT */}
      {activeStrategyTab === 'holdings' && (
        <div className="space-y-4">
          {/* Filter Tabs */}
          <div className="flex flex-wrap items-center gap-2">
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

          <div className="space-y-4">
            {filteredAnalyses.length === 0 ? (
              <div className="p-8 rounded-xl bg-slate-900 border border-slate-800 text-center text-slate-400 text-sm">
                No stocks matched the selected filter. Try "ALL" or click "Refresh Scans".
              </div>
            ) : (
              filteredAnalyses.map((item) => (
                <div
                  key={item.symbol}
                  className="p-5 rounded-xl bg-slate-900 border border-slate-800 shadow-md hover:border-slate-700 transition space-y-3"
                >
                  <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-3 border-b border-slate-800">
                    <div className="flex items-center gap-3">
                      <div>
                        <div className="flex items-center gap-2">
                          <h3 className="text-base font-extrabold text-white font-mono">{item.symbol}</h3>
                          <span className="text-xs text-slate-400">{item.companyName}</span>
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

                  <div className="grid grid-cols-2 sm:grid-cols-4 gap-2.5 p-3 rounded-lg bg-slate-950 border border-slate-800 text-xs">
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

                  <div className="space-y-1.5 text-xs">
                    <p className="text-slate-200 font-semibold leading-relaxed">{item.summary}</p>
                    <p className="text-slate-400 text-[11px]">💡 <strong>Rationale:</strong> {item.technicalRationale}</p>
                    <p className="text-slate-500 text-[11px] flex items-center gap-1">
                      <Clock size={11} className="text-sky-400" />
                      Suggested Entry Time: {formatIstTimestamp(item.analyzedAtUtc)}
                    </p>
                  </div>

                  {(item.action === 'STRONG_BUY' || item.action === 'BUY_THE_DIP') && (
                    <div className="pt-2 flex items-center justify-between border-t border-slate-800">
                      <span className="text-[11px] text-slate-500 flex items-center gap-1">
                        <Clock size={12} /> {approvalDurationSeconds}s Rejection countdown
                      </span>
                      <button
                        onClick={() => handleSimulateBuy(item.symbol)}
                        className="flex items-center gap-1.5 px-3 py-1.5 bg-gradient-to-r from-emerald-600 to-teal-600 hover:from-emerald-500 hover:to-teal-500 text-white rounded-lg text-xs font-bold shadow-md transition"
                      >
                        <Zap size={13} />
                        Trigger Buy
                      </button>
                    </div>
                  )}
                </div>
              ))
            )}
          </div>
        </div>
      )}

      {/* AI Configuration Modal */}
      {isConfigOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/75 backdrop-blur-sm animate-in fade-in duration-200">
          <div className="bg-slate-900 border border-slate-800 rounded-2xl w-full max-w-xl overflow-hidden shadow-2xl">
            <div className="flex items-center justify-between px-6 py-4 border-b border-slate-800 bg-slate-950/50">
              <div className="flex items-center gap-2.5">
                <span className="p-1.5 rounded-lg bg-indigo-500/20 text-indigo-400 border border-indigo-500/30">
                  <Sliders size={18} />
                </span>
                <div>
                  <h3 className="text-base font-bold text-white">AI Advisor Configuration</h3>
                  <p className="text-xs text-slate-400">Switch providers or fine-tune conviction threshold</p>
                </div>
              </div>
              <button
                onClick={() => setIsConfigOpen(false)}
                className="p-1.5 text-slate-400 hover:text-white rounded-lg hover:bg-slate-800 transition"
              >
                <X size={18} />
              </button>
            </div>

            <form onSubmit={handleSaveConfig} className="p-6 space-y-4">
              {configSuccessMsg && (
                <div className="p-3 rounded-lg bg-emerald-950/60 border border-emerald-500/40 text-emerald-300 text-xs flex items-center gap-2">
                  <Check size={16} />
                  {configSuccessMsg}
                </div>
              )}

              <div>
                <label className="block text-xs font-semibold text-slate-300 mb-1.5">AI Provider</label>
                <select
                  value={config.provider}
                  onChange={(e) => {
                    const p = e.target.value;
                    const defaultModels: Record<string, string> = {
                      GeminiPro: 'gemini-2.5-flash',
                      OpenAI: 'gpt-4.1',
                      Claude: 'claude-sonnet-4-20250514',
                      DeepSeek: 'deepseek-reasoner',
                      BuiltInQuant: 'quant-engine-v2',
                    };
                    setConfig({ ...config, provider: p, model: defaultModels[p] || 'gemini-2.5-flash' });
                  }}
                  className="w-full px-3.5 py-2.5 bg-slate-950 border border-slate-700 rounded-lg text-xs text-white"
                >
                  <option value="GeminiPro">Google Gemini (Latest 2.5 — Recommended)</option>
                  <option value="OpenAI">OpenAI (GPT-4.1 / GPT-4o)</option>
                  <option value="Claude">Anthropic Claude (Sonnet 4)</option>
                  <option value="DeepSeek">DeepSeek (R1 / V3)</option>
                  <option value="BuiltInQuant">Built-in Quantitative Engine (No Key Needed)</option>
                </select>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-300 mb-1.5">Model Identifier</label>
                <select
                  value={config.model}
                  onChange={(e) => setConfig({ ...config, model: e.target.value })}
                  className="w-full px-3.5 py-2.5 bg-slate-950 border border-slate-700 rounded-lg text-xs text-white font-mono"
                >
                  {config.provider === 'GeminiPro' && (
                    <>
                      <option value="gemini-2.5-flash">gemini-2.5-flash (Latest Fast — Default)</option>
                      <option value="gemini-2.5-pro">gemini-2.5-pro (Latest Deep Reasoning)</option>
                      <option value="gemini-2.0-flash">gemini-2.0-flash</option>
                    </>
                  )}
                  {config.provider === 'OpenAI' && (
                    <>
                      <option value="gpt-4.1">gpt-4.1 (Latest — Default)</option>
                      <option value="gpt-4o">gpt-4o</option>
                      <option value="o3-mini">o3-mini</option>
                    </>
                  )}
                  {config.provider === 'Claude' && (
                    <>
                      <option value="claude-sonnet-4-20250514">claude-sonnet-4-20250514 (Latest — Default)</option>
                      <option value="claude-3-7-sonnet-latest">claude-3-7-sonnet-latest</option>
                    </>
                  )}
                  {config.provider === 'DeepSeek' && (
                    <>
                      <option value="deepseek-reasoner">deepseek-reasoner (R1 — Default)</option>
                      <option value="deepseek-chat">deepseek-chat (V3)</option>
                    </>
                  )}
                  {config.provider === 'BuiltInQuant' && (
                    <option value="quant-engine-v2">quant-engine-v2 (Local Deterministic)</option>
                  )}
                </select>
              </div>

              <div>
                <div className="flex items-center justify-between mb-1.5">
                  <label className="block text-xs font-semibold text-slate-300">API Key</label>
                  {config.hasApiKey && (
                    <span className="text-[11px] font-semibold text-emerald-400">
                      ✓ Configured ({config.activeSource === 'AppSettings' ? 'Server .env' : 'Saved'})
                    </span>
                  )}
                </div>
                <input
                  type="text"
                  value={config.apiKey || ''}
                  onChange={(e) => setConfig({ ...config, apiKey: e.target.value })}
                  placeholder={config.hasApiKey ? 'Configured on server (paste new key to replace)' : 'Paste API Key'}
                  className="w-full px-3.5 py-2 bg-slate-950 border border-slate-700 rounded-lg text-xs text-white font-mono"
                />
              </div>

              <div>
                <div className="flex items-center justify-between text-xs mb-1.5">
                  <span className="font-semibold text-slate-300">Minimum Conviction</span>
                  <span className="font-bold text-indigo-400 font-mono">{Math.round(config.minConfidenceThreshold * 100)}%</span>
                </div>
                <input
                  type="range"
                  min="0.60"
                  max="0.95"
                  step="0.05"
                  value={config.minConfidenceThreshold}
                  onChange={(e) => setConfig({ ...config, minConfidenceThreshold: parseFloat(e.target.value) })}
                  className="w-full accent-indigo-500 bg-slate-800 rounded-lg cursor-pointer"
                />
              </div>

              <div className="flex items-center justify-between pt-4 border-t border-slate-800">
                <button
                  type="button"
                  onClick={handleResetConfig}
                  disabled={savingConfig}
                  className="flex items-center gap-1.5 px-3 py-2 text-xs font-semibold text-slate-400 hover:text-white bg-slate-800/60 rounded-lg border border-slate-700"
                >
                  <RotateCcw size={13} /> Defaults
                </button>
                <div className="flex items-center gap-2">
                  <button
                    type="button"
                    onClick={() => setIsConfigOpen(false)}
                    className="px-3 py-2 text-xs font-semibold text-slate-400 hover:text-white"
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    disabled={savingConfig}
                    className="flex items-center gap-1.5 px-4 py-2 bg-gradient-to-r from-indigo-500 to-sky-600 hover:from-indigo-400 hover:to-sky-500 text-white rounded-lg text-xs font-bold"
                  >
                    <Check size={14} /> Save
                  </button>
                </div>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* 4-STAGE SKILL INSPECTOR MODAL */}
      {inspectSymbol && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/80 backdrop-blur-sm p-4">
          <div className="bg-slate-900 border border-slate-800 rounded-2xl w-full max-w-2xl max-h-[85vh] overflow-y-auto p-6 shadow-2xl space-y-4">
            <div className="flex items-center justify-between border-b border-slate-800 pb-3">
              <div>
                <h3 className="text-lg font-bold text-white flex items-center gap-2">
                  <Sparkles size={18} className="text-sky-400" />
                  4-Stage Skill Analysis: {inspectSymbol}
                </h3>
                <p className="text-xs text-slate-400">
                  india-news • india-analyst (XBRL) • india-entry (5m) • india-timemachine (60d)
                </p>
              </div>
              <button
                onClick={() => setInspectSymbol(null)}
                className="p-1.5 text-slate-400 hover:text-white rounded-lg hover:bg-slate-800"
              >
                <X size={18} />
              </button>
            </div>

            {isInspectLoading ? (
              <div className="py-12 text-center text-slate-400 text-sm">
                Running NSE News, Quarterly XBRL, 5m Entry & 60-Session Time Machine...
              </div>
            ) : (
              <div className="space-y-4 text-xs">
                {/* Stage 2: News */}
                <div className="p-3.5 rounded-xl bg-slate-950/60 border border-slate-800 space-y-1.5">
                  <div className="flex items-center justify-between">
                    <span className="font-bold text-sky-300">1. NSE Announcements & News (india-news)</span>
                    {inspectData?.news?.hasRedFlag ? (
                      <span className="px-2 py-0.5 rounded bg-rose-500/20 text-rose-300 border border-rose-500/40 font-semibold">
                        Red Flag Detected
                      </span>
                    ) : (
                      <span className="px-2 py-0.5 rounded bg-emerald-500/15 text-emerald-300 border border-emerald-500/30 font-semibold">
                        No Red Flags
                      </span>
                    )}
                  </div>
                  {(inspectData?.news?.items?.length ?? 0) > 0 ? (
                    <ul className="space-y-1 text-slate-300">
                      {inspectData?.news?.items.slice(0, 3).map((item: any, idx: number) => (
                        <li key={idx} className="truncate">• [{item.source}] {item.title}</li>
                      ))}
                    </ul>
                  ) : (
                    <p className="text-slate-500">No recent breaking items in last 48h.</p>
                  )}
                </div>

                {/* Stage 2b: Analyst */}
                <div className="p-3.5 rounded-xl bg-slate-950/60 border border-slate-800 space-y-1.5">
                  <div className="flex items-center justify-between">
                    <span className="font-bold text-indigo-300">2. Quarterly XBRL Results (india-analyst)</span>
                    {inspectData?.analyst?.resultsImminent && (
                      <span className="px-2 py-0.5 rounded bg-amber-500/20 text-amber-300 border border-amber-500/40 font-semibold">
                        Results Imminent
                      </span>
                    )}
                  </div>
                  {(inspectData?.analyst?.changes?.length ?? 0) > 0 ? (
                    <ul className="space-y-1 text-slate-300">
                      {inspectData?.analyst?.changes.slice(0, 3).map((c: string, idx: number) => (
                        <li key={idx}>• {c}</li>
                      ))}
                    </ul>
                  ) : (
                    <p className="text-slate-500">Quarterly XBRL data unavailable for this symbol.</p>
                  )}
                  {inspectData?.analyst?.nextResultsNote && (
                    <p className="text-slate-400 italic">{inspectData.analyst.nextResultsNote}</p>
                  )}
                </div>

                {/* Stage 3: Entry */}
                <div className="p-3.5 rounded-xl bg-slate-950/60 border border-slate-800 space-y-1">
                  <div className="flex items-center justify-between">
                    <span className="font-bold text-amber-300">3. 5m Candle Confirmation (india-entry)</span>
                    <span className="px-2 py-0.5 rounded bg-slate-800 text-slate-200 font-mono uppercase">
                      {inspectData?.entry?.status || 'no-data'}
                    </span>
                  </div>
                  <p className="text-slate-300">{inspectData?.entry?.reason || 'Market status checked.'}</p>
                  <p className="text-slate-500">
                    Session Close: {inspectData?.entry?.sessionCloseTime || '15:15'} IST • MIS Square-off: {inspectData?.entry?.squareOffTime || '15:12'} IST
                  </p>
                </div>

                {/* Stage 4: Time Machine */}
                <div className="p-3.5 rounded-xl bg-slate-950/60 border border-slate-800 space-y-1.5">
                  <div className="flex items-center justify-between">
                    <span className="font-bold text-emerald-300">4. 60-Session Time Machine (india-timemachine)</span>
                    <span className="text-slate-400">{inspectData?.timemachine?.replays || 0} replays</span>
                  </div>
                  {(inspectData?.timemachine?.replays ?? 0) > 0 ? (
                    <>
                      <div className="grid grid-cols-4 gap-2 text-center py-1">
                        <div className="bg-slate-900 rounded p-2">
                          <div className="text-slate-400">Target 1st</div>
                          <div className="text-emerald-400 font-bold">{inspectData?.timemachine?.targetFirstPercent}%</div>
                        </div>
                        <div className="bg-slate-900 rounded p-2">
                          <div className="text-slate-400">Stop 1st</div>
                          <div className="text-rose-400 font-bold">{inspectData?.timemachine?.stopFirstPercent}%</div>
                        </div>
                        <div className="bg-slate-900 rounded p-2">
                          <div className="text-slate-400">Avg R (Gross)</div>
                          <div className="text-white font-bold">{inspectData?.timemachine?.avgRGross}R</div>
                        </div>
                        <div className="bg-slate-900 rounded p-2">
                          <div className="text-slate-400">Avg R (Net)</div>
                          <div className="text-sky-400 font-bold">{inspectData?.timemachine?.avgRNet}R</div>
                        </div>
                      </div>
                      <p className="text-slate-300">{inspectData?.timemachine?.verdict}</p>
                    </>
                  ) : (
                    <p className="text-slate-500">{inspectData?.timemachine?.verdict || 'Replay unavailable.'}</p>
                  )}
                </div>
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
};
