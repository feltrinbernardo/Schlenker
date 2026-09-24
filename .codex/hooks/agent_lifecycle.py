#!/usr/bin/env python3
"""Deterministic Codex lifecycle guardrails for the Schlenker repository.

The hook validates only facts available in the hook payload. It is deliberately
not an authorization engine and never launches an engineering application,
captures a screen, calls a model, or accesses a controller.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import sys
import time
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


HOOK_FILE = Path(__file__).resolve()
REPOSITORY_ROOT = HOOK_FILE.parents[2]
LOG_ROOT = REPOSITORY_ROOT / "logs" / "hooks"

PROTECTED_ORIGINAL = "d:/gx works/schlenker.gxw"
NATIVE_PROJECT_SUFFIXES = {
    ".gxw",
    ".gx3",
    ".ap19",
    ".zap19",
}
POLICY_SURFACE_PREFIXES = (
    ".codex/hooks",
    "agent/",
    "evals/cases/002-",
    "docs/agent-foundation/",
)
POLICY_SURFACE_FILES = {
    ".codex/hooks.json",
    "AGENTS.md",
    "scripts/test-prompt-judge.ps1",
    "scripts/test-repository.ps1",
}
HOOK_MATCHER = r"^(Bash|apply_patch|mcp__cua_repl__js)$"
HOOK_TIMEOUT_SECONDS = 5

MUTATION_VERB_PATTERN = re.compile(
    r"(?ix)"
    r"\b(remove-item|move-item|copy-item|set-content|add-content|out-file|"
    r"clear-content|rename-item|del|erase|rmdir|rm|mv|cp)\b|"
    r"\[\s*(?:system\.)?io\.file\s*\]::(?:write|delete|move|copy)|"
    r"(?:^|\s)(?:>>|>)(?:\s|$)"
)
PATH_WRITE_PATTERN_TEMPLATE = (
    r"(?ix)(?:\b(?:remove-item|move-item|set-content|add-content|out-file|"
    r"clear-content|rename-item|del|erase|rmdir|rm|mv)\b[^\r\n]*{path}|"
    r"(?:>>|>)\s*['\"]?{path})"
)
PATH_TOKEN_PATTERN = re.compile(
    r"(?:'([^']+)'|\"([^\"]+)\"|"
    r"((?:[a-zA-Z]:[\\/]|\.\.[\\/]|/)[^\s;|]+))"
)
ENGINEERING_LAUNCH_PATTERN = re.compile(
    r"(?ix)"
    r"(?:start-process|\bcmd(?:\.exe)?\s+/c\s+start|"
    r"(?:^|[;&|])\s*&?\s*['\"]?[^\r\n;&|]*?)"
    r"(?:gd2\.exe|siemens\.automation\.portal[^\s'\"]*\.exe)"
)
BROAD_DESTRUCTIVE_PATTERNS = (
    re.compile(r"(?i)\brm\s+-rf\s+(?:/|~|\$home)(?:\s|$)"),
    re.compile(
        r"(?i)\bremove-item\b[^\r\n]*\b-recurse\b[^\r\n]*"
        r"(?:\$home|~|['\"]?[a-z]:\\?['\"]?)(?:\s|$)"
    ),
)
MATERIAL_OR_CRITICAL_PATTERN = re.compile(
    r"(?ix)\b("
    r"select(?:ing)?\s+(?:the\s+)?(?:project|target)|"
    r"online\s+mode|read\s+from\s+plc|"
    r"transfer|write\s+to\s+plc|remote\s+operation|"
    r"plc\s+(?:run|stop|reset)|connection\s+change|"
    r"device[- ]memory"
    r")\b"
)
ACTION_PATTERN = re.compile(
    r"(?i)\.(?:click|double_?click|type|keypress|press|drag|scroll)\s*\("
)
PATCH_PATH_PATTERN = re.compile(
    r"(?m)^\*\*\* (?:Add|Update|Delete) File: (.+?)\s*$|"
    r"^\*\*\* Move to: (.+?)\s*$"
)


def utc_now() -> str:
    return datetime.now(timezone.utc).isoformat()


def _safe_id(value: str) -> str:
    return hashlib.sha256(value.encode("utf-8", errors="replace")).hexdigest()[:20]


def _json_digest(value: Any) -> str:
    encoded = json.dumps(value, sort_keys=True, default=str).encode("utf-8")
    return hashlib.sha256(encoded).hexdigest()


def _normalize_text(value: Any) -> str:
    if isinstance(value, str):
        return value
    return json.dumps(value, sort_keys=True, default=str)


def _normalized_slashes(value: str) -> str:
    return value.replace("\\", "/").lower()


def _command_writes_path(command: str, normalized_path_pattern: str) -> bool:
    normalized = _normalized_slashes(command)
    pattern = PATH_WRITE_PATTERN_TEMPLATE.format(path=normalized_path_pattern)
    return re.search(pattern, normalized) is not None


def _command_writes_native_project(command: str) -> bool:
    suffix_pattern = r"[^\s'\"]+\.(?:gxw|gx3|ap19|zap19)"
    return _command_writes_path(command, suffix_pattern)


def _recognized_write_targets(command: str, cwd: str) -> list[Path]:
    targets: list[Path] = []
    for segment in re.split(r"[;|\r\n]+", command):
        if not MUTATION_VERB_PATTERN.search(segment):
            continue
        for match in PATH_TOKEN_PATTERN.finditer(segment):
            raw = next((value for value in match.groups() if value), None)
            if raw and not raw.startswith("-"):
                targets.append(_resolve_hook_path(raw, cwd))
    return targets


def _within_repository(path: Path) -> bool:
    try:
        path.resolve(strict=False).relative_to(REPOSITORY_ROOT.resolve())
        return True
    except ValueError:
        return False


def _resolve_hook_path(raw_path: str, cwd: str) -> Path:
    cleaned = raw_path.strip().strip("'\"")
    candidate = Path(cleaned)
    if not candidate.is_absolute():
        candidate = Path(cwd) / candidate
    return candidate.resolve(strict=False)


def extract_patch_paths(command: str) -> list[str]:
    paths: list[str] = []
    for match in PATCH_PATH_PATTERN.finditer(command):
        path = match.group(1) or match.group(2)
        if path:
            paths.append(path.strip())
    return paths


def _repository_relative(path: Path) -> str:
    try:
        return path.resolve(strict=False).relative_to(
            REPOSITORY_ROOT.resolve()
        ).as_posix()
    except ValueError:
        return str(path)


def _deny(reason_code: str, message: str) -> dict[str, Any]:
    return {
        "hookSpecificOutput": {
            "hookEventName": "PreToolUse",
            "permissionDecision": "deny",
            "permissionDecisionReason": f"{reason_code}: {message}",
        }
    }


def _permission_deny(reason_code: str, message: str) -> dict[str, Any]:
    return {
        "hookSpecificOutput": {
            "hookEventName": "PermissionRequest",
            "decision": {
                "behavior": "deny",
                "message": f"{reason_code}: {message}",
            },
        }
    }


def _permission_allow() -> dict[str, Any]:
    return {
        "hookSpecificOutput": {
            "hookEventName": "PermissionRequest",
            "decision": {
                "behavior": "allow",
            },
        }
    }


def _context(event: str, message: str) -> dict[str, Any]:
    return {
        "hookSpecificOutput": {
            "hookEventName": event,
            "additionalContext": message,
        }
    }


def _post_block(reason_code: str, message: str) -> dict[str, Any]:
    return {
        "decision": "block",
        "reason": f"{reason_code}: {message}",
        "hookSpecificOutput": {
            "hookEventName": "PostToolUse",
            "additionalContext": (
                "The tool has already run. Stop, preserve evidence, and "
                f"reconcile before continuing. {reason_code}: {message}"
            ),
        },
    }


def _tool_name(payload: dict[str, Any]) -> str:
    return str(payload.get("tool_name", ""))


def _tool_input(payload: dict[str, Any]) -> Any:
    return payload.get("tool_input", {})


def _command(payload: dict[str, Any]) -> str:
    tool_input = _tool_input(payload)
    if isinstance(tool_input, dict):
        return _normalize_text(tool_input.get("command", tool_input.get("code", "")))
    return _normalize_text(tool_input)


def _cwd_is_safe(payload: dict[str, Any]) -> bool:
    cwd = Path(str(payload.get("cwd", REPOSITORY_ROOT)))
    return _within_repository(cwd)


def _validate_patch(payload: dict[str, Any]) -> dict[str, Any] | None:
    command = _command(payload)
    cwd = str(payload.get("cwd", REPOSITORY_ROOT))
    for raw_path in extract_patch_paths(command):
        normalized = _normalized_slashes(raw_path)
        if PROTECTED_ORIGINAL in normalized:
            return _deny(
                "PROTECTED_SOURCE_WRITE",
                "The protected GX Works2 source must remain immutable.",
            )
        path = _resolve_hook_path(raw_path, cwd)
        if not _within_repository(path):
            return _deny(
                "PATH_OUTSIDE_REPOSITORY",
                f"Patch target is outside the repository: {raw_path}",
            )
        if path.suffix.lower() in NATIVE_PROJECT_SUFFIXES:
            return _deny(
                "NATIVE_PROJECT_PATCH_FORBIDDEN",
                f"Native engineering projects cannot be patched directly: {raw_path}",
            )
        relative = _repository_relative(path).lower()
        if relative.startswith("logs/change-log/"):
            return _deny(
                "APPEND_ONLY_LOG",
                "Use scripts/write-change-log.ps1 for change-log entries.",
            )
    return None


def _validate_bash(payload: dict[str, Any]) -> dict[str, Any] | None:
    command = _command(payload)
    normalized = _normalized_slashes(command)
    cwd = str(payload.get("cwd", REPOSITORY_ROOT))

    if any(pattern.search(command) for pattern in BROAD_DESTRUCTIVE_PATTERNS):
        return _deny(
            "BROAD_DESTRUCTIVE_COMMAND",
            "Broad recursive deletion of a root or home path is prohibited.",
        )
    if ENGINEERING_LAUNCH_PATTERN.search(command):
        return _deny(
            "ENGINEERING_APP_SHELL_LAUNCH",
            "Engineering applications must be selected through approved Computer Use discovery.",
        )
    protected_pattern = re.escape(PROTECTED_ORIGINAL)
    if PROTECTED_ORIGINAL in normalized and _command_writes_path(
        command, protected_pattern
    ):
        return _deny(
            "PROTECTED_SOURCE_WRITE",
            "The protected GX Works2 source may be inspected but not mutated.",
        )
    if MUTATION_VERB_PATTERN.search(command):
        if _command_writes_native_project(command):
            return _deny(
                "NATIVE_PROJECT_SHELL_WRITE",
                "Native engineering project mutation is not an approved shell capability.",
            )
        if "logs/change-log/" in normalized or "logs/change-log\\" in command.lower():
            if "write-change-log.ps1" not in normalized:
                return _deny(
                    "APPEND_ONLY_LOG",
                    "Use scripts/write-change-log.ps1 for change-log entries.",
                )

    outside_write_targets = [
        path
        for path in _recognized_write_targets(command, cwd)
        if not _within_repository(path)
    ]
    if outside_write_targets:
        return _deny(
            "SHELL_WRITE_OUTSIDE_REPOSITORY",
            "Recognized shell write target is outside the repository: "
            f"{outside_write_targets[0]}",
        )

    capture_helper = (
        "capture-gxworks2-window.ps1" in normalized
        or "capture-tia-portal-window.ps1" in normalized
    )
    if capture_helper:
        output_directory = _named_argument(command, "OutputDirectory")
        if output_directory:
            output_path = _resolve_hook_path(
                output_directory, cwd
            )
            runs_root = (REPOSITORY_ROOT / "runs").resolve(strict=False)
            try:
                output_path.relative_to(runs_root)
            except ValueError:
                return _deny(
                    "CAPTURE_OUTPUT_OUTSIDE_RUNS",
                    "Capture output must remain beneath the repository runs directory.",
                )
        if "capture-tia-portal-window.ps1" in normalized:
            expected_project = _named_argument(command, "ExpectedProjectPath")
            if expected_project:
                project_normalized = _normalized_slashes(expected_project)
                if (
                    not project_normalized.endswith(".ap19")
                    or "-deployworking-" not in project_normalized
                ):
                    return _deny(
                        "CAPTURE_PROJECT_NOT_APPROVED",
                        "TIA capture requires an explicit DeployWorking .ap19 project.",
                    )
    if capture_helper and "-validateonly" not in normalized:
        return _context(
            "PreToolUse",
            "CAPTURE_CHECKPOINT: a real hybrid capture requires the current-run "
            "operator confirmation, exact approved foreground application/project, "
            "and output beneath runs/. This hook does not grant that authority.",
        )
    return None


def _named_argument(command: str, name: str) -> str | None:
    pattern = re.compile(
        rf"(?i)-{re.escape(name)}\s+(?:'([^']+)'|\"([^\"]+)\"|([^\s;|]+))"
    )
    match = pattern.search(command)
    if not match:
        return None
    return next((value for value in match.groups() if value is not None), None)


def _is_exact_offline_validation(command: str) -> bool:
    normalized = _normalized_slashes(command).strip()
    normalized = re.sub(r"^&\s*", "", normalized)
    return normalized in {
        "./scripts/test-prompt-judge.ps1",
        "./scripts/test-repository.ps1",
    }


def _validate_computer_use(payload: dict[str, Any]) -> dict[str, Any] | None:
    code = _command(payload)
    action_count = len(ACTION_PATTERN.findall(code))
    if action_count > 5:
        return _deny(
            "ACTION_BATCH_TOO_LARGE",
            f"Routine Computer Use batches are limited to five actions; observed {action_count}.",
        )
    if action_count > 1 and MATERIAL_OR_CRITICAL_PATTERN.search(code):
        return _deny(
            "CRITICAL_ACTION_BATCH_FORBIDDEN",
            "Material or critical operations must be performed one action at a time.",
        )
    if MATERIAL_OR_CRITICAL_PATTERN.search(code):
        return _context(
            "PreToolUse",
            "CRITICAL_CHECKPOINT: verify exact target identity, current confirmation, "
            "applicable authorization, and pre/post observation. This hook does not "
            "grant the operation.",
        )
    return None


def handle_pre_tool(payload: dict[str, Any]) -> dict[str, Any] | None:
    tool = _tool_name(payload)
    if tool in {"Bash", "apply_patch"} and not _cwd_is_safe(payload):
        return _deny(
            "CWD_OUTSIDE_REPOSITORY",
            "Repository hooks permit shell and patch operations only inside this repository.",
        )
    if tool == "apply_patch":
        return _validate_patch(payload)
    if tool == "Bash":
        return _validate_bash(payload)
    if tool == "mcp__cua_repl__js":
        return _validate_computer_use(payload)
    return None


def handle_permission_request(payload: dict[str, Any]) -> dict[str, Any] | None:
    tool = _tool_name(payload)
    command = _command(payload)
    normalized = _normalized_slashes(command)
    if tool in {"Bash", "apply_patch"} and PROTECTED_ORIGINAL in normalized:
        protected_pattern = re.escape(PROTECTED_ORIGINAL)
        if _command_writes_path(command, protected_pattern) or tool == "apply_patch":
            return _permission_deny(
                "PROTECTED_SOURCE_WRITE",
                "Approval cannot override protected-source immutability.",
            )
    if tool == "Bash" and ENGINEERING_LAUNCH_PATTERN.search(command):
        return _permission_deny(
            "ENGINEERING_APP_SHELL_LAUNCH",
            "Approval cannot replace approved Computer Use discovery.",
        )
    if tool == "Bash" and _is_exact_offline_validation(command):
        return _permission_allow()
    # Hardware-facing requests deliberately receive no automatic allow/deny.
    # The normal permission flow and primary policy remain authoritative.
    return None


def _response_failed(response: Any) -> bool:
    if isinstance(response, dict):
        if response.get("isError") is True:
            return True
        exit_code = response.get("exit_code")
        if isinstance(exit_code, int) and exit_code != 0:
            return True
        status = str(response.get("status", "")).lower()
        if status in {"failed", "error", "cancelled"}:
            return True
    return False


def _ledger_path(session_id: str) -> Path:
    return LOG_ROOT / "sessions" / f"{_safe_id(session_id or 'unknown')}.json"


def _load_ledger(session_id: str) -> dict[str, Any]:
    path = _ledger_path(session_id)
    if not path.exists():
        return {
            "schema_version": 1,
            "session": _safe_id(session_id or "unknown"),
            "created_at": utc_now(),
            "changed_files": [],
            "tests_passed": [],
            "change_log_written": False,
            "issues": [],
        }
    try:
        value = json.loads(path.read_text(encoding="utf-8"))
        return value if isinstance(value, dict) else {}
    except (OSError, json.JSONDecodeError):
        return {
            "schema_version": 1,
            "session": _safe_id(session_id or "unknown"),
            "created_at": utc_now(),
            "changed_files": [],
            "tests_passed": [],
            "change_log_written": False,
            "issues": ["LEDGER_RECOVERY_REQUIRED"],
        }


def _save_ledger(session_id: str, ledger: dict[str, Any]) -> None:
    path = _ledger_path(session_id)
    path.parent.mkdir(parents=True, exist_ok=True)
    ledger["updated_at"] = utc_now()
    temporary = path.with_suffix(".tmp")
    temporary.write_text(
        json.dumps(ledger, indent=2, sort_keys=True), encoding="utf-8"
    )
    os.replace(temporary, path)


def _is_policy_surface(relative: str) -> bool:
    normalized = relative.replace("\\", "/")
    return normalized in POLICY_SURFACE_FILES or normalized.startswith(
        POLICY_SURFACE_PREFIXES
    )


def _update_post_ledger(payload: dict[str, Any], failed: bool) -> None:
    session_id = str(payload.get("session_id", "unknown"))
    ledger = _load_ledger(session_id)
    tool = _tool_name(payload)
    command = _command(payload)

    changed_files = set(str(item) for item in ledger.get("changed_files", []))
    tests_passed = set(str(item) for item in ledger.get("tests_passed", []))
    issues = set(str(item) for item in ledger.get("issues", []))

    if tool == "apply_patch" and not failed:
        cwd = str(payload.get("cwd", REPOSITORY_ROOT))
        for raw_path in extract_patch_paths(command):
            path = _resolve_hook_path(raw_path, cwd)
            if _within_repository(path):
                changed_files.add(_repository_relative(path))

    normalized = _normalized_slashes(command)
    if tool == "Bash" and not failed:
        if "test-prompt-judge.ps1" in normalized:
            tests_passed.add("test-prompt-judge")
        if "test-repository.ps1" in normalized:
            tests_passed.add("test-repository")
        if "write-change-log.ps1" in normalized:
            ledger["change_log_written"] = True

    if failed:
        issues.add("TOOL_RESULT_FAILED")

    ledger["changed_files"] = sorted(changed_files)
    ledger["tests_passed"] = sorted(tests_passed)
    ledger["issues"] = sorted(issues)
    ledger["policy_surfaces_changed"] = any(
        _is_policy_surface(path) for path in changed_files
    )
    _save_ledger(session_id, ledger)


def handle_post_tool(payload: dict[str, Any]) -> dict[str, Any] | None:
    response = payload.get("tool_response")
    failed = _response_failed(response)
    _update_post_ledger(payload, failed)

    if _tool_name(payload) == "apply_patch":
        violation = _validate_patch(payload)
        if violation:
            reason = violation["hookSpecificOutput"]["permissionDecisionReason"]
            return _post_block(
                "POST_WRITE_INTEGRITY_FAILURE",
                reason,
            )
    if failed:
        return _context(
            "PostToolUse",
            "TOOL_RESULT_FAILED: do not report success. Inspect the original tool "
            "result, preserve relevant evidence, and reconcile before continuing.",
        )
    return None


def handle_stop(payload: dict[str, Any]) -> dict[str, Any] | None:
    if payload.get("stop_hook_active") is True:
        return None
    session_id = str(payload.get("session_id", "unknown"))
    ledger_path = _ledger_path(session_id)
    if not ledger_path.exists():
        return None
    ledger = _load_ledger(session_id)
    missing: list[str] = []
    changed_files = ledger.get("changed_files", [])
    tests_passed = set(ledger.get("tests_passed", []))

    if changed_files and not ledger.get("change_log_written", False):
        missing.append("mandatory change-log command")
    if ledger.get("policy_surfaces_changed", False):
        if "test-prompt-judge" not in tests_passed:
            missing.append("scripts/test-prompt-judge.ps1")
        if "test-repository" not in tests_passed:
            missing.append("scripts/test-repository.ps1")
    if "POST_WRITE_INTEGRITY_FAILURE" in ledger.get("issues", []):
        missing.append("post-write integrity reconciliation")

    if not missing:
        return None
    message = "Completion checks are incomplete: " + ", ".join(missing)
    return {"decision": "block", "reason": message}


def write_telemetry(payload: dict[str, Any], hook_duration_ms: float) -> None:
    try:
        LOG_ROOT.mkdir(parents=True, exist_ok=True)
        response = payload.get("tool_response")
        failed = _response_failed(response)
        command = _command(payload)
        event = {
            "ts": utc_now(),
            "event": payload.get("hook_event_name", "unknown"),
            "matcher": HOOK_MATCHER,
            "session": _safe_id(str(payload.get("session_id", "unknown"))),
            "turn": _safe_id(str(payload.get("turn_id", "unknown"))),
            "tool_name": payload.get("tool_name"),
            "tool_input_digest": _json_digest(payload.get("tool_input", {})),
            "tool_failed": failed,
            "decision": "failure_observed" if failed else "observed",
            "reason_code": "TOOL_RESULT_FAILED" if failed else None,
            "hook_duration_ms": round(hook_duration_ms, 3),
            "tool_duration_ms": None,
            "duration_source": "unavailable_in_hook_payload",
            "synchronous": False,
            "timeout_seconds": HOOK_TIMEOUT_SECONDS,
            "tool_response_bytes": len(
                json.dumps(response, sort_keys=True, default=str).encode("utf-8")
            ),
            "computer_use_action_count": len(ACTION_PATTERN.findall(command)),
            "screenshot_operation_mentions": len(
                re.findall(r"(?i)\b(?:screenshot|emitImage|capture)\b", command)
            ),
            "mode": "async_telemetry",
        }
        filename = f"hooks-{datetime.now(timezone.utc):%Y-%m-%d}.jsonl"
        with (LOG_ROOT / filename).open("a", encoding="utf-8") as handle:
            handle.write(json.dumps(event, sort_keys=True) + "\n")
    except OSError:
        # Telemetry is non-authoritative and must not affect tool execution.
        return


def dispatch(payload: dict[str, Any]) -> dict[str, Any] | None:
    event = str(payload.get("hook_event_name", ""))
    if event == "PreToolUse":
        return handle_pre_tool(payload)
    if event == "PermissionRequest":
        return handle_permission_request(payload)
    if event == "PostToolUse":
        return handle_post_tool(payload)
    if event == "Stop":
        return handle_stop(payload)
    return None


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--telemetry-only", action="store_true")
    args = parser.parse_args(argv)
    started = time.perf_counter()
    try:
        payload = json.load(sys.stdin)
        if not isinstance(payload, dict):
            raise ValueError("Hook input must be a JSON object")
        if args.telemetry_only:
            write_telemetry(payload, (time.perf_counter() - started) * 1000)
            return 0
        result = dispatch(payload)
    except Exception as error:  # noqa: BLE001
        if args.telemetry_only:
            return 0
        event = ""
        try:
            event = str(locals().get("payload", {}).get("hook_event_name", ""))
        except Exception:  # noqa: BLE001
            event = ""
        if event == "PreToolUse":
            result = _deny("HOOK_VALIDATION_ERROR", str(error))
        elif event == "PermissionRequest":
            result = _permission_deny("HOOK_VALIDATION_ERROR", str(error))
        elif event == "PostToolUse":
            result = _post_block("HOOK_VALIDATION_ERROR", str(error))
        else:
            result = {
                "continue": False,
                "stopReason": f"HOOK_VALIDATION_ERROR: {error}",
                "systemMessage": "Lifecycle-hook validation failed.",
            }
    if result is not None:
        sys.stdout.write(json.dumps(result, separators=(",", ":")))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
