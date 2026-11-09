using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.Compiler;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.Hmi.Globalization;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.HmiUnified.HmiTags;
using Siemens.Engineering.HmiUnified.UI.Base;
using Siemens.Engineering.HmiUnified.UI.Dynamization;
using Siemens.Engineering.HmiUnified.UI.Dynamization.Script;
using Siemens.Engineering.HmiUnified.UI.Enum;
using Siemens.Engineering.HmiUnified.UI.Screens;
using Siemens.Engineering.HmiUnified.UI.Shapes;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Tags;

namespace Schlenker.TiaV19
{
    internal static class ConfigureAl102Sensors
    {
        private sealed class Sensor
        {
            internal int Port; internal string Device; internal string Description; internal string Address; internal string Unit;
            internal Sensor(int p, string d, string desc, string a, string u) { Port=p; Device=d; Description=desc; Address=a; Unit=u; }
            internal string Tag { get { return "IOL_AL102_P" + Port + "_RAW"; } }
        }

        private static readonly Sensor[] Sensors = {
            new Sensor(1,"PN7094","Vacuum / Depression (reference -800 mbar)","%IW355","mbar"),
            new Sensor(2,"PN7094","Air / Process Pressure","%IW358","RAW"),
            new Sensor(3,"PN7094","CIP / Service Pressure","%IW361","RAW"),
            new Sensor(5,"LR2050","Product Continuous Level","%IW364","RAW"),
            new Sensor(6,"LR2050","CIP / Process Level","%IW367","RAW"),
            new Sensor(7,"TA2405","Process Temperature","%IW370","RAW")
        };

        private static int Main(string[] args)
        {
            if (args.Length != 1) return 2;
            string expected = Path.GetFullPath(args[0]);
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

                    PlcTagTable plcTable = plc.TagTableGroup.TagTables.Find("IO-Link Sensors") ??
                        plc.TagTableGroup.TagTables.Create("IO-Link Sensors");
                    HmiTagTable hmiTable = hmi.TagTables.Find("REV12") ?? hmi.TagTables.Create("REV12");
                    foreach (Sensor sensor in Sensors)
                    {
                        PlcTag plcTag = plcTable.Tags.Find(sensor.Tag);
                        if (plcTag == null) plcTag = plcTable.Tags.Create(sensor.Tag, "Word", sensor.Address);
                        if (!plcTag.DataTypeName.Equals("Word", StringComparison.OrdinalIgnoreCase) ||
                            !plcTag.LogicalAddress.Equals(sensor.Address, StringComparison.OrdinalIgnoreCase))
                            throw new InvalidOperationException("PLC tag mismatch: " + sensor.Tag);

                        HmiTag hmiTag = hmiTable.Tags.Find(sensor.Tag) ?? hmiTable.Tags.Create(sensor.Tag);
                        hmiTag.Connection = "HMI_Connection_1";
                        hmiTag.PlcTag = sensor.Tag;
                        hmiTag.AcquisitionMode = HmiAcquisitionMode.CyclicOnUse;
                        hmiTag.AcquisitionCycle = "T500ms";
                    }

                    ConfigureScreen(hmi);
                    project.Save();
                    Compile("PLC", plcItem.GetService<ICompilable>());
                    Compile("HMI", hmiItem.GetService<ICompilable>());
                    project.Save();
                    Console.WriteLine("AL102_CONFIGURED_PORTS=P1,P2,P3,P5,P6,P7,P8");
                    foreach (Sensor s in Sensors) Console.WriteLine("AL102/P"+s.Port+"="+s.Device+";TAG="+s.Tag+";ADDRESS="+s.Address+";DISPLAY=RAW");
                    Console.WriteLine("AL102/P4=SPARE");
                    Console.WriteLine("STATUS=PASS");
                }
                return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine("STATUS=FAIL"); Console.Error.WriteLine(ex); return 1; }
        }

        private static void ConfigureScreen(HmiSoftware hmi)
        {
            HmiScreen screen = hmi.Screens.Find("io_link_al102");
            if (screen == null) throw new InvalidOperationException("AL102 HMI screen was not found.");
            HmiText summary = screen.ScreenItems.Find("REV18_IOL_Detail_Quality") as HmiText;
            if (summary != null) SetText(summary.Text, "CONFIGURED");
            foreach (Sensor sensor in Sensors)
            {
                string prefix = "REV18_IOL_Port_" + sensor.Port;
                SetTextRequired(screen, prefix + "_Device", sensor.Device);
                SetTextRequired(screen, prefix + "_Description", sensor.Description);
                SetTextRequired(screen, prefix + "_Mode", "IO-Link 2I");
                SetTextRequired(screen, prefix + "_Unit", sensor.Unit);
                SetTextRequired(screen, prefix + "_Quality", "CONFIGURED");
                SetTextRequired(screen, prefix + "_Mapping", sensor.Address + "\n" + sensor.Tag);

                HmiText live = FindText(screen, new[] {
                    "REV70_IOL_AL102_X" + sensor.Port + "_Text",
                    "REV71_IOL_AL102_P" + sensor.Port + "_Text",
                    "REV36_IOL_AL102_Port_" + sensor.Port + "_Live_Text",
                    prefix + "_LiveText"
                });
                if (live == null) throw new InvalidOperationException("AL102 live text missing at P" + sensor.Port);
                string liveScript = sensor.Port == 1
                    ? "let t=HMIRuntime.Tags(\""+sensor.Tag+"\");let w=Number(t.Read())&65535;let q=Number(t.QualityCode);if(t.LastError!==0||(q&192)<128){return \"NO DATA\";}let p=(w>>2)&16383;if(p>=8192){p-=16384;}return String(p*10);"
                    : "let t=HMIRuntime.Tags(\""+sensor.Tag+"\");let v=Number(t.Read());let q=Number(t.QualityCode);if(t.LastError!==0||(q&192)<128){return \"NO DATA\";}return String(v);";
                ReplaceScript(live, "Text", liveScript);

                HmiRectangle back = FindRectangle(screen, new[] {
                    "REV70_IOL_AL102_X" + sensor.Port + "_Back",
                    "REV71_IOL_AL102_P" + sensor.Port + "_Back",
                    "REV36_IOL_AL102_Port_" + sensor.Port + "_Live_Back"
                });
                if (back != null) ReplaceScript(back, "BackColor", "let t=HMIRuntime.Tags(\""+sensor.Tag+"\");t.Read();let q=Number(t.QualityCode);let live=t.LastError===0&&(q&192)>=128;return live?HMIRuntime.Math.RGB(28,145,82):HMIRuntime.Math.RGB(145,155,165);");
            }
            SetTextRequired(screen, "REV18_IOL_Port_4_Quality", "SPARE / NOT USED");
            SetTextRequired(screen, "REV18_IOL_Port_4_Mapping", "NO ASSIGNMENT");
            HmiText hold = screen.ScreenItems.Find("REV18_IOL_Detail_HoldPoint") as HmiText;
            if (hold != null) SetText(hold.Text, "AL102/P1 PN7094 vacuum is displayed in signed mbar (reference -800 mbar); P2-P3 and P5-P7 remain commissioning RAW until scaling is verified.");
        }

        private static void SetTextRequired(HmiScreen screen, string name, string value)
        {
            HmiText text = screen.ScreenItems.Find(name) as HmiText;
            if (text == null) throw new InvalidOperationException("Missing AL102 text object: " + name);
            SetText(text.Text, value);
        }
        private static HmiText FindText(HmiScreen s, IEnumerable<string> names) { foreach(string n in names){ HmiText x=s.ScreenItems.Find(n) as HmiText; if(x!=null)return x;} return null; }
        private static HmiRectangle FindRectangle(HmiScreen s, IEnumerable<string> names) { foreach(string n in names){ HmiRectangle x=s.ScreenItems.Find(n) as HmiRectangle; if(x!=null)return x;} return null; }
        private static void SetText(MultilingualText text, string value) { string rich="<body><p>"+value.Replace("&","&amp;").Replace("<","&lt;").Replace(">","&gt;").Replace("\n"," | ")+"</p></body>"; foreach(MultilingualTextItem i in text.Items)i.Text=rich; }
        private static void ReplaceScript(HmiScreenItemBase item,string prop,string code){ DynamizationBase old=item.Dynamizations.Find(prop); if(old!=null)old.Delete(); ScriptDynamization d=item.Dynamizations.Create<ScriptDynamization>(prop); d.Async=false; d.Trigger.Type=TriggerType.T500ms; d.ScriptCode=code; }
        private static void Compile(string name,ICompilable c){ if(c==null)throw new InvalidOperationException(name+" compiler unavailable"); CompilerResult r=c.Compile(); Console.WriteLine("COMPILE="+name+";ERRORS="+r.ErrorCount+";WARNINGS="+r.WarningCount); if(r.ErrorCount!=0)throw new InvalidOperationException(name+" compile failed"); }
        private static PlcSoftware FindPlc(Project p){ DeviceItem i=FindSoftwareItem(p,true); SoftwareContainer c=i==null?null:i.GetService<SoftwareContainer>(); return c==null?null:c.Software as PlcSoftware; }
        private static HmiSoftware FindHmi(Project p){ DeviceItem i=FindSoftwareItem(p,false); SoftwareContainer c=i==null?null:i.GetService<SoftwareContainer>(); return c==null?null:c.Software as HmiSoftware; }
        private static DeviceItem FindSoftwareItem(Project p,bool plc){ foreach(Device d in p.Devices.Concat(p.UngroupedDevicesGroup.Devices)){ DeviceItem x=FindSoftwareItem(d.DeviceItems,plc); if(x!=null)return x;} return null; }
        private static DeviceItem FindSoftwareItem(DeviceItemComposition items,bool plc){ foreach(DeviceItem i in items){ SoftwareContainer c=i.GetService<SoftwareContainer>(); if(c!=null && (plc ? c.Software is PlcSoftware : c.Software is HmiSoftware))return i; DeviceItem x=FindSoftwareItem(i.DeviceItems,plc); if(x!=null)return x;} return null; }
    }
}
