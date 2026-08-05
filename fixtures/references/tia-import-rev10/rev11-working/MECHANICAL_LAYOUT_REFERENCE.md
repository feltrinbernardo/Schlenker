# Mechanical Layout Reference - REV11 Working

## Source and permitted use

The operator supplied a raster image of drawing `FBS- Frame Tot Assy`, sheet
`1 / 2`, for layout use. The image may be used as the visual reference when
building the WinCC Unified machine-overview and diagnostics layouts.

For the visual screen hierarchy, navigation and reusable page patterns, also
see `HMI_LAYOUT_STYLE_REFERENCE.md`. The style photographs are from another
machine and do not define Schenker functions or tags.

This is a layout reference only. It is not an issued mechanical drawing, an
electrical schematic, a safety plan, or an authoritative source for dimensions,
door identities, wiring, I/O addresses, or safety-device locations.

## Visible layout information

- Approximate plan envelope: `4460 mm x 3708 mm`.
- Approximate overall height: `3013 mm`.
- The supplied sheet includes front/side elevations, a top view and an
  isometric view.
- The electrical board is shown on one side of the enclosure.
- A roof-mounted unit is labelled `FAN DIF-PUR 6-12`.
- The drawing contains numbered panel callouts from `1` through `11`, including
  combined callouts such as `3-4` and `8-9`.

All dimensions and positions must be checked against the controlled CAD source
before they are used for fabrication or field installation.

## HMI layout treatment

- Use a cleaned, simplified top or isometric view as the machine-overview
  background; keep operating buttons and alarm text visually separate from the
  drawing.
- The overview may show the 11 confirmed guard states after each PLC/Pilz guard
  tag is physically cross-checked against the drawing.
- Do not assume that drawing callout `1` equals `Guard 1`, or make any other
  callout-to-guard mapping, until the mechanical and electrical identifiers are
  reconciled.
- Confirm the operator-facing orientation for `Front`, `Rear`, `Right Infeed`
  and `Left Outfeed` before placing guard indicators or request stations.
- Indicate the electrical-board and air-filter/fan areas for maintenance
  navigation only. A displayed symbol must never be used as safety proof.
- Preserve the existing Main-page controls for machine lights and air filter;
  the layout image is a navigation/status aid, not a replacement for those
  controls.

## Required controlled handback

Before the native TIA Portal V19 HMI is released, obtain the original CAD/PDF,
confirm the drawing revision and machine identity, and issue an approved
callout-to-guard cross-reference for all 11 guarded doors.
