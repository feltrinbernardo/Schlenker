using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;

namespace Schlenker.TiaV19
{
    internal static class AssignAl1403IoSystemOffline
    {
        private static readonly string[] Names = { "AL100", "AL101", "AL102", "AL103" };
        private static int Main(string[] args)
        {
            if (args.Length != 1) return 2;
            string expected = Path.GetFullPath(args[0]);
            try
            {
                TiaPortalProcess process = TiaPortal.GetProcesses().FirstOrDefault(p =>
                    p.ProjectPath != null && p.ProjectPath.FullName.Equals(expected, StringComparison.OrdinalIgnoreCase));
                if (process == null) throw new InvalidOperationException("Exact working project is not open.");
                using (TiaPortal portal = process.Attach())
                {
                    Project project = portal.Projects.First();
                    List<Device> devices = AllDevices(project).ToList();
                    Device plcStation = devices.First(d => d.Name.Equals(
                        "S7-1500/ET200MP station_1", StringComparison.OrdinalIgnoreCase));
                    NetworkInterface controllerNetwork = FindNetworks(plcStation.DeviceItems)
                        .First(n => n.IoControllers.Count > 0 && n.Nodes.Any(node =>
                            node.ConnectedSubnet != null && node.ConnectedSubnet.Name.Equals(
                                "PN/IE_1", StringComparison.OrdinalIgnoreCase)));
                    IoController controller = controllerNetwork.IoControllers.First();
                    IoSystem ioSystem = controller.IoSystem ?? controller.CreateIoSystem("PROFINET IO-System_1");
                    if (ioSystem == null) throw new InvalidOperationException("PROFINET IO system could not be created.");
                    foreach (string name in Names)
                    {
                        Device device = devices.First(d => d.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                        NetworkInterface network = FindNetworks(device.DeviceItems).First(n =>
                            n.InterfaceOperatingMode.ToString().Equals("IoDevice", StringComparison.OrdinalIgnoreCase));
                        IoConnector connector = network.IoConnectors.First();
                        if (connector.ConnectedToIoSystem == null) connector.ConnectToIoSystem(ioSystem);
                        if (connector.ConnectedToIoSystem == null ||
                            !connector.ConnectedToIoSystem.Name.Equals(ioSystem.Name, StringComparison.OrdinalIgnoreCase))
                            throw new InvalidOperationException(name + " IO-system assignment failed.");
                        Console.WriteLine(name + "=" + connector.ConnectedToIoSystem.Name);
                    }
                    project.Save();
                    Console.WriteLine("PROJECT=" + project.Path.FullName);
                    Console.WriteLine("SAVE=YES; ONLINE=NO; DOWNLOAD=NO; STATUS=PASS");
                }
                return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine("STATUS=FAIL"); Console.Error.WriteLine(ex); return 1; }
        }
        private static IEnumerable<NetworkInterface> FindNetworks(DeviceItemComposition items)
        {
            foreach (DeviceItem item in items)
            {
                NetworkInterface network = item.GetService<NetworkInterface>();
                if (network != null) yield return network;
                foreach (NetworkInterface child in FindNetworks(item.DeviceItems)) yield return child;
            }
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
    }
}
