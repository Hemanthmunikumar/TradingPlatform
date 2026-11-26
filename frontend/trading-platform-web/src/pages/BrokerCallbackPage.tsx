import React, { useEffect, useState } from 'react';
import { useNavigate, useSearchParams, Link } from 'react-router-dom';
import { apiClient } from '../api/client';
import type { ApiResponse, BrokerConnectionDto } from '../types';
import { AlertCircle, RefreshCw, CheckCircle2 } from 'lucide-react';

export const BrokerCallbackPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  const [status, setStatus] = useState<'processing' | 'success' | 'error'>('processing');
  const [statusMessage, setStatusMessage] = useState('Exchanging authorization token with broker...');
  const [errorDetails, setErrorDetails] = useState<string | null>(null);

  useEffect(() => {
    const exchangeToken = async () => {
      // Zerodha Kite returns ?action=login&status=success&request_token=xyz
      // Upstox returns ?code=xyz
      const requestToken = searchParams.get('request_token') || searchParams.get('code');
      const authStatus = searchParams.get('status');
      const state = searchParams.get('state');

      if (authStatus === 'cancelled' || searchParams.get('error')) {
        setStatus('error');
        setErrorDetails(searchParams.get('error_description') || searchParams.get('error') || 'Broker authorization was cancelled by the user.');
        return;
      }

      if (!requestToken) {
        setStatus('error');
        setErrorDetails('No authorization code or request token found in callback URL. Please retry login from the Brokers page.');
        return;
      }

      // Retrieve connectionId from state parameter or localStorage
      const connectionId = state || localStorage.getItem('tp_pending_oauth_connection_id');
      if (!connectionId) {
        setStatus('error');
        setErrorDetails('Unable to determine broker connection ID for OAuth callback. Please retry login from the Brokers page.');
        return;
      }

      try {
        const redirectUri = window.location.origin + '/brokers/callback';
        const res = await apiClient.post<ApiResponse<BrokerConnectionDto>>(
          `/brokers/connections/${connectionId}/exchange-token`,
          {
            authorizationCode: requestToken,
            redirectUri,
          }
        );

        if (res.data.success) {
          setStatus('success');
          setStatusMessage(`Successfully authenticated ${res.data.data?.displayName || 'broker'}! Engine is now ready.`);
          localStorage.removeItem('tp_pending_oauth_connection_id');
          setTimeout(() => {
            navigate('/brokers');
          }, 2000);
        } else {
          setStatus('error');
          setErrorDetails(res.data.message || 'Token exchange failed with broker.');
        }
      } catch (err: any) {
        setStatus('error');
        setErrorDetails(
          err.response?.data?.message ||
          err.response?.data?.errors?.[0] ||
          'Failed to exchange authorization token with the broker API. Please ensure your API secret matches.'
        );
      }
    };

    exchangeToken();
  }, [searchParams, navigate]);

  return (
    <div className="min-h-screen bg-slate-950 flex items-center justify-center p-4">
      <div className="bg-slate-900 border border-slate-800 rounded-2xl max-w-md w-full p-8 shadow-2xl text-center space-y-6">
        {status === 'processing' && (
          <>
            <div className="w-16 h-16 mx-auto rounded-full bg-sky-500/10 border border-sky-500/20 flex items-center justify-center">
              <RefreshCw className="w-8 h-8 text-sky-400 animate-spin" />
            </div>
            <div>
              <h2 className="text-xl font-bold text-white tracking-tight">Authenticating Broker</h2>
              <p className="text-xs text-slate-400 mt-2">{statusMessage}</p>
            </div>
          </>
        )}

        {status === 'success' && (
          <>
            <div className="w-16 h-16 mx-auto rounded-full bg-emerald-500/10 border border-emerald-500/20 flex items-center justify-center">
              <CheckCircle2 className="w-8 h-8 text-emerald-400" />
            </div>
            <div>
              <h2 className="text-xl font-bold text-white tracking-tight">Authentication Successful</h2>
              <p className="text-xs text-emerald-400 mt-2">{statusMessage}</p>
              <p className="text-[11px] text-slate-500 mt-3">Redirecting back to Brokers & Accounts in 2 seconds...</p>
            </div>
          </>
        )}

        {status === 'error' && (
          <>
            <div className="w-16 h-16 mx-auto rounded-full bg-rose-500/10 border border-rose-500/20 flex items-center justify-center">
              <AlertCircle className="w-8 h-8 text-rose-400" />
            </div>
            <div>
              <h2 className="text-xl font-bold text-white tracking-tight">Authentication Failed</h2>
              <div className="mt-3 p-3 bg-rose-950/40 border border-rose-800/40 rounded-xl text-left">
                <p className="text-xs text-rose-300 font-mono break-words">{errorDetails}</p>
              </div>
            </div>
            <div className="pt-2 flex flex-col gap-2">
              <Link
                to="/brokers"
                className="w-full py-2.5 px-4 rounded-xl bg-sky-600 hover:bg-sky-500 text-white text-xs font-semibold transition"
              >
                Back to Brokers & Accounts
              </Link>
              <Link
                to="/help"
                className="text-xs text-slate-400 hover:text-slate-300 underline"
              >
                Read Broker OAuth Setup Guide
              </Link>
            </div>
          </>
        )}
      </div>
    </div>
  );
};
