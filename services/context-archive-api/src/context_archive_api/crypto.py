"""Application-level authenticated encryption for archived transcripts."""

from __future__ import annotations

import gzip
import json
import os
import struct
from dataclasses import dataclass

from cryptography.hazmat.primitives.ciphers.aead import AESGCM

from .protocol import ArchiveEnvelope


MAGIC = b"SCA1"


@dataclass(frozen=True)
class SealedArchive:
    object_bytes: bytes
    key_id: str


def _associated_data(envelope: ArchiveEnvelope) -> bytes:
    return json.dumps(
        {
            "snapshot_id": envelope.snapshot_id,
            "transcript_sha256": envelope.transcript_sha256,
            "transcript_bytes": envelope.transcript_bytes,
        },
        sort_keys=True,
        separators=(",", ":"),
    ).encode("utf-8")


def seal_archive(
    envelope: ArchiveEnvelope, encryption_key: bytes, key_id: str
) -> SealedArchive:
    compressed = gzip.compress(envelope.transcript, compresslevel=6, mtime=0)
    nonce = os.urandom(12)
    associated_data = _associated_data(envelope)
    ciphertext = AESGCM(encryption_key).encrypt(nonce, compressed, associated_data)
    header = json.dumps(
        {
            "schema_version": 1,
            "cipher": "AES-256-GCM",
            "compression": "gzip",
            "key_id": key_id,
            "nonce_hex": nonce.hex(),
            "associated_data": json.loads(associated_data),
        },
        sort_keys=True,
        separators=(",", ":"),
    ).encode("utf-8")
    return SealedArchive(
        object_bytes=MAGIC + struct.pack(">I", len(header)) + header + ciphertext,
        key_id=key_id,
    )
