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
  HelpCircle
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

  // Modal State
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [selectedProviderId, setSelectedProviderId] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [apiKey, setApiKey] = useState('');
  const [apiSecret, setApiSecret] = useState('');
  const [tradingMode, setTradingMode] = useState<TradingMode>('Paper');
  const [initialCapital, setInitialCapital] = useState(500000);

  const selectedProvider = providers.find((p) => p.id === selectedProviderId);

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
    try {
      const redirectUri = window.location.origin + '/brokers/callback';
      const res = await apiClient.post<ApiResponse<{ authUrl: string }>>(
        `/brokers/connections/${connectionId}/auth-url`,
        { redirectUri }
      );
      if (res.data.success && res.data.data?.authUrl) {
        window.location.href = res.data.data.authUrl;
      }
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to generate OAuth redirect URL.');
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
            Need help finding your <strong>API Key</strong>, <strong>API Secret</strong>, or <strong>Redirect URL</strong>? Follow our step-by-step walkthrough.
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
        <div className="p-4 rounded-xl bg-rose-500/10 border border-rose-500/30 text-rose-400 text-sm flex items-start gap-2.5">
          <AlertCircle size={18} className="shrink-0 mt-0.5" />
          <span>{error}</span>
        </div>
      )}

      {/* Connected Brokers Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
        {connections.map((conn) => (
          <div
            key={conn.id}
            className="bg-slate-900 border border-slate-800 rounded-xl p-5 flex flex-col justify-between shadow-md relative overflow-hidden"
          >
            {/* Top decorative badge */}
            <div className="flex items-center justify-between">
              <span className="text-xs font-mono font-bold px-2 py-0.5 rounded bg-slate-800 text-slate-300 border border-slate-700">
                {conn.providerCode}
              </span>
              <span
                className={`px-2 py-0.5 rounded-full text-[11px] font-semibold flex items-center gap-1 ${
                  conn.tradingMode === 'Paper'
                    ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/30'
                    : 'bg-rose-500/10 text-rose-400 border border-rose-500/30'
                }`}
              >
                <span className={`w-1.5 h-1.5 rounded-full ${conn.tradingMode === 'Paper' ? 'bg-emerald-400' : 'bg-rose-400'}`} />
                {conn.tradingMode} Trading
              </span>
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
                  <span className={`inline-flex items-center gap-1 font-semibold ${
                    conn.isAuthenticated ? 'text-emerald-400' : 'text-amber-400'
                  }`}>
                    {conn.isAuthenticated ? <CheckCircle2 size={13} /> : <AlertCircle size={13} />}
                    {conn.isAuthenticated ? 'Authenticated' : 'Pending Login'}
                  </span>
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-slate-500">Secrets Vault:</span>
                  <span className="text-slate-400 flex items-center gap-1">
                    <ShieldCheck size={12} className="text-emerald-400" /> AES-256-GCM
                  </span>
                </div>
              </div>
            </div>

            <div className="mt-5 pt-4 border-t border-slate-800/80 flex items-center justify-between gap-2">
              {!conn.isAuthenticated && conn.providerCode !== 'Mock' ? (
                <button
                  onClick={() => handleOAuthLogin(conn.id)}
                  className="w-full py-2 px-3 rounded-lg bg-sky-600 hover:bg-sky-500 text-white text-xs font-semibold flex items-center justify-center gap-1.5 transition"
                >
                  <ExternalLink size={14} />
                  Authorize via Broker OAuth
                </button>
              ) : (
                <div className="w-full flex items-center justify-between text-xs text-slate-400">
                  <span className="text-emerald-400 font-semibold flex items-center gap-1">
                    <CheckCircle2 size={14} /> Engine Ready
                  </span>
                  <span className="text-slate-500">ID: {conn.id.slice(0, 8)}...</span>
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
                  className="w-full px-3 py-2 bg-slate-950 border border-slate-700 rounded-lg text-slate-100 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                >
                  {providers.length === 0 ? (
                    <option value="">Loading providers from server...</option>
                  ) : (
                    providers.map((p) => (
                      <option key={p.id} value={p.id}>
                        {p.name} ({p.code})
                      </option>
                    ))
                  )}
                </select>

                {selectedProvider && (
                  <div className="mt-2 p-2.5 rounded-lg bg-sky-950/40 border border-sky-800/40 text-xs text-sky-300 leading-relaxed">
                    {selectedProvider.code === 'Mock' && (
                      <span>
                        ⚡ <strong>Mock Simulator:</strong> Built-in zero-latency paper engine. Pre-filled with sandbox credentials for instant testing without needing real API keys.
                      </span>
                    )}
                    {selectedProvider.code === 'Upstox' && (
                      <span>
                        🌐 <strong>Upstox API:</strong> Enter your App Key &amp; Secret from the{' '}
                        <a href="https://developer.upstox.com" target="_blank" rel="noreferrer" className="underline font-bold text-sky-200">
                          Upstox Developer Portal
                        </a>
                        . After saving, click "Log In / Authorize" to complete 1-click OAuth.
                      </span>
                    )}
                    {selectedProvider.code === 'Zerodha' && (
                      <span>
                        📈 <strong>Zerodha Kite:</strong> Enter your API Key &amp; Secret from the{' '}
                        <a href="https://developers.kite.trade" target="_blank" rel="noreferrer" className="underline font-bold text-sky-200">
                          Kite Connect Console
                        </a>
                        . After saving, click "Log In / Authorize" to generate daily token.
                      </span>
                    )}
                  </div>
                )}
              </div>

              <div>
                <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1">
                  Connection Display Name
                </label>
                <input
                  type="text"
                  required
                  placeholder="e.g. Upstox Intraday Primary"
                  value={displayName}
                  onChange={(e) => setDisplayName(e.target.value)}
                  className="w-full px-3 py-2 bg-slate-950 border border-slate-700 rounded-lg text-slate-100 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1">
                  Trading Mode
                </label>
                <div className="grid grid-cols-2 gap-2">
                  <button
                    type="button"
                    onClick={() => setTradingMode('Paper')}
                    className={`py-2 text-xs font-bold rounded-lg border transition ${
                      tradingMode === 'Paper'
                        ? 'bg-emerald-500/20 border-emerald-500 text-emerald-400'
                        : 'bg-slate-950 border-slate-800 text-slate-400'
                    }`}
                  >
                    Paper Trading (Safe)
                  </button>
                  <button
                    type="button"
                    onClick={() => setTradingMode('Live')}
                    className={`py-2 text-xs font-bold rounded-lg border transition ${
                      tradingMode === 'Live'
                        ? 'bg-rose-500/20 border-rose-500 text-rose-400'
                        : 'bg-slate-950 border-slate-800 text-slate-400'
                    }`}
                  >
                    Live Trading
                  </button>
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1">
                  API Key / App Key
                </label>
                <div className="relative">
                  <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-500">
                    <Key size={16} />
                  </div>
                  <input
                    type="text"
                    required
                    placeholder="Enter broker API key"
                    value={apiKey}
                    onChange={(e) => setApiKey(e.target.value)}
                    className="w-full pl-9 pr-3 py-2 bg-slate-950 border border-slate-700 rounded-lg text-slate-100 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1">
                  API Secret
                </label>
                <div className="relative">
                  <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-500">
                    <Lock size={16} />
                  </div>
                  <input
                    type="password"
                    required
                    placeholder="Enter broker API secret"
                    value={apiSecret}
                    onChange={(e) => setApiSecret(e.target.value)}
                    className="w-full pl-9 pr-3 py-2 bg-slate-950 border border-slate-700 rounded-lg text-slate-100 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1">
                  Allocated Capital (₹ INR)
                </label>
                <input
                  type="number"
                  min={1000}
                  step={5000}
                  value={initialCapital}
                  onChange={(e) => setInitialCapital(Number(e.target.value))}
                  className="w-full px-3 py-2 bg-slate-950 border border-slate-700 rounded-lg text-slate-100 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                />
              </div>

              <div className="pt-2 flex items-center justify-end gap-2">
                <button
                  type="button"
                  onClick={() => setIsModalOpen(false)}
                  className="px-4 py-2 text-xs font-semibold text-slate-400 hover:text-white"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={isSubmitting}
                  className="px-5 py-2 bg-gradient-to-r from-sky-500 to-indigo-600 hover:from-sky-400 hover:to-indigo-500 text-white text-xs font-semibold rounded-lg shadow-md transition disabled:opacity-50"
                >
                  {isSubmitting ? 'Saving...' : 'Connect Broker'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
