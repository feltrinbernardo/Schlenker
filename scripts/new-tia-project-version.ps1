[CmdletBinding()]
param(
    [Parameter()]
    [string]$ProjectRoot = 'C:\TIA Projects\Schlenkers 36-10 190036-7-8v2.12',

    [Parameter()]
    [string]$VaultRoot = 'C:\TIA Projects\Schlenkers 36-10 190036-7-8v2.12-VersionControl',

    [Parameter(Mandatory)]
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9._-]+$')]
    [string]$VersionId,

    [Parameter()]
    [string]$PlcSourceRoot,

    [Parameter()]
    [string]$OfficialArchive
)

$ErrorActionPreference = 'Stop'

$projectPath = [System.IO.Path]::GetFullPath($ProjectRoot).TrimEnd('\')
$vaultPath = [System.IO.Path]::GetFullPath($VaultRoot).TrimEnd('\')

if (-not (Test-Path -LiteralPath $projectPath -PathType Container)) {
    throw "TIA project directory was not found: $projectPath"
}

if ($vaultPath.Equals($projectPath, [System.StringComparison]::OrdinalIgnoreCase) -or
    $vaultPath.StartsWith($projectPath + '\', [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'The version vault must be outside the active TIA project directory.'
}

if (@(Get-Process -Name 'Siemens.Automation.Portal' -ErrorAction SilentlyContinue).Count -gt 0) {
    throw 'TIA Portal is running. Close TIA Portal before creating a folder snapshot.'
}

$projectFiles = @(Get-ChildItem -LiteralPath $projectPath -File -Filter '*.ap19')
if ($projectFiles.Count -ne 1) {
    throw "Expected exactly one .ap19 file at the project root; found $($projectFiles.Count)."
}

$snapshotDirectory = Join-Path $vaultPath 'snapshots'
$sourceDirectory = Join-Path (Join-Path $vaultPath 'sources') $VersionId
$archiveDirectory = Join-Path $vaultPath 'archives'
$manifestDirectory = Join-Path $vaultPath 'manifests'

foreach ($directory in @($vaultPath, $snapshotDirectory, $archiveDirectory, $manifestDirectory)) {
    if (-not (Test-Path -LiteralPath $directory)) {
        $null = New-Item -ItemType Directory -Path $directory
    }
}

$snapshotPath = Join-Path $snapshotDirectory ($VersionId + '.zip')
$manifestPath = Join-Path $manifestDirectory ($VersionId + '.json')
if (Test-Path -LiteralPath $snapshotPath) {
    throw "Snapshot already exists and will not be overwritten: $snapshotPath"
}
if (Test-Path -LiteralPath $manifestPath) {
    throw "Manifest already exists and will not be overwritten: $manifestPath"
}

$includedNames = @('AdditionalFiles', 'IM', 'src', 'System', 'UserFiles', 'Vci', 'XRef')
$includedPaths = [System.Collections.Generic.List[string]]::new()
foreach ($name in $includedNames) {
    $candidate = Join-Path $projectPath $name
    if (Test-Path -LiteralPath $candidate) {
        $includedPaths.Add($candidate)
    }
}
$includedPaths.Add($projectFiles[0].FullName)

Compress-Archive -LiteralPath $includedPaths.ToArray() -DestinationPath $snapshotPath -CompressionLevel Optimal

$sourceCopy = $null
if (-not [string]::IsNullOrWhiteSpace($PlcSourceRoot)) {
    $resolvedSource = [System.IO.Path]::GetFullPath($PlcSourceRoot)
    if (-not (Test-Path -LiteralPath $resolvedSource -PathType Container)) {
        throw "PLC source directory was not found: $resolvedSource"
    }
    if (Test-Path -LiteralPath $sourceDirectory) {
        throw "Versioned source directory already exists: $sourceDirectory"
    }
    $null = New-Item -ItemType Directory -Path $sourceDirectory
    Get-ChildItem -LiteralPath $resolvedSource -Force |
        Copy-Item -Destination $sourceDirectory -Recurse -Force
    $sourceCopy = $sourceDirectory
}

$archiveCopy = $null
$archiveHash = $null
if (-not [string]::IsNullOrWhiteSpace($OfficialArchive)) {
    $resolvedArchive = [System.IO.Path]::GetFullPath($OfficialArchive)
    if (-not (Test-Path -LiteralPath $resolvedArchive -PathType Leaf)) {
        throw "Official archive was not found: $resolvedArchive"
    }
    $archiveCopy = Join-Path $archiveDirectory ([System.IO.Path]::GetFileName($resolvedArchive))
    if (-not (Test-Path -LiteralPath $archiveCopy)) {
        Copy-Item -LiteralPath $resolvedArchive -Destination $archiveCopy
    }
    $archiveHash = (Get-FileHash -LiteralPath $archiveCopy -Algorithm SHA256).Hash
}

$limitations = @(
    'Folder snapshot is not an official TIA Portal archive.',
    'Review the recorded offline compile report before relying on restoration or release.'
)
if ($null -eq $archiveCopy) {
    $limitations += 'A new post-correction .zap19 archive is still required for formal release.'
}
else {
    $limitations += 'The referenced official archive contains this version; unresolved compile findings remain release hold points.'
}

$snapshotItem = Get-Item -LiteralPath $snapshotPath
$manifest = [ordered]@{
    schemaVersion = 1
    versionId = $VersionId
    createdLocal = [DateTimeOffset]::Now.ToString('o')
    createdUtc = [DateTimeOffset]::UtcNow.ToString('o')
    tiaVersion = 'V19'
    projectRoot = $projectPath
    projectFile = $projectFiles[0].Name
    projectFileSha256 = (Get-FileHash -LiteralPath $projectFiles[0].FullName -Algorithm SHA256).Hash
    snapshotType = 'OFFLINE_FOLDER_COPY_NON_OFFICIAL'
    snapshotPath = $snapshotPath
    snapshotBytes = $snapshotItem.Length
    snapshotSha256 = (Get-FileHash -LiteralPath $snapshotPath -Algorithm SHA256).Hash
    includedTopLevelItems = @($includedPaths | ForEach-Object { [System.IO.Path]::GetFileName($_) })
    excludedTopLevelItems = @('TMP', 'Logs', 'Backup Schlenkers 36-10 190036-7-8v2.13')
    plcSourceCopy = $sourceCopy
    officialArchiveCopy = $archiveCopy
    officialArchiveSha256 = $archiveHash
    onlineApisUsed = $false
    limitations = $limitations
}

$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $manifestPath -Encoding utf8

[PSCustomObject]@{
    Status = 'PASS'
    VersionId = $VersionId
    Snapshot = $snapshotPath
    SnapshotBytes = $snapshotItem.Length
    SnapshotSha256 = $manifest.snapshotSha256
    Manifest = $manifestPath
    PlcSources = $sourceCopy
    OfficialArchive = $archiveCopy
}
