# StrikeSutra

**StrikeSutra** is a self-hosted, broker-independent Indian algorithmic trading platform for NSE/BSE equities and F&O options, featuring an AI Market Co-Pilot, Pre-Market 21-EMA pullback & ATM option strike scanner, risk guardrails, and Zerodha Kite / Upstox / Paper execution.

## Core Capabilities
- **Pre-Market F&O 21-EMA Scanner**: Filters NSE F&O stocks up `1.0% – 2.5%` in a confirmed uptrend with positive news support and volume `>= 20,000`, auto-selects ATM Call Option strikes, and tracks 5m pullbacks to the 21 EMA with option-based Stop Loss (`-2.5%` below 21 EMA) and Target (`+10%`).
- **Special Monitor & Watchlist**: Live NSE/BSE symbol validation, 21-EMA diagnostics, and 1-click buy tickets.
- **Broker & Mode Controls**: Paper Trading & Live Trading mode switch with Zerodha Kite Connect and Upstox adapters, plus SEBI/NSE rate-limit & order-tagging guards.
- **Configurable Risk Guardrails**: Separate Intraday (MIS) and Delivery (CNC) limits for volume, SL %, target %, max capital allocation, single order value, daily loss limit, and max open positions.
