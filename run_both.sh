#!/bin/bash
#
# run_both.sh — Start Analytics API + Crawlers together
#
# Usage:
#   ./run_both.sh                       # API + full analysis crawl
#   ./run_both.sh --blueprints-only     # API + blueprint-only crawl
#   ./run_both.sh --show-browser       # Show browser windows
#

set -e

PROJECT_ROOT="/home/rf/Desktop/Printify_prodcuct_generator"
API_URL="http://localhost:5000"
API_PID=""
CRAWLER_SCRIPT="src/PrintifyGenerator.Crawlers/run_full_analysis.sh"

# Parse arguments
SHOW_BROWSER=""
while [[ $# -gt 0 ]]; do
    case $1 in
        --blueprints-only)
            CRAWLER_SCRIPT="src/PrintifyGenerator.Crawlers/run_blueprints.sh"
            shift
            ;;
        --show-browser)
            SHOW_BROWSER="--show-browser"
            shift
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

echo "============================================"
echo "  STARTING API + CRAWLERS"
echo "============================================"
echo ""

# ── Start Analytics API ────────────────────────────────────────
echo "[1/3] Starting Analytics API..."
cd "$PROJECT_ROOT"

nohup dotnet run --project src/PrintifyGenerator.AnalyticsApi \
    --urls="$API_URL" \
    > /tmp/analytics_api.log 2>&1 &

API_PID=$!
echo "      API PID: $API_PID"
echo "      Log: /tmp/analytics_api.log"

# Wait for API to be ready
echo -n "      Waiting for API to start..."
for i in $(seq 1 20); do
    if curl -s "$API_URL/api/analytics/datasets" > /dev/null 2>&1; then
        echo " READY"
        break
    fi
    if [ $i -eq 20 ]; then
        echo " FAILED"
        echo "Check /tmp/analytics_api.log"
        kill $API_PID 2>/dev/null || true
        exit 1
    fi
    sleep 1
done

# ── Start Crawlers ──────────────────────────────────────────────
echo ""
echo "[2/3] Starting crawler script: $(basename $CRAWLER_SCRIPT)"
echo "      (Ctrl+C to stop both)"
echo ""

# Trap Ctrl+C → kill API too
trap 'echo ""; echo "Stopping API..."; kill $API_PID 2>/dev/null || true; exit' INT

if [ -n "$SHOW_BROWSER" ]; then
    bash "$PROJECT_ROOT/$CRAWLER_SCRIPT" --show-browser
else
    bash "$PROJECT_ROOT/$CRAWLER_SCRIPT"
fi

# ── Done ───────────────────────────────────────────────────────
echo ""
echo "[3/3] Crawl complete."
echo ""
echo "API is still running (PID: $API_PID)"
echo "Stop it with: kill $API_PID"
echo ""
echo "Try these queries:"
echo "  curl $API_URL/api/analytics/products"
echo "  curl \"$API_URL/api/analytics/analysis/success-factors\""
echo "  curl \"$API_URL/api/analytics/analysis/price-comparison/Unisex%20Cotton%20Crew%20Tee\""
