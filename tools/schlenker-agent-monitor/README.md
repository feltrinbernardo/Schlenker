# Schlenker Agent Monitor

Painel local para acompanhar sessões multiagente executadas pela OpenAI Agents
API. O backend consome o event stream oficial e mostra agentes, estados, eventos
de coordenação e saída consolidada em tempo real.

## Limite desta primeira fase

O monitor cria sessões com `environment.type = none`. Os agentes não têm acesso
ao repositório local, TIA Portal, GX Works2, PLC, HMI ou equipamentos. Esse
limite é exibido permanentemente na interface e não deve ser confundido com a
arquitetura local usada pelo Codex Desktop.

Memória persistente e Railway não são configurados nesta fase.

## Pré-requisitos

- Python 3.11 ou superior.
- `OPENAI_API_KEY` disponível somente no ambiente do servidor.
- Chave com `api.agents.read`, `api.agents.write` e `api.responses.write`.

Nenhuma chave é lida do navegador, gravada em arquivo ou devolvida pelas APIs
locais.

## Executar

```powershell
python tools/schlenker-agent-monitor/server.py
```

Abra `http://127.0.0.1:8765`.

O modelo padrão segue o exemplo atual da documentação oficial. Para utilizar um
modelo habilitado no projeto sem alterar arquivos:

```powershell
$env:SCHLENKER_AGENT_MODEL = "modelo-habilitado"
python tools/schlenker-agent-monitor/server.py
```

O servidor rejeita bind fora de localhost e aceita no máximo quatro subagentes.

## Testes

```powershell
python -m unittest discover -s tools/schlenker-agent-monitor/tests -v
```

## Referências oficiais

- <https://developers.openai.com/api/docs/guides/agents-api/quickstart>
- <https://developers.openai.com/api/docs/guides/agents-api/multi-agent>
- <https://developers.openai.com/api/docs/guides/agents-api/sessions/events>
- <https://developers.openai.com/api/docs/guides/agents-api/observability>
