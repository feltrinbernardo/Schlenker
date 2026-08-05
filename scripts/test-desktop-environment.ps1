[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$codexExe = 'C:\Users\SIMATIC User\AppData\Local\Programs\OpenAI\Codex\bin\codex.exe'
$gitExe = 'C:\Program Files\Git\cmd\git.exe'
$gxWorksExe = 'C:\Program Files (x86)\MELSOFT\GPPW2\GD2.exe'
$globalConfigPath = 'C:\Users\SIMATIC User\.codex\config.toml'
$projectConfigPath = Join-Path $PSScriptRoot '..\.codex\config.toml'
$workingCopyPath = [System.IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot '..\fixtures\projects\schlenker-working\schlenker.gxw')
)
$sourcePath = 'D:\GX Works\schlenker.gxw'
$captureHelperPath = Join-Path $PSScriptRoot 'capture-gxworks2-window.ps1'
$expectedAppId = '{7C5A40EF-A0FB-4BFC-874A-C0F2E0B9FA8E}\MELSOFT\GPPW2\GD2.EXE'
$expectedSafeDirectory = [System.IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot '..\fixtures\projects\schlenker-working')
).TrimEnd('\') + '\'
$gxWorksAppKey = 'HKCU:\Software\MITSUBISHI\SWnDN-GPPW2\App'
$gxWorksRecentKey = 'HKCU:\Software\MITSUBISHI\SWnDN-GPPW2\MainFrame\Recent File List'

$globalConfig = Get-Content -LiteralPath $globalConfigPath -Raw
$projectConfig = Get-Content -LiteralPath $projectConfigPath -Raw
$pluginList = & $codexExe plugin list | Out-String
$machinePath = [Environment]::GetEnvironmentVariable('Path', 'Machine')
$sourceHash = (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash
$workingCopyHash = (Get-FileHash -LiteralPath $workingCopyPath -Algorithm SHA256).Hash
$captureValidation = & $captureHelperPath -SelfTest

$checks = [ordered]@{
    CodexInstalled = Test-Path -LiteralPath $codexExe
    GitInstalled = Test-Path -LiteralPath $gitExe
    GitRegisteredForNewProcesses = $machinePath.Split(';') -contains 'C:\Program Files\Git\cmd'
    GXWorks2Installed = Test-Path -LiteralPath $gxWorksExe
    ComputerUsePluginEnabled = $pluginList -match 'computer-use@openai-bundled\s+installed, enabled'
    GXWorks2AlwaysAllowed = $globalConfig.Contains($expectedAppId)
    ProjectConfigPinsComputerUse = $projectConfig -match 'computer-use@openai-bundled'
    ProjectUsesWorkspaceSandbox = $projectConfig -match 'sandbox_mode\s*=\s*"workspace-write"'
    DisposableCopyExists = Test-Path -LiteralPath $workingCopyPath
    OriginalAndCopyHashesMatch = $sourceHash -eq $workingCopyHash
    GXWorksDefaultFolderIsDisposable =
        (Get-ItemPropertyValue -LiteralPath $gxWorksAppKey -Name OneFilePrjFileDir) -eq $expectedSafeDirectory
    GXWorksRecentProjectIsDisposable =
        (Get-ItemPropertyValue -LiteralPath $gxWorksRecentKey -Name File1) -eq $workingCopyPath
    GXWorksCaptureHelperValid =
        $captureValidation.Status -eq 'PASS' -and
        $captureValidation.InputCapability -eq 'none' -and
        $captureValidation.UnsafeTitleRejection -eq 'PASS'
}

$failed = @($checks.GetEnumerator() | Where-Object { -not $_.Value })
$checks.GetEnumerator() | ForEach-Object {
    [PSCustomObject]@{
        Check = $_.Key
        Result = if ($_.Value) { 'PASS' } else { 'FAIL' }
    }
} | Format-Table -AutoSize

if ($failed.Count -gt 0) {
    throw "Desktop environment checks failed: $($failed.Key -join ', ')"
}

Write-Output "Source/copy SHA-256: $sourceHash"
Write-Output 'Desktop environment configuration: PASS'
