[CmdletBinding()]
param(
    [string]$ProjectPath = (Join-Path $PSScriptRoot '..\fixtures\projects\schlenker-working\schlenker.gxw')
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$fixtureRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $repositoryRoot 'fixtures\projects')
).TrimEnd('\') + '\'
$safeProject = [System.IO.Path]::GetFullPath($ProjectPath)
$safeDirectory = [System.IO.Path]::GetDirectoryName($safeProject).TrimEnd('\') + '\'
$appKey = 'HKCU:\Software\MITSUBISHI\SWnDN-GPPW2\App'
$recentKey = 'HKCU:\Software\MITSUBISHI\SWnDN-GPPW2\MainFrame\Recent File List'

if (-not $safeProject.StartsWith(
    $fixtureRoot,
    [System.StringComparison]::OrdinalIgnoreCase
)) {
    throw "GX Works2 safe project must stay under $fixtureRoot"
}
if ([System.IO.Path]::GetExtension($safeProject) -ne '.gxw') {
    throw "GX Works2 safe project must be a .gxw file: $safeProject"
}
if (-not (Test-Path -LiteralPath $safeProject)) {
    throw "Disposable GX Works2 project not found: $safeProject"
}
if (-not (Test-Path -LiteralPath $appKey) -or -not (Test-Path -LiteralPath $recentKey)) {
    throw 'Expected GX Works2 user registry keys were not found.'
}

Set-ItemProperty -LiteralPath $appKey -Name OneFilePrjFileDir -Value $safeDirectory
Set-ItemProperty -LiteralPath $recentKey -Name File1 -Value $safeProject

$configuredDirectory = Get-ItemPropertyValue -LiteralPath $appKey -Name OneFilePrjFileDir
$configuredRecentFile = Get-ItemPropertyValue -LiteralPath $recentKey -Name File1
if ($configuredDirectory -ne $safeDirectory -or $configuredRecentFile -ne $safeProject) {
    throw 'GX Works2 safe-path settings did not persist.'
}

[PSCustomObject]@{
    DefaultProjectDirectory = $configuredDirectory
    MostRecentProject = $configuredRecentFile
    Status = 'PASS'
}
