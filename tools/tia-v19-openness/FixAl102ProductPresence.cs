using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Siemens.Engineering;
using Siemens.Engineering.Compiler;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.Hmi.Globalization;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.HmiUnified.UI.Base;
using Siemens.Engineering.HmiUnified.UI.Dynamization;
using Siemens.Engineering.HmiUnified.UI.Dynamization.Script;
using Siemens.Engineering.HmiUnified.UI.Enum;
using Siemens.Engineering.HmiUnified.UI.Screens;
using Siemens.Engineering.HmiUnified.UI.Shapes;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;

namespace Schlenker.TiaV19
{
    internal static class FixAl102ProductPresence
    {
        private const string OldSource = "SPL100_LMT100_ProcessData";
        private const string NewSource = "IOL_AL102_P1_RAW";

        private static int Main(string[] args)
        {
            if (args.Length != 2) return 2;
            string expected = Path.GetFullPath(args[0]);
            string evidence = Path.GetFullPath(args[1]);
            Directory.CreateDirectory(evidence);
            try
            {
                TiaPortalProcess process = TiaPortal.GetProcesses().Single(p => p.ProjectPath != null &&
                    Path.GetFullPath(p.ProjectPath.FullName).Equals(expected, StringComparison.OrdinalIgnoreCase));
                using (TiaPortal portal = process.Attach())
                {
                    Project project = portal.Projects.Single();
                    PlcSoftware plc = FindPlc(project);
                    HmiSoftware hmi = FindHmi(project);
                    DeviceItem plcItem = FindSoftwareItem(project, true);
                    DeviceItem hmiItem = FindSoftwareItem(project, false);
                    if (plc == null || hmi == null || plcItem == null || hmiItem == null)
                        throw new InvalidOperationException("PLC/HMI software was not found.");

                    PlcBlockComposition owner = null;
                    PlcBlock block = FindBlock(plc.BlockGroup, "FB_Application", ref owner);
                    if (block == null || owner == null) throw new InvalidOperationException("FB_Application was not found.");
                    string before = Path.Combine(evidence, "FB_Application.before.xml");
                    string changedPath = Path.Combine(evidence, "FB_Application.al102-product-presence.xml");
                    block.Export(new FileInfo(before), ExportOptions.WithDefaults);
                    string xml = File.ReadAllText(before, Encoding.UTF8);
                    int occurrences = Count(xml, "Component Name=\"" + OldSource + "\"");
                    if (occurrences != 1)
                        throw new InvalidOperationException("Expected one old LMT100 source in FB_Application, found " + occurrences + ".");
                    string changed = xml.Replace("Component Name=\"" + OldSource + "\"",
                        "Component Name=\"" + NewSource + "\"");
                    File.WriteAllText(changedPath, changed, new UTF8Encoding(false));
                    owner.Import(new FileInfo(changedPath), ImportOptions.Override);

                    HmiScreen screen = hmi.Screens.Find("io_link_al102");
                    if (screen == null) throw new InvalidOperationException("AL102 HMI screen was not found.");
                    SetTextRequired(screen, "REV18_IOL_Port_1_Device", "LMT100");
                    SetTextRequired(screen, "REV18_IOL_Port_1_Description", "Product presence before pump");
                    SetTextRequired(screen, "REV18_IOL_Port_1_Mode", "IO-Link 2I");
                    SetTextRequired(screen, "REV18_IOL_Port_1_Unit", "BOOL");
                    SetTextRequired(screen, "REV18_IOL_Port_1_Quality", "CONFIGURED");
                    SetTextRequired(screen, "REV18_IOL_Port_1_Mapping", "%IW355.%X0\nProduct_Present_At_Pump");

                    HmiText live = Find<HmiText>(screen, new[] {
                        "REV70_IOL_AL102_X1_Text", "REV71_IOL_AL102_P1_Text",
                        "REV36_IOL_AL102_Port_1_Live_Text", "REV18_IOL_Port_1_LiveText" });
                    HmiRectangle back = Find<HmiRectangle>(screen, new[] {
                        "REV70_IOL_AL102_X1_Back", "REV71_IOL_AL102_P1_Back",
                        "REV36_IOL_AL102_Port_1_Live_Back" });
                    if (live == null || back == null) throw new InvalidOperationException("AL102 P1 live status objects were not found.");
                    ReplaceScript(live, "Text",
                        "let t=HMIRuntime.Tags(\"Product_Present_At_Pump\");let v=Boolean(t.Read());" +
                        "let q=Number(t.QualityCode);if(t.LastError!==0||(q&192)<128){return \"NO DATA\";}return v?\"ON\":\"OFF\";");
                    ReplaceScript(back, "BackColor",
                        "let t=HMIRuntime.Tags(\"Product_Present_At_Pump\");let v=Boolean(t.Read());" +
                        "let q=Number(t.QualityCode);if(t.LastError!==0||(q&192)<128){return HMIRuntime.Math.RGB(145,155,165);}" +
                        "return v?HMIRuntime.Math.RGB(28,145,82):HMIRuntime.Math.RGB(145,155,165);");

                    project.Save();
                    Compile("PLC", plcItem.GetService<ICompilable>());
                    Compile("HMI", hmiItem.GetService<ICompilable>());
                    project.Save();
                    Console.WriteLine("PLC_ASSIGNMENT=DB_Global.Inp.ProductPresentAtPump := IOL_AL102_P1_RAW.%X0");
                    Console.WriteLine("AL102_P1=LMT100;DISPLAY=ON/OFF/NO DATA;PROCESS_WORD=%IW355;OUT1=%X0");
                    Console.WriteLine("DOWNLOAD_PERFORMED=NO");
                    Console.WriteLine("STATUS=PASS");
                }
                return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine("STATUS=FAIL"); Console.Error.WriteLine(ex); return 1; }
        }

        private static int Count(string source, string value) { int n=0,i=0; while((i=source.IndexOf(value,i,StringComparison.Ordinal))>=0){n++;i+=value.Length;} return n; }
        private static void Compile(string name,ICompilable c){ if(c==null)throw new InvalidOperationException(name+" compiler unavailable"); CompilerResult r=c.Compile(); Console.WriteLine("COMPILE="+name+";ERRORS="+r.ErrorCount+";WARNINGS="+r.WarningCount); if(r.ErrorCount!=0)throw new InvalidOperationException(name+" compile failed"); }
        private static void SetTextRequired(HmiScreen s,string n,string v){HmiText x=s.ScreenItems.Find(n) as HmiText;if(x==null)throw new InvalidOperationException("Missing AL102 object: "+n);SetText(x.Text,v);}
        private static void SetText(MultilingualText t,string v){string r="<body><p>"+v.Replace("&","&amp;").Replace("<","&lt;").Replace(">","&gt;").Replace("\n"," | ")+"</p></body>";foreach(MultilingualTextItem i in t.Items)i.Text=r;}
        private static T Find<T>(HmiScreen s,IEnumerable<string> names) where T:HmiScreenItemBase{foreach(string n in names){T x=s.ScreenItems.Find(n) as T;if(x!=null)return x;}return null;}
        private static void ReplaceScript(HmiScreenItemBase i,string p,string c){DynamizationBase o=i.Dynamizations.Find(p);if(o!=null)o.Delete();ScriptDynamization d=i.Dynamizations.Create<ScriptDynamization>(p);d.Async=false;d.Trigger.Type=TriggerType.T500ms;d.ScriptCode=c;}
        private static PlcBlock FindBlock(PlcBlockGroup g,string n,ref PlcBlockComposition o){PlcBlock b=g.Blocks.Find(n);if(b!=null){o=g.Blocks;return b;}foreach(PlcBlockUserGroup c in g.Groups){b=FindBlock(c,n,ref o);if(b!=null)return b;}return null;}
        private static PlcSoftware FindPlc(Project p){DeviceItem i=FindSoftwareItem(p,true);SoftwareContainer c=i==null?null:i.GetService<SoftwareContainer>();return c==null?null:c.Software as PlcSoftware;}
        private static HmiSoftware FindHmi(Project p){DeviceItem i=FindSoftwareItem(p,false);SoftwareContainer c=i==null?null:i.GetService<SoftwareContainer>();return c==null?null:c.Software as HmiSoftware;}
        private static DeviceItem FindSoftwareItem(Project p,bool plc){foreach(Device d in p.Devices.Concat(p.UngroupedDevicesGroup.Devices)){DeviceItem x=FindSoftwareItem(d.DeviceItems,plc);if(x!=null)return x;}return null;}
        private static DeviceItem FindSoftwareItem(DeviceItemComposition items,bool plc){foreach(DeviceItem i in items){SoftwareContainer c=i.GetService<SoftwareContainer>();if(c!=null&&(plc?c.Software is PlcSoftware:c.Software is HmiSoftware))return i;DeviceItem x=FindSoftwareItem(i.DeviceItems,plc);if(x!=null)return x;}return null;}
    }
}
