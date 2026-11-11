# SMC final numbering — source revision and verification

Date: 2026-11-10. Status: final eleven-card native naming/layout reopened and
visually verified; WinCC Unified software rebuild 15:17:39 and PLC software
rebuild after native FB13 descriptive comment update 15:24:08 Sydney both
completed with 0 errors / 0 warnings.
Physical mapping PARTIAL. No download or output actuation.

## Evidence boundary

The operator confirms physical references 125Y1 through 125Y11 only;
125Y12 does not exist. Y9 and Y10 each have one opening solenoid and a
spring-return cylinder. Double-solenoid dual-loading arrangements must be
preserved. These assertions establish functional identities, not channel
numbers, polarity or gateway byte order.

The owner's final eleven-valve functional list supersedes every earlier
station-to-function assignment, including the earlier Y1–Y5 names and the
Y6/Y7 identity-only status. The authoritative current names are in the table
below. This confirms valve identity and function, not an EV tag association,
coil/channel mapping, installed wiring, polarity or output reassignment.
Preserve existing production logic and addresses pending a separate verified
reconciliation; retaining them is not proof that they control the newly named
physical function correctly.

The initially unresponsive TIA instance was terminated after explicit operator
authorization accepting possible loss of unsaved edits. A verified 304-file
disk backup was preserved and a separate recovery copy opened successfully.
Bounded corrections were applied to the actual native blocks and Manual Valves
screen. PLC software rebuild: 0 errors / 0 warnings; PLC hardware rebuild:
0 errors / 8 warnings; WinCC Unified rebuild: 0 errors / 0 warnings.
See [native recovery report](../../docs/TIA_RECOVERY_SMC_NATIVE_2026-11-10.md)
for exact scope, backup, remaining source/native differences and evidence limits.

## Mapping table

`SMC125.*` below means `DB_Global.SMC125.*`. Logical requests are not physical
feedback. No final SMC channel or absolute PLC output address is verified in
this run. Operator-confirmed functional names and historically associated
software commands are recorded separately from physical verification.

| Physical reference | Final owner-confirmed function | Existing logical PLC reference, not verified actuation | Existing HMI reference, association unverified | SMC channel / PLC output | Verification / missing data |
|---|---|---|---|---|---|
|125Y1|Infeed Vacuum|SMC125.Y1OpenRequest is a logical target only|SMC_125Y1_Desired_Open; old SMC_EV010_Manual control exists but its association to Infeed Vacuum is UNVERIFIED|Unverified|Function confirmed; reconcile actual command, process dependencies, coil A/B and gateway channel; do not assume the old product-inlet command is correct|
|125Y2|Vacuum|SMC125.Y2OpenRequest is a logical target only|SMC_125Y2_Desired_Open; old SMC_EV011_Manual control exists but its association to Vacuum is UNVERIFIED|Unverified|Function confirmed; reconcile actual command, mutual-exclusion/dual-loading behavior, coil order and gateway channel|
|125Y3|Drainage|SMC125.Y3OpenRequest is a logical target only|SMC_125Y3_Desired_Open; old SMC_EV210_Manual control exists but its association to Drainage is UNVERIFIED|Unverified|Function confirmed; reconcile actual drainage command and sequence, polarity and channel|
|125Y4|Return Dummy Bottle CIP|SMC125.Y4OpenRequest is a logical target only|SMC_125Y4_Desired_Open; old SMC_EV211_Manual control exists but its association to Return Dummy Bottle CIP is UNVERIFIED|Unverified|Function confirmed; reconcile actual CIP return command and sequence, polarity and channel|
|125Y5|Bypass|SMC125.Y5OpenRequest is a logical target only|SMC_125Y5_Desired_Open; old SMC_EV212_Manual control exists but its association to Bypass is UNVERIFIED|Unverified|Function confirmed; reconcile actual bypass command and interlocks, polarity and channel|
|125Y6|Infeed Product|SMC125.Y6OpenRequest is a logical target only; preserve existing pump-stop ordering pending verification|SMC_125Y6_Desired_Open; no operational manual command association established|Unverified; preserve existing addresses|Function confirmed; identify actual infeed-product command and coil/channel; old distributor-air association is invalid|
|125Y7|Spray CIP|SMC125.Y7OpenRequest is a logical target only|SMC_125Y7_Desired_Open; no operational manual command association established|Unverified; preserve existing addresses|Function confirmed; identify actual spray-CIP command and coil/channel; old Cap Channel Air association is invalid|
|125Y8|Cap Distributor Air|SMC125.Y8AirRequest (source-only field), from SMCCapDistributorCmd; physical association unverified|SMC_EV220_Manual retained legacy binding, association unverified; command held disabled|Unverified|Function confirmed; reconcile actual coil/channel and existing command; name similarity is not mapping proof|
|125Y9|Infeed Gate Open|SMC125.Y9OpenRequest (source-only field), from GateFinalOpenCmd; physical association unverified|SMC_EV001_Open_Manual retained legacy binding, association unverified; command held disabled|Unverified|Function and single opening coil/spring return confirmed; verify actual channel, direct-output duplication and gate behavior; no separate close output|
|125Y10|Cap Opening|SMC125.Y10OpenRequest (source-only field), held FALSE|No operational command assigned; configuration-hold card|Unverified; no assigned output|Function and single opening coil/spring return confirmed; need actual opening command, approved sequence and channel; do not reuse a legacy wash output|
|125Y11|External Washing|SMC125.Y11WashRequest (source-only field), from SMCBottleWashCmd; physical association unverified|SMC_Bottle_External_Wash_Manual retained legacy binding, association unverified; command held disabled|Unverified|Function confirmed; verify physical washing solenoid and whether the inherited bottle-wash command covers the required operation|

The new Y8–Y11 fields document logical identities only; they do not remap the
output image and are not yet native PLC/HMI tags. Y1–Y7 production logic and
the inherited output-channel allocation have not been reassigned.

### Required Manual Valves card order

The requested screen contains exactly eleven cards, ordered left to right:

| Row | Column 1 | Column 2 | Column 3 |
|---|---|---|---|
|1|125Y1|125Y2|125Y3|
|2|125Y4|125Y5|125Y6|
|3|125Y7|125Y8|125Y9|
|4|125Y10|125Y11|No valve card|

Use all eleven final owner-confirmed functional names from the table,
including Y6 Infeed Product and Y7 Spray CIP. All unverified command-to-valve
associations and physical channels retain MISSING CONFIG and disabled manual
commands. The native screen was reopened and visually verified in the approved
recovered-work-copy with all eleven exact names and this layout. Current evidence:
`runs/2026-11-10-smc-owner-final/native-reopened-final.png`, with separate native
HMI and PLC software rebuild result screenshots in that folder. These successful
software builds do not verify physical coils, output addresses or gateway transfer.

### Superseded physical associations and remaining software reconciliation

The final owner list replaces the previous Y1 product inlet, Y2 product
shutoff/pump circuit, Y3 vacuum after pump, Y4 vacuum in pump, and Y5 vacuum
before pump labels. The old Y6 distributor-air and Y7 Cap Channel Air
assignments are also invalid: Y6 is Infeed Product and Y7 is Spray CIP.
An even older Y1 Gate Open association is superseded by Y9 Infeed Gate Open.
Do not recover or infer current physical assignments from those old labels.

The existing EV010/EV011/EV210/EV211/EV212 HMI/PLC commands have NOT been
verified against the new Y1–Y5 functions. A label change is not permission to
enable or repurpose these commands. The legacy command-to-function/sequence
cross-reference must be reconciled separately before physical release.
Likewise, do not equate Y6/Y7 to old output bits 10/12, or infer another bit
from a valve number. Preserve dual-loading behavior and existing addresses.

The existing logical target matrix remains preserved, not revalidated by
the naming update: production Y1/Y2/Y6 OPEN, Y3/Y4/Y5/Y7 CLOSED; Y6 remains
OPEN until valid pump-stopped feedback during a level pause. Verify the
matrix and its process dependencies against the final physical functions
before deployment.

Evidence distinction: the owner's latest explicit list supplies all eleven
current functional names; the preserved target matrix is in
[ROB_HMI_PLC_Modifiche_2026-11-10.md](ROB_HMI_PLC_Modifiche_2026-11-10.md).
The [superseded REV03 table](REV12_SMC_EX260_REV03_Engineering_Change.md)
explains the conflicting historical labels; it is not a current wiring
authority. Coil A/B, gateway PDO and installed wiring evidence remain missing.

## Gateway path and unresolved conflicts

Repository code writes `SWAP(DB_Global.Out.ValveBits)` to
`Y_SMC_EX260_OutputImage`, documented as `%QD128` (four bytes). The historical
configuration report under `runs/2026-09-21-anybus-tia/` records output start
128, length 32 bits and module SMC_OUTPUT_4B. This is historical evidence,
not a current online configuration check.

The intended path is PLC PROFINET → Anybus ABC3113-A → EtherCAT → EX260-SEC1.
The installed gateway configuration/PDO export was not found in the searched
materials. The inherited logical bits are EV010=0, EV011=2, EV210=4,
EV211=6, EV212=8, distributor air=10, external wash=14 and gate open=16.
Do not translate these into final physical coil addresses until the SWAP,
gateway data-area transfer and EtherCAT PDO order are checked end to end.

Needed to close MISSING CONFIG:

1. Installed valve/coil A/B and cable-to-channel table for all eleven stations,
   especially the double-solenoid dual-loading function, plus the verified
   command/sequence association for each final owner-confirmed function.
2. Actual gateway output data-area offset/length, EtherCAT slave identity and
   PDO/channel order, including byte/bit order through SWAP.
3. Explicit Y10 opening command/sequence and its channel; confirmation that
   the existing bottle-washing request is the intended Y11 function.
4. Reconciliation of direct legacy outputs (including gate Q4.3/Q4.4) against
   the cabinet and the Anybus path; no unrelated outputs have been removed.
5. EtherCAT/manifold health and communication-loss/watchdog behavior. Current
   source leaves SmcEtherCATOK and ManifoldOK FALSE; PROFINET health alone is
   not proof of manifold health.
6. Complete runtime binding and physical verification before separately
   authorized hardware tests. Native bounded corrections and compilation are
   complete; they do not establish physical mapping or successful FAT.

`PhysicalMapValidated` remains FALSE; no configuration-complete flag was
forced. Existing GatewayConfigured/MappingConfigured defaults are not proof
of physical validation. The output write uses Out.ValveBits directly;
MISSING CONFIG and the new read-only identity fields must not be interpreted
as a global output inhibition. Other existing command paths remain to audit.

## Completed source changes and legacy handling

- Corrected the supervisor branch that recreated Out.GateClose; it now writes
  FALSE. Existing source also disables the legacy close request and bit17.
- Kept the retired Cap Channel Air bit12 inactive; preserved compatibility
  members and unrelated hardware assignments. The recovered native ValveManager,
  Application and OpenPointsSupervisor now contain the bounded close/cap-channel
  corrections; the entire repository baseline was not imported over the project.
- Updated Manual Valve Test labels to Y8/Y9/Y10/Y11; Y10 is no longer spare.
  Held unverified Y8/Y9/Y11 test buttons disabled and Y10 visibly MISSING CONFIG.
  These labels and command holds were also applied and visually checked in the
  recovered native Manual Valves screen before its successful WinCC rebuild.
- Retained EV001 aliases and Close_Manual compatibility references where
  needed for existing bindings/reset-to-FALSE scripts. No separate close
  function is intentionally provided. Retained historical PILZ/source copies
  were not silently rewritten; they must not be used as the current baseline.
- No Y12 assignment was found in the active source/builder search and none
  was created. Historical documents were marked superseded rather than erased.

## Validation

- C# HMI builder/audit sources compile as a library against installed TIA V19
  assemblies. The library was not executed. This is NOT a native HMI compile.
- Six manual-valve audit regression cases pass.
- Existing offline AL102/CIP/SMC production-matrix checks pass, including
  Y1–Y7 and Y6 pump-stop ordering. These are source/model checks, not FAT.
- Evidence and source-hash comparison: `runs/2026-11-10-smc-final-numbering/`.
- Recovered native project: PLC software rebuild 0/0, hardware rebuild 0/8,
  WinCC Unified software rebuild 0/0. Hardware warnings concern device
  replacement without exchangeable medium (seven) and CPU protection level (one).
- Current native recovery evidence: `runs/2026-11-10-tia-recovery-smc/`.
  The additional repository SMC125 Y8-Y11 metadata fields remain source-only;
  the native project uses existing request members and preserved interfaces.

TIA operation and native compilation have been recovered. Physical/gateway
evidence above remains required. No PLC/HMI download, forcing or output
energization was performed.

### Follow-up at 13:45 Sydney

All five additional native EV010/EV011/EV210/EV211/EV212 test commands were
individually confirmed disabled with MISSING CONFIG and saved. Together with
the existing Y8/Y9/Y11 holds, all eight existing valve command buttons remain
held. The source builder now preserves those eight holds. No new physical
association was established, so the legacy titles remain pending verification.
Y10 still has no assigned operational channel or command.

The existing global alarm counter was made visible above the status-bar
background, without replacing its scripts. Full HMI rebuild finished at
13:45:08 with 0 errors / 0 warnings. Hardware rebuild finished at 13:45:51
with 0 errors / 8 warnings; all exact texts and recommendations are in the
[current hardware audit](../../docs/TIA_RECOVERY_SMC_NATIVE_2026-11-10.md).
These results do not change any physical verification status in the table.
