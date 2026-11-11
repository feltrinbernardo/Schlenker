// Narrow post-application HMI cleanup. No PLC, hardware, online or transfer APIs.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Siemens.Engineering;
using Siemens.Engineering.Compiler;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.HmiUnified.UI.Base;
using Siemens.Engineering.HmiUnified.UI.Enum;
using Siemens.Engineering.HmiUnified.UI.Shapes;
using Siemens.Engineering.HmiUnified.UI.Widgets;

namespace Schlenker.TiaV19
{
    internal static class ClearSmcLogicalPlaceholders
    {
        const string ApprovedProject=@"C:\www\Schlenker\runs\2026-11-10-tia-recovery-smc\recovered-work-copy\Backup Schlenkers 36-10 190036-7-8v2.12 10-11.ap19";
        static readonly string[] Notes={"REV50_ValveTest_MappingTbc_Note","REV50_ValveTest_MappingTbc_Note_1","REV50_ValveTest_Y10Config_Note"};
        static readonly string[] Commands={"REV50_ValveTest_EV010_Command","REV50_ValveTest_EV011_Command","REV50_ValveTest_EV210_Command","REV50_ValveTest_EV211_Command","REV50_ValveTest_EV212_Command","SMC_Logical_Y6_Command","SMC_Logical_Y7_Command","REV50_ValveTest_EV220_Command","REV50_ValveTest_EV001Open_Command","SMC_Logical_Y10_Command","REV50_ValveTest_BottleExternalWash_Command"};
        static int Main(string[] args)
        {
            if(args.Length!=3){Console.Error.WriteLine("Usage: ClearSmcLogicalPlaceholders <exact-ap19> <fresh-complete-backup-directory> <report>");return 2;}
            try
            {
                string path=Path.GetFullPath(args[0]);
                Require(String.Equals(path,ApprovedProject,StringComparison.OrdinalIgnoreCase),"Exact approved project required.");
                VerifyBackup(path,Path.GetFullPath(args[1]));
                var process=TiaPortal.GetProcesses().Single(p=>p.ProjectPath!=null && String.Equals(Path.GetFullPath(p.ProjectPath.FullName),path,StringComparison.OrdinalIgnoreCase));
                using(var report=new StreamWriter(args[2],false))
                using(var portal=process.Attach())
                {
                    var project=portal.Projects.Single();
                    Require(String.Equals(Path.GetFullPath(project.Path.FullName),path,StringComparison.OrdinalIgnoreCase),"Project changed.");
                    var candidates=project.Devices.SelectMany(d=>Items(d.DeviceItems).Select(i=>new{Device=d,Item=i,Container=i.GetService<SoftwareContainer>()})).Where(x=>x.Container!=null && x.Container.Software is HmiSoftware).ToArray();
                    Require(candidates.Length==1,"Exactly one Unified HMI required.");
                    var target=candidates[0]; var hmi=(HmiSoftware)target.Container.Software;
                    var screen=hmi.Screens.Find("manual_valves"); Require(screen!=null,"Screen missing.");
                    var items=Walk(screen,new HashSet<IEngineeringObject>()).OfType<HmiScreenItemBase>().ToDictionary(x=>x.Name,StringComparer.Ordinal);
                    var table=hmi.TagTables.Find("REV12"); Require(table!=null,"REV12 missing.");
                    var mappings=new Dictionary<string,string>();
                    for(int i=1;i<=11;i++)foreach(string f in new[]{"Request","Accepted","State","Channel","PhysicalVerified"})mappings.Add("SMC_Y"+i+"_"+f,"DB_SMC_LogicalTest."+f+"["+i+"]");
                    foreach(string f in new[]{"PageActive","Enable","Permit","HmiLinkOK","ProfinetDeviceOK","SafetyOK","ManualModeOK","ProcessInactive","AirPressureOK","DoorsOK","AlarmsOK","PhysicalOutputsEnabled"})mappings.Add("SMC_Logical_"+f,"DB_SMC_LogicalTest."+f);
                    foreach(var pair in mappings){var tag=table.Tags.Find(pair.Key);Require(tag!=null && tag.Connection=="HMI_Connection_1" && tag.PlcTag==pair.Value,"Tag mismatch: "+pair.Key);}
                    foreach(var type in new[]{HmiScreenEventType.Loaded,HmiScreenEventType.Unloaded}){var e=screen.EventHandlers.Find(type);Require(e!=null && e.Script.ScriptCode.Contains("// SMC_LOGICAL_INTERFACE_V1"),"Applied lifecycle marker missing.");}
                    for(int i=0;i<11;i++)
                    {
                        HmiScreenItemBase item; Require(items.TryGetValue(Commands[i],out item) && item is HmiButton,"Command missing: "+Commands[i]);
                        var b=(HmiButton)item;var down=b.EventHandlers.Find(HmiButtonEventType.Down);var up=b.EventHandlers.Find(HmiButtonEventType.Up);
                        Require(down!=null && up!=null && down.Script.ScriptCode=="HMIRuntime.Tags(\"SMC_Y"+(i+1)+"_Request\").Write(true);" && up.Script.ScriptCode=="HMIRuntime.Tags(\"SMC_Y"+(i+1)+"_Request\").Write(false);","Request mapping mismatch.");
                    }
                    var notes=Notes.Select(n=>{HmiScreenItemBase item;Require(items.TryGetValue(n,out item) && item is HmiText,"Note missing: "+n);return (HmiText)item;}).ToArray();
                    foreach(var note in notes)Require(!note.Visible && !note.Enabled,"Expected already hidden/disabled note: "+note.Name);
                    var compiler=target.Item.GetService<ICompilable>()??target.Device.GetService<ICompilable>();Require(compiler!=null,"HMI compiler missing.");
                    report.WriteLine("Preflight=PASS; Tags=67; Requests=11; LifecycleMarkers=2; Project="+path+"; Timestamp="+DateTimeOffset.Now.ToString("o"));
                    // Only these three existing objects are mutated; identities/geometry remain intact.
                    foreach(var note in notes)
                    {
                        foreach(MultilingualTextItem text in note.Text.Items)text.Text="<body><p></p></body>";
                        note.Visible=false; note.Enabled=false;
                        report.WriteLine("CLEARED_PLACEHOLDER="+note.Name);
                    }
                    var result=compiler.Compile();Messages(result.Messages,report);
                    report.WriteLine("Errors="+result.ErrorCount+"; Warnings="+result.WarningCount);
                    if(result.ErrorCount!=0){report.WriteLine("FAILED_UNSAVED; discard in-memory changes before retry.");return 1;}
                    project.Save();report.WriteLine("SAVED; visual verification required.");
                }
                return 0;
            }
            catch(Exception e){Console.Error.WriteLine(e);return 1;}
        }
        static IEnumerable<DeviceItem> Items(DeviceItemComposition items){foreach(var i in items){yield return i;foreach(var c in Items(i.DeviceItems))yield return c;}}
        static IEnumerable<IEngineeringObject> Walk(IEngineeringObject obj,HashSet<IEngineeringObject> seen)
        {
            if(!seen.Add(obj))yield break;yield return obj;
            foreach(var info in obj.GetCompositionInfos())
            {
                object c=obj.GetComposition(info.Name);var single=c as IEngineeringObject;
                if(single!=null){foreach(var n in Walk(single,seen))yield return n;}
                else{var list=c as IEnumerable;if(list==null)continue;foreach(object value in list){var item=value as IEngineeringObject;if(item!=null)foreach(var n in Walk(item,seen))yield return n;}}
            }
        }
        static void VerifyBackup(string project,string backup)
        {
            string source=Path.GetDirectoryName(project);
            Require(!String.Equals(source,backup,StringComparison.OrdinalIgnoreCase) && !backup.StartsWith(source+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase),"Backup must be outside project.");
            foreach(string file in Directory.GetFiles(source,"*",SearchOption.AllDirectories))
            {
                string ext=Path.GetExtension(file);if(ext.Equals(".lck",StringComparison.OrdinalIgnoreCase)||ext.Equals(".lock",StringComparison.OrdinalIgnoreCase))continue;
                string relative=file.Substring(source.Length+1),dest=Path.Combine(backup,relative);
                Require(File.Exists(dest)&&Hash(file)==Hash(dest),"Incomplete/stale backup: "+relative);
            }
        }
        static string Hash(string file){using(var sha=SHA256.Create())using(var stream=new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))return BitConverter.ToString(sha.ComputeHash(stream));}
        static void Messages(IEnumerable<CompilerResultMessage> messages,TextWriter report){foreach(var m in messages){report.WriteLine(m.State+"\t"+m.Path+"\t"+m.Description);Messages(m.Messages,report);}}
        static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    }
}
