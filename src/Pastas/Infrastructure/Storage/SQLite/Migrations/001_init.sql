CREATE TABLE IF NOT EXISTS schema_version (
    version INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS clipboard_items (
    id TEXT PRIMARY KEY,
    type TEXT NOT NULL,
    preview_text TEXT NULL,
    content_text TEXT NULL,
    encrypted_content TEXT NULL,
    image_path TEXT NULL,
    thumbnail_path TEXT NULL,
    source_app TEXT NULL,
    source_window_title TEXT NULL,
    hash TEXT NOT NULL,
    is_pinned INTEGER NOT NULL DEFAULT 0,
    is_protected INTEGER NOT NULL DEFAULT 0,
    copy_count INTEGER NOT NULL DEFAULT 1,
    size_bytes INTEGER NOT NULL DEFAULT 0,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    last_copied_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS app_settings (
    key TEXT PRIMARY KEY,
    value TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_clipboard_items_hash ON clipboard_items(hash);
CREATE INDEX IF NOT EXISTS idx_clipboard_items_type ON clipboard_items(type);
CREATE INDEX IF NOT EXISTS idx_clipboard_items_last_copied_at ON clipboard_items(last_copied_at);
CREATE INDEX IF NOT EXISTS idx_clipboard_items_is_pinned ON clipboard_items(is_pinned);
CREATE INDEX IF NOT EXISTS idx_clipboard_items_is_protected ON clipboard_items(is_protected);
