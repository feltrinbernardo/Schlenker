// Narrow Y6/Y7 static border/title-color normalization. No PLC, hardware, online or transfer APIs.
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
    internal static class NormalizeSmcCardStyle
    {
        const string ApprovedProject=@"C:\www\Schlenker\runs\2026-11-10-tia-recovery-smc\recovered-work-copy\Backup Schlenkers 36-10 190036-7-8v2.12 10-11.ap19";
        // V19 HmiRectangle exposes BorderColor/BorderWidth, no BorderStyle. Do not copy StyleItemClass: it could alter backgrounds.
        static readonly string[] Commands={"REV50_ValveTest_EV010_Command","REV50_ValveTest_EV011_Command","REV50_ValveTest_EV210_Command","REV50_ValveTest_EV211_Command","REV50_ValveTest_EV212_Command","SMC_Logical_Y6_Command","SMC_Logical_Y7_Command","REV50_ValveTest_EV220_Command","REV50_ValveTest_EV001Open_Command","SMC_Logical_Y10_Command","REV50_ValveTest_BottleExternalWash_Command"};
        static int Main(string[] args)
        {
            if(args.Length!=3){Console.Error.WriteLine("Usage: NormalizeSmcCardStyle <exact-ap19> <fresh-complete-backup-directory> <report>");return 2;}
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
                    var referenceCard=Required<HmiRectangle>(items,"REV50_ValveTest_EV212_Card");
                    var referenceTitle=Required<HmiText>(items,"REV50_ValveTest_EV212_Title");
                    var cards=new[]{Required<HmiRectangle>(items,"REV50_ValveTest_MappingTbc_Card_1"),Required<HmiRectangle>(items,"REV50_ValveTest_MappingTbc_Card")};
                    var titles=new[]{Required<HmiText>(items,"REV50_ValveTest_MappingTbc_Title_1"),Required<HmiText>(items,"REV50_ValveTest_MappingTbc_Title")};
                    Require(HasText(referenceTitle,"125Y5 - Bypass"),"Reference title mismatch.");
                    Require(HasText(titles[0],"125Y6 - Infeed Product") && HasText(titles[1],"125Y7 - Spray CIP"),"Target title identities mismatch.");
                    foreach(var card in cards.Concat(new[]{referenceCard}))Require(card.Dynamizations.Find("BorderColor")==null && card.Dynamizations.Find("BorderWidth")==null,"Border has a dynamization; no alteration permitted.");
                    foreach(var title in titles.Concat(new[]{referenceTitle}))Require(title.Dynamizations.Find("ForeColor")==null,"Title color has a dynamization; no alteration permitted.");
                    var compiler=target.Item.GetService<ICompilable>()??target.Device.GetService<ICompilable>();Require(compiler!=null,"HMI compiler missing.");
                    report.WriteLine("Preflight=PASS; Tags=67; Requests=11; LifecycleMarkers=2; Project="+path+"; Timestamp="+DateTimeOffset.Now.ToString("o"));
                    // Only two static border properties and title foreground colors change.
                    for(int i=0;i<cards.Length;i++)
                    {
                        cards[i].BorderColor=referenceCard.BorderColor;
                        cards[i].BorderWidth=referenceCard.BorderWidth;
                        titles[i].ForeColor=referenceTitle.ForeColor;
                        report.WriteLine("NORMALIZED="+cards[i].Name+";BorderColor="+cards[i].BorderColor+";BorderWidth="+cards[i].BorderWidth+";TitleForeColor="+titles[i].ForeColor);
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
        static T Required<T>(Dictionary<string,HmiScreenItemBase> items,string name) where T:HmiScreenItemBase
        {HmiScreenItemBase value;Require(items.TryGetValue(name,out value) && value is T,"Missing/wrong object: "+name);return (T)value;}
        static bool HasText(HmiText item,string expected)
        {return item.Text.Items.Any(t=>System.Text.RegularExpressions.Regex.Replace(t.Text,"<[^>]+>","").Trim().Replace(" | "," - ")==expected);}
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

