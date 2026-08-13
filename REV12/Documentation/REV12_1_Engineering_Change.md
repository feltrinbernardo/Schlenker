# REV12.1 Engineering Change — Bottle Handling and Product Run-Out

Date: 2026-08-07  
Target: Siemens TIA Portal V19 / S7-1512C-1 PN / MTP1500 Unified Comfort

## Implemented architecture

- `FB_ExternalBottleWash` commands the external bottle-washing valve only for
  production RUN or manual JOG while the tracked bottle count is greater than
  zero. A parameterized 2.5 s OFF delay applies to normal RUN/count removal;
  safety loss or CIP closes the command immediately.
- `FB_AutoBottleGateRequest` adds a bounded sequence below the existing main
  state controller. It reduces the existing main/filler speed command, proves
  actual speed, opens the gate, waits for the admission-complete handshake,
  closes the gate and restores the production speed limit. Alarm, CIP or safety
  loss closes the gate, aborts the sequence and requests a controlled stop.
- `FB_RunOutProduct` retains automatic production while product is removed,
  stops the enabled pump after the product-present sensor clears, closes the
  gate at 3% tank level, waits for a zero tracked-bottle count and a further
  20 seconds, then requests a controlled filler stop.
- Product Pump OFF is a maintained gravity-feed selection. The pump command is
  inhibited, tank level remains monitored, and automatic start is inhibited
  below the configurable minimum level with alarm 1303.
- Gate command priority is: CIP/safety/critical-alarm close; Run Out override;
  Automatic Gate Request override; existing manual/accumulation control.
- The existing `FB_MainState` remains the only machine-state controller.

## New commissioning interfaces

The existing project had no validated tags for bottle count, admission
completion, product presence or actual filler/main speed. REV12.1 adds the
following symbolic inputs without inventing physical addresses:

| PLC interface | Required source | Safe effect while unavailable |
|---|---|---|
| `DB_Global.Inp.BottleCountInMachine` | Validated bottle tracker/counter | Wash and run-out progression remain inhibited or waiting |
| `DB_Global.Inp.BottleAdmissionComplete` | Bottle admission controller handshake | Automatic gate sequence waits and times out closed |
| `DB_Global.Inp.ProductPresentAtPump` | Product-presence sensor at pump | Must be polarity-tested before Run Out is enabled |
| `DB_Global.Inp.MainSpeedActualPct` | Main/filler drive actual speed | Automatic gate sequence cannot advance without valid proof |
| `DB_Global.Inp.MainSpeedActualValid` | Drive feedback validity | Automatic gate sequence times out closed if FALSE |
| `DB_Global.Out.ExternalBottleWashValveCmd` | SMC output bit 9 (software spare) | Physical solenoid/valve number remains unassigned |

These interfaces must be mapped and proved during offline simulation and
commissioning before the associated HMI commands are released to operators.

## HMI additions

- Home: external wash-valve status.
- Home: Gate OFF / Gate ON maintained selector.
- Home: Product Pump OFF / Product Pump ON maintained selector.
- Home: momentary Run Out Product command with Active and Completed status.
- Alarm 1202: Automatic bottle gate sequence fault (critical, bit 22).
- Alarm 1303: Product level not reached. (warning/start inhibited, bit 21).

## Validation gates

1. Generate all numbered SCL external sources and complete PLC software
   `Rebuild all` with zero errors and zero warnings.
2. Import the 91-tag workbook and 23-alarm workbook.
3. Run the HMI builder, compile the HMI with zero errors and zero warnings, and
   run the Openness navigation/command/field audit.
4. Test all transitions in PLCSIM with a controlled signal matrix, including
   alarm injection at every automatic-gate step and sensor-loss cases.
5. Perform physical I/O validation and end-to-end testing only under a separate
   download/commissioning authorization.

No hardware download is part of this engineering change implementation.

## Current validation result

- TIA Portal V19 PLC source generation: passed, 21 source files.
- PLC software compile: passed, 0 errors and 0 warnings.
- HMI import/rebuild and PLCSIM behavior tests: pending.
