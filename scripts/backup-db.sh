#!/usr/bin/env bash
set -euo pipefail

TIMESTAMP=$(date +%Y%m%d_%H%M%S)
SNAPSHOT="/tmp/boxtracker_${TIMESTAMP}.db"

sqlite3 /opt/boxtracker/data/boxtracker.db "VACUUM INTO '${SNAPSHOT}'"
rclone copy "${SNAPSHOT}" boxes-db:boxes-db/ --b2-hard-delete
rm -f "${SNAPSHOT}"

echo "DB backup complete: ${TIMESTAMP}"
