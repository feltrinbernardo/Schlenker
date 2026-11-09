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
using Siemens.Engineering.SW;

namespace Schlenker.TiaV19
{
    internal static class SetAl103SparePortsSio
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
                    Device al103 = AllDevices(project).Single(d => d.Name.Equals("AL103", StringComparison.OrdinalIgnoreCase));
                    DeviceItem module = FindItems(al103.DeviceItems).Single(i =>
                        (i.TypeIdentifier ?? "").IndexOf("IOLM_MODULE_8P_ENMO", StringComparison.OrdinalIgnoreCase) >= 0);

                    for (int port = 5; port <= 8; port++)
                    {
                        DeviceItem existing = FindPortDevice(al103, module, port);
                        string requiredType = Prefix + "IDS DI";
                        if (existing != null && !(existing.TypeIdentifier ?? "").Equals(requiredType, StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine("AL103/X" + port + "=REMOVE;TYPE=" + existing.TypeIdentifier + ";NAME=" + existing.Name);
                            existing.Delete();
                            existing = null;
                        }
                        if (existing == null)
                        {
                            string name = "X" + port + "_Spare_SIO_DI";
                            if (!module.CanPlugNew(requiredType, name, port))
                                throw new InvalidOperationException("Cannot configure AL103/X" + port + " as SIO DI.");
                            existing = module.PlugNew(requiredType, name, port);
                        }
                        existing.Name = "X" + port + "_Spare_SIO_DI";
                        Console.WriteLine("AL103/X" + port + "=SPARE_SIO_DI;ADDRESSES=" +
                            String.Join(",", existing.Addresses.Select(a => a.IoType + ":" + a.StartAddress + ":" + a.Length)));
                    }

                    Device al102 = AllDevices(project).Single(d => d.Name.Equals("AL102", StringComparison.OrdinalIgnoreCase));
                    DeviceItem module102 = FindItems(al102.DeviceItems).Single(i =>
                        (i.TypeIdentifier ?? "").IndexOf("IOLM_MODULE_8P_ENMO", StringComparison.OrdinalIgnoreCase) >= 0);
                    foreach (int port in new[] { 6, 8 })
                    {
                        DeviceItem existing = FindPortDevice(al102, module102, port);
                        string requiredType = Prefix + "IDS DI";
                        if (existing != null && !(existing.TypeIdentifier ?? "").Equals(requiredType, StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine("AL102/X" + port + "=REMOVE_TEMPORARILY;TYPE=" + existing.TypeIdentifier + ";NAME=" + existing.Name);
                            existing.Delete();
                            existing = null;
                        }
                        if (existing == null)
                        {
                            string name = "X" + port + "_Temporary_Spare_SIO_DI";
                            if (!module102.CanPlugNew(requiredType, name, port))
                                throw new InvalidOperationException("Cannot configure AL102/X" + port + " as temporary SIO DI.");
                            existing = module102.PlugNew(requiredType, name, port);
                        }
                        existing.Name = "X" + port + "_Temporary_Spare_SIO_DI";
                        Console.WriteLine("AL102/X" + port + "=TEMPORARY_SPARE_SIO_DI;ADDRESSES=" +
                            String.Join(",", existing.Addresses.Select(a => a.IoType + ":" + a.StartAddress + ":" + a.Length)));
                    }

                    HmiSoftware hmi = FindHmi(project);
                    if (hmi == null) throw new InvalidOperationException("HMI software was not found.");
                    HmiScreen screen = hmi.Screens.Find("io_link_al103");
                    if (screen == null) throw new InvalidOperationException("io_link_al103 was not found.");
                    for (int port = 5; port <= 8; port++)
                    {
                        string p = "REV18_IOL_Port_" + port;
                        SetTextRequired(screen, p + "_Device", "SPARE");
                        SetTextRequired(screen, p + "_Description", "Spare port");
                        SetTextRequired(screen, p + "_Mode", "SIO DI");
                        SetTextRequired(screen, p + "_Unit", "—");
                        SetTextRequired(screen, p + "_Quality", "SPARE / NOT USED");
                        SetTextRequired(screen, p + "_Mapping", "NO PROCESS ASSIGNMENT");
                    }

                    HmiScreen screen102 = hmi.Screens.Find("io_link_al102");
                    if (screen102 == null) throw new InvalidOperationException("io_link_al102 was not found.");
                    SetTemporarySpareRow(screen102, 6, "LR2050 pending installation");
                    SetTemporarySpareRow(screen102, 8, "AL2330 pending delivery");

                    project.Save();
                    Compile("PLC", FindSoftwareItem(project, true).GetService<ICompilable>());
                    project.Save();
                    Console.WriteLine("UNCHANGED=AL103/X1-X4");
                    Console.WriteLine("UNCHANGED=AL102/X1-X5,X7");
                    Console.WriteLine("EXPECTED_LED_STATE=AL103_X5_X8_OFF;AL102_X6_X8_OFF_WHEN_NO_INPUT_CONNECTED");
                    Console.WriteLine("PLC_HARDWARE_DOWNLOAD_REQUIRED=YES");
                    Console.WriteLine("STATUS=PASS");
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("STATUS=FAIL");
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        private static DeviceItem FindPortDevice(Device d, DeviceItem module, int port)
        {
            return FindItems(d.DeviceItems).FirstOrDefault(i => !Object.ReferenceEquals(i, module) &&
                ((i.TypeIdentifier ?? "").IndexOf("/SM/IDS DI", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 (i.TypeIdentifier ?? "").IndexOf("/SM/IDS IO-Link", StringComparison.OrdinalIgnoreCase) >= 0) &&
                i.PositionNumber == port);
        }
        private static void Compile(string name, ICompilable c) { CompilerResult r=c.Compile(); Console.WriteLine("COMPILE="+name+";ERRORS="+r.ErrorCount+";WARNINGS="+r.WarningCount); if(r.ErrorCount!=0)throw new InvalidOperationException(name+" compile failed"); }
        private static void SetTemporarySpareRow(HmiScreen s,int port,string description){string p="REV18_IOL_Port_"+port;SetTextRequired(s,p+"_Device","SPARE");SetTextRequired(s,p+"_Description",description);SetTextRequired(s,p+"_Mode","SIO DI");SetTextRequired(s,p+"_Unit","—");SetTextRequired(s,p+"_Quality","TEMPORARY SPARE / NOT USED");SetTextRequired(s,p+"_Mapping","DEVICE PENDING / NO ACTIVE PROCESS DATA");}
        private static void SetTextRequired(HmiScreen s,string n,string v){HmiText x=s.ScreenItems.Find(n) as HmiText;if(x==null)throw new InvalidOperationException("Missing HMI object: "+n);SetText(x.Text,v);}
        private static void SetText(MultilingualText text,string value){string rich="<body><p>"+value.Replace("&","&amp;").Replace("<","&lt;").Replace(">","&gt;")+"</p></body>";foreach(MultilingualTextItem i in text.Items)i.Text=rich;}
        private static HmiSoftware FindHmi(Project p){DeviceItem i=FindSoftwareItem(p,false);SoftwareContainer c=i==null?null:i.GetService<SoftwareContainer>();return c==null?null:c.Software as HmiSoftware;}
        private static DeviceItem FindSoftwareItem(Project p,bool plc){foreach(Device d in AllDevices(p)){DeviceItem x=FindSoftwareItem(d.DeviceItems,plc);if(x!=null)return x;}return null;}
        private static DeviceItem FindSoftwareItem(DeviceItemComposition items,bool plc){foreach(DeviceItem i in items){SoftwareContainer c=i.GetService<SoftwareContainer>();if(c!=null&&(plc?c.Software is PlcSoftware:c.Software is HmiSoftware))return i;DeviceItem x=FindSoftwareItem(i.DeviceItems,plc);if(x!=null)return x;}return null;}
        private static IEnumerable<Device> AllDevices(Project p){HashSet<Device>s=new HashSet<Device>();foreach(Device d in p.Devices)if(s.Add(d))yield return d;foreach(Device d in p.UngroupedDevicesGroup.Devices)if(s.Add(d))yield return d;foreach(DeviceUserGroup g in p.DeviceGroups)foreach(Device d in DevicesInGroup(g))if(s.Add(d))yield return d;}
        private static IEnumerable<Device> DevicesInGroup(DeviceUserGroup g){foreach(Device d in g.Devices)yield return d;foreach(DeviceUserGroup c in g.Groups)foreach(Device d in DevicesInGroup(c))yield return d;}
        private static IEnumerable<DeviceItem> FindItems(DeviceItemComposition items){foreach(DeviceItem i in items){yield return i;foreach(DeviceItem c in FindItems(i.DeviceItems))yield return c;}}
    }
}
