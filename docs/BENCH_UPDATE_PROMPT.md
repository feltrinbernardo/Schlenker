# Prompt de atualização e ativação na bancada

Cole o texto abaixo no Codex aberto na pasta do Schlenker no laptop da bancada.
O conteúdo também pode ser usado em um fork, desde que o ambiente e a pasta
sejam os da bancada. Histórico de chat não confirma estado local ou autorizações.

---

Atualize e prepare a execução do Schlenker com as correções publicadas em
`origin/codex/batch-failure-diagnostics`, remoto
`https://github.com/feltrinbernardo/Schlenker.git`.

Escopo: atualizar código/instruções, reconstruir somente os executáveis helpers
C#, verificar testes offline e atualizar a automação horária já existente.
Não abra nem opere o TIA, não faça attach a processos TIA, não execute helpers
contra projetos, não compile projeto TIA, não altere `.ap19`, não faça downloads
para equipamentos nem operações online. O build C# não autoriza executar o binário.
Não crie novos agentes ou automações duplicadas.

1. Leia o AGENTS.md aplicável. Confira diretório, branch, remoto de fetch/push,
   alterações locais, índice e processos que possam estar usando helpers. Preserve
   todo o trabalho existente. Faça fetch da branch de correções; registre seu SHA.
   O destino esperado da bancada é `codex/revised-open-points`. Não troque de
   branch automaticamente se estiver em outra. Com working tree e índice limpos,
   sem operação Git em andamento e sem tarefa concorrente, integre a atualização
   somente por fast-forward: primeiro atualize o destino com seu upstream e depois
   use `git merge --ff-only origin/codex/batch-failure-diagnostics` se ainda necessário.
   Se já estiver integrado, confirme por ancestralidade. Se houver divergência,
   alterações locais ou conflito, pare essa integração e informe os commits e
   caminhos envolvidos; não faça reset, stash, rebase, force-push ou sobrescrita.
   Não publique automaticamente a branch da bancada nesta atualização.

2. Leia novamente AGENTS.md, docs/CONTINUITY.md, a skill canônica
   `.agents/skills/schlenker-repo-checkpoint/SKILL.md` e
   `tools/tia-v19-openness/README.md` já atualizados. Confirme a revisão de cada
   item necessário e diferencie informações deste chat de estado observado na
   bancada. Use os registros para retomar; investigue somente as lacunas relevantes.

3. Sincronize a skill instalada do checkpoint com a cópia canônica do repositório,
   preservando uma cópia anterior fora do Git. Confira CODEX_HOME e a localização
   efetiva; o caminho documentado é
   `C:/Users/SIMATIC User/.codex/skills/schlenker-repo-checkpoint/SKILL.md`.
   Se houver customizações locais divergentes, compare e preserve-as antes de
   substituir. Verifique SHA-256 idêntico entre origem e destino ao concluir.

4. Localize a automação horária EXISTENTE usando os recursos do Codex. Confira
   sua identidade, projeto, diretório, branch e remoto. Preserve frequência,
   modelo, notificações e demais configurações. Atualize o prompt para exigir,
   em cada execução, leitura da skill canônica do repositório e consolidação de
   docs/CONTINUITY.md antes do commit. Mantenha decisões e motivos com fonte,
   descobertas, alterações/WIP, testes efetivamente executados e limitações,
   pendências e próximo passo. Não invente contexto ausente, não exporte chats
   brutos ou segredos, não gere commit sem mudança e não sobrescreva autor ativo.
   Mantenha o destino autorizado `origin/codex/revised-open-points` e as demais
   verificações da skill. Se a automação estiver pausada manualmente, reporte o
   estado e não reverta uma pausa sem esclarecer o motivo. Confirme por releitura
   a configuração salva; se a automação não estiver acessível, declare pendente,
   sem criar uma substituta nem afirmar que foi ativada.

5. Confira as instruções e hooks efetivamente carregados no Codex da bancada.
   Se houver alteração que exija confiança pelo usuário, explique o item e aguarde
   a revisão normal em /hooks; não contorne permissões. Se a conversa estiver com
   instruções antigas, solicite nova conversa/recarregamento após salvar o handoff.

6. Recompile SOMENTE os helpers C# alterados usando os comandos/referências V19
   existentes e confirmados: BuildUnifiedHomeScreen (incluindo sua dependência
   VerifyNavigationMaster), Rev25FrameAudit, AuditCommonRenderTarget,
   RefineSmcReadability e MatchSmcStateBacks. Compile primeiro para uma pasta de
   saída separada e ignorada pelo Git, sem substituir executável em uso. Preserve
   os antigos e registre os caminhos/hashes antes de ativar os binários nos
   caminhos realmente usados pelos runners. Não invente referências ou opções de
   build se não localizar o procedimento; reporte a dependência faltante.
   Para RefineSmcReadability, inclua também MatchSmcStateBacks.cs e selecione
   `/main:Schlenker.TiaV19.RefineSmcReadability`. O modo `--with-state-backs` é
   opcional e só se aplica ao baseline previsto pelos guards; não execute a migração
   SMC agora, não a reaplique em projeto já migrado e não relaxe os guards.

7. Rode as verificações offline compatíveis com Windows:
   - scripts/test-repository.ps1
   - scripts/test-prompt-judge.ps1 (sem -Live)
   - scripts/test-openness-diagnostics.ps1
   - scripts/test-openness-exclusivity.ps1
   - scripts/test-smc-session.ps1
   - node scripts/test-smc-logical-hmi.cjs
   Os testes Python de subprocessos test-batch-transactions.py e
   test-project-exclusivity.py usam fixtures POSIX; não os declare aprovados no
   Windows sem adaptação/ambiente compatível. Não altere sua implementação só
   para concluir esta ativação. Registre versão do PowerShell e dependências.
   No macOS houve uma falha conhecida de caminho Windows em 1 de 44 testes de
   política; no Windows execute e investigue falhas reais, sem dispensar o teste.

8. Atualize docs/CONTINUITY.md com o resultado real desta ativação e grave o
   change-log com scripts/write-change-log.ps1. Informe SHA integrado, testes e
   falhas, binários reconstruídos/caminhos/hashes, skill sincronizada, automação
   confirmada e pendências. Não confunda configuração salva com primeira execução
   horária bem-sucedida; esta última precisa de evidência de um run concluído.
   Não faça um checkpoint/push manual apenas para testar a agenda. A rotina
   existente fará o checkpoint conforme as condições já autorizadas.

Finalize separando: código atualizado, controles configurados, testes offline,
primeiro checkpoint horário observado ou pendente, e validação TIA ainda não
executada. Não declare validação física nem exclusão universal de concorrência.
