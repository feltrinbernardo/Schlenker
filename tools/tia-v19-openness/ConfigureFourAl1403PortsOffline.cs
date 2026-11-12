using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;

namespace Schlenker.TiaV19
{
    internal static class ConfigureFourAl1403PortsOffline
    {
        private const string Prefix = "GSD:GSDML-V2.35-IFM-AL1X0X-20241112-144500.XML/SM/";

        private sealed class PortPlan
        {
            internal string Master; internal int Port; internal string Type; internal string Name;
            internal PortPlan(string master, int port, string type, string name)
            { Master = master; Port = port; Type = type; Name = name; }
        }

        private static readonly PortPlan[] Plans =
        {
            new PortPlan("AL100",1,"IDS DI","X1_BottleShortage1_O6P301"),
            new PortPlan("AL100",2,"IDS DI","X2_BottleShortage2_O6P301"),
            new PortPlan("AL100",3,"IDS DI","X3_OutfeedAccumulation1_O6P301"),
            new PortPlan("AL100",4,"IDS DI","X4_OutfeedAccumulation2_O6P301"),
            new PortPlan("AL100",5,"IDS DI","X5_CapperCylinder_IFS240"),
            // Operator reassignment 2026-11-12: single-bottle detection at infeed star.
            // Sensor model remains unconfirmed; do not inherit the former IFS240 model.
            new PortPlan("AL100",6,"IDS DI","X6_BottleInfeed"),
            new PortPlan("AL100",7,"IDS DI","X7_CapperMaximumHeight_IFS240"),
            new PortPlan("AL100",8,"IDS DI","X8_CapperMinimumHeight_IFS240"),

            new PortPlan("AL101",1,"IDS DI","X1_CapPresentPickPlace_O6P301"),
            new PortPlan("AL101",2,"IDS DI","X2_CapHopperLow_O6P301"),
            new PortPlan("AL101",3,"IDS DI","X3_CapChannelRequest_O6P301"),
            new PortPlan("AL101",4,"IDS DI","X4_CapChannelEmpty_O6P301"),
            new PortPlan("AL101",5,"IDS IO-Link 2I","X5_MachineAirPressure_PN7094"),
            new PortPlan("AL101",6,"IDS DI","X6_MachineZero_IFS240"),
            new PortPlan("AL101",7,"IDS DI","X7_FillerCylinder_IFS240"),
            new PortPlan("AL101",8,"IDS DI","X8_FillerAntiRotation_IFS240"),

            new PortPlan("AL102",1,"IDS IO-Link 2I","X1_ProductPressure_PN7094"),
            new PortPlan("AL102",2,"IDS IO-Link 2I","X2_AirPressure_PN7094"),
            new PortPlan("AL102",3,"IDS IO-Link 2I","X3_CIPPressure_PN7094"),
            new PortPlan("AL102",5,"IDS IO-Link 2I","X5_ProductLevel_LR2050"),
            new PortPlan("AL102",6,"IDS IO-Link 2I","X6_CIPLevel_LR2050"),
            new PortPlan("AL102",7,"IDS IO-Link 2I","X7_ProcessTemperature_TA2405"),
            new PortPlan("AL102",8,"IDS IO-Link 8I_8O","X8_StackLightHub_AL2330"),

            new PortPlan("AL103",1,"IDS IO-Link 2I","X1_ProductPresence_LMT100"),
            new PortPlan("AL103",2,"IDS DI","X2_FillerMaximumHeight_IFS240"),
            new PortPlan("AL103",3,"IDS DI","X3_FillerMinimumHeight_IFS240"),
            new PortPlan("AL103",4,"IDS DI","X4_DummyBottleWashing_IFS240")
        };

        private static int Main(string[] args)
        {
            if (args.Length != 2 || (args[0] != "audit" && args[0] != "apply")) return 2;
            string mode = args[0]; string expected = Path.GetFullPath(args[1]);
            try
            {
                TiaPortalProcess process = TiaPortal.GetProcesses().SingleOrDefault(p =>
                    p.ProjectPath != null && p.ProjectPath.FullName.Equals(expected, StringComparison.OrdinalIgnoreCase));
                if (process == null) throw new InvalidOperationException("Exact working project is not open.");
                using (TiaPortal portal = process.Attach())
                {
                    Project project = portal.Projects.Single();
                    Console.WriteLine("PROJECT=" + project.Path.FullName);
                    Console.WriteLine("MODE=" + mode.ToUpperInvariant() + "; ONLINE=NO; DOWNLOAD=NO");
                    List<DeviceItem> created = new List<DeviceItem>();
                    try
                    {
                        foreach (IGrouping<string, PortPlan> group in Plans.GroupBy(p => p.Master))
                        {
                            Device device = AllDevices(project).Single(d => d.Name.Equals(group.Key, StringComparison.OrdinalIgnoreCase));
                            DeviceItem module = FindItems(device.DeviceItems).Single(i =>
                                (i.TypeIdentifier ?? "").IndexOf("IOLM_MODULE_8P_ENMO", StringComparison.OrdinalIgnoreCase) >= 0);
                            foreach (PortPlan plan in group)
                            {
                                string type = Prefix + plan.Type;
                                DeviceItem existing = FindItems(device.DeviceItems).FirstOrDefault(i =>
                                    !Object.ReferenceEquals(i, module) &&
                                    ((i.TypeIdentifier ?? "").IndexOf("/SM/IDS DI", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     (i.TypeIdentifier ?? "").IndexOf("/SM/IDS IO-Link", StringComparison.OrdinalIgnoreCase) >= 0) &&
                                    i.PositionNumber == plan.Port);
                                if (existing != null)
                                {
                                    if (!(existing.TypeIdentifier ?? "").Equals(type, StringComparison.OrdinalIgnoreCase))
                                        throw new InvalidOperationException(plan.Master + "/X" + plan.Port +
                                            " occupied by unexpected type " + existing.TypeIdentifier);
                                    string addresses = String.Join(",", existing.Addresses.Select(address =>
                                        address.IoType + ":" + address.StartAddress + ":" + address.Length));
                                    Console.WriteLine(plan.Master + "/X" + plan.Port + "=EXISTING; TYPE=" + plan.Type +
                                        "; NAME=" + existing.Name + "; ADDRESSES=" + addresses);
                                    continue;
                                }
                                bool canPlug = module.CanPlugNew(type, plan.Name, plan.Port);
                                Console.WriteLine(plan.Master + "/X" + plan.Port + "=MISSING; TYPE=" + plan.Type + "; CAN_PLUG=" + canPlug);
                                if (!canPlug) throw new InvalidOperationException("Cannot configure " + plan.Master + "/X" + plan.Port);
                                if (mode == "apply") created.Add(module.PlugNew(type, plan.Name, plan.Port));
                            }
                        }
                        if (mode == "apply") { project.Save(); Console.WriteLine("SAVE=YES"); }
                        else Console.WriteLine("SAVE=NO");
                    }
                    catch
                    {
                        foreach (DeviceItem item in created.AsEnumerable().Reverse()) { try { item.Delete(); } catch { } }
                        throw;
                    }
                }
                Console.WriteLine("STATUS=PASS"); return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine("STATUS=FAIL"); Console.Error.WriteLine(ex); return 1; }
        }

        private static IEnumerable<Device> AllDevices(Project project)
        {
            HashSet<Device> seen = new HashSet<Device>();
            foreach (Device d in project.Devices) if (seen.Add(d)) yield return d;
            foreach (Device d in project.UngroupedDevicesGroup.Devices) if (seen.Add(d)) yield return d;
            foreach (DeviceUserGroup g in project.DeviceGroups)
                foreach (Device d in DevicesInGroup(g)) if (seen.Add(d)) yield return d;
        }
        private static IEnumerable<Device> DevicesInGroup(DeviceUserGroup group)
        {
            foreach (Device d in group.Devices) yield return d;
            foreach (DeviceUserGroup child in group.Groups)
                foreach (Device d in DevicesInGroup(child)) yield return d;
        }
        private static IEnumerable<DeviceItem> FindItems(DeviceItemComposition items)
        {
            foreach (DeviceItem item in items)
            {
                yield return item;
                foreach (DeviceItem child in FindItems(item.DeviceItems)) yield return child;
            }
        }
    }
}
