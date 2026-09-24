using System;
using System.Collections.Generic;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;

namespace Schlenker.TiaV19
{
    internal static class ConfigureAl103Offline
    {
        private const string ExpectedProject =
            @"C:\TIA Projects\Schlenkers 36-10 190036-7-8v2.12-MTP1200-DeployWorking-20260918\Backup Schlenkers 36-10 190036-7-8v2.12.ap19";
        private const string Al1403Type =
            "GSD:GSDML-V2.35-IFM-AL1X0X-20241112-144500.XML/DAP/DIM_AL1403";

        private static int Main(string[] args)
        {
            string mode = args.Length == 0 ? "audit" : args[0].Trim().ToLowerInvariant();
            if (mode != "audit" && mode != "apply")
            {
                Console.Error.WriteLine("STATUS=BLOCKED");
                Console.Error.WriteLine("Usage: ConfigureAl103Offline.exe audit|apply");
                return 4;
            }

            try
            {
                TiaPortalProcess selected = TiaPortal.GetProcesses().SingleOrDefault(
                    process => process.ProjectPath != null &&
                    process.ProjectPath.FullName.Equals(ExpectedProject, StringComparison.OrdinalIgnoreCase));
                if (selected == null)
                {
                    Console.Error.WriteLine("STATUS=FAIL");
                    Console.Error.WriteLine("The exact authorized TIA project is not open.");
                    return 2;
                }

                using (TiaPortal portal = selected.Attach())
                {
                    Project project = portal.Projects.SingleOrDefault();
                    if (project == null ||
                        !project.Path.FullName.Equals(ExpectedProject, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Attached project identity changed unexpectedly.");

                    Console.WriteLine("PROJECT=" + project.Path.FullName);
                    if (mode == "apply")
                        Apply(project);
                    else
                        Audit(project);
                }

                Console.WriteLine("STATUS=PASS");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("STATUS=FAIL");
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        private static void Audit(Project project)
        {
            Console.WriteLine("MODE=READ_ONLY; SAVE=NO; ONLINE=NO");
            foreach (Device device in AllDevices(project))
            {
                Console.WriteLine("DEVICE name={0}", device.Name);
                foreach (DeviceItem item in device.DeviceItems)
                    Console.WriteLine("  ROOT_ITEM name={0}; type={1}", item.Name, item.TypeIdentifier ?? "<null>");
                foreach (Node node in FindNodes(device.DeviceItems))
                    Console.WriteLine("  NODE name={0}; address={1}; mask={2}; pn={3}; subnet={4}",
                        node.Name, SafeGet(node, "Address"), SafeGet(node, "SubnetMask"),
                        SafeGet(node, "PnDeviceName"),
                        node.ConnectedSubnet == null ? "<none>" : node.ConnectedSubnet.Name);
            }
        }

        private static void Apply(Project project)
        {
            if (AllDevices(project).Any(device =>
                device.Name.Equals("AL103", StringComparison.OrdinalIgnoreCase) ||
                ContainsNameOrType(device.DeviceItems, "al103", "DIM_AL1403")))
                throw new InvalidOperationException("AL103 or an AL1403 device already exists; refusing duplicate creation.");

            Subnet subnet = FindSubnet(project, "PN/IE_1");
            if (subnet == null)
                throw new InvalidOperationException("Authorized PROFINET subnet PN/IE_1 was not found.");

            Device created = null;
            try
            {
                created = project.Devices.CreateWithItem(Al1403Type, "al103", "AL103");
                List<Node> nodes = FindNodes(created.DeviceItems).ToList();
                if (nodes.Count != 1)
                    throw new InvalidOperationException("Expected exactly one AL1403 Ethernet node, found " + nodes.Count + ".");

                Node node = nodes[0];
                IEngineeringObject attributes = node;
                attributes.SetAttribute("IpProtocolSelection", IpProtocolSelection.Project);
                attributes.SetAttribute("Address", "192.168.10.33");
                attributes.SetAttribute("SubnetMask", "255.255.255.0");
                attributes.SetAttribute("UseRouter", false);
                attributes.SetAttribute("PnDeviceNameAutoGeneration", false);
                attributes.SetAttribute("PnDeviceName", "al103");
                node.ConnectToSubnet(subnet);

                if (!"192.168.10.33".Equals(attributes.GetAttribute("Address") as string,
                        StringComparison.OrdinalIgnoreCase) ||
                    !"255.255.255.0".Equals(attributes.GetAttribute("SubnetMask") as string,
                        StringComparison.OrdinalIgnoreCase) ||
                    !"al103".Equals(attributes.GetAttribute("PnDeviceName") as string,
                        StringComparison.OrdinalIgnoreCase) ||
                    node.ConnectedSubnet == null ||
                    !"PN/IE_1".Equals(node.ConnectedSubnet.Name, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("AL103 validation failed before save.");

                project.Save();
                Console.WriteLine("MODE=OFFLINE_APPLY; SAVE=YES; ONLINE=NO");
                Console.WriteLine("CREATED_DEVICE=AL103");
                Console.WriteLine("TYPE=" + Al1403Type);
                Console.WriteLine("PN_NAME=" + attributes.GetAttribute("PnDeviceName"));
                Console.WriteLine("IP=" + attributes.GetAttribute("Address"));
                Console.WriteLine("MASK=" + attributes.GetAttribute("SubnetMask"));
                Console.WriteLine("SUBNET=" + node.ConnectedSubnet.Name);
                Console.WriteLine("PORT_MAPPING=TBC_NOT_CONFIGURED");
            }
            catch
            {
                if (created != null)
                {
                    try { created.Delete(); }
                    catch { }
                }
                throw;
            }
        }

        private static Subnet FindSubnet(Project project, string name)
        {
            foreach (Device device in AllDevices(project))
                foreach (Node node in FindNodes(device.DeviceItems))
                    if (node.ConnectedSubnet != null &&
                        node.ConnectedSubnet.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                        return node.ConnectedSubnet;
            return null;
        }

        private static IEnumerable<Device> AllDevices(Project project)
        {
            HashSet<Device> seen = new HashSet<Device>();
            foreach (Device device in project.Devices)
                if (seen.Add(device)) yield return device;
            foreach (Device device in project.UngroupedDevicesGroup.Devices)
                if (seen.Add(device)) yield return device;
            foreach (DeviceUserGroup group in project.DeviceGroups)
                foreach (Device device in DevicesInGroup(group))
                    if (seen.Add(device)) yield return device;
        }

        private static IEnumerable<Device> DevicesInGroup(DeviceUserGroup group)
        {
            foreach (Device device in group.Devices)
                yield return device;
            foreach (DeviceUserGroup child in group.Groups)
                foreach (Device device in DevicesInGroup(child))
                    yield return device;
        }

        private static IEnumerable<Node> FindNodes(DeviceItemComposition items)
        {
            foreach (DeviceItem item in items)
            {
                NetworkInterface network = item.GetService<NetworkInterface>();
                if (network != null)
                    foreach (Node node in network.Nodes)
                        if (node.NodeType == NetType.Ethernet)
                            yield return node;
                foreach (Node child in FindNodes(item.DeviceItems))
                    yield return child;
            }
        }

        private static bool ContainsNameOrType(DeviceItemComposition items, string name, string typeFragment)
        {
            if (items == null)
                return false;

            foreach (DeviceItem item in items)
            {
                string itemName = item.Name ?? String.Empty;
                string typeIdentifier = item.TypeIdentifier ?? String.Empty;
                if (itemName.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                    typeIdentifier.IndexOf(typeFragment, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    ContainsNameOrType(item.DeviceItems, name, typeFragment))
                    return true;
            }
            return false;
        }

        private static void InspectItems(DeviceItemComposition items, string indent)
        {
            foreach (DeviceItem item in items)
            {
                Console.WriteLine("{0}ITEM name={1}; type={2}", indent, item.Name, item.TypeIdentifier);
                NetworkInterface network = item.GetService<NetworkInterface>();
                if (network != null)
                {
                    foreach (Node node in network.Nodes)
                    {
                        Console.WriteLine("{0}NODE name={1}; subnet={2}", indent + "  ", node.Name,
                            node.ConnectedSubnet == null ? "<none>" : node.ConnectedSubnet.Name);
                        Console.WriteLine("{0}  ADDRESS={1}; MASK={2}; PN_NAME={3}", indent + "  ",
                            SafeGet(node, "Address"), SafeGet(node, "SubnetMask"), SafeGet(node, "PnDeviceName"));
                    }
                }

                InspectItems(item.DeviceItems, indent + "  ");
            }
        }

        private static object SafeGet(IEngineeringObject obj, string name)
        {
            try { return obj.GetAttribute(name) ?? "<null>"; }
            catch { return "<unavailable>"; }
        }
    }
}
