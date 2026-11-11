using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;

namespace Schlenker.TiaV19
{
    internal static class ConfigureFourAl1403Offline
    {
        private const string DeviceType =
            "GSD:GSDML-V2.35-IFM-AL1X0X-20241112-144500.XML/DAP/DIM_AL1403";

        private static readonly Tuple<string, string>[] Targets =
        {
            Tuple.Create("AL100", "192.168.10.30"),
            Tuple.Create("AL101", "192.168.10.31"),
            Tuple.Create("AL102", "192.168.10.32"),
            Tuple.Create("AL103", "192.168.10.33")
        };

        private static int Main(string[] args)
        {
            if (args.Length != 2 || (args[0] != "audit" && args[0] != "apply"))
            {
                Console.Error.WriteLine("Usage: ConfigureFourAl1403Offline.exe audit|apply <exact-ap19-path>");
                return 2;
            }

            string mode = args[0];
            string expected = Path.GetFullPath(args[1]);
            try
            {
                TiaPortalProcess process = TiaPortal.GetProcesses().SingleOrDefault(candidate =>
                    candidate.ProjectPath != null && candidate.ProjectPath.FullName.Equals(
                        expected, StringComparison.OrdinalIgnoreCase));
                if (process == null) throw new InvalidOperationException("Exact working project is not open.");

                using (TiaPortal portal = process.Attach())
                {
                    Project project = portal.Projects.Single();
                    if (!project.Path.FullName.Equals(expected, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Attached project identity changed.");
                    Subnet subnet = FindSubnet(project, "PN/IE_1");
                    if (subnet == null) throw new InvalidOperationException("PN/IE_1 was not found.");

                    Console.WriteLine("PROJECT=" + project.Path.FullName);
                    Console.WriteLine("MODE=" + mode.ToUpperInvariant() + "; ONLINE=NO; DOWNLOAD=NO");
                    List<Device> created = new List<Device>();
                    try
                    {
                        foreach (Tuple<string, string> target in Targets)
                        {
                            string deviceName = target.Item1;
                            string pnName = deviceName.ToLowerInvariant();
                            Device device = AllDevices(project).SingleOrDefault(item =>
                                item.Name.Equals(deviceName, StringComparison.OrdinalIgnoreCase));
                            if (device == null)
                            {
                                if (mode == "audit")
                                {
                                    Console.WriteLine(deviceName + "=MISSING; TARGET_IP=" + target.Item2);
                                    continue;
                                }
                                device = project.Devices.CreateWithItem(DeviceType, pnName, deviceName);
                                created.Add(device);
                            }

                            Node node = FindNodes(device.DeviceItems).Single(item => item.NodeType == NetType.Ethernet);
                            IEngineeringObject attributes = node;
                            if (mode == "apply")
                            {
                                if (node.ConnectedSubnet == null) node.ConnectToSubnet(subnet);
                                attributes.SetAttribute("IpProtocolSelection", IpProtocolSelection.Project);
                                attributes.SetAttribute("Address", target.Item2);
                                attributes.SetAttribute("SubnetMask", "255.255.255.0");
                                attributes.SetAttribute("PnDeviceNameAutoGeneration", false);
                                attributes.SetAttribute("PnDeviceName", pnName);
                                if (!target.Item2.Equals(SafeGet(attributes, "Address") as string,
                                        StringComparison.OrdinalIgnoreCase))
                                    throw new InvalidOperationException(deviceName + " IP validation failed.");
                            }
                            Console.WriteLine(deviceName + "; IP=" + SafeGet(attributes, "Address") +
                                "; PN=" + SafeGet(attributes, "PnDeviceName") +
                                "; SUBNET=" + (node.ConnectedSubnet == null ? "<none>" : node.ConnectedSubnet.Name));
                        }

                        if (mode == "apply")
                        {
                            project.Save();
                            Console.WriteLine("SAVE=YES");
                        }
                        else Console.WriteLine("SAVE=NO");
                    }
                    catch
                    {
                        foreach (Device device in created) { try { device.Delete(); } catch { } }
                        throw;
                    }
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

        private static Subnet FindSubnet(Project project, string name)
        {
            foreach (Device device in AllDevices(project))
                foreach (Node node in FindNodes(device.DeviceItems))
                    if (node.ConnectedSubnet != null && node.ConnectedSubnet.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                        return node.ConnectedSubnet;
            return null;
        }

        private static IEnumerable<Device> AllDevices(Project project)
        {
            HashSet<Device> seen = new HashSet<Device>();
            foreach (Device device in project.Devices) if (seen.Add(device)) yield return device;
            foreach (Device device in project.UngroupedDevicesGroup.Devices) if (seen.Add(device)) yield return device;
            foreach (DeviceUserGroup group in project.DeviceGroups)
                foreach (Device device in DevicesInGroup(group)) if (seen.Add(device)) yield return device;
        }

        private static IEnumerable<Device> DevicesInGroup(DeviceUserGroup group)
        {
            foreach (Device device in group.Devices) yield return device;
            foreach (DeviceUserGroup child in group.Groups)
                foreach (Device device in DevicesInGroup(child)) yield return device;
        }

        private static IEnumerable<Node> FindNodes(DeviceItemComposition items)
        {
            foreach (DeviceItem item in items)
            {
                NetworkInterface network = item.GetService<NetworkInterface>();
                if (network != null) foreach (Node node in network.Nodes) yield return node;
                foreach (Node child in FindNodes(item.DeviceItems)) yield return child;
            }
        }

        private static object SafeGet(IEngineeringObject obj, string name)
        {
            try { return obj.GetAttribute(name) ?? "<null>"; }
            catch { return "<unavailable>"; }
        }
    }
}
