# Complete Broker Configuration & Setup Guide

This guide provides step-by-step instructions with direct links and configuration parameters to connect your Indian equity brokers (**Upstox**, **Zerodha Kite Connect**, and the **Mock Paper Simulator**) to the Automated Trading Platform.

---

## 1. Quick Start: Mock Simulator (Zero Setup / Instant)

If you want to test trading strategies, execute paper trades, and verify the 2-second BUY rejection window without real broker accounts:

1. Log in to the platform at [http://localhost:5173](http://localhost:5173) (or sign in using demo account `trader1@example.com` / `Password123!`).
2. Go to **Brokers & Accounts** ([http://localhost:5173/brokers](http://localhost:5173/brokers)).
3. Click **"Connect New Broker"**.
4. The dialog defaults to:
   - **Broker Provider**: `Mock Simulator (Mock)`
   - **Display Name**: `Mock Simulator (Paper)`
   - **Trading Mode**: `Paper Trading (Safe)`
   - **API Key**: `mock_api_key_sandbox` *(pre-filled)*
   - **API Secret**: `mock_api_secret_sandbox` *(pre-filled)*
   - **Initial Capital**: `₹5,00,000`
5. Click **"Connect Broker"**.
6. The account is created and **immediately authenticated** with ₹5,00,000 in simulated funds.

---

## 2. Upstox API v2 Setup Guide

Upstox provides REST API v2 and WebSocket market feeds for algorithmic order placement.

### Step 1: Log in to the Upstox Developer Portal
- Open [https://developer.upstox.com](https://developer.upstox.com) in your browser.
- Sign in with your registered Upstox mobile number, 6-digit PIN, and OTP.

### Step 2: Create a New Developer App
- In your developer dashboard, click **"My Apps"** &rarr; **"New App"**.
- Enter an App Name (e.g. `Algo Trading Platform`).

### Step 3: Configure the Crucial Redirect URL
- In the **Redirect URL** field, enter:
  ```text
  http://localhost:5173/brokers/callback
  ```
  *(If hosting in production or Docker on port 80, use `http://<your-host-or-domain>/brokers/callback`)*.
- **Important**: The URL must match character-for-character, or Upstox will reject the OAuth login callback.

### Step 4: Copy API Key & API Secret
- Once saved, your app dashboard displays:
  - **API Key**: (e.g. `2501xxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`)
  - **API Secret**: (secret alphanumeric token)

### Step 5: Save & Authorize in the Trading Platform
1. In the platform, go to **Brokers & Accounts** &rarr; **"Connect New Broker"**.
2. Select **Broker Provider**: `Upstox (Upstox)`.
3. Set **Connection Display Name**: `Upstox Primary`.
4. Choose **Paper Trading** or **Live Trading**.
5. Paste your **API Key** and **API Secret**.
6. Click **"Connect Broker"**.
7. On your newly created Upstox card, click the green **"Log In / Authorize"** button.
8. Approve access in the Upstox OAuth window.
9. You will be redirected back with an active, authenticated green badge!

---

## 3. Zerodha Kite Connect 3.0 Setup Guide

Zerodha Kite Connect provides institutional-grade REST APIs for order execution and portfolio management across NSE and BSE.

### Prerequisites
- Active Zerodha trading account.
- Active Kite Connect developer subscription (₹2,000/month charged by Zerodha on [https://developers.kite.trade](https://developers.kite.trade)).

### Step 1: Log in to the Kite Developer Console
- Open [https://developers.kite.trade](https://developers.kite.trade).
- Log in with your developer email and password.

### Step 2: Create a New App
- Click **"Create New App"**:
  - **App Type**: `Connect`
  - **App Name**: `Algo Trading Platform`
  - **Zerodha Client ID**: Your 6-character Kite User ID (e.g., `AB1234`).
  - **Redirect URL**:
    ```text
    http://localhost:5173/brokers/callback
    ```

### Step 3: Copy API Key & API Secret
- On your app overview page:
  - Copy **API Key** (`api_key`)
  - Copy **API Secret** (`api_secret`)

### Step 4: Save & Authorize in the Trading Platform
1. Open **Brokers & Accounts** &rarr; **"Connect New Broker"**.
2. Select **Broker Provider**: `Zerodha (Zerodha)`.
3. Set **Display Name**: `Zerodha Kite Primary`.
4. Paste your **API Key** and **API Secret**.
5. Click **"Connect Broker"**.
6. Click **"Log In / Authorize"** &rarr; enter your Zerodha credentials & TOTP.
7. The platform exchanges the request token for a daily access token.

### Daily Token Renewal (SEBI Regulatory Requirement)
> [!IMPORTANT]
> Under SEBI guidelines, Zerodha invalidates all API tokens every morning at 06:00 AM IST. Each trading morning before market open (09:15 AM IST), simply visit the **Brokers** page and click **"Log In / Authorize"** once to activate your session for the trading day.

---

## 4. Docker Deployment & Automatic Database Seeding

The platform includes automatic database migration and seeding on every start. Zero manual SQL scripts are required.

### 1-Command Startup:
```bash
docker compose up -d --build
```

### Services Started:
| Service | Container Name | Port | Description |
| :--- | :--- | :--- | :--- |
| **PostgreSQL 17** | `algo-trading-postgres` | `5432` | Relational storage for trades, accounts, orders, and audits |
| **.NET 10 API** | `algo-trading-api` | `5000` | REST API, SignalR Hub, Risk Engine, and Order Router |
| **Worker** | `algo-trading-worker` | - | Background trade execution, price polling, and daily reports |
| **Frontend** | `algo-trading-frontend` | `80` (or `5173` dev) | React 19 / Vite Web Dashboard & PWA |

### What Gets Auto-Seeded on Startup:
1. **Roles**: `Admin` and `User`.
2. **Broker Providers**: `Mock Simulator`, `Upstox`, and `Zerodha`.
3. **Default Demo User**:
   - **Email**: `trader1@example.com`
   - **Password**: `Password123!`
   - **Auto-provisioned**: Mock Simulator connection + `MOCK-ACC-001` equity paper account with ₹5,00,000 balance.
4. **NSE Top Equities**: Pre-seeded instruments for `RELIANCE`, `TCS`, `INFY`, `HDFCBANK`, `ICICIBANK`, `SBIN`, `TATAMOTORS`, `BHARTIARTL`.
5. **Encryption Keys**: All broker API keys and secrets are encrypted at rest with AES-256-GCM.

---

## 5. Risk Safety Gate & Fast-Action 2-Second BUY Window

### How the 2-Second BUY Window Works:
1. When an automated strategy triggers a **BUY** order, the system holds the order in a transient `PendingApproval` state for **2 seconds**.
2. A real-time toast notification with an interactive countdown appears in the dashboard.
3. **If you click "Reject"**: The trade is cancelled immediately with an audit log entry.
4. **If 2 seconds expire**: The safety gate approves the order and dispatches it cleanly to the broker adapter.

### Emergency Kill Switch:
- Located under **Risk & Strategy Settings** ([/settings](http://localhost:5173/settings)).
- Activating the Kill Switch immediately blocks all new order creation and suspends automated strategy execution across all connected accounts.

---

## 6. AI Market Expert Co-Pilot & Mock Simulator Trials

The platform includes an autonomous algorithmic equity advisor powered by configurable AI and quantitative models.

### Key Capabilities:
1. **Holdings Dip Accumulation ("Buy the Dip")**:
   - Ingests your broker's current portfolio holdings and tracks intraday pullbacks.
   - When a quality stock tests a major moving average (e.g., 50 EMA, 200 EMA) or enters oversold territory (RSI < 38), the advisor recommends accumulating additional quantity.
2. **Special Monitor Watchlist**:
   - Add any Indian stock (e.g., `ZOMATO`, `BEL`, `TATASTEEL`) to a high-frequency tracking queue.
   - The engine analyzes 3-timeframe trend alignment, VWAP support, and volume spikes before recommending entries.
3. **Smart Profit Booking & Stop-Loss**:
   - Generates alerts to lock in 50% profits (`BOOK_PROFIT_50`) when positions achieve swing targets or hit upper Bollinger bands.
   - Issues `EXIT_STOP_LOSS` alerts when technical support structures break.
4. **1-Click Interactive Test Trial**:
   - In the **AI Co-Pilot** page (`http://localhost:5173/ai-advisor`), click **"⚡ Test 2s BUY Rejection Window"**.
   - Triggers an algorithmic BUY signal, displays the real-time 2-second countdown toast, and automatically executes the trade into the paper portfolio if not rejected.

### Pluggable AI Configuration (`appsettings.json`):
```json
"AiAdvisor": {
  "Provider": "GeminiPro", // Options: "GeminiPro", "OpenAI", "Claude", "BuiltInQuant"
  "GeminiApiKey": "",      // Paste Google Gemini API Key (or leave blank to use fast built-in C# quant engine)
  "Model": "gemini-1.5-pro",
  "EnableDipAccumulation": true,
  "EnableSpecialWatchlist": true,
  "DefaultConfidenceThreshold": 75
}
```

---

## 7. Frequently Asked Questions (FAQ)

### Q: Why do my secrets stay masked as `***`?
**A:** Secrets are never sent to or displayed in the frontend in plaintext. They are encrypted using authenticated AES-256-GCM before database write and decrypted only in backend memory when communicating directly with broker endpoints.

### Q: Can I run both Paper and Live accounts at the same time?
**A:** Yes. The platform supports multi-tenant, multi-broker isolation. You can have a Mock Simulator paper account running alongside an Upstox live account with separate capital limits and separate risk thresholds.

### Q: Where do I view help inside the running web app?
**A:** Click **"Help / Support"** in the sidebar navigation or visit **[http://localhost:5173/help](http://localhost:5173/help)** anytime.
