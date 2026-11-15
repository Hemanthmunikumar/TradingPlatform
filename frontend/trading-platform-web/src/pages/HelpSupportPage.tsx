import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { 
  HelpCircle, 
  ExternalLink, 
  Copy, 
  Check, 
  ShieldCheck, 
  Layers, 
  Key, 
  Server, 
  Clock, 
  AlertTriangle, 
  Cpu, 
  Terminal,
  Zap,
  ArrowRight,
  Sparkles,
  PlayCircle
} from 'lucide-react';

export const HelpSupportPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'upstox' | 'zerodha' | 'mock' | 'ai' | 'docker' | 'safety'>('ai');
  const [copiedText, setCopiedText] = useState<string | null>(null);

  const redirectUrl = `${window.location.origin}/brokers/callback`;

  const copyToClipboard = (text: string, label: string) => {
    navigator.clipboard.writeText(text);
    setCopiedText(label);
    setTimeout(() => setCopiedText(null), 2500);
  };

  return (
    <div className="space-y-6 max-w-5xl mx-auto">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <div className="flex items-center gap-2">
            <span className="p-1.5 rounded-lg bg-sky-500/10 text-sky-400 border border-sky-500/30">
              <HelpCircle size={20} />
            </span>
            <h1 className="text-2xl sm:text-3xl font-extrabold text-white tracking-tight">
              Help &amp; Broker Setup Guide
            </h1>
          </div>
          <p className="text-xs sm:text-sm text-slate-400 mt-1">
            Complete step-by-step instructions, direct links, and copyable URLs to configure your trading brokers.
          </p>
        </div>

        <Link
          to="/brokers"
          className="inline-flex items-center gap-1.5 px-4 py-2 bg-gradient-to-r from-sky-500 to-indigo-600 hover:from-sky-400 hover:to-indigo-500 text-white rounded-lg text-xs font-semibold shadow-md shadow-sky-500/20 transition self-start sm:self-auto"
        >
          <Layers size={14} />
          Go to Brokers Page
          <ArrowRight size={14} />
        </Link>
      </div>

      {/* Tabs */}
      <div className="flex flex-wrap gap-2 border-b border-slate-800 pb-3">
        {[
          { id: 'ai', label: '1. AI Co-Pilot & Trials', icon: Sparkles },
          { id: 'upstox', label: '2. Upstox API Guide', icon: Zap },
          { id: 'zerodha', label: '3. Zerodha Kite Connect', icon: Key },
          { id: 'mock', label: '4. Mock Simulator (Paper)', icon: Cpu },
          { id: 'docker', label: '5. Docker & Database', icon: Server },
          { id: 'safety', label: '6. Safety & 2s Buy Gate', icon: ShieldCheck },
        ].map((tab) => {
          const Icon = tab.icon;
          const isActive = activeTab === tab.id;
          return (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id as any)}
              className={`flex items-center gap-2 px-3.5 py-2 rounded-lg text-xs font-semibold transition ${
                isActive
                  ? 'bg-sky-500 text-white shadow-md shadow-sky-500/20'
                  : 'bg-slate-900 text-slate-400 hover:text-white hover:bg-slate-800 border border-slate-800'
              }`}
            >
              <Icon size={14} />
              {tab.label}
            </button>
          );
        })}
      </div>

      {/* TAB 0: AI CO-PILOT & TRIALS */}
      {activeTab === 'ai' && (
        <div className="space-y-6">
          <div className="p-4 rounded-xl bg-slate-900 border border-slate-800">
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
              <div>
                <h2 className="text-base font-bold text-white flex items-center gap-2">
                  AI Market Expert Co-Pilot &amp; Mock Trials
                  <span className="px-2 py-0.5 rounded text-[10px] font-bold bg-amber-500/20 text-amber-300 border border-amber-500/30">
                    Live Engine
                  </span>
                </h2>
                <p className="text-xs text-slate-400 mt-0.5">
                  Autonomous algorithmic equity analyst powered by configurable AI and quantitative models.
                </p>
              </div>

              <Link
                to="/ai-advisor"
                className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-gradient-to-r from-amber-500 to-indigo-600 hover:from-amber-400 hover:to-indigo-500 text-white rounded-lg text-xs font-semibold shadow-md shadow-amber-500/20 transition self-start sm:self-auto"
              >
                <Sparkles size={14} />
                Open AI Co-Pilot
                <ArrowRight size={14} />
              </Link>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {/* Card 1: Existing Holdings Dip Accumulation */}
            <div className="p-5 rounded-xl bg-slate-900/60 border border-slate-800 space-y-3">
              <div className="flex items-center gap-2 text-emerald-400 text-xs font-bold uppercase tracking-wider">
                <Zap size={16} />
                Holdings Dip Accumulation
              </div>
              <p className="text-xs text-slate-300 leading-relaxed">
                The platform monitors your broker's portfolio holdings in real time. If fundamentally strong stocks pull back to oversold levels (RSI &lt; 38 or touching 50 EMA / VWAP intraday support), the AI raises a <strong>BUY THE DIP</strong> recommendation to average in at lower prices.
              </p>
              <div className="p-2.5 rounded-lg bg-slate-950 border border-slate-800 text-[11px] text-slate-400 font-mono">
                Trigger: RSI &lt; 38 + Holding P&amp;L down &gt; 1.5% + Daily Volume &gt; 1.2x Avg
              </div>
            </div>

            {/* Card 2: Special Monitor Watchlist */}
            <div className="p-5 rounded-xl bg-slate-900/60 border border-slate-800 space-y-3">
              <div className="flex items-center gap-2 text-sky-400 text-xs font-bold uppercase tracking-wider">
                <Cpu size={16} />
                Special Monitor Watchlist
              </div>
              <p className="text-xs text-slate-300 leading-relaxed">
                Have a stock in mind (e.g. <code>ZOMATO</code>, <code>BEL</code>, <code>TATASTEEL</code>) but unsure of the exact entry point? Add it to the <strong>Special Monitor Watchlist</strong> in the AI Co-Pilot. The engine tracks 3-timeframe trend momentum and only issues a BUY when market structure confirms.
              </p>
              <div className="p-2.5 rounded-lg bg-slate-950 border border-slate-800 text-[11px] text-slate-400 font-mono">
                Type ticker symbol (e.g. HAL) &rarr; Click Add Stock &rarr; Live AI tracking active
              </div>
            </div>

            {/* Card 3: 1-Click Interactive Test Trial */}
            <div className="p-5 rounded-xl bg-slate-900/60 border border-slate-800 space-y-3">
              <div className="flex items-center gap-2 text-amber-400 text-xs font-bold uppercase tracking-wider">
                <PlayCircle size={16} />
                1-Click Mock Simulator Test Trial
              </div>
              <p className="text-xs text-slate-300 leading-relaxed">
                Want to see the entire trade lifecycle before trading real money? In the AI Co-Pilot page, click <strong>"⚡ Test 2s BUY Rejection Window"</strong>.
              </p>
              <ul className="list-disc list-inside text-xs text-slate-300 space-y-1 pl-1">
                <li>Simulates an algorithmic BUY signal on mock stocks.</li>
                <li>Pops up the real-time <strong>2-second countdown toast</strong>.</li>
                <li>You can click <em>"Reject"</em> to cancel, or let it auto-execute into the mock portfolio!</li>
              </ul>
            </div>

            {/* Card 4: Configurable AI Providers */}
            <div className="p-5 rounded-xl bg-slate-900/60 border border-slate-800 space-y-3">
              <div className="flex items-center gap-2 text-purple-400 text-xs font-bold uppercase tracking-wider">
                <Terminal size={16} />
                Configurable AI Providers
              </div>
              <p className="text-xs text-slate-300 leading-relaxed">
                Easily swap between Gemini Pro, Claude, OpenAI, or local models inside <code>appsettings.json</code>:
              </p>
              <div className="p-2.5 rounded-lg bg-slate-950 border border-slate-800 text-[11px] text-slate-400 font-mono overflow-x-auto">
                <pre>{`"AiAdvisor": {
  "Provider": "GeminiPro", // or OpenAI, Claude, BuiltInQuant
  "GeminiApiKey": "YOUR_API_KEY",
  "Model": "gemini-1.5-pro"
}`}</pre>
              </div>
              <p className="text-[11px] text-slate-400">
                * When no API key is set, the system uses its sub-millisecond C# quantitative expert engine automatically.
              </p>
            </div>
          </div>
        </div>
      )}

      {/* TAB 1: UPSTOX */}
      {activeTab === 'upstox' && (
        <div className="space-y-6">
          <div className="p-4 rounded-xl bg-slate-900 border border-slate-800">
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
              <div>
                <h2 className="text-base font-bold text-white flex items-center gap-2">
                  Upstox Developer Integration Guide
                  <span className="px-2 py-0.5 rounded text-[10px] font-bold bg-purple-500/20 text-purple-300 border border-purple-500/30">
                    REST API v2
                  </span>
                </h2>
                <p className="text-xs text-slate-400 mt-0.5">
                  Connect your Upstox trading account with automated order routing and portfolio synchronization.
                </p>
              </div>

              <a
                href="https://developer.upstox.com"
                target="_blank"
                rel="noreferrer"
                className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-slate-800 hover:bg-slate-700 text-sky-400 rounded-lg text-xs font-semibold border border-slate-700 transition shrink-0"
              >
                Upstox Developer Portal
                <ExternalLink size={13} />
              </a>
            </div>
          </div>

          {/* Step-by-Step Cards */}
          <div className="grid grid-cols-1 gap-4">
            {/* Step 1 */}
            <div className="p-5 rounded-xl bg-slate-900/60 border border-slate-800 space-y-2">
              <div className="flex items-center gap-2 text-sky-400 text-xs font-bold uppercase tracking-wider">
                <span className="w-5 h-5 rounded-full bg-sky-500/20 flex items-center justify-center text-[11px]">1</span>
                Login to Upstox Developer Portal
              </div>
              <p className="text-xs text-slate-300 leading-relaxed pl-7">
                Go to <a href="https://developer.upstox.com" target="_blank" rel="noreferrer" className="text-sky-400 underline font-semibold">developer.upstox.com</a> and sign in using your Upstox mobile number, 6-digit PIN, and OTP.
              </p>
            </div>

            {/* Step 2 */}
            <div className="p-5 rounded-xl bg-slate-900/60 border border-slate-800 space-y-2">
              <div className="flex items-center gap-2 text-sky-400 text-xs font-bold uppercase tracking-wider">
                <span className="w-5 h-5 rounded-full bg-sky-500/20 flex items-center justify-center text-[11px]">2</span>
                Create a New API Application
              </div>
              <p className="text-xs text-slate-300 leading-relaxed pl-7">
                In your developer dashboard, click on <strong>"My Apps"</strong> &rarr; <strong>"New App"</strong>. Enter an app title such as <code className="text-sky-300 bg-slate-950 px-1 py-0.5 rounded">Algo Trading Platform</code>.
              </p>
            </div>

            {/* Step 3 - Redirect URL */}
            <div className="p-5 rounded-xl bg-slate-900/60 border border-sky-500/30 space-y-3">
              <div className="flex items-center gap-2 text-sky-400 text-xs font-bold uppercase tracking-wider">
                <span className="w-5 h-5 rounded-full bg-sky-500/20 flex items-center justify-center text-[11px]">3</span>
                Crucial Step: Configure the Redirect URL
              </div>
              <p className="text-xs text-slate-300 leading-relaxed pl-7">
                Under the <strong>"Redirect URL"</strong> field in your Upstox app settings, enter this exact callback URL:
              </p>
              <div className="ml-7 flex items-center gap-2 p-2.5 rounded-lg bg-slate-950 border border-slate-800">
                <code className="text-xs font-mono text-emerald-400 flex-1 truncate">{redirectUrl}</code>
                <button
                  onClick={() => copyToClipboard(redirectUrl, 'redirect')}
                  className="flex items-center gap-1 px-2.5 py-1 rounded bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-semibold transition"
                >
                  {copiedText === 'redirect' ? <Check size={13} className="text-emerald-400" /> : <Copy size={13} />}
                  {copiedText === 'redirect' ? 'Copied' : 'Copy URL'}
                </button>
              </div>
              <p className="text-[11px] text-slate-400 pl-7">
                ⚠️ If the redirect URL does not match exactly, Upstox will reject the OAuth login callback.
              </p>
            </div>

            {/* Step 4 */}
            <div className="p-5 rounded-xl bg-slate-900/60 border border-slate-800 space-y-2">
              <div className="flex items-center gap-2 text-sky-400 text-xs font-bold uppercase tracking-wider">
                <span className="w-5 h-5 rounded-full bg-sky-500/20 flex items-center justify-center text-[11px]">4</span>
                Copy API Key and API Secret
              </div>
              <p className="text-xs text-slate-300 leading-relaxed pl-7">
                Once created, click on your app in the Upstox portal to reveal:
              </p>
              <ul className="list-disc list-inside text-xs text-slate-300 space-y-1 pl-9">
                <li><strong>API Key</strong> (e.g. <code className="text-sky-300 bg-slate-950 px-1 rounded">2501xxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx</code>)</li>
                <li><strong>API Secret</strong> (a secret string generated for your app)</li>
              </ul>
            </div>

            {/* Step 5 */}
            <div className="p-5 rounded-xl bg-slate-900/60 border border-slate-800 space-y-2">
              <div className="flex items-center gap-2 text-sky-400 text-xs font-bold uppercase tracking-wider">
                <span className="w-5 h-5 rounded-full bg-sky-500/20 flex items-center justify-center text-[11px]">5</span>
                Connect &amp; Authenticate in this Platform
              </div>
              <ol className="list-decimal list-inside text-xs text-slate-300 space-y-1 pl-9">
                <li>Open <Link to="/brokers" className="text-sky-400 underline font-semibold">Brokers &amp; Accounts</Link> in the navigation.</li>
                <li>Click <strong>"Connect New Broker"</strong> and pick <strong>Upstox</strong>.</li>
                <li>Paste your API Key and API Secret, choose Paper or Live mode, and click <strong>"Connect Broker"</strong>.</li>
                <li>On the newly created Upstox card, click the green <strong>"Log In / Authorize"</strong> button.</li>
                <li>Approve permissions in the Upstox OAuth window. You will be redirected back with a green <strong>"Active &amp; Authenticated"</strong> badge!</li>
              </ol>
            </div>
          </div>
        </div>
      )}

      {/* TAB 2: ZERODHA */}
      {activeTab === 'zerodha' && (
        <div className="space-y-6">
          <div className="p-4 rounded-xl bg-slate-900 border border-slate-800">
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
              <div>
                <h2 className="text-base font-bold text-white flex items-center gap-2">
                  Zerodha Kite Connect Setup Guide
                  <span className="px-2 py-0.5 rounded text-[10px] font-bold bg-amber-500/20 text-amber-300 border border-amber-500/30">
                    Kite Connect 3.0
                  </span>
                </h2>
                <p className="text-xs text-slate-400 mt-0.5">
                  Connect your Zerodha account using the official Kite Connect developer API.
                </p>
              </div>

              <a
                href="https://developers.kite.trade"
                target="_blank"
                rel="noreferrer"
                className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-slate-800 hover:bg-slate-700 text-sky-400 rounded-lg text-xs font-semibold border border-slate-700 transition shrink-0"
              >
                Kite Developer Console
                <ExternalLink size={13} />
              </a>
            </div>
          </div>

          <div className="p-4 rounded-xl bg-amber-500/10 border border-amber-500/20 text-xs text-amber-300 flex items-start gap-2.5">
            <AlertTriangle size={18} className="shrink-0 mt-0.5" />
            <div>
              <strong>Note regarding Kite Connect Subscription:</strong> Zerodha charges ₹2,000/month for active Kite Connect API access. Ensure your subscription is active on <a href="https://developers.kite.trade" target="_blank" rel="noreferrer" className="underline font-bold text-amber-200">developers.kite.trade</a> before attempting OAuth authorization.
            </div>
          </div>

          {/* Step-by-Step Cards */}
          <div className="grid grid-cols-1 gap-4">
            {/* Step 1 */}
            <div className="p-5 rounded-xl bg-slate-900/60 border border-slate-800 space-y-2">
              <div className="flex items-center gap-2 text-sky-400 text-xs font-bold uppercase tracking-wider">
                <span className="w-5 h-5 rounded-full bg-sky-500/20 flex items-center justify-center text-[11px]">1</span>
                Login to Kite Developer Console
              </div>
              <p className="text-xs text-slate-300 leading-relaxed pl-7">
                Open <a href="https://developers.kite.trade" target="_blank" rel="noreferrer" className="text-sky-400 underline font-semibold">developers.kite.trade</a> and log in with your Kite developer account credentials.
              </p>
            </div>

            {/* Step 2 */}
            <div className="p-5 rounded-xl bg-slate-900/60 border border-slate-800 space-y-2">
              <div className="flex items-center gap-2 text-sky-400 text-xs font-bold uppercase tracking-wider">
                <span className="w-5 h-5 rounded-full bg-sky-500/20 flex items-center justify-center text-[11px]">2</span>
                Create New App
              </div>
              <p className="text-xs text-slate-300 leading-relaxed pl-7">
                Click <strong>"Create New App"</strong>:
              </p>
              <ul className="list-disc list-inside text-xs text-slate-300 space-y-1 pl-9">
                <li>App Type: <strong>Connect</strong></li>
                <li>App Name: <code className="text-sky-300 bg-slate-950 px-1 rounded">Algo Trading Platform</code></li>
                <li>Zerodha Client ID: Your 6-character Kite User ID (e.g. <code className="text-sky-300 bg-slate-950 px-1 rounded">AB1234</code>)</li>
              </ul>
            </div>

            {/* Step 3 - Redirect URL */}
            <div className="p-5 rounded-xl bg-slate-900/60 border border-amber-500/30 space-y-3">
              <div className="flex items-center gap-2 text-amber-400 text-xs font-bold uppercase tracking-wider">
                <span className="w-5 h-5 rounded-full bg-amber-500/20 flex items-center justify-center text-[11px]">3</span>
                Set Redirect URL
              </div>
              <p className="text-xs text-slate-300 leading-relaxed pl-7">
                In the Kite app form, set the <strong>"Redirect URL"</strong> to:
              </p>
              <div className="ml-7 flex items-center gap-2 p-2.5 rounded-lg bg-slate-950 border border-slate-800">
                <code className="text-xs font-mono text-emerald-400 flex-1 truncate">{redirectUrl}</code>
                <button
                  onClick={() => copyToClipboard(redirectUrl, 'redirect')}
                  className="flex items-center gap-1 px-2.5 py-1 rounded bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-semibold transition"
                >
                  {copiedText === 'redirect' ? <Check size={13} className="text-emerald-400" /> : <Copy size={13} />}
                  {copiedText === 'redirect' ? 'Copied' : 'Copy URL'}
                </button>
              </div>
            </div>

            {/* Step 4 */}
            <div className="p-5 rounded-xl bg-slate-900/60 border border-slate-800 space-y-2">
              <div className="flex items-center gap-2 text-sky-400 text-xs font-bold uppercase tracking-wider">
                <span className="w-5 h-5 rounded-full bg-sky-500/20 flex items-center justify-center text-[11px]">4</span>
                Copy API Key &amp; API Secret
              </div>
              <p className="text-xs text-slate-300 leading-relaxed pl-7">
                Copy your <strong>api_key</strong> and <strong>api_secret</strong> from the app overview page.
              </p>
            </div>

            {/* Step 5 - Daily Token Renewal */}
            <div className="p-5 rounded-xl bg-slate-900/60 border border-slate-800 space-y-2">
              <div className="flex items-center gap-2 text-sky-400 text-xs font-bold uppercase tracking-wider">
                <span className="w-5 h-5 rounded-full bg-sky-500/20 flex items-center justify-center text-[11px]">5</span>
                Daily Morning Session Token Renewal (SEBI Requirement)
              </div>
              <p className="text-xs text-slate-300 leading-relaxed pl-7">
                Per Indian regulatory rules, Zerodha Kite access tokens expire daily at 06:00 AM IST. Each morning before trading hours (09:15 AM), simply visit the <Link to="/brokers" className="text-sky-400 underline font-semibold">Brokers</Link> page and click <strong>"Log In / Authorize"</strong> once to refresh your session for the trading day.
              </p>
            </div>
          </div>
        </div>
      )}

      {/* TAB 3: MOCK SIMULATOR */}
      {activeTab === 'mock' && (
        <div className="space-y-6">
          <div className="p-4 rounded-xl bg-slate-900 border border-slate-800">
            <h2 className="text-base font-bold text-white flex items-center gap-2">
              Mock Simulator (Zero-Setup Paper Trading)
              <span className="px-2 py-0.5 rounded text-[10px] font-bold bg-emerald-500/20 text-emerald-300 border border-emerald-500/30">
                Instant Access
              </span>
            </h2>
            <p className="text-xs text-slate-400 mt-0.5">
              Built-in high-fidelity Indian equity exchange simulator. Ideal for scenario testing, strategy backtesting, and paper execution.
            </p>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div className="p-4 rounded-xl bg-slate-900/60 border border-slate-800 space-y-1">
              <p className="text-xs font-semibold text-slate-400">Virtual Capital</p>
              <p className="text-xl font-bold text-emerald-400">₹5,00,000</p>
              <p className="text-[11px] text-slate-500">Ready for simulated equity orders</p>
            </div>
            <div className="p-4 rounded-xl bg-slate-900/60 border border-slate-800 space-y-1">
              <p className="text-xs font-semibold text-slate-400">Execution Speed</p>
              <p className="text-xl font-bold text-sky-400">&lt; 10 ms</p>
              <p className="text-[11px] text-slate-500">In-memory order book simulation</p>
            </div>
            <div className="p-4 rounded-xl bg-slate-900/60 border border-slate-800 space-y-1">
              <p className="text-xs font-semibold text-slate-400">Credentials Required</p>
              <p className="text-xl font-bold text-purple-400">None</p>
              <p className="text-[11px] text-slate-500">Pre-filled sandbox credentials</p>
            </div>
          </div>

          <div className="p-5 rounded-xl bg-slate-900/60 border border-slate-800 space-y-3">
            <h3 className="text-sm font-bold text-white">How to Activate Mock Simulator:</h3>
            <ol className="list-decimal list-inside text-xs text-slate-300 space-y-2 pl-2">
              <li>Go to <Link to="/brokers" className="text-sky-400 underline font-semibold">Brokers &amp; Accounts</Link>.</li>
              <li>Click <strong>"Connect New Broker"</strong>.</li>
              <li>The form defaults to <strong>Mock Simulator (Mock)</strong> with pre-filled test keys.</li>
              <li>Click <strong>"Connect Broker"</strong>.</li>
              <li>The connection is created and authenticated instantly with ₹5,00,000 allocated funds!</li>
            </ol>
          </div>
        </div>
      )}

      {/* TAB 4: DOCKER & DATABASE */}
      {activeTab === 'docker' && (
        <div className="space-y-6">
          <div className="p-4 rounded-xl bg-slate-900 border border-slate-800">
            <h2 className="text-base font-bold text-white flex items-center gap-2">
              Docker Deployment &amp; Automatic Database Seeding
              <span className="px-2 py-0.5 rounded text-[10px] font-bold bg-blue-500/20 text-blue-300 border border-blue-500/30">
                Production Ready
              </span>
            </h2>
            <p className="text-xs text-slate-400 mt-0.5">
              Zero manual database setup. Every time the Docker container starts, PostgreSQL tables, roles, broker providers, and default accounts are automatically seeded.
            </p>
          </div>

          <div className="p-5 rounded-xl bg-slate-900/60 border border-slate-800 space-y-3">
            <div className="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-sky-400">
              <Terminal size={15} />
              1-Command Docker Startup
            </div>
            <div className="flex items-center justify-between p-3 rounded-lg bg-slate-950 border border-slate-800">
              <code className="text-xs font-mono text-sky-300">docker compose up -d --build</code>
              <button
                onClick={() => copyToClipboard('docker compose up -d --build', 'docker')}
                className="flex items-center gap-1 px-2.5 py-1 rounded bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-semibold transition"
              >
                {copiedText === 'docker' ? <Check size={13} className="text-emerald-400" /> : <Copy size={13} />}
                {copiedText === 'docker' ? 'Copied' : 'Copy'}
              </button>
            </div>
            <p className="text-xs text-slate-400">
              This launches PostgreSQL 17 on port 5432, .NET 10 API on port 5000, Execution Worker, and React Nginx Frontend on port 80.
            </p>
          </div>

          <div className="p-5 rounded-xl bg-slate-900/60 border border-slate-800 space-y-3">
            <h3 className="text-sm font-bold text-white">Default Auto-Seeded Data:</h3>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs">
              <div className="p-3 rounded-lg bg-slate-950 border border-slate-800/80 space-y-1">
                <p className="font-bold text-sky-400">Pre-Configured Demo Account</p>
                <p className="text-slate-300">Email: <code className="text-amber-300">trader1@example.com</code></p>
                <p className="text-slate-300">Password: <code className="text-amber-300">Password123!</code></p>
                <p className="text-[11px] text-slate-500">Auto-created with ₹5,00,000 mock funds and User role.</p>
              </div>

              <div className="p-3 rounded-lg bg-slate-950 border border-slate-800/80 space-y-1">
                <p className="font-bold text-emerald-400">Supported Broker Providers</p>
                <p className="text-slate-300">• Mock Simulator (Instant Paper)</p>
                <p className="text-slate-300">• Upstox (v2 Dialog OAuth)</p>
                <p className="text-slate-300">• Zerodha (Kite Connect 3.0)</p>
                <p className="text-[11px] text-slate-500">Seeded in table with all API endpoints.</p>
              </div>

              <div className="p-3 rounded-lg bg-slate-950 border border-slate-800/80 space-y-1">
                <p className="font-bold text-purple-400">Top Indian Equities (NSE)</p>
                <p className="text-slate-300">RELIANCE, TCS, INFY, HDFCBANK, ICICIBANK, SBIN, TATAMOTORS, BHARTIARTL</p>
                <p className="text-[11px] text-slate-500">Pre-seeded with NSE tokens and tick sizes.</p>
              </div>

              <div className="p-3 rounded-lg bg-slate-950 border border-slate-800/80 space-y-1">
                <p className="font-bold text-rose-400">AES-256-GCM Encryption</p>
                <p className="text-slate-300">Master Key: Configured in environment</p>
                <p className="text-slate-300">All broker API keys and secrets encrypted at rest.</p>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* TAB 5: SAFETY GATE */}
      {activeTab === 'safety' && (
        <div className="space-y-6">
          <div className="p-4 rounded-xl bg-slate-900 border border-slate-800">
            <h2 className="text-base font-bold text-white flex items-center gap-2">
              Risk Engine &amp; 2-Second BUY Rejection Window
              <span className="px-2 py-0.5 rounded text-[10px] font-bold bg-rose-500/20 text-rose-300 border border-rose-500/30">
                Safety First
              </span>
            </h2>
            <p className="text-xs text-slate-400 mt-0.5">
              Protecting your capital against flash crashes, rogue signals, and abnormal market volatility.
            </p>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="p-5 rounded-xl bg-slate-900/60 border border-slate-800 space-y-2">
              <div className="flex items-center gap-2 text-sky-400 text-xs font-bold uppercase tracking-wider">
                <Clock size={16} />
                Fast-Action 2-Second BUY Rejection Window
              </div>
              <p className="text-xs text-slate-300 leading-relaxed">
                Whenever an algorithmic strategy triggers a <strong>BUY</strong> signal, the platform raises a persistent real-time toast alert with an interactive 2-second countdown timer.
              </p>
              <ul className="list-disc list-inside text-xs text-slate-300 space-y-1 pl-2">
                <li>Click <strong>"Reject Trade"</strong> within 2 seconds to immediately cancel execution.</li>
                <li>If not rejected, the order auto-dispatches cleanly to the broker adapter upon timer expiration.</li>
              </ul>
            </div>

            <div className="p-5 rounded-xl bg-slate-900/60 border border-slate-800 space-y-2">
              <div className="flex items-center gap-2 text-rose-400 text-xs font-bold uppercase tracking-wider">
                <ShieldCheck size={16} />
                19-Point Pre-Order Risk Verification
              </div>
              <p className="text-xs text-slate-300 leading-relaxed">
                Before any order touches an exchange or simulation, it must pass 19 automated risk checks:
              </p>
              <ul className="list-disc list-inside text-xs text-slate-300 space-y-1 pl-2">
                <li>Capital &amp; margin sufficiency check</li>
                <li>Single-stock maximum exposure limit (e.g. max 20%)</li>
                <li>Daily loss limit breaker (auto-halts if breached)</li>
                <li>Mandatory Stop-Loss verification on all intraday positions</li>
              </ul>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
