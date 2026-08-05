# LLM-AS-A-JUDGE

Real-time prompt quality scorer for the Schlenker industrial automation project.
Hooks into Codex's `UserPromptSubmit` lifecycle event and injects a score banner
into the model's context before every prompt is processed.

---

## What it does

Every time you type a message in the Codex IDE, this judge:

1. Runs a fast **hard-violation filter** (no API call) to detect forbidden
   patterns like "write to PLC" or opening the protected source file.
2. Calls the **judge model** (`gpt-5.6-sol` by default) to score your prompt
   on four rubric dimensions.
3. Injects a **score banner** as `additionalContext` — Codex sees it before
   answering you, so it can ask clarifying questions if your prompt is weak.
4. Writes a **JSONL log entry** for every evaluated turn.

---

## Score dimensions

| Dimension | Weight | What it measures |
|---|---|---|
| **Clarity** | 20% | Is the target named? Is the outcome explicit? Single intent? |
| **Safety Compliance** | 30% | Respects AGENTS.md: no PLC writes, no protected file, fresh safety gate |
| **SCL / PLC Specificity** | 25% | Uses correct IEC 61131-3 / TIA Portal / GX Works2 terminology and project artefacts |
| **AGENTS.md Adherence** | 25% | Follows Computer Use contract, change-log policy, evidence requirements |

---

## Score legend

| Score | Label | Action |
|---|---|---|
| 1.0 – 1.9 | 🔴 REJECT | Block or strongly warn |
| 2.0 – 2.9 | 🟠 POOR | Significant clarification required |
| 3.0 – 3.9 | 🟡 FAIR | Proceed with caution, agent may ask one clarifying question |
| 4.0 – 4.4 | 🟢 GOOD | Proceed — minor suggestions only |
| 4.5 – 5.0 | ✅ EXCELLENT | Proceed confidently |

---

## What the banner looks like in Codex

When you submit a prompt you will see (as injected context before the agent responds):

```
╔══════════════════════════════════════════════════════════╗
║  SCHLENKER PROMPT JUDGE   🟢 GOOD       4.10/5.00  ║
║  [████████████████████████████████░░░░░░]  ║
╠══════════════════════════════════════════════════════════╣
║  Clarity          4/5  │  Safety Compliance  5/5  ║
║  SCL/PLC Specific 4/5  │  AGENTS.md Policy   3/5  ║
╠══════════════════════════════════════════════════════════╣
║  [4/5] Clarity    Good: target FB named, outcome clear   ║
║  [5/5] Safety     Compliant: offline fixture only        ║
║  [4/5] SCL/PLC    Uses correct UDT and FB names          ║
║  [3/5] AGENTS.md  Missing: change-log mention            ║
╠══════════════════════════════════════════════════════════╣
║  ⚠ TOP ISSUE: Add a change-log note to the request      ║
║  💡 SUGGEST:  Append 'and record the change-log entry   ║
║               via write-change-log.ps1' to the prompt   ║
╚══════════════════════════════════════════════════════════╝
```

---

## Project structure

```
LLM-AS-A-JUDGE/
├── .codex/
│   ├── hooks.json                      ← Codex hook wiring (UserPromptSubmit)
│   └── hooks/
│       └── user_prompt_submit.py       ← Hook entry point called by Codex
├── judge/
│   ├── judge.py                        ← Core judge engine + formatter
│   └── rubrics.json                    ← Rubric definitions + score anchors
├── logs/
│   └── judge-YYYY-MM-DD.jsonl          ← Append-only evaluation logs
├── AGENTS.md                           ← Agent policy for this repo
└── README.md                           ← This file
```

---

## Setup

### 1. Install the OpenAI Python package

```bash
pip install openai
```

### 2. Set your API key

```bash
export OPENAI_API_KEY="sk-..."
```

Add this to your shell profile or Codex `.env` file so it's always available.

### 3. Wire the hook into Codex

Copy or symlink `.codex/hooks.json` into the Schlenker repo's `.codex/` directory:

```bash
cp /path/to/LLM-AS-A-JUDGE/.codex/hooks.json /path/to/Schlenker/.codex/hooks.json
```

Or run both repos side by side and point Codex at this repo's `.codex/` config.

### 4. Trust the hook in Codex

In the Codex IDE, run `/hooks` and trust the `user_prompt_submit.py` hook.
Codex requires a one-time trust step for project-local command hooks.

### 5. Verify

Submit a test prompt like:

```
Add a comment to FB_DoorAccess explaining the 11-door aggregate logic.
```

You should see the score banner appear in the Codex context pane before
the agent responds.

---

## Configuration

Set these environment variables to tune behaviour:

| Variable | Default | Description |
|---|---|---|
| `OPENAI_API_KEY` | — | Required. Your OpenAI API key. |
| `JUDGE_MODEL` | `gpt-5.6-sol` | Model used as the judge. |
| `JUDGE_THRESHOLD` | `0.0` (disabled) | Block prompts scoring below this value. Set e.g. `2.5` to block POOR prompts. |
| `JUDGE_MIN_WORDS` | `5` | Skip evaluation for prompts shorter than this. |
| `JUDGE_LOG_DIR` | `logs/` | Directory for JSONL evaluation logs. |

---

## How the scoring works

1. **Hard-violation filter** — instant, no API call. Matches patterns like
   `write to plc`, `d:\gx works\schlenker.gxw`, `skip the safety gate`.
   Triggers a `decision: block` response immediately.

2. **Judge LLM call** — sends the prompt + all four rubric definitions to
   `gpt-5.6-sol` at `temperature=0` with `response_format: json_object`.
   Returns `{"scores": {...}, "bonus": 0.0, "feedback": {...}, "top_issue": "...", "suggestion": "..."}`.

3. **Weighted score** — `overall = sum(score[dim] * weight[dim]) + bonus`.
   Bonus (+0.1 each, max +0.3) for prompts that explicitly reference policy
   clauses, safety-gate confirmations, or merge-register open items.

4. **additionalContext injection** — the banner is injected into the model's
   context window via the `hookSpecificOutput.additionalContext` field.
   Codex sees it as developer context before generating its response.

---

## Rubric sources

The rubrics in `judge/rubrics.json` are drawn from:

- **Siemens TIA Portal Programming Guideline** (doc 81318674, v14 EN)
  — naming conventions, FB/DB/OB structure, SCL coding style
- **IEC 61131-3 Ed.3** — language constructs, data types, execution model
- **ISA-88 / OMAC PackML** — state machine naming (Idle, Ready, Automatic…)
- **Siemens SIMATIC S7 SCL Reference Manual** — VAR section types,
  multi-instance declarations, OB call structure
- **Schlenker AGENTS.md** — safety gate, Computer Use contract,
  change-log policy, evidence requirements, protected-source rules
- **Schlenker REV12 Manifest & Merge Register** — confirmed hardware,
  retained FB inventory, open items

---

## Log format

Each JSONL line:

```json
{
  "ts":           "2026-08-05T05:12:39.123Z",
  "session_id":   "thr_abc123",
  "turn_id":      "turn_001",
  "prompt_len":   142,
  "blocked":      false,
  "block_reason": "",
  "elapsed_ms":   2341.5,
  "result": {
    "overall_score": 4.10,
    "label":         "GOOD",
    "scores": {
      "clarity":             4,
      "safety_compliance":   5,
      "scl_plc_specificity": 4,
      "agents_md_adherence": 3
    },
    "bonus": 0.0,
    "feedback": { ... },
    "top_issue":   "...",
    "suggestion":  "..."
  }
}
```

---

## Safety notes

- The hook **always exits 0** — it can never crash your Codex session.
- Hard blocks use `decision: block` — Codex stops the turn and shows the
  reason to the user. These only fire on direct safety-policy violations.
- Score-based blocks only fire when `JUDGE_THRESHOLD > 0` — disabled by default.
- The hook **never logs** full prompt text longer than 500 characters,
  API keys, credentials, or PLC addresses.
