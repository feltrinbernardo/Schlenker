using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.Hmi.Globalization;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.HmiUnified.UI.Base;
using Siemens.Engineering.HmiUnified.UI.Screens;
using Siemens.Engineering.HmiUnified.UI.Shapes;
using Siemens.Engineering.HmiUnified.UI.Dynamization;
using Siemens.Engineering.HmiUnified.UI.Dynamization.Script;
using Siemens.Engineering.HmiUnified.UI.Enum;
using Siemens.Engineering.SW;

namespace Schlenker.TiaV19
{
    internal static class RenameTls100ToLr100
    {
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
                    HmiSoftware hmi = FindHmi(project);
                    if (hmi == null) throw new InvalidOperationException("HMI software not found.");
                    int changed = 0;
                    foreach (HmiScreen screen in hmi.Screens)
                    {
                        int screenChanges = 0;
                        foreach (HmiScreenItemBase item in screen.ScreenItems)
                        {
                            HmiText text = item as HmiText;
                            if (text == null) continue;
                            foreach (MultilingualTextItem language in text.Text.Items)
                            {
                                string before = language.Text ?? String.Empty;
                                string after = before.Replace("TLS100", "LR100");
                                if (!after.Equals(before, StringComparison.Ordinal))
                                {
                                    language.Text = after;
                                    screenChanges++;
                                    changed++;
                                }
                            }
                        }
                        if (screenChanges != 0)
                            Console.WriteLine("SCREEN=" + screen.Name + ";TLS100_TO_LR100=" + screenChanges);
                    }
                    Console.WriteLine("VISIBLE_TLS100_REPLACEMENTS=" + changed);

                    HmiScreen diagnostics = hmi.Screens.Find("io_link_al102");
                    if (diagnostics == null) throw new InvalidOperationException("io_link_al102 not found.");
                    HmiText x5Live = FindText(diagnostics, new[] {
                        "REV70_IOL_AL102_X5_Text", "REV71_IOL_AL102_P5_Text",
                        "REV36_IOL_AL102_Port_5_Live_Text", "REV18_IOL_Port_5_LiveText" });
                    if (x5Live == null) throw new InvalidOperationException("AL102 X5 live text not found.");
                    ReplaceScript(x5Live, "Text",
                        "let t=HMIRuntime.Tags(\"IOL_AL102_P5_RAW\");let v=Number(t.Read())&65535;"+
                        "let q=Number(t.QualityCode);if(t.LastError!==0||(q&192)<128){return \"NO DATA\";}"+
                        "let mm=(v>>2)&16383;if(mm===8188||mm===8189||mm===8190){return \"DIAG \"+String(mm);}"+
                        "return String(mm);");
                    SetTextRequired(diagnostics, "REV18_IOL_Port_5_Device", "LR2050");
                    SetTextRequired(diagnostics, "REV18_IOL_Port_5_Description", "Filler tank product level");
                    SetTextRequired(diagnostics, "REV18_IOL_Port_5_Unit", "mm");
                    SetTextRequired(diagnostics, "REV18_IOL_Port_5_Quality", "CONFIGURED");
                    SetTextRequired(diagnostics, "REV18_IOL_Port_5_Mapping", "%IW364\nIOL_AL102_P5_RAW");

                    HmiText x6Live = FindText(diagnostics, new[] {
                        "REV70_IOL_AL102_X6_Text", "REV71_IOL_AL102_P6_Text",
                        "REV36_IOL_AL102_Port_6_Live_Text", "REV18_IOL_Port_6_LiveText" });
                    if (x6Live == null) throw new InvalidOperationException("AL102 X6 live text not found.");
                    ReplaceScript(x6Live, "Text",
                        "let t=HMIRuntime.Tags(\"IOL_AL102_P6_RAW\");let v=Number(t.Read())&65535;"+
                        "let q=Number(t.QualityCode);if(t.LastError!==0||(q&192)<128){return \"BYPASS\";}"+
                        "let mm=(v>>2)&16383;if(mm===8188||mm===8189||mm===8190){return \"BYPASS\";}"+
                        "return String(mm);");
                    SetTextRequired(diagnostics, "REV18_IOL_Port_6_Device", "LR2050");
                    SetTextRequired(diagnostics, "REV18_IOL_Port_6_Description", "CIP level - sensor not installed");
                    SetTextRequired(diagnostics, "REV18_IOL_Port_6_Mode", "IO-Link 2I");
                    SetTextRequired(diagnostics, "REV18_IOL_Port_6_Unit", "mm");
                    SetTextRequired(diagnostics, "REV18_IOL_Port_6_Quality", "BYPASSED / NOT INSTALLED");
                    SetTextRequired(diagnostics, "REV18_IOL_Port_6_Mapping", "%IW367 | IOL_AL102_P6_RAW | NO PROCESS LOGIC");

                    HmiText x7Live = FindText(diagnostics, new[] {
                        "REV70_IOL_AL102_X7_Text", "REV71_IOL_AL102_P7_Text",
                        "REV36_IOL_AL102_Port_7_Live_Text", "REV18_IOL_Port_7_LiveText" });
                    if (x7Live == null) throw new InvalidOperationException("AL102 X7 live text not found.");
                    ReplaceScript(x7Live, "Text",
                        "let t=HMIRuntime.Tags(\"IOL_AL102_P7_RAW\");let v=Number(t.Read())&65535;"+
                        "let q=Number(t.QualityCode);if(t.LastError!==0||(q&192)<128){return \"NO DATA\";}"+
                        "if(v>=32768){v-=65536;}return (v*0.1).toFixed(1);");
                    SetTextRequired(diagnostics, "REV18_IOL_Port_7_Device", "TA2405");
                    SetTextRequired(diagnostics, "REV18_IOL_Port_7_Description", "Process temperature");
                    SetTextRequired(diagnostics, "REV18_IOL_Port_7_Mode", "IO-Link 2I");
                    SetTextRequired(diagnostics, "REV18_IOL_Port_7_Unit", "°C");
                    SetTextRequired(diagnostics, "REV18_IOL_Port_7_Quality", "CONFIGURED");
                    SetTextRequired(diagnostics, "REV18_IOL_Port_7_Mapping", "%IW370 | IOL_AL102_P7_RAW");
                    project.Save();
                    Console.WriteLine("TOTAL_REPLACEMENTS=" + changed);
                    Console.WriteLine("TANK_LEVEL_HMI_TAG=Tank_Level_Pct;PLC_TAG=DB_Global.Inp.TankLevelPct");
                    Console.WriteLine("DOWNLOAD=NO");
                    Console.WriteLine("STATUS=PASS");
                }
                return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine("STATUS=FAIL"); Console.Error.WriteLine(ex); return 1; }
        }

        private static HmiText FindText(HmiScreen s, IEnumerable<string> names) { foreach (string n in names) { HmiText x=s.ScreenItems.Find(n) as HmiText; if(x!=null)return x; } return null; }
        private static void SetTextRequired(HmiScreen s,string n,string value){HmiText x=s.ScreenItems.Find(n) as HmiText;if(x==null)throw new InvalidOperationException("Missing HMI object: "+n);string rich="<body><p>"+value.Replace("&","&amp;").Replace("<","&lt;").Replace(">","&gt;").Replace("\n"," | ")+"</p></body>";foreach(MultilingualTextItem i in x.Text.Items)i.Text=rich;}
        private static void ReplaceScript(HmiScreenItemBase item,string prop,string code){DynamizationBase old=item.Dynamizations.Find(prop);if(old!=null)old.Delete();ScriptDynamization d=item.Dynamizations.Create<ScriptDynamization>(prop);d.Async=false;d.Trigger.Type=TriggerType.T500ms;d.ScriptCode=code;}

        private static HmiSoftware FindHmi(Project p)
        {
            foreach (Device d in p.Devices.Concat(p.UngroupedDevicesGroup.Devices))
            {
                HmiSoftware h = FindHmi(d.DeviceItems);
                if (h != null) return h;
            }
            return null;
        }
        private static HmiSoftware FindHmi(DeviceItemComposition items)
        {
            foreach (DeviceItem i in items)
            {
                SoftwareContainer c = i.GetService<SoftwareContainer>();
                HmiSoftware h = c == null ? null : c.Software as HmiSoftware;
                if (h != null) return h;
                h = FindHmi(i.DeviceItems);
                if (h != null) return h;
            }
            return null;
        }
    }
}
