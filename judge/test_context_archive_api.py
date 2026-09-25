from __future__ import annotations

import base64
import gzip
import hashlib
import json
import sys
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SERVICE_SRC = ROOT / "services" / "context-archive-api" / "src"
sys.path.insert(0, str(SERVICE_SRC))

from context_archive_api.config import ConfigurationError, Settings  # noqa: E402
from context_archive_api.protocol import (  # noqa: E402
    ArchiveValidationError,
    decompress_gzip_limited,
    parse_envelope,
)


class ContextArchiveApiTests(unittest.TestCase):
    @staticmethod
    def envelope(transcript: bytes = b"exact transcript bytes"):
        session_digest = hashlib.sha256(b"session").hexdigest()
        turn_digest = hashlib.sha256(b"turn").hexdigest()
        transcript_sha256 = hashlib.sha256(transcript).hexdigest()
        snapshot_id = hashlib.sha256(
            "|".join(
                (session_digest, turn_digest, "auto", transcript_sha256)
            ).encode("ascii")
        ).hexdigest()
        return {
            "schema_version": 1,
            "snapshot_id": snapshot_id,
            "captured_at": "2026-09-25T00:00:00+00:00",
            "session_digest": session_digest,
            "turn_digest": turn_digest,
            "trigger": "auto",
            "model": "test-model",
            "cwd": ".",
            "transcript_sha256": transcript_sha256,
            "transcript_bytes": len(transcript),
            "transcript_base64": base64.b64encode(transcript).decode("ascii"),
        }

    def test_exact_transcript_and_deterministic_id_are_validated(self):
        payload = self.envelope()
        encoded = json.dumps(payload).encode("utf-8")
        parsed = parse_envelope(
            encoded,
            payload["snapshot_id"],
            payload["transcript_sha256"],
            1024,
        )
        self.assertEqual(parsed.transcript, b"exact transcript bytes")

    def test_hash_mismatch_is_rejected(self):
        payload = self.envelope()
        payload["transcript_sha256"] = "0" * 64
        with self.assertRaises(ArchiveValidationError) as raised:
            parse_envelope(
                json.dumps(payload).encode("utf-8"),
                payload["snapshot_id"],
                payload["transcript_sha256"],
                1024,
            )
        self.assertEqual(raised.exception.reason_code, "TRANSCRIPT_HASH_MISMATCH")

    def test_header_mismatch_is_rejected(self):
        payload = self.envelope()
        with self.assertRaises(ArchiveValidationError) as raised:
            parse_envelope(
                json.dumps(payload).encode("utf-8"),
                "f" * 64,
                payload["transcript_sha256"],
                1024,
            )
        self.assertEqual(raised.exception.reason_code, "HEADER_MISMATCH")

    def test_gzip_limit_prevents_expansion_bomb(self):
        compressed = gzip.compress(b"a" * 4096, mtime=0)
        with self.assertRaises(ArchiveValidationError) as raised:
            decompress_gzip_limited(compressed, 128)
        self.assertEqual(
            raised.exception.reason_code, "DECOMPRESSED_BODY_TOO_LARGE"
        )

    def test_configuration_requires_32_byte_encryption_key(self):
        environment = {
            "DATABASE_URL": "postgresql://example.invalid/db",
            "CONTEXT_ARCHIVE_TOKEN": "t" * 32,
            "CONTEXT_ARCHIVE_ENCRYPTION_KEY": base64.b64encode(b"short").decode(),
            "CONTEXT_ARCHIVE_KEY_ID": "key-1",
            "ARCHIVE_S3_BUCKET": "bucket",
            "ARCHIVE_S3_ENDPOINT_URL": "https://storage.example.invalid",
            "ARCHIVE_S3_ACCESS_KEY_ID": "access",
            "ARCHIVE_S3_SECRET_ACCESS_KEY": "secret",
        }
        with self.assertRaises(ConfigurationError):
            Settings.from_environment(environment)

    def test_configuration_repr_does_not_expose_secrets(self):
        environment = {
            "DATABASE_URL": "postgresql://user:database-secret@example.invalid/db",
            "CONTEXT_ARCHIVE_TOKEN": "bearer-secret-which-is-at-least-32-chars",
            "CONTEXT_ARCHIVE_ENCRYPTION_KEY": base64.b64encode(b"k" * 32).decode(),
            "CONTEXT_ARCHIVE_KEY_ID": "key-1",
            "ARCHIVE_S3_BUCKET": "bucket",
            "ARCHIVE_S3_ENDPOINT_URL": "https://storage.example.invalid",
            "ARCHIVE_S3_ACCESS_KEY_ID": "access-secret",
            "ARCHIVE_S3_SECRET_ACCESS_KEY": "storage-secret",
        }
        rendered = repr(Settings.from_environment(environment))
        for secret in (
            "database-secret",
            "bearer-secret",
            "access-secret",
            "storage-secret",
        ):
            self.assertNotIn(secret, rendered)

    def test_object_storage_endpoint_requires_https(self):
        environment = {
            "DATABASE_URL": "postgresql://example.invalid/db",
            "CONTEXT_ARCHIVE_TOKEN": "t" * 32,
            "CONTEXT_ARCHIVE_ENCRYPTION_KEY": base64.b64encode(b"k" * 32).decode(),
            "CONTEXT_ARCHIVE_KEY_ID": "key-1",
            "ARCHIVE_S3_BUCKET": "bucket",
            "ARCHIVE_S3_ENDPOINT_URL": "http://storage.example.invalid",
            "ARCHIVE_S3_ACCESS_KEY_ID": "access",
            "ARCHIVE_S3_SECRET_ACCESS_KEY": "secret",
        }
        with self.assertRaises(ConfigurationError):
            Settings.from_environment(environment)


if __name__ == "__main__":
    unittest.main()
