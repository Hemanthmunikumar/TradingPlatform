#!/bin/bash
set -e

echo "=================================================="
echo "Deploying Indian Automated Stock Trading Monolith"
echo "=================================================="

# Pull latest code or branches if running on server
# git pull origin master

echo "1. Building and starting production containers..."
docker compose up -d --build

echo "2. Waiting for database and API to be ready..."
sleep 10

echo "3. Running health check..."
./scripts/health-check.sh

echo "Deployment finished successfully!"
