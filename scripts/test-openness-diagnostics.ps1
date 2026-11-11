# Tests the actual diagnostic methods against fake Siemens types; no Siemens DLL is loaded.
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
foreach ($name in @('BuildUnifiedHomeScreen', 'Rev25FrameAudit', 'AuditCommonRenderTarget')) {
    $source = Get-Content (Join-Path $root "tools/tia-v19-openness/$name.cs") -Raw
    $start = $source.IndexOf('        private static string operationPhase')
    $end = $source.IndexOf('        private static int Main(', $start)
    if ($start -lt 0 -or $end -lt 0) { throw "Diagnostic methods missing: $name" }
    $methods = $source.Substring($start, $end - $start)
    $saveTest = if ($name -eq 'BuildUnifiedHomeScreen') { @'
        try { SaveDiagnosed(new Project()); throw new Exception("save should fail"); }
        catch (TimeoutException e) { ReportFailure(e); }
        Require(output.ToString().Contains("PHASE=SAVE_STARTED"), "save start missing");
        Require(!output.ToString().Contains("PHASE=SAVE_COMPLETED"), "false save completion");
        Require(errors.ToString().Contains("FAILURE_CODE=TIMEOUT|PHASE=SAVE_STARTED"), "save timeout context lost");
'@ } else { '' }
    Add-Type -TypeDefinition @"
using System;
using System.IO;
namespace Test$name {
class EngineeringSecurityException : Exception { }
class TiaPortal { }
class TiaPortalProcess {
 public bool Fail;
 public TiaPortal Attach() { if(Fail) throw new EngineeringSecurityException(); return new TiaPortal(); }
}
class Project { public void Save() { throw new TimeoutException(); } }
public class Harness {
$methods
static void Require(bool condition,string message) { if(!condition) throw new Exception(message); }
public static void Run() {
 var output=new StringWriter(); var errors=new StringWriter();
 var originalOut=Console.Out; var originalError=Console.Error;
 try {
  Console.SetOut(output); Console.SetError(errors);
  try { AttachDiagnosed(new TiaPortalProcess { Fail=true }); throw new Exception("attach should fail"); }
  catch(EngineeringSecurityException e) { ReportFailure(new Exception("wrapped",e)); }
  Require(output.ToString().Contains("PHASE=ATTACH_STARTED"),"missing attach start");
  Require(!output.ToString().Contains("ATTACH_COMPLETED"),"false attach completion");
  Require(errors.ToString().Contains("FAILURE_CODE=OPENNESS_SECURITY|PHASE=ATTACH_STARTED"),"security classification lost");
  output.GetStringBuilder().Clear(); errors.GetStringBuilder().Clear();
  AttachDiagnosed(new TiaPortalProcess());
  Require(output.ToString().Contains("ATTACH_COMPLETED"),"missing successful attach");
  output.GetStringBuilder().Clear(); errors.GetStringBuilder().Clear();
$saveTest
 } finally { Console.SetOut(originalOut); Console.SetError(originalError); }
}
}
}
"@
    $type = "Test$name.Harness" -as [type]
    $type::Run()
    Write-Output "PASS=$name diagnostics (fake API only)"
}
