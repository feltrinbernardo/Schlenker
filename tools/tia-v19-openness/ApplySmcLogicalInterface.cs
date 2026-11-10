// Reviewable OFFLINE HMI-only helper. Execution requires a separate current-run
// exception to the repository GUI-only rule, operator development-route confirmation,
// and a fresh complete native-project backup. Compilation is not native validation.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using Siemens.Engineering;
using Siemens.Engineering.Compiler;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.HmiUnified.HmiTags;
using Siemens.Engineering.HmiUnified.UI.Base;
using Siemens.Engineering.HmiUnified.UI.Controls;
using Siemens.Engineering.HmiUnified.UI.Widgets;
using Siemens.Engineering.HmiUnified.UI.Dynamization.Script;
using Siemens.Engineering.HmiUnified.UI.Enum;
using Siemens.Engineering.HmiUnified.UI.Events;
using Siemens.Engineering.HmiUnified.UI.Screens;
using Siemens.Engineering.HmiUnified.UI.Shapes;

namespace Schlenker.TiaV19
{
    internal static class ApplySmcLogicalInterface
    {
        const string ApprovedProject = @"C:\www\Schlenker\runs\2026-11-10-tia-recovery-smc\recovered-work-copy\Backup Schlenkers 36-10 190036-7-8v2.12 10-11.ap19";
        const string Prefix = "REV50_ValveTest_";
        const string Marker = "// SMC_LOGICAL_INTERFACE_V1";
        static readonly string[] Suffix = { "EV010", "EV011", "EV210", "EV211", "EV212", "Y6", "Y7", "EV220", "EV001Open", "FillerExternalWash", "BottleExternalWash" };
        static readonly string[] Titles = { "Infeed Vacuum", "Vacuum", "Drainage", "Return Dummy Bottle CIP", "Bypass", "Infeed Product", "Spray CIP", "Cap Distributor Air", "Infeed Gate Open", "Cap Opening", "External Washing" };
        public sealed class Binding
        {
            public string screenLoadedBody { get; set; }
            public string screenUnloadedBody { get; set; }
            public string enableOnBody { get; set; }
            public string enableOffBody { get; set; }
            public string enableOnEnabled { get; set; }
            public string requestEnabled { get; set; }
            public string notice { get; set; }
            public Valve[] valves { get; set; }
        }
        public sealed class Valve
        {
            public string valve { get; set; }
            public string function { get; set; }
            public string existingCommand { get; set; }
            public string requestTag { get; set; }
            public string downBody { get; set; }
            public string upBody { get; set; }
            public string text { get; set; }
            public string stateText { get; set; }
        }
        static int Main(string[] args)
        {
            if (args.Length != 5) { Console.Error.WriteLine("Usage: ApplySmcLogicalInterface <exact-ap19> <fresh-backup-directory> <tags-csv> <bindings-json> <report>"); return 2; }
            try
            {
                string path = Path.GetFullPath(args[0]);
                Require(String.Equals(path, ApprovedProject, StringComparison.OrdinalIgnoreCase), "Only the approved disposable project is permitted.");
                VerifyBackup(path, Path.GetFullPath(args[1]));
                Require(Hash(args[2]) == "402CF561917410C03C3C3BDB48310F105B1FFA76B26E05008153F1227A86B578" && Hash(args[3]) == "710266B9CB6C87BEFE7553CEA84802E4DCF77253B5DBA9CCE8FCE406F3E70C39", "Only the reviewed tag CSV and binding JSON bytes are permitted.");
                string[][] tags = File.ReadAllLines(args[2]).Skip(1).Where(l => !String.IsNullOrWhiteSpace(l)).Select(l => l.Split(',')).ToArray();
                ValidateTags(tags);
                Binding bindings = new JavaScriptSerializer().Deserialize<Binding>(File.ReadAllText(args[3]));
                ValidateBindings(bindings);
                // Never select by project title or process index. No headless fallback.
                TiaPortalProcess process = TiaPortal.GetProcesses().Single(p => p.ProjectPath != null && String.Equals(Path.GetFullPath(p.ProjectPath.FullName), path, StringComparison.OrdinalIgnoreCase));
                using (StreamWriter report = new StreamWriter(args[4], false))
                using (TiaPortal portal = process.Attach())
                {
                    Project project = portal.Projects.Single();
                    Require(String.Equals(Path.GetFullPath(project.Path.FullName), path, StringComparison.OrdinalIgnoreCase), "Project identity changed.");
                    var hmis = project.Devices.SelectMany(d => DeviceItems(d.DeviceItems).Select(i => new { Device = d, Item = i, Container = i.GetService<SoftwareContainer>() })).Where(x => x.Container != null && x.Container.Software is HmiSoftware).ToArray();
                    Require(hmis.Length == 1, "Expected exactly one Unified HMI.");
                    HmiSoftware hmi = (HmiSoftware)hmis[0].Container.Software;
                    HmiScreen screen = hmi.Screens.Find("manual_valves");
                    Require(screen != null, "manual_valves not found.");
                    Dictionary<string,HmiScreenItemBase> items = Walk(screen, new HashSet<IEngineeringObject>()).OfType<HmiScreenItemBase>().ToDictionary(i => i.Name, StringComparer.Ordinal);
                    AuditPreflight(screen, items);
                    HmiTagTable auditTable = hmi.TagTables.Find("REV12");
                    Console.WriteLine("AUDIT_REV12_TABLE_PRESENT="+(auditTable!=null));
                    foreach(string[] row in tags) Console.WriteLine("AUDIT_TAG="+row[0]+";PRESENT="+(auditTable!=null && auditTable.Tags.Find(row[0])!=null));
                    Preflight(screen, items, bindings);
                    HmiTagTable table = hmi.TagTables.Find("REV12");
                    Require(table != null, "Existing REV12 tag table required.");
                    foreach (string[] row in tags) Require(table.Tags.Find(row[0]) == null, "Tag already exists: " + row[0] + ". Refusing a partial/repeated application.");
                    Require(hmi.Connections.Find("HMI_Connection_1") != null, "Existing HMI_Connection_1 required; helper does not create connections.");
                    ICompilable compiler = hmis[0].Item.GetService<ICompilable>() ?? hmis[0].Device.GetService<ICompilable>();
                    Require(compiler != null, "HMI compiler unavailable.");
                    report.WriteLine("Preflight=PASS; Project=" + path + "; Timestamp=" + DateTimeOffset.Now.ToString("o"));
                    report.WriteLine("Scope=HMI_ONLY; Online/Download/Transfer/Hardware/PLC-modification APIs=NONE");
                    report.Flush();
                    // All identities and required objects checked before the first mutation.
                    foreach (string[] row in tags)
                    {
                        HmiTag tag = table.Tags.Create(row[0]);
                        tag.Connection = "HMI_Connection_1";
                        tag.PlcTag = row[1]; // Siemens resolves the DB field type, as in ImportUnifiedHmiTags.
                        tag.AcquisitionMode = HmiAcquisitionMode.CyclicOnUse;
                        tag.AcquisitionCycle = "T1s";
                    }
                    Apply(screen, items, bindings);
                    VerifyApplied(screen,items,bindings);
                    CompilerResult result = compiler.Compile();
                    Messages(result.Messages, report);
                    report.WriteLine("Errors=" + result.ErrorCount + "; Warnings=" + result.WarningCount);
                    if (result.ErrorCount != 0) { report.WriteLine("FAILED_UNSAVED; in-memory changes remain; discard and restore backup before retry."); return 1; }
                    project.Save();
                    report.WriteLine("SAVED; GUI visual verification and separate PLC compile still required.");
                }
                return 0;
            }
            catch (Exception e) { Console.Error.WriteLine(e); return 1; }
        }
        static void VerifyBackup(string project, string backup)
        {
            string source = Path.GetDirectoryName(project);
            Require(!backup.StartsWith(source + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && !String.Equals(backup, source, StringComparison.OrdinalIgnoreCase), "Backup must be outside the working project.");
            string copy = Path.Combine(backup, Path.GetFileName(project));
            Require(File.Exists(copy) && Hash(copy) == Hash(project), "Fresh project backup identity/hash missing or different.");
            // Complete project tree, except known volatile locks. Source must be saved/quiescent.
            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                if (Path.GetExtension(file).Equals(".lck", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(file).Equals(".lock", StringComparison.OrdinalIgnoreCase)) continue;
                string dest = Path.Combine(backup, file.Substring(source.Length + 1));
                Require(File.Exists(dest) && Hash(file) == Hash(dest), "Backup incomplete or stale: " + file.Substring(source.Length + 1));
            }
        }
        static string Hash(string file) { using (var sha = SHA256.Create()) using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", ""); }
        static IEnumerable<DeviceItem> DeviceItems(DeviceItemComposition items) { foreach (DeviceItem i in items) { yield return i; foreach (DeviceItem child in DeviceItems(i.DeviceItems)) yield return child; } }
        // Traverse only the documented engineering compositions: no private reflection,
        // guessed group names, ungrouping, or recreation of existing grouped objects.
        static IEnumerable<IEngineeringObject> Walk(IEngineeringObject obj, HashSet<IEngineeringObject> seen)
        {
            if (!seen.Add(obj)) yield break;
            yield return obj;
            foreach (EngineeringCompositionInfo info in obj.GetCompositionInfos())
            {
                object child = obj.GetComposition(info.Name);
                IEngineeringObject single = child as IEngineeringObject;
                if (single != null) { foreach (var nested in Walk(single, seen)) yield return nested; }
                else
                {
                    IEnumerable list = child as IEnumerable;
                    if (list == null) continue;
                    foreach (object value in list) { IEngineeringObject item = value as IEngineeringObject; if (item != null) foreach (var nested in Walk(item, seen)) yield return nested; }
                }
            }
        }
        static T Item<T>(Dictionary<string,HmiScreenItemBase> items, string name) where T:HmiScreenItemBase
        { HmiScreenItemBase item; Require(items.TryGetValue(name, out item) && item is T, "Required native item missing/wrong type: " + name + ". Grouped items must be exposed by the public API."); return (T)item; }
        static string CardName(int i) { return i==5 ? Prefix+"MappingTbc_Card_1" : i==6 ? Prefix+"MappingTbc_Card" : Prefix+Suffix[i]+"_Card"; }
        static string TitleName(int i) { return i==5 ? Prefix+"MappingTbc_Title_1" : i==6 ? Prefix+"MappingTbc_Title" : Prefix+Suffix[i]+"_Title"; }
        static bool IsNew(int index) { return index == 5 || index == 6 || index == 9; }
        // Read-only bounded evidence before guards. Never creates handlers or items.
        static void AuditPreflight(HmiScreen screen, Dictionary<string,HmiScreenItemBase> items)
        {
            Console.WriteLine("AUDIT_SCREEN="+screen.Name+";EVENT_COUNT="+screen.EventHandlers.Count);
            foreach(var handler in screen.EventHandlers)
            {
                Console.WriteLine("AUDIT_SCREEN_EVENT="+handler.EventType);
                string body=handler.Script.ScriptCode??"";
                Console.WriteLine("AUDIT_SCRIPT_BEGIN\n"+body.Substring(0,Math.Min(body.Length,12000))+"\nAUDIT_SCRIPT_END;LEN="+body.Length);
            }
            var names=new HashSet<string>(StringComparer.Ordinal);
            for(int i=0;i<11;i++)
            {
                names.Add(CardName(i)); names.Add(TitleName(i));
                if(!IsNew(i)) { names.Add(Prefix+Suffix[i]+"_Command"); names.Add("REV41_MANUAL_ValveOutput"+Suffix[i]+"_Text"); names.Add("REV41_MANUAL_ValveOutput"+Suffix[i]+"_Back"); }
            }
            foreach(string suffix in new[]{"EnableOn","EnableOff","Instruction","MappingTbc_Note","MappingTbc_Note_1","Y10Config_Note"}) names.Add(Prefix+suffix);
            foreach(string badge in new[]{"ValveSafety","ValveManual","ValveComm","ValveEnable"}) { names.Add("REV41_MANUAL_"+badge+"_Text"); names.Add("REV41_MANUAL_"+badge+"_Back"); }
            foreach(string name in names.OrderBy(n=>n))
            {
                HmiScreenItemBase item;
                if(!items.TryGetValue(name,out item)) { Console.WriteLine("AUDIT_MISSING="+name); continue; }
                Console.WriteLine("AUDIT_ITEM="+name+";TYPE="+item.GetType().FullName+";PARENT="+(item.Parent==null?"null":item.Parent.GetType().FullName)+";PARENT_IS_SCREEN="+Object.Equals(item.Parent,screen));
                foreach(string property in new[]{"Left","Top","Width","Height"})
                {
                    var info=item.GetType().GetProperty(property);
                    if(info!=null)Console.WriteLine("AUDIT_BOUND="+property+":"+info.GetValue(item,null));
                }
                HmiText text=item as HmiText;
                if(text!=null)foreach(MultilingualTextItem value in text.Text.Items)Console.WriteLine("AUDIT_TEXT="+value.Text);
            }
            Console.WriteLine("AUDIT_PREFLIGHT_END_NO_MUTATION");
        }
        static void Preflight(HmiScreen screen, Dictionary<string,HmiScreenItemBase> items, Binding b)
        {
            // Native audit has no lifecycle handlers. Create absent handlers only after all preflight guards.
            foreach (var type in new[] { HmiScreenEventType.Loaded, HmiScreenEventType.Unloaded }) { var handler=screen.EventHandlers.Find(type); Require(handler==null || !(handler.Script.ScriptCode??"").Contains(Marker), "Already applied."); }
            for (int i=0;i<11;i++)
            {
                HmiRectangle card = Item<HmiRectangle>(items, CardName(i));
                HmiText title = Item<HmiText>(items, TitleName(i));
                Require(title.Text.Items.Any(t => Regex.Replace(t.Text, "<[^>]+>", "").Trim().Replace(" | ", " - ") == "125Y" + (i+1) + " - " + Titles[i]), "Unexpected title; no title changes permitted.");
                Require(card.Width == 350 && card.Height == 100, "Unexpected card dimensions.");
                if (!IsNew(i))
                {
                    HmiButton button = Item<HmiButton>(items, b.valves[i].existingCommand);
                    Require(button.EventHandlers.All(e => e.EventType == HmiButtonEventType.Down || e.EventType == HmiButtonEventType.Up), "Unexpected command event handler.");
                    Item<HmiText>(items, "REV41_MANUAL_ValveOutput"+Suffix[i]+"_Text");
                    Item<HmiRectangle>(items, "REV41_MANUAL_ValveOutput"+Suffix[i]+"_Back");
                }
                else
                {
                    // Native audit proves grouped cards can expose Parent=screen and local 0,0.
                    // New root controls use separately GUI-verified absolute layout coordinates.
                    Require(Object.Equals(card.Parent, screen), "Unexpected card public parent.");
                    int absoluteLeft=i==5?765:25, absoluteTop=i==5?365:i==6?475:585;
                    Require((card.Left==absoluteLeft && card.Top==absoluteTop) || (i!=9 && card.Left==0 && card.Top==0), "Card coordinates differ from the audited local or GUI-verified absolute layout.");
                    Require(!items.ContainsKey("SMC_Logical_Y"+(i+1)+"_Command") && !items.ContainsKey("SMC_Logical_Y"+(i+1)+"_State"), "New logical object name already exists.");
                }
            }
            Item<HmiButton>(items, Prefix+"EnableOn"); Item<HmiButton>(items, Prefix+"EnableOff");
            if(items.ContainsKey(Prefix+"FillerExternalWash_Command")) Item<HmiButton>(items,Prefix+"FillerExternalWash_Command");
            Item<HmiText>(items, Prefix+"Instruction");
            Item<HmiText>(items, Prefix+"MappingTbc_Note"); Item<HmiText>(items, Prefix+"MappingTbc_Note_1"); Item<HmiText>(items, Prefix+"Y10Config_Note");
            foreach(string badge in new[]{"ValveSafety","ValveManual","ValveComm","ValveEnable"}) { Item<HmiText>(items,"REV41_MANUAL_"+badge+"_Text"); Item<HmiRectangle>(items,"REV41_MANUAL_"+badge+"_Back"); }
        }
        static void Apply(HmiScreen screen, Dictionary<string,HmiScreenItemBase> items, Binding b)
        {
            for (int i=0;i<11;i++)
            {
                Valve v = b.valves[i]; HmiButton command; HmiText state;
                if (IsNew(i))
                {
                    int left=i==5?765:25, top=i==5?365:i==6?475:585;
                    command = screen.ScreenItems.Create<HmiButton>("SMC_Logical_Y"+(i+1)+"_Command");
                    command.Left=left+15; command.Top=top+43; command.Width=205; command.Height=42;
                    command.BackColor=Color.FromArgb(18,91,148); command.ForeColor=Color.White; command.Font.Size=12;
                    command.Content.HorizontalTextAlignment=HmiHorizontalAlignment.Center; command.Content.VerticalTextAlignment=HmiVerticalAlignment.Center;
                    state=screen.ScreenItems.Create<HmiText>("SMC_Logical_Y"+(i+1)+"_State");
                    state.Left=left+230; state.Top=top+47; state.Width=105; state.Height=34;
                    state.ForeColor=Color.FromArgb(40,55,70); state.Font.Size=8;
                    state.HorizontalTextAlignment=HmiHorizontalAlignment.Center; state.VerticalTextAlignment=HmiVerticalAlignment.Center;
                }
                else
                {
                    command=Item<HmiButton>(items,v.existingCommand);
                    state=Item<HmiText>(items,"REV41_MANUAL_ValveOutput"+Suffix[i]+"_Text");
                    HmiRectangle back=Item<HmiRectangle>(items,"REV41_MANUAL_ValveOutput"+Suffix[i]+"_Back");
                    Dynamic(back,"BackColor","return HMIRuntime.Math.RGB(92,105,118);"); // Remove legacy coil/active colors.
                }
                SetText(command.Text,v.text); command.Visible=true; command.Enabled=false;
                Event(command,HmiButtonEventType.Down,v.downBody); Event(command,HmiButtonEventType.Up,v.upBody);
                Dynamic(command,"Enabled",b.requestEnabled);
                SetText(state.Text,"NO DATA"); state.Visible=true; Dynamic(state,"Text",v.stateText);
            }
            if(items.ContainsKey(Prefix+"FillerExternalWash_Command")) { var retired=Item<HmiButton>(items,Prefix+"FillerExternalWash_Command"); retired.Visible=false; retired.Enabled=false; Dynamic(retired,"Enabled","return false;"); Dynamic(retired,"Visible","return false;"); }
            HmiButton on=Item<HmiButton>(items,Prefix+"EnableOn"), off=Item<HmiButton>(items,Prefix+"EnableOff");
            foreach (var e in on.EventHandlers.ToArray()) e.Delete(); foreach (var e in off.EventHandlers.ToArray()) e.Delete();
            Event(on,HmiButtonEventType.Tapped,b.enableOnBody); Event(off,HmiButtonEventType.Tapped,b.enableOffBody);
            on.Enabled=false; off.Enabled=true; Dynamic(on,"Enabled",b.enableOnEnabled); Dynamic(off,"Enabled","return true;");
            string[] badges={"ValveSafety","ValveManual","ValveComm","ValveEnable"};
            string[] fields={"SafetyOK","ManualModeOK","ProfinetDeviceOK","Enable"};
            string[] yes={"SAFETY OK","MANUAL","PN DEVICE OK","TEST ENABLED"};
            string[] no={"SAFETY BLOCK","NOT MANUAL","PN DEVICE FAULT","TEST DISABLED"};
            for(int i=0;i<badges.Length;i++)
            {
                string read="let h=HMIRuntime.Tags(\"SMC_Logical_HmiLinkOK\");let hv=h.Read();let t=HMIRuntime.Tags(\"SMC_Logical_"+fields[i]+"\");let v=t.Read();if(h.LastError!==0||(Number(h.QualityCode)&192)<128||!hv||t.LastError!==0||(Number(t.QualityCode)&192)<128)";
                Dynamic(Item<HmiText>(items,"REV41_MANUAL_"+badges[i]+"_Text"),"Text",read+"return \"NO DATA\";return v?\""+yes[i]+"\":\""+no[i]+"\";");
                Dynamic(Item<HmiRectangle>(items,"REV41_MANUAL_"+badges[i]+"_Back"),"BackColor",read+"return HMIRuntime.Math.RGB(145,155,165);return v?HMIRuntime.Math.RGB(30,130,75):HMIRuntime.Math.RGB(150,80,30);");
            }
            AppendLifecycle(screen,HmiScreenEventType.Loaded,b.screenLoadedBody);
            AppendLifecycle(screen,HmiScreenEventType.Unloaded,b.screenUnloadedBody);
            SetText(Item<HmiText>(items,Prefix+"Instruction").Text,b.notice+" ACCEPTED = software request accepted, not physical feedback.");
            foreach(string n in new[]{"MappingTbc_Note","MappingTbc_Note_1","Y10Config_Note"}) { HmiText note=Item<HmiText>(items,Prefix+n); note.Visible=false; note.Enabled=false; }
        }
        static void VerifyApplied(HmiScreen screen,Dictionary<string,HmiScreenItemBase> original,Binding b)
        {
            for(int i=0;i<11;i++)
            {
                HmiButton command=IsNew(i)?screen.ScreenItems.Find("SMC_Logical_Y"+(i+1)+"_Command") as HmiButton:Item<HmiButton>(original,b.valves[i].existingCommand);
                Require(command!=null && command.Visible,"Applied request button missing.");
                Require(command.EventHandlers.Find(HmiButtonEventType.Down).Script.ScriptCode==b.valves[i].downBody && command.EventHandlers.Find(HmiButtonEventType.Up).Script.ScriptCode==b.valves[i].upBody,"Applied request mapping mismatch.");
                var enabled=command.Dynamizations.Find("Enabled") as ScriptDynamization;
                Require(enabled!=null && enabled.ScriptCode==b.requestEnabled && !command.Enabled,"Applied permit/quality guard mismatch.");
                Console.WriteLine("VERIFY_REQUEST="+b.valves[i].valve+";OBJECT="+command.Name+";TAG="+b.valves[i].requestTag+";DOWN_UP_EXACT=true;PERMIT_QUALITY_EXACT=true");
            }
            if(original.ContainsKey(Prefix+"FillerExternalWash_Command"))
            {
                var retired=Item<HmiButton>(original,Prefix+"FillerExternalWash_Command");
                Require(!retired.Visible && !retired.Enabled,"Retired Y10 command must stay hidden and disabled.");
                Console.WriteLine("VERIFY_RETIRED_Y10=HIDDEN_DISABLED;LOGICAL_Y10_DISTINCT=true");
            }
        }
        static void AppendLifecycle(HmiScreen screen,HmiScreenEventType type,string body)
        {
            var handler=screen.EventHandlers.Find(type);
            bool created=handler==null;
            if(created)handler=screen.EventHandlers.Create(type);
            string original=handler.Script.ScriptCode??"";
            handler.Script.ScriptCode=original+"\n"+Marker+"\n"+body;
            Console.WriteLine("LIFECYCLE="+type+";CREATED="+created+";PRESERVED_SCRIPT_LENGTH="+original.Length+";NEW_DB_RESETS_APPENDED=true");
        }
        static void Event(HmiButton button,HmiButtonEventType type,string body) { (button.EventHandlers.Find(type)??button.EventHandlers.Create(type)).Script.ScriptCode=body; }
        static void Dynamic(HmiScreenItemBase item,string property,string body) { var old=item.Dynamizations.Find(property); if(old!=null)old.Delete(); var d=item.Dynamizations.Create<ScriptDynamization>(property); d.Async=false; d.Trigger.Type=TriggerType.T500ms; d.ScriptCode=body; }
        static void SetText(MultilingualText text,string value) { foreach(MultilingualTextItem t in text.Items)t.Text="<body><p>"+value.Replace("&","&amp;").Replace("<","&lt;").Replace(">","&gt;")+"</p></body>"; }
        static void ValidateTags(string[][] tags)
        {
            var expected=new Dictionary<string,string>();
            for(int i=1;i<=11;i++) foreach(string field in new[]{"Request","Accepted","State","Channel","PhysicalVerified"})expected.Add("SMC_Y"+i+"_"+field,"DB_SMC_LogicalTest."+field+"["+i+"]");
            foreach(string field in new[]{"PageActive","Enable","Permit","HmiLinkOK","ProfinetDeviceOK","SafetyOK","ManualModeOK","ProcessInactive","AirPressureOK","DoorsOK","AlarmsOK","PhysicalOutputsEnabled"})expected.Add("SMC_Logical_"+field,"DB_SMC_LogicalTest."+field);
            Require(tags.Length==67 && tags.Select(t=>t[0]).Distinct().Count()==67,"Exactly 67 unique logical tags required.");
            foreach(var row in tags)Require(row.Length==6 && expected.ContainsKey(row[0]) && expected[row[0]]==row[1],"Unexpected CSV mapping.");
        }
        static void ValidateBindings(Binding b)
        {
            Require(b!=null && b.valves!=null && b.valves.Length==11,"Exactly 11 bindings required.");
            for(int i=0;i<11;i++)
            {
                Valve v=b.valves[i]; string tag="SMC_Y"+(i+1)+"_Request";
                Require(v.valve=="125Y"+(i+1) && v.function==Titles[i] && v.requestTag==tag,"Valve identity mismatch.");
                Require(v.existingCommand==(IsNew(i)?null:Prefix+Suffix[i]+"_Command"),"Command identity mismatch.");
                Require(v.downBody=="HMIRuntime.Tags(\""+tag+"\").Write(true);" && v.upBody=="HMIRuntime.Tags(\""+tag+"\").Write(false);","Unexpected request write.");
                Require(v.stateText.Contains("SMC_Y"+(i+1)+"_State") && v.stateText.Contains("QualityCode") && !v.stateText.Contains(".Write("),"Unexpected state script.");
            }
            Require(b.requestEnabled.Contains("SMC_Logical_Permit") && b.requestEnabled.Contains("QualityCode") && !b.requestEnabled.Contains(".Write("),"Permit/quality binding required.");
            Require(b.enableOnEnabled.Contains("QualityCode") && !b.enableOnEnabled.Contains(".Write("),"Enable quality required.");
            Require(b.enableOnBody=="HMIRuntime.Tags(\"SMC_Logical_Enable\").Write(true);" && b.enableOffBody=="HMIRuntime.Tags(\"SMC_Logical_Enable\").Write(false);","Unexpected enable writes.");
            foreach(string body in new[]{b.screenLoadedBody,b.screenUnloadedBody})
            {
                string remainder=Regex.Replace(body,@"HMIRuntime\.Tags\(""SMC_(?:Logical_(?:Enable|PageActive)|Y(?:[1-9]|10|11)_Request)""\)\.Write\((?:false|true)\);","");
                Require(String.IsNullOrWhiteSpace(remainder),"Unexpected lifecycle operation.");
                for(int i=1;i<=11;i++)Require(body.Contains("HMIRuntime.Tags(\"SMC_Y"+i+"_Request\").Write(false);"),"Lifecycle request reset missing.");
                Require(body.Contains("HMIRuntime.Tags(\"SMC_Logical_Enable\").Write(false);"),"Lifecycle enable reset missing.");
            }
        }
        static void Messages(IEnumerable<CompilerResultMessage> messages,TextWriter report) { foreach(var m in messages){report.WriteLine(m.State+"\t"+m.Path+"\t"+m.Description); Messages(m.Messages,report);} }
        static void Require(bool condition,string message) { if(!condition)throw new InvalidOperationException(message); }
    }
}








