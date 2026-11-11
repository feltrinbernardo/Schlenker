# 01 — Production logic
Status: functional design, onsite verification pending.

## Startup and operation
- Safe enable and machine homing use filler valve #1 zero reference.
- Normal production: 125Y1 OPEN; 125Y2 OPEN; 125Y3 CLOSED; 125Y4 CLOSED; 125Y5 CLOSED; 125Y6 OPEN when product feeding; 125Y7 CLOSED. Verify actual pneumatic states.
- Tank level target nominal 50%, band 40–60%. “Delta P” in operator discussion means level percentage, NOT differential pressure.
- M102 product pump modulates continuously while bottles are produced; avoid repetitive ON/OFF that creates foam. Confirm VFD setpoint scaling.
- Product inlet 125Y6 OPEN, wait 2 seconds, then start M102 with liquid presence proven; on stopping M102 wait 3 seconds before closing Y6. No dry running.
- During bottle absence, level may rise to 54%, then stop M102 and close Y6 after 3 seconds. Resume feed according to actual level and demand; do not force 45% threshold blindly.
- M103 vacuum pump in production; standby delay-off approximately 3–4 minutes subject to process confirmation.

## Bottle flow
- Two upstream and two downstream accumulation sensors.
- Infeed far absent: reduce speed. Infeed near absent: close inlet gate at minimum speed.
- Every normal gate OPEN/CLOSE occurs at minimum speed; emergency stops are immediate.
- Outfeed far accumulation: slow to minimum, close gate, return to nominal, clear tracked bottles, then exactly THREE additional FULL EMPTY filler revolutions; decelerate and stop in Auto standby, blinking green Auto Start; automatically restart when accumulation clears and safety permits. Restart at minimum speed for gate opening, then nominal.
- Outfeed near accumulation: protective controlled stop without waiting for complete emptying; define safe stop category with controls engineer.
- Fault/emergency/safety trips NEVER auto-restart.

## Runout Production — end of product
- No incoming product: M102 stops on lack of product. Low tank level alarm at 45% during ordinary production.
- Operator selects Runout Production to continue processing remaining tank content, with M102 OFF and production tank-level permissives excluded. Other safety/mechanical protections remain active.
- Operator requests infeed gate close; gate action occurs at minimum speed, then machine can run at nominal speed while remaining bottles exit.
- Operator stops production manually, then selects CIP in HMI Functions.
- Do not confuse this operator runout with automatic outfeed accumulation runout.
