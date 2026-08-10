#!/usr/bin/env python3
"""
judge.py — Core LLM-as-a-Judge engine for the Schlenker project.

Evaluates user prompts on four rubric dimensions:
  1. clarity             (0.20 weight)
  2. safety_compliance   (0.30 weight)
  3. scl_plc_specificity (0.25 weight)
  4. agents_md_adherence (0.25 weight)

Returns a JSON ScoreResult with per-dimension scores, an overall weighted
score, a label, and actionable feedback for each dimension.

Usage (standalone):
    echo '{"prompt": "add a comment to FB_DoorAccess"}' | python3 judge.py

Usage (from hook):
    import judge
    result = judge.evaluate(prompt_text, api_key, model)
"""

from __future__ import annotations

import json
import os
import sys
import textwrap
from pathlib import Path
from typing import Any

# ---------------------------------------------------------------------------
# Constants
# ---------------------------------------------------------------------------

JUDGE_DIR = Path(__file__).parent
RUBRICS_PATH = JUDGE_DIR / "rubrics.json"
CONFIG_PATH = JUDGE_DIR / "config.json"

DEFAULT_CONFIG: dict[str, Any] = {
    "model": "gpt-5.6-terra",
    "reasoning_effort": "low",
    "request_timeout_seconds": 20,
    "max_retries": 0,
    "threshold": 0.0,
    "min_words": 5,
    "log_directory": "logs/judge",
}

DIMENSION_WEIGHTS = {
    "clarity": 0.20,
    "safety_compliance": 0.30,
    "scl_plc_specificity": 0.25,
    "agents_md_adherence": 0.25,
}

SCORE_LABELS = {
    (1.0, 1.9): ("REJECT",    "🔴"),
    (2.0, 2.9): ("POOR",      "🟠"),
    (3.0, 3.9): ("FAIR",      "🟡"),
    (4.0, 4.4): ("GOOD",      "🟢"),
    (4.5, 5.0): ("EXCELLENT", "✅"),
}

def load_config() -> dict[str, Any]:
    """Load and minimally validate the versioned judge configuration."""
    config = DEFAULT_CONFIG.copy()
    if CONFIG_PATH.exists():
        with CONFIG_PATH.open(encoding="utf-8") as fh:
            loaded = json.load(fh)
        if not isinstance(loaded, dict):
            raise ValueError("judge/config.json must contain a JSON object")
        config.update(loaded)

    if not str(config["model"]).strip():
        raise ValueError("judge model must not be empty")
    if str(config["reasoning_effort"]) not in {
        "none", "low", "medium", "high", "xhigh", "max"
    }:
        raise ValueError("unsupported judge reasoning_effort")
    if float(config["request_timeout_seconds"]) <= 0:
        raise ValueError("request_timeout_seconds must be positive")
    if int(config["max_retries"]) < 0:
        raise ValueError("max_retries must not be negative")
    if not 0.0 <= float(config["threshold"]) <= 5.0:
        raise ValueError("threshold must be between 0 and 5")
    if int(config["min_words"]) < 1:
        raise ValueError("min_words must be at least 1")
    return config


RUNTIME_CONFIG = load_config()
DEFAULT_MODEL = os.environ.get("JUDGE_MODEL", str(RUNTIME_CONFIG["model"]))
DEFAULT_REASONING_EFFORT = os.environ.get(
    "JUDGE_REASONING_EFFORT",
    str(RUNTIME_CONFIG["reasoning_effort"]),
)
REQUEST_TIMEOUT_SECONDS = float(os.environ.get(
    "JUDGE_REQUEST_TIMEOUT_SECONDS",
    str(RUNTIME_CONFIG["request_timeout_seconds"]),
))
MAX_RETRIES = int(os.environ.get(
    "JUDGE_MAX_RETRIES",
    str(RUNTIME_CONFIG["max_retries"]),
))
MAX_PROMPT_CHARS = 8000  # truncate very long prompts to save tokens

JUDGMENT_SCHEMA: dict[str, Any] = {
    "type": "object",
    "properties": {
        "scores": {
            "type": "object",
            "properties": {
                dimension: {"type": "integer"}
                for dimension in DIMENSION_WEIGHTS
            },
            "required": list(DIMENSION_WEIGHTS),
            "additionalProperties": False,
        },
        "bonus": {"type": "number"},
        "feedback": {
            "type": "object",
            "properties": {
                dimension: {"type": "string"}
                for dimension in DIMENSION_WEIGHTS
            },
            "required": list(DIMENSION_WEIGHTS),
            "additionalProperties": False,
        },
        "top_issue": {"type": "string"},
        "suggestion": {"type": "string"},
    },
    "required": ["scores", "bonus", "feedback", "top_issue", "suggestion"],
    "additionalProperties": False,
}

# ---------------------------------------------------------------------------
# Rubric loader
# ---------------------------------------------------------------------------

def load_rubrics() -> dict[str, Any]:
    """Load rubrics.json from the judge/ directory."""
    with RUBRICS_PATH.open(encoding="utf-8") as fh:
        return json.load(fh)


# ---------------------------------------------------------------------------
# Judge prompt builder
# ---------------------------------------------------------------------------

def build_judge_prompt(user_prompt: str, rubrics: dict[str, Any]) -> str:
    """Build the system + user message for the judge LLM call."""
    dims = rubrics["dimensions"]

    # Build a compact rubric summary for each dimension
    rubric_blocks = []
    for dim_key, weight in DIMENSION_WEIGHTS.items():
        dim = dims[dim_key]
        anchors = dim["score_anchors"]
        anchor_text = "\n".join(
            f"  Score {k}: {v}" for k, v in anchors.items()
        )
        rubric_blocks.append(
            f"### {dim_key.upper()} (weight {int(weight * 100)}%)\n"
            f"{dim['description']}\n\n"
            f"Score anchors:\n{anchor_text}"
        )

    # Hard violations for safety
    hard_violations = "\n".join(
        f"  • {v}" for v in dims["safety_compliance"]["hard_violations"]
    )

    # Bonus and penalty signals
    bonus_patterns = "\n".join(
        f"  • {p}" for p in rubrics["bonus_signals"]["patterns"]
    )
    penalty_patterns = "\n".join(
        f"  • {p['pattern']} → {p['penalty']:+.1f} on {p['dimension']}"
        for p in rubrics["penalty_signals"]["patterns"]
    )

    rubric_text = "\n\n".join(rubric_blocks)

    system_prompt = textwrap.dedent(f"""
        You are a strict industrial-automation quality judge for the Schlenker project.
        The Schlenker project develops:
          - A Mitsubishi FX3G Ladder PLC project managed in GX Works2 1.560J
          - A Siemens S7-1512C-1 PN / TIA Portal V19 project (REV12 SCL sources)
          - An HMI on MTP1500 Unified Comfort (WinCC Unified V19)

        You will receive a user prompt that was submitted to the Codex IDE agent.
        Your job is to score that prompt on FOUR dimensions using 1–5 integer scores.

        === RUBRICS ===
        {rubric_text}

        === SAFETY HARD VIOLATIONS (auto-score safety_compliance = 1) ===
        Any of these in the prompt sets safety_compliance to 1 regardless of other content:
        {hard_violations}

        === BONUS SIGNALS (+0.1 each, max +0.3 to overall) ===
        {bonus_patterns}

        === PENALTY SIGNALS (deducted from dimension score) ===
        {penalty_patterns}

        === OUTPUT FORMAT ===
        Respond with ONLY valid JSON — no markdown fences, no preamble.
        Schema:
        {{
          "scores": {{
            "clarity":             <1–5 integer>,
            "safety_compliance":   <1–5 integer>,
            "scl_plc_specificity": <1–5 integer>,
            "agents_md_adherence": <1–5 integer>
          }},
          "bonus": <0.0, 0.1, 0.2, or 0.3>,
          "feedback": {{
            "clarity":             "<one concise sentence: what is good or what is missing>",
            "safety_compliance":   "<one concise sentence>",
            "scl_plc_specificity": "<one concise sentence>",
            "agents_md_adherence": "<one concise sentence>"
          }},
          "top_issue": "<the single most important thing the user should fix or clarify>",
          "suggestion": "<one concrete rewrite suggestion for the weakest part of the prompt>"
        }}
    """).strip()

    user_message = textwrap.dedent(f"""
        === USER PROMPT TO EVALUATE ===
        {user_prompt[:MAX_PROMPT_CHARS]}

        Score this prompt on all four dimensions. Apply any hard violations, bonuses,
        and penalties. Return only the JSON object described above.
    """).strip()

    return system_prompt, user_message


# ---------------------------------------------------------------------------
# OpenAI call
# ---------------------------------------------------------------------------

def call_judge(
    system_prompt: str,
    user_message: str,
    api_key: str,
    model: str = DEFAULT_MODEL,
) -> dict[str, Any]:
    """Call the OpenAI API and return the parsed JSON judgment."""
    try:
        import openai
    except ImportError as exc:
        raise RuntimeError(
            "openai package not installed. Run: pip install openai"
        ) from exc

    client = openai.OpenAI(
        api_key=api_key,
        timeout=REQUEST_TIMEOUT_SECONDS,
        max_retries=MAX_RETRIES,
    )

    response = client.responses.create(
        model=model,
        reasoning={"effort": DEFAULT_REASONING_EFFORT},
        input=[
            {"role": "system", "content": system_prompt},
            {"role": "user",   "content": user_message},
        ],
        text={
            "format": {
                "type": "json_schema",
                "name": "schlenker_prompt_judgment",
                "strict": True,
                "schema": JUDGMENT_SCHEMA,
            }
        },
    )

    if response.status != "completed":
        raise RuntimeError(f"judge response status was {response.status!r}")
    return json.loads(response.output_text)


def validate_judgment(judgment: dict[str, Any]) -> None:
    """Validate scoring ranges before using model output for hook decisions."""
    scores = judgment.get("scores")
    if not isinstance(scores, dict):
        raise ValueError("judge response is missing scores")
    for dimension in DIMENSION_WEIGHTS:
        value = scores.get(dimension)
        if not isinstance(value, int) or isinstance(value, bool):
            raise ValueError(f"{dimension} score must be an integer")
        if not 1 <= value <= 5:
            raise ValueError(f"{dimension} score must be between 1 and 5")

    bonus = judgment.get("bonus")
    if not isinstance(bonus, (int, float)) or isinstance(bonus, bool):
        raise ValueError("bonus must be numeric")
    if float(bonus) not in {0.0, 0.1, 0.2, 0.3}:
        raise ValueError("bonus must be 0.0, 0.1, 0.2, or 0.3")


# ---------------------------------------------------------------------------
# Score computation
# ---------------------------------------------------------------------------

def compute_overall(
    scores: dict[str, int],
    bonus: float,
) -> float:
    """Compute the weighted overall score with bonus applied."""
    weighted = sum(
        scores[dim] * weight
        for dim, weight in DIMENSION_WEIGHTS.items()
    )
    return round(min(5.0, weighted + bonus), 2)


def get_label(overall: float) -> tuple[str, str]:
    """Return (label, emoji) for an overall score."""
    for (lo, hi), (label, emoji) in SCORE_LABELS.items():
        if lo <= overall <= hi:
            return label, emoji
    return "UNKNOWN", "❓"


# ---------------------------------------------------------------------------
# Main evaluate function
# ---------------------------------------------------------------------------

def evaluate(
    prompt: str,
    api_key: str | None = None,
    model: str = DEFAULT_MODEL,
) -> dict[str, Any]:
    """
    Full evaluation pipeline.

    Returns a dict with:
      overall_score, label, emoji, scores, bonus, feedback,
      top_issue, suggestion, model_used
    """
    if not api_key:
        api_key = os.environ.get("OPENAI_API_KEY", "")
    if not api_key:
        raise ValueError(
            "No OpenAI API key found. Set OPENAI_API_KEY environment variable."
        )

    rubrics = load_rubrics()
    system_prompt, user_message = build_judge_prompt(prompt, rubrics)
    judgment = call_judge(system_prompt, user_message, api_key, model)
    validate_judgment(judgment)

    scores = judgment.get("scores", {})
    bonus = float(judgment.get("bonus", 0.0))
    overall = compute_overall(scores, bonus)
    label, emoji = get_label(overall)

    return {
        "overall_score": overall,
        "label": label,
        "emoji": emoji,
        "scores": scores,
        "bonus": bonus,
        "feedback": judgment.get("feedback", {}),
        "top_issue": judgment.get("top_issue", ""),
        "suggestion": judgment.get("suggestion", ""),
        "model_used": model,
        "prompt_length": len(prompt),
    }


# ---------------------------------------------------------------------------
# Formatter — produces the additionalContext string for Codex
# ---------------------------------------------------------------------------

def format_score_banner(result: dict[str, Any]) -> str:
    """
    Render a compact, visually clear score banner that Codex will show
    as additionalContext before processing the prompt.
    """
    overall = result["overall_score"]
    label   = result["label"]
    emoji   = result["emoji"]
    scores  = result["scores"]

    bar = _score_bar(overall)

    lines = [
        "╔══════════════════════════════════════════════════════════╗",
        f"║  SCHLENKER PROMPT JUDGE   {emoji} {label:<10} {overall:.2f}/5.00  ║",
        f"║  {bar}  ║",
        "╠══════════════════════════════════════════════════════════╣",
        f"║  Clarity          {scores.get('clarity',0)}/5  │  Safety Compliance  {scores.get('safety_compliance',0)}/5  ║",
        f"║  SCL/PLC Specific {scores.get('scl_plc_specificity',0)}/5  │  AGENTS.md Policy   {scores.get('agents_md_adherence',0)}/5  ║",
        "╠══════════════════════════════════════════════════════════╣",
    ]

    # Per-dimension feedback
    for dim, fb in result.get("feedback", {}).items():
        short_dim = {
            "clarity": "Clarity",
            "safety_compliance": "Safety",
            "scl_plc_specificity": "SCL/PLC",
            "agents_md_adherence": "AGENTS.md",
        }.get(dim, dim)
        score_val = scores.get(dim, "?")
        # Wrap long feedback lines
        fb_wrapped = textwrap.wrap(fb, width=46)
        first_line = fb_wrapped[0] if fb_wrapped else ""
        lines.append(f"║  [{score_val}/5] {short_dim:<10} {first_line:<36}  ║")
        for extra in fb_wrapped[1:]:
            lines.append(f"║               {extra:<46}  ║")

    lines.append("╠══════════════════════════════════════════════════════════╣")

    # Top issue
    if result.get("top_issue"):
        issue_lines = textwrap.wrap(result["top_issue"], width=54)
        lines.append(f"║  ⚠ TOP ISSUE: {issue_lines[0]:<42}  ║")
        for il in issue_lines[1:]:
            lines.append(f"║               {il:<42}  ║")

    # Suggestion
    if result.get("suggestion"):
        sug_lines = textwrap.wrap(result["suggestion"], width=54)
        lines.append(f"║  💡 SUGGEST:  {sug_lines[0]:<42}  ║")
        for sl in sug_lines[1:]:
            lines.append(f"║               {sl:<42}  ║")

    lines.append("╚══════════════════════════════════════════════════════════╝")
    return "\n".join(lines)


def _score_bar(score: float, width: int = 40) -> str:
    """Render a simple ASCII progress bar for the overall score."""
    filled = int(round((score / 5.0) * width))
    bar = "█" * filled + "░" * (width - filled)
    return f"[{bar}]"


# ---------------------------------------------------------------------------
# CLI entry point (also used by hook for testing)
# ---------------------------------------------------------------------------

def main() -> None:
    """
    Read JSON from stdin: {"prompt": "...", "model": "...", "api_key": "..."}
    Write JSON to stdout: full ScoreResult
    Write the banner to stderr for visual inspection.
    """
    try:
        raw_input = sys.stdin.read()
        if not raw_input.strip():
            print(json.dumps({"error": "empty input"}), flush=True)
            sys.exit(1)

        data = json.loads(raw_input)
        prompt    = data.get("prompt", "")
        model     = data.get("model", DEFAULT_MODEL)
        api_key   = data.get("api_key") or os.environ.get("OPENAI_API_KEY", "")

        if not prompt:
            print(json.dumps({"error": "no prompt provided"}), flush=True)
            sys.exit(1)

        result = evaluate(prompt, api_key=api_key, model=model)
        banner = format_score_banner(result)

        # Write banner to stderr so hook can optionally display it
        print(banner, file=sys.stderr, flush=True)
        # Write structured result to stdout
        print(json.dumps(result, indent=2), flush=True)

    except Exception as exc:  # noqa: BLE001
        print(json.dumps({"error": str(exc)}), file=sys.stdout, flush=True)
        sys.exit(1)


if __name__ == "__main__":
    main()
