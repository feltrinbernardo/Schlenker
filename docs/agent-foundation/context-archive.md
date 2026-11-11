# Pre-Compaction Context Archive

Date: 2026-09-25  
Status: local retention and remote service implemented; Railway deployment pending

## Outcome

Codex already tracks the active model context and decides when its configured
automatic compaction threshold has been reached. This repository explicitly
uses the full active context scope and attaches a synchronous `PreCompact` hook
to both `manual` and `auto` triggers.

Immediately before compaction, the hook:

1. reads the exact bytes at the Codex-provided `transcript_path` without parsing
   its unstable format;
2. computes a SHA-256 and deterministic snapshot ID;
3. stores a gzip-compressed exact copy in a transactional local SQLite database;
4. optionally sends a gzip-compressed JSON envelope to an allowlisted HTTPS
   ingestion endpoint; and
5. blocks compaction when local storage fails or when required remote delivery
   does not receive a successful response.

After compaction, a `SessionStart` hook adds only a short receipt containing the
snapshot prefix, hash prefix, byte count, and remote status. Reinjecting the
full archive would negate the purpose of compaction.

## Storage

Local storage is always required:

```text
logs/context-archive/context-archive.sqlite3
```

The directory is ignored by Git and rejected by repository validation if it is
ever staged. SQLite uses a full-synchronous transaction and stores the exact
transcript as a gzip BLOB.

Remote storage is configured at runtime:

```text
SCHLENKER_CONTEXT_ARCHIVE_MODE=local|mirror|required-http
SCHLENKER_CONTEXT_ARCHIVE_URL=https://context.example.internal/v1/snapshots
SCHLENKER_CONTEXT_ARCHIVE_TOKEN=<runtime secret>
SCHLENKER_CONTEXT_ARCHIVE_TIMEOUT_SECONDS=15
SCHLENKER_CONTEXT_ARCHIVE_MAX_BYTES=104857600
```

The endpoint implementation now exists under `services/context-archive-api/`,
but no credential or public Railway domain is committed. Until the deployment
gate is completed, the default is durable local retention. Use `required-http`
only after `/readyz` passes and an authenticated end-to-end archive test returns
a receipt.

The remote architecture stores encrypted transcript objects in the private
Evidence bucket and metadata/idempotency state in the existing PostgreSQL
database. This avoids turning PostgreSQL into a large binary transcript store.
See [Railway context-archive deployment](railway-context-archive-deployment.md).

## What “complete context” means

The archive contains every byte in the session transcript file that Codex
exposes to the hook at `PreCompact`. Official documentation describes that path
as a convenience interface with an unstable format. Therefore:

- preserve the bytes rather than relying on individual JSONL fields;
- do not claim that hidden chain-of-thought or internal product state is present;
- images and large artifacts may be represented by references instead of their
  original binary content; and
- database access must be treated as access to sensitive conversation data.

## Verification

```powershell
python .codex/hooks/context_archive.py --verify
.\scripts\test-prompt-judge.ps1
.\scripts\test-repository.ps1
```

The deterministic tests use temporary transcripts and databases. They never
send network traffic or store test context in the project archive directory.

Changing the hook definition requires a new Codex `/hooks` trust review.
