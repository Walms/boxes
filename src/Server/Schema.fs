module BoxTracker.Schema

let createTables : string =
    """
    CREATE TABLE IF NOT EXISTS location (
        code        TEXT PRIMARY KEY,
        name        TEXT NOT NULL,
        is_archived INTEGER NOT NULL DEFAULT 0,
        photo_path  TEXT,
        created_at  TEXT NOT NULL
    );

    CREATE TABLE IF NOT EXISTS box (
        id          TEXT PRIMARY KEY,
        label       TEXT,
        photo_path  TEXT,
        created_at  TEXT NOT NULL
    );

    CREATE TABLE IF NOT EXISTS item (
        id          TEXT PRIMARY KEY,
        name        TEXT NOT NULL,
        photo_path  TEXT,
        added_at    TEXT NOT NULL
    );

    CREATE TABLE IF NOT EXISTS move (
        id          TEXT PRIMARY KEY,
        entity_type TEXT NOT NULL,
        entity_id   TEXT NOT NULL,
        to_type     TEXT,
        to_id       TEXT,
        moved_at    TEXT NOT NULL
    );

    CREATE INDEX IF NOT EXISTS idx_move_entity
        ON move (entity_type, entity_id, moved_at DESC);

    CREATE VIRTUAL TABLE IF NOT EXISTS item_search USING fts5 (
        item_id,
        item_name,
        box_label,
        location_name,
        tokenize    = 'porter unicode61'
    );
    """
