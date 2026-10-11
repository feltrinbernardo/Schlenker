"""Object storage plus PostgreSQL metadata persistence."""

from __future__ import annotations

import hashlib
from dataclasses import dataclass
from importlib.resources import files

import boto3
from botocore.exceptions import ClientError
from psycopg.rows import dict_row
from psycopg_pool import ConnectionPool

from .config import Settings
from .crypto import SealedArchive
from .protocol import ArchiveEnvelope


class PersistenceError(RuntimeError):
    """Persistence failed without exposing credentials or payload contents."""


class SnapshotConflict(PersistenceError):
    """An existing id refers to different immutable content."""


@dataclass(frozen=True)
class PersistenceReceipt:
    status: str
    snapshot_id: str
    object_key: str


class S3BlobStore:
    def __init__(self, settings: Settings):
        self.bucket = settings.s3_bucket
        self.prefix = settings.object_prefix
        self.client = boto3.client(
            "s3",
            endpoint_url=settings.s3_endpoint_url,
            region_name=settings.s3_region,
            aws_access_key_id=settings.s3_access_key_id,
            aws_secret_access_key=settings.s3_secret_access_key,
        )

    def object_key(self, envelope: ArchiveEnvelope) -> str:
        date_path = envelope.captured_at.strftime("%Y/%m/%d")
        return (
            f"{self.prefix}/{date_path}/{envelope.snapshot_id}-"
            f"{envelope.transcript_sha256[:16]}.jsonl.gz.aesgcm"
        )

    def put(self, key: str, sealed: SealedArchive, envelope: ArchiveEnvelope) -> None:
        try:
            self.client.put_object(
                Bucket=self.bucket,
                Key=key,
                Body=sealed.object_bytes,
                ContentType="application/octet-stream",
                Metadata={
                    "snapshot-id": envelope.snapshot_id,
                    "transcript-sha256": envelope.transcript_sha256,
                    "transcript-bytes": str(envelope.transcript_bytes),
                    "key-id": sealed.key_id,
                },
            )
        except ClientError as error:
            raise PersistenceError("Object storage rejected the archive") from error

    def ping(self) -> None:
        try:
            self.client.head_bucket(Bucket=self.bucket)
        except ClientError as error:
            raise PersistenceError("Object storage is unavailable") from error


class ArchiveStore:
    def __init__(self, settings: Settings):
        self.settings = settings
        self.blobs = S3BlobStore(settings)
        self.pool = ConnectionPool(
            conninfo=settings.database_url,
            min_size=1,
            max_size=5,
            timeout=10,
            kwargs={"row_factory": dict_row},
        )

    def close(self) -> None:
        self.pool.close()

    def bootstrap(self) -> None:
        schema = files("context_archive_api").joinpath("schema.sql").read_text("utf-8")
        with self.pool.connection() as connection:
            with connection.transaction():
                connection.execute(schema)

    def ping(self) -> None:
        with self.pool.connection() as connection:
            connection.execute("SELECT 1").fetchone()
        self.blobs.ping()

    def _existing(self, snapshot_id: str):
        with self.pool.connection() as connection:
            return connection.execute(
                """
                SELECT snapshot_id, transcript_sha256, transcript_bytes,
                       object_key, encryption_key_id
                FROM context_archive.snapshots
                WHERE snapshot_id = %s
                """,
                (snapshot_id,),
            ).fetchone()

    @staticmethod
    def _assert_same(existing, envelope: ArchiveEnvelope) -> None:
        if (
            str(existing["transcript_sha256"]).strip() != envelope.transcript_sha256
            or int(existing["transcript_bytes"]) != envelope.transcript_bytes
        ):
            raise SnapshotConflict("Snapshot id already exists with different content")

    def persist(
        self, envelope: ArchiveEnvelope, sealed: SealedArchive
    ) -> PersistenceReceipt:
        existing = self._existing(envelope.snapshot_id)
        if existing is not None:
            self._assert_same(existing, envelope)
            with self.pool.connection() as connection:
                with connection.transaction():
                    connection.execute(
                        """
                        UPDATE context_archive.snapshots
                        SET last_received_at = now(), ingest_count = ingest_count + 1
                        WHERE snapshot_id = %s
                        """,
                        (envelope.snapshot_id,),
                    )
            return PersistenceReceipt(
                "duplicate", envelope.snapshot_id, str(existing["object_key"])
            )

        object_key = self.blobs.object_key(envelope)
        self.blobs.put(object_key, sealed, envelope)
        with self.pool.connection() as connection:
            with connection.transaction():
                inserted = connection.execute(
                    """
                    INSERT INTO context_archive.snapshots (
                        snapshot_id, captured_at, session_digest, turn_digest,
                        trigger, model, cwd_digest, transcript_sha256, transcript_bytes,
                        object_bucket, object_key, encryption_key_id
                    ) VALUES (
                        %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s
                    )
                    ON CONFLICT (snapshot_id) DO NOTHING
                    RETURNING snapshot_id
                    """,
                    (
                        envelope.snapshot_id,
                        envelope.captured_at,
                        envelope.session_digest,
                        envelope.turn_digest,
                        envelope.trigger,
                        envelope.model,
                        hashlib.sha256(
                            envelope.cwd.encode("utf-8", errors="replace")
                        ).hexdigest(),
                        envelope.transcript_sha256,
                        envelope.transcript_bytes,
                        self.settings.s3_bucket,
                        object_key,
                        sealed.key_id,
                    ),
                ).fetchone()
        if inserted is not None:
            return PersistenceReceipt("stored", envelope.snapshot_id, object_key)

        existing = self._existing(envelope.snapshot_id)
        if existing is None:
            raise PersistenceError("Snapshot metadata insert was not durable")
        self._assert_same(existing, envelope)
        return PersistenceReceipt(
            "duplicate", envelope.snapshot_id, str(existing["object_key"])
        )
