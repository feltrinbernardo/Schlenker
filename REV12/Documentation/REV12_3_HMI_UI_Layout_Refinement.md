# REV12.3 HMI UI Layout Refinement

Date: 2026-08-07  
Target: TIA Portal V19 / MTP1500 Unified Comfort

## Implemented

- Preserved the existing colors, Siemens Sans typography, command behavior and symbolic tag assignments.
- Retained the shared 1366 x 768 page structure and common content margins.
- Applied a uniform 12-pixel corner radius to the Infeed, Filler, Capper, Outfeed and Filter Service shapes.
- Extended the common navigation rail to the Diagnostics page.
- Created a Diagnostics page using the same header, status bar, panels, navigation and footer as the other REV12 pages.
- Used existing read-only tags for network, safety/access and process diagnostic values.
- Added a clean OPERATE navigation button between Safety and Production on all 12 screens and tightened the 12-button rail spacing without overlap.
- Created a dedicated Unified screen named `production`; OPERATE now opens `operate` and PRODUCTION opens `production`.
- Removed stale Home and Production objects left by earlier layouts and moved the Run Out Active/Complete fields fully inside the Process Commands panel.
- Archived the saved project as the official 2026-08-07 HMI baseline before applying the superseding design clarification.
- Preserved the Machine Overview arrangement.
- Rebuilt Main Auxiliaries to the Bernardo reference pattern with separate Lights and Air Filter rows, aligned status fields and a right-side Filter Service card.
- Deleted all safety/door-access objects from Home, expanded Process Commands into the full right-side area and limited it to process devices.
- Created a dedicated Safety & Door Access screen and added Safety to the shared navigation rail.
- Expanded the audit to check overlap between every visible foreground label/value pair on every screen, in addition to button and boundary checks.
- Realigned the Production process-status label/value columns found in the current `v2.1` project copy.

## TIA Portal Result

- HMI tags: 128
- Used HMI tags: 102
- HMI compile errors: 0
- HMI compile warnings: 0
- Project saved successfully.

## Verification Result

- Navigation checks: 144
- Command checks: 33
- Field and symbolic-link checks: 231
- Layout boundary, rounded-style, button-to-button, button-to-foreground and foreground-to-foreground overlap checks: 24808
- Failures: 0

Latest evidence is stored in `outputs/019fca72-rev12-hmi-tags/REV12_HMI_Dedicated_Production_Page_Final_Build.stdout.txt`, `REV12_HMI_Dedicated_Production_Page_Final_Audit.tsv`, and `REV12_HMI_Dedicated_Production_Page_Inventory.txt`.

No PLC or HMI hardware download was performed.
