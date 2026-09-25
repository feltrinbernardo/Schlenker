using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;

namespace Schlenker.TiaV19
{
    internal static class CleanupDuplicateAl1403Offline
    {
        private static int Main(string[] args)
        {
            if (args.Length != 1) return 2;
            string expected = Path.GetFullPath(args[0]);
            try
            {
                TiaPortalProcess process = TiaPortal.GetProcesses().SingleOrDefault(p =>
                    p.ProjectPath != null && p.ProjectPath.FullName.Equals(expected,
                        StringComparison.OrdinalIgnoreCase));
                if (process == null) throw new InvalidOperationException("Exact project is not open.");
                using (TiaPortal portal = process.Attach())
                {
                    Project project = portal.Projects.Single();
                    Device duplicate = AllDevices(project).Single(d =>
                        d.Name.Equals("GSD device_1", StringComparison.OrdinalIgnoreCase));
                    Node duplicateNode = FindNodes(duplicate.DeviceItems).Single();
                    RequireNode(duplicateNode, "192.168.10.35", "al1403_1", "PN/IE_1");
                    if (!ContainsType(duplicate.DeviceItems, "DIM_AL1403"))
                        throw new InvalidOperationException("Duplicate device is not an IFM AL1403.");

                    Device al103 = AllDevices(project).Single(d =>
                        d.Name.Equals("al103", StringComparison.OrdinalIgnoreCase));
                    DeviceItem stray = al103.DeviceItems.Single(i =>
                        i.Name.Equals("AL1403", StringComparison.OrdinalIgnoreCase));
                    if ((stray.TypeIdentifier ?? "").IndexOf("DIM_AL1403",
                            StringComparison.OrdinalIgnoreCase) < 0)
                        throw new InvalidOperationException("Unexpected al103 stray item type.");
                    Node strayNode = FindNodes(stray.DeviceItems).Concat(
                        NodesOnItem(stray)).Single();
                    RequireNode(strayNode, "192.168.0.1", "al1403", null);

                    stray.Delete();
                    duplicate.Delete();
                    project.Save();
                    Console.WriteLine("REMOVED_DEVICE=GSD device_1; PN=al1403_1; IP=192.168.10.35");
                    Console.WriteLine("REMOVED_STRAY_ITEM=al103/AL1403; PN=al1403; IP=192.168.0.1");
                    Console.WriteLine("PRESERVED=al100,al101,al102,al103,al104,ANYBUS_SMC");
                    Console.WriteLine("STATUS=PASS");
                    return 0;
                }
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("STATUS=FAIL");
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static void RequireNode(Node node, string address, string pnName, string subnet)
        {
            IEngineeringObject attributes = node;
            string actualAddress = Convert.ToString(attributes.GetAttribute("Address"));
            string actualPn = Convert.ToString(attributes.GetAttribute("PnDeviceName"));
            string actualSubnet = node.ConnectedSubnet == null ? null : node.ConnectedSubnet.Name;
            if (!address.Equals(actualAddress, StringComparison.OrdinalIgnoreCase) ||
                !pnName.Equals(actualPn, StringComparison.OrdinalIgnoreCase) ||
                !String.Equals(subnet, actualSubnet, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Duplicate identity guard failed.");
        }

        private static IEnumerable<Device> AllDevices(Project project)
        {
            HashSet<Device> seen = new HashSet<Device>();
            foreach (Device device in project.Devices) if (seen.Add(device)) yield return device;
            foreach (Device device in project.UngroupedDevicesGroup.Devices)
                if (seen.Add(device)) yield return device;
            foreach (DeviceUserGroup group in project.DeviceGroups)
                foreach (Device device in DevicesInGroup(group)) if (seen.Add(device)) yield return device;
        }

        private static IEnumerable<Device> DevicesInGroup(DeviceUserGroup group)
        {
            foreach (Device device in group.Devices) yield return device;
            foreach (DeviceUserGroup child in group.Groups)
                foreach (Device device in DevicesInGroup(child)) yield return device;
        }

        private static bool ContainsType(DeviceItemComposition items, string fragment)
        {
            foreach (DeviceItem item in items)
            {
                if ((item.TypeIdentifier ?? "").IndexOf(fragment,
                        StringComparison.OrdinalIgnoreCase) >= 0 ||
                    ContainsType(item.DeviceItems, fragment)) return true;
            }
            return false;
        }

        private static IEnumerable<Node> NodesOnItem(DeviceItem item)
        {
            NetworkInterface network = item.GetService<NetworkInterface>();
            if (network != null) foreach (Node node in network.Nodes) yield return node;
        }

        private static IEnumerable<Node> FindNodes(DeviceItemComposition items)
        {
            foreach (DeviceItem item in items)
            {
                foreach (Node node in NodesOnItem(item)) yield return node;
                foreach (Node node in FindNodes(item.DeviceItems)) yield return node;
            }
        }
    }
}
