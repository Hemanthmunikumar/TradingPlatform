import React from 'react';
import { TrendingUp, Layers } from 'lucide-react';

export const OrdersPage: React.FC = () => {
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
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {/* Open Positions Card */}
        <div className="bg-slate-900 border border-slate-800 rounded-xl p-5 shadow-sm space-y-4">
          <div className="flex items-center justify-between pb-3 border-b border-slate-800">
            <h3 className="text-sm font-bold text-white uppercase tracking-wider flex items-center gap-2">
              <TrendingUp size={16} className="text-emerald-400" />
              Open Positions (0)
            </h3>
            <span className="text-xs text-slate-500">Intraday Square-off 15:15 IST</span>
          </div>

          <div className="p-8 text-center text-slate-500 text-xs">
            No open market positions.
          </div>
        </div>

        {/* Today's Order Book */}
        <div className="bg-slate-900 border border-slate-800 rounded-xl p-5 shadow-sm space-y-4">
          <div className="flex items-center justify-between pb-3 border-b border-slate-800">
            <h3 className="text-sm font-bold text-white uppercase tracking-wider flex items-center gap-2">
              <Layers size={16} className="text-sky-400" />
              Today&apos;s Orders (0)
            </h3>
            <span className="text-xs text-slate-500">FIFO Matched</span>
          </div>

          <div className="p-8 text-center text-slate-500 text-xs">
            No orders submitted today.
          </div>
        </div>
      </div>
    </div>
  );
};
