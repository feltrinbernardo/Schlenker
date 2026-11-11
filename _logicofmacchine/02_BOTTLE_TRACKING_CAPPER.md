# 02 — Bottle tracking and capper
## Machine geometry
36 filler valves, 10 capper heads. Path: infeed timing screw → 10-pocket infeed star → 36-valve filler → 10-pocket transfer star → 10-head capper → 10-pocket outfeed star → conveyor.
Single bottle-presence sensor at the middle of timing screw. No extra sensor at capper entrance.
Bottle occupancy mapped to synchronized pockets by wired process encoder and filler valve #1 zero. Do not shift the map for empty pockets. Capper zero derived mechanically by calibrated offset from filler; no independent capper zero sensor.
Track bottle presence through transfer positions. Tracking predicts occupancy; it cannot independently detect unexpected physical bottle loss.

## No Bottle No Cap
Cap release gate: air-to-open, spring-to-close. For predicted occupied cap-receive position energize opening; for empty position de-energize to close; keep open across consecutive occupied positions. Calibrate opening/closing phase and actuator delay. Confirm safe closing on air/power loss.
The 10 capper heads are entirely mechanical: cam lifts/lowers each head; star supplies caps; friction clutch tightens. PLC does NOT actuate individual head up/down/screwing. Heads continue cam motion with empty bottle positions.

## Cap feed
- Cap recall sensor near distributor: if channel needs caps, blower ON 0.5 s before distributor rotation. After recall satisfied, continue distributor 2–3 s HMI-adjustable (initial 2 s), stop distributor, then blower OFF.
- Caps missing sensor near capper: block production start or stop production with CAPS MISSING; refilling must be possible while machine stopped.
- Hopper sensor governs external elevator. Confirmed elevator request conditions: recall channel requests caps AND capper-end caps missing AND hopper requests caps. Elevator 24 V command allowed when machine stationary; hopper full blocks elevator. On hopper recovery, 3 s run-on then OFF, subject to safe permissions. This strict three-condition rule may defer preventative replenishment; retain as requested pending testing.
