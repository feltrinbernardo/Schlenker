using System;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.HmiUnified.UI.Base;
using Siemens.Engineering.HmiUnified.UI.Controls;
using Siemens.Engineering.HmiUnified.UI.Dynamization;
using Siemens.Engineering.HmiUnified.UI.Dynamization.Script;
using Siemens.Engineering.HmiUnified.UI.Screens;
using Siemens.Engineering.HmiUnified.UI.Widgets;
using Siemens.Engineering.SW;

namespace Schlenker.TiaV19
{
    internal static class InspectHmiProductionFormats
    {
        private static int Main(string[] args)
        {
            string projectPath = Path.GetFullPath(args[0]);
            string reportPath = Path.GetFullPath(args[1]);
            TiaPortalProcess process = TiaPortal.GetProcesses().Single(candidate =>
                candidate.ProjectPath != null && candidate.ProjectPath.FullName.Equals(
                    projectPath, StringComparison.OrdinalIgnoreCase));
            using (TiaPortal portal = process.Attach())
            using (StreamWriter report = new StreamWriter(reportPath, false))
            {
                Project project = portal.Projects.Single();
                HmiSoftware hmi = Find(project);
                HmiScreen screen = hmi.Screens.Find("production");
                foreach (HmiScreenItemBase item in screen.ScreenItems)
                {
                    bool speedRelated = item.Name.IndexOf("speed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        item.Name.IndexOf("vacuum", StringComparison.OrdinalIgnoreCase) >= 0;
                    HmiIOField field = item as HmiIOField;
                    TagDynamization binding = field == null ? null :
                        field.Dynamizations.Find("ProcessValue") as TagDynamization;
                    if (binding != null &&
                        (binding.Tag.IndexOf("speed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         binding.Tag.IndexOf("vacuum", StringComparison.OrdinalIgnoreCase) >= 0))
                        speedRelated = true;
                    string scripts = String.Join(" || ", item.Dynamizations.OfType<ScriptDynamization>()
                        .Select(d => d.ScriptCode));
                    if (scripts.IndexOf("speed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        scripts.IndexOf("toFixed", StringComparison.OrdinalIgnoreCase) >= 0)
                        speedRelated = true;
                    if (!speedRelated) continue;
                    report.WriteLine("Name=" + item.Name);
                    report.WriteLine("Type=" + item.GetType().FullName);
                    report.WriteLine("Visible=" + item.Visible);
                    report.WriteLine("Bounds=" + Read(item, "Left") + "," + Read(item, "Top") + "," +
                        Read(item, "Width") + "," + Read(item, "Height"));
                    if (field != null)
                    {
                        report.WriteLine("Tag=" + (binding == null ? "" : binding.Tag));
                        report.WriteLine("OutputFormat=" + field.OutputFormat);
                    }
                    report.WriteLine("Scripts=" + scripts);
                    report.WriteLine();
                }
            }
            return 0;
        }

        private static string Read(HmiScreenItemBase item, string attribute)
        {
            try { return Convert.ToString(item.GetAttribute(attribute)); }
            catch { return "n/a"; }
        }

        private static HmiSoftware Find(Project project)
        {
            foreach (Device device in project.Devices)
            {
                HmiSoftware hmi = Find(device.DeviceItems);
                if (hmi != null) return hmi;
            }
            throw new InvalidOperationException("Unified HMI not found.");
        }

        private static HmiSoftware Find(DeviceItemComposition items)
        {
            foreach (DeviceItem item in items)
            {
                SoftwareContainer container = item.GetService<SoftwareContainer>();
                HmiSoftware hmi = container == null ? null : container.Software as HmiSoftware;
                if (hmi != null) return hmi;
                hmi = Find(item.DeviceItems);
                if (hmi != null) return hmi;
            }
            return null;
        }
    }
}
