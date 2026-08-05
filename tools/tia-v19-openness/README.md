# TIA Portal V19 Offline Project Creator

`CreateOfflineProject.cs` is a local Siemens TIA Portal Openness helper. It
creates and saves a new `.ap19` project only; it contains no online, download,
CPU control or device-memory operations.

## Current prerequisite

The Windows account running this helper must be a member of the local
`Siemens TIA Openness` group. An administrator must make that change outside
Codex, then the user must sign out and back in (or restart Windows) so the new
group token is active.

After that prerequisite is satisfied, rebuild the helper against:

`C:\Program Files\Siemens\Automation\Portal V19\PublicAPI\V19\Siemens.Engineering.dll`

The target project for this package is:

`fixtures/references/tia-import-rev10/rev11-working/tia-v19/Schenker_REV11_V19.ap19`

The helper refuses to run if any `.ap19` project already exists below its
target directory, preventing accidental overwrite.
