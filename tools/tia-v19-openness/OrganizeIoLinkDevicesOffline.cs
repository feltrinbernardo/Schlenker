using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;

namespace Schlenker.TiaV19
{
    internal static class OrganizeIoLinkDevicesOffline
    {
        private static int Main(string[] args)
        {
            if (args.Length < 2 || args.Length > 3)
            {
                Console.Error.WriteLine(
                    "Usage: OrganizeIoLinkDevicesOffline <inspect|apply> <exact-ap19-path> [report-path]");
                return 2;
            }

            string mode = args[0].Trim().ToLowerInvariant();
            if (mode != "inspect" && mode != "apply")
            {
                Console.Error.WriteLine("Mode must be inspect or apply.");
                return 2;
            }

            string projectPath = Path.GetFullPath(args[1]);
            string reportPath = args.Length == 3 ? Path.GetFullPath(args[2]) : null;

            try
            {
                TiaPortalProcess process = TiaPortal.GetProcesses().SingleOrDefault(candidate =>
                    candidate.ProjectPath != null && candidate.ProjectPath.FullName.Equals(
                        projectPath, StringComparison.OrdinalIgnoreCase));

                bool attached = process != null;
                using (TiaPortal portal = attached
                    ? process.Attach()
                    : new TiaPortal(TiaPortalMode.WithoutUserInterface))
                {
                    Project project = attached
                        ? portal.Projects.Single()
                        : portal.Projects.Open(new FileInfo(projectPath));

                    List<string> lines = Inspect(project);
                    lines.Insert(0, "PROJECT=" + project.Path.FullName);
                    lines.Insert(1, "MODE=" + mode.ToUpperInvariant() +
                        "; ATTACHED=" + attached +
                        "; ONLINE_WRITE=NO; DOWNLOAD=NO");

                    if (mode == "apply")
                        throw new NotSupportedException(
                            "Apply is intentionally blocked until the exact ungrouped-device inventory " +
                            "and supported relocation method are verified.");

                    lines.Add("STATUS=PASS");
                    foreach (string line in lines) Console.WriteLine(line);
                    if (!String.IsNullOrWhiteSpace(reportPath))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
                        File.WriteAllLines(reportPath, lines);
                    }
                    if (!attached) project.Close();
                }
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("STATUS=FAIL");
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static List<string> Inspect(Project project)
        {
            List<string> lines = new List<string>();
            lines.Add("UNGROUPED_SYSTEM_GROUP=" + project.UngroupedDevicesGroup.Name);
            foreach (Device device in project.UngroupedDevicesGroup.Devices)
            {
                lines.Add("UNGROUPED_DEVICE=" + device.Name + "; TYPE=" + device.TypeIdentifier);
                InspectNetwork(device.DeviceItems, device.Name, lines);
            }

            foreach (Device device in project.Devices)
                lines.Add("ROOT_DEVICE=" + device.Name + "; TYPE=" + device.TypeIdentifier);

            foreach (DeviceUserGroup group in project.DeviceGroups)
                InspectGroup(group, group.Name, lines);

            return lines;
        }

        private static void InspectNetwork(
            DeviceItemComposition items,
            string deviceName,
            List<string> lines)
        {
            foreach (DeviceItem item in items)
            {
                NetworkInterface network = item.GetService<NetworkInterface>();
                if (network != null)
                {
                    foreach (Node node in network.Nodes)
                    {
                        lines.Add("NETWORK_NODE=" + deviceName + "::" + item.Name +
                            "; SUBNET=" + (node.ConnectedSubnet == null
                                ? "<none>"
                                : node.ConnectedSubnet.Name));
                    }
                    foreach (IoConnector connector in network.IoConnectors)
                    {
                        lines.Add("IO_CONNECTOR=" + deviceName + "::" + item.Name +
                            "; IOSYSTEM=" + (connector.ConnectedToIoSystem == null
                                ? "<none>"
                                : connector.ConnectedToIoSystem.Name));
                    }
                }
                InspectNetwork(item.DeviceItems, deviceName, lines);
            }
        }

        private static void InspectGroup(DeviceUserGroup group, string path, List<string> lines)
        {
            lines.Add("USER_GROUP=" + path);
            foreach (Device device in group.Devices)
                lines.Add("GROUP_DEVICE=" + path + "::" + device.Name +
                    "; TYPE=" + device.TypeIdentifier);
            foreach (DeviceUserGroup child in group.Groups)
                InspectGroup(child, path + "/" + child.Name, lines);
        }
    }
}
