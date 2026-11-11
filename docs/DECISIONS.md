# Architecture Decision Records

> **Document Version:** 1.0  
> **Last Updated:** 2026-11-11  
> **Status:** Draft — Milestone 0  

This document records all significant architecture decisions for the Indian Stock Trading Platform. Each decision follows the ADR (Architecture Decision Record) format to capture context, rationale, and consequences.

---

## Table of Contents

| ADR   | Title                                      | Status   |
|-------|--------------------------------------------|----------|
| 001   | .NET 10 LTS as Backend Framework           | Accepted |
| 002   | Modular Monolith Architecture              | Accepted |
| 003   | PostgreSQL as Database                     | Accepted |
| 004   | React + TypeScript + Vite for Frontend     | Accepted |
| 005   | Adapter Pattern for Broker Integration     | Accepted |
| 006   | Server-Side BUY Approval Timer             | Accepted |
| 007   | Paper Trading as Default                   | Accepted |
| 008   | Tenant Isolation from Day One              | Accepted |
| 009   | AI as Advisory Only                        | Accepted |
| 010   | Docker-Based Deployment                    | Accepted |
| 011   | SignalR for Real-Time Notifications        | Accepted |
| 012   | Serilog for Structured Logging             | Accepted |
| 013   | EF Core Code-First Migrations             | Accepted |
| 014   | Idempotent Order Execution                 | Accepted |

---

## ADR-001: .NET 10 LTS as Backend Framework

**Status:** Accepted  
**Date:** 2026-11-11  
**Deciders:** Principal Architect  

### Context

The platform requires a backend framework that is:
- Production-stable for a financial application
- Supported with security patches for a minimum of 3 years
- Performant for real-time trading operations
- Well-integrated with PostgreSQL via Entity Framework Core

At the time of this decision, .NET 10 is the current LTS (Long-Term Support) release, supported until November 2028. .NET 11 is available as an STS (Standard-Term Support) release with 18 months of support.

### Decision

Use **.NET 10 LTS** (target framework `net10.0`) for all backend projects (API, Worker Service, Application, Domain, Infrastructure).

### Consequences

**Positive:**
- Security patches and bug fixes guaranteed until November 2028
- Mature ecosystem, stable APIs unlikely to break
- Strong async/await support for I/O-bound trading operations
- Excellent performance benchmarks for HTTP and WebSocket workloads
- First-class Docker and Linux support

**Negative:**
- May miss features introduced in .NET 11 (STS)
- Upgrade to .NET 12 LTS (expected November 2027) will require testing

**Mitigation:**
- Can upgrade to .NET 12 LTS when available, well before .NET 10 EOL
- No dependency on .NET 11-only features identified

---

## ADR-002: Modular Monolith Architecture

**Status:** Accepted  
**Date:** 2026-11-11  
**Deciders:** Principal Architect  

### Context

The platform is built by a single developer for initial use by 1–5 users. Architecture choices include:
1. **Microservices** — Independent services communicating over HTTP/messaging
2. **Traditional monolith** — Single project, minimal separation
3. **Modular monolith** — Single deployment unit with strict internal module boundaries

Microservices add operational complexity (service discovery, distributed tracing, eventual consistency) that is not justified at this scale. A traditional monolith risks becoming a tangle of dependencies.

### Decision

Use a **modular monolith** with clean layer separation:
- `TradingPlatform.Domain` — Entities, value objects, enums, domain interfaces
- `TradingPlatform.Application` — Business logic, services, DTOs, CQRS patterns
- `TradingPlatform.Infrastructure` — EF Core, broker adapters, external integrations
- `TradingPlatform.API` — HTTP endpoints, SignalR hubs, middleware
- `TradingPlatform.Worker` — Background processing service (separate executable)

The Worker Service is a separate .NET executable but shares the same codebase, Domain, Application, and Infrastructure layers.

### Consequences

**Positive:**
- Single deployment unit (Docker Compose) simplifies operations
- Shared database avoids distributed transaction complexity
- In-process method calls instead of network hops
- Easier debugging with a single debugger session
- Can evolve to microservices by extracting modules along existing boundaries

**Negative:**
- All modules share the same process (API); a crash affects everything
- Cannot scale modules independently (until extracted)
- Worker is separate process but shares the same database

**Mitigation:**
- Worker process isolation prevents background task crashes from affecting the API
- Module boundaries enforced via project references (Domain has no outward dependencies)
- Scaling path documented: extract Worker functions, add API replicas behind load balancer

---

## ADR-003: PostgreSQL as Database

**Status:** Accepted  
**Date:** 2026-11-11  
**Deciders:** Principal Architect  

### Context

The platform needs a relational database for:
- Order management with ACID transactions
- User and account data with referential integrity
- Audit trails and trade history
- JSONB support for flexible metadata (strategy parameters, broker responses)
- Table partitioning for high-volume trade data

Options considered:
1. **SQL Server** — Excellent .NET integration, but licensing cost and heavier resource usage
2. **PostgreSQL** — Open-source, JSONB support, excellent partitioning, strong EF Core provider
3. **SQLite** — Too limited for production multi-user, concurrent write issues

### Decision

Use **PostgreSQL 17** running via Docker in all environments.

### Consequences

**Positive:**
- No licensing costs
- Native JSONB for flexible metadata storage
- Table partitioning for time-series trade data
- Excellent EF Core provider (Npgsql)
- Identical database engine in development and production
- No local installation required — Docker container in all environments

**Negative:**
- Requires Docker Desktop for local development
- Npgsql EF Core provider has minor behavioral differences from SQL Server provider
- Team must be familiar with PostgreSQL-specific features

**Mitigation:**
- Docker Desktop is already required for the deployment strategy
- Npgsql provider is mature and well-documented
- PostgreSQL-specific features (JSONB, partitioning) are opt-in enhancements

---

## ADR-004: React + TypeScript + Vite for Frontend

**Status:** Accepted  
**Date:** 2026-11-11  
**Deciders:** Principal Architect  

### Context

The frontend requires:
- Responsive, mobile-first design (primary usage on mobile during market hours)
- Real-time updates (trade approvals, P&L, order status)
- PWA capabilities for mobile app-like experience
- Rich interactive dashboard with charts and tables

Options considered:
1. **React** — Largest ecosystem, excellent SignalR client, strong PWA tooling
2. **Angular** — Batteries-included, but heavier for a small team
3. **Blazor WASM** — Full .NET stack, but larger bundle size and less mature PWA support
4. **Vue.js** — Good option, but smaller ecosystem for financial dashboards

### Decision

Use **React** with **TypeScript** and **Vite** as the build tool.

### Consequences

**Positive:**
- TypeScript provides compile-time type safety for complex trading data models
- Vite offers fast development builds and optimized production bundles
- Extensive library ecosystem (charting, tables, forms)
- Official Microsoft SignalR client for React
- Excellent PWA support via `vite-plugin-pwa`
- Large talent pool if team grows

**Negative:**
- Separate technology stack from backend (not Blazor)
- Requires Node.js toolchain in addition to .NET
- More boilerplate for state management compared to Angular

**Mitigation:**
- TypeScript DTOs can be generated from .NET OpenAPI spec for type consistency
- Node.js is lightweight and well-understood
- State management kept simple with React Context + hooks initially

---

## ADR-005: Adapter Pattern for Broker Integration

**Status:** Accepted  
**Date:** 2026-11-11  
**Deciders:** Principal Architect  

### Context

Indian stock brokers (Zerodha/Kite, Upstox, Angel One, etc.) each have unique APIs with different:
- Authentication flows (OAuth 2.0 variants, API key + secret)
- Order placement formats and parameters
- Market data endpoints and WebSocket protocols
- Response schemas and error codes

The application must support multiple brokers without coupling domain logic to any specific broker's API.

### Decision

Implement the **Adapter Pattern** with an `IBrokerAdapter` interface. Each broker gets a separate adapter implementation that translates between the platform's domain model and the broker's API.

```
IBrokerAdapter
├── MockBrokerAdapter        (paper trading)
├── UpstoxBrokerAdapter      (live trading)
├── ZerodhaBrokerAdapter     (live trading)
└── AngelOneBrokerAdapter    (live trading, future)
```

### Consequences

**Positive:**
- Domain code never references broker-specific types
- New brokers added by implementing a single interface
- Mock adapter enables complete paper trading without any broker connection
- Each adapter is independently testable
- Broker-specific quirks are isolated to their adapter

**Negative:**
- Interface must be generic enough to support all brokers
- Lowest common denominator risk — some broker-specific features may not fit the interface
- Each new broker requires understanding their API documentation

**Mitigation:**
- Interface designed with extension points (metadata dictionaries, optional capabilities)
- Adapter factory resolves the correct adapter per broker-account configuration
- Start with MockBrokerAdapter and one real broker (Upstox), refine interface based on real usage

---

## ADR-006: Server-Side BUY Approval Timer

**Status:** Accepted  
**Date:** 2026-11-11  
**Deciders:** Principal Architect  

### Context

The platform implements a 2-second rejection window for BUY signals: the user has 2 seconds to reject a BUY order before it auto-executes. This is a safety mechanism that must work reliably regardless of client state.

Two approaches considered:
1. **Client-side timer**: Browser runs the countdown, sends execute/reject to API
2. **Server-side timer**: Backend tracks the deadline, Worker processes after expiry

Client-side timer fails if the browser is closed, the tab is backgrounded (mobile throttling), or the network is temporarily unavailable.

### Decision

Use a **server-side approval timer**:
1. Strategy generates a BUY signal
2. API creates a `PendingTradeApproval` record with `Deadline = now + 2 seconds`
3. SignalR pushes notification to connected clients (toast with countdown)
4. If user rejects within 2 seconds → API marks approval as `Rejected`
5. If deadline passes → Worker picks up the approval, re-validates, and executes
6. UI countdown is **display only** — the server is the source of truth

### Consequences

**Positive:**
- Works even if browser is closed — Worker processes server-side deadline
- No dependency on client clock accuracy
- Durable state survives API restarts (approval is in database)
- Auditable — every approval state change is recorded

**Negative:**
- Slightly more complex than client-side timer
- Worker must poll or be notified of pending approvals frequently (sub-second responsiveness)
- Database write for every BUY signal (even if user is watching)

**Mitigation:**
- Worker polls `PendingTradeApproval` table at 200ms intervals during market hours
- Database write is lightweight (single row insert)
- Re-validation before execution catches stale signals

---

## ADR-007: Paper Trading as Default

**Status:** Accepted  
**Date:** 2026-11-11  
**Deciders:** Principal Architect  

### Context

A trading platform that can execute real orders carries inherent financial risk. During development, testing, and initial deployment, accidental live order execution must be impossible.

### Decision

**Paper trading is the default mode** for all accounts and all environments:
- `Trading:LiveTradingEnabled = false` in default configuration
- Each account must be explicitly configured for live trading
- Live trading requires: valid broker credentials, authenticated session, and explicit configuration flag
- Auto-execute on timeout defaults to OFF for live accounts

### Consequences

**Positive:**
- Zero risk of accidental live trading during development
- New accounts start in safe mode
- Complete end-to-end testing possible without broker connections
- Gradual transition: paper trade → verify → enable live

**Negative:**
- Additional configuration step required to enable live trading
- Must maintain parity between paper and live execution paths

**Mitigation:**
- Clear documentation and UI indicators for trading mode
- MockBrokerAdapter simulates realistic execution including slippage and partial fills
- Mode indicator prominently displayed in dashboard header

---

## ADR-008: Tenant Isolation from Day One

**Status:** Accepted  
**Date:** 2026-11-11  
**Deciders:** Principal Architect  

### Context

The initial deployment supports a single user, but the architecture should accommodate multiple users without schema changes or significant refactoring.

### Decision

Implement **row-level tenant isolation** using EF Core global query filters:
- Every tenant-scoped entity includes a `UserId` foreign key
- EF Core `HasQueryFilter(e => e.UserId == currentUserId)` applied to all tenant entities
- The `currentUserId` is resolved from the JWT claim via `ICurrentUserService`
- All queries automatically filter by the authenticated user
- Admin operations can bypass filters when explicitly required

### Consequences

**Positive:**
- Multi-user ready without schema migration
- No risk of cross-tenant data leakage (enforced at ORM level)
- Simple shared-database multi-tenancy (no per-tenant database complexity)
- Security tests can verify isolation with multiple test users

**Negative:**
- Every query includes a WHERE clause for UserId (minor performance impact)
- Must remember to include UserId when seeding data or writing raw SQL
- Admin/support queries require explicit filter bypass

**Mitigation:**
- UserId column is indexed on all tenant-scoped tables
- Base entity class enforces UserId presence
- Integration tests specifically verify cross-tenant isolation

---

## ADR-009: AI as Advisory Only

**Status:** Accepted  
**Date:** 2026-11-11  
**Deciders:** Principal Architect  

### Context

The platform integrates AI capabilities for:
- News sentiment analysis
- Market condition assessment
- Trade opportunity identification

AI models (LLMs, sentiment models) can produce inconsistent outputs, hallucinate data, or fail silently. Using AI output directly in order execution creates unpredictable and potentially dangerous behavior.

### Decision

**AI is advisory only** — it provides information and recommendations but never directly triggers or modifies order execution:
- AI output feeds into a `MarketInsight` or `SentimentScore` that users can view
- All trading rules, indicator calculations, and entry/exit logic are **deterministic C# code**
- Strategy signals are generated by technical indicators (EMA, RSI, MACD, etc.) with exact, reproducible calculations
- AI sentiment may influence strategy parameters (e.g., reduce position size in negative sentiment) but through explicit, configurable rules

### Consequences

**Positive:**
- Trading behavior is predictable and reproducible
- AI failures do not affect order execution
- Can backtest strategies with deterministic results
- Easier to debug and audit trading decisions
- Regulatory compliance: trading logic is explainable

**Negative:**
- Cannot implement fully autonomous AI-driven trading strategies
- AI integration is supplementary rather than core

**Mitigation:**
- AI insights displayed prominently in dashboard for human decision support
- Future option to add AI-influenced parameters with explicit user opt-in and safety rails

---

## ADR-010: Docker-Based Deployment

**Status:** Accepted  
**Date:** 2026-11-11  
**Deciders:** Principal Architect  

### Context

The development environment is Windows 10 and the production environment is Ubuntu VPS. Deployment must be consistent across these platforms, reproducible, and simple to manage for a single developer.

### Decision

Use **Docker Compose** for all environments:
- Development: `docker-compose.dev.yml` (database only, API/Worker/Frontend run natively)
- Production: `docker-compose.yml` (all four containers)
- Private Docker network (`trading-net`) for inter-container communication
- Host Nginx reverse proxy for HTTPS termination

### Consequences

**Positive:**
- Identical runtime environment in dev and prod
- Single `docker compose up` to start the entire stack
- Easy to add services (Redis, Seq) later
- Container isolation for security
- Reproducible builds via Dockerfiles

**Negative:**
- Docker Desktop required for local development (resource usage on Windows)
- Docker adds a layer of complexity for debugging
- Image build times during development

**Mitigation:**
- Dev mode runs API/Worker/Frontend natively (Docker only for PostgreSQL)
- Multi-stage Dockerfiles minimize image size
- Docker Compose profiles can selectively start services

---

## ADR-011: SignalR for Real-Time Notifications

**Status:** Accepted  
**Date:** 2026-11-11  
**Deciders:** Principal Architect  

### Context

The platform requires real-time push notifications for:
- BUY approval countdown (2-second window)
- Order status updates (placed, filled, rejected)
- P&L updates
- SELL execution notifications with trade details
- System alerts (kill switch, risk limit reached)

Options considered:
1. **SignalR** — ASP.NET Core native, supports WebSocket + fallbacks
2. **Raw WebSockets** — More control, but must build protocol and reconnection
3. **Server-Sent Events (SSE)** — Simpler, but unidirectional (server → client only)
4. **Polling** — Simplest, but latency and overhead unacceptable for 2-second timer

### Decision

Use **ASP.NET Core SignalR** with automatic transport negotiation (WebSocket → Server-Sent Events → Long Polling fallback).

### Consequences

**Positive:**
- Native integration with ASP.NET Core authentication and DI
- Automatic reconnection and transport fallback
- Official React client (`@microsoft/signalr`)
- Group-based messaging (per-user channels)
- Scales with Redis backplane when needed

**Negative:**
- WebSocket connections are long-lived (resource consideration)
- Nginx requires WebSocket upgrade configuration
- SignalR protocol adds overhead compared to raw WebSockets

**Mitigation:**
- Connection limit monitored via health checks
- Nginx WebSocket configuration documented in deployment guide
- Redis backplane planned for horizontal scaling phase

---

## ADR-012: Serilog for Structured Logging

**Status:** Accepted  
**Date:** 2026-11-11  
**Deciders:** Principal Architect  

### Context

A trading platform requires comprehensive logging for:
- Trade execution audit trail
- Error diagnosis in production
- Performance monitoring
- Security event tracking

Structured logging enables querying and filtering logs by properties (UserId, OrderId, Symbol) rather than parsing text strings.

### Decision

Use **Serilog** with structured JSON output:
- Console sink for Docker log capture
- File sink for persistent rolling logs
- Optional Seq sink for log aggregation and search UI
- Enrichers for UserId, RequestId, MachineName, Timestamp

### Consequences

**Positive:**
- Rich structured data in every log entry
- Queryable by any property (e.g., "all errors for OrderId X")
- Multiple sinks configurable without code changes
- Serilog is the de facto standard for .NET structured logging
- Minimal performance impact with async sinks

**Negative:**
- JSON log format less human-readable in raw console output
- Additional NuGet dependencies
- Seq (optional) requires separate deployment

**Mitigation:**
- Console output formatted for readability in development
- JSON format in production (machine-parsed)
- Seq is optional; file + console sufficient for initial deployment

---

## ADR-013: EF Core Code-First Migrations

**Status:** Accepted  
**Date:** 2026-11-11  
**Deciders:** Principal Architect  

### Context

Database schema must evolve over time as features are added. Schema changes must be:
- Version-controlled alongside code
- Reviewable in pull requests
- Reversible if issues are found
- Auditable for compliance

### Decision

Use **EF Core Code-First Migrations**:
- Migrations generated from entity model changes
- Migration files committed to Git
- Reviewed as part of pull request process
- Applied manually in production (never auto-applied)
- SQL script generation for production review

### Consequences

**Positive:**
- Schema changes tracked in version control
- Migration history provides an audit trail
- Idempotent script generation for safe production deployment
- Rollback migrations available for reversing changes
- Developer-friendly workflow integrated with `dotnet ef` CLI

**Negative:**
- Migration conflicts possible with concurrent development (single developer mitigates this)
- Complex migrations may require manual SQL alongside EF-generated code
- EF Core migrations less suitable for very large data transformations

**Mitigation:**
- Single developer eliminates migration merge conflicts
- Complex migrations documented with manual SQL when needed
- Pre-migration backups mandatory in production

---

## ADR-014: Idempotent Order Execution

**Status:** Accepted  
**Date:** 2026-11-11  
**Deciders:** Principal Architect  

### Context

The trading platform operates in an environment where:
- Workers may restart mid-execution
- Network calls to broker APIs may timeout without confirmation
- Database commits may succeed but the subsequent broker API call may fail
- Duplicate order placement can cause significant financial damage

An order placed twice at market price results in double the intended position and potential financial loss.

### Decision

Implement **idempotent order execution** at multiple levels:

1. **Idempotency Keys**: Every `PendingTradeApproval` and `Order` carries a unique idempotency key (UUID). The broker adapter passes this key to the broker API (brokers that support it) and checks locally before placing.

2. **State Machine Guards**: Orders follow a strict state machine. An order in `Submitted` state cannot be submitted again. State transitions are validated before any action.

3. **Reconciliation on Startup**: When the Worker starts (or restarts), it reconciles:
   - Pending approvals past their deadline
   - Orders in transient states (`Submitting`, `CancelRequested`)
   - Queries broker API for actual order status and updates local state

4. **Database Transaction Boundaries**: State transitions use optimistic concurrency (row version) to prevent duplicate processing by concurrent workers.

### Consequences

**Positive:**
- Safe against duplicate order placement
- Worker restart does not cause double execution
- Network failures handled gracefully with reconciliation
- Auditable state machine transitions

**Negative:**
- Additional complexity in order processing logic
- Reconciliation adds startup latency for Worker
- Optimistic concurrency requires retry logic for conflicts

**Mitigation:**
- State machine rigorously tested with unit tests
- Reconciliation logic runs only on startup and periodic intervals
- Retry with backoff for concurrency conflicts (max 3 attempts)

---

## Decision Log Summary

| ADR | Decision | Key Driver | Risk Level |
|-----|----------|------------|------------|
| 001 | .NET 10 LTS | Long-term support for financial app | Low |
| 002 | Modular Monolith | Single developer, operational simplicity | Low |
| 003 | PostgreSQL | JSONB, partitioning, cost, EF Core support | Low |
| 004 | React + TypeScript + Vite | Ecosystem, PWA, real-time support | Low |
| 005 | Adapter Pattern | Broker API isolation, extensibility | Low |
| 006 | Server-Side Timer | Reliability without browser dependency | Medium |
| 007 | Paper Trading Default | Financial safety | Low |
| 008 | Tenant Isolation | Future multi-user readiness | Low |
| 009 | AI Advisory Only | Predictability, regulatory compliance | Low |
| 010 | Docker Deployment | Cross-platform consistency | Low |
| 011 | SignalR | Native .NET real-time, transport fallback | Low |
| 012 | Serilog | Structured logging standard | Low |
| 013 | EF Core Migrations | Version-controlled schema evolution | Low |
| 014 | Idempotent Execution | Duplicate order prevention | High |

---

## Pending Decisions

The following decisions will be made during implementation:

| Topic | Milestone | Options Under Consideration |
|-------|-----------|----------------------------|
| State management library (React) | M6 | Context + hooks vs. Zustand vs. Redux Toolkit |
| Charting library | M6 | Recharts vs. Lightweight Charts vs. TradingView widget |
| Background job framework | M10 | Custom Worker vs. Hangfire vs. Quartz.NET |
| Email templating engine | M14 | Razor templates vs. Scriban vs. Fluid |
| Caching strategy | M7 | In-memory → Redis migration path |

---

*New ADRs will be appended as architectural decisions are made during implementation.*
