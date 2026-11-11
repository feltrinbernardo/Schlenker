const state = {
  config: null,
  run: null,
};

const $ = (selector) => document.querySelector(selector);

function escapeHtml(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

function statusClass(status) {
  return ["completed"].includes(status)
    ? "ok"
    : ["failed", "cancelled"].includes(status)
      ? "bad"
      : ["running", "starting", "created"].includes(status)
        ? "active"
        : "idle";
}

function renderConfig(config) {
  state.config = config;
  $("#runtimeInfo").textContent = `${config.model} · até ${config.maxSubagents} subagentes · environment none`;
  $("#roleCount").textContent = config.roles.length;
  $("#roleList").innerHTML = config.roles
    .map(
      (role) => `
        <article class="role-card">
          <span class="role-state ${escapeHtml(statusClass(role.runtimeState))}"></span>
          <div>
            <strong>${escapeHtml(role.id)}</strong>
            <p>${escapeHtml(role.responsibility)}</p>
            <small>${escapeHtml(role.runtimeState)}</small>
          </div>
        </article>`,
    )
    .join("");

  $("#connectionDot").className = `dot ${config.apiKeyPresent ? "dot-ok" : "dot-bad"}`;
  $("#connectionText").textContent = config.apiKeyPresent
    ? "Monitor local conectado · chave disponível no servidor"
    : "Monitor conectado · OPENAI_API_KEY ausente";
}

function renderRun(run) {
  state.run = run;
  const status = run.status || "idle";
  const statusEl = $("#runStatus");
  statusEl.textContent = status.toUpperCase();
  statusEl.className = `status status-${statusClass(status)}`;
  $("#sessionId").textContent = run.sessionId || "Sem sessão";
  $("#agentCount").textContent = run.agents?.length || 0;
  $("#eventCount").textContent = run.events?.length || 0;
  $("#runButton").disabled = ["starting", "running"].includes(status);

  const agents = run.agents || [];
  $("#agentList").className = `agent-list ${agents.length ? "" : "empty"}`;
  $("#agentList").innerHTML = agents.length
    ? agents
        .map(
          (agent) => `
            <article class="agent-card">
              <span class="agent-pulse ${escapeHtml(statusClass(agent.status))}"></span>
              <div>
                <strong>${escapeHtml(agent.role)}</strong>
                <code>${escapeHtml(agent.id)}</code>
                <small>${escapeHtml(agent.status)} · ${escapeHtml(agent.lastEvent || "aguardando")}</small>
              </div>
            </article>`,
        )
        .join("")
    : "Nenhum agente em execução.";

  const events = (run.events || []).slice().reverse();
  $("#eventList").className = `event-list ${events.length ? "" : "empty"}`;
  $("#eventList").innerHTML = events.length
    ? events
        .map(
          (event) => `
            <li>
              <time>${escapeHtml(new Date(event.time).toLocaleTimeString())}</time>
              <div>
                <strong>${escapeHtml(event.type)}</strong>
                <small>${escapeHtml(event.subagentId || "coordinator")}</small>
              </div>
            </li>`,
        )
        .join("")
    : "<li>Aguardando eventos.</li>";

  $("#output").textContent = run.output || "Nenhuma saída recebida.";
  const error = $("#errorMessage");
  error.hidden = !run.error;
  error.textContent = run.error || "";
}

async function loadInitialState() {
  const [configResponse, stateResponse] = await Promise.all([
    fetch("/api/config"),
    fetch("/api/state"),
  ]);
  if (!configResponse.ok || !stateResponse.ok) {
    throw new Error("Não foi possível carregar o monitor local.");
  }
  renderConfig(await configResponse.json());
  renderRun(await stateResponse.json());
}

function connectEvents() {
  const source = new EventSource("/api/events");
  source.onmessage = (message) => {
    const payload = JSON.parse(message.data);
    if (payload.state) renderRun(payload.state);
  };
  source.onerror = () => {
    $("#connectionDot").className = "dot dot-warn";
    $("#connectionText").textContent = "Reconectando ao event stream local...";
  };
  source.onopen = () => {
    if (state.config) renderConfig(state.config);
  };
}

$("#taskForm").addEventListener("submit", async (event) => {
  event.preventDefault();
  const task = $("#taskInput").value.trim();
  if (!task) return;
  $("#runButton").disabled = true;
  const response = await fetch("/api/tasks", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ task }),
  });
  const payload = await response.json();
  if (!response.ok) {
    $("#runButton").disabled = false;
    alert(payload.error || "Falha ao iniciar a tarefa.");
    return;
  }
  renderRun(payload.state);
});

loadInitialState()
  .then(connectEvents)
  .catch((error) => {
    $("#connectionDot").className = "dot dot-bad";
    $("#connectionText").textContent = error.message;
  });
