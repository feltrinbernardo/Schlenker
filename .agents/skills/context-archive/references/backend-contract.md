# Context Archive Backend Contract

The hook always stores an exact gzip-compressed copy of the transcript bytes in
the ignored local SQLite database at `logs/context-archive/context-archive.sqlite3`.

## Modes

- `local` (default): require only the local SQLite transaction.
- `mirror`: require the local transaction and attempt remote delivery; a remote
  failure is recorded but does not block compaction.
- `required-http`: require both the local transaction and a successful remote
  response before compaction may continue.

Configure with process environment variables:

```text
SCHLENKER_CONTEXT_ARCHIVE_MODE=required-http
SCHLENKER_CONTEXT_ARCHIVE_URL=https://context.example.internal/v1/snapshots
SCHLENKER_CONTEXT_ARCHIVE_TOKEN=<secret from the runtime secret store>
SCHLENKER_CONTEXT_ARCHIVE_TIMEOUT_SECONDS=15
```

The URL must use HTTPS. Plain HTTP is accepted only for `localhost`, `127.0.0.1`,
or `::1` integration tests. Redirects are rejected. The bearer token is optional
for endpoints that use another transport-level identity, but it must never be
committed, logged, or returned to the model.

## Request

The hook sends `POST` with `Content-Type: application/json` and
`Content-Encoding: gzip`. The decompressed object contains:

- `schema_version`;
- `snapshot_id`;
- UTC capture time;
- hashed session and turn identifiers;
- compaction trigger and model;
- repository-relative working-directory metadata;
- transcript SHA-256 and byte count; and
- `transcript_base64`, the exact transcript bytes encoded without parsing.

The repository implementation at `services/context-archive-api/` authenticates
the request, validates the deterministic snapshot ID and transcript hash,
encrypts the gzip-compressed transcript with AES-256-GCM, writes the opaque
object to the private Evidence bucket, and writes only metadata/idempotency
state to PostgreSQL. Duplicate IDs with identical immutable content return the
existing receipt; conflicts are rejected. The response is a bounded JSON
receipt with HTTP status `200`.

The transcript format is intentionally opaque because Codex documents it as an
unstable convenience interface. The backend must preserve bytes rather than
depending on individual JSONL fields.
