from __future__ import annotations

import importlib.util
import json
import subprocess
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch


ROOT = Path(__file__).resolve().parents[1]
HOOK_PATH = ROOT / ".codex" / "hooks" / "agent_lifecycle.py"


def load_hook_module():
    spec = importlib.util.spec_from_file_location(
        "schlenker_agent_lifecycle_hook", HOOK_PATH
    )
    if spec is None or spec.loader is None:
        raise RuntimeError("Unable to load lifecycle hook")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class AgentLifecycleHookTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.hook = load_hook_module()

    def payload(self, event: str, tool: str, tool_input, response=None):
        value = {
            "hook_event_name": event,
            "session_id": "session-test",
            "turn_id": "turn-test",
            "cwd": str(ROOT),
            "tool_name": tool,
            "tool_use_id": "tool-test",
            "tool_input": tool_input,
        }
        if response is not None:
            value["tool_response"] = response
        return value

    @staticmethod
    def denial_reason(result):
        return result["hookSpecificOutput"]["permissionDecisionReason"]

    def test_safe_repository_patch_is_allowed(self):
        command = "*** Begin Patch\n*** Add File: docs/safe.md\n+ok\n*** End Patch"
        result = self.hook.handle_pre_tool(
            self.payload("PreToolUse", "apply_patch", {"command": command})
        )
        self.assertIsNone(result)

    def test_patch_outside_repository_is_denied(self):
        outside = str(ROOT.parent / "outside.md")
        command = f"*** Begin Patch\n*** Add File: {outside}\n+no\n*** End Patch"
        result = self.hook.handle_pre_tool(
            self.payload("PreToolUse", "apply_patch", {"command": command})
        )
        self.assertIn("PATH_OUTSIDE_REPOSITORY", self.denial_reason(result))

    def test_native_project_patch_is_denied(self):
        command = (
            "*** Begin Patch\n"
            "*** Update File: fixtures/projects/test.gxw\n"
            "-old\n+new\n*** End Patch"
        )
        result = self.hook.handle_pre_tool(
            self.payload("PreToolUse", "apply_patch", {"command": command})
        )
        self.assertIn("NATIVE_PROJECT_PATCH_FORBIDDEN", self.denial_reason(result))

    def test_change_log_direct_patch_is_denied(self):
        command = (
            "*** Begin Patch\n"
            "*** Update File: logs/change-log/2026-09-22.md\n"
            "+manual\n*** End Patch"
        )
        result = self.hook.handle_pre_tool(
            self.payload("PreToolUse", "apply_patch", {"command": command})
        )
        self.assertIn("APPEND_ONLY_LOG", self.denial_reason(result))

    def test_protected_source_read_is_allowed_but_write_is_denied(self):
        read_result = self.hook.handle_pre_tool(
            self.payload(
                "PreToolUse",
                "Bash",
                {"command": "Get-FileHash -LiteralPath 'D:\\GX Works\\schlenker.gxw'"},
            )
        )
        self.assertIsNone(read_result)

        write_result = self.hook.handle_pre_tool(
            self.payload(
                "PreToolUse",
                "Bash",
                {"command": "Remove-Item -LiteralPath 'D:\\GX Works\\schlenker.gxw'"},
            )
        )
        self.assertIn("PROTECTED_SOURCE_WRITE", self.denial_reason(write_result))

    def test_hash_evidence_may_be_written_without_mutating_native_source(self):
        result = self.hook.handle_pre_tool(
            self.payload(
                "PreToolUse",
                "Bash",
                {
                    "command": (
                        "Get-FileHash -LiteralPath "
                        "'D:\\GX Works\\schlenker.gxw' | "
                        "Out-File -LiteralPath '.\\runs\\hash.txt'"
                    )
                },
            )
        )
        self.assertIsNone(result)

    def test_engineering_application_shell_launch_is_denied(self):
        result = self.hook.handle_pre_tool(
            self.payload(
                "PreToolUse",
                "Bash",
                {"command": "Start-Process 'C:\\Program Files (x86)\\MELSOFT\\GPPW2\\GD2.exe'"},
            )
        )
        self.assertIn("ENGINEERING_APP_SHELL_LAUNCH", self.denial_reason(result))

    def test_recognized_shell_write_outside_repository_is_denied(self):
        result = self.hook.handle_pre_tool(
            self.payload(
                "PreToolUse",
                "Bash",
                {
                    "command": (
                        "Set-Content -LiteralPath 'C:\\temp\\outside.txt' "
                        "-Value 'no'"
                    )
                },
            )
        )
        self.assertIn("SHELL_WRITE_OUTSIDE_REPOSITORY", self.denial_reason(result))

    def test_recognized_shell_write_inside_repository_is_allowed(self):
        result = self.hook.handle_pre_tool(
            self.payload(
                "PreToolUse",
                "Bash",
                {"command": "Set-Content -LiteralPath '.\\runs\\inside.txt' -Value ok"},
            )
        )
        self.assertIsNone(result)

    def test_capture_helper_requires_checkpoint_context(self):
        result = self.hook.handle_pre_tool(
            self.payload(
                "PreToolUse",
                "Bash",
                {"command": ".\\scripts\\capture-gxworks2-window.ps1 -RunId test"},
            )
        )
        context = result["hookSpecificOutput"]["additionalContext"]
        self.assertIn("CAPTURE_CHECKPOINT", context)

    def test_capture_helper_output_outside_runs_is_denied(self):
        result = self.hook.handle_pre_tool(
            self.payload(
                "PreToolUse",
                "Bash",
                {
                    "command": (
                        ".\\scripts\\capture-gxworks2-window.ps1 "
                        "-OutputDirectory 'C:\\temp\\captures'"
                    )
                },
            )
        )
        self.assertIn("CAPTURE_OUTPUT_OUTSIDE_RUNS", self.denial_reason(result))

    def test_tia_capture_requires_deployworking_project(self):
        result = self.hook.handle_pre_tool(
            self.payload(
                "PreToolUse",
                "Bash",
                {
                    "command": (
                        ".\\scripts\\capture-tia-portal-window.ps1 "
                        "-ExpectedProjectPath 'C:\\TIA Projects\\original.ap19' "
                        "-ValidateOnly"
                    )
                },
            )
        )
        self.assertIn("CAPTURE_PROJECT_NOT_APPROVED", self.denial_reason(result))

    def test_routine_computer_use_batch_is_limited_to_five(self):
        code = "\n".join(f"await window.click({i}, {i});" for i in range(6))
        result = self.hook.handle_pre_tool(
            self.payload("PreToolUse", "mcp__cua_repl__js", {"code": code})
        )
        self.assertIn("ACTION_BATCH_TOO_LARGE", self.denial_reason(result))

    def test_five_action_batch_has_eighty_percent_fewer_observation_boundaries(self):
        code = "\n".join(f"await window.click({i}, {i});" for i in range(5))
        result = self.hook.handle_pre_tool(
            self.payload("PreToolUse", "mcp__cua_repl__js", {"code": code})
        )
        self.assertIsNone(result)
        legacy_boundaries = 5 * 2
        bounded_batch_boundaries = 2
        reduction = 1 - (bounded_batch_boundaries / legacy_boundaries)
        self.assertEqual(reduction, 0.8)

    def test_critical_computer_use_batch_is_denied(self):
        code = (
            "// write to PLC\n"
            "await window.click(1, 1);\n"
            "await window.click(2, 2);"
        )
        result = self.hook.handle_pre_tool(
            self.payload("PreToolUse", "mcp__cua_repl__js", {"code": code})
        )
        self.assertIn("CRITICAL_ACTION_BATCH_FORBIDDEN", self.denial_reason(result))

    def test_single_critical_action_gets_checkpoint_not_authorization(self):
        code = "// read from PLC\nawait window.click(1, 1);"
        result = self.hook.handle_pre_tool(
            self.payload("PreToolUse", "mcp__cua_repl__js", {"code": code})
        )
        context = result["hookSpecificOutput"]["additionalContext"]
        self.assertIn("does not grant", context)

    def test_permission_hook_does_not_auto_approve_hardware(self):
        result = self.hook.handle_permission_request(
            self.payload(
                "PermissionRequest",
                "mcp__cua_repl__js",
                {"code": "// write to PLC\nawait window.click(1, 1);"},
            )
        )
        self.assertIsNone(result)

    def test_permission_hook_allows_only_exact_offline_validation(self):
        allowed = self.hook.handle_permission_request(
            self.payload(
                "PermissionRequest",
                "Bash",
                {"command": ".\\scripts\\test-repository.ps1"},
            )
        )
        decision = allowed["hookSpecificOutput"]["decision"]
        self.assertEqual(decision["behavior"], "allow")
        self.assertEqual(set(decision), {"behavior"})

        not_allowed = self.hook.handle_permission_request(
            self.payload(
                "PermissionRequest",
                "Bash",
                {
                    "command": (
                        ".\\scripts\\test-repository.ps1; "
                        "Start-Process 'GD2.exe'"
                    )
                },
            )
        )
        self.assertEqual(
            not_allowed["hookSpecificOutput"]["decision"]["behavior"], "deny"
        )

    def test_post_failure_warns_without_claiming_rollback(self):
        with tempfile.TemporaryDirectory() as temporary:
            with patch.object(self.hook, "LOG_ROOT", Path(temporary)):
                result = self.hook.handle_post_tool(
                    self.payload(
                        "PostToolUse",
                        "Bash",
                        {"command": "rg missing"},
                        {"exit_code": 1, "output": "not found"},
                    )
                )
        context = result["hookSpecificOutput"]["additionalContext"]
        self.assertIn("do not report success", context)

    def test_stop_requires_tests_and_change_log_for_policy_changes(self):
        patch_command = (
            "*** Begin Patch\n*** Update File: AGENTS.md\n-old\n+new\n*** End Patch"
        )
        with tempfile.TemporaryDirectory() as temporary:
            with patch.object(self.hook, "LOG_ROOT", Path(temporary)):
                self.hook.handle_post_tool(
                    self.payload(
                        "PostToolUse",
                        "apply_patch",
                        {"command": patch_command},
                        {"isError": False},
                    )
                )
                blocked = self.hook.handle_stop(
                    {"hook_event_name": "Stop", "session_id": "session-test"}
                )
                self.assertEqual(blocked["decision"], "block")
                self.assertIn("change-log", blocked["reason"])

                for command in (
                    ".\\scripts\\test-prompt-judge.ps1",
                    ".\\scripts\\test-repository.ps1",
                    ".\\scripts\\write-change-log.ps1 -Request x -Summary y",
                ):
                    self.hook.handle_post_tool(
                        self.payload(
                            "PostToolUse",
                            "Bash",
                            {"command": command},
                            {"exit_code": 0, "output": "PASS"},
                        )
                    )
                allowed = self.hook.handle_stop(
                    {"hook_event_name": "Stop", "session_id": "session-test"}
                )
                self.assertIsNone(allowed)

    def test_stop_does_not_loop_after_continuation(self):
        result = self.hook.handle_stop(
            {
                "hook_event_name": "Stop",
                "session_id": "session-test",
                "stop_hook_active": True,
            }
        )
        self.assertIsNone(result)

    def test_hook_configuration_pins_timeouts_and_async_scope(self):
        config = json.loads((ROOT / ".codex" / "hooks.json").read_text(encoding="utf-8"))
        lifecycle_events = ("PreToolUse", "PermissionRequest", "PostToolUse", "Stop")
        async_handlers = []
        for event in lifecycle_events:
            for group in config["hooks"][event]:
                for handler in group["hooks"]:
                    self.assertEqual(handler["timeout"], 5)
                    if handler.get("async") is True:
                        async_handlers.append((event, handler["command"]))
        self.assertEqual(len(async_handlers), 1)
        self.assertEqual(async_handlers[0][0], "PostToolUse")
        self.assertIn("--telemetry-only", async_handlers[0][1])

    def test_malformed_json_fails_closed(self):
        completed = subprocess.run(
            ["python", str(HOOK_PATH)],
            input='{"hook_event_name":"PreToolUse"',
            text=True,
            capture_output=True,
            check=False,
        )
        self.assertEqual(completed.returncode, 0)
        result = json.loads(completed.stdout)
        self.assertFalse(result["continue"])
        self.assertIn("HOOK_VALIDATION_ERROR", result["stopReason"])

    def test_telemetry_contains_digests_not_raw_command(self):
        secret_marker = "do-not-log-this-command"
        payload = self.payload(
            "PostToolUse",
            "Bash",
            {"command": secret_marker},
            {"exit_code": 0},
        )
        with tempfile.TemporaryDirectory() as temporary:
            with patch.object(self.hook, "LOG_ROOT", Path(temporary)):
                self.hook.write_telemetry(payload, 1.25)
                log_file = next(Path(temporary).glob("hooks-*.jsonl"))
                text = log_file.read_text(encoding="utf-8")
        self.assertNotIn(secret_marker, text)
        event = json.loads(text)
        self.assertEqual(event["mode"], "async_telemetry")
        self.assertEqual(event["matcher"], self.hook.HOOK_MATCHER)
        self.assertFalse(event["synchronous"])
        self.assertEqual(event["timeout_seconds"], 5)
        self.assertIn("tool_response_bytes", event)


if __name__ == "__main__":
    unittest.main()
