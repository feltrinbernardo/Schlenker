# PNOZ m EF 1MM encoder wiring correction

## Approved drawing basis

The installed Pilz `PNOZ m EF 1MM`, order number `772170`, is retained as the
safe-motion module for Axis 1. The existing DFS60S Pro encoder wiring shown on
electrical drawing sheet 31 is superseded by the `X12` connection below.

The confirmed PNOZmulti project position is `a3`. The physical rack order is
`PNOZ m ES -> PNOZ m B0 -> PNOZ m EF 1MM (a3) -> PNOZ m EF 16DI (a4) ->
PNOZ m EF 8DI4DO (a5)`, followed by the separately mounted safety relays.

| X12 pin | Pilz track | Existing DFS60S Pro conductor | Encoder function |
|---:|---|---|---|
| 1 | S | red | `+U` supply-voltage monitor input |
| 2 | GND | blue | encoder GND / 0 V |
| 3 | Z | purple | Z index |
| 4 | A | white | A track |
| 5 | /A | brown | inverted A track |
| 6 | /Z | yellow | inverted Z index |
| 7 | B | pink | B track |
| 8 | /B | black | inverted B track |

The encoder `+U` conductor is supplied from the approved 24 VDC PELV/SELV
supply and is monitored by X12 pin 1 `S`. X12 pin 1 is not a sensor power
output. Encoder GND and X12 pin 2 connect to the same 0 V reference. Connect
the cable shield to protective earth at one point only.

## Superseded connection

The former direct encoder allocation to Siemens `DI0` through `DI5` on sheet 31
is removed from the approved encoder path. Those Siemens channels require a
separate allocation review before reuse. Do not parallel the encoder tracks to
the Siemens inputs without an approved Pilz/encoder adapter and a documented
loading and safety assessment.

## Installation and commissioning hold

- Confirm the installed encoder label and conductor continuity before
  termination.
- Confirm that the PNOZmulti project assigns the installed `PNOZ m EF 1MM` to
  Axis 1 and uses the DFS60S Pro signal type.
- Complete the required safety validation before operation.
- Physical cabinet wiring is to be performed de-energised by a qualified
  electrical/safety technician.

