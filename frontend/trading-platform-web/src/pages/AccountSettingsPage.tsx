import React, { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import { 
  Sliders, 
  ShieldAlert, 
  Clock, 
  AlertTriangle, 
  CheckCircle2, 
  Save, 
  Power,
  Info
} from 'lucide-react';
import { apiClient } from '../api/client';
import type { ApiResponse, TradingAccountDto, AccountConfigurationDto } from '../types';

export const AccountSettingsPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const requestedAccountId = searchParams.get('accountId');

  const [accounts, setAccounts] = useState<TradingAccountDto[]>([]);
  const [selectedAccountId, setSelectedAccountId] = useState<string>('');
  const [config, setConfig] = useState<AccountConfigurationDto>({
    maxCapitalAllocation: 500000,
    maxOrderValue: 100000,
    maxPositions: 5,
    maxRiskPerTradePercent: 2.0,
    dailyLossLimit: 10000,
    maxExposurePerStockPercent: 25.0,
    maxTradesPerDay: 20,
    stopLossRequired: true,
    approvalDurationSeconds: 2,
    autoExecuteOnTimeout: true,
    killSwitchActive: false,
    tradingScheduleStart: '09:15:00',
    tradingScheduleEnd: '15:30:00',
    intradaySquareOffTime: '15:15:00',
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
          if (match.configuration) {
            setConfig(match.configuration);
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
    if (acc?.configuration) {
      setConfig(acc.configuration);
    }
    setSuccessMsg(null);
    setErrorMsg(null);
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
        setConfig(res.data.data);
        setSuccessMsg('Account risk & execution configuration saved successfully.');
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

  return (
    <div className="space-y-6 max-w-4xl mx-auto">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl sm:text-3xl font-extrabold text-white tracking-tight">
            Risk &amp; Execution Configuration
          </h1>
          <p className="text-xs sm:text-sm text-slate-400 mt-0.5">
            19-point risk verification rules, capital limits, and approval countdowns
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

      {/* Account Safety Warning Banner if Live */}
      {selectedAccount?.tradingMode === 'Live' && (
        <div className="p-4 rounded-xl bg-rose-500/10 border border-rose-500/30 text-rose-300 text-xs flex items-center gap-3">
          <ShieldAlert size={20} className="shrink-0 text-rose-400" />
          <div>
            <span className="font-bold">Live Trading Account Warning:</span> Real capital is at risk. Auto-execute on timeout is disabled by default for live trading accounts to ensure every execution requires intentional human oversight.
          </div>
        </div>
      )}

      <form onSubmit={handleSave} className="space-y-6">
        {/* Section 1: 2-Second BUY Rejection & Timeout Policy */}
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
                <Info size={12} /> Default is strictly 2 seconds as specified in production requirements.
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

        {/* Section 2: Capital & Position Limits */}
        <div className="bg-slate-900 border border-slate-800 rounded-xl p-5 shadow-sm space-y-4">
          <div className="flex items-center gap-2.5 pb-3 border-b border-slate-800">
            <Sliders size={20} className="text-sky-400" />
            <div>
              <h3 className="text-sm font-bold text-white uppercase tracking-wider">
                Capital Allocation &amp; Trade Exposure
              </h3>
              <p className="text-xs text-slate-400 mt-0.5">
                Enforced by independent pre-order risk gate before order reaches broker
              </p>
            </div>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-4">
            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1">
                Max Capital Allocation (₹)
              </label>
              <input
                type="number"
                min={0}
                step="any"
                value={config.maxCapitalAllocation}
                onChange={(e) => setConfig({ ...config, maxCapitalAllocation: Number(e.target.value) })}
                className="w-full px-3 py-2 bg-slate-950 border border-slate-700 rounded-lg text-slate-100 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500 font-mono"
              />
            </div>

            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1">
                Max Single Order Value (₹)
              </label>
              <input
                type="number"
                min={0}
                step="any"
                value={config.maxOrderValue}
                onChange={(e) => setConfig({ ...config, maxOrderValue: Number(e.target.value) })}
                className="w-full px-3 py-2 bg-slate-950 border border-slate-700 rounded-lg text-slate-100 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500 font-mono"
              />
            </div>

            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1">
                Daily Loss Limit (₹)
              </label>
              <input
                type="number"
                min={0}
                step="any"
                value={config.dailyLossLimit}
                onChange={(e) => setConfig({ ...config, dailyLossLimit: Number(e.target.value) })}
                className="w-full px-3 py-2 bg-slate-950 border border-slate-700 rounded-lg text-slate-100 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500 font-mono"
              />
            </div>

            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1">
                Max Open Positions
              </label>
              <input
                type="number"
                min={1}
                max={50}
                step={1}
                value={config.maxPositions}
                onChange={(e) => setConfig({ ...config, maxPositions: Number(e.target.value) })}
                className="w-full px-3 py-2 bg-slate-950 border border-slate-700 rounded-lg text-slate-100 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500 font-mono"
              />
            </div>

            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1">
                Max Risk Per Trade (%)
              </label>
              <input
                type="number"
                min={0.1}
                max={100.0}
                step="any"
                value={config.maxRiskPerTradePercent}
                onChange={(e) => setConfig({ ...config, maxRiskPerTradePercent: Number(e.target.value) })}
                className="w-full px-3 py-2 bg-slate-950 border border-slate-700 rounded-lg text-slate-100 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500 font-mono"
              />
            </div>

            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1">
                Max Exposure Per Stock (%)
              </label>
              <input
                type="number"
                min={1.0}
                max={100.0}
                step="any"
                value={config.maxExposurePerStockPercent}
                onChange={(e) => setConfig({ ...config, maxExposurePerStockPercent: Number(e.target.value) })}
                className="w-full px-3 py-2 bg-slate-950 border border-slate-700 rounded-lg text-slate-100 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500 font-mono"
              />
            </div>
          </div>
        </div>

        {/* Section 3: Kill Switch & Circuit Breaker */}
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
            className="flex items-center gap-2 px-6 py-2.5 bg-gradient-to-r from-sky-500 to-indigo-600 hover:from-sky-400 hover:to-indigo-500 text-white text-sm font-semibold rounded-lg shadow-md shadow-sky-500/20 transition disabled:opacity-50"
          >
            <Save size={16} />
            {isSaving ? 'Saving Configuration...' : 'Save Risk Rules'}
          </button>
        </div>
      </form>
    </div>
  );
};
