#!/usr/bin/env python3
"""
user_prompt_submit.py — Codex UserPromptSubmit hook for the Schlenker project.

Called by Codex every time the user submits a prompt in the IDE.
Receives a JSON payload on stdin with at minimum:
    {
        "hook_event_name": "UserPromptSubmit",
        "prompt": "<the user's message>",
        "session_id": "...",
        "cwd": "...",
        "model": "..."
    }

Outputs one of:

1. A JSON object with additionalContext (the score banner) — Codex injects
   this as extra context before the model processes the prompt.

2. A JSON object with decision=block and reason — if the prompt contains a
   hard safety violation, the hook stops the turn entirely.

3. Exit code 0 with empty output — if the prompt is short (greeting/meta)
   and doesn't warrant evaluation.

Configuration (environment variables):
    OPENAI_API_KEY   — required; the key used to call the judge model
    JUDGE_MODEL      — optional; defaults to gpt-5.6-sol
    JUDGE_THRESHOLD  — optional float; prompts scoring below this cause a
                       block decision (default: disabled / 0.0 = never block)
    JUDGE_MIN_WORDS  — optional int; skip evaluation for prompts shorter than
                       this many words (default: 5)
    JUDGE_LOG_DIR    — optional path to write per-turn JSON logs
                       (defaults to <repo>/.codex/logs/)

Safety-block threshold:
    If JUDGE_THRESHOLD is set to e.g. 2.0, any prompt scoring below 2.0
    will be blocked with an explanation. Set to 0.0 (default) to never
    block — only annotate.
"""

from __future__ import annotations

import json
import os
import sys
import time
import traceback
from datetime import datetime, timezone
from pathlib import Path

# ---------------------------------------------------------------------------
# Resolve judge module location
# ---------------------------------------------------------------------------
# The hook runs from the repo root (cwd = session cwd), but judge.py lives
# in LLM-AS-A-JUDGE/judge/. We locate it relative to this file.
# ---------------------------------------------------------------------------

_HOOK_FILE     = Path(__file__).resolve()
_CODEX_DIR     = _HOOK_FILE.parent          # .codex/hooks/
_REPO_ROOT     = _CODEX_DIR.parent.parent   # Schlenker/
_JUDGE_DIR     = _REPO_ROOT / "judge"
_LOGS_DIR_DEFAULT = _REPO_ROOT / "logs" / "judge"

if str(_JUDGE_DIR) not in sys.path:
    sys.path.insert(0, str(_JUDGE_DIR))

# ---------------------------------------------------------------------------
# Configuration
# ---------------------------------------------------------------------------

JUDGE_MODEL     = os.environ.get("JUDGE_MODEL", "gpt-5.6-sol")
OPENAI_API_KEY  = os.environ.get("OPENAI_API_KEY", "")
JUDGE_THRESHOLD = float(os.environ.get("JUDGE_THRESHOLD", "0.0"))
JUDGE_MIN_WORDS = int(os.environ.get("JUDGE_MIN_WORDS", "5"))
JUDGE_LOG_DIR   = Path(os.environ.get("JUDGE_LOG_DIR", str(_LOGS_DIR_DEFAULT)))

# Hard-violation keyword patterns — trigger an immediate safety block
# before even calling the judge model (latency-free first-pass filter).
HARD_VIOLATION_PATTERNS: list[tuple[str, str]] = [
    ("write to plc",         "AGENTS.md prohibits writing to PLC without explicit authorization."),
    ("download to plc",      "AGENTS.md prohibits downloading to PLC without explicit authorization."),
    ("transfer to plc",      "AGENTS.md prohibits PLC transfer without explicit authorization."),
    ("remote operation",     "AGENTS.md prohibits Remote Operation without explicit authorization."),
    ("d:\\gx works\\schlenker.gxw", "AGENTS.md prohibits opening the protected original project."),
    ("d:/gx works/schlenker.gxw",   "AGENTS.md prohibits opening the protected original project."),
    ("skip the safety gate", "Safety gate cannot be skipped per AGENTS.md."),
    ("bypass the hash",      "SHA-256 integrity check cannot be bypassed per AGENTS.md."),
    ("add click to the capture",     "Capture helper must remain read-only per AGENTS.md."),
    ("add input to the capture",     "Capture helper must remain read-only per AGENTS.md."),
]


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _is_trivial_prompt(prompt: str) -> bool:
    """Return True for greetings / meta prompts that don't need scoring."""
    words = prompt.strip().split()
    if len(words) < JUDGE_MIN_WORDS:
        return True
    trivial_starts = (
        "hello", "hi ", "thanks", "thank you", "ok", "okay",
        "yes", "no", "sure", "got it", "understood",
    )
    lower = prompt.lower().strip()
    return any(lower.startswith(t) for t in trivial_starts)


def _hard_violation_check(prompt: str) -> str | None:
    """
    Fast regex-free check for the most dangerous patterns.
    Returns the violation reason string, or None if clean.
    """
    lower = prompt.lower()
    for pattern, reason in HARD_VIOLATION_PATTERNS:
        if pattern in lower:
            return reason
    return None


def _write_log(
    session_id: str,
    turn_id: str,
    prompt: str,
    result: dict | None,
    blocked: bool,
    block_reason: str,
    elapsed_ms: float,
) -> None:
    """Write a per-turn JSON log entry to JUDGE_LOG_DIR."""
    try:
        JUDGE_LOG_DIR.mkdir(parents=True, exist_ok=True)
        today = datetime.now(timezone.utc).strftime("%Y-%m-%d")
        log_file = JUDGE_LOG_DIR / f"judge-{today}.jsonl"

        entry = {
            "ts":          datetime.now(timezone.utc).isoformat(),
            "session_id":  session_id,
            "turn_id":     turn_id,
            "prompt_len":  len(prompt),
            "blocked":     blocked,
            "block_reason": block_reason,
            "elapsed_ms":  round(elapsed_ms, 1),
            "result":      result,
        }
        with log_file.open("a", encoding="utf-8") as fh:
            fh.write(json.dumps(entry) + "\n")
    except Exception:  # noqa: BLE001
        pass  # logging failure must never crash the hook


def _format_additional_context(result: dict) -> str:
    """
    Build the additionalContext string injected into the model context.
    Codex will see this before processing the prompt.
    """
    # Import here so missing openai package during non-judge tests still works
    from judge import format_score_banner  # noqa: PLC0415

    banner = format_score_banner(result)
    overall = result["overall_score"]
    label   = result["label"]
    top_issue = result.get("top_issue", "")
    suggestion = result.get("suggestion", "")

    parts = [
        f"[SCHLENKER JUDGE] Prompt scored {overall:.2f}/5.00 — {label}",
        "",
        banner,
        "",
    ]

    if overall < 3.0:
        parts.append(
            "⚠️  The user prompt scored POOR or REJECT. Before proceeding, "
            "consider asking the user to clarify the following:"
        )
        if top_issue:
            parts.append(f"   → {top_issue}")
        if suggestion:
            parts.append(f"   💡 {suggestion}")
        parts.append("")

    elif overall < 4.0:
        parts.append(
            "ℹ️  The prompt scored FAIR. Proceed, but you may ask one "
            "clarifying question if the scope is ambiguous."
        )
        parts.append("")

    return "\n".join(parts)


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main() -> None:
    t0 = time.monotonic()
    blocked      = False
    block_reason = ""
    result       = None
    session_id   = ""
    turn_id      = ""

    try:
        raw = sys.stdin.read()
        payload = json.loads(raw) if raw.strip() else {}

        prompt     = payload.get("prompt", "")
        session_id = payload.get("session_id", "")
        turn_id    = payload.get("turn_id", "")
        cwd        = payload.get("cwd", "")

        # ------------------------------------------------------------------
        # 1. Skip trivial prompts silently
        # ------------------------------------------------------------------
        if _is_trivial_prompt(prompt):
            sys.exit(0)

        # ------------------------------------------------------------------
        # 2. Fast hard-violation filter (no API call needed)
        # ------------------------------------------------------------------
        violation = _hard_violation_check(prompt)
        if violation:
            blocked      = True
            block_reason = violation
            elapsed_ms   = (time.monotonic() - t0) * 1000
            _write_log(session_id, turn_id, prompt, None, True, block_reason, elapsed_ms)

            output = {
                "decision": "block",
                "reason": (
                    f"🚨 SCHLENKER SAFETY BLOCK\n\n"
                    f"The prompt triggers a hard policy violation:\n"
                    f"  {violation}\n\n"
                    f"Reference: Schlenker AGENTS.md — Development-environment safety gate.\n"
                    f"Please revise your request to comply with the project safety policy."
                ),
            }
            print(json.dumps(output), flush=True)
            sys.exit(0)

        # ------------------------------------------------------------------
        # 3. No API key — annotate without scoring
        # ------------------------------------------------------------------
        if not OPENAI_API_KEY or OPENAI_API_KEY.strip() in ("", "none", "sk-..."):
            note = (
                "[SCHLENKER JUDGE] Prompt evaluation skipped — "
                "OPENAI_API_KEY not set. Set the environment variable to "
                "enable real-time scoring."
            )
            output = {
                "hookSpecificOutput": {
                    "hookEventName": "UserPromptSubmit",
                    "additionalContext": note,
                }
            }
            print(json.dumps(output), flush=True)
            sys.exit(0)

        # ------------------------------------------------------------------
        # 4. Call the judge engine
        # ------------------------------------------------------------------
        from judge import evaluate, format_score_banner  # noqa: PLC0415

        result    = evaluate(prompt, api_key=OPENAI_API_KEY, model=JUDGE_MODEL)
        overall   = result["overall_score"]
        elapsed_ms = (time.monotonic() - t0) * 1000

        # ------------------------------------------------------------------
        # 5. Block if below threshold (only when JUDGE_THRESHOLD > 0)
        # ------------------------------------------------------------------
        if JUDGE_THRESHOLD > 0.0 and overall < JUDGE_THRESHOLD:
            blocked      = True
            block_reason = f"Score {overall:.2f} below threshold {JUDGE_THRESHOLD:.2f}"
            _write_log(session_id, turn_id, prompt, result, True, block_reason, elapsed_ms)

            top_issue  = result.get("top_issue", "Please clarify your request.")
            suggestion = result.get("suggestion", "")
            output = {
                "decision": "block",
                "reason": (
                    f"🟠 SCHLENKER JUDGE — Prompt scored {overall:.2f}/5.00 "
                    f"({result['label']})\n\n"
                    f"⚠️  Top issue: {top_issue}\n"
                    + (f"💡 Suggestion: {suggestion}\n" if suggestion else "")
                    + f"\nPlease refine your prompt before continuing."
                ),
            }
            print(json.dumps(output), flush=True)
            sys.exit(0)

        # ------------------------------------------------------------------
        # 6. Annotate — inject score as additionalContext
        # ------------------------------------------------------------------
        _write_log(session_id, turn_id, prompt, result, False, "", elapsed_ms)

        additional_context = _format_additional_context(result)
        output = {
            "hookSpecificOutput": {
                "hookEventName": "UserPromptSubmit",
                "additionalContext": additional_context,
            }
        }
        print(json.dumps(output), flush=True)
        sys.exit(0)

    except Exception:  # noqa: BLE001
        # Hook failures must never crash the Codex session.
        # Log the traceback but exit 0 so Codex continues normally.
        elapsed_ms = (time.monotonic() - t0) * 1000
        tb = traceback.format_exc()
        _write_log(
            session_id, turn_id,
            payload.get("prompt", "") if "payload" in dir() else "",
            None, False, f"HOOK_ERROR: {tb}", elapsed_ms
        )
        # Silently continue — don't break the IDE
        sys.exit(0)


if __name__ == "__main__":
    main()
