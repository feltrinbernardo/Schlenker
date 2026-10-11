# Biblioteca técnica — PLC e processo

Criado em 25/09/2026. Índice inicial local com referências oficiais na web. Não é um conjunto de procedimentos já testados nem uma integração automática de recuperação de documentos.

## Referências

| Fonte oficial | Aplicação pretendida | Estado da consulta |
| --- | --- | --- |
| [SIMATIC STEP 7 Basic/Professional V19 e SIMATIC WinCC V19](https://support.industry.siemens.com/cs/ww/en/view/109826862) | Referência principal de engenharia da versão 19; localizar capítulos relevantes conforme a tarefa | Título e link confirmados em bibliografia oficial Siemens. Página de destino inacessível pela ferramenta usada; capítulos ainda não revisados. |
| [Programming Guideline e Programming Styleguide para S7-1200/S7-1500](https://support.industry.siemens.com/cs/ww/en/view/81318674) | Organização e convenções de programação; não substitui manual de navegação V19 | Link confirmado em documentação oficial; destino inacessível nesta consulta. Conferir edição e compatibilidade antes de aplicar. |
| [Notes on TIA Portal — STEP 7/WinCC V19](https://support.industry.siemens.com/cs/attachments/109820994/ReadMe_STEP7_WinCC_V19_enUS.pdf) | Notas específicas da versão e limitações | Localizado na busca oficial; abertura do PDF falhou. Conteúdo não revisado. |
| [Notas de atualização V19 Update 4](https://support.industry.siemens.com/cs/attachments/109925643/ReadMe_TIA_V19_UPD4_enUS.pdf) | Referência de atualização, somente se aplicável à instalação | Localizado na busca oficial; abertura falhou. Não implica que Update 4 esteja instalado ou seja a atualização mais recente. |

Proveniência consultada:

- [Bibliografia oficial Siemens — item 17 identifica o manual V19](https://docs.tia.siemens.cloud/r/en-us/v1.0/guide-for-migrating-to-wincc-unified/5.-appendix/5.3.-links-and-literature).
- [Siemens Guidelines — referência aos guias de programação](https://docs.tia.siemens.cloud/r/en-us/v2.2/automation-framework/automation-framework-v2.2/19.-additional-information/19.1.-siemens-guidelines).

Essas páginas confirmam referências, não a execução de procedimentos na bancada. Não foram usados fóruns ou manuais de versões posteriores como prova de comportamento no V19.

## Índice por tarefa

Os tópicos abaixo são categorias de consulta, não títulos de capítulos confirmados. Acrescentar seção/página exata após acesso ao manual.

| Necessidade | Procurar na referência V19 | Completar com dados do projeto |
| --- | --- | --- |
| Orientar-se no projeto | Project tree, project view, editors | Projeto aberto, CPU configurada e localização dos blocos existentes |
| Editar lógica | Program blocks, SCL, OB, FB, FC, DB | Requisito, blocos afetados e dependências |
| Definir sinais e tipos | PLC tags, PLC data types, data blocks | Mapa de sinais e interfaces acordadas com HMI/dispositivos |
| Compilar e investigar falhas | Compile, compiler messages, cross-references | Resultado integral da compilação e alteração que o precedeu |
| Comparar projeto e equipamento | Online/offline comparison | Versão validada, mudanças em andamento e identidade do alvo |
| Transferir alteração autorizada | Download to device | Escopo autorizado, projeto/backup e destino confirmado |
| Observar integração | Online diagnostics, monitoring, watch tables | Valores esperados, origem dos retornos e partes simuladas |

## Uso sob demanda

1. Identificar a tarefa e a versão/update instalado, idioma da interface e CPU/firmware relevantes. Essas informações da bancada ainda não foram verificadas nesta atividade.
2. Abrir somente a fonte e o trecho necessários. Se a web estiver indisponível, consultar a ajuda correspondente instalada no TIA, quando acessível; não inventar instruções a partir do título do manual.
3. Confrontar a orientação com a interface atual e os requisitos do projeto. Documentação não autoriza uma operação por si só.
4. Registrar a seção utilizada e o resultado observado. Localizações do projeto são construídas e atualizadas durante o desenvolvimento; não é necessário um mapa completo antes de começar.
5. Promover um procedimento a verificado somente após execução com evidências no ambiente identificado.

Cache pode reaproveitar trechos estáveis conforme sua revisão. Não substitui o estado atual da tela/equipamento nem a memória persistente. Não é necessária uma base vetorial para este índice inicial.

## Registro mínimo de procedimento

- Tarefa e objetivo.
- Estado: referência não testada / verificado na bancada / requer revisão.
- TIA, update, idioma, CPU/firmware aplicáveis.
- Fonte oficial e seção/página.
- Projeto/revisão e pré-condições.
- Caminho de navegação e ações efetivamente observados, sem coordenadas fixas reutilizadas cegamente.
- Resultado esperado, observado e referências às evidências.
- Limitações, simulações e intervenção humana quando houver.
- Data de verificação e condições que exigem nova revisão.

Nenhum procedimento de bancada foi verificado nesta atividade. Procedimentos gerais poderão ser reutilizados em projetos futuros após conferir compatibilidade; endereços e sinais específicos não são universais.

## Próxima validação prática

No computador da bancada, começar por uma consulta sem alteração: localizar um bloco existente e suas variáveis, confrontar com a ajuda V19 e registrar o caminho realmente observado. Depois, no piloto já planejado, avaliar execução autorizada, passagem de controle e evidências.

Medir êxito da tarefa, ações desnecessárias, retrabalho e tempo total em tarefas comparáveis. Nenhum ganho de eficiência foi medido até agora.
