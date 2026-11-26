import React, { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import { 
  ShieldAlert, 
  Clock, 
  AlertTriangle, 
  CheckCircle2, 
  Save, 
  Power,
  Info,
  TrendingUp,
  Briefcase,
  Layers,
  ArrowRightLeft,
  XCircle
} from 'lucide-react';
import { apiClient } from '../api/client';
import type { ApiResponse, TradingAccountDto, AccountConfigurationDto, TradingMode } from '../types';

export const AccountSettingsPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const requestedAccountId = searchParams.get('accountId');

  const [accounts, setAccounts] = useState<TradingAccountDto[]>([]);
  const [selectedAccountId, setSelectedAccountId] = useState<string>('');
  const [showModeModal, setShowModeModal] = useState(false);
  const [isTogglingMode, setIsTogglingMode] = useState(false);

  const [config, setConfig] = useState<AccountConfigurationDto>({
    maxCapitalAllocation: 500000,
    maxOrderValue: 100000,
    maxPositions: 5,
    maxRiskPerTradePercent: 2.0,
    dailyLossLimit: 10000,
    maxExposurePerStockPercent: 25.0,
    maxTradesPerDay: 20,
    stopLossRequired: true,
    approvalDurationSeconds: 5,
    autoExecuteOnTimeout: true,
    killSwitchActive: false,
    tradingScheduleStart: '09:15:00',
    tradingScheduleEnd: '15:30:00',
    intradaySquareOffTime: '15:15:00',
    allowedTradingSegment: 'Both',
    intradayMinVolume: 20000,
    intradayStopLossPercent: 2.0,
    intradayTargetPercent: 5.0,
    intradayMaxCapitalAllocation: 500000,
    intradayMaxOrderValue: 50000,
    intradayDailyLossLimit: 10000,
    intradayMaxPositions: 5,
    deliveryMinVolume: 50000,
    deliveryStopLossPercent: 4.0,
    deliveryTargetPercent: 10.0,
    deliveryMaxCapitalAllocation: 500000,
    deliveryMaxOrderValue: 100000,
    deliveryDailyLossLimit: 20000,
    deliveryMaxPositions: 10,
  });

  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  const fetchAccounts = async () => {
    setIsLoading(true);
    try {
      const res = await apiClient.get<ApiResponse<TradingAccountDto[]>>('/trading-accounts');
      if (res.data.success && res.data.data) {
        setAccounts(res.data.data);
        const match = requestedAccountId 
          ? res.data.data.find(a => a.id === requestedAccountId) 
          : res.data.data[0];
        
        if (match) {
          setSelectedAccountId(match.id);
          const c = match.configuration;
          if (c) {
            setConfig(prev => ({
              ...prev,
              ...c,
              allowedTradingSegment: c.allowedTradingSegment || 'Both',
              intradayMinVolume: c.intradayMinVolume ?? 20000,
              intradayStopLossPercent: c.intradayStopLossPercent ?? 2.0,
              intradayTargetPercent: c.intradayTargetPercent ?? 5.0,
              intradayMaxCapitalAllocation: c.intradayMaxCapitalAllocation ?? 500000,
              intradayMaxOrderValue: c.intradayMaxOrderValue ?? 50000,
              intradayDailyLossLimit: c.intradayDailyLossLimit ?? 10000,
              intradayMaxPositions: c.intradayMaxPositions ?? 5,
              deliveryMinVolume: c.deliveryMinVolume ?? 50000,
              deliveryStopLossPercent: c.deliveryStopLossPercent ?? 4.0,
              deliveryTargetPercent: c.deliveryTargetPercent ?? 10.0,
              deliveryMaxCapitalAllocation: c.deliveryMaxCapitalAllocation ?? 500000,
              deliveryMaxOrderValue: c.deliveryMaxOrderValue ?? 100000,
              deliveryDailyLossLimit: c.deliveryDailyLossLimit ?? 20000,
              deliveryMaxPositions: c.deliveryMaxPositions ?? 10,
            }));
          }
        }
      }
    } catch (err: any) {
      setErrorMsg('Failed to load trading accounts.');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchAccounts();
  }, []);

  const handleAccountChange = (id: string) => {
    setSelectedAccountId(id);
    const acc = accounts.find(a => a.id === id);
    const c = acc?.configuration;
    if (c) {
      setConfig(prev => ({
        ...prev,
        ...c,
        allowedTradingSegment: c.allowedTradingSegment || 'Both',
        intradayMinVolume: c.intradayMinVolume ?? 20000,
        intradayStopLossPercent: c.intradayStopLossPercent ?? 2.0,
        intradayTargetPercent: c.intradayTargetPercent ?? 5.0,
        intradayMaxCapitalAllocation: c.intradayMaxCapitalAllocation ?? 500000,
        intradayMaxOrderValue: c.intradayMaxOrderValue ?? 50000,
        intradayDailyLossLimit: c.intradayDailyLossLimit ?? 10000,
        intradayMaxPositions: c.intradayMaxPositions ?? 5,
        deliveryMinVolume: c.deliveryMinVolume ?? 50000,
        deliveryStopLossPercent: c.deliveryStopLossPercent ?? 4.0,
        deliveryTargetPercent: c.deliveryTargetPercent ?? 10.0,
        deliveryMaxCapitalAllocation: c.deliveryMaxCapitalAllocation ?? 500000,
        deliveryMaxOrderValue: c.deliveryMaxOrderValue ?? 100000,
        deliveryDailyLossLimit: c.deliveryDailyLossLimit ?? 20000,
        deliveryMaxPositions: c.deliveryMaxPositions ?? 10,
      }));
    }
    setSuccessMsg(null);
    setErrorMsg(null);
  };

  const handleToggleTradingMode = async () => {
    if (!selectedAccount) return;
    setIsTogglingMode(true);
    setSuccessMsg(null);
    setErrorMsg(null);
    const targetMode: TradingMode = selectedAccount.tradingMode === 'Live' ? 'Paper' : 'Live';

    try {
      const res = await apiClient.post<ApiResponse<TradingAccountDto>>(
        `/trading-accounts/${selectedAccount.id}/toggle-mode`,
        { tradingMode: targetMode }
      );
      if (res.data.success && res.data.data) {
        setSuccessMsg(`Trading mode successfully switched to ${targetMode}.`);
        setShowModeModal(false);
        await fetchAccounts();
      } else {
        setErrorMsg(res.data.message || 'Failed to toggle trading mode.');
      }
    } catch (err: any) {
      setErrorMsg(err.response?.data?.message || 'Failed to switch trading mode.');
    } finally {
      setIsTogglingMode(false);
    }
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedAccountId) return;
    setIsSaving(true);
    setSuccessMsg(null);
    setErrorMsg(null);

    try {
      const res = await apiClient.put<ApiResponse<AccountConfigurationDto>>(
        `/trading-accounts/${selectedAccountId}/configuration`,
        config
      );
      if (res.data.success && res.data.data) {
        const d = res.data.data;
        setConfig(prev => ({
          ...prev,
          ...d,
          allowedTradingSegment: d.allowedTradingSegment || 'Both',
          intradayMinVolume: d.intradayMinVolume ?? 20000,
          intradayStopLossPercent: d.intradayStopLossPercent ?? 2.0,
          intradayTargetPercent: d.intradayTargetPercent ?? 5.0,
          intradayMaxCapitalAllocation: d.intradayMaxCapitalAllocation ?? 500000,
          intradayMaxOrderValue: d.intradayMaxOrderValue ?? 50000,
          intradayDailyLossLimit: d.intradayDailyLossLimit ?? 10000,
          intradayMaxPositions: d.intradayMaxPositions ?? 5,
          deliveryMinVolume: d.deliveryMinVolume ?? 50000,
          deliveryStopLossPercent: d.deliveryStopLossPercent ?? 4.0,
          deliveryTargetPercent: d.deliveryTargetPercent ?? 10.0,
          deliveryMaxCapitalAllocation: d.deliveryMaxCapitalAllocation ?? 500000,
          deliveryMaxOrderValue: d.deliveryMaxOrderValue ?? 100000,
          deliveryDailyLossLimit: d.deliveryDailyLossLimit ?? 20000,
          deliveryMaxPositions: d.deliveryMaxPositions ?? 10,
        }));
        setSuccessMsg('All Intraday, Delivery, and Safety configurations saved successfully.');
      } else {
        setErrorMsg(res.data.message || 'Failed to update configuration.');
      }
    } catch (err: any) {
      setErrorMsg(
        err.response?.data?.message ||
        err.response?.data?.errors?.[0] ||
        'Failed to save configuration.'
      );
    } finally {
      setIsSaving(false);
    }
  };

  const selectedAccount = accounts.find(a => a.id === selectedAccountId);

  if (isLoading) {
    return (
      <div className="py-20 text-center text-slate-400">
        <div className="w-8 h-8 border-2 border-sky-500 border-t-transparent rounded-full animate-spin mx-auto mb-3" />
        <p className="text-xs">Loading account risk profiles...</p>
      </div>
    );
  }

  const isIntradayActive = config.allowedTradingSegment === 'Both' || config.allowedTradingSegment === 'Intraday';
  const isDeliveryActive = config.allowedTradingSegment === 'Both' || config.allowedTradingSegment === 'Delivery';

  return (
    <div className="space-y-6 max-w-5xl mx-auto pb-16">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl sm:text-3xl font-extrabold text-white tracking-tight">
            Risk &amp; Execution Configuration
          </h1>
          <p className="text-xs sm:text-sm text-slate-400 mt-0.5">
            Independent parameters for <strong>Intraday (MIS)</strong> &amp; <strong>Delivery (CNC)</strong>, Segment Selection, and Approval Timeouts
          </p>
        </div>

        {accounts.length > 0 && (
          <div className="flex items-center gap-2">
            <label className="text-xs font-semibold text-slate-400 uppercase">Account:</label>
            <select
              value={selectedAccountId}
              onChange={(e) => handleAccountChange(e.target.value)}
              className="px-3 py-1.5 bg-slate-900 border border-slate-700 rounded-lg text-slate-100 text-xs font-medium focus:outline-none focus:ring-2 focus:ring-sky-500"
            >
              {accounts.map(a => (
                <option key={a.id} value={a.id}>
                  {a.accountName} ({a.tradingMode})
                </option>
              ))}
            </select>
          </div>
        )}
      </div>

      {successMsg && (
        <div className="p-4 rounded-xl bg-emerald-500/10 border border-emerald-500/30 text-emerald-400 text-sm flex items-center gap-2">
          <CheckCircle2 size={18} />
          <span>{successMsg}</span>
        </div>
      )}

      {errorMsg && (
        <div className="p-4 rounded-xl bg-rose-500/10 border border-rose-500/30 text-rose-400 text-sm flex items-center gap-2">
          <AlertTriangle size={18} />
          <span>{errorMsg}</span>
        </div>
      )}

      {/* Trading Mode Banner & Quick Toggle (Paper vs Live) */}
      <div className={`p-4 rounded-xl border flex flex-col sm:flex-row sm:items-center justify-between gap-4 ${
        selectedAccount?.tradingMode === 'Live'
          ? 'bg-rose-950/30 border-rose-600/40'
          : 'bg-emerald-950/20 border-emerald-600/30'
      }`}>
        <div className="flex items-center gap-3">
          <div className={`w-3 h-3 rounded-full animate-ping ${
            selectedAccount?.tradingMode === 'Live' ? 'bg-rose-500' : 'bg-emerald-500'
          }`} />
          <div>
            <div className="flex items-center gap-2">
              <span className="text-xs font-bold uppercase tracking-wider text-slate-400">Current Execution Mode:</span>
              <span className={`text-xs font-extrabold px-2 py-0.5 rounded-full ${
                selectedAccount?.tradingMode === 'Live'
                  ? 'bg-rose-500/20 text-rose-300 border border-rose-500/40'
                  : 'bg-emerald-500/20 text-emerald-300 border border-emerald-500/40'
              }`}>
                {selectedAccount?.tradingMode === 'Live' ? 'LIVE TRADING (Real Capital)' : 'PAPER TRADING (Simulated)'}
              </span>
            </div>
            <p className="text-[11px] text-slate-400 mt-0.5">
              {selectedAccount?.tradingMode === 'Live'
                ? 'Live orders will be submitted to your configured broker (Zerodha Kite).'
                : 'Safe simulation: Trades execute virtually with instant zero-capital settlement.'}
            </p>
          </div>
        </div>

        <button
          type="button"
          onClick={() => setShowModeModal(true)}
          className={`px-3.5 py-2 rounded-lg text-xs font-bold transition flex items-center gap-1.5 self-start sm:self-center ${
            selectedAccount?.tradingMode === 'Live'
              ? 'bg-emerald-600 hover:bg-emerald-500 text-white'
              : 'bg-amber-600 hover:bg-amber-500 text-white'
          }`}
        >
          <ArrowRightLeft size={14} />
          {selectedAccount?.tradingMode === 'Live' ? 'Switch to Paper Trading' : 'Switch to Live Trading'}
        </button>
      </div>

      <form onSubmit={handleSave} className="space-y-6">
        {/* Section 1: Today's Trading Focus & Segment Selector */}
        <div className="bg-slate-900 border border-slate-800 rounded-xl p-5 shadow-sm space-y-4">
          <div className="flex items-center gap-2.5 pb-3 border-b border-slate-800">
            <Layers size={20} className="text-sky-400" />
            <div>
              <h3 className="text-sm font-bold text-white uppercase tracking-wider">
                Select Today's Active Segment: Intraday, Delivery, or Both
              </h3>
              <p className="text-xs text-slate-400 mt-0.5">
                Controls which trade types our algorithmic scanners and AI advisor will execute today
              </p>
            </div>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 pt-1">
            {[
              { id: 'Both', label: 'Both (Intraday + Delivery)', badge: 'Recommended', desc: 'Scan & trade MIS intraday setups + CNC delivery accumulation' },
              { id: 'Intraday', label: 'Intraday Only (MIS)', badge: 'Auto Square-off 15:15', desc: 'Strictly same-day trades; never held overnight' },
              { id: 'Delivery', label: 'Delivery Only (CNC)', badge: 'Swing / Holdings', desc: 'Cash-and-carry swing trades & dip accumulation with overnight hold' }
            ].map(seg => (
              <label
                key={seg.id}
                className={`p-3.5 rounded-xl border cursor-pointer transition flex flex-col justify-between ${
                  config.allowedTradingSegment === seg.id
                    ? 'bg-sky-500/10 border-sky-500 text-white shadow-lg shadow-sky-500/10 ring-1 ring-sky-500'
                    : 'bg-slate-950 border-slate-800 text-slate-400 hover:border-slate-700'
                }`}
              >
                <div>
                  <div className="flex items-center justify-between mb-1.5">
                    <span className="text-xs font-bold text-slate-100">{seg.label}</span>
                    <input
                      type="radio"
                      name="allowedTradingSegment"
                      value={seg.id}
                      checked={config.allowedTradingSegment === seg.id}
                      onChange={() => setConfig({ ...config, allowedTradingSegment: seg.id as any })}
                      className="accent-sky-500 w-4 h-4"
                    />
                  </div>
                  <span className={`inline-block px-1.5 py-0.5 rounded text-[10px] font-bold mb-2 ${
                    config.allowedTradingSegment === seg.id ? 'bg-sky-500/20 text-sky-300' : 'bg-slate-800 text-slate-400'
                  }`}>
                    {seg.badge}
                  </span>
                </div>
                <p className="text-[11px] text-slate-400">{seg.desc}</p>
              </label>
            ))}
          </div>
        </div>

        {/* Section 2: SEPARATE CONFIGURATIONS FOR INTRADAY (MIS) */}
        <div className={`rounded-xl border p-5 shadow-sm space-y-4 transition ${
          isIntradayActive
            ? 'bg-slate-900 border-sky-500/40 ring-1 ring-sky-500/20'
            : 'bg-slate-900/60 border-slate-800 opacity-60'
        }`}>
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 pb-3 border-b border-slate-800">
            <div className="flex items-center gap-2.5">
              <div className="p-2 rounded-lg bg-sky-500/20 text-sky-400 border border-sky-500/30">
                <TrendingUp size={18} />
              </div>
              <div>
                <div className="flex items-center gap-2">
                  <h3 className="text-sm font-extrabold text-white uppercase tracking-wider">
                    Intraday (MIS) Configurations
                  </h3>
                  <span className={`px-2 py-0.5 rounded text-[10px] font-extrabold ${
                    isIntradayActive
                      ? 'bg-emerald-500/20 text-emerald-300 border border-emerald-500/40'
                      : 'bg-slate-800 text-slate-500'
                  }`}>
                    {isIntradayActive ? 'ACTIVE TODAY' : 'PAUSED TODAY'}
                  </span>
                </div>
                <p className="text-xs text-slate-400 mt-0.5">
                  Applied to intraday breakouts, 21-EMA pullbacks, and momentum setups. Auto square-off at 15:15 IST.
                </p>
              </div>
            </div>
            <span className="text-xs font-mono font-bold text-sky-400 bg-sky-950/60 px-2.5 py-1 rounded border border-sky-800/40 self-start sm:self-center">
              Segment: MIS
            </span>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-4 pt-1">
            {/* 1. Volume */}
            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1">
                1) Minimum Volume
              </label>
              <input
                type="number"
                min={0}
                step="any"
                value={config.intradayMinVolume ?? 20000}
                onChange={(e) => setConfig({ ...config, intradayMinVolume: e.target.value === '' ? 0 : Number(e.target.value) })}
                className="w-full px-3 py-2 bg-slate-950 border border-slate-700 rounded-lg text-slate-100 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500 font-mono"
                placeholder="20000"
              />
              <p className="text-[11px] text-slate-400 mt-1">Default: <strong>20,000</strong></p>
            </div>

            {/* 2. Stop Loss % */}
            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1">
                2) Stop Loss (%)
              </label>
              <input
                type="number"
                min={0.1}
                max={50}
                step="any"
                value={config.intradayStopLossPercent ?? 2.0}
                onChange={(e) => setConfig({ ...config, intradayStopLossPercent: e.target.value === '' ? 0 : Number(e.target.value) })}
                className="w-full px-3 py-2 bg-slate-950 border border-slate-700 rounded-lg text-slate-100 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500 font-mono"
                placeholder="2.0"
              />
              <p className="text-[11px] text-slate-400 mt-1">Default: <strong>2%</strong></p>
            </div>

            {/* 3. Target % */}
            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1">
                3) Target (%)
              </label>
              <input
                type="number"
                min={0.1}
                max={100}
                step="any"
                value={config.intradayTargetPercent ?? 5.0}
                onChange={(e) => setConfig({ ...config, intradayTargetPercent: e.target.value === '' ? 0 : Number(e.target.value) })}
                className="w-full px-3 py-2 bg-slate-950 border border-slate-700 rounded-lg text-slate-100 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500 font-mono"
                placeholder="5.0"
              />
              <p className="text-[11px] text-slate-400 mt-1">Default: <strong>5%</strong></p>
            </div>

            {/* 4. Max Capital Allocation */}
            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1">
                4) Max Capital Allocation (₹)
              </label>
              <input
                type="number"
                min={0}
                step="any"
                value={config.intradayMaxCapitalAllocation ?? 500000}
                onChange={(e) => setConfig({ ...config, intradayMaxCapitalAllocation: e.target.value === '' ? 0 : Number(e.target.value) })}
                className="w-full px-3 py-2 bg-slate-950 border border-slate-700 rounded-lg text-slate-100 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500 font-mono"
                placeholder="500000"
              />
              <p className="text-[11px] text-slate-400 mt-1">Total Intraday Fund Pool</p>
            </div>

            {/* 5. Max Single Order Value */}
            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1">
                5) Max Single Order Value (₹)
              </label>
              <input
                type="number"
                min={0}
                step="any"
                value={config.intradayMaxOrderValue ?? 50000}
                onChange={(e) => setConfig({ ...config, intradayMaxOrderValue: e.target.value === '' ? 0 : Number(e.target.value) })}
                className="w-full px-3 py-2 bg-slate-950 border border-slate-700 rounded-lg text-slate-100 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500 font-mono"
                placeholder="50000"
              />
              <p className="text-[11px] text-slate-400 mt-1">Max per single MIS order</p>
            </div>

            {/* 6. Daily Loss Limit */}
            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1">
                6) Daily Loss Limit (₹)
              </label>
              <input
                type="number"
                min={0}
                step="any"
                value={config.intradayDailyLossLimit ?? 10000}
                onChange={(e) => setConfig({ ...config, intradayDailyLossLimit: e.target.value === '' ? 0 : Number(e.target.value) })}
                className="w-full px-3 py-2 bg-slate-950 border border-slate-700 rounded-lg text-slate-100 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500 font-mono"
                placeholder="10000"
              />
              <p className="text-[11px] text-slate-400 mt-1">Intraday Drawdown Circuit</p>
            </div>

            {/* 7. Max Open Positions */}
            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1">
                7) Max Open Positions
              </label>
              <input
                type="number"
                min={1}
                max={50}
                step="any"
                value={config.intradayMaxPositions ?? 5}
                onChange={(e) => setConfig({ ...config, intradayMaxPositions: e.target.value === '' ? 0 : Number(e.target.value) })}
                className="w-full px-3 py-2 bg-slate-950 border border-slate-700 rounded-lg text-slate-100 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500 font-mono"
                placeholder="5"
              />
              <p className="text-[11px] text-slate-400 mt-1">Max active intraday trades</p>
            </div>
          </div>
        </div>

        {/* Section 3: SEPARATE CONFIGURATIONS FOR DELIVERY (CNC) */}
        <div className={`rounded-xl border p-5 shadow-sm space-y-4 transition ${
          isDeliveryActive
            ? 'bg-slate-900 border-indigo-500/40 ring-1 ring-indigo-500/20'
            : 'bg-slate-900/60 border-slate-800 opacity-60'
        }`}>
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 pb-3 border-b border-slate-800">
            <div className="flex items-center gap-2.5">
              <div className="p-2 rounded-lg bg-indigo-500/20 text-indigo-400 border border-indigo-500/30">
                <Briefcase size={18} />
              </div>
              <div>
                <div className="flex items-center gap-2">
                  <h3 className="text-sm font-extrabold text-white uppercase tracking-wider">
                    Delivery (CNC) Configurations
                  </h3>
                  <span className={`px-2 py-0.5 rounded text-[10px] font-extrabold ${
                    isDeliveryActive
                      ? 'bg-emerald-500/20 text-emerald-300 border border-emerald-500/40'
                      : 'bg-slate-800 text-slate-500'
                  }`}>
                    {isDeliveryActive ? 'ACTIVE TODAY' : 'PAUSED TODAY'}
                  </span>
                </div>
                <p className="text-xs text-slate-400 mt-0.5">
                  Applied to swing trading, dip accumulation on quality stocks, and overnight portfolio holdings.
                </p>
              </div>
            </div>
            <span className="text-xs font-mono font-bold text-indigo-400 bg-indigo-950/60 px-2.5 py-1 rounded border border-indigo-800/40 self-start sm:self-center">
              Segment: CNC
            </span>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-4 pt-1">
            {/* 1. Volume */}
            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1">
                1) Minimum Volume
              </label>
              <input
                type="number"
                min={0}
                step="any"
                value={config.deliveryMinVolume ?? 50000}
                onChange={(e) => setConfig({ ...config, deliveryMinVolume: e.target.value === '' ? 0 : Number(e.target.value) })}
                className="w-full px-3 py-2 bg-slate-950 border border-slate-700 rounded-lg text-slate-100 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 font-mono"
                placeholder="50000"
              />
              <p className="text-[11px] text-slate-400 mt-1">Default: <strong>50,000</strong></p>
            </div>

            {/* 2. Stop Loss % */}
            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1">
                2) Stop Loss (%)
              </label>
              <input
                type="number"
                min={0.1}
                max={50}
                step="any"
                value={config.deliveryStopLossPercent ?? 4.0}
                onChange={(e) => setConfig({ ...config, deliveryStopLossPercent: e.target.value === '' ? 0 : Number(e.target.value) })}
                className="w-full px-3 py-2 bg-slate-950 border border-slate-700 rounded-lg text-slate-100 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 font-mono"
                placeholder="4.0"
              />
              <p className="text-[11px] text-slate-400 mt-1">Default: <strong>4%</strong></p>
            </div>

            {/* 3. Target % */}
            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1">
                3) Target (%)
              </label>
              <input
                type="number"
                min={0.1}
                max={100}
                step="any"
                value={config.deliveryTargetPercent ?? 10.0}
                onChange={(e) => setConfig({ ...config, deliveryTargetPercent: e.target.value === '' ? 0 : Number(e.target.value) })}
                className="w-full px-3 py-2 bg-slate-950 border border-slate-700 rounded-lg text-slate-100 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 font-mono"
                placeholder="10.0"
              />
              <p className="text-[11px] text-slate-400 mt-1">Default: <strong>10%</strong></p>
            </div>

            {/* 4. Max Capital Allocation */}
            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1">
                4) Max Capital Allocation (₹)
              </label>
              <input
                type="number"
                min={0}
                step="any"
                value={config.deliveryMaxCapitalAllocation ?? 500000}
                onChange={(e) => setConfig({ ...config, deliveryMaxCapitalAllocation: e.target.value === '' ? 0 : Number(e.target.value) })}
                className="w-full px-3 py-2 bg-slate-950 border border-slate-700 rounded-lg text-slate-100 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 font-mono"
                placeholder="500000"
              />
              <p className="text-[11px] text-slate-400 mt-1">Total Delivery Fund Pool</p>
            </div>

            {/* 5. Max Single Order Value */}
            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1">
                5) Max Single Order Value (₹)
              </label>
              <input
                type="number"
                min={0}
                step="any"
                value={config.deliveryMaxOrderValue ?? 100000}
                onChange={(e) => setConfig({ ...config, deliveryMaxOrderValue: e.target.value === '' ? 0 : Number(e.target.value) })}
                className="w-full px-3 py-2 bg-slate-950 border border-slate-700 rounded-lg text-slate-100 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 font-mono"
                placeholder="100000"
              />
              <p className="text-[11px] text-slate-400 mt-1">Max per delivery buy ticket</p>
            </div>

            {/* 6. Daily Loss Limit */}
            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1">
                6) Daily Loss Limit (₹)
              </label>
              <input
                type="number"
                min={0}
                step="any"
                value={config.deliveryDailyLossLimit ?? 20000}
                onChange={(e) => setConfig({ ...config, deliveryDailyLossLimit: e.target.value === '' ? 0 : Number(e.target.value) })}
                className="w-full px-3 py-2 bg-slate-950 border border-slate-700 rounded-lg text-slate-100 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 font-mono"
                placeholder="20000"
              />
              <p className="text-[11px] text-slate-400 mt-1">Portfolio Swing Circuit</p>
            </div>

            {/* 7. Max Open Positions */}
            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1">
                7) Max Open Positions
              </label>
              <input
                type="number"
                min={1}
                max={100}
                step="any"
                value={config.deliveryMaxPositions ?? 10}
                onChange={(e) => setConfig({ ...config, deliveryMaxPositions: e.target.value === '' ? 0 : Number(e.target.value) })}
                className="w-full px-3 py-2 bg-slate-950 border border-slate-700 rounded-lg text-slate-100 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 font-mono"
                placeholder="10"
              />
              <p className="text-[11px] text-slate-400 mt-1">Max accumulated holdings</p>
            </div>
          </div>
        </div>

        {/* Section 4: BUY Signal Rejection Window & Execution Timeout */}
        <div className="bg-slate-900 border border-slate-800 rounded-xl p-5 shadow-sm space-y-4">
          <div className="flex items-center gap-2.5 pb-3 border-b border-slate-800">
            <Clock size={20} className="text-amber-400" />
            <div>
              <h3 className="text-sm font-bold text-white uppercase tracking-wider">
                BUY Signal Rejection Window &amp; Execution Timeout
              </h3>
              <p className="text-xs text-slate-400 mt-0.5">
                Critical window allowed for human trader to manually cancel/reject a generated BUY signal
              </p>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-5 pt-1">
            <div>
              <div className="flex items-center justify-between mb-1.5">
                <label className="text-xs font-semibold uppercase tracking-wider text-slate-300">
                  Rejection Window Duration (Seconds)
                </label>
                <span className="text-sm font-mono font-bold text-amber-400">
                  {config.approvalDurationSeconds}s
                </span>
              </div>
              <input
                type="range"
                min={1}
                max={30}
                step={1}
                value={config.approvalDurationSeconds}
                onChange={(e) => setConfig({ ...config, approvalDurationSeconds: Number(e.target.value) })}
                className="w-full accent-amber-400 h-2 bg-slate-950 rounded-lg cursor-pointer"
              />
              <p className="text-[11px] text-slate-500 mt-1.5 flex items-center gap-1">
                <Info size={12} /> Dynamic rejection window before order submit. Can be set between 1s and 30s.
              </p>
            </div>

            <div className="flex flex-col justify-center space-y-2">
              <label className="flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  checked={config.autoExecuteOnTimeout}
                  onChange={(e) => setConfig({ ...config, autoExecuteOnTimeout: e.target.checked })}
                  className="rounded bg-slate-950 border-slate-700 text-sky-500 focus:ring-0 w-4 h-4"
                />
                <span className="text-xs font-medium text-slate-200">
                  Auto-Execute on Timeout (Proceed to Order if Not Rejected)
                </span>
              </label>
              <p className="text-[11px] text-slate-400 pl-6">
                If checked, when the countdown expires without rejection, the engine will route the BUY order to the broker.
              </p>
            </div>
          </div>
        </div>

        {/* Section 5: Kill Switch & Circuit Breaker */}
        <div className="bg-slate-900 border border-slate-800 rounded-xl p-5 shadow-sm space-y-4">
          <div className="flex items-center justify-between pb-3 border-b border-slate-800">
            <div className="flex items-center gap-2.5">
              <Power size={20} className={config.killSwitchActive ? 'text-rose-400' : 'text-slate-400'} />
              <div>
                <h3 className="text-sm font-bold text-white uppercase tracking-wider">
                  Account Emergency Kill Switch
                </h3>
                <p className="text-xs text-slate-400 mt-0.5">
                  Instantly halts new orders for this account and prevents strategy execution
                </p>
              </div>
            </div>

            <button
              type="button"
              onClick={() => setConfig({ ...config, killSwitchActive: !config.killSwitchActive })}
              className={`px-4 py-2 rounded-lg text-xs font-bold transition flex items-center gap-1.5 ${
                config.killSwitchActive
                  ? 'bg-rose-600 hover:bg-rose-500 text-white shadow-lg shadow-rose-600/30'
                  : 'bg-slate-800 hover:bg-slate-700 text-slate-300 border border-slate-700'
              }`}
            >
              <Power size={14} />
              {config.killSwitchActive ? 'KILL SWITCH ENGAGED' : 'Disarmed (Normal)'}
            </button>
          </div>
        </div>

        {/* Save Actions */}
        <div className="flex items-center justify-end gap-3 pt-2">
          <button
            type="submit"
            disabled={isSaving}
            className="flex items-center gap-2 px-8 py-3 bg-gradient-to-r from-sky-500 to-indigo-600 hover:from-sky-400 hover:to-indigo-500 text-white text-sm font-bold rounded-xl shadow-lg shadow-sky-500/25 transition disabled:opacity-50"
          >
            <Save size={18} />
            {isSaving ? 'Saving Configurations...' : 'Save All Intraday & Delivery Rules'}
          </button>
        </div>
      </form>

      {/* Confirmation Modal for Switching Trading Mode */}
      {showModeModal && selectedAccount && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/75 backdrop-blur-sm">
          <div className="bg-slate-900 border border-slate-800 rounded-2xl max-w-md w-full p-6 shadow-2xl space-y-4">
            <div className="flex items-center justify-between pb-3 border-b border-slate-800">
              <div className="flex items-center gap-2.5 text-amber-400">
                <AlertTriangle size={22} />
                <h3 className="font-bold text-white text-base">
                  Confirm Trading Mode Switch
                </h3>
              </div>
              <button
                type="button"
                onClick={() => setShowModeModal(false)}
                className="text-slate-400 hover:text-white"
              >
                <XCircle size={20} />
              </button>
            </div>

            <div className="space-y-3 text-xs text-slate-300 leading-relaxed">
              <p>
                You are about to switch account <span className="font-bold text-white">{selectedAccount.accountName}</span> from{' '}
                <span className="font-bold text-sky-400">{selectedAccount.tradingMode} Trading</span> to{' '}
                <span className={`font-bold ${selectedAccount.tradingMode === 'Live' ? 'text-emerald-400' : 'text-rose-400'}`}>
                  {selectedAccount.tradingMode === 'Live' ? 'Paper Trading' : 'Live Trading'}
                </span>.
              </p>

              {selectedAccount.tradingMode === 'Paper' && (
                <div className="p-3 rounded-lg bg-rose-500/10 border border-rose-500/30 text-rose-300 space-y-1">
                  <div className="font-bold flex items-center gap-1.5">
                    <ShieldAlert size={14} /> LIVE TRADING WARNING:
                  </div>
                  <div>
                    Real capital will be at stake. When signals trigger and are approved, real orders will be placed at your broker. Auto-execute on timeout will be turned off for extra safety.
                  </div>
                </div>
              )}

              {selectedAccount.tradingMode === 'Live' && (
                <div className="p-3 rounded-lg bg-emerald-500/10 border border-emerald-500/30 text-emerald-300">
                  Switching to Paper Trading will simulate all executions safely without routing to the broker or risking actual funds.
                </div>
              )}
            </div>

            <div className="flex items-center justify-end gap-3 pt-3 border-t border-slate-800">
              <button
                type="button"
                onClick={() => setShowModeModal(false)}
                className="px-4 py-2 rounded-lg bg-slate-800 text-slate-300 hover:bg-slate-700 text-xs font-semibold"
              >
                Cancel
              </button>
              <button
                type="button"
                disabled={isTogglingMode}
                onClick={handleToggleTradingMode}
                className={`px-4 py-2 rounded-lg text-xs font-bold text-white transition ${
                  selectedAccount.tradingMode === 'Live'
                    ? 'bg-emerald-600 hover:bg-emerald-500'
                    : 'bg-rose-600 hover:bg-rose-500'
                }`}
              >
                {isTogglingMode ? 'Switching...' : `Confirm Switch to ${selectedAccount.tradingMode === 'Live' ? 'Paper' : 'Live'}`}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
