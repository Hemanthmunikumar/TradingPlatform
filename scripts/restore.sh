#!/bin/bash
set -e

if [ -z "$1" ]; then
    echo "Usage: ./scripts/restore.sh <path_to_backup.sql.gz>"
    exit 1
fi

BACKUP_FILE="$1"

if [ ! -f "$BACKUP_FILE" ]; then
    echo "Error: Backup file '$BACKUP_FILE' not found."
    exit 1
fi

echo "WARNING: This will overwrite the current 'trading_platform' database!"
read -p "Are you sure you want to proceed? (y/N): " CONFIRM
if [[ "$CONFIRM" != [yY] ]]; then
    echo "Restore cancelled."
    exit 0
fi

echo "Restoring database from $BACKUP_FILE..."
gunzip -c "$BACKUP_FILE" | docker exec -i algo-trading-postgres psql -U postgres -d trading_platform

echo "Database restore completed successfully."
