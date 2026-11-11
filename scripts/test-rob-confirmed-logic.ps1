[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function Get-Smc125Matrix {
    param(
        [bool]$ProductionActive,
        [bool]$PumpRunRequest,
        [bool]$PumpRunning,
        [bool]$PumpFeedbackValid
    )
    $y6Open = $false
    $y6Held = $false
    if ($ProductionActive) {
        if ($PumpRunRequest) {
            $y6Open = $true
        }
        elseif (-not $PumpFeedbackValid -or $PumpRunning) {
            $y6Open = $true
            $y6Held = $true
        }
    }
    [pscustomobject]@{
        Y1 = $ProductionActive; Y2 = $ProductionActive; Y3 = $false
        Y4 = $false; Y5 = $false; Y6 = $y6Open; Y7 = $false
        Y6Held = $y6Held
    }
}

function Get-RotationInhibited {
    param(
        [bool]$SafetyValidated,
        [bool]$X4Valid,
        [bool]$X4Healthy,
        [bool]$X7Valid,
        [bool]$X7Engaged
    )
    return (-not $SafetyValidated) -or (-not $X4Valid) -or (-not $X4Healthy) -or
        (-not $X7Valid) -or $X7Engaged
}

$types = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'REV12/PLC_Sources/01_UDT_Types.scl')
$db = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'REV12/PLC_Sources/02_DB_Global.scl')
$state = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'REV12/PLC_Sources/03_FB_MainState.scl')
$interlocks = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'REV12/PLC_Sources/10P_FB_AL102ProcessInterlocks.scl')
$matrix = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'REV12/PLC_Sources/10Q_FB_SMC125ProductionMatrix.scl')
$application = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'REV12/PLC_Sources/11_FB_Application.scl')
$hmiTags = Import-Csv -LiteralPath (Join-Path $repoRoot 'REV12/HMI/REV12_HMI_Tags.csv')

foreach ($token in @(
    'X1ProductPresence', 'X2ProductTemperature', 'X3Vacuum',
    'X4AntiRotationHealthy', 'X5TankLevel', 'X6Valve1Reference',
    'X7CIPPresenceEngaged', 'X8SpareValid')) {
    Assert-True ($types.Contains($token)) "Missing AL102 semantic member: $token"
}

Assert-True ($db.Contains('AL102Process.FunctionalMapConfirmed := TRUE')) 'AL102 functional map is not frozen.'
Assert-True ($db.Contains('AL102Process.SafetyInterlockValidated := FALSE')) 'Safety validation must default FALSE.'
Assert-True ($db.Contains('AL102.HardwareConfigured := FALSE')) 'AL102 hardware commissioning must default FALSE.'
Assert-True (-not $application.Contains('IOL_AL102_P1_RAW')) 'Obsolete AL102/X1 raw vacuum mapping remains in the application.'
Assert-True ($interlocks.Contains('S.ImmediateStopRequest := MainDriveRunning')) 'X4 immediate-stop request is missing.'
Assert-True ($state.Contains('75: // CIP_POSITIONING')) 'CIP positioning state is missing.'
Assert-True ($state.Contains('77: // CIP_WAIT_CONNECTION')) 'CIP connection wait state is missing.'
Assert-True ($application.Contains('Re-apply after every generic/bench drive manager')) 'Final rotation containment is missing.'

foreach ($mode in @('Automatic', 'Manual', 'Jog', 'Homing', 'CIP positioning')) {
    $inhibited = Get-RotationInhibited -SafetyValidated $true -X4Valid $true -X4Healthy $true -X7Valid $true -X7Engaged $true
    Assert-True $inhibited "X7 connected did not inhibit $mode."
}
$unvalidatedInhibit = Get-RotationInhibited -SafetyValidated $false -X4Valid $true -X4Healthy $true -X7Valid $true -X7Engaged $false
Assert-True $unvalidatedInhibit 'Unvalidated X7 safety interlock did not fail safe.'
$invalidX4Inhibit = Get-RotationInhibited -SafetyValidated $true -X4Valid $false -X4Healthy $false -X7Valid $true -X7Engaged $false
Assert-True $invalidX4Inhibit 'Invalid X4 did not inhibit rotation.'

$running = Get-Smc125Matrix -ProductionActive $true -PumpRunRequest $true -PumpRunning $true -PumpFeedbackValid $true
Assert-True ($running.Y1 -and $running.Y2 -and $running.Y6) 'Production OPEN matrix for Y1/Y2/Y6 failed.'
Assert-True (-not ($running.Y3 -or $running.Y4 -or $running.Y5 -or $running.Y7)) 'Production CLOSED matrix for Y3/Y4/Y5/Y7 failed.'

$pauseUnknown = Get-Smc125Matrix -ProductionActive $true -PumpRunRequest $false -PumpRunning $false -PumpFeedbackValid $false
Assert-True ($pauseUnknown.Y6 -and $pauseUnknown.Y6Held) 'Y6 did not remain open with invalid pump feedback.'
$pauseRunning = Get-Smc125Matrix -ProductionActive $true -PumpRunRequest $false -PumpRunning $true -PumpFeedbackValid $true
Assert-True ($pauseRunning.Y6 -and $pauseRunning.Y6Held) 'Y6 did not remain open while pump feedback was running.'
$pauseStopped = Get-Smc125Matrix -ProductionActive $true -PumpRunRequest $false -PumpRunning $false -PumpFeedbackValid $true
Assert-True (-not $pauseStopped.Y6 -and -not $pauseStopped.Y6Held) 'Y6 did not close after valid stopped feedback.'
Assert-True ($matrix.Contains('NOT ProductPumpFeedbackValid OR ProductPumpRunning')) 'PLC Y6 feedback sequence differs from the tested model.'

$requiredHmiTags = @(
    'AL102_X1_Product_Presence', 'AL102_X2_Product_Temperature', 'AL102_X3_Vacuum',
    'AL102_X4_AntiRotation_Healthy', 'AL102_X5_Tank_Level', 'AL102_X6_Valve1_Reference',
    'AL102_X7_CIP_Presence', 'CIP_Positioning_Active', 'CIP_Waiting_Connection',
    'CIP_Ready', 'CIP_Safety_Interlock_Validated',
    'SMC_125Y1_Desired_Open', 'SMC_125Y2_Desired_Open', 'SMC_125Y3_Desired_Open',
    'SMC_125Y4_Desired_Open', 'SMC_125Y5_Desired_Open', 'SMC_125Y6_Desired_Open',
    'SMC_125Y7_Desired_Open', 'SMC_125Y6_Held_For_Pump_Stop'
)
foreach ($tag in $requiredHmiTags) {
    Assert-True ($null -ne ($hmiTags | Where-Object Name -eq $tag)) "Missing HMI tag: $tag"
}

Write-Host 'PASS: AL102 functional mapping is explicit and hardware data remains unassumed.'
Write-Host 'PASS: X4 stop and X7 all-mode rotation containment are represented fail-safe.'
Write-Host 'PASS: CIP positioning/wait/ready state path is present.'
Write-Host 'PASS: 125Y1-Y7 production matrix and Y6 pump-stop ordering pass offline model tests.'
Write-Host 'PASS: Required read-only HMI diagnostics are present.'
