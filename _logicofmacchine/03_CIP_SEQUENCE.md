# 03 — CIP functional sequence
Status: user-described sequence; requires process/P&ID and safe-state validation. This document supersedes earlier draft sequences.

## Enter CIP
1. Finish product with Runout Production, remove remaining bottles, manually Stop Production.
2. Select CIP in HMI Functions; machine seeks filler valve #1 zero and stops.
3. Operator connects CIP tube. CIP Presence confirms connection; operator confirms readiness on HMI and presses Start CIP.
4. PLC requests CIP from external CIP unit. Confirm CIP fluid presence before starting M102. Do not bypass dry-run protection.

## Start CIP — initial sequence only
Assume valves initially closed, after checking physical conditions:
A. Open 125Y6 (product/CIP inlet); wait approximately 4 s AND confirm fluid presence; start M102 product pump. M102 remains ON throughout circulation and spray until CIP stop, subject to protections.
B. Open 125Y4 CIP return; keep OPEN throughout active CIP.
C. Hold initial return configuration for 30 s (confirm timer origin on machine).
D. Open 125Y1 and 125Y5; close 125Y6. Open 125Y2 and start M103 vacuum pump for 6 s.
E. Close 125Y2; keep M103 ON for another 20 s, then STOP M103. M103 stays OFF for all subsequent loops; it can restart only on a NEW Start CIP.
F. Continue current circuit configuration for 3 min. M102 stays ON.
G. Open 125Y7 spray; close 125Y5 and 125Y1; maintain spray for 6 min.

## Repeating CIP loop (NO M103, NO 125Y2)
H. Open 125Y6 THEN close 125Y7; hold 3 min.
I. Open 125Y1 and 125Y5; close 125Y6; hold 3 min.
J. Open 125Y7; close 125Y5 and 125Y1; hold 6 min.
Repeat H → I → J while external CIP demand remains valid.
125Y4 stays OPEN and M102 remains ON throughout; M103 OFF and 125Y2 CLOSED in loop.

## Important unresolved safety / implementation
- Verify the physical liquid path and valve switching order, avoiding deadhead/overpressure while M102 runs continuously. Do not invent an extra pressure-release solenoid. Appropriate independent pressure relief and monitored flow/pressure may be necessary.
- Exact control of 125Y3 (drain) during this corrected CIP sequence was NOT restated; do not infer it from the superseded sequence.
- Confirm what 125Y1 and 125Y5 route physically, how CIP liquid presence is measured, and whether spray route can sustain M102.
- Define external CIP request/ready/complete handshake, chemical rinse phases, number of repetitions, and operator stop/end/abort transitions.
- Define safe response to lost CIP Presence, loss of liquid, CIP skid fault, pump fault, overpressure, safety opening and power loss; stop M102 safely and route/close valves in verified order.
- All times user-proposed; test and tune with machine and CIP skid.
