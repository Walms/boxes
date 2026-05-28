#!/usr/bin/env bash
set -euo pipefail

rclone sync /opt/boxtracker/data/photos/ boxes-images:boxes-images/ \
  --checksum \
  --b2-hard-delete \
  --transfers 4 \
  --fast-list

echo "Photos sync complete: $(date)"
