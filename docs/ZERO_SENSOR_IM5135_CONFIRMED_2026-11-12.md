# Zero sensor identity update - 2026-11-12

## Evidence and current assignment

The operator explicitly confirmed that the photographed IFM IM5135 is the carousel zero/reference sensor connected to AL102/X6. This supersedes the historical IFS240 / AL101-X6 zero assignment. It does not establish the current use of AL101/X6.

Photo: `.codex-remote-attachments/01a0b2a8-9afd-7e40-8df2-f046eda2b7d7/f32b0d6d-8889-46f9-a373-e24491112c41/1-image-1794469853332.jpg`.

The operator also stated that the sensors have been replaced with IM5135. Individual additional port identities have not been enumerated; do not bulk-replace unrelated photoelectric, pressure, temperature or level devices, or treat every historical IFS240 port as field verified.

## Device and software implications

- Manufacturer model: IFM IM5135; inductive, PNP complementary outputs, 10-36 VDC, 200 Hz.
- Source: https://www.ifm.com/de/en/product/IM5135
- On an IO-Link master this device requires the appropriate digital-input/SIO configuration, not an assumed IO-Link process-data telegram. Actual native port mode and selected output pin/polarity remain to be checked.
- Preserve logical references `DB_Global.AL102Process.X6Valve1Reference` and `AL102_X6_Valve1_Reference` until a reviewed implementation changes them.
- Existing encoder remains IFM RO3101. No SICK encoder is installed according to the operator.
- IM5135 is a candidate reference sensor for EF1MM based on its signal class. Identity confirmation is not validation of the safety function or shared wiring.
- Before any EF1MM wiring release: verify raw-signal routing, shared-load/fault effects on CPU and AL102, mechanical pulse ratio, diagnostic reaction time, applicable module documentation and required safety performance. Complementary outputs from one IM5135 are not independent sensors.

## Scope of this update

Repository documentation only. No TIA/PILZ native project edits, hardware configuration changes, download, output activation, safety approval or physical continuity test performed.
