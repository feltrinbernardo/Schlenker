#!/usr/bin/env python3
"""Local real-time monitor for Schlenker Agents API sessions.

The server binds to localhost, keeps the OpenAI API key server-side, and
forwards a bounded, normalized view of the official Agents API event stream to
the browser. It intentionally uses environment.type=none in this first phase:
the monitored agents cannot access the repository, desktop, or equipment.
"""

from __future__ import annotations

import argparse
import json
import os
import queue
import re
import threading
import time
import urllib.error
import urllib.request
from collections import deque
from dataclasses import dataclass, field
from datetime import datetime, timezone
from http import HTTPStatus
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from typing import Any, Iterable
from urllib.parse import urlparse


ROOT = Path(__file__).resolve().parents[2]
WEB_ROOT = Path(__file__).resolve().parent / "web"
ARCHITECTURE_PATH = ROOT / "config" / "agents" / "schlenker-architecture.json"
ROLE_FILES = (
    ROOT / "documentos" / "schlenker" / "agente-coordenador.md",
    ROOT / "documentos" / "schlenker" / "agente-plc-processo.md",
    ROOT / "documentos" / "schlenker" / "agente-hmi-alarmes.md",
    ROOT / "documentos" / "schlenker" / "agente-redes-dispositivos.md",
    ROOT / "documentos" / "schlenker" / "agente-seguranca-interfaces.md",
    ROOT / "documentos" / "schlenker" / "agente-verificacao-integracao.md",
)
API_URL = "https://api.openai.com/v1/agents/sessions"
MAX_EVENTS = 300
MAX_OUTPUT_CHARS = 50_000
SECRET_PATTERN = re.compile(r"sk-[A-Za-z0-9_*\-]{8,}")


def utc_now() -> str:
    return datetime.now(timezone.utc).isoformat()


def redact_secrets(value: str) -> str:
    """Remove API-key-shaped values from messages before browser exposure."""
    return SECRET_PATTERN.sub("[REDACTED_API_KEY]", value)


def load_architecture() -> dict[str, Any]:
    return json.loads(ARCHITECTURE_PATH.read_text(encoding="utf-8"))


def build_instructions() -> str:
    sections = []
    for path in ROLE_FILES:
        sections.append(f"\n--- ROLE DEFINITION: {path.name} ---\n")
        sections.append(path.read_text(encoding="utf-8"))
    return """You are the Schlenker coordinator in a monitored Agents API pilot.
Use the approved role definitions below. Organize and consolidate; delegate
bounded specialist work when useful. Do not claim access to local files,
desktop applications, PLCs, HMIs, networks, safety systems, or equipment: this
session has environment.type=none and no engineering tools. Do not simulate
observations. Distinguish documented statements, inference, simulation, and
pending work. Persistent memory, Railway, equipment operations, and safety
validation are outside scope. Only one specialist may conceptually own an
engineering interface at a time, although no such interface is connected in
this phase.
""" + "".join(sections)


def iter_sse_events(lines: Iterable[bytes]) -> Iterable[dict[str, Any]]:
    """Parse an SSE byte stream and yield JSON payloads from data fields."""
    data_lines: list[str] = []
    for raw_line in lines:
        line = raw_line.decode("utf-8", errors="replace").rstrip("\r\n")
        if not line:
            if data_lines:
                payload = "\n".join(data_lines)
                data_lines.clear()
                if payload == "[DONE]":
                    return
                try:
                    value = json.loads(payload)
                except json.JSONDecodeError:
                    value = {"type": "monitor.unparsed", "payload": payload}
                if isinstance(value, dict):
                    yield value
            continue
        if line.startswith(":"):
            continue
        if line.startswith("data:"):
            data_lines.append(line[5:].lstrip())
    if data_lines:
        payload = "\n".join(data_lines)
        if payload != "[DONE]":
            try:
                value = json.loads(payload)
            except json.JSONDecodeError:
                value = {"type": "monitor.unparsed", "payload": payload}
            if isinstance(value, dict):
                yield value


def first_nested(value: Any, keys: tuple[str, ...]) -> Any:
    if isinstance(value, dict):
        for key in keys:
            if key in value and value[key] not in (None, ""):
                return value[key]
        for child in value.values():
            found = first_nested(child, keys)
            if found not in (None, ""):
                return found
    elif isinstance(value, list):
        for child in value:
            found = first_nested(child, keys)
            if found not in (None, ""):
                return found
    return None


@dataclass
class MonitorState:
    lock: threading.RLock = field(default_factory=threading.RLock)
    listeners: list[queue.Queue] = field(default_factory=list)
    status: str = "idle"
    task: str = ""
    session_id: str | None = None
    started_at: str | None = None
    finished_at: str | None = None
    output: str = ""
    error: str | None = None
    agents: dict[str, dict[str, Any]] = field(default_factory=dict)
    events: deque = field(default_factory=lambda: deque(maxlen=MAX_EVENTS))

    def reset_for_task(self, task: str) -> None:
        with self.lock:
            self.status = "starting"
            self.task = task
            self.session_id = None
            self.started_at = utc_now()
            self.finished_at = None
            self.output = ""
            self.error = None
            self.agents = {
                "root": {
                    "id": "root",
                    "role": "coordinator",
                    "status": "starting",
                    "lastEvent": None,
                }
            }
            self.events.clear()

    def snapshot(self) -> dict[str, Any]:
        with self.lock:
            return {
                "status": self.status,
                "task": self.task,
                "sessionId": self.session_id,
                "startedAt": self.started_at,
                "finishedAt": self.finished_at,
                "output": self.output,
                "error": self.error,
                "agents": list(self.agents.values()),
                "events": list(self.events),
                "mode": "agents-api-environment-none",
                "limitations": [
                    "No local repository access",
                    "No desktop or engineering application access",
                    "No equipment access",
                    "No persistent memory or Railway integration",
                ],
            }

    def subscribe(self) -> queue.Queue:
        listener: queue.Queue = queue.Queue(maxsize=100)
        with self.lock:
            self.listeners.append(listener)
        return listener

    def unsubscribe(self, listener: queue.Queue) -> None:
        with self.lock:
            if listener in self.listeners:
                self.listeners.remove(listener)

    def publish(self, event: dict[str, Any]) -> None:
        event_type = str(event.get("type", "unknown"))
        now = utc_now()
        normalized = {
            "type": event_type,
            "time": now,
            "subagentId": first_nested(event.get("turn", event), ("subagent_id",)),
            "itemType": first_nested(event.get("item", event), ("item_type", "type")),
        }
        with self.lock:
            session_id = first_nested(event, ("session_id",))
            if session_id:
                self.session_id = str(session_id)

            if event_type == "agent.session.subagent.created":
                agent_id = first_nested(event, ("subagent_id", "agent_id", "id"))
                if agent_id:
                    agent_id = str(agent_id)
                    self.agents[agent_id] = {
                        "id": agent_id,
                        "role": "specialist",
                        "status": "created",
                        "lastEvent": event_type,
                    }

            subagent_id = normalized["subagentId"]
            agent_key = str(subagent_id) if subagent_id else "root"
            if agent_key not in self.agents:
                self.agents[agent_key] = {
                    "id": agent_key,
                    "role": "specialist" if subagent_id else "coordinator",
                    "status": "running",
                    "lastEvent": event_type,
                }
            agent = self.agents[agent_key]
            agent["lastEvent"] = event_type

            if event_type.endswith("turn.started") or event_type.endswith("turn.in_progress"):
                agent["status"] = "running"
                self.status = "running"
            elif event_type == "agent.session.turn.completed":
                agent["status"] = "completed"
                if not subagent_id:
                    self.status = "completed"
                    self.finished_at = now
            elif event_type.endswith("turn.failed"):
                agent["status"] = "failed"
                if not subagent_id:
                    self.status = "failed"
                    self.finished_at = now
            elif event_type.endswith("turn.cancelled"):
                agent["status"] = "cancelled"
                if not subagent_id:
                    self.status = "cancelled"
                    self.finished_at = now
            elif event_type in {"agent.session.failed", "agent.session.environment.failed", "error"}:
                self.status = "failed"
                self.finished_at = now

            if event_type == "agent.session.turn.output_text.delta":
                delta = event.get("delta", "")
                if isinstance(delta, str):
                    self.output = (self.output + delta)[-MAX_OUTPUT_CHARS:]
            elif event_type == "agent.session.turn.output_text.done":
                text = event.get("text")
                if isinstance(text, str):
                    self.output = text[-MAX_OUTPUT_CHARS:]

            self.events.append(normalized)
            listeners = tuple(self.listeners)

        message = {"kind": "event", "event": normalized, "state": self.snapshot()}
        for listener in listeners:
            try:
                listener.put_nowait(message)
            except queue.Full:
                pass

    def fail(self, message: str) -> None:
        with self.lock:
            self.status = "failed"
            self.error = redact_secrets(message)
            self.finished_at = utc_now()
            if "root" in self.agents:
                self.agents["root"]["status"] = "failed"
                self.agents["root"]["lastEvent"] = "monitor.failed"
        self.publish({"type": "monitor.failed"})


STATE = MonitorState()


def run_agents_task(task: str, model: str, max_subagents: int) -> None:
    key = os.environ.get("OPENAI_API_KEY")
    if not key:
        STATE.fail("OPENAI_API_KEY is not available in the server environment.")
        return

    payload = {
        "agent": {
            "model": model,
            "instructions": build_instructions(),
            "multi_agent": {
                "enabled": True,
                "max_concurrent_subagents": max_subagents,
            },
        },
        "environment": {"type": "none"},
        "input": task,
        "stream": True,
    }
    request = urllib.request.Request(
        API_URL,
        data=json.dumps(payload).encode("utf-8"),
        method="POST",
        headers={
            "Authorization": f"Bearer {key}",
            "Content-Type": "application/json",
            "Accept": "text/event-stream",
            "OpenAI-Beta": "agents=v1",
        },
    )
    try:
        with urllib.request.urlopen(request, timeout=300) as response:
            STATE.status = "running"
            for event in iter_sse_events(response):
                STATE.publish(event)
    except urllib.error.HTTPError as error:
        body = error.read().decode("utf-8", errors="replace")
        try:
            detail = json.loads(body).get("error", {}).get("message", body)
        except json.JSONDecodeError:
            detail = body
        STATE.fail(f"Agents API HTTP {error.code}: {detail}")
    except Exception as error:  # noqa: BLE001 - boundary must report safely
        STATE.fail(f"Agents API stream failed: {error}")


class MonitorHandler(BaseHTTPRequestHandler):
    server_version = "SchlenkerAgentMonitor/0.1"

    def log_message(self, format_string: str, *args: Any) -> None:
        print(f"[{self.log_date_time_string()}] {format_string % args}")

    def send_json(self, value: Any, status: int = HTTPStatus.OK) -> None:
        body = json.dumps(value, ensure_ascii=False).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(body)))
        self.send_header("Cache-Control", "no-store")
        self.end_headers()
        self.wfile.write(body)

    def serve_file(self, path: Path, content_type: str) -> None:
        if not path.is_file():
            self.send_error(HTTPStatus.NOT_FOUND)
            return
        body = path.read_bytes()
        self.send_response(HTTPStatus.OK)
        self.send_header("Content-Type", content_type)
        self.send_header("Content-Length", str(len(body)))
        self.send_header("Cache-Control", "no-store")
        self.end_headers()
        self.wfile.write(body)

    def do_GET(self) -> None:  # noqa: N802 - BaseHTTPRequestHandler contract
        route = urlparse(self.path).path
        if route == "/":
            self.serve_file(WEB_ROOT / "index.html", "text/html; charset=utf-8")
        elif route == "/app.js":
            self.serve_file(WEB_ROOT / "app.js", "text/javascript; charset=utf-8")
        elif route == "/styles.css":
            self.serve_file(WEB_ROOT / "styles.css", "text/css; charset=utf-8")
        elif route == "/api/state":
            self.send_json(STATE.snapshot())
        elif route == "/api/config":
            architecture = load_architecture()
            self.send_json(
                {
                    "roles": architecture["roles"],
                    "apiKeyPresent": bool(os.environ.get("OPENAI_API_KEY")),
                    "model": self.server.model,
                    "maxSubagents": self.server.max_subagents,
                    "environment": "none",
                }
            )
        elif route == "/api/events":
            self.stream_browser_events()
        else:
            self.send_error(HTTPStatus.NOT_FOUND)

    def do_POST(self) -> None:  # noqa: N802 - BaseHTTPRequestHandler contract
        route = urlparse(self.path).path
        if route != "/api/tasks":
            self.send_error(HTTPStatus.NOT_FOUND)
            return
        length = int(self.headers.get("Content-Length", "0"))
        if length <= 0 or length > 100_000:
            self.send_json({"error": "Invalid request size."}, HTTPStatus.BAD_REQUEST)
            return
        try:
            body = json.loads(self.rfile.read(length).decode("utf-8"))
        except (UnicodeDecodeError, json.JSONDecodeError):
            self.send_json({"error": "Invalid JSON."}, HTTPStatus.BAD_REQUEST)
            return
        task = str(body.get("task", "")).strip()
        if not task:
            self.send_json({"error": "Task is required."}, HTTPStatus.BAD_REQUEST)
            return
        if STATE.snapshot()["status"] in {"starting", "running"}:
            self.send_json({"error": "A task is already running."}, HTTPStatus.CONFLICT)
            return

        STATE.reset_for_task(task)
        thread = threading.Thread(
            target=run_agents_task,
            args=(task, self.server.model, self.server.max_subagents),
            name="agents-api-stream",
            daemon=True,
        )
        thread.start()
        self.send_json({"accepted": True, "state": STATE.snapshot()}, HTTPStatus.ACCEPTED)

    def stream_browser_events(self) -> None:
        listener = STATE.subscribe()
        self.send_response(HTTPStatus.OK)
        self.send_header("Content-Type", "text/event-stream")
        self.send_header("Cache-Control", "no-cache")
        self.send_header("Connection", "keep-alive")
        self.end_headers()
        try:
            initial = json.dumps(
                {"kind": "snapshot", "state": STATE.snapshot()}, ensure_ascii=False
            )
            self.wfile.write(f"data: {initial}\n\n".encode("utf-8"))
            self.wfile.flush()
            while True:
                try:
                    message = listener.get(timeout=15)
                    data = json.dumps(message, ensure_ascii=False)
                    self.wfile.write(f"data: {data}\n\n".encode("utf-8"))
                except queue.Empty:
                    self.wfile.write(b": keep-alive\n\n")
                self.wfile.flush()
        except (BrokenPipeError, ConnectionResetError):
            pass
        finally:
            STATE.unsubscribe(listener)


class MonitorServer(ThreadingHTTPServer):
    daemon_threads = True

    def __init__(self, address: tuple[str, int], model: str, max_subagents: int):
        super().__init__(address, MonitorHandler)
        self.model = model
        self.max_subagents = max_subagents


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=8765)
    parser.add_argument(
        "--model", default=os.environ.get("SCHLENKER_AGENT_MODEL", "gpt-6-astra")
    )
    parser.add_argument(
        "--max-subagents",
        type=int,
        default=int(os.environ.get("SCHLENKER_MAX_SUBAGENTS", "4")),
    )
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    if args.host not in {"127.0.0.1", "localhost", "::1"}:
        raise SystemExit("This pilot monitor may bind only to localhost.")
    if not 1 <= args.max_subagents <= 4:
        raise SystemExit("--max-subagents must be between 1 and 4.")
    server = MonitorServer((args.host, args.port), args.model, args.max_subagents)
    print(f"Schlenker Agent Monitor: http://{args.host}:{args.port}")
    print("Mode: Agents API / environment none / no equipment access")
    server.serve_forever()


if __name__ == "__main__":
    main()
