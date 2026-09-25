# Schlenker PILZ / PLC / HMI Implementation Report

Date: 2026-08-15  
Project: `C:\Users\SIMATIC User\Desktop\Schlenkers 36-10 190036-7-8v2.12\Backup Schlenkers 36-10 190036-7-8v2.12.ap19`  
Operating mode: offline only

## Outcome

The supplied PILZ engineering allocation was audited and converted into an implementation package. The local PNOZmulti 2 module/channel allocation is complete and documented. The HMI implementation tooling compiles successfully and is prepared to update the existing `safety_pilz_diagnostics` screen without touching Production, PLC logic, hardware configuration, network configuration or safety logic.

The TIA page transaction was not applied in this run because TIA Portal did not receive the required Openness authorization. The helper was terminated without saving changes. The official TIA project file timestamp and length remained unchanged.

## Completed

- Read and interpreted the supplied PILZ implementation specification.
- Verified the active TIA project identity and offline project path.
- Created a pre-change filesystem checkpoint containing 309 files and 37,868,179 bytes.
- Recorded the checkpoint `.ap19` SHA-256: `FC19FB2F197B9B50CDFC7661691CB41C0921D193030DC97A40655423A62B3E8E`.
- Documented every specified PNOZ m B0, EF 16DI, EF 8DI4DO and EF 1MM channel.
- Created a relative PROFINET logical-telegram proposal with every absolute PLC address explicitly marked `MISSING`.
- Created the safety-logic implementation worksheet and FAT checklist.
- Added a page-scoped Openness implementation mode for `safety_pilz_diagnostics`.
- Compiled the updated Openness helper successfully.
- Confirmed no HMI Safety Reset command is created.

## Not applied / blocked

- The HMI page has not yet been written into TIA Portal; Openness authorization was not granted during the transaction window.
- PLC and HMI rebuild evidence is therefore not available for this revision.
- An executable PNOZmulti safety project cannot be created because the native PNOZmulti project/software and final authorised safety design are not present.
- Absolute Siemens input/output addresses cannot be assigned because the GSDML device assignment, station name, IP configuration, telegram byte lengths and bit offsets are not present in the TIA project.
- Individual DR01-DR12 runtime tags remain uncommissioned.
- Earlier project material identifies 11 doors while this revision specifies 12; the physical inventory must be confirmed before changing aggregate guard semantics.

## Required next operator action

With the official project open offline, rerun the page-scoped transaction and approve the TIA Portal **Openness access** dialog using **Yes to all**. Then compile the HMI, validate navigation/layout, and capture build evidence before this task can be marked complete.
