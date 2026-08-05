[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [switch]$ConfirmDisconnected,

    [string]$SourcePath = 'D:\GX Works\schlenker.gxw',

    [string]$DestinationPath = (Join-Path $PSScriptRoot '..\fixtures\projects\schlenker-working\schlenker.gxw')
)

$ErrorActionPreference = 'Stop'

if (-not $ConfirmDisconnected) {
    throw 'Refusing to prepare the fixture without explicit confirmation that GX Works2 is disconnected from all physical PLC communication paths.'
}

$sourceItem = Get-Item -LiteralPath $SourcePath
if ($sourceItem.PSIsContainer -or $sourceItem.Extension -ne '.gxw') {
    throw "The source must be a GX Works project file with a .gxw extension: $SourcePath"
}

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$fixtureRoot = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot 'fixtures\projects\schlenker-working'))
$resolvedDestination = [System.IO.Path]::GetFullPath($DestinationPath)
$fixturePrefix = $fixtureRoot.TrimEnd('\') + '\'

if (-not $resolvedDestination.StartsWith($fixturePrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Destination must remain inside $fixtureRoot"
}

$openSourceWindow = Get-Process -Name GD2 -ErrorAction SilentlyContinue |
    Where-Object { $_.MainWindowTitle -like "*$($sourceItem.FullName)*" }
if ($openSourceWindow) {
    throw 'GX Works2 still has the source project open. Close it safely before preparing the fixture.'
}

try {
    $readProbe = [System.IO.File]::Open(
        $sourceItem.FullName,
        [System.IO.FileMode]::Open,
        [System.IO.FileAccess]::Read,
        [System.IO.FileShare]::Read
    )
    $readProbe.Close()
}
catch {
    throw "The source project is locked or unreadable. Close GX Works2 before preparing the fixture. $($_.Exception.Message)"
}

if (Test-Path -LiteralPath $resolvedDestination) {
    throw "The working copy already exists; refusing to overwrite it: $resolvedDestination"
}

New-Item -ItemType Directory -Path $fixtureRoot -Force | Out-Null

$sourceHashBefore = (Get-FileHash -LiteralPath $sourceItem.FullName -Algorithm SHA256).Hash
$sourceLengthBefore = $sourceItem.Length
$sourceWriteTimeBefore = $sourceItem.LastWriteTimeUtc
$temporaryPath = Join-Path $fixtureRoot ('.schlenker-' + [guid]::NewGuid().ToString('N') + '.tmp')

Copy-Item -LiteralPath $sourceItem.FullName -Destination $temporaryPath
$temporaryHash = (Get-FileHash -LiteralPath $temporaryPath -Algorithm SHA256).Hash
if ($temporaryHash -ne $sourceHashBefore) {
    throw "The copied file hash does not match the source. The temporary file was retained for diagnosis: $temporaryPath"
}

Move-Item -LiteralPath $temporaryPath -Destination $resolvedDestination

$sourceItemAfter = Get-Item -LiteralPath $sourceItem.FullName
$sourceHashAfter = (Get-FileHash -LiteralPath $sourceItemAfter.FullName -Algorithm SHA256).Hash
if (
    $sourceHashAfter -ne $sourceHashBefore -or
    $sourceItemAfter.Length -ne $sourceLengthBefore -or
    $sourceItemAfter.LastWriteTimeUtc -ne $sourceWriteTimeBefore
) {
    throw 'The original project metadata changed while the working copy was being prepared.'
}

$manifest = [ordered]@{
    prepared_at = [DateTimeOffset]::Now.ToString('o')
    disconnection_confirmed_by_operator = $true
    source = [ordered]@{
        path = $sourceItem.FullName
        length = $sourceLengthBefore
        last_write_utc = $sourceWriteTimeBefore.ToString('o')
        sha256 = $sourceHashBefore
    }
    working_copy = [ordered]@{
        path = $resolvedDestination
        sha256 = (Get-FileHash -LiteralPath $resolvedDestination -Algorithm SHA256).Hash
    }
}

$manifestPath = Join-Path $fixtureRoot 'fixture-manifest.json'
$manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $manifestPath -Encoding UTF8

$manifest
