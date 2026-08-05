# Operator Panel Reference - REV11 Working

## Photo-confirmed arrangement

The operator supplied a photograph of the physical controls below the HMI.
The visible left-to-right order is:

1. `AUX. VOLTAGE` - illuminated pushbutton.
2. `MAN / AUT` - keyed two-position selector.
3. `OPEN / CLOSE DOORS` - illuminated pushbutton.
4. `RESET` - blue pushbutton.
5. `START` - green pushbutton.
6. `STOP` - red illuminated pushbutton.
7. Emergency stop - red mushroom operator with yellow background.

The photograph confirms labels, colour and physical order only. It does not
confirm contact type, normal state, voltage, PLC/Pilz channel, lamp circuit or
the selector's electrical polarity.

## Working PLC aliases

| Physical label | Working alias | Functional interpretation |
|---|---|---|
| AUX. VOLTAGE | `Panel_AuxiliaryResetPB` | Auxiliary/safety-circuit reset or enable used after all doors are closed; final electrical function must be confirmed. |
| MAN / AUT | `Panel_ModeSelectorAuto` | Logical mode-selector input; contact arrangement and polarity remain open. |
| OPEN / CLOSE DOORS | `DoorAccess_RequestPanelPB` | Starts the controlled request-to-open sequence. Closing remains a physical operator action; this button does not power-close a guard. |
| RESET | `Panel_AlarmResetPB` | Alarm reset after the auxiliary reset step. |
| START | `Panel_StartMachinePB` | Separate deliberate machine-start/restart command. |
| STOP | `Panel_StopMachinePB` | Machine stop command. |
| Emergency stop | Pilz series circuit | One aggregate HMI alarm is used; individual E-stop identification is not required. |

The final TIA V19 integration must define how the physical `MAN / AUT`
selector gates or overrides the Auto and Manual commands currently shown on the
HMI. No mode change may cause an automatic machine restart.
