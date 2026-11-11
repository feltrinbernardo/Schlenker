"""FastAPI entry point for authenticated context snapshot ingestion."""

from __future__ import annotations

import asyncio
import hmac
from contextlib import asynccontextmanager

from fastapi import FastAPI, Request
from fastapi.responses import JSONResponse

from .config import ConfigurationError, Settings
from .crypto import seal_archive
from .protocol import ArchiveValidationError, decompress_gzip_limited, parse_envelope
from .store import ArchiveStore, PersistenceError, SnapshotConflict


def _error(status_code: int, reason_code: str, message: str) -> JSONResponse:
    return JSONResponse(
        status_code=status_code,
        content={"status": "error", "reason_code": reason_code, "message": message},
    )


@asynccontextmanager
async def lifespan(application: FastAPI):
    settings = Settings.from_environment()
    store = ArchiveStore(settings)
    try:
        await asyncio.to_thread(store.bootstrap)
        application.state.settings = settings
        application.state.store = store
        yield
    finally:
        await asyncio.to_thread(store.close)


app = FastAPI(
    title="Schlenker Context Archive",
    version="1.0.0",
    docs_url=None,
    redoc_url=None,
    openapi_url=None,
    lifespan=lifespan,
)


@app.exception_handler(ConfigurationError)
async def configuration_error(_request: Request, _error_value: ConfigurationError):
    return _error(503, "SERVICE_NOT_CONFIGURED", "Service configuration is invalid")


@app.get("/healthz")
async def healthz():
    return {"status": "ok"}


@app.get("/readyz")
async def readyz(request: Request):
    try:
        await asyncio.to_thread(request.app.state.store.ping)
    except PersistenceError:
        return _error(503, "DEPENDENCY_UNAVAILABLE", "Storage dependency is unavailable")
    return {"status": "ready"}


@app.post("/v1/snapshots")
async def ingest_snapshot(request: Request):
    settings: Settings = request.app.state.settings
    authorization = request.headers.get("authorization", "")
    expected = f"Bearer {settings.archive_token}"
    if not hmac.compare_digest(authorization, expected):
        return _error(401, "UNAUTHORIZED", "Bearer token is invalid")
    if request.headers.get("content-encoding", "").lower() != "gzip":
        return _error(415, "GZIP_REQUIRED", "Content-Encoding must be gzip")
    if not request.headers.get("content-type", "").lower().startswith(
        "application/json"
    ):
        return _error(415, "JSON_REQUIRED", "Content-Type must be application/json")

    content_length = request.headers.get("content-length")
    if content_length:
        try:
            declared_length = int(content_length)
        except ValueError:
            return _error(400, "INVALID_CONTENT_LENGTH", "Content-Length is invalid")
        if declared_length > settings.max_compressed_bytes:
            return _error(413, "COMPRESSED_BODY_TOO_LARGE", "Request body exceeds limit")

    compressed = bytearray()
    async for chunk in request.stream():
        compressed.extend(chunk)
        if len(compressed) > settings.max_compressed_bytes:
            return _error(413, "COMPRESSED_BODY_TOO_LARGE", "Request body exceeds limit")

    try:
        encoded_json = decompress_gzip_limited(
            bytes(compressed), settings.max_decompressed_bytes
        )
        envelope = parse_envelope(
            encoded_json,
            request.headers.get("x-context-snapshot-id", ""),
            request.headers.get("x-context-transcript-sha256", ""),
            settings.max_transcript_bytes,
        )
        sealed = seal_archive(
            envelope, settings.encryption_key, settings.encryption_key_id
        )
        receipt = await asyncio.to_thread(
            request.app.state.store.persist, envelope, sealed
        )
    except ArchiveValidationError as error:
        return _error(error.status_code, error.reason_code, str(error))
    except SnapshotConflict:
        return _error(409, "SNAPSHOT_CONFLICT", "Snapshot id conflicts with stored data")
    except PersistenceError:
        return _error(503, "ARCHIVE_PERSISTENCE_FAILED", "Archive could not be persisted")

    return {
        "status": receipt.status,
        "snapshot_id": receipt.snapshot_id,
        "transcript_sha256": envelope.transcript_sha256,
        "transcript_bytes": envelope.transcript_bytes,
        "key_id": sealed.key_id,
    }
