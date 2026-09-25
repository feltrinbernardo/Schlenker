# Railway Context-Archive Deployment

Date: 2026-09-25  
Status: implementation validated; external deployment gate pending

## Reviewed live topology

The read-only Railway review found two production projects:

- `soothing-purpose`: an active PostgreSQL 18 service, the CLI-deployed
  `fbs-collector`, and the private `Evidence` bucket;
- `discerning-enchantment`: an online PostgreSQL 18 service with no user tables.

The active collector has no connected source repository, no public domain, and
no healthcheck. Replacing its CLI deployment would risk the existing Telegram
audio/transcription workflow, so the archive boundary is a separate service.

Neither PostgreSQL service has a managed backup or point-in-time recovery. The
Railway UI reports those features as Pro-plan capabilities, so enabling them is
a separate financial decision and is not silently performed by this chore.

## Target topology

```text
Codex PreCompact
  -> public Railway HTTPS domain
  -> context-archive-api (bearer auth, size/hash/id checks)
  -> AES-256-GCM object in private Evidence bucket
  -> metadata row in PostgreSQL context_archive.snapshots

Existing fbs-collector
  -> unchanged existing events/media/state/transcriptions tables
```

The API is deliberately write-only. It does not expose a transcript read,
search, list, delete, or key-management endpoint. Recovery therefore requires
an explicitly authorized offline process with the database receipt, bucket
object, and matching encryption key.

## Deployment gate

Before deploying:

1. connect the Schlenker GitHub repository to Railway or use an independently
   authenticated Railway CLI workflow;
2. create a separate `context-archive-api` service rooted at
   `/services/context-archive-api`;
3. reference the production PostgreSQL and Evidence bucket variables;
4. create independent bearer and AES-256-GCM secrets in the runtime secret
   store;
5. generate a public Railway HTTPS domain;
6. set `/readyz` as the healthcheck; and
7. run one synthetic end-to-end archive before enabling `required-http` in
   Codex.

Connecting the GitHub App changes account permissions, creating secrets creates
persistent credentials, and upgrading Railway can create a financial
commitment. Those steps require an action-time operator checkpoint.

## Priority risks and controls

| Priority | Observed risk | Implemented or required control |
|---|---|---|
| P0 | No PostgreSQL backups or PITR | Decide on Pro managed backup/PITR or a separately reviewed encrypted logical-backup service before schema changes |
| P0 | Sensitive full conversation content | Independent bearer token, strict request caps, AES-256-GCM before object storage, no body/access logs |
| P1 | Existing collector source is not versioned or connected | Preserve it; recover/export its source before the next feature change |
| P1 | Single replica and no HA | Accept for current low load or approve a paid HA plan based on recovery objectives |
| P1 | No query-performance extension | Enable `pg_stat_statements` only after backup and a maintenance window |
| P2 | One table is flagged for vacuum | Low absolute dead-row count; verify autovacuum and run `VACUUM (ANALYZE)` only after backup |
| P2 | Empty second PostgreSQL service | Confirm ownership and retention need before deletion; deletion is not authorized by this document |

## Acceptance evidence

Repository acceptance requires:

```powershell
python -m unittest judge.test_context_archive_api -v
.\scripts\test-prompt-judge.ps1
.\scripts\test-repository.ps1
```

Live acceptance additionally requires a successful `/readyz`, a `stored`
receipt for a synthetic snapshot, a `duplicate` receipt on replay, matching
PostgreSQL metadata, and an encrypted Evidence object that does not contain the
plaintext transcript.
