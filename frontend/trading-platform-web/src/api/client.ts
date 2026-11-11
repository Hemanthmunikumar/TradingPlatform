import axios, { type AxiosError } from 'axios';
import type { ApiResponse, AuthResponse, PendingTradeApprovalDto } from '../types';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api/v1';

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor: inject Access Token
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('access_token');
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor: handle 401 and refresh token rotation
apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as any;

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;
      const refreshToken = localStorage.getItem('refresh_token');

      if (refreshToken) {
        try {
          const res = await axios.post<ApiResponse<AuthResponse>>(
            `${API_BASE_URL}/auth/refresh-token`,
            { refreshToken }
          );

          if (res.data.success && res.data.data) {
            const { accessToken, refreshToken: newRefreshToken } = res.data.data;
            localStorage.setItem('access_token', accessToken);
            localStorage.setItem('refresh_token', newRefreshToken);

            originalRequest.headers.Authorization = `Bearer ${accessToken}`;
            return apiClient(originalRequest);
          }
        } catch (refreshErr) {
          localStorage.removeItem('access_token');
          localStorage.removeItem('refresh_token');
          localStorage.removeItem('user');
          window.location.href = '/login';
        }
      }
    }

    return Promise.reject(error);
  }
);

export const approvalsApi = {
  getPending: async (tradingAccountId?: string) => {
    const res = await apiClient.get<PendingTradeApprovalDto[]>('/trade-approvals/pending', {
      params: { tradingAccountId },
    });
    return res.data;
  },
  getHistory: async (tradingAccountId?: string, page = 1, pageSize = 50) => {
    const res = await apiClient.get<PendingTradeApprovalDto[]>('/trade-approvals', {
      params: { tradingAccountId, page, pageSize },
    });
    return res.data;
  },
  reject: async (approvalId: string, reason?: string) => {
    const res = await apiClient.post(`/trade-approvals/${approvalId}/reject`, { reason });
    return res.data;
  },
  create: async (data: {
    tradingAccountId: string;
    instrumentId: string;
    symbol: string;
    companyName: string;
    side: string;
    quantity: number;
    indicativePrice: number;
    approvalDurationSeconds?: number;
  }) => {
    const res = await apiClient.post<PendingTradeApprovalDto>('/trade-approvals', data);
    return res.data;
  },
};
