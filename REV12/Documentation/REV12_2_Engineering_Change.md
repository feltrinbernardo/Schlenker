# REV12.2 Engineering Change — Production, Run Out and CIP Integration

Date: 2026-08-07  
Target: TIA Portal V19 / S7-1512C-1 PN / MTP1500 Unified Comfort

## Implemented control architecture

- `FB_MainState` remains the single mode authority. Explicit mutually exclusive
  modes are Production (state 30), CIP (state 80), Run Out Product (state 95)
  and Off/Ready. Mode entry is accepted only through the stopped Ready state,
  except Run Out which is entered from Production.
- `FB_ProductCircuitManager` selects the logical product-valve matrix using the
  priority Safety/Critical Alarm, CIP, Run Out, Production. It commands logical
  valves 210, 217, 213, 212 and 247 without inventing physical terminal bits.
- `FB_CIPMediaPump` starts the product pump after media is seen, holds it through
  a configurable 3.5 second presence loss and stops/alarms if media does not
  return.
- `FB_ProductionVacuum` applies a configurable -400 mbar threshold and 5.5
  second startup inhibit. One reset/restart attempt is allowed; the second
  failure remains latched until adequate vacuum is restored and reset.
- `FB_VacuumCIP` implements the tank-full sequence: 4 second confirmation,
  vacuum pump start, 1 second valve-preopen delay, Valve 212 open for 7 seconds,
  then 10 seconds vacuum-pump post-run. Step elapsed time resets on cycle, step
  or medium change.
- `FB_RunOutProduct` is subordinated to state 95, ignores normal low-level stop,
  stops the product pump when upstream product is lost, closes the gate at 3%,
  waits for bottle count zero and a configurable 20 second final run, then
  stops the filler and reports completion.
- Bottle Gate OFF is authoritative and forces the gate closed. Gate ON retains
  low-speed proof, admission handshake, close proof, timeout and critical-alarm
  abort behavior. CIP always overrides it closed.
- External bottle wash remains limited to Production RUN or Manual JOG with a
  positive tracked bottle count and a configurable 2.5 second normal OFF delay.

## HMI implementation

- Home: explicit mode code/status, Production ON/OFF, CIP Start, Bottle Gate
  ON/OFF, Product Pump ON/OFF, Run Out Product and bottle-wash status.
- Production (`operate` screen): production commands, speed/tank/vacuum values,
  pump/product/vacuum/gate/run-out status and all five logical product valves.
- Dedicated `cip` screen: Start/Stop/Reset, medium selection, sequence step,
  elapsed time, tank/vacuum values, pump states, five valves, media status,
  forced-closed gate and alarm summaries.
- Manual: interlocked Product Pump ON/OFF for residual product removal; leaving
  the page clears the manual pump command with the other manual commands.
- HMI data contract: 128 symbolic tags and 26 `Alarm_Word1` discrete alarms.

## Commissioning interfaces not assigned by software

The logical functions are complete, but these physical sources/outputs require
the approved electrical and I/O schedule before download:

| Symbolic interface | Required commissioning evidence |
|---|---|
| `Inp.ProductPresentAtPump` | Sensor address, polarity and wet/dry tests |
| `Inp.VacuumActualMbar` / `VacuumSignalValid` | Analog channel, scaling and fault limits |
| `Inp.BottleCountInMachine` | Validated bottle-tracker count |
| `Inp.BottleAdmissionComplete` | Admission-controller handshake |
| `Inp.MainSpeedActualPct` / `MainSpeedActualValid` | Drive feedback and validity source |
| `Inp.CIPValve217PathRequest` / `CIPValve213PathRequest` | Existing CIP-path controller ownership |
| `Out.Valve217Cmd`, `Out.Valve213Cmd`, `Out.Valve247Cmd` | Confirmed SMC/electrical output bits |
| `Out.ExternalBottleWashValveCmd` | Confirmed wash-valve solenoid/output |

## Offline validation result

- PLC source rebuild before the final elapsed-timer refinement: 0 errors,
  0 warnings. The refinement uses the same compiled TON pattern; a final attach
  attempt was blocked by the TIA Openness channel and must be repeated.
- HMI software rebuild: 128 tags, 100 used tags, 0 errors, 0 warnings; project
  saved successfully.
- Independent audit: 81 navigation, 32 command and 181 field checks. The first
  audit found no substantive navigation/command/tag defect; nine failures were
  verifier classifications for hidden/symbolic fields. The verifier was fixed,
  but its repeat attach was blocked by the same Openness channel.
- PLCSIM acceptance testing and physical I/O testing remain pending.
- No PLC or HMI download was performed.

