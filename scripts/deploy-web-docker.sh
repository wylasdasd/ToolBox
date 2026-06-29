#!/usr/bin/env bash
# Build and run ToolBox Web in Docker.
# Usage:
#   ./scripts/deploy-web-docker.sh              # build + start on port 18488
#   ./scripts/deploy-web-docker.sh --port 3000  # custom host port
#   ./scripts/deploy-web-docker.sh --down       # stop and remove container
#   ./scripts/deploy-web-docker.sh --logs       # follow logs
#   ./scripts/deploy-web-docker.sh --no-build   # start without rebuild

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PORT=18488
DOWN=false
LOGS=false
NO_BUILD=false

while [[ $# -gt 0 ]]; do
    case "$1" in
        --port|-p) PORT="$2"; shift 2 ;;
        --down) DOWN=true; shift ;;
        --logs) LOGS=true; shift ;;
        --no-build) NO_BUILD=true; shift ;;
        -h|--help)
            sed -n '2,8p' "$0"
            exit 0
            ;;
        *) echo "Unknown option: $1" >&2; exit 1 ;;
    esac
done

command -v docker >/dev/null 2>&1 || { echo "Docker is not installed or not in PATH." >&2; exit 1; }

cd "$REPO_ROOT"

if $DOWN; then
    docker compose down
    echo "ToolBox Web container stopped."
    exit 0
fi

if $LOGS; then
    docker compose logs -f toolbox-web
    exit 0
fi

export TOOLBOX_WEB_PORT="$PORT"

if $NO_BUILD; then
    docker compose up -d
else
    docker compose up -d --build
fi

echo ""
echo "ToolBox Web is running: http://localhost:${PORT}"
echo "Stop:  ./scripts/deploy-web-docker.sh --down"
echo "Logs:  ./scripts/deploy-web-docker.sh --logs"
