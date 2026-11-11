# Regression test for the exact C# predicate; does not load Siemens libraries or TIA.
$ErrorActionPreference = 'Stop'
$source = Get-Content (Join-Path $PSScriptRoot '../tools/tia-v19-openness/BuildManualValveTestOffline.cs') -Raw
$method = [regex]::Match($source, '(?s)internal static bool ActivatesTag\(.*?\r?\n        \}').Value
if (!$method) { throw 'Audit predicate not found' }
if (-not ('ManualValveAuditRegression' -as [type])) {
    Add-Type -TypeDefinition ('using System; using System.Text.RegularExpressions; public class ManualValveAuditRegression { ' + $method.Replace('internal static','public static') + ' }')
}
$tag = 'SMC_EV001_Close_Manual'
$cases = @(
    @('HMIRuntime.Tags("SMC_EV001_Close_Manual").Write(false);HMIRuntime.Tags("Cmd_ManualSelect").Write(true);', $false),
    @('HMIRuntime.Tags("SMC_EV001_Close_Manual").Write(true);', $true),
    @("HMIRuntime.Tags('SMC_EV001_Close_Manual').Write(1);", $true),
    @('HMIRuntime . Tags ( "SMC_EV001_Close_Manual" ) . Write ( true );', $true),
    @('HMIRuntime.Tags("SMC_EV001_Close_Manual").Write(false);', $false),
    @('HMIRuntime.Tags("SMC_EV001_Open_Manual").Write(true);', $false)
)
foreach ($case in $cases) {
    if ([ManualValveAuditRegression]::ActivatesTag($case[0], $tag) -ne $case[1]) {
        throw 'Manual valve audit regression failed'
    }
}
'PASS: 6 manual-valve audit regression cases'
