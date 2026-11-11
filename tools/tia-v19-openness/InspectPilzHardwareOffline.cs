using System;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;

namespace Schlenker.TiaV19
{
    internal static class InspectPilzHardwareOffline
    {
        private static int Main(string[] args)
        {
            if (args.Length != 2) return 2;
            string expected = Path.GetFullPath(args[0]);
            string reportPath = Path.GetFullPath(args[1]);
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            try
            {
                TiaPortalProcess process = TiaPortal.GetProcesses().SingleOrDefault(candidate =>
                    candidate.ProjectPath != null &&
                    candidate.ProjectPath.FullName.Equals(expected, StringComparison.OrdinalIgnoreCase));
                if (process == null) throw new InvalidOperationException("Exact project is not open.");
                using (StreamWriter report = new StreamWriter(reportPath, false))
                using (TiaPortal portal = process.Attach())
                {
                    Project project = portal.Projects.Single();
                    report.WriteLine("MODE=ATTACHED_READ_ONLY");
                    report.WriteLine("PROJECT=" + project.Path.FullName);
                    foreach (Device device in project.Devices)
                        Inspect(device.Name, device.DeviceItems, report, "");
                    foreach (Device device in project.UngroupedDevicesGroup.Devices)
                        Inspect(device.Name, device.DeviceItems, report, "");
                    report.WriteLine("STATUS=PASS");
                }
                return 0;
            }
            catch (Exception exception)
            {
                File.AppendAllText(reportPath, Environment.NewLine + "STATUS=FAIL" +
                    Environment.NewLine + exception + Environment.NewLine);
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static void Inspect(string deviceName, DeviceItemComposition items,
            StreamWriter report, string indent)
        {
            foreach (DeviceItem item in items)
            {
                bool relevant =
                    deviceName.IndexOf("mes-profinet", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.Name.IndexOf("mes-profinet", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.Name.IndexOf("PNOZ", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.TypeIdentifier.IndexOf("MES-PROFINET", StringComparison.OrdinalIgnoreCase) >= 0;
                if (relevant)
                {
                    report.WriteLine(indent + "ITEM\tDevice=" + deviceName +
                        "\tName=" + item.Name + "\tType=" + item.TypeIdentifier +
                        "\tPosition=" + item.PositionNumber);
                    foreach (HwIdentifier identifier in item.HwIdentifiers)
                        report.WriteLine(indent + "  HW_ID\t" + identifier.Identifier);
                    foreach (Address address in item.Addresses)
                        report.WriteLine(indent + "  ADDRESS\t" + address.IoType +
                            "\tStart=" + address.StartAddress + "\tLength=" + address.Length);
                }
                Inspect(deviceName, item.DeviceItems, report, indent + "  ");
            }
        }
    }
}
