CREATE SCHEMA IF NOT EXISTS context_archive;

CREATE TABLE IF NOT EXISTS context_archive.snapshots (
    snapshot_id character(64) PRIMARY KEY,
    captured_at timestamptz NOT NULL,
    session_digest character(64) NOT NULL,
    turn_digest character(64) NOT NULL,
    trigger text NOT NULL CHECK (trigger IN ('manual', 'auto')),
    model text NOT NULL,
    cwd_digest character(64) NOT NULL,
    transcript_sha256 character(64) NOT NULL,
    transcript_bytes bigint NOT NULL CHECK (transcript_bytes >= 0),
    object_bucket text NOT NULL,
    object_key text NOT NULL UNIQUE,
    encryption_key_id text NOT NULL,
    first_received_at timestamptz NOT NULL DEFAULT now(),
    last_received_at timestamptz NOT NULL DEFAULT now(),
    ingest_count integer NOT NULL DEFAULT 1 CHECK (ingest_count > 0),
    CONSTRAINT snapshots_snapshot_hex CHECK (snapshot_id ~ '^[0-9a-f]{64}$'),
    CONSTRAINT snapshots_transcript_hex CHECK (transcript_sha256 ~ '^[0-9a-f]{64}$'),
    CONSTRAINT snapshots_session_hex CHECK (session_digest ~ '^[0-9a-f]{64}$'),
    CONSTRAINT snapshots_turn_hex CHECK (turn_digest ~ '^[0-9a-f]{64}$'),
    CONSTRAINT snapshots_cwd_hex CHECK (cwd_digest ~ '^[0-9a-f]{64}$')
);

CREATE INDEX IF NOT EXISTS snapshots_session_captured_idx
    ON context_archive.snapshots (session_digest, captured_at DESC);

CREATE INDEX IF NOT EXISTS snapshots_received_idx
    ON context_archive.snapshots (first_received_at DESC);

REVOKE ALL ON SCHEMA context_archive FROM PUBLIC;
REVOKE ALL ON TABLE context_archive.snapshots FROM PUBLIC;
