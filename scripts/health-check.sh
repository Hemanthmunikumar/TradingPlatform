#!/bin/bash
set -e

API_URL="${API_URL:-http://localhost:5000/health}"

echo "Checking Trading Platform API health at $API_URL..."
STATUS_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$API_URL" || echo "000")

if [ "$STATUS_CODE" -eq 200 ]; then
    echo "HEALTHY: API returned 200 OK."
    exit 0
else
    echo "UNHEALTHY: API returned status $STATUS_CODE"
    exit 1
fi
