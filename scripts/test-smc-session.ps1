# Executes the real session orchestration with fake attach/stages/compiler/save.
# No Siemens library or native project is opened.
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$source = Get-Content (Join-Path $root 'tools/tia-v19-openness/RefineSmcReadability.cs') -Raw
$start = $source.IndexOf('        static void Phase(')
$end = $source.IndexOf('        // Mutation-only stage:', $start)
if ($start -lt 0 -or $end -lt 0) { throw 'Session orchestration missing.' }
$methods = $source.Substring($start, $end-$start)
Add-Type -TypeDefinition @"
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
namespace SmcSessionTest {
static class State {
 public static string Case; public static int Attach,PortalDispose,ExclusiveDispose,First,Second,Compile,Save,Backup;
 public static bool Grouped;
 public static List<string> Events=new List<string>();
 public static void Reset(string c,bool grouped) { Case=c;Grouped=grouped;Attach=PortalDispose=ExclusiveDispose=First=Second=Compile=Save=Backup=0;Events.Clear(); }
}
class Result { public int ErrorCount; public int WarningCount=0; public object Messages=null; }
class Compiler {
 public Result Compile() { State.Compile++; State.Events.Add("compile"); if(State.Case=="compile-throw")throw new Exception("compiler unavailable"); return new Result { ErrorCount=State.Case=="compile-errors"?1:0 }; }
}
class Project {
 public FileInfo Path = new FileInfo(System.IO.Path.GetFullPath(State.Case=="identity"?"wrong.fixture":"expected.fixture"));
 public void Save() { State.Save++;State.Events.Add("save");if(State.Case=="save")throw new IOException("uncertain save"); }
}
class Exclusive : IDisposable {
 public bool IsCancellationRequested { get { return State.Case=="cancel-before" || (State.Case=="cancel-first" && State.First>0) || (State.Case=="cancel-second" && State.Second>0) || (State.Case=="cancel-compile" && State.Compile>0); } }
 public void Dispose() { State.ExclusiveDispose++; }
}
class Portal : IDisposable {
 public Project[] Projects=new[]{new Project()};
 public Exclusive ExclusiveAccess(string description) { if(State.Case=="exclusive")throw new Exception("busy");return new Exclusive(); }
 public void Dispose() { State.PortalDispose++; }
}
class TiaPortalProcess {
 public Portal Attach() { State.Attach++; if(State.Case=="attach")throw new Exception("attach denied");return new Portal(); }
}
static class MatchSmcStateBacks {
 public static Compiler ApplyTo(Project p,TextWriter report) {
  State.Second++;State.Events.Add("match");if(State.First!=1)throw new Exception("backgrounds unavailable");
  if(State.Case=="second")throw new Exception("second stage failed");return new Compiler();
 }
}
public class Harness {
$methods
 static void Require(bool condition,string message) { if(!condition)throw new Exception(message); }
 static void Messages(object messages,TextWriter report) { }
 static Compiler ApplyTo(Project p,TextWriter report) { State.First++;State.Events.Add("refine");if(State.Case=="first")throw new Exception("first stage failed");return new Compiler(); }
 static void Assert(bool value,string message) { if(!value)throw new Exception(State.Case+": "+message); }
 public static void Run(string c,bool grouped,int first,int second,int compile,int save) {
  State.Reset(c,grouped);var report=new StringWriter();var outOld=Console.Out;var errOld=Console.Error;int result;
  try {
   Console.SetOut(new StringWriter());Console.SetError(new StringWriter());
   result=RunAttached(new TiaPortalProcess(),Path.GetFullPath("expected.fixture"),()=>{State.Backup++;if(c=="backup")throw new Exception("backup mismatch");},report,grouped);
  } finally { Console.SetOut(outOld);Console.SetError(errOld); }
  Assert(result==(c=="success"?0:1),"exit code");
  Assert(State.Attach==1,"attach repeated");Assert(State.First==first && State.Second==second,"stage sequence");
  Assert(State.Compile==compile && State.Save==save,"compile/save count");
  Assert(State.Backup<2,"backup repeated");
  Assert(State.PortalDispose==(c=="attach"?0:1),"portal leaked");
  Assert(State.ExclusiveDispose==((c=="attach"||c=="exclusive")?0:1),"exclusive scope leaked");
  Assert(report.ToString().Contains("SAVE_COMPLETED")== (c=="success"),"false save completion");
  if(c=="success") {
   Assert(String.Join(",",State.Events)==(grouped?"refine,match,compile,save":"refine,compile,save"),"ordering");
   Assert(State.Backup==1,"backup not checked");
  }
 }
}
}
"@
# scenario, grouped, expected stage1, stage2, compile, save calls
$cases = @(
    @('success',$true,1,1,1,1), @('success',$false,1,0,1,1),
    @('attach',$true,0,0,0,0), @('exclusive',$true,0,0,0,0),
    @('identity',$true,0,0,0,0), @('backup',$true,0,0,0,0),
    @('first',$true,1,0,0,0), @('second',$true,1,1,0,0),
    @('compile-errors',$true,1,1,1,0), @('compile-throw',$true,1,1,1,0),
    @('save',$true,1,1,1,1), @('cancel-before',$true,0,0,0,0),
    @('cancel-first',$true,1,0,0,0), @('cancel-second',$true,1,1,0,0),
    @('cancel-compile',$true,1,1,1,0)
)
foreach ($c in $cases) {
    [SmcSessionTest.Harness]::Run($c[0],$c[1],$c[2],$c[3],$c[4],$c[5])
    Write-Output "PASS=$($c[0]);grouped=$($c[1])"
}
