[CmdletBinding()]
param(
    [switch]$Quiet,

    [string]$RepositoryRoot
)

$ErrorActionPreference = 'Stop'

$resolvedRepositoryRoot = if ($RepositoryRoot) {
    [System.IO.Path]::GetFullPath($RepositoryRoot)
}
else {
    [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
}
$lockPath = Join-Path $resolvedRepositoryRoot 'config\controlled-decisions\door-inventory-lock.json'
$inventoryPath = Join-Path $resolvedRepositoryRoot 'REV12\Documentation\REV12_Guard_Inventory.csv'
$udtPath = Join-Path $resolvedRepositoryRoot 'REV12\PLC_Sources\01A_UDT_OpenPoints.scl'
$openPointsPath = Join-Path $resolvedRepositoryRoot 'docs\PENDENZE_OPEN_POINTS_IT.md'
$sequencePath = Join-Path $resolvedRepositoryRoot 'fixtures\references\tia-import-rev10\DOOR_ACCESS_SEQUENCE.md'
$hmiPagesPath = Join-Path $resolvedRepositoryRoot 'REV12\HMI\REV12_HMI_Pages.md'

$requiredFiles = @(
    $lockPath,
    $inventoryPath,
    $udtPath,
    $openPointsPath,
    $sequencePath,
    $hmiPagesPath
)
foreach ($path in $requiredFiles) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Door-inventory lock validation cannot find: $path"
    }
}

$lock = Get-Content -LiteralPath $lockPath -Raw | ConvertFrom-Json
$expectedLocations = @(
    'Front 1',
    'Front 2',
    'Front 3',
    'Front 4',
    'Rear 1',
    'Rear 2',
    'Rear 3',
    'Rear 4',
    'Right Infeed 1',
    'Right Infeed 2',
    'Left Outfeed 1'
)

if ($lock.status -ne 'LOCKED') {
    throw 'Door inventory is not marked LOCKED.'
}
if ([int]$lock.doorCount -ne 11) {
    throw 'Door inventory lock must remain at 11 doors until explicit operator release.'
}
if ([int]$lock.accessRequestStationCount -ne 3) {
    throw 'Door inventory lock must retain three access-request stations.'
}
if ($lock.changeControl -ne 'EXPLICIT_OPERATOR_CONFIRMATION_REQUIRED') {
    throw 'Door inventory change control is missing or has been weakened.'
}
if (@($lock.doors).Count -ne 11) {
    throw 'Door inventory lock must contain exactly 11 door records.'
}

$inventory = @(Import-Csv -LiteralPath $inventoryPath)
if ($inventory.Count -ne 11) {
    throw "REV12_Guard_Inventory.csv contains $($inventory.Count) records; expected 11."
}

for ($index = 0; $index -lt 11; $index++) {
    $expectedChannel = $index + 1
    $expectedLocation = $expectedLocations[$index]
    $lockedDoor = $lock.doors[$index]
    $inventoryDoor = $inventory[$index]

    if ([int]$lockedDoor.channel -ne $expectedChannel -or
        [string]$lockedDoor.location -ne $expectedLocation) {
        throw "Lock record $expectedChannel does not match the controlled 11-door baseline."
    }
    if ([int]$inventoryDoor.Channel -ne $expectedChannel -or
        [string]$inventoryDoor.Location -ne $expectedLocation) {
        throw "Guard inventory record $expectedChannel does not match the locked baseline."
    }
}

$udtText = Get-Content -LiteralPath $udtPath -Raw
if ($udtText -notmatch 'Door\s*:\s*Array\[1\.\.11\]\s+of\s+"UDT_DoorDiagnostic"') {
    throw 'UDT_OpenPoints.Door must remain Array[1..11].'
}
if ($udtText -match 'Door\s*:\s*Array\[1\.\.12\]') {
    throw 'A 12-door PLC array is prohibited by the active door-inventory lock.'
}

$openPointsText = Get-Content -LiteralPath $openPointsPath -Raw
if ($openPointsText -notmatch 'baseline controllata della macchina conferma \*\*11 porte\*\*') {
    throw 'Open-points documentation no longer states the confirmed 11-door baseline.'
}
if ($openPointsText -notmatch 'non è approvata una dodicesima porta `DR12`') {
    throw 'Open-points documentation no longer rejects an unapproved DR12.'
}

$sequenceText = Get-Content -LiteralPath $sequencePath -Raw
if ($sequenceText -notmatch '### Guarded doors: 11') {
    throw 'Door-access sequence no longer identifies 11 guarded doors.'
}

$hmiPagesText = Get-Content -LiteralPath $hmiPagesPath -Raw
if ($hmiPagesText -notmatch 'All 11 doors closed / unlocked status') {
    throw 'HMI page specification no longer identifies the aggregate 11-door status.'
}

if (-not $Quiet) {
    [PSCustomObject]@{
        Status = 'PASS'
        Decision = $lock.decisionId
        DoorCount = 11
        AccessRequestStations = 3
        ChangeControl = $lock.changeControl
    }
}
