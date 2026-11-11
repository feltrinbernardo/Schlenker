[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectPath,

    [Parameter(Mandatory = $true)]
    [string]$BuildExe,

    [Parameter(Mandatory = $true)]
    [string]$AuditExe,

    [Parameter(Mandatory = $true)]
    [string]$CheckpointArchive,

    [Parameter(Mandatory = $true)]
    [string]$OutputRoot,

    [string[]]$Screens = @(
        'alarms', 'cip', 'cip_edit', 'diagnostics', 'efficiency', 'function',
        'home', 'io_diagnostics', 'io_link_al100', 'io_link_al101',
        'io_link_al102', 'io_link_al103', 'io_link_al104', 'io_link_overview',
        'lift_warning', 'manual', 'operate', 'recipe', 'safety',
        'safety_pilz_diagnostics', 'settings_external_wash', 'settings_inputs',
        'settings_outputs', 'settings_timers', 'setup'
    )
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$ProjectPath = [System.IO.Path]::GetFullPath($ProjectPath)
$resultsPath = Join-Path $OutputRoot 'completion-ledger.tsv'

foreach ($requiredFile in @($ProjectPath, $BuildExe, $AuditExe, $CheckpointArchive)) {
    if (-not (Test-Path -LiteralPath $requiredFile -PathType Leaf)) {
        throw "Required file not found: $requiredFile"
    }
}

. (Join-Path $PSScriptRoot 'tia-project-lease.ps1')
$lease = Enter-TiaProjectLease -ProjectPath $ProjectPath
$helperInFlight = $false
try {
# Native state cannot be reconstructed from a page-name-only ledger.
# Do not replay or skip a prior run; reconcile it and use a fresh output directory.
if ((Test-Path -LiteralPath $OutputRoot) -and
    @(Get-ChildItem -LiteralPath $OutputRoot -Force).Count -gt 0) {
    throw 'RESUME_REQUIRES_RECONCILIATION: OutputRoot is not empty. Preserve the previous evidence; reconcile native state before starting a fresh run.'
}
if ($Screens.Count -eq 0 -or @($Screens | Select-Object -Unique).Count -ne $Screens.Count -or
    @($Screens | Where-Object { $_ -notmatch '^[A-Za-z0-9_-]+$' }).Count -gt 0) {
    throw 'Screens must contain unique, nonempty screen names without path separators.'
}
New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null
if (-not (Test-Path -LiteralPath $resultsPath -PathType Leaf)) {
    "Screen`tStatus`tApplyExit`tCompileErrors`tCompileWarnings`tAuditExit`tFrameStatus`tIssueCount`tUTC`tNotes" |
        Set-Content -LiteralPath $resultsPath -Encoding UTF8
}

foreach ($screen in $Screens) {
    $screenDirectory = Join-Path (Join-Path $OutputRoot 'pages') $screen
    New-Item -ItemType Directory -Path $screenDirectory -Force | Out-Null
    $applyLog = Join-Path $screenDirectory 'apply-compile.log'
    $applyErrorLog = Join-Path $screenDirectory 'apply-compile.err.log'
    $auditLog = Join-Path $screenDirectory 'frame-audit.log'
    $auditErrorLog = Join-Path $screenDirectory 'frame-audit.err.log'
    $auditTsv = Join-Path $screenDirectory 'frame-audit.tsv'
    $utc = [DateTime]::UtcNow.ToString('o')

    Write-Output ("PAGE_START={0}|UTC={1}" -f $screen, $utc)
    $helperInFlight = $true
    & $BuildExe $ProjectPath $screenDirectory $CheckpointArchive '--global-common-render-fix' $screen `
        1> $applyLog 2> $applyErrorLog
    $applyExit = $LASTEXITCODE
    $helperInFlight = $false
    $applyText = if (Test-Path -LiteralPath $applyLog) {
        Get-Content -LiteralPath $applyLog -Raw
    } else { '' }
    $compileErrors = if ($applyText -match 'COMPILE_ERRORS=(\d+)') { $Matches[1] } else { 'UNKNOWN' }
    $compileWarnings = if ($applyText -match 'COMPILE_WARNINGS=(\d+)') { $Matches[1] } else { 'UNKNOWN' }
    $statusPass = $applyText -match '(?m)^STATUS=PASS\r?$'

    if ($applyExit -ne 0 -or -not $statusPass -or
        $compileErrors -ne '0' -or $compileWarnings -ne '0') {
        $line = "{0}`tBLOCKED`t{1}`t{2}`t{3}`tNOT RUN`tNOT RUN`t`t{4}`tApply/compile failed; page transaction stopped." -f `
            $screen, $applyExit, $compileErrors, $compileWarnings, $utc
        $line | Add-Content -LiteralPath $resultsPath -Encoding UTF8
        Write-Output ("PAGE_BLOCKED={0}|APPLY_EXIT={1}|ERRORS={2}|WARNINGS={3}" -f `
            $screen, $applyExit, $compileErrors, $compileWarnings)
        Write-Output "RUN_STATUS=BLOCKED;RECONCILIATION_REQUIRED"
        exit 1
    }

    # AuditCommonRenderTarget contract: project, screen, report.
    $helperInFlight = $true
    & $AuditExe $ProjectPath $screen $auditTsv 1> $auditLog 2> $auditErrorLog
    $auditExit = $LASTEXITCODE
    $helperInFlight = $false
    $frameStatus = 'UNKNOWN'
    $issueCount = 'UNKNOWN'
    if ($auditExit -eq 0 -and (Test-Path -LiteralPath $auditTsv)) {
        $auditRow = @(Import-Csv -LiteralPath $auditTsv -Delimiter "`t")
        if ($auditRow.Count -eq 1 -and $auditRow[0].Screen -ceq $screen) {
            $frameStatus = $auditRow[0].Status
            $issueCount = $auditRow[0].IssueCount
        }
    }

    if ($auditExit -eq 0 -and $frameStatus -eq 'VALIDATED' -and $issueCount -eq '0') {
        $line = "{0}`tVALIDATED`t{1}`t{2}`t{3}`t{4}`t{5}`t{6}`t{7}`tPage-scoped apply, HMI compile, save, and independent frame audit passed." -f `
            $screen, $applyExit, $compileErrors, $compileWarnings, $auditExit, $frameStatus, $issueCount, $utc
        $line | Add-Content -LiteralPath $resultsPath -Encoding UTF8
        Write-Output ("PAGE_VALIDATED={0}|ERRORS=0|WARNINGS=0|FRAME_ISSUES=0" -f $screen)
    }
    else {
        $line = "{0}`tBLOCKED`t{1}`t{2}`t{3}`t{4}`t{5}`t{6}`t{7}`tCompile/save passed, but independent frame audit did not validate." -f `
            $screen, $applyExit, $compileErrors, $compileWarnings, $auditExit, $frameStatus, $issueCount, $utc
        $line | Add-Content -LiteralPath $resultsPath -Encoding UTF8
        Write-Output ("PAGE_BLOCKED={0}|AUDIT_EXIT={1}|FRAME={2}|ISSUES={3}" -f `
            $screen, $auditExit, $frameStatus, $issueCount)
        Write-Output "RUN_STATUS=BLOCKED;RECONCILIATION_REQUIRED"
        exit 1
    }
}

Write-Output ("RUN_COMPLETE={0}|UTC={1}" -f $resultsPath, [DateTime]::UtcNow.ToString('o'))

Write-Output "RUN_STATUS=PASS"
exit 0

}
finally {
    if ($helperInFlight) {
        # Cancellation/launch exceptions do not prove the child stopped.
        $lease.Stream.Dispose()
        Write-Warning "PROJECT_LEASE_RECONCILIATION_REQUIRED: $($lease.Path)"
    }
    else {
        Exit-TiaProjectLease -Lease $lease
    }
}
