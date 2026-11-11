using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.HmiUnified.UI.Shapes;
using Siemens.Engineering.HmiUnified.UI.Screens;
using Siemens.Engineering.HmiUnified.UI.Widgets;

namespace Schlenker.TiaV19
{
    internal static class AuditNavigationActiveMarkers
    {
        private static int Main(string[] args)
        {
            if (args.Length < 2 || args.Length > 3) return 2;
            string projectPath = Path.GetFullPath(args[0]);
            string reportPath = Path.GetFullPath(args[1]);
            string screenFilter = args.Length == 3 ? args[2] : String.Empty;
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            try
            {
                TiaPortalProcess process = TiaPortal.GetProcesses().Single(candidate =>
                    candidate.ProjectPath != null &&
                    Path.GetFullPath(candidate.ProjectPath.FullName).Equals(
                        projectPath, StringComparison.OrdinalIgnoreCase));
                using (TiaPortal portal = process.Attach())
                using (StreamWriter report = new StreamWriter(reportPath, false))
                {
                    Project project = portal.Projects.Single();
                    HmiSoftware hmi = FindHmi(project);
                    int screens = 0;
                    int mismatches = 0;
                    foreach (HmiScreen screen in hmi.Screens)
                    {
                        if (!String.IsNullOrEmpty(screenFilter) &&
                            !screen.Name.Equals(screenFilter, StringComparison.OrdinalIgnoreCase))
                            continue;
                        Console.WriteLine("CHECKING=" + screen.Name);
                        HmiRectangle bar = screen.ScreenItems.Find("REF05_NavActiveBar") as HmiRectangle;
                        if (bar == null) continue;
                        HmiButton[] activeButtons = screen.ScreenItems.OfType<HmiButton>()
                            .Where(button => button.Name.StartsWith(
                                "REV13_Nav_Button_", StringComparison.OrdinalIgnoreCase) &&
                                button.Visible && !button.Enabled).ToArray();
                        if (activeButtons.Length != 1)
                            throw new InvalidOperationException(screen.Name +
                                ": expected exactly one visible disabled active navigation button, found " +
                                activeButtons.Length + ".");
                        HmiButton active = activeButtons[0];
                        bool aligned = bar.Top == active.Top;
                        screens++;
                        if (!aligned) mismatches++;
                        report.WriteLine(
                            "SCREEN={0}|ACTIVE={1}|BUTTON={2},{3},{4},{5}|BAR={6},{7},{8},{9}|ALIGNED={10}",
                            screen.Name,
                            active.Name.Substring("REV13_Nav_Button_".Length),
                            active.Left, active.Top, active.Width, active.Height,
                            bar.Left, bar.Top, bar.Width, bar.Height, aligned);
                    }
                    report.WriteLine("SCREENS=" + screens);
                    report.WriteLine("MISMATCHES=" + mismatches);
                    report.WriteLine("STATUS=PASS");
                    Console.WriteLine("SCREENS=" + screens);
                    Console.WriteLine("MISMATCHES=" + mismatches);
                    Console.WriteLine("STATUS=PASS");
                    return 0;
                }
            }
            catch (Exception exception)
            {
                File.AppendAllText(reportPath,
                    Environment.NewLine + "STATUS=FAIL" + Environment.NewLine + exception);
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static HmiSoftware FindHmi(Project project)
        {
            foreach (Device device in project.Devices)
                foreach (DeviceItem item in Flatten(device.DeviceItems))
                {
                    SoftwareContainer container = item.GetService<SoftwareContainer>();
                    HmiSoftware hmi = container == null ? null : container.Software as HmiSoftware;
                    if (hmi != null) return hmi;
                }
            throw new InvalidOperationException("Unified HMI was not found.");
        }

        private static IEnumerable<DeviceItem> Flatten(DeviceItemComposition items)
        {
            foreach (DeviceItem item in items)
            {
                yield return item;
                foreach (DeviceItem child in Flatten(item.DeviceItems)) yield return child;
            }
        }
    }
}
