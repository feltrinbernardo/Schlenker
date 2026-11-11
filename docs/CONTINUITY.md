# Continuidade — Schlenker

Este registro resume o trabalho conhecido; não concede autorização e não prova
qual projeto está aberto no TIA. Leia o AGENTS.md e confira o checkout antes de
retomar. O histórico deste arquivo no Git preserva versões anteriores.

## Escopo e revisão observados

- Atualizado em: 2026-11-11, Australia/Melbourne (UTC+11).
- Escopo: checkout local `schlenker-operational-fix`, fora do laptop da bancada.
- Branch observada: `codex/batch-failure-diagnostics`.
- Base: `7bf2d2043b7209eecac57008b584f7edf60ce2c2`.
- Este documento integra o pacote de correções preparado para publicação na
  branch acima, com autorização explícita do usuário para commit/push. Confirme
  o commit que o contém em `git log` e sua presença no remoto. Não assumir merge
  em `codex/revised-open-points` nem instalação na bancada.
- Estado atual do TIA, executáveis e equipamentos da bancada: não observado.

## Objetivo e decisões

Fonte: pedidos explícitos do usuário neste chat em 2026-11-11; registro das
implementações em [change-log](../logs/change-log/2026-11-11.md).

- Melhorar a execução existente com mudanças pequenas, reversíveis e testes
  sem TIA. Não reconstruir a arquitetura nem adicionar agentes.
- Implementar tratamento de falhas nos lotes/diagnóstico, controle de acesso
  concorrente e agrupamento dos ajustes SMC. Não alterar `.ap19`, compilar no
  TIA nem realizar operações online nesta etapa.
- Atualizar o checkpoint horário para preservar decisões, descobertas, testes e
  próximo passo, permitindo continuidade em outro chat. Preservar a rotina
  existente; o pedido posterior autorizou publicar o pacote local de correções e preparar
  o prompt de ativação, sem executar agora a rotina horária da bancada.

## Alterações e descobertas

1. **Lotes/diagnóstico:** os dois runners `scripts/run-global-common-*-page-transactions.ps1`
   interrompem em falha ou auditoria incompleta e exigem saída vazia. Helpers
   relevantes registram fases e diagnóstico. Evita tratar uma etapa incompleta
   como sucesso; não adiciona retries automáticos.
2. **Concorrência:** `scripts/tia-project-lease.ps1` protege os runners
   participantes; helpers corrigidos usam ExclusiveAccess durante seu acesso.
   Marcadores após interrupção exigem reconciliação. Não é um bloqueio universal
   para qualquer ferramenta, usuário, alias de caminho ou clique na GUI.
3. **SMC:** `RefineSmcReadability --with-state-backs` aplica as etapas existentes
   RefineSmcReadability e MatchSmcStateBacks com uma sessão, uma compilação e um
   salvamento finais. Mantém guards e baseline restrito; falhas podem deixar
   alterações parciais em memória. Não reaplicar a migração em projeto já migrado.
   Rebuild requer ambos os fontes e seleção do Main. Detalhes:
   [README dos helpers](../tools/tia-v19-openness/README.md).
4. **Memória:** commits preservam arquivos, não o conteúdo inteiro da conversa.
   O projeto nativo TIA tem procedimento/cofre separado, descrito em
   [versionamento TIA](../tia-project-versioning/README.md). Não foi conferida a
   execução recente de backups nem dos checkpoints horários na bancada.

## Testes e limites da evidência

Resultados abaixo foram observados nesta conversa contra o working tree local,
antes de esta atualização documental; são relatos de execução, não logs brutos
versionados. Ver detalhes das correções no change-log acima.

- `scripts/test-batch-transactions.py`: 18 cenários passaram com fixtures POSIX.
- `scripts/test-openness-diagnostics.ps1`: 3 cenários passaram com tipos simulados.
- `scripts/test-project-exclusivity.py`: 3 testes passaram, incluindo processos
  concorrentes e marcador de crash.
- `scripts/test-openness-exclusivity.ps1`: 4 cenários passaram com APIs simuladas.
- `scripts/test-smc-session.ps1`: 15 cenários passaram; sequência, contagem de
  operações, falhas, cancelamentos e liberação de recursos.
- `node scripts/test-smc-logical-hmi.cjs`: 134 execuções dos scripts passaram.
- `scripts/test-repository.ps1`: passou após a terceira correção.
- `scripts/test-prompt-judge.ps1`: na verificação anterior, 43/44 testes passaram.
  `test_recognized_shell_write_outside_repository_is_denied` falhou no macOS com
  caminho Windows `C:\temp\outside.txt`; a falha foi reproduzida no teste isolado
  da base HEAD. Não foi corrigida nem contornada nesta tarefa.
- Nenhuma compilação Siemens, execução TIA, transferência ou validação física.
  Redução de chamadas no fluxo SMC foi simulada; latência real não foi medida.

## Checkpoint com continuidade — arquivos preparados, ativação pendente

- `AGENTS.md` orienta o dono da tarefa a registrar fatos enquanto o chat está
  disponível e orienta a nova conversa a ler este documento.
- A skill canônica `.agents/skills/schlenker-repo-checkpoint/SKILL.md` consolida
  informações com fontes antes do commit, preserva lacunas e evita sobrescrever
  escrita concorrente ou gerar commits vazios apenas por passagem de tempo.
- A automação Schlenker e a skill instalada no laptop Windows não estão
  acessíveis neste ambiente. O inventário local de automações/projetos não
  apresentou a bancada. Não foi criada outra automação nem alterado o horário.

- Verificação desta atualização: `quick_validate.py` aprovou a skill;
  `scripts/test-repository.ps1` passou. A suíte `test-prompt-judge.ps1` voltou a
  apresentar 43/44 passes e a mesma falha de caminho Windows no macOS descrita
  acima. Não foi adicionado código de execução nem teste de engenharia.

## Pendências e próximo passo

1. Ativar a atualização na bancada após publicação; os arquivos locais foram
   verificados, mas a agenda e a skill instalada não foram atualizadas daqui.
2. Seguir o [prompt de ativação](BENCH_UPDATE_PROMPT.md) na bancada, após
   confirmar o commit remoto. Não confundir a branch de correções com o destino
   autorizado do checkpoint horário; a publicação desta branch não é um merge.
3. Após disponibilizar os arquivos na bancada, sincronizar a skill instalada em
   `C:/Users/SIMATIC User/.codex/skills/schlenker-repo-checkpoint/SKILL.md` com a
   cópia canônica e verificar hashes. No prompt da automação horária existente,
   exigir leitura da skill do repositório a cada execução e atualização de
   `docs/CONTINUITY.md` antes do commit. Preservar agenda, branch e remoto.
4. Verificar uma execução real do checkpoint: handoff com fontes, commit e push
   comprovados; não declarar a ativação apenas porque estes arquivos existem.
5. Em uma nova conversa, conferir checkout/revisão, ler este registro e resolver
   lacunas pelas fontes. Rebuild dos helpers e teste Windows/V19 permanecem
   pendentes; nenhuma operação online está autorizada por este documento.
