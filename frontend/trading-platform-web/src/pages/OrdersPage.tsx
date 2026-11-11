import React, { useState, useEffect } from 'react';
import { TrendingUp, Layers, History, Download, RefreshCw, XCircle } from 'lucide-react';
import { ordersApi, tradesApi } from '../api/client';
import type { PositionDto, OrderDto, TradeHistoryDto } from '../types';

export const OrdersPage: React.FC = () => {
  const [positions, setPositions] = useState<PositionDto[]>([]);
  const [orders, setOrders] = useState<OrderDto[]>([]);
  const [trades, setTrades] = useState<TradeHistoryDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [activeTab, setActiveTab] = useState<'positions' | 'orders' | 'history'>('positions');

  const loadData = async () => {
    setLoading(true);
    try {
      const [posData, ordData, trdData] = await Promise.all([
        ordersApi.getPositions().catch(() => []),
        ordersApi.getOrders().catch(() => []),
        tradesApi.getHistory().catch(() => []),
      ]);
      setPositions(posData);
      setOrders(ordData);
      setTrades(trdData);
    } catch (err) {
      console.error('Error fetching order & trade data:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleClosePosition = async (id: string) => {
    if (window.confirm('Are you sure you want to square off this position at market price?')) {
      try {
        await ordersApi.closePosition(id, 'Manual square-off via UI');
        loadData();
      } catch (err: any) {
        alert(err.response?.data?.message || 'Failed to square off position');
      }
    }
  };

  const handleCancelOrder = async (id: string) => {
    if (window.confirm('Cancel this pending order?')) {
      try {
        await ordersApi.cancelOrder(id, 'User cancelled via UI');
        loadData();
      } catch (err: any) {
        alert(err.response?.data?.message || 'Failed to cancel order');
      }
    }
  };

  const downloadCsv = () => {
    window.open(tradesApi.getExportCsvUrl(), '_blank');
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl sm:text-3xl font-extrabold text-white tracking-tight">
            Orders &amp; Positions
          </h1>
          <p className="text-xs sm:text-sm text-slate-400 mt-0.5">
            Active order book, fills, and portfolio positions across broker accounts
          </p>
        </div>
        <div className="flex items-center gap-3">
          <button
            onClick={downloadCsv}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-slate-800 text-xs font-semibold text-slate-200 hover:bg-slate-700 border border-slate-700 transition-colors"
          >
            <Download size={14} /> Export CSV
          </button>
          <button
            onClick={loadData}
            disabled={loading}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-emerald-600/20 text-xs font-semibold text-emerald-400 hover:bg-emerald-600/30 border border-emerald-500/30 transition-colors"
          >
            <RefreshCw size={14} className={loading ? 'animate-spin' : ''} /> Refresh
          </button>
        </div>
      </div>

      {/* Tabs */}
      <div className="flex items-center gap-2 border-b border-slate-800 pb-2">
        <button
          onClick={() => setActiveTab('positions')}
          className={`flex items-center gap-2 px-3 py-1.5 rounded-lg text-xs font-bold uppercase transition-all ${
            activeTab === 'positions'
              ? 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/30'
              : 'text-slate-400 hover:text-white'
          }`}
        >
          <TrendingUp size={14} /> Open Positions ({positions.length})
        </button>
        <button
          onClick={() => setActiveTab('orders')}
          className={`flex items-center gap-2 px-3 py-1.5 rounded-lg text-xs font-bold uppercase transition-all ${
            activeTab === 'orders'
              ? 'bg-sky-500/20 text-sky-400 border border-sky-500/30'
              : 'text-slate-400 hover:text-white'
          }`}
        >
          <Layers size={14} /> Orders ({orders.length})
        </button>
        <button
          onClick={() => setActiveTab('history')}
          className={`flex items-center gap-2 px-3 py-1.5 rounded-lg text-xs font-bold uppercase transition-all ${
            activeTab === 'history'
              ? 'bg-purple-500/20 text-purple-400 border border-purple-500/30'
              : 'text-slate-400 hover:text-white'
          }`}
        >
          <History size={14} /> Realized Trades ({trades.length})
        </button>
      </div>

      {/* Positions Content */}
      {activeTab === 'positions' && (
        <div className="bg-slate-900 border border-slate-800 rounded-xl overflow-hidden shadow-sm">
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead className="bg-slate-950/60 text-slate-400 border-b border-slate-800 uppercase tracking-wider font-semibold">
                <tr>
                  <th className="py-3 px-4">Instrument</th>
                  <th className="py-3 px-4">Product</th>
                  <th className="py-3 px-4">Quantity</th>
                  <th className="py-3 px-4">Avg Buy Price</th>
                  <th className="py-3 px-4">Current Price</th>
                  <th className="py-3 px-4">Unrealized P&amp;L</th>
                  <th className="py-3 px-4 text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-800">
                {positions.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="py-8 text-center text-slate-500">
                      No open positions currently held.
                    </td>
                  </tr>
                ) : (
                  positions.map((pos) => {
                    const isProfit = (pos.unrealizedPnL ?? 0) >= 0;
                    const invested = pos.averageBuyPrice * pos.quantity;
                    const pnlPct = invested > 0 ? ((pos.unrealizedPnL ?? 0) / invested) * 100 : 0;
                    return (
                      <tr key={pos.id} className="hover:bg-slate-800/40 transition-colors">
                        <td className="py-3 px-4 font-bold text-white">{pos.symbol}</td>
                        <td className="py-3 px-4 text-slate-300 font-mono">{pos.productType}</td>
                        <td className="py-3 px-4 text-emerald-400 font-semibold">{pos.quantity}</td>
                        <td className="py-3 px-4 text-slate-300 font-mono">₹{pos.averageBuyPrice.toFixed(2)}</td>
                        <td className="py-3 px-4 text-slate-300 font-mono">₹{pos.currentPrice.toFixed(2)}</td>
                        <td className="py-3 px-4 font-mono font-bold">
                          <span className={isProfit ? 'text-emerald-400' : 'text-rose-400'}>
                            {isProfit ? '+' : ''}₹{(pos.unrealizedPnL ?? 0).toFixed(2)} ({pnlPct.toFixed(2)}%)
                          </span>
                        </td>
                        <td className="py-3 px-4 text-right">
                          <button
                            onClick={() => handleClosePosition(pos.id)}
                            className="px-2.5 py-1 rounded bg-rose-600/20 text-rose-400 hover:bg-rose-600/30 border border-rose-500/30 text-[11px] font-semibold transition-colors"
                          >
                            Square Off
                          </button>
                        </td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Orders Content */}
      {activeTab === 'orders' && (
        <div className="bg-slate-900 border border-slate-800 rounded-xl overflow-hidden shadow-sm">
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead className="bg-slate-950/60 text-slate-400 border-b border-slate-800 uppercase tracking-wider font-semibold">
                <tr>
                  <th className="py-3 px-4">Time</th>
                  <th className="py-3 px-4">Symbol</th>
                  <th className="py-3 px-4">Side</th>
                  <th className="py-3 px-4">Type</th>
                  <th className="py-3 px-4">Qty</th>
                  <th className="py-3 px-4">Price</th>
                  <th className="py-3 px-4">Status</th>
                  <th className="py-3 px-4 text-right">Action</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-800">
                {orders.length === 0 ? (
                  <tr>
                    <td colSpan={8} className="py-8 text-center text-slate-500">
                      No orders found today.
                    </td>
                  </tr>
                ) : (
                  orders.map((ord) => (
                    <tr key={ord.id} className="hover:bg-slate-800/40 transition-colors">
                      <td className="py-3 px-4 text-slate-400 font-mono">
                        {new Date(ord.createdAtUtc).toLocaleTimeString()}
                      </td>
                      <td className="py-3 px-4 font-bold text-white">{ord.symbol}</td>
                      <td className="py-3 px-4">
                        <span
                          className={`px-2 py-0.5 rounded text-[10px] font-bold uppercase ${
                            ord.side === 'Buy'
                              ? 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/30'
                              : 'bg-rose-500/20 text-rose-400 border border-rose-500/30'
                          }`}
                        >
                          {ord.side}
                        </span>
                      </td>
                      <td className="py-3 px-4 text-slate-300 font-mono">{ord.orderType}</td>
                      <td className="py-3 px-4 text-slate-300 font-mono">{ord.quantity}</td>
                      <td className="py-3 px-4 text-slate-300 font-mono">
                        {ord.averagePrice ? `₹${ord.averagePrice.toFixed(2)}` : ord.price ? `₹${ord.price.toFixed(2)}` : 'MKT'}
                      </td>
                      <td className="py-3 px-4">
                        <span className="px-2 py-0.5 rounded text-[10px] font-bold uppercase bg-slate-800 text-slate-300 border border-slate-700">
                          {ord.status}
                        </span>
                      </td>
                      <td className="py-3 px-4 text-right">
                        {(ord.status === 'Open' || ord.status === 'Accepted' || ord.status === 'Created') && (
                          <button
                            onClick={() => handleCancelOrder(ord.id)}
                            className="p-1 rounded text-rose-400 hover:text-rose-300 hover:bg-rose-950/40"
                            title="Cancel Order"
                          >
                            <XCircle size={16} />
                          </button>
                        )}
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Trade History Content */}
      {activeTab === 'history' && (
        <div className="bg-slate-900 border border-slate-800 rounded-xl overflow-hidden shadow-sm">
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead className="bg-slate-950/60 text-slate-400 border-b border-slate-800 uppercase tracking-wider font-semibold">
                <tr>
                  <th className="py-3 px-4">Executed Time</th>
                  <th className="py-3 px-4">Symbol</th>
                  <th className="py-3 px-4">Side</th>
                  <th className="py-3 px-4">Product</th>
                  <th className="py-3 px-4">Qty</th>
                  <th className="py-3 px-4">Price</th>
                  <th className="py-3 px-4">Total Value</th>
                  <th className="py-3 px-4">Realized P&amp;L</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-800">
                {trades.length === 0 ? (
                  <tr>
                    <td colSpan={8} className="py-8 text-center text-slate-500">
                      No executed trade history recorded.
                    </td>
                  </tr>
                ) : (
                  trades.map((trd) => {
                    const isSell = trd.side === 'Sell';
                    const hasPnl = isSell && trd.realizedPnL !== undefined && trd.realizedPnL !== null;
                    const isProfit = (trd.realizedPnL ?? 0) >= 0;
                    return (
                      <tr key={trd.executionId} className="hover:bg-slate-800/40 transition-colors">
                        <td className="py-3 px-4 text-slate-400 font-mono">
                          {new Date(trd.executedAtUtc).toLocaleTimeString()}
                        </td>
                        <td className="py-3 px-4 font-bold text-white">{trd.symbol}</td>
                        <td className="py-3 px-4">
                          <span
                            className={`px-2 py-0.5 rounded text-[10px] font-bold uppercase ${
                              trd.side === 'Buy'
                                ? 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/30'
                                : 'bg-rose-500/20 text-rose-400 border border-rose-500/30'
                            }`}
                          >
                            {trd.side}
                          </span>
                        </td>
                        <td className="py-3 px-4 text-slate-300 font-mono">{trd.productType}</td>
                        <td className="py-3 px-4 text-slate-300 font-mono">{trd.quantity}</td>
                        <td className="py-3 px-4 text-slate-300 font-mono">₹{trd.price.toFixed(2)}</td>
                        <td className="py-3 px-4 text-slate-300 font-mono">₹{trd.totalValue.toFixed(2)}</td>
                        <td className="py-3 px-4 font-mono font-bold">
                          {hasPnl ? (
                            <span className={isProfit ? 'text-emerald-400' : 'text-rose-400'}>
                              {isProfit ? '+' : ''}₹{(trd.realizedPnL ?? 0).toFixed(2)}
                            </span>
                          ) : (
                            <span className="text-slate-500">-</span>
                          )}
                        </td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
};
