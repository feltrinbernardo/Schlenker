import importlib.util
import json
import sys
import unittest
from pathlib import Path


MODULE_PATH = Path(__file__).resolve().parents[1] / "server.py"
SPEC = importlib.util.spec_from_file_location("schlenker_agent_monitor", MODULE_PATH)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
sys.modules[SPEC.name] = MODULE
SPEC.loader.exec_module(MODULE)


class SseParserTests(unittest.TestCase):
    def test_parses_multiple_json_events(self):
        source = [
            b"event: message\n",
            b'data: {"type":"agent.session.turn.started"}\n',
            b"\n",
            b'data: {"type":"agent.session.turn.completed","turn":{"subagent_id":null}}\n',
            b"\n",
        ]
        events = list(MODULE.iter_sse_events(source))
        self.assertEqual(2, len(events))
        self.assertEqual("agent.session.turn.started", events[0]["type"])
        self.assertIsNone(events[1]["turn"]["subagent_id"])

    def test_joins_multiline_data(self):
        payload = {"type": "custom", "value": "ok"}
        encoded = json.dumps(payload)
        source = [f"data: {encoded}\n".encode(), b"\n"]
        self.assertEqual(payload, list(MODULE.iter_sse_events(source))[0])


class MonitorStateTests(unittest.TestCase):
    def test_tracks_subagent_and_root_completion(self):
        monitor = MODULE.MonitorState()
        monitor.reset_for_task("test")
        monitor.publish(
            {"type": "agent.session.subagent.created", "subagent_id": "agent_1"}
        )
        monitor.publish(
            {
                "type": "agent.session.turn.completed",
                "turn": {"subagent_id": "agent_1"},
            }
        )
        self.assertEqual("completed", monitor.agents["agent_1"]["status"])
        self.assertNotEqual("completed", monitor.status)
        monitor.publish(
            {
                "type": "agent.session.turn.completed",
                "turn": {"subagent_id": None},
            }
        )
        self.assertEqual("completed", monitor.status)

    def test_appends_output_delta(self):
        monitor = MODULE.MonitorState()
        monitor.reset_for_task("test")
        monitor.publish(
            {"type": "agent.session.turn.output_text.delta", "delta": "hello"}
        )
        self.assertEqual("hello", monitor.output)


class ConfigurationTests(unittest.TestCase):
    def test_architecture_and_role_files_exist(self):
        architecture = MODULE.load_architecture()
        self.assertEqual(6, len(architecture["roles"]))
        for path in MODULE.ROLE_FILES:
            self.assertTrue(path.is_file(), path)

    def test_redacts_api_keys_from_errors(self):
        value = MODULE.redact_secrets("bad key sk-proj-1234567890_SECRET")
        self.assertNotIn("1234567890", value)
        self.assertIn("[REDACTED_API_KEY]", value)


if __name__ == "__main__":
    unittest.main()
