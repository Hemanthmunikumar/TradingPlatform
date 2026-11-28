import React, { useState, useEffect, useCallback } from 'react';
import { Outlet, Link, useLocation } from 'react-router-dom';
import { 
  LayoutDashboard, 
  Layers, 
  ShieldAlert, 
  Sliders, 
  LogOut, 
  Menu, 
  X, 
  CheckCircle2, 
  TrendingUp,
  Clock,
  HelpCircle,
  Sparkles
} from 'lucide-react';
import { useAuth } from '../context/AuthContext';
import { NotificationCenter } from '../components/NotificationCenter';
import { BuyApprovalToast } from '../components/BuyApprovalToast';
import { apiClient } from '../api/client';
import type { ApiResponse, BrokerConnectionDto } from '../types';

export const DashboardLayout: React.FC = () => {
  const { user, logout } = useAuth();
  const location = useLocation();
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);
  const [tradingMode, setTradingMode] = useState<'Paper' | 'Live' | 'LiveExpired' | 'None'>('Paper');
  const [isKillSwitchActive] = useState(false);

  const syncHeaderTradingMode = useCallback(async () => {
    try {
      const res = await apiClient.get<ApiResponse<BrokerConnectionDto[]>>('/brokers/connections');
      const enabledConns = (res.data.data || []).filter((c) => c.isActive);
      const authConns = enabledConns.filter((c) => c.isAuthenticated);
      if (enabledConns.length === 0) {
        setTradingMode('None');
      } else if (authConns.some((c) => c.tradingMode === 'Live')) {
        setTradingMode('Live');
      } else if (enabledConns.some((c) => c.tradingMode === 'Live' && !c.isAuthenticated)) {
        setTradingMode('LiveExpired');
      } else if (authConns.length > 0) {
        setTradingMode('Paper');
      } else {
        setTradingMode('None');
      }
    } catch {
      // Keep current state on transient failure
    }
  }, []);

  useEffect(() => {
    syncHeaderTradingMode();
    const handler = () => syncHeaderTradingMode();
    window.addEventListener('broker-state-changed', handler);
    return () => window.removeEventListener('broker-state-changed', handler);
  }, [syncHeaderTradingMode, location.pathname]);

  const navItems = [
    { path: '/', label: 'Dashboard', icon: LayoutDashboard },
    { path: '/ai-advisor', label: 'AI Co-Pilot', icon: Sparkles },
    { path: '/brokers', label: 'Brokers & Accounts', icon: Layers },
    { path: '/approvals', label: 'Pending Approvals', icon: Clock },
    { path: '/orders', label: 'Orders & Trades', icon: TrendingUp },
    { path: '/settings', label: 'Risk & Strategy', icon: Sliders },
    { path: '/help', label: 'Help / Support', icon: HelpCircle },
  ];

  return (
    <div className="min-h-screen flex flex-col bg-slate-950 text-slate-100">
      {/* Top Header */}
      <header className="sticky top-0 z-40 bg-slate-900/90 backdrop-blur-md border-b border-slate-800 px-4 py-3 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <button 
            onClick={() => setIsSidebarOpen(!isSidebarOpen)}
            className="md:hidden p-1.5 rounded-lg bg-slate-800 text-slate-400 hover:text-white"
            aria-label="Toggle Navigation Menu"
          >
            {isSidebarOpen ? <X size={20} /> : <Menu size={20} />}
          </button>
          
          <div className="flex items-center gap-2">
            <div className="w-8 h-8 rounded-lg bg-gradient-to-tr from-sky-500 to-indigo-600 flex items-center justify-center font-bold text-white shadow-lg shadow-sky-500/20">
              SS
            </div>
            <span className="font-bold text-lg tracking-tight bg-gradient-to-r from-white to-slate-400 bg-clip-text text-transparent">
              StrikeSutra
            </span>
          </div>
        </div>

        {/* Global Trading Status Pills */}
        <div className="flex items-center gap-2.5">
          {/* Trading Mode Badge (Clickable link to Brokers/Settings) */}
          <Link 
            to={tradingMode === 'None' || tradingMode === 'LiveExpired' ? '/brokers' : '/settings'}
            title="Click to manage Broker Mode or Risk Settings"
            className={`px-2.5 py-1 rounded-full text-xs font-semibold tracking-wider uppercase flex items-center gap-1.5 transition hover:scale-105 cursor-pointer ${
              tradingMode === 'Live'
                ? 'bg-rose-500/10 text-rose-400 border border-rose-500/30 animate-pulse hover:bg-rose-500/20'
                : tradingMode === 'LiveExpired'
                  ? 'bg-amber-500/10 text-amber-300 border border-amber-500/30 hover:bg-amber-500/20'
                  : tradingMode === 'None'
                    ? 'bg-slate-800 text-slate-400 border border-slate-700 hover:bg-slate-700'
                    : 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/30 hover:bg-emerald-500/20'
            }`}
          >
            <span className={`w-2 h-2 rounded-full ${
              tradingMode === 'Live' ? 'bg-rose-400' : tradingMode === 'LiveExpired' ? 'bg-amber-400' : tradingMode === 'None' ? 'bg-slate-500' : 'bg-emerald-400'
            }`} />
            {tradingMode === 'None' ? 'No Active Broker' : tradingMode === 'LiveExpired' ? 'Live (Re-Auth Needed)' : `${tradingMode} Trading`}
          </Link>

          {/* Kill Switch Indicator */}
          {isKillSwitchActive && (
            <div className="px-2 py-0.5 rounded text-xs font-bold bg-rose-600 text-white flex items-center gap-1">
              <ShieldAlert size={14} />
              HALTED
            </div>
          )}

          {/* Notification Center */}
          <NotificationCenter />

          {/* User Profile & Logout */}
          <div className="flex items-center gap-2 pl-2 border-l border-slate-800">
            <div className="hidden sm:block text-right">
              <p className="text-xs font-medium text-slate-200">{user?.fullName || 'Trader'}</p>
              <p className="text-[10px] text-slate-400 truncate max-w-[120px]">{user?.email}</p>
            </div>
            <button 
              onClick={logout} 
              className="p-2 rounded-lg text-slate-400 hover:text-rose-400 hover:bg-slate-800/60 transition"
              title="Logout"
            >
              <LogOut size={18} />
            </button>
          </div>
        </div>
      </header>

      <div className="flex-1 flex">
        {/* Desktop Sidebar */}
        <aside className={`
          fixed md:static inset-y-0 left-0 z-30 w-64 bg-slate-900/95 border-r border-slate-800 p-4 flex flex-col justify-between
          transform transition-transform duration-200 ease-in-out md:translate-x-0
          ${isSidebarOpen ? 'translate-x-0' : '-translate-x-full'}
        `}>
          <nav className="space-y-1.5 mt-2">
            {navItems.map((item) => {
              const Icon = item.icon;
              const isActive = location.pathname === item.path;
              return (
                <Link
                  key={item.path}
                  to={item.path}
                  onClick={() => setIsSidebarOpen(false)}
                  className={`flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition ${
                    isActive 
                      ? 'bg-sky-500/10 text-sky-400 border border-sky-500/30' 
                      : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/50'
                  }`}
                >
                  <Icon size={18} />
                  {item.label}
                </Link>
              );
            })}
          </nav>

          <div className="p-3 bg-slate-950/60 rounded-xl border border-slate-800/60 text-xs text-slate-400 space-y-1">
            <div className="flex items-center gap-1.5 text-slate-300 font-semibold">
              <CheckCircle2 size={14} className="text-emerald-400" />
              Engine Online
            </div>
            <p>Exchange: NSE/BSE (IST)</p>
            <p className="text-[10px] text-slate-500">v1.0.0 — .NET 10 LTS Backend</p>
          </div>
        </aside>

        {/* Backdrop for mobile drawer */}
        {isSidebarOpen && (
          <div 
            onClick={() => setIsSidebarOpen(false)} 
            className="fixed inset-0 bg-black/60 z-20 md:hidden backdrop-blur-sm"
          />
        )}

        {/* Main Workspace Body */}
        <main className="flex-1 p-4 sm:p-6 lg:p-8 max-w-7xl mx-auto w-full pb-20 md:pb-8">
          <Outlet />
        </main>
      </div>

      {/* Mobile Bottom Navigation Bar */}
      <nav className="md:hidden fixed bottom-0 inset-x-0 bg-slate-900/95 backdrop-blur-md border-t border-slate-800 z-40 px-2 py-1 flex justify-around">
        {navItems.map((item) => {
          const Icon = item.icon;
          const isActive = location.pathname === item.path;
          return (
            <Link
              key={item.path}
              to={item.path}
              className={`flex flex-col items-center py-1.5 px-3 rounded-lg text-[10px] font-medium transition ${
                isActive ? 'text-sky-400 font-bold' : 'text-slate-400 hover:text-slate-200'
              }`}
            >
              <Icon size={18} className="mb-0.5" />
              {item.label.split(' ')[0]}
            </Link>
          );
        })}
      </nav>
      {/* Global Persistent Countdown Approval Toast + Tab Flash + Desktop Alerts */}
      <BuyApprovalToast />
    </div>
  );
};
