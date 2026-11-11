# Context Archive API

This service is the remote ingestion boundary for the repository's synchronous
`PreCompact` hook. It does not replace the existing `fbs-collector` Railway
service.

## Architecture

```text
Codex PreCompact hook
  -> HTTPS bearer-authenticated POST /v1/snapshots
  -> strict envelope/hash/id validation
  -> deterministic gzip + AES-256-GCM
  -> private Evidence bucket (encrypted opaque object)
  -> PostgreSQL context_archive.snapshots (metadata and idempotency)
```

Transcript content is not logged, returned, or stored as plaintext in
PostgreSQL. The object is self-describing enough to recover it from the bucket
when the encryption key identified by `key_id` is available.

## Required Railway variables

Configure these as runtime secrets or references. Never commit literal values.

```text
DATABASE_URL=<reference to the production Postgres service>
CONTEXT_ARCHIVE_TOKEN=<independent high-entropy bearer token>
CONTEXT_ARCHIVE_ENCRYPTION_KEY=<base64 of exactly 32 random bytes>
CONTEXT_ARCHIVE_KEY_ID=railway-2026-01
ARCHIVE_S3_BUCKET=<Evidence bucket name/reference>
ARCHIVE_S3_ENDPOINT_URL=<Evidence S3 endpoint/reference>
ARCHIVE_S3_REGION=<Evidence region/reference>
ARCHIVE_S3_ACCESS_KEY_ID=<Evidence access-key reference>
ARCHIVE_S3_SECRET_ACCESS_KEY=<Evidence secret-key reference>
```

Railway bucket templates may expose the shorter `BUCKET`, `ENDPOINT`, `REGION`,
`ACCESS_KEY_ID`, and `SECRET_ACCESS_KEY` names; the service accepts either set.
The bearer token must contain at least 32 characters, the encryption key must be
32 random bytes encoded as base64, and the object-storage endpoint must use
HTTPS.

Recommended service settings:

- root directory: `/services/context-archive-api`;
- healthcheck path: `/readyz`;
- one replica initially;
- public Railway HTTPS domain;
- restart on failure;
- no serverless sleep while `required-http` is the client mode.

After the public domain and secrets are installed, configure the Codex process:

```text
SCHLENKER_CONTEXT_ARCHIVE_MODE=required-http
SCHLENKER_CONTEXT_ARCHIVE_URL=https://<railway-domain>/v1/snapshots
SCHLENKER_CONTEXT_ARCHIVE_TOKEN=<same runtime secret>
```

## Local verification

The protocol tests use no network, database, bucket, or secret:

```powershell
python -m unittest judge.test_context_archive_api -v
```

The deployment should not be activated until the PostgreSQL and Evidence bucket
references, token, encryption key, public domain, and `/readyz` healthcheck are
all present. A Railway plan decision is separate because enabling managed
backups/PITR can create a financial commitment.
