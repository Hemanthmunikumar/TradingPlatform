#!/bin/bash
set -e

BACKUP_DIR="${BACKUP_DIR:-./backups}"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
BACKUP_FILE="${BACKUP_DIR}/trading_db_backup_${TIMESTAMP}.sql.gz"

mkdir -p "$BACKUP_DIR"

echo "Creating PostgreSQL backup to ${BACKUP_FILE}..."
docker exec -t algo-trading-postgres pg_dump -U postgres trading_platform | gzip > "$BACKUP_FILE"

echo "Backup complete: ${BACKUP_FILE} ($(du -h "$BACKUP_FILE" | cut -f1))"

# Retention: keep backups for 14 days
find "$BACKUP_DIR" -name "trading_db_backup_*.sql.gz" -mtime +14 -exec rm {} \;
echo "Cleaned backups older than 14 days."
