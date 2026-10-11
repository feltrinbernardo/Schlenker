using System;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.HmiUnified.UI.Controls;
using Siemens.Engineering.HmiUnified.UI.Parts;
using Siemens.Engineering.HmiUnified.UI.Screens;
using Siemens.Engineering.SW;

namespace Schlenker.TiaV19
{
    internal static class InspectAlarmControlColumns
    {
        private static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("Usage: InspectAlarmControlColumns <exact-ap19>");
                return 2;
            }

            string projectPath = Path.GetFullPath(args[0]);
            TiaPortalProcess process = TiaPortal.GetProcesses().SingleOrDefault(candidate =>
                candidate.ProjectPath != null &&
                Path.GetFullPath(candidate.ProjectPath.FullName)
                    .Equals(projectPath, StringComparison.OrdinalIgnoreCase));
            if (process == null)
            {
                Console.Error.WriteLine("The exact TIA project is not open.");
                return 1;
            }

            using (TiaPortal portal = process.Attach())
            {
                Project project = portal.Projects.Single();
                HmiSoftware hmi = FindUnifiedHmi(project);
                HmiScreen screen = hmi == null ? null : hmi.Screens.Find("alarms");
                HmiAlarmControl control = screen == null ? null :
                    screen.ScreenItems.Find("REV12_Active_Alarm_Control") as HmiAlarmControl;
                if (control == null)
                {
                    Console.Error.WriteLine("The active alarm control was not found.");
                    return 1;
                }

                Console.WriteLine("Project=" + project.Path.FullName);
                Console.WriteLine("Filter=" + control.Filter);
                int index = 0;
                foreach (HmiAlarmColumnPart column in
                    control.AlarmView.Columns.OfType<HmiAlarmColumnPart>())
                {
                    string header = String.Join(" | ", column.Header.Text.Items
                        .Select(item => item.Language.Culture.Name + "=" + StripRichText(item.Text)));
                    Console.WriteLine(
                        "Index={0}; Name={1}; Block={2}; Visible={3}; Enabled={4}; Width={5}; Header={6}",
                        index++, column.Name, column.AlarmBlock, column.Visible,
                        column.Enabled, column.Width, header);
                }
            }
            return 0;
        }

        private static string StripRichText(string value)
        {
            if (String.IsNullOrEmpty(value)) return String.Empty;
            return value.Replace("<body><p>", String.Empty)
                .Replace("</p></body>", String.Empty)
                .Replace("<p>", String.Empty).Replace("</p>", String.Empty);
        }

        private static HmiSoftware FindUnifiedHmi(Project project)
        {
            foreach (Device device in project.Devices)
            {
                foreach (DeviceItem item in device.DeviceItems)
                {
                    HmiSoftware hmi = FindUnifiedHmi(item);
                    if (hmi != null) return hmi;
                }
            }
            return null;
        }

        private static HmiSoftware FindUnifiedHmi(DeviceItem item)
        {
            SoftwareContainer container = item.GetService<SoftwareContainer>();
            HmiSoftware hmi = container == null ? null : container.Software as HmiSoftware;
            if (hmi != null) return hmi;
            foreach (DeviceItem child in item.DeviceItems)
            {
                hmi = FindUnifiedHmi(child);
                if (hmi != null) return hmi;
            }
            return null;
        }
    }
}
