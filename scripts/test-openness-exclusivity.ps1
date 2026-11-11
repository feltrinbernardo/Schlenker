# Validate the actual nested scope declarations with fake API types only.
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
foreach ($name in @('BuildUnifiedHomeScreen','Rev25FrameAudit','AuditCommonRenderTarget','VerifyNavigationMaster')) {
    $source = Get-Content (Join-Path $root "tools/tia-v19-openness/$name.cs") -Raw
    $match = [regex]::Match($source, 'using \(TiaPortal portal = ([^\r\n]+)\)\s*using \(ExclusiveAccess access = portal.ExclusiveAccess\("[^"]+"\)\)')
    if (!$match.Success) { throw "Missing protected attachment: $name" }
    if (!$source.Contains('Project changed after attach; no operation permitted.')) { throw "Missing identity guard: $name" }
    $scopes = $match.Value
    Add-Type -TypeDefinition @"
using System;
namespace LockTest$name {
class ExclusiveAccess : IDisposable {
 public static bool Busy; public bool IsCancellationRequested { get { return false; } }
 public ExclusiveAccess() { if(Busy) throw new InvalidOperationException("busy"); Busy=true; }
 public void Dispose() { Busy=false; }
}
class TiaPortal : IDisposable {
 public static int Connections;
 public TiaPortal() { Connections++; }
 public ExclusiveAccess ExclusiveAccess(string name) { return new ExclusiveAccess(); }
 public void Dispose() { Connections--; }
}
class TiaPortalProcess { public TiaPortal Attach() { return new TiaPortal(); } }
public class Harness {
 static int Work;
 static TiaPortal AttachDiagnosed(TiaPortalProcess p) { return p.Attach(); }
 static void Execute(bool fail) {
  var selected=new TiaPortalProcess(); var matches=new[]{selected};
  $scopes
  { Work++; if(fail) throw new Exception("simulated work failure"); }
 }
 public static void Run() {
  ExclusiveAccess.Busy=true;
  try { Execute(false); throw new Exception("contender was admitted"); }
  catch(InvalidOperationException) { }
  if(Work!=0 || TiaPortal.Connections!=0) throw new Exception("work before exclusive acquisition or leaked attach");
  ExclusiveAccess.Busy=false;
  try { Execute(true); } catch(Exception e) { if(e.Message!="simulated work failure") throw; }
  if(ExclusiveAccess.Busy || TiaPortal.Connections!=0) throw new Exception("scope leaked on exception");
  Execute(false);
  if(Work!=2 || ExclusiveAccess.Busy || TiaPortal.Connections!=0) throw new Exception("scope leaked on success");
 }
}
}
"@
    $type = "LockTest$name.Harness" -as [type]
    $type::Run()
    Write-Output "PASS=$name exclusive scope (fake API only)"
}
