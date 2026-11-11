// Read-only inspection of the approved offline Manual Valves alarm counter.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.HmiUnified.UI.Dynamization.Script;
using Siemens.Engineering.HmiUnified.UI.Shapes;
internal static class InspectManualAlarm {
 const string ApprovedProject=@"C:\www\Schlenker\runs\2026-11-10-tia-recovery-smc\recovered-work-copy\Backup Schlenkers 36-10 190036-7-8v2.12 10-11.ap19";
 static int Main(string[] args) {
  try {
   if(args.Length!=2)throw new ArgumentException("Usage: InspectManualAlarm <exact-ap19> <report>");
   string path=Path.GetFullPath(args[0]);
   Require(String.Equals(path,ApprovedProject,StringComparison.OrdinalIgnoreCase),"Exact approved project required.");
   var process=TiaPortal.GetProcesses().Single(p=>p.ProjectPath!=null && String.Equals(Path.GetFullPath(p.ProjectPath.FullName),path,StringComparison.OrdinalIgnoreCase));
   using(var report=new StreamWriter(args[1],false))using(var portal=process.Attach()) {
    var project=portal.Projects.Single();Require(String.Equals(Path.GetFullPath(project.Path.FullName),path,StringComparison.OrdinalIgnoreCase),"Project changed.");
    var candidates=project.Devices.SelectMany(d=>Items(d.DeviceItems)).Select(i=>i.GetService<SoftwareContainer>()).Where(c=>c!=null && c.Software is HmiSoftware).ToArray();
    Require(candidates.Length==1,"Exactly one Unified HMI required.");
    var hmi=(HmiSoftware)candidates[0].Software;var screen=hmi.Screens.Find("manual_valves");Require(screen!=null,"Screen missing.");
    var alarm=screen.ScreenItems.Find("REV13_Common_AlarmText") as HmiText;Require(alarm!=null,"Alarm counter missing.");
    report.WriteLine("READ_ONLY; Project="+path+"; Timestamp="+DateTimeOffset.Now.ToString("o"));
    report.WriteLine("OBJECT="+alarm.Name+"; Visible="+alarm.Visible+"; Geometry="+alarm.Left+","+alarm.Top+","+alarm.Width+","+alarm.Height);
    foreach(var text in alarm.Text.Items)report.WriteLine("STATIC_TEXT="+text.Text);
    foreach(var d in alarm.Dynamizations) {
     report.WriteLine("DYNAMIC="+d.PropertyName+"; Type="+d.GetType().FullName);
     var s=d as ScriptDynamization;if(s!=null)report.WriteLine("TRIGGER="+s.Trigger.Type+"; Async="+s.Async+"; SCRIPT="+s.ScriptCode);
    }
    int count=0;foreach(var table in hmi.TagTables)foreach(var tag in table.Tags)if(tag.Name=="Alarm_Word1"||tag.Name=="Alarm_Word2") {
     count++;report.WriteLine("TAG="+tag.Name+"; Table="+table.Name+"; Connection="+tag.Connection+"; PlcTag="+tag.PlcTag+"; DataType="+tag.DataType);
    }
    report.WriteLine("ALARM_WORD_TAGS="+count+"; No compile/save/download/output actions.");
   }
   return 0;
  }catch(Exception e){Console.Error.WriteLine(e);return 1;}
 }
 static IEnumerable<DeviceItem> Items(DeviceItemComposition items){foreach(var i in items){yield return i;foreach(var c in Items(i.DeviceItems))yield return c;}}
 static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
}