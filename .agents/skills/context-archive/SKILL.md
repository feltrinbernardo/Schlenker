---
name: context-archive
description: Inspect, validate, or recover Codex context archives created immediately before manual or automatic compaction. Use for context-retention checks, archive receipts, compaction troubleshooting, or backend configuration; do not use as a substitute for the PreCompact hook.
---

# Context Archive

The repository hook, not this skill, performs automatic archival. Codex itself
decides when automatic compaction is needed from the active model context; do
not estimate token usage from transcript byte size or claim that a skill can
invoke `/compact` autonomously.

When this skill is used:

1. Read `.codex/hooks.json` and confirm that synchronous `PreCompact` handling
   points to `.codex/hooks/context_archive.py` for both `manual` and `auto`.
2. Run `python .codex/hooks/context_archive.py --verify` to inspect local store
   readiness and the configured backend mode. Never print the archive content
   or authentication token.
3. Treat the raw `transcript_path` file as the complete archiveable transcript
   exposed by Codex, not as a stable public schema and not as proof that hidden
   model reasoning or every product-internal context item is available.
4. On archive failure, keep compaction blocked and report the stable reason
   code. Do not bypass the hook merely to finish compacting.
5. For recovery, identify the snapshot by receipt, SHA-256, session digest, and
   timestamp. Load raw archived content only when the user explicitly requests
   it and the destination is authorized.

Local SQLite storage is mandatory. Remote mirroring is configured only through
environment variables; secrets never belong in the repository. Read
[`references/backend-contract.md`](references/backend-contract.md) when setting
up or troubleshooting the remote database ingestion endpoint.
