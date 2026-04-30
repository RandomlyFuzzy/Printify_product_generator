#!/bin/bash
#
# run_parallel.sh — Run ALL crawlers in parallel
#
# Usage:
#   ./run_parallel.sh                                    # All platforms, all blueprints
#   ./run_parallel.sh --blueprints-only                       # Skip category terms
#   ./run_parallel.sh --max-products=10 --jobs=3            # Limit parallel jobs
#   ./run_parallel.sh "cotton tee" "hoodie"                  # Custom search terms
#

set -e

PROJECT_ROOT="/home/rf/Desktop/Printify_prodcuct_generator"
BLUEPRINT_FILE="$PROJECT_ROOT/src/data/staged/blueprints/blueprints.json"
MAX_PRODUCTS=20
JOBS=5  # Number of parallel crawler processes
BLUEPRINTS_ONLY=false
SHOW_BROWSER=""
CUSTOM_QUERIES=""

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --blueprints-only)
            BLUEPRINTS_ONLY=true
            shift
            ;;
        --max-products=*)
            MAX_PRODUCTS="${1#*=}"
            shift
            ;;
        --jobs=*)
            JOBS="${1#*=}"
            shift
            ;;
        --show-browser)
            SHOW_BROWSER="--show-browser"
            shift
            ;;
        -*)
            echo "Unknown option: $1"
            exit 1
            ;;
        *)
            CUSTOM_QUERIES="$CUSTOM_QUERIES $1"
            shift
            ;;
    esac
done

cd "$PROJECT_ROOT"

echo "============================================"
echo "  PARALLEL CRAWLER"
echo "============================================"
echo "Max products per query: $MAX_PRODUCTS"
echo "Parallel jobs: $JOBS"
echo "Platforms: amazon, etsy, ebay, walmart, aliexpress"
echo ""

# Function to run a single crawler
run_crawler() {
    local platform=$1
    local query=$2
    local output_dir=$3
    
    echo "[$platform] Starting: $query"
    
    dotnet run --project "$PROJECT_ROOT/src/PrintifyGenerator.Crawlers" \
        --no-build \
        $SHOW_BROWSER \
        --max-products="$MAX_PRODUCTS" \
        --platforms="$platform" \
        "$query" \
        > "/tmp/crawl_${platform}_${query// /_}.log" 2>&1
    
    local exit_code=$?
    if [ $exit_code -eq 0 ]; then
        echo "[$platform] COMPLETE: $query"
    else
        echo "[$platform] FAILED (exit $exit_code): $query (see /tmp/crawl_${platform}_${query// /_}.log)"
    fi
    return $exit_code
}

export -f run_crawler
export PROJECT_ROOT MAX_PRODUCTS SHOW_BROWSER

# Get search queries
if [ -n "$CUSTOM_QUERIES" ]; then
    QUERIES="$CUSTOM_QUERIES"
    echo "Using custom queries: $QUERIES"
else
    echo "Loading blueprint names from $BLUEPRINT_FILE..."
    if [ ! -f "$BLUEPRINT_FILE" ]; then
        BLUEPRINT_FILE="$PROJECT_ROOT/src/data/Cached/blueprints.json"
    fi
    QUERIES=$(jq -r '.[].title' "$BLUEPRINT_FILE" 2>/dev/null | head -30)
    QUERY_COUNT=$(echo "$QUERIES" | wc -l)
    echo "Found $QUERY_COUNT blueprint names"
fi

if [ -z "$QUERIES" ]; then
    echo "Error: No search queries found!"
    exit 1
fi

echo ""
echo "============================================"
echo "  PHASE 1: BLUEPRINTS (YOUR PRODUCTS)"
echo "============================================"

# Run all platforms in parallel for each blueprint
count=0
for query in $QUERIES; do
    count=$((count + 1))
    echo ""
    echo "[$count] Query: $query"
    echo "--------------------------------------------"
    
    # Launch all 5 platforms in parallel
    for platform in amazon etsy ebay walmart aliexpress; do
        run_crawler "$platform" "$query" "" &
        
        # Limit parallel jobs
        if [ $(jobs -r | wc -l) -ge $JOBS ]; then
            wait -n  # Wait for any job to finish
        fi
    done
    
    wait  # Wait for all platforms to finish this query
    sleep 2  # Brief pause between queries
done

# Phase 2: Category terms (unless --blueprints-only)
if [ "$BLUEPRINTS_ONLY" = false ] && [ -z "$CUSTOM_QUERIES" ]; then
    echo ""
    echo "============================================"
    echo "  PHASE 2: CATEGORY TERMS (COMPETITORS)"
    echo "============================================"
    
    # Extract broader category terms
    CATEGORY_TERMS=$(echo "$QUERIES" | \
        tr ' ' '\n' | \
        sed 's/[^a-zA-Z0-9]//g' | \
        grep -E '....' | \
        sort | uniq | \
        head -10)
    
    echo "Category terms: $CATEGORY_TERMS"
    echo ""
    
    cat_count=0
    for term in $CATEGORY_TERMS; do
        cat_count=$((cat_count + 1))
        echo ""
        echo "[Cat $cat_count] Term: $term"
        echo "--------------------------------------------"
        
        for platform in amazon etsy ebay walmart aliexpress; do
            run_crawler "$platform" "$term" "" &
            
            if [ $(jobs -r | wc -l) -ge $JOBS ]; then
                wait -n
            fi
        done
        
        wait
        sleep 2
    done
fi

echo ""
echo "============================================"
echo "  ALL CRAWLS COMPLETE"
echo "============================================"
echo ""
echo "Output directory: $PROJECT_ROOT/src/PrintifyGenerator.Crawlers/output/"
echo "Check logs in /tmp/crawl_*.log"
echo ""
echo "Next: Ingest into API:"
echo "  dotnet run --project src/PrintifyGenerator.AnalyticsApi"
echo "  curl -X POST http://localhost:5000/api/data/products/{id} -d @output/file.json"
