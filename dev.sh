#!/usr/bin/env bash
set -euo pipefail

cleanup() {
    echo "Shutting down..."
    kill "$SERVER_PID" "$CLIENT_PID" 2>/dev/null
    wait "$SERVER_PID" "$CLIENT_PID" 2>/dev/null
    echo "Done."
}
trap cleanup EXIT INT TERM

REPO_ROOT="$(cd "$(dirname "$0")" && pwd)"
export BOXTRACKER_DATA="${BOXTRACKER_DATA:-$REPO_ROOT/data}"

mkdir -p "$BOXTRACKER_DATA"

nix develop "$REPO_ROOT" --command bash -c \
    "dotnet run --project src/Server/BoxTracker.Server.fsproj --urls http://localhost:5000" &
SERVER_PID=$!

nix develop "$REPO_ROOT" --command bash -c "npm start" &
CLIENT_PID=$!

echo "Server:  http://localhost:5000"
echo "Client:  http://localhost:5173"
echo "Press Ctrl+C to stop both."

wait
