from __future__ import annotations

import importlib.util
import json
import sys
import unittest
from pathlib import Path
from types import SimpleNamespace
from unittest.mock import patch

import judge


ROOT = Path(__file__).resolve().parents[1]
HOOK_PATH = ROOT / ".codex" / "hooks" / "user_prompt_submit.py"


def load_hook_module():
    spec = importlib.util.spec_from_file_location("schlenker_prompt_hook", HOOK_PATH)
    if spec is None or spec.loader is None:
        raise RuntimeError("Unable to load prompt hook")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class JudgeTests(unittest.TestCase):
    def test_versioned_config_is_valid(self):
        config = judge.load_config()
        self.assertEqual(config["model"], "gpt-5.6-terra")
        self.assertEqual(config["threshold"], 0.0)

    def test_weighted_score(self):
        scores = {
            "clarity": 4,
            "safety_compliance": 5,
            "scl_plc_specificity": 4,
            "agents_md_adherence": 3,
        }
        self.assertEqual(judge.compute_overall(scores, 0.1), 4.15)

    def test_structured_responses_request(self):
        judgment = {
            "scores": {dimension: 4 for dimension in judge.DIMENSION_WEIGHTS},
            "bonus": 0.0,
            "feedback": {
                dimension: "Clear and compliant."
                for dimension in judge.DIMENSION_WEIGHTS
            },
            "top_issue": "",
            "suggestion": "Name the acceptance criterion.",
        }
        captured = {}

        class FakeResponses:
            @staticmethod
            def create(**kwargs):
                captured.update(kwargs)
                return SimpleNamespace(
                    status="completed",
                    output_text=json.dumps(judgment),
                )

        class FakeClient:
            def __init__(self, **kwargs):
                captured["client"] = kwargs
                self.responses = FakeResponses()

        fake_openai = SimpleNamespace(OpenAI=FakeClient)
        with patch.dict(sys.modules, {"openai": fake_openai}):
            result = judge.call_judge("system", "user", "test-key")

        self.assertEqual(result, judgment)
        self.assertEqual(captured["model"], "gpt-5.6-terra")
        self.assertEqual(captured["reasoning"]["effort"], "low")
        self.assertEqual(
            captured["text"]["format"]["type"],
            "json_schema",
        )

    def test_hard_filter_blocks_bypass_not_safe_wording(self):
        hook = load_hook_module()
        self.assertIsNotNone(hook._hard_violation_check("skip the safety gate"))
        self.assertIsNone(
            hook._hard_violation_check(
                "Review the logic offline and do not write to PLC."
            )
        )


if __name__ == "__main__":
    unittest.main()
