# TIA recovery and native SMC changes — 2026-11-10

Status: native offline changes saved and compiled; physical SMC commissioning remains open. No download, online operation, forcing or output actuation was performed.

## Recovery and protected sources

TIA stopped responding to Save and normal Close. No separate blocking modal was found. Approximately 300 MB free RAM was observed; this does not prove the cause of the freeze. The operator explicitly authorized forced termination with possible loss of unsaved changes after the last observed 11:11 disk save.

Before termination, all 304 files in the disk backup matched the source SHA256 values. The verified TIA process and its identified children were terminated, and TIA successfully reopened a separate recovery copy. This recovers the saved disk state, not a guarantee that every unsaved edit survived.

Working project:

`runs/2026-11-10-tia-recovery-smc/recovered-work-copy/Backup Schlenkers 36-10 190036-7-8v2.12 10-11.ap19`

Pre-recovery backup: `runs/2026-11-10-tia-recovery-smc/pre-recovery-disk-copy/`.

Final native archive created by TIA after saving/compilation:
`runs/2026-11-10-tia-recovery-smc/Schlenker_SMC_recovered_compiled_2026-11-10.zap19`
(26,426,960 bytes). SHA256 is recorded in `final-archive-hash.json` in the same run folder. The native project/archive remain local engineering artifacts, not Git-tracked deliverables.

The three protected original AP19 files have unchanged SHA256 hashes. The comparison is in `runs/2026-11-10-tia-recovery-smc/source-after-final.json`; it covers those three files, not every companion file of their projects.

## Native PLC changes

- `FB_SMC_ValveManager [FB7]`: preserved inherited output allocation and product-valve mutual exclusion; retired Cap Channel Air bit 12 and separate gate-close bit 17 are explicitly FALSE. Gate opening retains inherited bit 16 pending physical verification.
- `FB_OpenPointsSupervisor [FB28]`: removed the branch that recreated `Out.GateClose`; it now assigns FALSE.
- `FB_Application [FB13]`: exported the actual native block and applied bounded changes, preserving its interface and existing production logic. Gate-close requests cancel the opening request rather than command a second coil. The close output, retired cap-channel command, simulation bits 12/17 and obsolete active indications are FALSE. Compatibility manual-close and cap-channel request members are reset to FALSE.
- Y8/Y9/Y10/Y11 functional identities are documented in the native logic. Y10 receives no invented physical channel or production sequence. Dual-loading allocation was not reassigned.

The complete repository baseline was **not** imported over the native project. New repository `SMC125.Y8AirRequest` through `Y11WashRequest` metadata fields remain source-only; existing native request paths were used. This avoids treating the repository model as an exact export of the running project's structure.

## Native Manual Valves changes

| Physical reference | Visible function | Native manual command status |
|---|---|---|
|125Y1–125Y7|Existing EV010/EV011/EV210/EV211/EV212 functions and other existing production logic preserved|No coil/channel reassignment; global physical mapping notice retained|
|125Y8|CAP DISTRIBUTOR AIR|Existing EV220 button retained, text MISSING CONFIG, Operator control - allow unchecked, no dynamization overriding that property|
|125Y9|INFEED GATE - SPRING RETURN|Existing EV001Open button retained, text MISSING CONFIG, Operator control - allow unchecked; no separate close control|
|125Y10|CAP OPENING - SPRING RETURN|No operational button or output assigned; visible note says coil channel / command pending|
|125Y11|EXTERNAL WASHING|Existing BottleExternalWash button retained, text MISSING CONFIG, Operator control - allow unchecked|

The Y10 spare/double-solenoid label was replaced. Its text object is now `REV50_ValveTest_Y10Config_Note`; contrast was improved. The physical mapping notice was shortened to fit its card without removing MISSING CONFIG. Existing event bindings were preserved; this run did not claim a complete runtime event/cross-reference audit.

Some GUI text insertions encountered clipboard interference and initially inserted unrelated text. Each affected field was corrected and visually verified before saving and compiling. A display-resolution change also required re-observation/rebinding; TIA remained responsive afterward.

## Native compiler evidence

| Operation | Errors | Warnings | Observed result |
|---|---:|---:|---|
|Generate FB_Application from corrected native source|0|0|Successful import/generation|
|PLC software — rebuild all|0|0|Successful native rebuild|
|PLC hardware — rebuild all|0|8|Seven warnings under PROFINET interface concerning device replacement without exchangeable medium; one states PLC_1 has no configured protection level|
|WinCC Unified software — rebuild all|0|0|Completed 12:20:20 local; 518 tags, 378 used tags, 1 connection, 32 used screens|

Hardware warning texts were partially truncated in the table. No protection setting or network topology was changed merely to suppress these warnings. Screenshots were observed through official Computer Use; standalone evidence PNGs were not persisted in the run folder. This is an evidence-pack limitation, not a hidden claim of runtime testing.

## Remaining physical verification

No final station-to-coil mapping has been verified. The intended path remains PLC PROFINET → Anybus ABC3113-A → EtherCAT → SMC EX260-SEC1. Historical `%QD128` and inherited logical bits are not final physical wiring instructions. Required evidence remains:

1. Coil A/B and channel allocation for all eleven stations, including dual loading.
2. Installed gateway data-area/PDO mapping and byte order through `SWAP`.
3. Y10 channel and approved manual/production sequence; confirmation of the Y11 washing command's scope.
4. Reconciliation of legacy direct outputs and the gateway path.
5. Manifold/EtherCAT health and watchdog/communication-loss behavior.

MISSING CONFIG remains visible and no configuration-complete flag was forced. Disabling three test-page buttons is not a global PLC output inhibit. Compilation does not constitute FAT, safety validation or permission to download.

See also [station mapping table](../REV12/Documentation/SMC_FINAL_NUMBERING_2026-11-10.md).

## Current hardware warning audit — 13:45:51 Sydney

On 2026-11-10, PLC hardware was rebuilt offline solely to obtain the complete current warning list: **0 errors, 8 warnings**. No hardware, topology or CPU protection settings were changed. Full descriptions were read in TIA after widening the description column and persisted in `runs/2026-11-10-manual-valves-parallel-review/plc-hardware-warnings.png`.

| # | Dispositivo | Texto exato observado | Causa indicada | Correção proposta, não aplicada | Risco / limite |
|---|---|---|---|---|---|
|1|al104|The device replacement without exchangeable medium function cannot be used for device 'al104' without configuration of topology.|Topologia necessária à função não configurada.|Conferir portas/cabos e configurar a topologia real; validar suporte LLDP.|Substituição automática pode exigir atribuição manual do nome; não prova falha atual de I/O.|
|2|al102|The device replacement without exchangeable medium function cannot be used for device 'al102' without configuration of topology.|Topologia necessária à função não configurada.|Conferir portas/cabos e configurar a topologia real; validar suporte LLDP.|Mesmo risco de manutenção/substituição; comunicação atual não foi testada.|
|3|al101|The device replacement without exchangeable medium function cannot be used for device 'al101' without configuration of topology.|Topologia necessária à função não configurada.|Conferir portas/cabos e configurar a topologia real; validar suporte LLDP.|Mesmo risco de manutenção/substituição; comunicação atual não foi testada.|
|4|al100|The device replacement without exchangeable medium function cannot be used for device 'al100' without configuration of topology.|Topologia necessária à função não configurada.|Conferir portas/cabos e configurar a topologia real; validar suporte LLDP.|Mesmo risco de manutenção/substituição; comunicação atual não foi testada.|
|5|al103|The device replacement without exchangeable medium function cannot be used for device 'al103' without configuration of topology.|Topologia necessária à função não configurada.|Conferir portas/cabos e configurar a topologia real; validar suporte LLDP.|Mesmo risco de manutenção/substituição; comunicação atual não foi testada.|
|6|anybus-smc|The device replacement without exchangeable medium function cannot be used for device 'anybus-smc' without configuration of topology.|Topologia necessária à função não configurada.|Conferir ligação real ao switch/PLC e configurar portas; validar suporte do gateway.|Substituição do gateway pode atrasar recuperação; aviso não valida PDOs, EtherCAT ou bobinas.|
|7|mes-profinet|The device replacement without exchangeable medium function cannot be used for device 'mes-profinet' without configuration of topology.|Topologia necessária à função não configurada.|Confirmar identidade/ligação física do dispositivo antes de definir portas e topologia.|Substituição automática não demonstrada; nome do projeto não prova identidade física.|
|8|PLC_1|PLC_1 does not contain a configured protection level|Nenhum nível de proteção configurado segundo o compilador.|Revisar Protection & Security e acordar nível de acesso/credenciais compatível com HMI e manutenção; aplicar somente com autorização específica.|Risco de acesso não autorizado, dependendo da configuração efetiva/rede; o aviso não prova falha funcional nem segurança aceitável.|

As consequências acima são avaliação técnica do aviso, não resultados de ensaio online. A documentação Siemens descreve interconexão de portas e suporte LLDP como requisitos da substituição automática: [STEP 7, referência V20](https://docs.tia.siemens.cloud/r/en-us/v20/editing-devices-and-networks/creating-configurations/configuring-profinet-io/configure-profinet-io/enabling-device-replacement-without-exchangeable-medium). O projeto e os avisos observados são V19. Não se deve inventar topologia nem desabilitar proteção para obter uma compilação sem avisos.

O dispositivo anteriormente não identificado entre os sete avisos de topologia é **mes-profinet**. As linhas de agrupamento da árvore não são avisos adicionais. A [verificação Manual Valves](MANUAL_VALVES_VERIFICATION_2026-11-10.md) registra a compilação HMI posterior às correções, também sem download.
