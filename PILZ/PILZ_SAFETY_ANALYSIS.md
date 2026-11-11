# Schlenker — análise documental de segurança e equipamentos Pilz

**Data da análise:** 2026-09-25  
**Escopo:** análise somente leitura dos arquivos do projeto Schlenker.  
**Limite da conclusão:** código existente, compilação offline ou teste planejado não são tratados como validação funcional de segurança.

## 1. Classificação das informações

- **Confirmado/aprovado:** registrado como decisão de máquina ou requisito confirmado.
- **Requisito:** comportamento exigido, ainda sujeito a implementação, ensaio e aprovação.
- **Proposta:** arquitetura, alocação ou mapa ainda não aprovado para uso físico.
- **Código:** comportamento encontrado no SCL; não comprova validação.
- **Evidência:** compilação, auditoria ou inspeção efetivamente registrada, com o limite indicado.

## 2. Equipamento Pilz e ferramenta

| Informação | Classificação | Fonte |
|---|---|---|
| Família prevista: **Pilz PNOZmulti 2**, com módulo PROFINET **PNOZ m ES PROFINET 772138**. | Hardware retido/selecionado; não comprova instalação física. | [`REV12_MANIFEST.json`, `retained_confirmed_hardware`](sources/REV12/REV12_MANIFEST.json); [`REV12_Master_Cable_Schedule.csv`, SAFE1](sources/REV12/Documentation/REV12_Master_Cable_Schedule.csv) |
| O diagnóstico lógico `b_Pilz_DeviceOK` representa o estado da estação PNOZmulti 2. | Interface lógica confirmada; mapa físico ausente. | [`REV12_IO_Tag_Mapping.csv`](sources/REV12/Documentation/REV12_IO_Tag_Mapping.csv) |
| Uma alocação posterior descreve PNOZ m B0 `772100`, EF 16DI `772140`, EF 8DI4DO `772142`, EF 1MM `772170` e PROFINET `772138`. | **Proposta não validada**; utiliza 12 portas e três zonas, conflitantes com a baseline de 11 portas sem zoneamento aprovado. | [`PILZ_LOCAL_SAFETY_IO_ALLOCATION.csv`](sources/outputs/PILZ_PLC_HMI_IMPLEMENTATION_2026-08-15/PILZ_LOCAL_SAFETY_IO_ALLOCATION.csv); [`PENDENZE_OPEN_POINTS_IT.md`, §2.1](sources/docs/PENDENZE_OPEN_POINTS_IT.md) |
| A ferramenta citada é **PNOZmulti Configurator**. | Ferramenta requerida; versão, instalação e compatibilidade não comprovadas. | [`PILZ_SAFETY_LOGIC_IMPLEMENTATION_WORKSHEET.md`, Implementation blockers](sources/outputs/PILZ_PLC_HMI_IMPLEMENTATION_2026-08-15/PILZ_SAFETY_LOGIC_IMPLEMENTATION_WORKSHEET.md) |
| Não há projeto nativo PNOZmulti, desenhos elétricos safety ou estação Pilz configurada no TIA auditado. | Evidência de ausência no conjunto auditado. | [`REV22_Audit_and_Open_Items.md`, Proven project state](sources/outputs/REV22_PILZ_Safety_Software_HMI_Diagnostics/REV22_Audit_and_Open_Items.md) |

Não é possível declarar o modelo físico final ou que o equipamento esteja instalado. É defensável apenas afirmar que a família PNOZmulti 2 e o módulo PROFINET 772138 estão previstos.

## 3. Requisitos de segurança documentados

### 3.1 Autoridade e estado seguro

- O Pilz é a única autoridade de segurança.
- O PLC Siemens e a HMI executam somente coordenação e diagnóstico não seguros.
- E-stops, portas e travas, STO, contatores safety, remoção de energia, isolamento pneumático, EDM, standstill e prevenção de rearranque devem permanecer sob controle Pilz.
- Perda de permissivo, falha ou perda de comunicação deve resultar em estado não inicializável e nunca em falso `READY`, `HEALTHY`, `OFF` ou `CLOSED`.

Fontes: [`PENDENZE_OPEN_POINTS_IT.md`, §§3 e 7](sources/docs/PENDENZE_OPEN_POINTS_IT.md), [`PILZ_SAFETY_LOGIC_IMPLEMENTATION_WORKSHEET.md`, Safety boundary](sources/outputs/PILZ_PLC_HMI_IMPLEMENTATION_2026-08-15/PILZ_SAFETY_LOGIC_IMPLEMENTATION_WORKSHEET.md) e [`ROB_REV22_PILZ_Safety_Software_HMI_Diagnostics (1).docx`, §§2, 4, 5 e 11](sources/.pilz_safety_software/ROB_REV22_PILZ_Safety_Software_HMI_Diagnostics%20%281%29.docx).

### 3.2 Parada de emergência

- Baseline de **quatro E-stops**: frontal, traseiro, área de entrada e pendant.
- A documentação afirma uma cadeia agregada que remove o retorno 24 VDC ao Pilz.
- A HMI deve exibir somente o alarme agregado `Emergency stop pressed` enquanto não houver dados reais para identificação individual.
- O estado de home e tracking deve ser invalidado após uma ocorrência de emergência no comportamento do PLC padrão.

Fontes: [`DOOR_ACCESS_SEQUENCE.md`, Emergency stops](sources/fixtures/references/tia-import-rev10/DOOR_ACCESS_SEQUENCE.md), [`PENDENZE_OPEN_POINTS_IT.md`, decisões confirmadas](sources/docs/PENDENZE_OPEN_POINTS_IT.md), [`10_FB_AlarmManager.scl`](sources/REV12/PLC_Sources/10_FB_AlarmManager.scl) e [`03_FB_MainState.scl`](sources/REV12/PLC_Sources/03_FB_MainState.scl).

A categoria, PLr/SIL, canais físicos, tempos de resposta e fiação da cadeia ainda não estão validados.

### 3.3 Portas e acesso protegido

Baseline confirmada:

- 11 portas: quatro frontais, quatro traseiras, duas no lado direito/entrada e uma no lado esquerdo/saída;
- três estações físicas de pedido de abertura: painel, frente e traseira;
- não está aprovado o modelo de 12 portas nem o zoneamento Z1/Z2/Z3.

Sequência requerida:

1. Pedido físico de acesso.
2. Parada controlada coordenada pelo PLC padrão.
3. Confirmação de máquina parada/zero speed e retirada de potência trifásica.
4. Somente o Pilz pode autorizar o desbloqueio.
5. Todas as portas devem voltar a fechar.
6. Rearme auxiliar/safety deliberado.
7. Reset de alarme separado.
8. Retorno à permissão de rearranque.
9. Partida somente por comando Start independente.

Fontes: [`DOOR_ACCESS_SEQUENCE.md`, Required sequence](sources/fixtures/references/tia-import-rev10/DOOR_ACCESS_SEQUENCE.md) e [`PENDENZE_OPEN_POINTS_IT.md`, §2.1](sources/docs/PENDENZE_OPEN_POINTS_IT.md).

### 3.4 Rearme e prevenção de partida inesperada

- Fechar uma porta, liberar E-stop, restaurar energia ou reconhecer um alarme não pode iniciar a máquina.
- O rearme deve ser deliberado e aceito pelo Pilz somente com condições válidas.
- Acknowledge na HMI não substitui reset safety nem remove a causa ativa.
- Após reset deve existir comando Start físico separado.
- Comandos de motor, válvulas, JOG, CIP e sequências incompletas devem iniciar em estado seguro após restart.
- Warm restart, cold restart e power cycle devem ser ensaiados separadamente.

Fontes: [`DOOR_ACCESS_SEQUENCE.md`](sources/fixtures/references/tia-import-rev10/DOOR_ACCESS_SEQUENCE.md), [`PENDENZE_OPEN_POINTS_IT.md`, §4 e §7](sources/docs/PENDENZE_OPEN_POINTS_IT.md) e documento REV22, §§4 e 11.

## 4. Relação entre Pilz, PLC, HMI e inversores

### 4.1 Pilz para PLC Siemens

A interface prevista é PROFINET não segura para estados agregados: safety ready, falha geral, reset requerido, cadeia de E-stop, portas fechadas/desbloqueadas, circuito safety fechado, standstill e retirada de potência. STO, dump pneumático e EDM somente podem ser adicionados se forem realmente exportados pelo projeto Pilz aprovado.

No código atual:

- `PhysicalMapConfigured := FALSE`;
- `DataValid` não pode ficar verdadeiro;
- estados dependentes são apagados quando inválidos;
- nenhum endereço absoluto foi inventado.

Fontes: [`11_FB_Application.scl`](sources/REV12/PLC_Sources/11_FB_Application.scl) e [`REV23_PILZ_PROFINET_Mapping_Report.md`](sources/outputs/REV23_PILZ_PROFINET_NonSafety_Diagnostics/REV23_PILZ_PROFINET_Mapping_Report.md).

### 4.2 PLC Siemens para Pilz

Somente solicitações não seguras explicitamente aprovadas podem existir. A proposta atual contém pedidos de acesso e possível reset, mas os marca como `PROPOSED NOT COMMISSIONED` ou `BLOCKED PENDING SAFETY REVIEW`. Não existe autorização documental suficiente para implementar reset Pilz pela HMI/PLC.

Fonte: [`PILZ_PROFINET_LOGICAL_TELEGRAM_PROPOSAL.csv`](sources/outputs/PILZ_PLC_HMI_IMPLEMENTATION_2026-08-15/PILZ_PROFINET_LOGICAL_TELEGRAM_PROPOSAL.csv).

### 4.3 HMI

A HMI possui páginas diagnósticas read-only e estados agregados. Deve mostrar `COMMUNICATION FAULT / DATA INVALID` quando o mapa não for válido e `NOT CONFIGURED` para portas individuais, STO, dump e EDM sem fonte real. Não deve fornecer bypass, force, reset safety direto ou comando de saída safety.

Fontes: [`REV22_Audit_and_Open_Items.md`](sources/outputs/REV22_PILZ_Safety_Software_HMI_Diagnostics/REV22_Audit_and_Open_Items.md) e [`REV23_PILZ_PROFINET_Mapping_Report.md`, HMI implementation](sources/outputs/REV23_PILZ_PROFINET_NonSafety_Diagnostics/REV23_PILZ_PROFINET_Mapping_Report.md).

### 4.4 Inversores

Estão previstos quatro SINAMICS G120C. A proposta Pilz prevê STO em dois canais para os quatro drives, mas o circuito físico e o feedback não estão validados. No PLC padrão, perda de safety ou alarme crítico bloqueia comandos e `RotationPermitted` exige `SafetyOK`; telegramas, estados reais e scaling dos drives ainda não estão comissionados.

Fontes: [`PILZ_LOCAL_SAFETY_IO_ALLOCATION.csv`](sources/outputs/PILZ_PLC_HMI_IMPLEMENTATION_2026-08-15/PILZ_LOCAL_SAFETY_IO_ALLOCATION.csv), [`03_FB_MainState.scl`](sources/REV12/PLC_Sources/03_FB_MainState.scl) e [`ROB_IMPLEMENTATION_STATUS_2026-09-21.md`](sources/docs/ROB_IMPLEMENTATION_STATUS_2026-09-21.md).

## 5. Comportamento encontrado somente no código

Estes itens não equivalem a segurança validada:

- `FB_DoorAccess` implementa parada controlada, zero speed, retirada trifásica, desbloqueio, reset auxiliar, reset de alarme e permissão de rearranque; o comentário do bloco mantém o Pilz como autoridade. Fonte: [`10A_FB_DoorAccess.scl`](sources/REV12/PLC_Sources/10A_FB_DoorAccess.scl).
- Falha de E-stop ou do sistema safety leva o coordenador de portas ao estado de falha; restart permission exige portas fechadas e circuito safety fechado. Fonte: [`10A_FB_DoorAccess.scl`](sources/REV12/PLC_Sources/10A_FB_DoorAccess.scl).
- Saídas de válvulas são zeradas por padrão e somente preenchidas com `SafetyOK`, comunicação, permissão e ausência de conflito. Fonte: [`09_FB_SMC_ValveManager.scl`](sources/REV12/PLC_Sources/09_FB_SMC_ValveManager.scl).
- Há exceção temporária de bancada: `BenchCommissioningMode` permite selecionar MANUAL sem `SafetyOK`; o comentário exige removê-la antes do commissioning. Fonte: [`03_FB_MainState.scl`](sources/REV12/PLC_Sources/03_FB_MainState.scl).
- `SimulationMode` participa do teste manual de gate/válvulas, condicionado a modo manual, tela de teste, enable, comunicação SMC, ausência de alarme crítico e ausência de acesso protegido. É uma exceção de software, não uma função safety validada. Fonte: [`11_FB_Application.scl`](sources/REV12/PLC_Sources/11_FB_Application.scl).

As exceções de bancada/simulação precisam ser incluídas na análise de risco e possuir procedimento formal de remoção ou bloqueio antes de instalação em máquina.

## 6. Testes, critérios e evidências

| Item | Critério | Evidência disponível | Estado real |
|---|---|---|---|
| PLC padrão | Compilação sem erros/warnings | Rebuild 0/0 registrado | Executado; comprova consistência de software, não safety |
| HMI | Compilação e preservação de navegação/bindings | Rebuild 0/0 e auditorias de tela | Executado; não valida segurança |
| Interface Pilz lógica | Falhar como `DATA INVALID` sem mapa físico | Implementação e compile 0/0 | Executado offline |
| Programa Pilz | Compilar no PNOZmulti Configurator | Projeto nativo ausente | **Bloqueado/não executado** |
| Cada E-stop | Todos os elementos atingem estado seguro | Checklist somente | **Não executado** |
| Portas e locks | Teste individual, discrepância e prevenção de rearranque | Checklist baseado em 12 portas | **Não executado e baseline desatualizada** |
| STO/standstill | Falha impede unlock e produz diagnóstico | Checklist/proposta | **Não executado** |
| Dump pneumático | Falha de confirmação impede liberação | Checklist/proposta | **Não executado** |
| EDM | Falha impede restauração | Checklist/proposta | **Não executado** |
| Reset inválido | Rejeitar reset com porta aberta/desbloqueada | Checklist | **Não executado** |
| Rearranque inesperado | Guardas/energia restauradas nunca iniciam a máquina | Requisito e código | **Não executado** |
| Power cycle | Retorno sem partida, em estado seguro | Checklist | **Não executado** |
| Perda PROFINET | Pilz mantém segurança; HMI mostra dados inválidos | Lógica offline presente | Parcial; teste físico ausente |

Fontes: [`PILZ_FAT_CHECKLIST.csv`](sources/outputs/PILZ_PLC_HMI_IMPLEMENTATION_2026-08-15/PILZ_FAT_CHECKLIST.csv), [`REV22_Audit_and_Open_Items.md`, ledger](sources/outputs/REV22_PILZ_Safety_Software_HMI_Diagnostics/REV22_Audit_and_Open_Items.md), [`REV23_Implementation_Ledger.md`](sources/outputs/REV23_PILZ_PROFINET_NonSafety_Diagnostics/REV23_Implementation_Ledger.md), [`IMPLEMENTATION_REPORT.md`](sources/outputs/PILZ_PLC_HMI_IMPLEMENTATION_2026-08-15/IMPLEMENTATION_REPORT.md), [`COMPLETION_LEDGER.md`](sources/outputs/PILZ_PLC_HMI_IMPLEMENTATION_2026-08-15/COMPLETION_LEDGER.md) e [`change-log/2026-08-11.md`](sources/logs/change-log/2026-08-11.md).

## 7. Base disponível para definir o agente de segurança

Já pode ser utilizada como base de requisitos:

- Pilz como autoridade exclusiva de segurança;
- separação entre segurança e PLC/HMI padrão;
- PNOZmulti 2 como família prevista e módulo PROFINET 772138 como interface diagnóstica planejada;
- 11 portas, quatro E-stops agregados e três estações de acesso;
- parada controlada antes do pedido de desbloqueio;
- confirmação de standstill e retirada de energia antes da liberação;
- rearme deliberado e Start separado;
- proibição de partida automática;
- interface Pilz→PLC fail-invalid, somente diagnóstica;
- alarmes e estados agregados utilizáveis pela HMI;
- bloqueio de comandos de drives e válvulas quando `SafetyOK` não estiver disponível, ressalvadas as exceções temporárias de bancada identificadas.

## 8. Informações ainda necessárias

1. Modelo e composição física finais do PNOZmulti, números de pedido e firmware.
2. Projeto nativo e versão do PNOZmulti Configurator.
3. Avaliação de risco aprovada e PLr/categoria ou SIL de cada função.
4. Categorias de parada e tempos máximos permitidos.
5. Desenhos elétricos de E-stops, portas, locks, STO, contatores, EDM e dump pneumático.
6. Confirmação formal das 11 portas e rejeição/correção da proposta de 12 portas/três zonas.
7. Canais físicos, polaridades e test pulses.
8. Decisão entre unlock-all, liberação individual ou por zona.
9. Critérios validados de standstill e retirada de energia.
10. Critérios do estado seguro pneumático e pressão residual permitida.
11. GSDML, nome de dispositivo, IP, tamanhos do telegrama, offsets e watchdog PROFINET.
12. Definição aprovada de qualquer solicitação PLC→Pilz, especialmente reset.
13. Mapeamento STO e feedback reais dos quatro G120C.
14. Procedimento para desativar/remover `BenchCommissioningMode` e caminhos `SimulationMode`.
15. FAT/SAT revisado para 11 portas, executado e assinado pelo responsável de segurança.

## 9. Conclusão

A arquitetura e a filosofia de segurança estão suficientemente documentadas para elaborar a especificação do agente de segurança e das interfaces. O projeto safety físico, o mapeamento, a avaliação de risco e a validação ainda não estão disponíveis. As evidências atuais comprovam preparação e compilação offline do PLC/HMI, mas não comprovam que o Pilz ou a máquina estejam funcionalmente seguros ou comissionados.

## 10. Conteúdo do pacote

Todos os arquivos usados ou citados nesta análise foram copiados, sem alteração, para `PILZ/sources/`, preservando seus caminhos relativos. O arquivo `SOURCE_MANIFEST.csv` registra caminho de origem, caminho da cópia, tamanho e SHA-256 para conferência de integridade.
