import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { 
  Plus, 
  Layers, 
  ShieldCheck, 
  ExternalLink, 
  Key, 
  Lock, 
  AlertCircle, 
  CheckCircle2, 
  RefreshCw,
  X,
  HelpCircle,
  Zap,
  Trash2,
  ArrowRightLeft
} from 'lucide-react';
import { apiClient } from '../api/client';
import type { 
  ApiResponse, 
  BrokerProviderDto, 
  BrokerConnectionDto, 
  TradingMode 
} from '../types';

export const BrokersPage: React.FC = () => {
  const [providers, setProviders] = useState<BrokerProviderDto[]>([]);
  const [connections, setConnections] = useState<BrokerConnectionDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);

  // New Connection Modal State
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [selectedProviderId, setSelectedProviderId] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [apiKey, setApiKey] = useState('');
  const [apiSecret, setApiSecret] = useState('');
  const [tradingMode, setTradingMode] = useState<TradingMode>('Paper');
  const [initialCapital, setInitialCapital] = useState(500000);

  // Manual Token Modal State
  const [isTokenModalOpen, setIsTokenModalOpen] = useState(false);
  const [tokenConnectionId, setTokenConnectionId] = useState('');
  const [manualAccessToken, setManualAccessToken] = useState('');
  const [isSubmittingToken, setIsSubmittingToken] = useState(false);

  const handleSelectProvider = (providerId: string) => {
    setSelectedProviderId(providerId);
    const prov = providers.find((p) => p.id === providerId);
    if (prov) {
      if (prov.code === 'Mock') {
        setDisplayName('Mock Simulator (Paper)');
        setApiKey('mock_api_key_sandbox');
        setApiSecret('mock_api_secret_sandbox');
        setTradingMode('Paper');
      } else if (prov.code === 'Upstox') {
        if (!displayName || displayName.startsWith('Mock') || displayName.startsWith('Zerodha')) {
          setDisplayName('Upstox Primary');
        }
        if (apiKey.startsWith('mock_')) setApiKey('');
        if (apiSecret.startsWith('mock_')) setApiSecret('');
      } else if (prov.code === 'Zerodha') {
        if (!displayName || displayName.startsWith('Mock') || displayName.startsWith('Upstox')) {
          setDisplayName('Zerodha Kite Primary');
        }
        if (apiKey.startsWith('mock_')) setApiKey('');
        if (apiSecret.startsWith('mock_')) setApiSecret('');
      }
    }
  };

  const openModal = () => {
    setIsModalOpen(true);
    if (providers.length > 0) {
      const targetId = selectedProviderId || providers[0].id;
      handleSelectProvider(targetId);
    }
  };

  const fetchData = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const [provRes, connRes] = await Promise.all([
        apiClient.get<ApiResponse<BrokerProviderDto[]>>('/brokers/providers'),
        apiClient.get<ApiResponse<BrokerConnectionDto[]>>('/brokers/connections'),
      ]);

      if (provRes.data.success && provRes.data.data) {
        setProviders(provRes.data.data);
        if (provRes.data.data.length > 0 && !selectedProviderId) {
          setSelectedProviderId(provRes.data.data[0].id);
        }
      }
      if (connRes.data.success && connRes.data.data) {
        setConnections(connRes.data.data);
      }
    } catch (err: any) {
      setError('Failed to fetch broker data from server.');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, []);

  const handleCreateConnection = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);
    setError(null);
    setSuccessMsg(null);

    try {
      const res = await apiClient.post<ApiResponse<BrokerConnectionDto>>('/brokers/connections', {
        brokerProviderId: selectedProviderId,
        displayName,
        apiKey,
        apiSecret,
        tradingMode,
        initialCapital,
      });

      if (res.data.success) {
        setIsModalOpen(false);
        setDisplayName('');
        setApiKey('');
        setApiSecret('');
        setSuccessMsg(
          tradingMode === 'Paper'
            ? 'Paper trading broker connection established and Engine Ready!'
            : 'Broker connection added. Please authorize via OAuth or manual token.'
        );
        await fetchData();
      } else {
        setError(res.data.message || 'Failed to establish connection.');
      }
    } catch (err: any) {
      setError(
        err.response?.data?.message ||
        err.response?.data?.errors?.[0] ||
        'Failed to connect broker.'
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleOAuthLogin = async (connectionId: string) => {
    setError(null);
    try {
      const redirectUri = window.location.origin + '/brokers/callback';
      localStorage.setItem('tp_pending_oauth_connection_id', connectionId);
      const res = await apiClient.post<ApiResponse<{ authorizationUrl?: string; authUrl?: string }>>(
        `/brokers/connections/${connectionId}/auth-url`,
        { redirectUri }
      );
      const authUrl = res.data.data?.authorizationUrl || (res.data.data as any)?.authUrl;
      if (res.data.success && authUrl) {
        window.location.href = authUrl;
      } else {
        setError(res.data.message || 'Broker authorization URL could not be generated.');
      }
    } catch (err: any) {
      setError(
        err.response?.data?.message ||
        'Failed to generate OAuth redirect URL. If testing in paper mode, you can click "Activate Paper Engine" instead.'
      );
    }
  };

  const handleActivatePaper = async (connectionId: string) => {
    setIsLoading(true);
    setError(null);
    setSuccessMsg(null);
    try {
      const res = await apiClient.post<ApiResponse<BrokerConnectionDto>>(
        `/brokers/connections/${connectionId}/activate-paper`
      );
      if (res.data.success) {
        setSuccessMsg('Paper trading activated successfully! Engine is now Ready.');
        await fetchData();
      } else {
        setError(res.data.message || 'Failed to activate paper trading engine.');
      }
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to activate paper trading.');
    } finally {
      setIsLoading(false);
    }
  };

  const handleDeleteConnection = async (connectionId: string, name: string) => {
    if (!window.confirm(`Are you sure you want to remove broker connection "${name}"?`)) return;
    setError(null);
    try {
      await apiClient.delete(`/brokers/connections/${connectionId}`);
      setSuccessMsg(`Broker connection "${name}" removed.`);
      await fetchData();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to remove connection.');
    }
  };

  const handleToggleConnectionMode = async (connectionId: string, currentMode: 'Paper' | 'Live') => {
    const newMode = currentMode === 'Paper' ? 'Live' : 'Paper';
    if (newMode === 'Live') {
      const confirmed = window.confirm(
        '⚠️ SWITCH BROKER TO LIVE TRADING?\n\n' +
        'Real capital will be at stake. When buy/sell signals are approved, real orders will be placed on Zerodha Kite.\n\n' +
        'Are you sure you want to switch to Live mode?'
      );
      if (!confirmed) return;
    }

    setIsLoading(true);
    setError(null);
    setSuccessMsg(null);
    try {
      const res = await apiClient.post<ApiResponse<BrokerConnectionDto>>(
        `/brokers/connections/${connectionId}/toggle-mode`,
        { tradingMode: newMode }
      );
      if (res.data.success) {
        setSuccessMsg(`Switched broker connection to ${newMode} Trading mode!`);
        await fetchData();
        window.dispatchEvent(new Event('broker-state-changed'));
      } else {
        setError(res.data.message || 'Failed to switch trading mode.');
      }
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to switch trading mode.');
    } finally {
      setIsLoading(false);
    }
  };

  const handleToggleActive = async (connectionId: string) => {
    setIsLoading(true);
    setError(null);
    setSuccessMsg(null);
    try {
      const res = await apiClient.post<ApiResponse<BrokerConnectionDto>>(
        `/brokers/connections/${connectionId}/toggle-active`
      );
      if (res.data.success && res.data.data) {
        setSuccessMsg(res.data.message || 'Broker status updated.');
        await fetchData();
        window.dispatchEvent(new Event('broker-state-changed'));
      } else {
        setError(res.data.message || 'Failed to toggle broker status.');
      }
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to toggle broker status.');
    } finally {
      setIsLoading(false);
    }
  };

  const openTokenModal = (connectionId: string) => {
    setTokenConnectionId(connectionId);
    setManualAccessToken('');
    setIsTokenModalOpen(true);
  };

  const handleApplyManualToken = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!manualAccessToken.trim()) return;
    setIsSubmittingToken(true);
    setError(null);
    try {
      const res = await apiClient.post<ApiResponse<BrokerConnectionDto>>(
        `/brokers/connections/${tokenConnectionId}/manual-token`,
        { accessToken: manualAccessToken.trim() }
      );
      if (res.data.success) {
        setIsTokenModalOpen(false);
        setManualAccessToken('');
        setSuccessMsg('Access token saved successfully! Engine is now Ready.');
        await fetchData();
      } else {
        setError(res.data.message || 'Failed to apply access token.');
      }
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to apply manual token.');
    } finally {
      setIsSubmittingToken(false);
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl sm:text-3xl font-extrabold text-white tracking-tight">
            Broker Integrations
          </h1>
          <p className="text-xs sm:text-sm text-slate-400 mt-0.5">
            Connect Upstox, Zerodha Kite Connect, or our high-fidelity Mock Engine
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
          <button
            onClick={openModal}
            className="flex items-center gap-2 px-4 py-2 bg-gradient-to-r from-sky-500 to-indigo-600 hover:from-sky-400 hover:to-indigo-500 text-white rounded-lg text-xs font-semibold shadow-md shadow-sky-500/20 transition"
          >
            <Plus size={16} />
            Connect New Broker
          </button>
        </div>
      </div>

      {/* Help Banner */}
      <div className="p-3.5 rounded-xl bg-sky-950/40 border border-sky-800/40 flex flex-col sm:flex-row sm:items-center justify-between gap-3 text-xs">
        <div className="flex items-center gap-2.5 text-sky-300">
          <HelpCircle size={18} className="shrink-0 text-sky-400" />
          <span>
            Need help finding your <strong>API Key</strong>, <strong>API Secret</strong>, or setting up <strong>OAuth</strong>? Follow our step-by-step walkthrough.
          </span>
        </div>
        <Link
          to="/help"
          className="inline-flex items-center gap-1 font-bold text-sky-400 hover:text-sky-300 underline shrink-0"
        >
          View Broker Setup Guide &rarr;
        </Link>
      </div>

      {error && (
        <div className="p-4 rounded-xl bg-rose-500/10 border border-rose-500/30 text-rose-400 text-sm flex items-start justify-between gap-2.5">
          <div className="flex items-start gap-2.5">
            <AlertCircle size={18} className="shrink-0 mt-0.5" />
            <span>{error}</span>
          </div>
          <button onClick={() => setError(null)} className="text-rose-400 hover:text-rose-200">
            <X size={16} />
          </button>
        </div>
      )}

      {successMsg && (
        <div className="p-4 rounded-xl bg-emerald-500/10 border border-emerald-500/30 text-emerald-400 text-sm flex items-start justify-between gap-2.5">
          <div className="flex items-start gap-2.5">
            <CheckCircle2 size={18} className="shrink-0 mt-0.5" />
            <span>{successMsg}</span>
          </div>
          <button onClick={() => setSuccessMsg(null)} className="text-emerald-400 hover:text-emerald-200">
            <X size={16} />
          </button>
        </div>
      )}

      {/* Connected Brokers Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
        {connections.map((conn) => (
          <div
            key={conn.id}
            className={`bg-slate-900 border rounded-xl p-5 flex flex-col justify-between shadow-md relative overflow-hidden transition ${
              conn.isActive ? 'border-slate-800' : 'border-slate-800/50 opacity-60'
            }`}
          >
            {/* Top decorative badge & Actions */}
            <div className="flex items-center justify-between gap-2">
              <div className="flex items-center gap-2 flex-wrap">
                <span className="text-xs font-mono font-bold px-2 py-0.5 rounded bg-slate-800 text-slate-300 border border-slate-700">
                  {conn.providerCode}
                </span>
                <button
                  type="button"
                  onClick={() => conn.providerCode !== 'Mock' && handleToggleConnectionMode(conn.id, conn.tradingMode)}
                  disabled={conn.providerCode === 'Mock'}
                  className={`px-2.5 py-0.5 rounded-full text-[11px] font-semibold flex items-center gap-1.5 transition ${
                    conn.tradingMode === 'Paper'
                      ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/30 hover:bg-emerald-500/20'
                      : 'bg-rose-500/10 text-rose-400 border border-rose-500/30 hover:bg-rose-500/20'
                  } ${conn.providerCode !== 'Mock' ? 'cursor-pointer' : 'cursor-default'}`}
                  title={conn.providerCode !== 'Mock' ? "Click to switch between Paper and Live mode" : "Mock Engine is always Paper"}
                >
                  <span className={`w-1.5 h-1.5 rounded-full ${conn.tradingMode === 'Paper' ? 'bg-emerald-400' : 'bg-rose-400'}`} />
                  {conn.tradingMode} Trading
                  {conn.providerCode !== 'Mock' && <ArrowRightLeft size={11} className="opacity-70 ml-0.5" />}
                </button>
              </div>

              <div className="flex items-center gap-1.5">
                <button
                  type="button"
                  onClick={() => handleToggleActive(conn.id)}
                  className={`px-2.5 py-0.5 rounded-full text-[11px] font-bold border transition cursor-pointer ${
                    conn.isActive
                      ? 'bg-sky-500/15 text-sky-300 border-sky-500/30 hover:bg-sky-500/25'
                      : 'bg-slate-800 text-slate-400 border-slate-700 hover:bg-slate-700'
                  }`}
                  title={conn.isActive ? 'Click to disable this broker across the entire app' : 'Click to enable this broker'}
                >
                  {conn.isActive ? '● Enabled' : '○ Disabled'}
                </button>
                {conn.providerCode !== 'Mock' && (
                  <button
                    onClick={() => handleDeleteConnection(conn.id, conn.displayName)}
                    title="Remove broker connection"
                    className="p-1 text-slate-500 hover:text-rose-400 hover:bg-slate-800 rounded transition"
                  >
                    <Trash2 size={15} />
                  </button>
                )}
              </div>
            </div>

            <div className="mt-4">
              <h3 className="text-lg font-bold text-white tracking-tight">{conn.displayName}</h3>
              <p className="text-xs text-slate-400 mt-0.5">{conn.providerName}</p>

              <div className="mt-4 space-y-2 text-xs text-slate-300 bg-slate-950/60 p-3 rounded-lg border border-slate-800/80">
                <div className="flex items-center justify-between">
                  <span className="text-slate-500">API Key:</span>
                  <span className="font-mono text-slate-300">{conn.maskedApiKey || '—'}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-slate-500">Authentication:</span>
                  {(() => {
                    const isExpired = conn.providerCode !== 'Mock' && !conn.isAuthenticated && Boolean(conn.tokenExpiresAtUtc);
                    return (
                      <span className={`inline-flex items-center gap-1 font-semibold ${
                        conn.isAuthenticated ? 'text-emerald-400' : isExpired ? 'text-rose-400' : 'text-amber-400'
                      }`}>
                        {conn.isAuthenticated ? <CheckCircle2 size={13} /> : <AlertCircle size={13} />}
                        {conn.isAuthenticated ? 'Authenticated' : isExpired ? 'Expired (06:00 IST)' : 'Pending Login'}
                      </span>
                    );
                  })()}
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-slate-500">Secrets Vault:</span>
                  <span className="text-slate-400 flex items-center gap-1">
                    <ShieldCheck size={12} className="text-emerald-400" /> AES-256-GCM
                  </span>
                </div>
              </div>
            </div>

            <div className="mt-5 pt-4 border-t border-slate-800/80">
              {!conn.isAuthenticated && conn.providerCode !== 'Mock' ? (
                <div className="space-y-2">
                  {conn.tradingMode === 'Paper' && (
                    <button
                      onClick={() => handleActivatePaper(conn.id)}
                      className="w-full py-2 px-3 rounded-lg bg-emerald-600 hover:bg-emerald-500 text-white text-xs font-semibold flex items-center justify-center gap-1.5 shadow-sm shadow-emerald-500/20 transition"
                    >
                      <Zap size={14} />
                      Activate Paper Engine (1-Click Ready)
                    </button>
                  )}
                  <button
                    onClick={() => handleOAuthLogin(conn.id)}
                    className="w-full py-2 px-3 rounded-lg bg-sky-600 hover:bg-sky-500 text-white text-xs font-semibold flex items-center justify-center gap-1.5 transition"
                  >
                    <ExternalLink size={14} />
                    {conn.tokenExpiresAtUtc ? 'Re-authenticate with Broker' : 'Authorize via Broker OAuth'}
                  </button>
                  <button
                    onClick={() => openTokenModal(conn.id)}
                    className="w-full py-1.5 px-3 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 text-[11px] font-medium flex items-center justify-center gap-1.5 transition border border-slate-700"
                  >
                    <Key size={13} />
                    Paste Access Token Manually
                  </button>
                </div>
              ) : (
                <div className="space-y-2.5">
                  <div className="w-full flex items-center justify-between text-xs text-slate-400">
                    <span className="text-emerald-400 font-semibold flex items-center gap-1">
                      <CheckCircle2 size={14} /> Engine Ready
                    </span>
                    <span className="text-slate-500">ID: {conn.id.slice(0, 8)}...</span>
                  </div>

                  {conn.providerCode !== 'Mock' && (
                    <div className="space-y-2">
                      <button
                        type="button"
                        onClick={() => handleToggleConnectionMode(conn.id, conn.tradingMode)}
                        className={`w-full py-2 px-3 rounded-lg text-xs font-bold flex items-center justify-center gap-1.5 transition shadow-sm ${
                          conn.tradingMode === 'Paper'
                            ? 'bg-rose-600/90 hover:bg-rose-500 text-white shadow-rose-600/20'
                            : 'bg-emerald-600/90 hover:bg-emerald-500 text-white shadow-emerald-600/20'
                        }`}
                      >
                        <ArrowRightLeft size={13} />
                        {conn.tradingMode === 'Paper' ? 'Switch to Live Trading Mode' : 'Switch to Paper Trading Mode'}
                      </button>
                      <button
                        type="button"
                        onClick={() => handleOAuthLogin(conn.id)}
                        className="w-full py-1.5 px-3 rounded-lg bg-slate-800 hover:bg-slate-700 text-sky-300 text-[11px] font-semibold flex items-center justify-center gap-1.5 border border-slate-700 transition"
                      >
                        <RefreshCw size={12} />
                        Re-authenticate {conn.providerName}
                      </button>
                    </div>
                  )}
                </div>
              )}
            </div>
          </div>
        ))}

        {connections.length === 0 && !isLoading && (
          <div className="col-span-full p-12 text-center bg-slate-900/50 rounded-2xl border border-dashed border-slate-800 space-y-3">
            <Layers size={40} className="mx-auto text-slate-600" />
            <h3 className="text-base font-semibold text-white">No Broker Connections Configured</h3>
            <p className="text-xs text-slate-400 max-w-sm mx-auto">
              Start by connecting a Mock Engine account for zero-risk paper trading, or link your official Upstox or Zerodha API keys.
            </p>
            <button
              onClick={() => setIsModalOpen(true)}
              className="mt-2 px-4 py-2 bg-sky-600 hover:bg-sky-500 text-white rounded-lg text-xs font-semibold transition"
            >
              Add Your First Broker
            </button>
          </div>
        )}
      </div>

      {/* Connection Modal */}
      {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm">
          <div className="bg-slate-900 border border-slate-800 rounded-2xl max-w-md w-full p-6 shadow-2xl relative">
            <button
              onClick={() => setIsModalOpen(false)}
              className="absolute top-4 right-4 text-slate-400 hover:text-white p-1 rounded-lg"
            >
              <X size={18} />
            </button>

            <h3 className="text-lg font-bold text-white tracking-tight">Connect Broker Account</h3>
            <p className="text-xs text-slate-400 mt-1">
              Add your broker credentials. Secrets are encrypted using authenticated AES-256-GCM.
            </p>

            <form className="mt-5 space-y-4" onSubmit={handleCreateConnection}>
              <div>
                <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1">
                  Broker Provider
                </label>
                <select
                  value={selectedProviderId}
                  onChange={(e) => handleSelectProvider(e.target.value)}
                  className="w-full px-3 py-2 bg-slate-950 border border-slate-700 rounded-lg text-white text-xs font-medium focus:outline-none focus:ring-2 focus:ring-sky-500"
                >
                  {providers.map((p) => (
                    <option key={p.id} value={p.id}>
                      {p.name} ({p.code})
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1">
                  Display Name
                </label>
                <input
                  type="text"
                  required
                  value={displayName}
                  onChange={(e) => setDisplayName(e.target.value)}
                  placeholder="e.g. Zerodha Primary"
                  className="w-full px-3 py-2 bg-slate-950 border border-slate-700 rounded-lg text-white text-xs font-medium focus:outline-none focus:ring-2 focus:ring-sky-500"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1">
                    Trading Mode
                  </label>
                  <select
                    value={tradingMode}
                    onChange={(e) => setTradingMode(e.target.value as TradingMode)}
                    className="w-full px-3 py-2 bg-slate-950 border border-slate-700 rounded-lg text-white text-xs font-medium focus:outline-none focus:ring-2 focus:ring-sky-500"
                  >
                    <option value="Paper">Paper Trading (Safe)</option>
                    <option value="Live">Live Trading (Real Capital)</option>
                  </select>
                </div>

                <div>
                  <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1">
                    Initial Capital (₹)
                  </label>
                  <input
                    type="number"
                    step="any"
                    min={0}
                    value={initialCapital}
                    onChange={(e) => setInitialCapital(Number(e.target.value))}
                    className="w-full px-3 py-2 bg-slate-950 border border-slate-700 rounded-lg text-white text-xs font-medium focus:outline-none focus:ring-2 focus:ring-sky-500"
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1">
                  API Key
                </label>
                <div className="relative">
                  <Key size={14} className="absolute left-3 top-2.5 text-slate-500" />
                  <input
                    type="text"
                    required
                    value={apiKey}
                    onChange={(e) => setApiKey(e.target.value)}
                    placeholder="Enter API Key"
                    className="w-full pl-9 pr-3 py-2 bg-slate-950 border border-slate-700 rounded-lg text-white text-xs font-mono focus:outline-none focus:ring-2 focus:ring-sky-500"
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1">
                  API Secret
                </label>
                <div className="relative">
                  <Lock size={14} className="absolute left-3 top-2.5 text-slate-500" />
                  <input
                    type="password"
                    required
                    value={apiSecret}
                    onChange={(e) => setApiSecret(e.target.value)}
                    placeholder="Enter API Secret"
                    className="w-full pl-9 pr-3 py-2 bg-slate-950 border border-slate-700 rounded-lg text-white text-xs font-mono focus:outline-none focus:ring-2 focus:ring-sky-500"
                  />
                </div>
              </div>

              {tradingMode === 'Paper' && (
                <div className="p-3 bg-emerald-950/30 border border-emerald-800/40 rounded-xl text-[11px] text-emerald-300">
                  Paper Trading connections activate immediately with high-fidelity market order simulation and virtual capital.
                </div>
              )}

              <div className="pt-2 flex justify-end gap-2">
                <button
                  type="button"
                  onClick={() => setIsModalOpen(false)}
                  className="px-4 py-2 bg-slate-800 hover:bg-slate-700 text-slate-300 rounded-lg text-xs font-semibold transition"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={isSubmitting}
                  className="px-4 py-2 bg-sky-600 hover:bg-sky-500 text-white rounded-lg text-xs font-semibold transition flex items-center gap-1.5"
                >
                  {isSubmitting ? <RefreshCw size={14} className="animate-spin" /> : <ShieldCheck size={14} />}
                  Connect & Vault Secrets
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Manual Token Modal */}
      {isTokenModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm">
          <div className="bg-slate-900 border border-slate-800 rounded-2xl max-w-md w-full p-6 shadow-2xl relative">
            <button
              onClick={() => setIsTokenModalOpen(false)}
              className="absolute top-4 right-4 text-slate-400 hover:text-white p-1 rounded-lg"
            >
              <X size={18} />
            </button>

            <h3 className="text-lg font-bold text-white tracking-tight">Enter Access Token</h3>
            <p className="text-xs text-slate-400 mt-1">
              If you have already generated a daily session token via Python/Kite CLI, paste it here to authenticate immediately.
            </p>

            <form className="mt-5 space-y-4" onSubmit={handleApplyManualToken}>
              <div>
                <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1">
                  Access Token / Session Token
                </label>
                <div className="relative">
                  <Key size={14} className="absolute left-3 top-2.5 text-slate-500" />
                  <input
                    type="password"
                    required
                    value={manualAccessToken}
                    onChange={(e) => setManualAccessToken(e.target.value)}
                    placeholder="Paste access token from Kite/Upstox"
                    className="w-full pl-9 pr-3 py-2 bg-slate-950 border border-slate-700 rounded-lg text-white text-xs font-mono focus:outline-none focus:ring-2 focus:ring-sky-500"
                  />
                </div>
              </div>

              <div className="pt-2 flex justify-end gap-2">
                <button
                  type="button"
                  onClick={() => setIsTokenModalOpen(false)}
                  className="px-4 py-2 bg-slate-800 hover:bg-slate-700 text-slate-300 rounded-lg text-xs font-semibold transition"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={isSubmittingToken || !manualAccessToken.trim()}
                  className="px-4 py-2 bg-sky-600 hover:bg-sky-500 text-white rounded-lg text-xs font-semibold transition flex items-center gap-1.5"
                >
                  {isSubmittingToken ? <RefreshCw size={14} className="animate-spin" /> : <CheckCircle2 size={14} />}
                  Save & Set Engine Ready
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
