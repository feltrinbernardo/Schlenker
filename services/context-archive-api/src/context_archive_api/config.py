"""Strict runtime configuration without logging secrets."""

from __future__ import annotations

import base64
import binascii
import os
from dataclasses import dataclass, field
from typing import Mapping
from urllib.parse import urlparse


class ConfigurationError(RuntimeError):
    """The service cannot start safely with the supplied environment."""


def _required(environment: Mapping[str, str], *names: str) -> str:
    for name in names:
        value = environment.get(name, "").strip()
        if value:
            return value
    raise ConfigurationError(f"Missing required setting: {' or '.join(names)}")


def _positive_integer(
    environment: Mapping[str, str], name: str, default: int
) -> int:
    raw = environment.get(name, str(default)).strip()
    try:
        value = int(raw)
    except ValueError as error:
        raise ConfigurationError(f"{name} must be an integer") from error
    if value <= 0:
        raise ConfigurationError(f"{name} must be positive")
    return value


@dataclass(frozen=True)
class Settings:
    database_url: str = field(repr=False)
    archive_token: str = field(repr=False)
    encryption_key: bytes = field(repr=False)
    encryption_key_id: str
    s3_bucket: str
    s3_endpoint_url: str
    s3_region: str
    s3_access_key_id: str = field(repr=False)
    s3_secret_access_key: str = field(repr=False)
    object_prefix: str
    max_compressed_bytes: int
    max_decompressed_bytes: int
    max_transcript_bytes: int

    @classmethod
    def from_environment(
        cls, environment: Mapping[str, str] | None = None
    ) -> "Settings":
        environment = os.environ if environment is None else environment
        database_url = _required(environment, "DATABASE_URL")
        if urlparse(database_url).scheme not in {"postgres", "postgresql"}:
            raise ConfigurationError("DATABASE_URL must be a PostgreSQL URL")

        archive_token = _required(environment, "CONTEXT_ARCHIVE_TOKEN")
        if len(archive_token) < 32:
            raise ConfigurationError(
                "CONTEXT_ARCHIVE_TOKEN must contain at least 32 characters"
            )

        endpoint_url = _required(
            environment, "ARCHIVE_S3_ENDPOINT_URL", "ENDPOINT"
        )
        parsed_endpoint = urlparse(endpoint_url)
        if parsed_endpoint.scheme != "https" or not parsed_endpoint.netloc:
            raise ConfigurationError("The object-storage endpoint must use HTTPS")

        encoded_key = _required(environment, "CONTEXT_ARCHIVE_ENCRYPTION_KEY")
        try:
            encryption_key = base64.b64decode(encoded_key, validate=True)
        except (binascii.Error, ValueError) as error:
            raise ConfigurationError(
                "CONTEXT_ARCHIVE_ENCRYPTION_KEY must be valid base64"
            ) from error
        if len(encryption_key) != 32:
            raise ConfigurationError(
                "CONTEXT_ARCHIVE_ENCRYPTION_KEY must decode to exactly 32 bytes"
            )

        key_id = _required(environment, "CONTEXT_ARCHIVE_KEY_ID")
        if len(key_id) > 64 or not all(
            character.isalnum() or character in "-_." for character in key_id
        ):
            raise ConfigurationError(
                "CONTEXT_ARCHIVE_KEY_ID must be 1-64 safe identifier characters"
            )

        prefix = environment.get(
            "CONTEXT_ARCHIVE_OBJECT_PREFIX", "context-archive/v1"
        ).strip(" /")
        if not prefix or ".." in prefix:
            raise ConfigurationError(
                "CONTEXT_ARCHIVE_OBJECT_PREFIX must be a non-empty safe prefix"
            )

        return cls(
            database_url=database_url,
            archive_token=archive_token,
            encryption_key=encryption_key,
            encryption_key_id=key_id,
            s3_bucket=_required(environment, "ARCHIVE_S3_BUCKET", "BUCKET"),
            s3_endpoint_url=endpoint_url,
            s3_region=environment.get(
                "ARCHIVE_S3_REGION", environment.get("REGION", "auto")
            ).strip()
            or "auto",
            s3_access_key_id=_required(
                environment, "ARCHIVE_S3_ACCESS_KEY_ID", "ACCESS_KEY_ID"
            ),
            s3_secret_access_key=_required(
                environment, "ARCHIVE_S3_SECRET_ACCESS_KEY", "SECRET_ACCESS_KEY"
            ),
            object_prefix=prefix,
            max_compressed_bytes=_positive_integer(
                environment, "CONTEXT_ARCHIVE_MAX_COMPRESSED_BYTES", 110 * 1024 * 1024
            ),
            max_decompressed_bytes=_positive_integer(
                environment,
                "CONTEXT_ARCHIVE_MAX_DECOMPRESSED_BYTES",
                140 * 1024 * 1024,
            ),
            max_transcript_bytes=_positive_integer(
                environment, "CONTEXT_ARCHIVE_MAX_TRANSCRIPT_BYTES", 100 * 1024 * 1024
            ),
        )
