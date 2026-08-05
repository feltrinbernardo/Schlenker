[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [switch]$ConfirmDisconnected,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9_-]{0,63}$')]
    [string]$RunId
)

$ErrorActionPreference = 'Stop'

if (-not $ConfirmDisconnected) {
    throw 'Refusing to prepare the proof copy without a current disconnection confirmation.'
}
if (Get-Process -Name GD2 -ErrorAction SilentlyContinue) {
    throw 'GX Works2 must be closed before preparing the hybrid proof copy.'
}

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$sourcePath = [System.IO.Path]::GetFullPath(
    (Join-Path $repositoryRoot 'fixtures\projects\schlenker-working\schlenker.gxw')
)
$proofRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $repositoryRoot 'fixtures\projects\schlenker-hybrid-proof')
)
$proofDirectory = Join-Path $proofRoot $RunId
$destinationPath = Join-Path $proofDirectory 'schlenker-proof.gxw'

if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
    throw "Normal disposable working copy was not found: $sourcePath"
}
if (Test-Path -LiteralPath $destinationPath) {
    throw "Refusing to overwrite an existing hybrid proof copy: $destinationPath"
}

$sourceItem = Get-Item -LiteralPath $sourcePath
$sourceHashBefore = (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash
$sourceLengthBefore = $sourceItem.Length
$sourceWriteTimeBefore = $sourceItem.LastWriteTimeUtc

New-Item -ItemType Directory -Path $proofDirectory -Force | Out-Null
$temporaryPath = Join-Path $proofDirectory (
    '.schlenker-proof-' + [guid]::NewGuid().ToString('N') + '.tmp'
)
Copy-Item -LiteralPath $sourcePath -Destination $temporaryPath

$temporaryHash = (Get-FileHash -LiteralPath $temporaryPath -Algorithm SHA256).Hash
if ($temporaryHash -ne $sourceHashBefore) {
    throw "Proof-copy hash mismatch; temporary file retained: $temporaryPath"
}
Move-Item -LiteralPath $temporaryPath -Destination $destinationPath

$sourceItemAfter = Get-Item -LiteralPath $sourcePath
$sourceHashAfter = (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash
if ($sourceHashAfter -ne $sourceHashBefore -or
    $sourceItemAfter.Length -ne $sourceLengthBefore -or
    $sourceItemAfter.LastWriteTimeUtc -ne $sourceWriteTimeBefore) {
    throw 'The normal disposable working copy changed while preparing the proof copy.'
}

$manifest = [ordered]@{
    prepared_at = [DateTimeOffset]::Now.ToString('o')
    run_id = $RunId
    disconnection_confirmed_by_operator = $true
    source = [ordered]@{
        path = $sourcePath
        length = $sourceLengthBefore
        last_write_utc = $sourceWriteTimeBefore.ToString('o')
        sha256 = $sourceHashBefore
    }
    proof_copy = [ordered]@{
        path = $destinationPath
        sha256 = (Get-FileHash -LiteralPath $destinationPath -Algorithm SHA256).Hash
    }
}
$manifestPath = Join-Path $proofDirectory 'proof-copy-manifest.json'
$manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $manifestPath -Encoding UTF8

[PSCustomObject]@{
    Status = 'PASS'
    RunId = $RunId
    ProjectPath = $destinationPath
    ManifestPath = $manifestPath
    SHA256 = $manifest.proof_copy.sha256
}
