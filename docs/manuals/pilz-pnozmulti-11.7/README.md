# PNOZmulti 11.7 reference set

This directory preserves the vendor documentation used while reviewing the
Schlenker PNOZmulti motion-monitoring project.

## Files

- `PNOZmulti_Configurator_HELP_EN_11.7.0.chm` — complete English online help
  installed with PNOZmulti Configurator 11.7.0.
- `PNOZ_m_EF_1MM_Operating_Manual_1003108-EN-16.pdf` — official Pilz
  operating manual for PNOZ m EF 1MM, document 1003108-EN-16.

## Safe speed monitoring topics

The CHM deliberately contains two similarly named **Safe speed monitoring
element** topics:

- topic `63050396509570443.html`: modules with unit version below 2;
- topic `63050396511649931.html`: modules from unit version 2.

The second topic states that it applies to PNOZ m EF 1MM/2MM from unit version
2. The physical unit version installed on the machine has not yet been
evidenced from its product label, so neither variant is asserted as the final
machine match here. Verify the unit version on the module label before using
version-specific limits or configuration behaviour.

Useful linked topics for the version-2 path:

- Configuration: `63050396511724939.html`
- Inputs and outputs: `63050396511694091.html`

## Provenance

- CHM source: local Pilz installation under
  `C:\ProgramData\Pilz\PNOZmulti Configurator_v11.7.0\AppData\Help\EN`.
- PDF source: official Pilz download URL
  `https://www.pilz.com/download/open/PNOZ_m_EF_1MM_Operat_Man_1003108-EN-16.pdf`.
- Retrieved/copied: 2026-10-31 (Australia/Sydney).

These files are vendor reference material. Pilz retains all applicable rights;
use the copies only for the project's internal engineering work.
