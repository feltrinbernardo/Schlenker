#!/usr/bin/env python3
"""Archive the exact Codex transcript bytes immediately before compaction.

Codex supplies ``transcript_path`` as a convenience interface whose internal
format is not stable. This hook preserves the bytes without parsing them. It
never prints transcript content or authentication material.
"""

from __future__ import annotations

import argparse
import base64
import gzip
import hashlib
import json
import os
import sqlite3
import sys
import urllib.error
import urllib.parse
import urllib.request
from contextlib import closing
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Mapping


HOOK_FILE = Path(__file__).resolve()
REPOSITORY_ROOT = HOOK_FILE.parents[2]
ARCHIVE_ROOT = REPOSITORY_ROOT / "logs" / "context-archive"
DATABASE_PATH = ARCHIVE_ROOT / "context-archive.sqlite3"

MODE_ENV = "SCHLENKER_CONTEXT_ARCHIVE_MODE"
URL_ENV = "SCHLENKER_CONTEXT_ARCHIVE_URL"
TOKEN_ENV = "SCHLENKER_CONTEXT_ARCHIVE_TOKEN"
TIMEOUT_ENV = "SCHLENKER_CONTEXT_ARCHIVE_TIMEOUT_SECONDS"
MAX_BYTES_ENV = "SCHLENKER_CONTEXT_ARCHIVE_MAX_BYTES"

VALID_MODES = {"local", "mirror", "required-http"}
DEFAULT_TIMEOUT_SECONDS = 15.0
DEFAULT_MAX_BYTES = 100 * 1024 * 1024


class ArchiveError(RuntimeError):
    """Expected archive failure with a stable reason code."""

    def __init__(self, reason_code: str, message: str):
        super().__init__(message)
        self.reason_code = reason_code


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):  # noqa: ANN001
        raise ArchiveError("REMOTE_REDIRECT_REJECTED", f"HTTP redirect {code} rejected")


def utc_now() -> str:
    return datetime.now(timezone.utc).isoformat()


def digest_text(value: str) -> str:
    return hashlib.sha256(value.encode("utf-8", errors="replace")).hexdigest()


def normalized_mode(environment: Mapping[str, str]) -> str:
    mode = environment.get(MODE_ENV, "local").strip().lower()
    if mode not in VALID_MODES:
        raise ArchiveError(
            "INVALID_ARCHIVE_MODE",
            f"{MODE_ENV} must be one of: {', '.join(sorted(VALID_MODES))}",
        )
    return mode


def numeric_environment(
    environment: Mapping[str, str], name: str, default: float
) -> float:
    raw = environment.get(name, "").strip()
    if not raw:
        return default
    try:
        value = float(raw)
    except ValueError as error:
        raise ArchiveError("INVALID_NUMERIC_SETTING", f"{name} must be numeric") from error
    if value <= 0:
        raise ArchiveError("INVALID_NUMERIC_SETTING", f"{name} must be positive")
    return value


def validate_remote_url(raw_url: str) -> str:
    if not raw_url:
        raise ArchiveError("REMOTE_URL_MISSING", f"{URL_ENV} is required")
    parsed = urllib.parse.urlparse(raw_url)
    hostname = (parsed.hostname or "").lower()
    local_http = parsed.scheme == "http" and hostname in {
        "localhost",
        "127.0.0.1",
        "::1",
    }
    if parsed.scheme != "https" and not local_http:
        raise ArchiveError(
            "REMOTE_URL_NOT_HTTPS",
            "Remote archive URL must use HTTPS (localhost HTTP is test-only)",
        )
    if not parsed.netloc or parsed.username or parsed.password:
        raise ArchiveError(
            "REMOTE_URL_INVALID",
            "Remote archive URL must have a host and must not embed credentials",
        )
    return raw_url


def connect_database(path: Path = DATABASE_PATH) -> sqlite3.Connection:
    path.parent.mkdir(parents=True, exist_ok=True)
    connection = sqlite3.connect(path)
    connection.execute("PRAGMA journal_mode=WAL")
    connection.execute("PRAGMA synchronous=FULL")
    connection.executescript(
        """
        CREATE TABLE IF NOT EXISTS context_snapshots (
            snapshot_id TEXT PRIMARY KEY,
            captured_at TEXT NOT NULL,
            session_digest TEXT NOT NULL,
            turn_digest TEXT NOT NULL,
            trigger TEXT NOT NULL,
            model TEXT NOT NULL,
            cwd TEXT NOT NULL,
            transcript_sha256 TEXT NOT NULL,
            transcript_bytes INTEGER NOT NULL,
            transcript_gzip BLOB NOT NULL,
            remote_status TEXT NOT NULL,
            remote_receipt TEXT,
            remote_error_code TEXT
        );
        CREATE INDEX IF NOT EXISTS idx_context_snapshots_session_time
        ON context_snapshots(session_digest, captured_at DESC);
        """
    )
    return connection


def read_transcript(payload: Mapping[str, Any], max_bytes: int) -> tuple[Path, bytes]:
    raw_path = payload.get("transcript_path")
    if not isinstance(raw_path, str) or not raw_path.strip():
        raise ArchiveError(
            "TRANSCRIPT_PATH_MISSING",
            "Codex did not provide a transcript_path for PreCompact",
        )
    path = Path(raw_path).resolve(strict=False)
    if not path.is_file():
        raise ArchiveError("TRANSCRIPT_NOT_FOUND", "Transcript file is unavailable")
    before = path.stat()
    size = before.st_size
    if size > max_bytes:
        raise ArchiveError(
            "TRANSCRIPT_TOO_LARGE",
            f"Transcript has {size} bytes; configured maximum is {max_bytes}",
        )
    try:
        content = path.read_bytes()
    except OSError as error:
        raise ArchiveError("TRANSCRIPT_READ_FAILED", str(error)) from error
    after = path.stat()
    if (
        len(content) != size
        or after.st_size != before.st_size
        or after.st_mtime_ns != before.st_mtime_ns
    ):
        raise ArchiveError(
            "TRANSCRIPT_CHANGED_DURING_READ",
            "Transcript size changed while it was being archived",
        )
    return path, content


def make_snapshot(payload: Mapping[str, Any], transcript: bytes) -> dict[str, Any]:
    session_digest = digest_text(str(payload.get("session_id", "unknown")))
    turn_digest = digest_text(str(payload.get("turn_id", "unknown")))
    transcript_sha256 = hashlib.sha256(transcript).hexdigest()
    trigger = str(payload.get("trigger", "unknown"))
    stable_key = "|".join(
        (session_digest, turn_digest, trigger, transcript_sha256)
    )
    return {
        "schema_version": 1,
        "snapshot_id": hashlib.sha256(stable_key.encode("ascii")).hexdigest(),
        "captured_at": utc_now(),
        "session_digest": session_digest,
        "turn_digest": turn_digest,
        "trigger": trigger,
        "model": str(payload.get("model", "unknown")),
        "cwd": str(payload.get("cwd", "")),
        "transcript_sha256": transcript_sha256,
        "transcript_bytes": len(transcript),
        "transcript_base64": base64.b64encode(transcript).decode("ascii"),
    }


def store_local(
    snapshot: Mapping[str, Any], transcript: bytes, path: Path = DATABASE_PATH
) -> None:
    compressed = gzip.compress(transcript, compresslevel=6, mtime=0)
    try:
        with closing(connect_database(path)) as connection:
            with connection:
                connection.execute(
                    """
                    INSERT INTO context_snapshots (
                        snapshot_id, captured_at, session_digest, turn_digest,
                        trigger, model, cwd, transcript_sha256, transcript_bytes,
                        transcript_gzip, remote_status, remote_receipt,
                        remote_error_code
                    ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, NULL, NULL)
                    ON CONFLICT(snapshot_id) DO UPDATE SET
                        captured_at = excluded.captured_at,
                        transcript_gzip = excluded.transcript_gzip,
                        remote_status = excluded.remote_status,
                        remote_receipt = NULL,
                        remote_error_code = NULL
                    """,
                    (
                        snapshot["snapshot_id"],
                        snapshot["captured_at"],
                        snapshot["session_digest"],
                        snapshot["turn_digest"],
                        snapshot["trigger"],
                        snapshot["model"],
                        snapshot["cwd"],
                        snapshot["transcript_sha256"],
                        snapshot["transcript_bytes"],
                        compressed,
                        "local-only",
                    ),
                )
    except sqlite3.Error as error:
        raise ArchiveError("LOCAL_ARCHIVE_FAILED", str(error)) from error


def update_remote_status(
    snapshot_id: str,
    status: str,
    receipt: str | None = None,
    error_code: str | None = None,
    path: Path = DATABASE_PATH,
) -> None:
    try:
        with closing(connect_database(path)) as connection:
            with connection:
                connection.execute(
                    """
                    UPDATE context_snapshots
                    SET remote_status = ?, remote_receipt = ?, remote_error_code = ?
                    WHERE snapshot_id = ?
                    """,
                    (status, receipt, error_code, snapshot_id),
                )
    except sqlite3.Error as error:
        raise ArchiveError("LOCAL_STATUS_UPDATE_FAILED", str(error)) from error


def send_remote(
    snapshot: Mapping[str, Any], environment: Mapping[str, str]
) -> str:
    url = validate_remote_url(environment.get(URL_ENV, "").strip())
    timeout = numeric_environment(
        environment, TIMEOUT_ENV, DEFAULT_TIMEOUT_SECONDS
    )
    encoded = json.dumps(snapshot, separators=(",", ":")).encode("utf-8")
    body = gzip.compress(encoded, compresslevel=6, mtime=0)
    headers = {
        "Content-Type": "application/json",
        "Content-Encoding": "gzip",
        "Accept": "application/json, text/plain",
        "X-Context-Snapshot-Id": str(snapshot["snapshot_id"]),
        "X-Context-Transcript-SHA256": str(snapshot["transcript_sha256"]),
    }
    token = environment.get(TOKEN_ENV, "").strip()
    if token:
        headers["Authorization"] = f"Bearer {token}"
    request = urllib.request.Request(url, data=body, headers=headers, method="POST")
    opener = urllib.request.build_opener(NoRedirect())
    try:
        with opener.open(request, timeout=timeout) as response:
            status = int(response.status)
            receipt_bytes = response.read(4097)
    except ArchiveError:
        raise
    except urllib.error.HTTPError as error:
        raise ArchiveError(
            "REMOTE_ARCHIVE_REJECTED", f"Remote returned HTTP {error.code}"
        ) from error
    except (urllib.error.URLError, TimeoutError, OSError) as error:
        raise ArchiveError("REMOTE_ARCHIVE_FAILED", str(error)) from error
    if not 200 <= status < 300:
        raise ArchiveError("REMOTE_ARCHIVE_REJECTED", f"Remote returned HTTP {status}")
    if len(receipt_bytes) > 4096:
        raise ArchiveError("REMOTE_RECEIPT_TOO_LARGE", "Remote receipt exceeds 4096 bytes")
    return receipt_bytes.decode("utf-8", errors="replace").strip() or f"HTTP {status}"


def archive_precompact(
    payload: Mapping[str, Any],
    environment: Mapping[str, str] | None = None,
    database_path: Path = DATABASE_PATH,
) -> dict[str, Any]:
    environment = os.environ if environment is None else environment
    mode = normalized_mode(environment)
    max_bytes = int(
        numeric_environment(environment, MAX_BYTES_ENV, DEFAULT_MAX_BYTES)
    )
    _, transcript = read_transcript(payload, max_bytes)
    snapshot = make_snapshot(payload, transcript)
    store_local(snapshot, transcript, database_path)

    if mode == "local":
        return snapshot
    try:
        receipt = send_remote(snapshot, environment)
        update_remote_status(
            str(snapshot["snapshot_id"]),
            "delivered",
            receipt=receipt,
            path=database_path,
        )
    except ArchiveError as error:
        update_remote_status(
            str(snapshot["snapshot_id"]),
            "failed",
            error_code=error.reason_code,
            path=database_path,
        )
        if mode == "required-http":
            raise
    return snapshot


def latest_receipt(
    session_id: str, database_path: Path = DATABASE_PATH
) -> dict[str, Any] | None:
    if not database_path.exists():
        return None
    session_digest = digest_text(session_id or "unknown")
    try:
        with closing(connect_database(database_path)) as connection:
            row = connection.execute(
                """
                SELECT snapshot_id, captured_at, transcript_sha256,
                       transcript_bytes, remote_status
                FROM context_snapshots
                WHERE session_digest = ?
                ORDER BY captured_at DESC
                LIMIT 1
                """,
                (session_digest,),
            ).fetchone()
    except sqlite3.Error as error:
        raise ArchiveError("LOCAL_RECEIPT_READ_FAILED", str(error)) from error
    if row is None:
        return None
    return {
        "snapshot_id": row[0],
        "captured_at": row[1],
        "transcript_sha256": row[2],
        "transcript_bytes": row[3],
        "remote_status": row[4],
    }


def block_output(error: ArchiveError) -> dict[str, Any]:
    reason = f"{error.reason_code}: context archive failed before compaction: {error}"
    return {
        "continue": False,
        "stopReason": reason,
        "systemMessage": reason,
    }


def resume_output(
    payload: Mapping[str, Any], database_path: Path = DATABASE_PATH
) -> dict[str, Any] | None:
    receipt = latest_receipt(
        str(payload.get("session_id", "unknown")), database_path
    )
    if receipt is None:
        return None
    short_id = str(receipt["snapshot_id"])[:16]
    short_hash = str(receipt["transcript_sha256"])[:16]
    context = (
        "CONTEXT_ARCHIVE_RECEIPT: pre-compaction transcript preserved; "
        f"snapshot={short_id}, sha256={short_hash}, "
        f"bytes={receipt['transcript_bytes']}, "
        f"remote={receipt['remote_status']}."
    )
    return {
        "hookSpecificOutput": {
            "hookEventName": "SessionStart",
            "additionalContext": context,
        }
    }


def verify_output(environment: Mapping[str, str]) -> dict[str, Any]:
    mode = normalized_mode(environment)
    remote_url = environment.get(URL_ENV, "").strip()
    if mode in {"mirror", "required-http"}:
        validate_remote_url(remote_url)
    return {
        "status": "PASS",
        "mode": mode,
        "database": str(DATABASE_PATH),
        "database_exists": DATABASE_PATH.exists(),
        "remote_configured": bool(remote_url),
        "token_configured": bool(environment.get(TOKEN_ENV, "").strip()),
        "raw_transcript_output": False,
    }


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--verify", action="store_true")
    parser.add_argument("--resume-context", action="store_true")
    args = parser.parse_args(argv)

    if args.verify:
        try:
            result = verify_output(os.environ)
        except ArchiveError as error:
            result = {"status": "FAIL", "reason_code": error.reason_code, "error": str(error)}
        sys.stdout.write(json.dumps(result, separators=(",", ":")))
        return 0 if result["status"] == "PASS" else 1

    try:
        payload = json.load(sys.stdin)
        if not isinstance(payload, dict):
            raise ArchiveError("HOOK_INPUT_INVALID", "Hook input must be a JSON object")
        if args.resume_context:
            result = resume_output(payload)
        elif payload.get("hook_event_name") == "PreCompact":
            archive_precompact(payload)
            result = None
        else:
            raise ArchiveError(
                "HOOK_EVENT_UNSUPPORTED",
                "Expected PreCompact or --resume-context SessionStart",
            )
    except (json.JSONDecodeError, OSError) as error:
        result = block_output(ArchiveError("HOOK_INPUT_INVALID", str(error)))
    except ArchiveError as error:
        result = block_output(error)

    if result is not None:
        sys.stdout.write(json.dumps(result, separators=(",", ":")))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
