# Backup System

Automated backups to Backblaze B2 for database and photos.

## Overview

The system performs hourly snapshots of the SQLite database and hourly syncs of all uploaded photos to separate B2 buckets. Backups run as cron jobs on the production server.

## Architecture

```
/opt/boxtracker/scripts/
  ├── backup-db.sh        ← SQLite VACUUM INTO snapshot + rclone copy to B2
  └── backup-photos.sh    ← rclone sync to B2 (mirror, no history)

/home/deploy/.config/rclone/rclone.conf
  ├── [boxes-db]          ← B2 remote (no bucket field; use path in command)
  └── [boxes-images]      ← B2 remote (no bucket field; use path in command)

Cron (deploy user):
  0  * * * *   /opt/boxtracker/scripts/backup-db.sh
  30 * * * *   /opt/boxtracker/scripts/backup-photos.sh
```

Scheduling is staggered (DB at :00, photos at :30 each hour) to avoid simultaneous server load.

## Database Backups

**Source**: `/opt/boxtracker/data/boxtracker.db` (live, running app)  
**Method**: `sqlite3 VACUUM INTO /tmp/boxtracker_<timestamp>.db` — creates consistent snapshot without stopping the app  
**Destination**: `boxes-db:boxes-db/` bucket on B2 with timestamped filename  
**Frequency**: Hourly (`:00` each hour)  
**Retention**: 30 days via B2 lifecycle rule (delete files older than 30 days)  
**Restore points**: 720 (30 days × 24 hours)

## Photo Backups

**Source**: `/opt/boxtracker/data/photos/` (flat file storage)  
**Method**: `rclone sync --checksum --b2-hard-delete` — incremental, checksummed, deleted files hard-deleted  
**Destination**: `boxes-images:boxes-images/` bucket on B2  
**Frequency**: Hourly (`:30` each hour)  
**Retention**: No expiration — bucket mirrors current server state (no history)  
**Transfer**: Up to 4 parallel transfers, `--fast-list` for large directories

## Credentials

rclone config stored at `/home/deploy/.config/rclone/rclone.conf` (mode 600, deploy-owned):

```ini
[boxes-db]
type = b2
account = <keyID>
key = <applicationKey>

[boxes-images]
type = b2
account = <keyID>
key = <applicationKey>
```

Each B2 app key is scoped to its respective bucket (boxes-db key has access only to boxes-db bucket, etc.).

**Important**: Do NOT add a `bucket` field to the rclone B2 config. Instead, specify the bucket explicitly in the remote path: `boxes-db:boxes-db/` not `boxes-db:/`.

Credentials are provisioned during CI/CD deploy via GitHub Actions secrets:
- `B2_DB_KEY_ID`, `B2_DB_APP_KEY` (for boxes-db bucket)
- `B2_IMAGES_KEY_ID`, `B2_IMAGES_APP_KEY` (for boxes-images bucket)

GitHub Actions writes the rclone.conf to the server on every deploy via SSH.

## Logs

- DB backups: `/var/log/boxtracker/backup-db.log` (hourly)
- Photo syncs: `/var/log/boxtracker/backup-photos.log` (hourly)

Check with: `tail -f /var/log/boxtracker/backup-db.log`

## Testing

Manually run backups on the server:

```bash
sudo -u deploy /opt/boxtracker/scripts/backup-db.sh
sudo -u deploy /opt/boxtracker/scripts/backup-photos.sh
```

List backups in B2 (requires B2 CLI authorization first):

```bash
b2 authorize-account <keyID> <applicationKey>
b2 ls b2://boxes-db --recursive
b2 ls b2://boxes-images --recursive
```

## Restore Procedure

For a database restore:

1. Download a timestamped `.db` file from the boxes-db bucket
2. Stop the BoxTracker service: `sudo systemctl stop boxtracker`
3. Replace `/opt/boxtracker/data/boxtracker.db` with the downloaded file
4. Restart: `sudo systemctl start boxtracker`

For photos, `rclone sync` can be reversed (sync FROM B2 TO local directory if needed).

## Tools

- **rclone**: Handles both B2 buckets with unified config; supports incremental sync (`--checksum`), parallel transfers, and hard-delete semantics
- **sqlite3**: Built-in `VACUUM INTO` for consistent snapshots without locking the live database
- **B2 CLI** (optional): For manual verification and testing

## Deployment

Backup system is deployed as part of the standard CI/CD pipeline:

1. GitHub Actions (`.github/workflows/ci.yml`):
   - SCP backup scripts to `/opt/boxtracker/scripts/`
   - SSH to server to write rclone.conf from secrets
   - Install rclone if missing
   - Idempotently install cron jobs (removes old lines, adds current ones)

2. Manual one-time setup (on initial server provisioning):
   - Allow deploy user to run `sudo mkdir`, `sudo chown` without password (sudoers config)
   - Create `/var/log/boxtracker` directory with deploy ownership

No changes needed for subsequent deployments.
