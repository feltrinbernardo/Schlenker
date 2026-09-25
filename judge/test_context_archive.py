from __future__ import annotations

import gzip
import importlib.util
import json
import sqlite3
import tempfile
import tomllib
import unittest
from contextlib import closing
from pathlib import Path
from unittest.mock import patch


ROOT = Path(__file__).resolve().parents[1]
HOOK_PATH = ROOT / ".codex" / "hooks" / "context_archive.py"


def load_hook_module():
    spec = importlib.util.spec_from_file_location(
        "schlenker_context_archive_hook", HOOK_PATH
    )
    if spec is None or spec.loader is None:
        raise RuntimeError("Unable to load context archive hook")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class ContextArchiveHookTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.hook = load_hook_module()

    @staticmethod
    def payload(transcript: Path, session: str = "session-test"):
        return {
            "hook_event_name": "PreCompact",
            "session_id": session,
            "turn_id": "turn-test",
            "cwd": str(ROOT),
            "model": "test-model",
            "trigger": "auto",
            "transcript_path": str(transcript),
        }

    @staticmethod
    def row(database: Path):
        with closing(sqlite3.connect(database)) as connection:
            return connection.execute(
                """
                SELECT transcript_sha256, transcript_bytes, transcript_gzip,
                       remote_status, remote_receipt, remote_error_code
                FROM context_snapshots
                """
            ).fetchone()

    def test_local_archive_preserves_exact_transcript_bytes(self):
        content = b'{"role":"user","content":"complete context"}\n\x00binary-tail'
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            transcript = root / "rollout.jsonl"
            database = root / "archive.sqlite3"
            transcript.write_bytes(content)

            snapshot = self.hook.archive_precompact(
                self.payload(transcript),
                {self.hook.MODE_ENV: "local"},
                database,
            )
            row = self.row(database)

        self.assertEqual(row[0], snapshot["transcript_sha256"])
        self.assertEqual(row[1], len(content))
        self.assertEqual(gzip.decompress(row[2]), content)
        self.assertEqual(row[3], "local-only")

    def test_required_remote_failure_blocks_after_local_archive(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            transcript = root / "rollout.jsonl"
            database = root / "archive.sqlite3"
            transcript.write_text("sensitive-context", encoding="utf-8")

            with self.assertRaises(self.hook.ArchiveError) as raised:
                self.hook.archive_precompact(
                    self.payload(transcript),
                    {self.hook.MODE_ENV: "required-http"},
                    database,
                )
            row = self.row(database)

        self.assertEqual(raised.exception.reason_code, "REMOTE_URL_MISSING")
        self.assertEqual(row[3], "failed")
        self.assertEqual(row[5], "REMOTE_URL_MISSING")

    def test_mirror_failure_is_recorded_without_blocking(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            transcript = root / "rollout.jsonl"
            database = root / "archive.sqlite3"
            transcript.write_text("context", encoding="utf-8")

            snapshot = self.hook.archive_precompact(
                self.payload(transcript),
                {self.hook.MODE_ENV: "mirror"},
                database,
            )
            row = self.row(database)

        self.assertTrue(snapshot["snapshot_id"])
        self.assertEqual(row[3], "failed")
        self.assertEqual(row[5], "REMOTE_URL_MISSING")

    def test_remote_success_records_bounded_receipt(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            transcript = root / "rollout.jsonl"
            database = root / "archive.sqlite3"
            transcript.write_text("context", encoding="utf-8")
            environment = {
                self.hook.MODE_ENV: "required-http",
                self.hook.URL_ENV: "https://archive.example.test/v1/snapshots",
            }

            with patch.object(self.hook, "send_remote", return_value="receipt-1"):
                self.hook.archive_precompact(
                    self.payload(transcript), environment, database
                )
            row = self.row(database)

        self.assertEqual(row[3], "delivered")
        self.assertEqual(row[4], "receipt-1")
        self.assertIsNone(row[5])

    def test_resume_context_contains_receipt_not_transcript(self):
        secret = "raw-context-must-not-be-in-hook-output"
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            transcript = root / "rollout.jsonl"
            database = root / "archive.sqlite3"
            transcript.write_text(secret, encoding="utf-8")
            self.hook.archive_precompact(
                self.payload(transcript),
                {self.hook.MODE_ENV: "local"},
                database,
            )
            output = self.hook.resume_output(
                {"session_id": "session-test"}, database
            )

        context = output["hookSpecificOutput"]["additionalContext"]
        self.assertIn("CONTEXT_ARCHIVE_RECEIPT", context)
        self.assertNotIn(secret, context)

    def test_remote_url_requires_https_except_loopback(self):
        with self.assertRaises(self.hook.ArchiveError) as raised:
            self.hook.validate_remote_url("http://archive.example.test/v1")
        self.assertEqual(raised.exception.reason_code, "REMOTE_URL_NOT_HTTPS")
        self.assertEqual(
            self.hook.validate_remote_url("http://127.0.0.1:8080/test"),
            "http://127.0.0.1:8080/test",
        )

    def test_hook_configuration_archives_before_manual_and_auto_compaction(self):
        config = json.loads((ROOT / ".codex" / "hooks.json").read_text("utf-8"))
        precompact = config["hooks"]["PreCompact"][0]
        self.assertEqual(precompact["matcher"], "^(manual|auto)$")
        handler = precompact["hooks"][0]
        self.assertNotIn("async", handler)
        self.assertIn("context_archive.py", handler["command"])

        session_start = config["hooks"]["SessionStart"][0]
        self.assertEqual(session_start["matcher"], "^compact$")
        self.assertIn("--resume-context", session_start["hooks"][0]["command"])

        with (ROOT / ".codex" / "config.toml").open("rb") as handle:
            codex_config = tomllib.load(handle)
        self.assertEqual(
            codex_config["model_auto_compact_token_limit_scope"], "total"
        )

    def test_verify_output_never_exposes_token(self):
        token = "super-secret-token"
        output = self.hook.verify_output(
            {
                self.hook.MODE_ENV: "required-http",
                self.hook.URL_ENV: "https://archive.example.test/v1/snapshots",
                self.hook.TOKEN_ENV: token,
            }
        )
        self.assertTrue(output["token_configured"])
        self.assertNotIn(token, json.dumps(output))


if __name__ == "__main__":
    unittest.main()
