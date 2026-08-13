[CmdletBinding()]
param(
    [switch]$Live
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$pythonCommand = Get-Command python -ErrorAction Stop

Push-Location $repositoryRoot
try {
    & $pythonCommand.Source -m unittest discover -s judge -p 'test_*.py' -v
    if ($LASTEXITCODE -ne 0) {
        throw 'Offline prompt-judge tests failed.'
    }

    if ($Live) {
        if ([string]::IsNullOrWhiteSpace($env:OPENAI_API_KEY)) {
            throw 'OPENAI_API_KEY is not available in this process.'
        }
        $payload = @{
            prompt = (
                'Review REV12/PLC_Sources/10A_FB_DoorAccess.scl for clarity ' +
                'only; do not modify files or communicate with a PLC.'
            )
        } | ConvertTo-Json -Compress
        $payload | & $pythonCommand.Source .\judge\judge.py
        if ($LASTEXITCODE -ne 0) {
            throw 'Live prompt-judge API test failed.'
        }
    }

    [PSCustomObject]@{
        Status = 'PASS'
        OfflineTests = 'passed'
        LiveApiTest = if ($Live) { 'passed' } else { 'not requested' }
    }
}
finally {
    Pop-Location
}
