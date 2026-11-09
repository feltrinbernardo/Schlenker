using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.Compiler;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.Hmi.Globalization;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.HmiUnified.UI.Screens;
using Siemens.Engineering.HmiUnified.UI.Shapes;
using Siemens.Engineering.HmiUnified.UI.Base;
using Siemens.Engineering.HmiUnified.UI.Dynamization;
using Siemens.Engineering.HmiUnified.UI.Dynamization.Script;
using Siemens.Engineering.HmiUnified.UI.Enum;
using Siemens.Engineering.SW;

namespace Schlenker.TiaV19
{
    internal static class SetAl102SparePortsSio
    {
        private const string Prefix = "GSD:GSDML-V2.35-IFM-AL1X0X-20241112-144500.XML/SM/";

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
                    Device al102 = AllDevices(project).Single(d => d.Name.Equals("AL102", StringComparison.OrdinalIgnoreCase));
                    DeviceItem module = FindItems(al102.DeviceItems).Single(i =>
                        (i.TypeIdentifier ?? "").IndexOf("IOLM_MODULE_8P_ENMO", StringComparison.OrdinalIgnoreCase) >= 0);

                    for (int port = 2; port <= 4; port++)
                    {
                        DeviceItem existing = FindPortDevice(al102, module, port);
                        string requiredType = Prefix + "IDS DI";
                        if (existing != null && !(existing.TypeIdentifier ?? "").Equals(requiredType, StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine("AL102/X" + port + "=REMOVE;TYPE=" + existing.TypeIdentifier + ";NAME=" + existing.Name);
                            existing.Delete();
                            existing = null;
                        }
                        if (existing == null)
                        {
                            string name = "X" + port + "_Spare_SIO_DI";
                            if (!module.CanPlugNew(requiredType, name, port))
                                throw new InvalidOperationException("Cannot configure AL102/X" + port + " as SIO DI.");
                            existing = module.PlugNew(requiredType, name, port);
                        }
                        existing.Name = "X" + port + "_Spare_SIO_DI";
                        Console.WriteLine("AL102/X" + port + "=SPARE_SIO_DI;ADDRESSES=" +
                            String.Join(",", existing.Addresses.Select(a => a.IoType + ":" + a.StartAddress + ":" + a.Length)));
                    }

                    HmiSoftware hmi = FindHmi(project);
                    if (hmi == null) throw new InvalidOperationException("HMI software was not found.");
                    HmiScreen screen = hmi.Screens.Find("io_link_al102");
                    if (screen == null) throw new InvalidOperationException("io_link_al102 was not found.");
                    for (int port = 2; port <= 4; port++)
                    {
                        string p = "REV18_IOL_Port_" + port;
                        SetTextRequired(screen, p + "_Device", "SPARE");
                        SetTextRequired(screen, p + "_Description", "Spare port");
                        SetTextRequired(screen, p + "_Mode", "SIO DI");
                        SetTextRequired(screen, p + "_Unit", "—");
                        SetTextRequired(screen, p + "_Quality", "SPARE / NOT USED");
                        SetTextRequired(screen, p + "_Mapping", "NO PROCESS ASSIGNMENT");
                    }
                    SetTextRequired(screen, "REV18_IOL_Port_5_Device", "LR2050");
                    SetTextRequired(screen, "REV18_IOL_Port_5_Description", "Filler tank product level");
                    SetTextRequired(screen, "REV18_IOL_Port_5_Mode", "IO-Link 2I");
                    SetTextRequired(screen, "REV18_IOL_Port_5_Unit", "mm");
                    SetTextRequired(screen, "REV18_IOL_Port_5_Quality", "CONFIGURED");
                    SetTextRequired(screen, "REV18_IOL_Port_5_Mapping", "%IW364\nIOL_AL102_P5_RAW");
                    HmiText x5Live = FindText(screen, new[] {
                        "REV70_IOL_AL102_X5_Text", "REV71_IOL_AL102_P5_Text",
                        "REV36_IOL_AL102_Port_5_Live_Text", "REV18_IOL_Port_5_LiveText" });
                    if (x5Live == null) throw new InvalidOperationException("AL102 X5 live text was not found.");
                    ReplaceScript(x5Live, "Text",
                        "let t=HMIRuntime.Tags(\"IOL_AL102_P5_RAW\");let v=Number(t.Read())&65535;"+
                        "let q=Number(t.QualityCode);if(t.LastError!==0||(q&192)<128){return \"NO DATA\";}"+
                        "let mm=(v>>2)&16383;if(mm===8188||mm===8189||mm===8190){return \"DIAG \"+String(mm);}"+
                        "return String(mm);");
                    HmiText note = screen.ScreenItems.Find("REV18_IOL_Detail_HoldPoint") as HmiText;
                    if (note != null) SetText(note.Text, "AL102 X1 PN7094 vacuum; X2-X4 spare SIO DI; X5 LR2050 filler-tank product level.");

                    project.Save();
                    Compile("PLC", FindSoftwareItem(project, true).GetService<ICompilable>());
                    project.Save();
                    Console.WriteLine("HMI_COMPILE=SEPARATE_DEVICE_COMPILE_REQUIRED");
                    Console.WriteLine("DOWNLOAD=NO");
                    Console.WriteLine("STATUS=PASS");
                }
                return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine("STATUS=FAIL"); Console.Error.WriteLine(ex); return 1; }
        }

        private static DeviceItem FindPortDevice(Device d, DeviceItem module, int port)
        {
            return FindItems(d.DeviceItems).FirstOrDefault(i => !Object.ReferenceEquals(i, module) &&
                ((i.TypeIdentifier ?? "").IndexOf("/SM/IDS DI", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 (i.TypeIdentifier ?? "").IndexOf("/SM/IDS IO-Link", StringComparison.OrdinalIgnoreCase) >= 0) &&
                i.PositionNumber == port);
        }
        private static void Compile(string name, ICompilable c) { CompilerResult r=c.Compile(); Console.WriteLine("COMPILE="+name+";ERRORS="+r.ErrorCount+";WARNINGS="+r.WarningCount); if(r.ErrorCount!=0)throw new InvalidOperationException(name+" compile failed"); }
        private static void SetTextRequired(HmiScreen s,string n,string v){HmiText x=s.ScreenItems.Find(n) as HmiText;if(x==null)throw new InvalidOperationException("Missing HMI object: "+n);SetText(x.Text,v);}
        private static HmiText FindText(HmiScreen s,IEnumerable<string> names){foreach(string n in names){HmiText x=s.ScreenItems.Find(n) as HmiText;if(x!=null)return x;}return null;}
        private static void ReplaceScript(HmiScreenItemBase item,string prop,string code){DynamizationBase old=item.Dynamizations.Find(prop);if(old!=null)old.Delete();ScriptDynamization d=item.Dynamizations.Create<ScriptDynamization>(prop);d.Async=false;d.Trigger.Type=TriggerType.T500ms;d.ScriptCode=code;}
        private static void SetText(MultilingualText text,string value){string rich="<body><p>"+value.Replace("&","&amp;").Replace("<","&lt;").Replace(">","&gt;")+"</p></body>";foreach(MultilingualTextItem i in text.Items)i.Text=rich;}
        private static HmiSoftware FindHmi(Project p){DeviceItem i=FindSoftwareItem(p,false);SoftwareContainer c=i==null?null:i.GetService<SoftwareContainer>();return c==null?null:c.Software as HmiSoftware;}
        private static DeviceItem FindSoftwareItem(Project p,bool plc){foreach(Device d in AllDevices(p)){DeviceItem x=FindSoftwareItem(d.DeviceItems,plc);if(x!=null)return x;}return null;}
        private static DeviceItem FindSoftwareItem(DeviceItemComposition items,bool plc){foreach(DeviceItem i in items){SoftwareContainer c=i.GetService<SoftwareContainer>();if(c!=null&&(plc?c.Software is PlcSoftware:c.Software is HmiSoftware))return i;DeviceItem x=FindSoftwareItem(i.DeviceItems,plc);if(x!=null)return x;}return null;}
        private static IEnumerable<Device> AllDevices(Project p){HashSet<Device>s=new HashSet<Device>();foreach(Device d in p.Devices)if(s.Add(d))yield return d;foreach(Device d in p.UngroupedDevicesGroup.Devices)if(s.Add(d))yield return d;foreach(DeviceUserGroup g in p.DeviceGroups)foreach(Device d in DevicesInGroup(g))if(s.Add(d))yield return d;}
        private static IEnumerable<Device> DevicesInGroup(DeviceUserGroup g){foreach(Device d in g.Devices)yield return d;foreach(DeviceUserGroup c in g.Groups)foreach(Device d in DevicesInGroup(c))yield return d;}
        private static IEnumerable<DeviceItem> FindItems(DeviceItemComposition items){foreach(DeviceItem i in items){yield return i;foreach(DeviceItem c in FindItems(i.DeviceItems))yield return c;}}
    }
}
