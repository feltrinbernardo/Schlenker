using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;

namespace Schlenker.TiaV19
{
    internal static class AuditAl1403PortAddresses
    {
        private static int Main(string[] args)
        {
            if (args.Length != 2) return 2;
            string expected = Path.GetFullPath(args[0]);
            string report = Path.GetFullPath(args[1]);
            try
            {
                TiaPortalProcess process = TiaPortal.GetProcesses().Single(p =>
                    p.ProjectPath != null && Path.GetFullPath(p.ProjectPath.FullName)
                        .Equals(expected, StringComparison.OrdinalIgnoreCase));
                List<string> lines = new List<string>();
                using (TiaPortal portal = process.Attach())
                {
                    Project project = portal.Projects.Single();
                    lines.Add("PROJECT=" + project.Path.FullName);
                    lines.Add("MODE=READ_ONLY; SAVE=NO; DOWNLOAD=NO");
                    foreach (string master in new[] { "AL100", "AL101", "AL102", "AL103" })
                    {
                        Device device = AllDevices(project).Single(d =>
                            d.Name.Equals(master, StringComparison.OrdinalIgnoreCase));
                        foreach (DeviceItem item in FindItems(device.DeviceItems)
                            .Where(i => i.PositionNumber >= 1 && i.PositionNumber <= 8 &&
                                ((i.TypeIdentifier ?? "").IndexOf("/SM/IDS DI", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 (i.TypeIdentifier ?? "").IndexOf("/SM/IDS IO-Link", StringComparison.OrdinalIgnoreCase) >= 0))
                            .OrderBy(i => i.PositionNumber))
                        {
                            string addresses = String.Join(",", item.Addresses.Select(a =>
                                a.IoType + ":" + a.StartAddress + ":" + a.Length));
                            lines.Add(master + "/X" + item.PositionNumber + ";NAME=" + item.Name +
                                ";TYPE=" + item.TypeIdentifier + ";ADDR=" + addresses);
                        }
                    }
                    lines.Add("STATUS=PASS");
                }
                Directory.CreateDirectory(Path.GetDirectoryName(report));
                File.WriteAllLines(report, lines);
                foreach (string line in lines) Console.WriteLine(line);
                return 0;
            }
            catch (Exception ex)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(report));
                File.WriteAllText(report, "STATUS=FAIL" + Environment.NewLine + ex);
                Console.Error.WriteLine(ex);
                return 1;
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
