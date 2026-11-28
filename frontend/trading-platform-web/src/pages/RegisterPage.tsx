import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Lock, Mail, User, Globe, AlertCircle, ArrowRight, CheckCircle2, Eye, EyeOff } from 'lucide-react';
import { useAuth } from '../context/AuthContext';
import { apiClient } from '../api/client';
import type { ApiResponse, AuthResponse } from '../types';

export const RegisterPage: React.FC = () => {
  const navigate = useNavigate();
  const { login } = useAuth();

  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [timezone, setTimezone] = useState('Asia/Kolkata');
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setIsLoading(true);

    try {
      const res = await apiClient.post<ApiResponse<AuthResponse>>('/auth/register', {
        email,
        password,
        fullName,
        timezone,
      });

      if (res.data.success && res.data.data) {
        login(res.data.data);
        navigate('/');
      } else {
        setError(res.data.message || 'Registration failed.');
      }
    } catch (err: any) {
      setError(
        err.response?.data?.message ||
        err.response?.data?.errors?.[0] ||
        'Failed to connect to the backend registration service.'
      );
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen w-full bg-slate-950 flex items-center justify-center py-12 sm:px-6 lg:px-8 px-4 text-slate-100">
      <div className="w-full max-w-md mx-auto">
        <div className="text-center">
          <div className="w-12 h-12 rounded-xl bg-gradient-to-tr from-sky-500 to-indigo-600 flex items-center justify-center font-black text-2xl text-white shadow-xl shadow-sky-500/25 mx-auto">
            SS
          </div>
          <h2 className="mt-4 text-3xl font-extrabold tracking-tight text-white">
            Create StrikeSutra Account
          </h2>
          <p className="mt-2 text-sm text-slate-400">
            Broker-isolated multi-tenant trading environment
          </p>
        </div>

        <div className="mt-8">
        <div className="bg-slate-900/90 py-8 px-6 shadow-2xl rounded-2xl border border-slate-800 sm:px-10 backdrop-blur-md">
          {error && (
            <div className="mb-6 p-3.5 rounded-xl bg-rose-500/10 border border-rose-500/30 text-rose-400 text-sm flex items-start gap-2.5">
              <AlertCircle size={18} className="shrink-0 mt-0.5" />
              <span>{error}</span>
            </div>
          )}

          <form className="space-y-4" onSubmit={handleSubmit}>
            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1">
                Full Legal Name
              </label>
              <div className="relative rounded-lg shadow-sm">
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-500">
                  <User size={18} />
                </div>
                <input
                  type="text"
                  required
                  value={fullName}
                  onChange={(e) => setFullName(e.target.value)}
                  placeholder="Rahul Sharma"
                  className="block w-full pl-10 pr-3 py-2.5 bg-slate-950/80 border border-slate-700/80 rounded-lg text-slate-100 placeholder-slate-500 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-transparent transition"
                />
              </div>
            </div>

            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1">
                Email Address
              </label>
              <div className="relative rounded-lg shadow-sm">
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-500">
                  <Mail size={18} />
                </div>
                <input
                  type="email"
                  required
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="trader@domain.com"
                  className="block w-full pl-10 pr-3 py-2.5 bg-slate-950/80 border border-slate-700/80 rounded-lg text-slate-100 placeholder-slate-500 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-transparent transition"
                />
              </div>
            </div>

            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1">
                Password
              </label>
              <div className="relative rounded-lg shadow-sm">
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-500">
                  <Lock size={18} />
                </div>
                <input
                  type={showPassword ? 'text' : 'password'}
                  required
                  minLength={8}
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="At least 8 characters"
                  className="block w-full pl-10 pr-10 py-2.5 bg-slate-950/80 border border-slate-700/80 rounded-lg text-slate-100 placeholder-slate-500 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-transparent transition"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword((prev) => !prev)}
                  className="absolute inset-y-0 right-0 pr-3 flex items-center text-slate-400 hover:text-slate-200 transition"
                  aria-label={showPassword ? 'Hide password' : 'Show password'}
                >
                  {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                </button>
              </div>
            </div>

            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1">
                Timezone
              </label>
              <div className="relative rounded-lg shadow-sm">
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-500">
                  <Globe size={18} />
                </div>
                <select
                  value={timezone}
                  onChange={(e) => setTimezone(e.target.value)}
                  className="block w-full pl-10 pr-3 py-2.5 bg-slate-950/80 border border-slate-700/80 rounded-lg text-slate-100 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-transparent transition"
                >
                  <option value="Asia/Kolkata">Asia/Kolkata (IST — UTC+05:30)</option>
                  <option value="UTC">UTC</option>
                  <option value="Asia/Dubai">Asia/Dubai (GST)</option>
                  <option value="Europe/London">Europe/London (GMT/BST)</option>
                </select>
              </div>
            </div>

            <div className="pt-2 text-xs text-slate-400 space-y-1">
              <div className="flex items-center gap-1.5 text-slate-300">
                <CheckCircle2 size={13} className="text-emerald-400" />
                <span>Paper trading mode enabled by default</span>
              </div>
              <div className="flex items-center gap-1.5 text-slate-300">
                <CheckCircle2 size={13} className="text-emerald-400" />
                <span>Separate broker account credential isolation</span>
              </div>
            </div>

            <button
              type="submit"
              disabled={isLoading}
              className="w-full mt-2 flex items-center justify-center gap-2 py-3 px-4 border border-transparent rounded-lg shadow-md text-sm font-semibold text-white bg-gradient-to-r from-sky-500 to-indigo-600 hover:from-sky-400 hover:to-indigo-500 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-sky-500 disabled:opacity-50 disabled:cursor-not-allowed transition transform active:scale-[0.99]"
            >
              {isLoading ? (
                <span>Setting Up Account...</span>
              ) : (
                <>
                  <span>Create Account</span>
                  <ArrowRight size={16} />
                </>
              )}
            </button>
          </form>

          <div className="mt-6 pt-6 border-t border-slate-800 text-center text-sm text-slate-400">
            Already have an account?{' '}
            <Link to="/login" className="font-semibold text-sky-400 hover:text-sky-300 transition">
              Sign In
            </Link>
          </div>
        </div>
      </div>
      </div>
    </div>
  );
};
