CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL,
    CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id)
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE TABLE broker_providers (
        id uuid NOT NULL,
        name character varying(64) NOT NULL,
        code integer NOT NULL,
        api_base_url character varying(256) NOT NULL,
        web_socket_url character varying(256),
        authorization_url character varying(256),
        token_url character varying(256),
        is_active boolean NOT NULL,
        capabilities_json jsonb,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_broker_providers PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE TABLE instruments (
        id uuid NOT NULL,
        exchange character varying(16) NOT NULL DEFAULT 'NSE',
        trading_symbol character varying(32) NOT NULL,
        instrument_token character varying(64) NOT NULL,
        isin character varying(32),
        company_name character varying(128) NOT NULL,
        instrument_type character varying(16) NOT NULL DEFAULT 'EQUITY',
        lot_size integer NOT NULL,
        tick_size numeric(18,4) NOT NULL,
        segment character varying(32) NOT NULL DEFAULT 'NSE_EQ',
        is_active boolean NOT NULL,
        last_updated_utc timestamp with time zone NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_instruments PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE TABLE roles (
        id uuid NOT NULL,
        name character varying(64) NOT NULL,
        description text,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_roles PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE TABLE system_configurations (
        id uuid NOT NULL,
        key character varying(64) NOT NULL,
        value character varying(1024) NOT NULL,
        description character varying(256),
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_system_configurations PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE TABLE users (
        id uuid NOT NULL,
        email character varying(256) NOT NULL,
        password_hash text NOT NULL,
        full_name character varying(128) NOT NULL,
        phone_number character varying(32),
        timezone character varying(64) NOT NULL DEFAULT 'Asia/Kolkata',
        is_active boolean NOT NULL,
        email_verified boolean NOT NULL,
        is_deleted boolean NOT NULL,
        deleted_at_utc timestamp with time zone,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_users PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE TABLE worker_heartbeats (
        id uuid NOT NULL,
        worker_name character varying(64) NOT NULL,
        last_heartbeat_utc timestamp with time zone NOT NULL,
        status character varying(32) NOT NULL,
        metadata_json jsonb,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_worker_heartbeats PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE TABLE market_candles (
        id uuid NOT NULL,
        instrument_id uuid NOT NULL,
        timeframe character varying(8) NOT NULL,
        timestamp_utc timestamp with time zone NOT NULL,
        open numeric(18,4) NOT NULL,
        high numeric(18,4) NOT NULL,
        low numeric(18,4) NOT NULL,
        close numeric(18,4) NOT NULL,
        volume bigint NOT NULL,
        vwap numeric(18,4),
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_market_candles PRIMARY KEY (id),
        CONSTRAINT fk_market_candles_instruments_instrument_id FOREIGN KEY (instrument_id) REFERENCES instruments (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE TABLE audit_logs (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        action character varying(64) NOT NULL,
        entity_type character varying(64) NOT NULL,
        entity_id character varying(64) NOT NULL,
        old_values_json jsonb,
        new_values_json jsonb,
        ip_address character varying(64),
        user_agent character varying(256),
        timestamp_utc timestamp with time zone NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_audit_logs PRIMARY KEY (id),
        CONSTRAINT fk_audit_logs_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE TABLE broker_connections (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        broker_provider_id uuid NOT NULL,
        display_name character varying(64) NOT NULL,
        encrypted_api_key character varying(512) NOT NULL,
        encrypted_api_secret character varying(512) NOT NULL,
        encrypted_access_token character varying(1024),
        encrypted_refresh_token character varying(1024),
        token_expires_at_utc timestamp with time zone,
        is_authenticated boolean NOT NULL,
        is_active boolean NOT NULL,
        trading_enabled boolean NOT NULL,
        trading_mode integer NOT NULL,
        last_authenticated_at_utc timestamp with time zone,
        is_deleted boolean NOT NULL,
        deleted_at_utc timestamp with time zone,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_broker_connections PRIMARY KEY (id),
        CONSTRAINT fk_broker_connections_broker_providers_broker_provider_id FOREIGN KEY (broker_provider_id) REFERENCES broker_providers (id) ON DELETE RESTRICT,
        CONSTRAINT fk_broker_connections_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE TABLE instrument_universes (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        name character varying(64) NOT NULL,
        description character varying(256),
        is_active boolean NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_instrument_universes PRIMARY KEY (id),
        CONSTRAINT fk_instrument_universes_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE TABLE refresh_tokens (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        token character varying(256) NOT NULL,
        expires_at_utc timestamp with time zone NOT NULL,
        revoked_at_utc timestamp with time zone,
        replaced_by_token text,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_refresh_tokens PRIMARY KEY (id),
        CONSTRAINT fk_refresh_tokens_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE TABLE strategy_definitions (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        name character varying(64) NOT NULL,
        description character varying(512),
        strategy_type integer NOT NULL,
        is_active boolean NOT NULL,
        is_deleted boolean NOT NULL,
        deleted_at_utc timestamp with time zone,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_strategy_definitions PRIMARY KEY (id),
        CONSTRAINT fk_strategy_definitions_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE TABLE user_roles (
        user_id uuid NOT NULL,
        role_id uuid NOT NULL,
        CONSTRAINT pk_user_roles PRIMARY KEY (user_id, role_id),
        CONSTRAINT fk_user_roles_roles_role_id FOREIGN KEY (role_id) REFERENCES roles (id) ON DELETE CASCADE,
        CONSTRAINT fk_user_roles_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE TABLE trading_accounts (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        broker_connection_id uuid NOT NULL,
        account_id character varying(64) NOT NULL,
        account_name character varying(64) NOT NULL,
        account_type integer NOT NULL,
        is_active boolean NOT NULL,
        trading_enabled boolean NOT NULL,
        trading_mode integer NOT NULL,
        available_funds numeric(18,4) NOT NULL,
        allocated_capital numeric(18,4) NOT NULL,
        is_deleted boolean NOT NULL,
        deleted_at_utc timestamp with time zone,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_trading_accounts PRIMARY KEY (id),
        CONSTRAINT fk_trading_accounts_broker_connections_broker_connection_id FOREIGN KEY (broker_connection_id) REFERENCES broker_connections (id) ON DELETE CASCADE,
        CONSTRAINT fk_trading_accounts_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE TABLE instrument_universe_items (
        id uuid NOT NULL,
        instrument_universe_id uuid NOT NULL,
        instrument_id uuid NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_instrument_universe_items PRIMARY KEY (id),
        CONSTRAINT fk_instrument_universe_items_instrument_universes_instrument_u FOREIGN KEY (instrument_universe_id) REFERENCES instrument_universes (id) ON DELETE CASCADE,
        CONSTRAINT fk_instrument_universe_items_instruments_instrument_id FOREIGN KEY (instrument_id) REFERENCES instruments (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE TABLE strategy_versions (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        strategy_definition_id uuid NOT NULL,
        version_number integer NOT NULL,
        configuration_json jsonb NOT NULL,
        stock_universe_json jsonb,
        capital_allocation numeric(18,4) NOT NULL,
        max_positions integer NOT NULL,
        risk_per_trade_percent numeric(5,2) NOT NULL,
        stop_loss_percent numeric(5,2) NOT NULL,
        target_percent numeric(5,2) NOT NULL,
        is_active boolean NOT NULL,
        activated_at_utc timestamp with time zone,
        deactivated_at_utc timestamp with time zone,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_strategy_versions PRIMARY KEY (id),
        CONSTRAINT fk_strategy_versions_strategy_definitions_strategy_definition_ FOREIGN KEY (strategy_definition_id) REFERENCES strategy_definitions (id) ON DELETE CASCADE,
        CONSTRAINT fk_strategy_versions_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE TABLE account_configurations (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        trading_account_id uuid NOT NULL,
        max_capital_allocation numeric(18,4) NOT NULL,
        max_order_value numeric(18,4) NOT NULL,
        max_positions integer NOT NULL,
        max_risk_per_trade_percent numeric(5,2) NOT NULL,
        daily_loss_limit numeric(18,4) NOT NULL,
        max_exposure_per_stock_percent numeric(5,2) NOT NULL,
        max_trades_per_day integer NOT NULL,
        stop_loss_required boolean NOT NULL,
        approval_duration_seconds integer NOT NULL,
        auto_execute_on_timeout boolean NOT NULL,
        kill_switch_active boolean NOT NULL,
        trading_schedule_start time without time zone NOT NULL,
        trading_schedule_end time without time zone NOT NULL,
        intraday_square_off_time time without time zone NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_account_configurations PRIMARY KEY (id),
        CONSTRAINT fk_account_configurations_trading_accounts_trading_account_id FOREIGN KEY (trading_account_id) REFERENCES trading_accounts (id) ON DELETE CASCADE,
        CONSTRAINT fk_account_configurations_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE TABLE daily_reports (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        trading_account_id uuid,
        report_date date NOT NULL,
        report_type character varying(32) NOT NULL,
        content_json jsonb NOT NULL,
        generated_at_utc timestamp with time zone NOT NULL,
        delivered_via_json jsonb,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_daily_reports PRIMARY KEY (id),
        CONSTRAINT fk_daily_reports_trading_accounts_trading_account_id FOREIGN KEY (trading_account_id) REFERENCES trading_accounts (id),
        CONSTRAINT fk_daily_reports_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE TABLE notifications (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        trading_account_id uuid,
        type character varying(32) NOT NULL,
        title character varying(128) NOT NULL,
        message character varying(1024) NOT NULL,
        data_json jsonb,
        is_read boolean NOT NULL,
        read_at_utc timestamp with time zone,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_notifications PRIMARY KEY (id),
        CONSTRAINT fk_notifications_trading_accounts_trading_account_id FOREIGN KEY (trading_account_id) REFERENCES trading_accounts (id),
        CONSTRAINT fk_notifications_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE TABLE risk_profiles (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        trading_account_id uuid NOT NULL,
        profile_name character varying(64) NOT NULL,
        max_capital numeric(18,4) NOT NULL,
        max_daily_loss numeric(18,4) NOT NULL,
        max_position_size numeric(18,4) NOT NULL,
        is_active boolean NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_risk_profiles PRIMARY KEY (id),
        CONSTRAINT fk_risk_profiles_trading_accounts_trading_account_id FOREIGN KEY (trading_account_id) REFERENCES trading_accounts (id) ON DELETE CASCADE,
        CONSTRAINT fk_risk_profiles_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE TABLE positions (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        trading_account_id uuid NOT NULL,
        instrument_id uuid NOT NULL,
        strategy_version_id uuid,
        side integer NOT NULL,
        product_type integer NOT NULL,
        quantity integer NOT NULL,
        average_buy_price numeric(18,4) NOT NULL,
        current_price numeric(18,4) NOT NULL,
        stop_loss numeric(18,4),
        target numeric(18,4),
        realized_gross_pn_l numeric(18,4) NOT NULL,
        realized_net_pn_l numeric(18,4) NOT NULL,
        total_charges numeric(18,4) NOT NULL,
        is_open boolean NOT NULL,
        opened_at_utc timestamp with time zone NOT NULL,
        closed_at_utc timestamp with time zone,
        exit_reason text,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_positions PRIMARY KEY (id),
        CONSTRAINT fk_positions_instruments_instrument_id FOREIGN KEY (instrument_id) REFERENCES instruments (id) ON DELETE RESTRICT,
        CONSTRAINT fk_positions_strategy_versions_strategy_version_id FOREIGN KEY (strategy_version_id) REFERENCES strategy_versions (id),
        CONSTRAINT fk_positions_trading_accounts_trading_account_id FOREIGN KEY (trading_account_id) REFERENCES trading_accounts (id) ON DELETE RESTRICT,
        CONSTRAINT fk_positions_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE TABLE strategy_account_mappings (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        strategy_version_id uuid NOT NULL,
        trading_account_id uuid NOT NULL,
        is_active boolean NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_strategy_account_mappings PRIMARY KEY (id),
        CONSTRAINT fk_strategy_account_mappings_strategy_versions_strategy_versio FOREIGN KEY (strategy_version_id) REFERENCES strategy_versions (id) ON DELETE CASCADE,
        CONSTRAINT fk_strategy_account_mappings_trading_accounts_trading_account_ FOREIGN KEY (trading_account_id) REFERENCES trading_accounts (id) ON DELETE CASCADE,
        CONSTRAINT fk_strategy_account_mappings_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE TABLE trading_signals (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        trading_account_id uuid NOT NULL,
        strategy_version_id uuid NOT NULL,
        instrument_id uuid NOT NULL,
        signal_type integer NOT NULL,
        price numeric(18,4) NOT NULL,
        quantity integer NOT NULL,
        stop_loss numeric(18,4),
        target numeric(18,4),
        reason text NOT NULL,
        confidence numeric(5,2) NOT NULL,
        indicator_values_json jsonb,
        status integer NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_trading_signals PRIMARY KEY (id),
        CONSTRAINT fk_trading_signals_instruments_instrument_id FOREIGN KEY (instrument_id) REFERENCES instruments (id) ON DELETE RESTRICT,
        CONSTRAINT fk_trading_signals_strategy_versions_strategy_version_id FOREIGN KEY (strategy_version_id) REFERENCES strategy_versions (id) ON DELETE CASCADE,
        CONSTRAINT fk_trading_signals_trading_accounts_trading_account_id FOREIGN KEY (trading_account_id) REFERENCES trading_accounts (id) ON DELETE CASCADE,
        CONSTRAINT fk_trading_signals_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE TABLE notification_deliveries (
        id uuid NOT NULL,
        notification_id uuid NOT NULL,
        channel integer NOT NULL,
        status integer NOT NULL,
        sent_at_utc timestamp with time zone,
        delivered_at_utc timestamp with time zone,
        error_message text,
        retry_count integer NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_notification_deliveries PRIMARY KEY (id),
        CONSTRAINT fk_notification_deliveries_notifications_notification_id FOREIGN KEY (notification_id) REFERENCES notifications (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE TABLE pending_trade_approvals (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        trading_account_id uuid NOT NULL,
        trading_signal_id uuid,
        symbol character varying(32) NOT NULL,
        company_name character varying(128) NOT NULL,
        side integer NOT NULL,
        quantity integer NOT NULL,
        indicative_price numeric(18,4) NOT NULL,
        stop_loss numeric(18,4),
        target numeric(18,4),
        risk_amount numeric(18,4),
        reason character varying(512) NOT NULL,
        approval_deadline_utc timestamp with time zone NOT NULL,
        status integer NOT NULL,
        decided_at_utc timestamp with time zone,
        decided_by_user_id uuid,
        decision_reason text,
        idempotency_key uuid NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_pending_trade_approvals PRIMARY KEY (id),
        CONSTRAINT fk_pending_trade_approvals_trading_accounts_trading_account_id FOREIGN KEY (trading_account_id) REFERENCES trading_accounts (id) ON DELETE RESTRICT,
        CONSTRAINT fk_pending_trade_approvals_trading_signals_trading_signal_id FOREIGN KEY (trading_signal_id) REFERENCES trading_signals (id) ON DELETE SET NULL,
        CONSTRAINT fk_pending_trade_approvals_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE TABLE trading_orders (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        trading_account_id uuid NOT NULL,
        pending_trade_approval_id uuid,
        instrument_id uuid NOT NULL,
        strategy_version_id uuid,
        broker_order_id character varying(64),
        side integer NOT NULL,
        order_type integer NOT NULL,
        product_type integer NOT NULL,
        quantity integer NOT NULL,
        filled_quantity integer NOT NULL,
        price numeric(18,4) NOT NULL,
        trigger_price numeric(18,4),
        average_price numeric(18,4) NOT NULL,
        status integer NOT NULL,
        status_message character varying(512),
        idempotency_key uuid NOT NULL,
        submitted_at_utc timestamp with time zone,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_trading_orders PRIMARY KEY (id),
        CONSTRAINT fk_trading_orders_instruments_instrument_id FOREIGN KEY (instrument_id) REFERENCES instruments (id) ON DELETE RESTRICT,
        CONSTRAINT fk_trading_orders_pending_trade_approvals_pending_trade_approv FOREIGN KEY (pending_trade_approval_id) REFERENCES pending_trade_approvals (id) ON DELETE SET NULL,
        CONSTRAINT fk_trading_orders_strategy_versions_strategy_version_id FOREIGN KEY (strategy_version_id) REFERENCES strategy_versions (id),
        CONSTRAINT fk_trading_orders_trading_accounts_trading_account_id FOREIGN KEY (trading_account_id) REFERENCES trading_accounts (id) ON DELETE RESTRICT,
        CONSTRAINT fk_trading_orders_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE TABLE order_events (
        id uuid NOT NULL,
        trading_order_id uuid NOT NULL,
        previous_status integer NOT NULL,
        new_status integer NOT NULL,
        event_type character varying(64) NOT NULL,
        message text,
        broker_response_json jsonb,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_order_events PRIMARY KEY (id),
        CONSTRAINT fk_order_events_trading_orders_trading_order_id FOREIGN KEY (trading_order_id) REFERENCES trading_orders (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE TABLE trade_executions (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        trading_order_id uuid NOT NULL,
        trading_account_id uuid NOT NULL,
        instrument_id uuid NOT NULL,
        broker_trade_id character varying(64),
        side integer NOT NULL,
        quantity integer NOT NULL,
        price numeric(18,4) NOT NULL,
        exchange character varying(16) NOT NULL DEFAULT 'NSE',
        executed_at_utc timestamp with time zone NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_trade_executions PRIMARY KEY (id),
        CONSTRAINT fk_trade_executions_instruments_instrument_id FOREIGN KEY (instrument_id) REFERENCES instruments (id) ON DELETE RESTRICT,
        CONSTRAINT fk_trade_executions_trading_accounts_trading_account_id FOREIGN KEY (trading_account_id) REFERENCES trading_accounts (id) ON DELETE CASCADE,
        CONSTRAINT fk_trade_executions_trading_orders_trading_order_id FOREIGN KEY (trading_order_id) REFERENCES trading_orders (id) ON DELETE CASCADE,
        CONSTRAINT fk_trade_executions_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    INSERT INTO broker_providers (id, api_base_url, authorization_url, capabilities_json, code, created_at_utc, is_active, name, token_url, updated_at_utc, web_socket_url)
    VALUES ('10000000-0000-0000-0000-000000000001', 'http://localhost:5000/mock-broker', NULL, NULL, 1, TIMESTAMPTZ '2026-11-11T08:24:16.019438Z', TRUE, 'Mock Simulator', NULL, NULL, NULL);
    INSERT INTO broker_providers (id, api_base_url, authorization_url, capabilities_json, code, created_at_utc, is_active, name, token_url, updated_at_utc, web_socket_url)
    VALUES ('10000000-0000-0000-0000-000000000002', 'https://api.upstox.com/v2', 'https://api.upstox.com/v2/login/authorization/dialog', NULL, 2, TIMESTAMPTZ '2026-11-11T08:24:16.020197Z', TRUE, 'Upstox', 'https://api.upstox.com/v2/login/authorization/token', NULL, 'wss://ws-feed.upstox.com/v2/feed');
    INSERT INTO broker_providers (id, api_base_url, authorization_url, capabilities_json, code, created_at_utc, is_active, name, token_url, updated_at_utc, web_socket_url)
    VALUES ('10000000-0000-0000-0000-000000000003', 'https://api.kite.trade', 'https://kite.zerodha.com/connect/login', NULL, 3, TIMESTAMPTZ '2026-11-11T08:24:16.020561Z', TRUE, 'Zerodha', 'https://api.kite.trade/session/token', NULL, 'wss://ws.kite.trade');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    INSERT INTO roles (id, created_at_utc, description, name, updated_at_utc)
    VALUES ('00000000-0000-0000-0000-000000000001', TIMESTAMPTZ '2026-11-11T08:24:16.097212Z', 'Platform Administrator', 'Admin', NULL);
    INSERT INTO roles (id, created_at_utc, description, name, updated_at_utc)
    VALUES ('00000000-0000-0000-0000-000000000002', TIMESTAMPTZ '2026-11-11T08:24:16.097531Z', 'Standard Trading User', 'User', NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_account_configurations_trading_account_id ON account_configurations (trading_account_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_account_configurations_user_id ON account_configurations (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_audit_logs_entity_type_entity_id ON audit_logs (entity_type, entity_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_audit_logs_user_id_timestamp_utc ON audit_logs (user_id, timestamp_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_broker_connections_broker_provider_id ON broker_connections (broker_provider_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_broker_connections_user_id_broker_provider_id ON broker_connections (user_id, broker_provider_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_broker_providers_code ON broker_providers (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_daily_reports_trading_account_id ON daily_reports (trading_account_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_daily_reports_user_id_report_date ON daily_reports (user_id, report_date);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_instrument_universe_items_instrument_id ON instrument_universe_items (instrument_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_instrument_universe_items_instrument_universe_id_instrument ON instrument_universe_items (instrument_universe_id, instrument_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_instrument_universes_user_id_name ON instrument_universes (user_id, name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_instruments_exchange_trading_symbol ON instruments (exchange, trading_symbol);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_instruments_instrument_token ON instruments (instrument_token);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_market_candles_instrument_id_timeframe_timestamp_utc ON market_candles (instrument_id, timeframe, timestamp_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_notification_deliveries_notification_id_channel ON notification_deliveries (notification_id, channel);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_notifications_trading_account_id ON notifications (trading_account_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_notifications_user_id_is_read ON notifications (user_id, is_read);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_order_events_trading_order_id ON order_events (trading_order_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_pending_trade_approvals_idempotency_key ON pending_trade_approvals (idempotency_key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_pending_trade_approvals_status_approval_deadline_utc ON pending_trade_approvals (status, approval_deadline_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_pending_trade_approvals_trading_account_id ON pending_trade_approvals (trading_account_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_pending_trade_approvals_trading_signal_id ON pending_trade_approvals (trading_signal_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_pending_trade_approvals_user_id ON pending_trade_approvals (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_positions_instrument_id ON positions (instrument_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_positions_strategy_version_id ON positions (strategy_version_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_positions_trading_account_id_instrument_id_is_open ON positions (trading_account_id, instrument_id, is_open);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_positions_trading_account_id_is_open ON positions (trading_account_id, is_open);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_positions_user_id ON positions (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_refresh_tokens_token ON refresh_tokens (token);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_refresh_tokens_user_id ON refresh_tokens (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_risk_profiles_trading_account_id ON risk_profiles (trading_account_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_risk_profiles_user_id ON risk_profiles (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_roles_name ON roles (name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_strategy_account_mappings_strategy_version_id_trading_accou ON strategy_account_mappings (strategy_version_id, trading_account_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_strategy_account_mappings_trading_account_id ON strategy_account_mappings (trading_account_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_strategy_account_mappings_user_id ON strategy_account_mappings (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_strategy_definitions_user_id_name ON strategy_definitions (user_id, name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_strategy_versions_strategy_definition_id_version_number ON strategy_versions (strategy_definition_id, version_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_strategy_versions_user_id ON strategy_versions (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_system_configurations_key ON system_configurations (key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_trade_executions_instrument_id ON trade_executions (instrument_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_trade_executions_trading_account_id ON trade_executions (trading_account_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_trade_executions_trading_order_id ON trade_executions (trading_order_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_trade_executions_user_id ON trade_executions (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_trading_accounts_broker_connection_id_account_id ON trading_accounts (broker_connection_id, account_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_trading_accounts_user_id ON trading_accounts (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_trading_orders_broker_order_id ON trading_orders (broker_order_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_trading_orders_idempotency_key ON trading_orders (idempotency_key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_trading_orders_instrument_id ON trading_orders (instrument_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_trading_orders_pending_trade_approval_id ON trading_orders (pending_trade_approval_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_trading_orders_strategy_version_id ON trading_orders (strategy_version_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_trading_orders_trading_account_id_status ON trading_orders (trading_account_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_trading_orders_user_id ON trading_orders (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_trading_signals_instrument_id ON trading_signals (instrument_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_trading_signals_strategy_version_id ON trading_signals (strategy_version_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_trading_signals_trading_account_id_status ON trading_signals (trading_account_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_trading_signals_user_id ON trading_signals (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_user_roles_role_id ON user_roles (role_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_users_email ON users (email);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    CREATE INDEX ix_worker_heartbeats_worker_name ON worker_heartbeats (worker_name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261111082422_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261111082422_InitialCreate', '10.0.15');
    END IF;
END $EF$;
COMMIT;

