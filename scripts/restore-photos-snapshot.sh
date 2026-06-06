#!/usr/bin/env bash
set -euo pipefail

# Restore the photos directory from a pre-migration snapshot taken by the
# deploy step (see .github/workflows/ci.yml). Use this to undo a migration
# that lost or corrupted images.
#
# Usage:
#   restore-photos-snapshot.sh            # restore the most recent snapshot
#   restore-photos-snapshot.sh <file>     # restore a specific snapshot tarball
#   restore-photos-snapshot.sh --list     # list available snapshots

BACKUP_DIR="/opt/boxtracker/backups"
DATA_DIR="/opt/boxtracker/data"
PHOTOS_DIR="${DATA_DIR}/photos"

list_snapshots() {
  ls -1t "${BACKUP_DIR}"/photos-pre-migration-*.tar.gz 2>/dev/null || {
    echo "No snapshots found in ${BACKUP_DIR}" >&2
    return 1
  }
}

if [[ "${1:-}" == "--list" ]]; then
  list_snapshots
  exit 0
fi

SNAPSHOT="${1:-}"
if [[ -z "${SNAPSHOT}" ]]; then
  SNAPSHOT=$(list_snapshots | head -n 1)
fi

if [[ ! -f "${SNAPSHOT}" ]]; then
  echo "Snapshot not found: ${SNAPSHOT}" >&2
  echo "Available snapshots:" >&2
  list_snapshots >&2 || true
  exit 1
fi

echo "Restoring photos from: ${SNAPSHOT}"

# Stop the app so it does not write while we swap directories.
sudo systemctl stop boxtracker

# Preserve the current (broken) state before overwriting, just in case.
if [[ -d "${PHOTOS_DIR}" ]]; then
  SAFETY="${PHOTOS_DIR}.before-restore-$(date +%Y%m%d-%H%M%S)"
  mv "${PHOTOS_DIR}" "${SAFETY}"
  echo "Current photos moved to: ${SAFETY}"
fi

# The tarball stores a top-level "photos" directory, so extract into DATA_DIR.
tar -xzf "${SNAPSHOT}" -C "${DATA_DIR}"

sudo systemctl start boxtracker

echo "Restore complete. Photos restored to ${PHOTOS_DIR}"
echo "If everything looks good, you can remove the pre-restore copy under ${DATA_DIR}."
