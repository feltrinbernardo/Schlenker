# TIA Portal project versioning

This hierarchy tracks the metadata and procedure used to version the Schlenker TIA Portal V19 project without placing Git metadata or repository artifacts inside the active engineering project.

## Active project

`C:\TIA Projects\Schlenkers 36-10 190036-7-8v2.12`

## External version vault

`C:\TIA Projects\Schlenkers 36-10 190036-7-8v2.12-VersionControl`

The external vault is a separate local Git repository. Large `.zip` folder snapshots and `.zap19` archives are stored through Git LFS. No remote is configured until a repository owner supplies or approves the destination.

## Repository contents

- `manifests/` — immutable version records, hashes, compile status and limitations.
- `scripts/new-tia-project-version.ps1` — safe snapshot generator in the main Schlenker repository.
- the curated PLC sources remain authoritative under `REV12/PLC_Sources/`.
- native TIA projects and archives remain excluded from the main Git repository.

## Snapshot policy

1. TIA Portal must be closed.
2. The snapshot destination must be outside the active project directory.
3. Include the `.ap19` manifest and the project data folders `AdditionalFiles`, `IM`, `src`, `System`, `UserFiles`, `Vci` and `XRef` when present.
4. Exclude transient `TMP` and `Logs` directories and unrelated nested backup projects.
5. Record SHA-256 hashes for the `.ap19`, snapshot package and any official `.zap19` archive.
6. Mark folder snapshots as non-official recovery copies. A formal release still requires an archive created by TIA Portal.
7. Never commit credentials, production connection details or online PLC captures.

## Restoring

An official `.zap19` archive is the preferred restoration method. A folder snapshot is a secondary offline recovery artifact and must be extracted to a new directory, opened offline in the matching TIA Portal version, compiled and validated before use.

