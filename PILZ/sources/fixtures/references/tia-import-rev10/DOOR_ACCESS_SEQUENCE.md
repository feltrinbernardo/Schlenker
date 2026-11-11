# Guarded-Door Access Request Sequence

## Scope

This document records the operator-confirmed functional sequence for requesting
access through the machine's guarded doors. It is an offline software and FAT
requirement, not a validated safety design. The Pilz safety project, electrical
schematics, risk assessment, and commissioning validation remain authoritative.

`Request Open Door` is separate from the pneumatic manual command
`Gate Open (-125Y1)`.

## HMI command and indication

- Navigation: `Home > Machine > Door Access`
- Three physical momentary request pushbuttons initiate the same sequence:
  panel station, front station, and rear station.
- Each request station has a blue indication lamp.
- The HMI shall show the current access-sequence state and the reason for any
  blocked transition.
- The request shall never directly unlock a guard or bypass a safety input.

The REV11 working lamp convention is flashing while the request is being
processed or a coordination fault is active, and steady while the doors are
unlocked, access/reset is active, or restart is permitted. This indication is
not a safety signal and requires operator confirmation during HMI/FAT review.

## Confirmed machine inventory

### Guarded doors: 11

- Front: 4 doors (`Front 1` through `Front 4`)
- Rear: 4 doors (`Rear 1` through `Rear 4`)
- Right/infeed side: 2 doors (`Right Infeed 1` and `Right Infeed 2`)
- Left/outfeed side: 1 door (`Left Outfeed 1`)

### Emergency stops: 4

- Front
- Rear
- Infeed-door area
- Jog pendant

The operator confirmed that the four emergency-stop contacts are wired in
series. Pressing any one removes the 24 VDC return to the Pilz safety PLC. The
standard PLC/HMI diagnostic alias is `Safety_EStopChain24VHealthy`; it is a
read-only status derived from the validated safety system and is not a
substitute for the safety input.

The HMI uses one aggregate alarm, `Emergency stop pressed`. Per the operator's
requirement, it does not identify which of the four devices was operated.

### Request Open Door stations: 3

- Panel station below the HMI, with blue lamp
- Front station, with blue lamp
- Rear station, with blue lamp

### Physical controls below the HMI: 6

- Emergency stop (treated as the previously counted front E-stop unless the
  operator confirms it is an additional fifth device)
- Start Machine
- Stop Machine
- Request Open Door, with blue lamp
- Auxiliary Reset
- Alarm Reset

These are physical panel devices, not touchscreen commands. The guarded-access
sequence uses fresh rising edges from the physical Auxiliary Reset and Alarm
Reset pushbuttons. Restart permission never starts the machine; the operator
must press the separate physical Start Machine button.

The Pilz system remains authoritative for all 11 door channels and the
four-device series emergency-stop circuit. Available channel states are
provided to the standard PLC/HMI for diagnostics only and must not replace the
validated safety logic.

## Required sequence

1. The operator presses any physical `Request Open Door` pushbutton.
2. The standard PLC records an access request and initiates a controlled machine
   stop.
3. The sequence waits for confirmed machine standstill and the required
   three-phase power-removal feedback.
4. Only the validated safety system may authorize and energize the guard-unlock
   function. The HMI displays `Doors unlocked` only from confirmed feedback.
5. Opening any guarded door keeps restart permission false.
6. After access is complete, every guarded door must be physically closed.
7. The operator deliberately resets the auxiliary/safety circuit using the
   physical Auxiliary Reset pushbutton below the HMI.
8. Alarm reset is permitted only after the doors are closed and the safety
   circuit reports a valid closed state.
9. Successful reset returns the machine to a restart-permitted/ready state. It
   must not start the machine automatically.
10. The operator must press the separate physical Start Machine pushbutton to
    restart the machine.

## Functional state model

`RUNNING -> ACCESS_REQUESTED -> CONTROLLED_STOP -> POWER_OFF_CONFIRMED -> DOORS_UNLOCKED -> ACCESS_ACTIVE -> DOORS_CLOSED -> AUX_RESET_REQUIRED -> ALARM_RESET_REQUIRED -> SAFETY_CIRCUIT_CLOSED -> RESTART_PERMITTED`

Any loss of a required permissive returns the sequence to a non-startable state.
Power restoration, safety reset, alarm reset, and Start must not be combined
into one automatic action.

## Minimum feedbacks and interlocks

- controlled stop complete;
- zero-speed or standstill confirmation appropriate to the machine risk;
- three-phase contactor/power-off feedback required by the validated design;
- per-door closed feedback;
- per-door locked/unlocked feedback where fitted;
- all-doors-closed aggregate;
- safety circuit closed/healthy feedback from the Pilz system;
- auxiliary reset acknowledgement;
- active access request and access-sequence state;
- restart permission;
- independent Start command.

## Failure behavior

- Failure to stop, remove the required power, or obtain unlock feedback shall
  block door-release completion and generate a diagnostic alarm.
- A contradictory door signal (open and closed, or locked and unlocked) shall
  block restart permission.
- A door opened during reset or restart preparation shall cancel restart
  permission and require the defined reset sequence again.
- Communication loss between the standard PLC/HMI and the safety system shall
  fail to a non-startable state; it shall not create an unlock or restart
  permission.
- Alarm acknowledgement must not clear the physical cause or substitute for a
  safety reset.

## Open validation points

- Electrical equipment identifiers and Pilz channel mapping for the 11 doors.
- Electrical equipment identifiers and Pilz channel mapping for the four
  emergency stops and three door-request stations.
- Whether the required three-phase isolation is machine-wide or limited to
  specified hazardous drives/actuators.
- Authoritative contactor, standstill, door-closed, door-lock, auxiliary-reset,
  and safety-circuit signals from the electrical and Pilz designs.
- Unlock-all behavior versus individually controlled guard locks.
- Final blue-lamp behavior and physical output mapping for all three request
  stations.
- Maximum stop and unlock times and the required timeout alarms.
- Reset control location, visibility requirements, and anti-tie-down behavior.

No PLC/HMI software shall be treated as safety validated until these points and
the complete sequence have been reviewed and tested by the responsible safety
engineer.

## Offline implementation status

The standard-PLC coordination draft is implemented in the separate
`rev11-working/source/` set:

- `01A_UDT_DoorAccess.scl`
- `10A_FB_DoorAccess.scl`
- `12A_HMI_DoorAccess_Tags.csv`
- `12B_HMI_DoorAccess_StateText.csv`
- `12C_HMI_DoorAccess_Alarms.csv`

`FB_MainState`, `FB_ManualValveOverride`, and the OB1 call-order specification
in that working set were updated for integration. The original REV10 source
set remains unchanged. The REV11 working set has received static checks only;
it has not been imported or compiled in TIA Portal V19.
