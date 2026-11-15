-- Automatically ensures trading_user exists with default development password
DO $$
BEGIN
   IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'trading_user') THEN
      CREATE ROLE trading_user WITH LOGIN PASSWORD 'trading_dev_pass' SUPERUSER;
   ELSE
      ALTER ROLE trading_user WITH PASSWORD 'trading_dev_pass' SUPERUSER;
   END IF;
END $$;

SELECT 'CREATE DATABASE trading_platform_dev OWNER trading_user'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'trading_platform_dev')\gexec
