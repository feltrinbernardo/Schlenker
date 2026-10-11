"""Wire validation for the opaque Codex transcript envelope."""

from __future__ import annotations

import base64
import binascii
import hashlib
import json
import re
import zlib
from dataclasses import dataclass
from datetime import datetime
from typing import Any, Mapping


HEX_64 = re.compile(r"^[0-9a-f]{64}$")


class ArchiveValidationError(ValueError):
    """A stable validation failure safe to return without request contents."""

    def __init__(self, reason_code: str, message: str, status_code: int = 400):
        super().__init__(message)
        self.reason_code = reason_code
        self.status_code = status_code


@dataclass(frozen=True)
class ArchiveEnvelope:
    snapshot_id: str
    captured_at: datetime
    session_digest: str
    turn_digest: str
    trigger: str
    model: str
    cwd: str
    transcript_sha256: str
    transcript_bytes: int
    transcript: bytes


def decompress_gzip_limited(compressed: bytes, maximum: int) -> bytes:
    if not compressed:
        raise ArchiveValidationError("EMPTY_BODY", "Request body is empty")
    decompressor = zlib.decompressobj(16 + zlib.MAX_WBITS)
    output = bytearray()
    view = memoryview(compressed)
    try:
        for offset in range(0, len(view), 64 * 1024):
            remaining = maximum - len(output)
            if remaining < 0:
                raise ArchiveValidationError(
                    "DECOMPRESSED_BODY_TOO_LARGE", "Decompressed body exceeds limit", 413
                )
            output.extend(
                decompressor.decompress(view[offset : offset + 64 * 1024], remaining + 1)
            )
            if len(output) > maximum:
                raise ArchiveValidationError(
                    "DECOMPRESSED_BODY_TOO_LARGE", "Decompressed body exceeds limit", 413
                )
        output.extend(decompressor.flush(maximum - len(output) + 1))
    except zlib.error as error:
        raise ArchiveValidationError("INVALID_GZIP", "Body is not valid gzip") from error
    if len(output) > maximum:
        raise ArchiveValidationError(
            "DECOMPRESSED_BODY_TOO_LARGE", "Decompressed body exceeds limit", 413
        )
    if not decompressor.eof or decompressor.unused_data:
        raise ArchiveValidationError(
            "INVALID_GZIP", "Gzip stream is incomplete or contains trailing data"
        )
    return bytes(output)


def _string(
    payload: Mapping[str, Any],
    name: str,
    maximum: int,
    pattern: re.Pattern[str] | None = None,
) -> str:
    value = payload.get(name)
    if not isinstance(value, str) or not value or len(value) > maximum:
        raise ArchiveValidationError(
            "INVALID_ENVELOPE", f"{name} must be a non-empty string up to {maximum} characters"
        )
    if pattern is not None and pattern.fullmatch(value) is None:
        raise ArchiveValidationError("INVALID_ENVELOPE", f"{name} has an invalid format")
    return value


def parse_envelope(
    encoded_json: bytes,
    snapshot_header: str,
    sha_header: str,
    max_transcript_bytes: int,
) -> ArchiveEnvelope:
    try:
        payload = json.loads(encoded_json.decode("utf-8"))
    except (UnicodeDecodeError, json.JSONDecodeError) as error:
        raise ArchiveValidationError("INVALID_JSON", "Body is not valid UTF-8 JSON") from error
    if not isinstance(payload, dict) or payload.get("schema_version") != 1:
        raise ArchiveValidationError(
            "UNSUPPORTED_SCHEMA", "schema_version must be 1"
        )

    snapshot_id = _string(payload, "snapshot_id", 64, HEX_64)
    transcript_sha256 = _string(payload, "transcript_sha256", 64, HEX_64)
    session_digest = _string(payload, "session_digest", 64, HEX_64)
    turn_digest = _string(payload, "turn_digest", 64, HEX_64)
    trigger = _string(payload, "trigger", 16)
    if trigger not in {"manual", "auto"}:
        raise ArchiveValidationError(
            "INVALID_ENVELOPE", "trigger must be manual or auto"
        )
    model = _string(payload, "model", 128)
    cwd = payload.get("cwd", "")
    if not isinstance(cwd, str) or len(cwd) > 2048:
        raise ArchiveValidationError(
            "INVALID_ENVELOPE", "cwd must be a string up to 2048 characters"
        )
    captured_raw = _string(payload, "captured_at", 64)
    try:
        captured_at = datetime.fromisoformat(captured_raw.replace("Z", "+00:00"))
    except ValueError as error:
        raise ArchiveValidationError(
            "INVALID_ENVELOPE", "captured_at must be ISO-8601"
        ) from error
    if captured_at.tzinfo is None:
        raise ArchiveValidationError(
            "INVALID_ENVELOPE", "captured_at must include a timezone"
        )

    byte_count = payload.get("transcript_bytes")
    if not isinstance(byte_count, int) or isinstance(byte_count, bool) or byte_count < 0:
        raise ArchiveValidationError(
            "INVALID_ENVELOPE", "transcript_bytes must be a non-negative integer"
        )
    if byte_count > max_transcript_bytes:
        raise ArchiveValidationError(
            "TRANSCRIPT_TOO_LARGE", "Transcript exceeds configured limit", 413
        )
    encoded_transcript = payload.get("transcript_base64")
    if not isinstance(encoded_transcript, str):
        raise ArchiveValidationError(
            "INVALID_ENVELOPE", "transcript_base64 must be a string"
        )
    try:
        transcript = base64.b64decode(encoded_transcript, validate=True)
    except (binascii.Error, ValueError) as error:
        raise ArchiveValidationError(
            "INVALID_TRANSCRIPT_BASE64", "transcript_base64 is invalid"
        ) from error
    if len(transcript) != byte_count:
        raise ArchiveValidationError(
            "TRANSCRIPT_LENGTH_MISMATCH", "Transcript byte count does not match"
        )
    actual_sha = hashlib.sha256(transcript).hexdigest()
    if actual_sha != transcript_sha256:
        raise ArchiveValidationError(
            "TRANSCRIPT_HASH_MISMATCH", "Transcript digest does not match"
        )

    stable_key = "|".join(
        (session_digest, turn_digest, trigger, transcript_sha256)
    ).encode("ascii")
    expected_snapshot = hashlib.sha256(stable_key).hexdigest()
    if expected_snapshot != snapshot_id:
        raise ArchiveValidationError(
            "SNAPSHOT_ID_MISMATCH", "Snapshot id is not derived from the envelope"
        )
    if snapshot_header != snapshot_id or sha_header != transcript_sha256:
        raise ArchiveValidationError(
            "HEADER_MISMATCH", "Archive headers do not match the envelope"
        )

    return ArchiveEnvelope(
        snapshot_id=snapshot_id,
        captured_at=captured_at,
        session_digest=session_digest,
        turn_digest=turn_digest,
        trigger=trigger,
        model=model,
        cwd=cwd,
        transcript_sha256=transcript_sha256,
        transcript_bytes=byte_count,
        transcript=transcript,
    )
