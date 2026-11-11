// Narrow command-label and Y6/Y7 border correction. No PLC, hardware, online or transfer APIs.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using Siemens.Engineering;
using Siemens.Engineering.Compiler;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.HmiUnified.UI.Base;
using Siemens.Engineering.HmiUnified.UI.Dynamization.Script;
using Siemens.Engineering.HmiUnified.UI.Enum;
using Siemens.Engineering.HmiUnified.UI.Shapes;
using Siemens.Engineering.HmiUnified.UI.Widgets;

namespace Schlenker.TiaV19
{
    internal static class RefineSmcReadability
    {
        const string ApprovedProject=@"C:\www\Schlenker\runs\2026-11-10-tia-recovery-smc\recovered-work-copy\Backup Schlenkers 36-10 190036-7-8v2.12 10-11.ap19";
        static readonly string[] Labels={"Infeed Vacuum","Vacuum","Drainage","Return Dummy Bottle CIP","Bypass","Infeed Product","Spray CIP","Cap Distributor Air","Infeed Gate Open","Cap Opening","External Washing"};
        static readonly string[] Commands={"REV50_ValveTest_EV010_Command","REV50_ValveTest_EV011_Command","REV50_ValveTest_EV210_Command","REV50_ValveTest_EV211_Command","REV50_ValveTest_EV212_Command","SMC_Logical_Y6_Command","SMC_Logical_Y7_Command","REV50_ValveTest_EV220_Command","REV50_ValveTest_EV001Open_Command","SMC_Logical_Y10_Command","REV50_ValveTest_BottleExternalWash_Command"};
        static int Main(string[] args)
        {
            if(args.Length!=3 && !(args.Length==4 && args[3]=="--with-state-backs")){Console.Error.WriteLine("Usage: RefineSmcReadability <exact-ap19> <fresh-complete-backup-directory> <report> [--with-state-backs]");return 2;}
            try
            {
                string path=Path.GetFullPath(args[0]);
                Require(String.Equals(path,ApprovedProject,StringComparison.OrdinalIgnoreCase),"Exact approved project required.");
                var process=TiaPortal.GetProcesses().Single(p=>p.ProjectPath!=null && String.Equals(Path.GetFullPath(p.ProjectPath.FullName),path,StringComparison.OrdinalIgnoreCase));
                using(var report=new StreamWriter(args[2],false))
                {
                    report.AutoFlush=true;
                    return RunAttached(process,path,()=>VerifyBackup(path,Path.GetFullPath(args[1])),report,args.Length==4);
                }
            }
            catch(Exception e){Console.Error.WriteLine(e);return 1;}
        }

        static void Phase(TextWriter report,string value)
        {
            string line="PHASE="+value+"|UTC="+DateTime.UtcNow.ToString("o");
            report.WriteLine(line);report.Flush();Console.WriteLine(line);Console.Out.Flush();
        }

        // Only this method owns attach/compile/save for the optional two-stage batch.
        internal static int RunAttached(TiaPortalProcess process,string path,Action verifyBackup,TextWriter report,bool withStateBacks)
        {
            bool mutationStarted=false;
            try
            {
                Phase(report,"ATTACH_STARTED");
                using(var portal=process.Attach())
                {
                    Phase(report,"ATTACH_COMPLETED");
                    Phase(report,"EXCLUSIVE_ACCESS_STARTED");
                    using(var access=portal.ExclusiveAccess("Schlenker SMC readability"))
                    {
                        Phase(report,"EXCLUSIVE_ACCESS_ACQUIRED");
                        var project=portal.Projects.Single();
                        Require(String.Equals(Path.GetFullPath(project.Path.FullName),path,StringComparison.OrdinalIgnoreCase),"Project changed.");
                        Require(!access.IsCancellationRequested,"Exclusive access cancelled.");
                        Phase(report,"BACKUP_CHECK");verifyBackup();
                        Phase(report,"READABILITY_STARTED");mutationStarted=true;
                        var compiler=ApplyTo(project,report);
                        Phase(report,"READABILITY_COMPLETED");
                        Require(!access.IsCancellationRequested,"Exclusive access cancelled.");
                        if(withStateBacks)
                        {
                            Phase(report,"STATE_BACKS_STARTED");
                            // Re-enumerates the objects created by readability and repeats binding guards.
                            compiler=MatchSmcStateBacks.ApplyTo(project,report);
                            Phase(report,"STATE_BACKS_COMPLETED");
                        }
                        Require(!access.IsCancellationRequested,"Exclusive access cancelled.");
                        Phase(report,"COMPILE_STARTED");
                        var result=compiler.Compile();Messages(result.Messages,report);
                        report.WriteLine("Errors="+result.ErrorCount+"; Warnings="+result.WarningCount);
                        Phase(report,"COMPILE_COMPLETED");
                        if(result.ErrorCount!=0)
                        {
                            report.WriteLine("FAILED_UNSAVED; partial in-memory changes may remain; reconcile before retry.");report.Flush();return 1;
                        }
                        Require(!access.IsCancellationRequested,"Exclusive access cancelled.");
                        Phase(report,"SAVE_STARTED");project.Save();Phase(report,"SAVE_COMPLETED");
                        report.WriteLine("SAVED; visual verification required.");report.Flush();
                    }
                }
                return 0;
            }
            catch(Exception e)
            {
                report.WriteLine(mutationStarted
                    ? "FAILED_STATE_UNCERTAIN; partial in-memory changes may remain; no automatic retry or rollback."
                    : "FAILED_BEFORE_MUTATION; no stage was applied.");
                report.WriteLine(e);report.Flush();Console.Error.WriteLine(e);return 1;
            }
        }
        // Mutation-only stage: caller owns the session, compilation and save.
        internal static ICompilable ApplyTo(Project project, TextWriter report)
        {
            string path=project.Path.FullName;
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
            string[] suffix={"EV010","EV011","EV210","EV211","EV212","Y6","Y7","EV220","EV001Open","FillerExternalWash","BottleExternalWash"};
            var buttons=Commands.Select(n=>Required<HmiButton>(items,n)).ToArray();
            var states=Enumerable.Range(0,11).Select(i=>Required<HmiText>(items,(i==5||i==6||i==9)?"SMC_Logical_Y"+(i+1)+"_State":"REV41_MANUAL_ValveOutput"+suffix[i]+"_Text")).ToArray();
            var referenceBack=Required<HmiRectangle>(items,"REV41_MANUAL_ValveOutputEV010_Back");
            var notice=Required<HmiText>(items,"REV50_ValveTest_Instruction");
            foreach(var button in buttons)Require(button.Width==205 && button.Height==42,"Unexpected command dimensions: "+button.Name);
            for(int i=0;i<11;i++)
            {
                Require(states[i].Dynamizations.Find("Text") is ScriptDynamization,"State diagnostic missing.");
                Require(states[i].Dynamizations.Find("ForeColor")==null,"State foreground dynamic unexpected.");
                Require(buttons[i].Dynamizations.Find("Enabled") is ScriptDynamization && !buttons[i].Enabled,"Permit guard missing.");
            }
            foreach(int i in new[]{5,6,9})
            {
                Require(!items.ContainsKey("SMC_Logical_Y"+(i+1)+"_Back"),"Background already exists.");
                Require(states[i].EventHandlers.Count==0 && states[i].Dynamizations.Count==1,"Replacement state has additional bindings.");
            }
            var compiler=target.Item.GetService<ICompilable>()??target.Device.GetService<ICompilable>();Require(compiler!=null,"HMI compiler missing.");
            report.WriteLine("Preflight=PASS; Tags=67; Requests=11; LifecycleMarkers=2; Project="+path+"; Timestamp="+DateTimeOffset.Now.ToString("o"));
            for(int i=0;i<11;i++)
            {
                foreach(MultilingualTextItem text in buttons[i].Text.Items)text.Text="<body><p>TIENI PREMUTO</p></body>";
                buttons[i].Font.Size=14;
                states[i].Font.Size=10; states[i].ForeColor=Color.White;
                states[i].HorizontalTextAlignment=HmiHorizontalAlignment.Center; states[i].VerticalTextAlignment=HmiVerticalAlignment.Center;
                report.WriteLine("READABILITY=125Y"+(i+1)+";ButtonFont=14;StateFont=10;BindingsPreserved=true");
            }
            foreach(int i in new[]{5,6,9})
            {
                var state=states[i]; var back=screen.ScreenItems.Create<HmiRectangle>("SMC_Logical_Y"+(i+1)+"_Back");
                back.Left=state.Left; back.Top=state.Top; back.Width=state.Width; back.Height=state.Height;
                back.BackColor=Color.FromArgb(92,105,118); back.BackFillPattern=HmiFillPattern.Solid;
                back.BorderColor=referenceBack.BorderColor; back.BorderWidth=referenceBack.BorderWidth;
                back.Corners.TopLeftRadius=referenceBack.Corners.TopLeftRadius; back.Corners.TopRightRadius=referenceBack.Corners.TopRightRadius;
                back.Corners.BottomLeftRadius=referenceBack.Corners.BottomLeftRadius; back.Corners.BottomRightRadius=referenceBack.Corners.BottomRightRadius;
                // HmiText has no fill property or public z-order API. Recreate only the three
                // verified standalone logical-state texts after their new backgrounds.
                var prior=(ScriptDynamization)state.Dynamizations.Find("Text");
                string script=prior.ScriptCode; var trigger=prior.Trigger.Type; bool async=prior.Async;
                string name=state.Name; int left=state.Left,top=state.Top; uint width=state.Width,height=state.Height;
                bool visible=state.Visible,enabled=state.Enabled;
                state.Delete();
                var replacement=screen.ScreenItems.Create<HmiText>(name);
                replacement.Left=left;replacement.Top=top;replacement.Width=width;replacement.Height=height;
                replacement.Visible=visible;replacement.Enabled=enabled;replacement.Font.Size=10;replacement.ForeColor=Color.White;
                replacement.HorizontalTextAlignment=HmiHorizontalAlignment.Center;replacement.VerticalTextAlignment=HmiVerticalAlignment.Center;
                foreach(MultilingualTextItem text in replacement.Text.Items)text.Text="<body><p>NO DATA</p></body>";
                var dynamic=replacement.Dynamizations.Create<ScriptDynamization>("Text");
                dynamic.Async=async;dynamic.Trigger.Type=trigger;dynamic.ScriptCode=script;
                Require(dynamic.ScriptCode==script && dynamic.Trigger.Type==trigger && dynamic.Async==async,"State script restoration mismatch.");
                states[i]=replacement;
                report.WriteLine("CREATED_BACK="+back.Name+";RECREATED_TEXT="+name+";DIAGNOSTIC_SCRIPT_EXACT=true;VISUAL_LAYER_VERIFICATION_REQUIRED");
            }
            foreach(MultilingualTextItem text in notice.Text.Items)text.Text="<body><p>Test logico: nessuna uscita fisica. ACCEPTED = richiesta accettata, non feedback valvola.</p></body>";
            notice.Font.Size=12;
            return compiler;
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


