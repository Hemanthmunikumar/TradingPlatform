import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Lock, Mail, AlertCircle, ArrowRight, ShieldCheck, Eye, EyeOff, KeyRound, CheckCircle2, X } from 'lucide-react';
import { useAuth } from '../context/AuthContext';
import { apiClient } from '../api/client';
import type { ApiResponse, AuthResponse } from '../types';

export const LoginPage: React.FC = () => {
  const navigate = useNavigate();
  const { login } = useAuth();

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  // Forgot / Reset Password Modal State
  const [isForgotOpen, setIsForgotOpen] = useState(false);
  const [forgotStep, setForgotStep] = useState<'email' | 'reset'>('email');
  const [forgotEmail, setForgotEmail] = useState('');
  const [resetCode, setResetCode] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [showNewPassword, setShowNewPassword] = useState(false);
  const [forgotMsg, setForgotMsg] = useState<string | null>(null);
  const [forgotErr, setForgotErr] = useState<string | null>(null);
  const [isForgotLoading, setIsForgotLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setIsLoading(true);

    try {
      const res = await apiClient.post<ApiResponse<AuthResponse>>('/auth/login', {
        email,
        password,
      });

      if (res.data.success && res.data.data) {
        login(res.data.data);
        navigate('/');
      } else {
        setError(res.data.message || 'Login failed. Please check your credentials.');
      }
    } catch (err: any) {
      setError(
        err.response?.data?.message ||
        err.response?.data?.errors?.[0] ||
        'Unable to connect to trading engine. Please verify the backend is running.'
      );
    } finally {
      setIsLoading(false);
    }
  };

  const handleSendResetCode = async (e: React.FormEvent) => {
    e.preventDefault();
    setForgotErr(null);
    setForgotMsg(null);
    setIsForgotLoading(true);
    try {
      const res = await apiClient.post<ApiResponse<boolean>>('/auth/forgot-password', {
        email: forgotEmail,
      });
      setForgotMsg(res.data.message || 'Verification code sent to your email.');
      setForgotStep('reset');
    } catch (err: any) {
      setForgotErr(err.response?.data?.message || 'Unable to send reset code.');
    } finally {
      setIsForgotLoading(false);
    }
  };

  const handleResetPassword = async (e: React.FormEvent) => {
    e.preventDefault();
    setForgotErr(null);
    setForgotMsg(null);
    setIsForgotLoading(true);
    try {
      const res = await apiClient.post<ApiResponse<boolean>>('/auth/reset-password', {
        email: forgotEmail,
        code: resetCode,
        newPassword,
      });
      setForgotMsg(res.data.message || 'Password updated! You can now sign in.');
      setEmail(forgotEmail);
      setTimeout(() => {
        setIsForgotOpen(false);
        setForgotStep('email');
        setResetCode('');
        setNewPassword('');
        setForgotMsg(null);
      }, 1600);
    } catch (err: any) {
      setForgotErr(err.response?.data?.message || 'Failed to reset password.');
    } finally {
      setIsForgotLoading(false);
    }
  };

  return (
    <div className="relative min-h-screen w-full bg-slate-950 flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8 text-slate-100 overflow-hidden">
      {/* Subtle Ambient Glow */}
      <div className="pointer-events-none absolute -top-40 left-1/2 -translate-x-1/2 w-[560px] h-[560px] rounded-full bg-sky-500/10 blur-3xl" />
      <div className="pointer-events-none absolute -bottom-40 right-1/4 w-[420px] h-[420px] rounded-full bg-indigo-600/10 blur-3xl" />

      <div className="relative z-10 w-full max-w-md mx-auto">
        <div className="text-center">
          <div className="w-14 h-14 rounded-2xl bg-gradient-to-tr from-sky-500 via-indigo-500 to-emerald-500 flex items-center justify-center font-black text-2xl text-white shadow-2xl shadow-sky-500/30 mx-auto ring-1 ring-white/15">
            SS
          </div>
          <h2 className="mt-5 text-3xl font-extrabold tracking-tight bg-gradient-to-r from-white via-slate-100 to-slate-400 bg-clip-text text-transparent">
            Sign In to StrikeSutra
          </h2>
          <p className="mt-2 text-sm text-slate-400">
            Precision Indian NSE/BSE F&amp;O &amp; Equity Algorithmic Trading
          </p>
        </div>

        <div className="mt-8 bg-slate-900/90 py-8 px-6 shadow-2xl shadow-black/60 rounded-2xl border border-slate-800/90 sm:px-10 backdrop-blur-xl">
          {/* Pre-Configured Demo Account (Username only, no password displayed) */}
          <div className="mb-5 p-3 rounded-xl bg-sky-500/10 border border-sky-500/20 flex items-center justify-between">
            <div>
              <p className="text-xs font-bold text-sky-400">Pre-Configured Demo Account</p>
              <p className="text-[11px] text-slate-400 font-mono">trader@strikesutra.com</p>
            </div>
            <button
              type="button"
              onClick={() => setEmail('trader@strikesutra.com')}
              className="px-2.5 py-1 text-xs font-semibold bg-sky-500 hover:bg-sky-400 text-white rounded-lg shadow transition"
            >
              Fill Email
            </button>
          </div>

          {error && (
            <div className="mb-6 p-3.5 rounded-xl bg-rose-500/10 border border-rose-500/30 text-rose-400 text-sm flex items-start gap-2.5">
              <AlertCircle size={18} className="shrink-0 mt-0.5" />
              <span>{error}</span>
            </div>
          )}

          <form className="space-y-5" onSubmit={handleSubmit}>
            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1.5">
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
                  placeholder="you@domain.com"
                  className="block w-full pl-10 pr-3 py-2.5 bg-slate-950/80 border border-slate-700/80 rounded-lg text-slate-100 placeholder-slate-500 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-transparent transition"
                />
              </div>
            </div>

            <div>
              <div className="flex items-center justify-between mb-1.5">
                <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300">
                  Password
                </label>
                <button
                  type="button"
                  onClick={() => {
                    setForgotEmail(email);
                    setForgotStep('email');
                    setForgotErr(null);
                    setForgotMsg(null);
                    setIsForgotOpen(true);
                  }}
                  className="text-xs font-semibold text-sky-400 hover:text-sky-300 transition"
                >
                  Forgot password?
                </button>
              </div>
              <div className="relative rounded-lg shadow-sm">
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-500">
                  <Lock size={18} />
                </div>
                <input
                  type={showPassword ? 'text' : 'password'}
                  required
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="••••••••••••"
                  className="block w-full pl-10 pr-10 py-2.5 bg-slate-950/80 border border-slate-700/80 rounded-lg text-slate-100 placeholder-slate-500 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-transparent transition"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword((prev) => !prev)}
                  className="absolute inset-y-0 right-0 pr-3 flex items-center text-slate-400 hover:text-slate-200 transition"
                  aria-label={showPassword ? 'Hide password' : 'Show password'}
                >
                  {showPassword ? <EyeOff size={17} /> : <Eye size={17} />}
                </button>
              </div>
            </div>

            <div className="flex items-center justify-between text-xs text-slate-400">
              <div className="flex items-center gap-1.5">
                <ShieldCheck size={14} className="text-emerald-400" />
                <span>AES-256-GCM Secured</span>
              </div>
              <span className="text-slate-500">IST Market Hours</span>
            </div>

            <button
              type="submit"
              disabled={isLoading}
              className="w-full flex items-center justify-center gap-2 py-3 px-4 border border-transparent rounded-lg shadow-md text-sm font-semibold text-white bg-gradient-to-r from-sky-500 to-indigo-600 hover:from-sky-400 hover:to-indigo-500 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-sky-500 disabled:opacity-50 disabled:cursor-not-allowed transition transform active:scale-[0.99]"
            >
              {isLoading ? (
                <span>Authenticating...</span>
              ) : (
                <>
                  <span>Sign In to Dashboard</span>
                  <ArrowRight size={16} />
                </>
              )}
            </button>
          </form>

          <div className="mt-6 pt-6 border-t border-slate-800 text-center text-sm text-slate-400">
            Don&apos;t have an account?{' '}
            <Link to="/register" className="font-semibold text-sky-400 hover:text-sky-300 transition">
              Create an account
            </Link>
          </div>
        </div>
      </div>

      {/* Forgot / Reset Password Modal */}
      {isForgotOpen && (
        <div className="fixed inset-0 z-50 bg-black/75 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="bg-slate-900 border border-slate-700 rounded-2xl w-full max-w-md p-6 shadow-2xl">
            <div className="flex items-center justify-between pb-3 border-b border-slate-800">
              <h3 className="text-base font-bold text-white flex items-center gap-2">
                <KeyRound size={18} className="text-sky-400" />
                {forgotStep === 'email' ? 'Forgot Password' : 'Enter Reset Code'}
              </h3>
              <button
                type="button"
                onClick={() => setIsForgotOpen(false)}
                className="text-slate-400 hover:text-white p-1 rounded"
              >
                <X size={18} />
              </button>
            </div>

            {forgotErr && (
              <div className="mt-4 p-3 rounded-lg bg-rose-500/10 border border-rose-500/30 text-rose-300 text-xs flex items-center gap-2">
                <AlertCircle size={15} />
                <span>{forgotErr}</span>
              </div>
            )}

            {forgotMsg && (
              <div className="mt-4 p-3 rounded-lg bg-emerald-500/10 border border-emerald-500/30 text-emerald-300 text-xs flex items-center gap-2">
                <CheckCircle2 size={15} />
                <span>{forgotMsg}</span>
              </div>
            )}

            {forgotStep === 'email' ? (
              <form onSubmit={handleSendResetCode} className="mt-4 space-y-4">
                <p className="text-xs text-slate-400">
                  Enter your registered email address and we&apos;ll send a 6-digit password reset code via Gmail SMTP.
                </p>
                <div>
                  <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1.5">
                    Email Address
                  </label>
                  <input
                    type="email"
                    required
                    value={forgotEmail}
                    onChange={(e) => setForgotEmail(e.target.value)}
                    placeholder="hemanthmunikumar@gmail.com"
                    className="w-full px-3 py-2.5 bg-slate-950 border border-slate-700 rounded-lg text-slate-100 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                  />
                </div>
                <div className="flex justify-end gap-2 pt-2">
                  <button
                    type="button"
                    onClick={() => setIsForgotOpen(false)}
                    className="px-4 py-2 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-semibold"
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    disabled={isForgotLoading}
                    className="px-4 py-2 rounded-lg bg-sky-600 hover:bg-sky-500 text-white text-xs font-bold disabled:opacity-50"
                  >
                    {isForgotLoading ? 'Sending...' : 'Send Reset Code'}
                  </button>
                </div>
              </form>
            ) : (
              <form onSubmit={handleResetPassword} className="mt-4 space-y-4">
                <div>
                  <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1.5">
                    6-Digit Verification Code
                  </label>
                  <input
                    type="text"
                    required
                    value={resetCode}
                    onChange={(e) => setResetCode(e.target.value)}
                    placeholder="123456"
                    className="w-full px-3 py-2.5 bg-slate-950 border border-slate-700 rounded-lg text-slate-100 text-sm font-mono tracking-widest focus:outline-none focus:ring-2 focus:ring-sky-500"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold uppercase tracking-wider text-slate-300 mb-1.5">
                    New Password
                  </label>
                  <div className="relative">
                    <input
                      type={showNewPassword ? 'text' : 'password'}
                      required
                      minLength={8}
                      value={newPassword}
                      onChange={(e) => setNewPassword(e.target.value)}
                      placeholder="Minimum 8 characters"
                      className="w-full pl-3 pr-10 py-2.5 bg-slate-950 border border-slate-700 rounded-lg text-slate-100 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                    />
                    <button
                      type="button"
                      onClick={() => setShowNewPassword((p) => !p)}
                      className="absolute inset-y-0 right-0 pr-3 flex items-center text-slate-400 hover:text-slate-200"
                    >
                      {showNewPassword ? <EyeOff size={17} /> : <Eye size={17} />}
                    </button>
                  </div>
                </div>
                <div className="flex justify-end gap-2 pt-2">
                  <button
                    type="button"
                    onClick={() => setForgotStep('email')}
                    className="px-4 py-2 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-semibold"
                  >
                    Back
                  </button>
                  <button
                    type="submit"
                    disabled={isForgotLoading}
                    className="px-4 py-2 rounded-lg bg-emerald-600 hover:bg-emerald-500 text-white text-xs font-bold disabled:opacity-50"
                  >
                    {isForgotLoading ? 'Updating...' : 'Reset Password'}
                  </button>
                </div>
              </form>
            )}
          </div>
        </div>
      )}
    </div>
  );
};
