[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectPath,

    [Parameter(Mandatory = $true)]
    [string]$BuildExe,

    [Parameter(Mandatory = $true)]
    [string]$AuditExe,

    [Parameter(Mandatory = $true)]
    [string]$OutputRoot,

    [string[]]$Screens = @(
        'operate', 'function', 'recipe', 'setup', 'manual', 'efficiency',
        'cip', 'diagnostics', 'safety', 'settings_inputs', 'settings_outputs',
        'settings_external_wash', 'lift_warning', 'io_diagnostics',
        'io_link_overview', 'io_link_al100', 'io_link_al101', 'io_link_al102',
        'io_link_al103', 'io_link_al104', 'safety_pilz_diagnostics', 'cip_edit',
        'alarms'
    )
)

$ErrorActionPreference = 'Stop'
$projectHint = [System.IO.Path]::GetFileName($ProjectPath)
$resultsPath = Join-Path $OutputRoot 'page-transaction-results.tsv'

if (-not (Test-Path -LiteralPath $ProjectPath -PathType Leaf)) {
    throw "Project file not found: $ProjectPath"
}
if (-not (Test-Path -LiteralPath $BuildExe -PathType Leaf)) {
    throw "Build executable not found: $BuildExe"
}
if (-not (Test-Path -LiteralPath $AuditExe -PathType Leaf)) {
    throw "Audit executable not found: $AuditExe"
}

New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null
"Screen`tStatus`tApplyExit`tCompileErrors`tCompileWarnings`tFrameStatus`tIssueCount`tNotes" |
    Set-Content -LiteralPath $resultsPath -Encoding UTF8

foreach ($screen in $Screens) {
    $screenDirectory = Join-Path (Join-Path $OutputRoot 'pages') $screen
    New-Item -ItemType Directory -Path $screenDirectory -Force | Out-Null
    $applyLog = Join-Path $screenDirectory 'apply-compile.log'
    $applyErrorLog = Join-Path $screenDirectory 'apply-compile.err.log'
    $auditLog = Join-Path $screenDirectory 'frame-audit.log'
    $auditErrorLog = Join-Path $screenDirectory 'frame-audit.err.log'
    $auditTsv = Join-Path $screenDirectory 'frame-audit.tsv'

    Write-Output ("PAGE_START={0}|UTC={1}" -f $screen, [DateTime]::UtcNow.ToString('o'))
    & $BuildExe $projectHint $screenDirectory $ProjectPath '--global-common-rebuild' $screen 1> $applyLog 2> $applyErrorLog
    $applyExit = $LASTEXITCODE
    $applyText = if (Test-Path -LiteralPath $applyLog) {
        Get-Content -LiteralPath $applyLog -Raw
    } else { '' }
    $compileErrors = if ($applyText -match 'COMPILE_ERRORS=(\d+)') { $Matches[1] } else { 'UNKNOWN' }
    $compileWarnings = if ($applyText -match 'COMPILE_WARNINGS=(\d+)') { $Matches[1] } else { 'UNKNOWN' }

    if ($applyExit -ne 0 -or $compileErrors -ne '0' -or $compileWarnings -ne '0') {
        $notes = 'Apply or compile failed; page transaction stopped before audit.'
        $resultLine = "{0}`tBLOCKED`t{1}`t{2}`t{3}`tNOT RUN`t`t{4}" -f $screen, $applyExit, $compileErrors, $compileWarnings, $notes
        $resultLine | Add-Content -LiteralPath $resultsPath -Encoding UTF8
        Write-Output ("PAGE_BLOCKED={0}|APPLY_EXIT={1}|ERRORS={2}|WARNINGS={3}" -f $screen, $applyExit, $compileErrors, $compileWarnings)
        continue
    }

    & $AuditExe $ProjectPath $auditTsv $screen 1> $auditLog 2> $auditErrorLog
    $auditExit = $LASTEXITCODE
    $frameStatus = 'UNKNOWN'
    $issueCount = 'UNKNOWN'
    if ($auditExit -eq 0 -and (Test-Path -LiteralPath $auditTsv)) {
        $auditRow = Import-Csv -LiteralPath $auditTsv -Delimiter "`t" | Select-Object -First 1
        if ($null -ne $auditRow) {
            $frameStatus = $auditRow.Status
            $issueCount = $auditRow.IssueCount
        }
    }

    if ($auditExit -eq 0 -and $frameStatus -eq 'VALIDATED' -and $issueCount -eq '0') {
        $resultLine = "{0}`tVALIDATED`t{1}`t{2}`t{3}`t{4}`t{5}`tIndependent page transaction passed." -f $screen, $applyExit, $compileErrors, $compileWarnings, $frameStatus, $issueCount
        $resultLine | Add-Content -LiteralPath $resultsPath -Encoding UTF8
        Write-Output ("PAGE_VALIDATED={0}|ERRORS=0|WARNINGS=0|FRAME_ISSUES=0" -f $screen)
    }
    else {
        $notes = 'Compile passed but frame audit did not validate; page requires inspection.'
        $resultLine = "{0}`tBLOCKED`t{1}`t{2}`t{3}`t{4}`t{5}`t{6}" -f $screen, $applyExit, $compileErrors, $compileWarnings, $frameStatus, $issueCount, $notes
        $resultLine | Add-Content -LiteralPath $resultsPath -Encoding UTF8
        Write-Output ("PAGE_BLOCKED={0}|AUDIT_EXIT={1}|FRAME={2}|ISSUES={3}" -f $screen, $auditExit, $frameStatus, $issueCount)
    }
}

Write-Output ("RUN_COMPLETE={0}|UTC={1}" -f $resultsPath, [DateTime]::UtcNow.ToString('o'))
