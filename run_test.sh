#!/bin/bash
#
# Sphere51a Test Runner Script (Linux/macOS)
# Runs Sphere51a weapon timing tests with proper error handling and logging.
#

set -e  # Exit on any error

# Script configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR/Projects/Application"
LOG_DIR="$SCRIPT_DIR/Distribution/AuditReports/Logs"

# Default parameters
SCENARIO="weapon_timing"
DURATION=30
QUICK=false
VERBOSE=false

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --scenario)
            SCENARIO="$2"
            shift 2
            ;;
        --duration)
            DURATION="$2"
            shift 2
            ;;
        --quick)
            QUICK=true
            shift
            ;;
        --verbose)
            VERBOSE=true
            shift
            ;;
        *)
            echo "Unknown option: $1"
            echo "Usage: $0 [--scenario SCENARIO] [--duration SECONDS] [--quick] [--verbose]"
            exit 1
            ;;
    esac
done

# Ensure log directory exists
mkdir -p "$LOG_DIR"

# Build arguments
ARGS=()
if [ "$QUICK" = true ]; then
    ARGS+=("--quick-test")
else
    ARGS+=("--test-mode")
fi

ARGS+=("--scenario" "$SCENARIO" "--duration" "$DURATION")

if [ "$VERBOSE" = true ]; then
    ARGS+=("--verbose")
fi

# Display execution info
echo "=========================================="
echo "  Sphere51a Test Runner (Linux/macOS)"
echo "=========================================="
echo "Scenario: $SCENARIO"
echo "Duration: $DURATION seconds"
echo "Mode: $(if [ "$QUICK" = true ]; then echo 'Quick'; else echo 'Standard'; fi)"
echo "Verbose: $VERBOSE"
echo "Project: $PROJECT_DIR"
echo "=========================================="
echo ""

# Change to project directory
cd "$PROJECT_DIR"

# Build the project
echo "Building project..."
if ! dotnet build --configuration Release --verbosity quiet; then
    echo "Build failed!"
    exit 1
fi

# Run the test
echo "Running test..."
echo "Command: dotnet run --project . ${ARGS[*]}"

# Execute and capture timing
START_TIME=$(date +%s)
if dotnet run --project . "${ARGS[@]}"; then
    EXIT_CODE=$?
else
    EXIT_CODE=$?
fi
END_TIME=$(date +%s)

# Display results
DURATION_EXEC=$((END_TIME - START_TIME))
echo ""
echo "=========================================="
echo "  Test Execution Complete"
echo "=========================================="

if [ $EXIT_CODE -eq 0 ]; then
    echo -e "Exit Code: $EXIT_CODE \033[32m(SUCCESS)\033[0m"
else
    echo -e "Exit Code: $EXIT_CODE \033[31m(FAILED)\033[0m"
fi

echo "Duration: $DURATION_EXEC seconds"

# Show recent log files
echo ""
echo "Recent Log Files:"
find "$LOG_DIR" -name "*.log" -type f -printf '%T@ %p\n' 2>/dev/null | sort -n | tail -3 | cut -d' ' -f2- | while read -r log; do
    echo "  $log"
done

# Show recent reports
REPORT_DIR="$SCRIPT_DIR/Distribution/AuditReports"
echo ""
echo "Latest Reports:"
find "$REPORT_DIR" -name "Latest_*.md" -type f -printf '%T@ %p\n' 2>/dev/null | sort -n | tail -3 | cut -d' ' -f2- | while read -r report; do
    echo "  $report"
done

exit $EXIT_CODE
