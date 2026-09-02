using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.HmiUnified.UI.Base;
using Siemens.Engineering.HmiUnified.UI.Screens;
using Siemens.Engineering.SW;

namespace Schlenker.TiaV19
{
    internal static class Rev25FrameAudit
    {
        private static readonly string[] GeometryProperties =
        {
            "Left", "Top", "Width", "Height",
            "CenterX", "CenterY", "RadiusX", "RadiusY",
            "X1", "Y1", "X2", "Y2"
        };

        private static readonly string[] VisualProperties =
        {
            "Visible", "BackColor", "ForeColor", "BorderColor", "BorderWidth",
            "FontSize", "FontWeight", "HorizontalAlignment", "VerticalAlignment", "Graphic",
            "LineColor", "LineWidth"
        };

        private sealed class FrameSpec
        {
            public string Name;
            public string Type;
            public Dictionary<string, string> Properties;
        }

        private static int Main(string[] args)
        {
            string projectPath = args.Length > 0 ? Path.GetFullPath(args[0]) : String.Empty;
            string outputPath = args.Length > 1 ? args[1] : "REV25_completion_ledger.tsv";
            string targetScreen = args.Length > 2 ? args[2] : null;

            try
            {
                List<TiaPortalProcess> matches = TiaPortal.GetProcesses()
                    .Where(candidate => candidate.ProjectPath != null &&
                        Path.GetFullPath(candidate.ProjectPath.FullName)
                            .Equals(projectPath, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (matches.Count != 1)
                {
                    throw new InvalidOperationException(String.Format(
                        "Expected one open TIA project at '{0}', found {1}.", projectPath, matches.Count));
                }

                using (TiaPortal portal = matches[0].Attach())
                {
                    Project project = portal.Projects.FirstOrDefault();
                    if (project == null) throw new InvalidOperationException("No open project.");
                    HmiSoftware hmi = FindHmi(project);
                    if (hmi == null) throw new InvalidOperationException("No Unified HMI found.");

                    HmiScreen production = hmi.Screens.Find("production");
                    if (production == null) throw new InvalidOperationException("Production master screen is missing.");
                    Dictionary<string, FrameSpec> master = CaptureMaster(production);
                    if (master.Count == 0) throw new InvalidOperationException("Production master frame is empty.");

                    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath)));
                    using (StreamWriter writer = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8))
                    {
                        writer.WriteLine("Screen\tStatus\tIssueCount\tIssues");
                        List<HmiScreen> auditScreens = hmi.Screens.OrderBy(value => value.Name).ToList();
                        if (!String.IsNullOrWhiteSpace(targetScreen))
                        {
                            auditScreens = auditScreens.Where(screen =>
                                screen.Name.Equals(targetScreen, StringComparison.OrdinalIgnoreCase)).ToList();
                            if (auditScreens.Count != 1)
                            {
                                throw new InvalidOperationException("Target screen was not found: " + targetScreen);
                            }
                        }

                        foreach (HmiScreen screen in auditScreens)
                        {
                            List<string> issues = screen.Name.Equals("production", StringComparison.OrdinalIgnoreCase)
                                ? new List<string>()
                                : Audit(screen, master);
                            string status = screen.Name.Equals("production", StringComparison.OrdinalIgnoreCase)
                                ? "FROZEN MASTER"
                                : issues.Count == 0 ? "VALIDATED" : "NOT STARTED";
                            writer.WriteLine(screen.Name + "\t" + status + "\t" + issues.Count + "\t" +
                                Clean(String.Join(" | ", issues)));
                        }
                    }

                    Console.WriteLine("PROJECT=" + project.Path.FullName);
                    Console.WriteLine("MASTER=production");
                    Console.WriteLine("MASTER_OBJECTS=" + master.Count);
                    Console.WriteLine("TARGET=" + (String.IsNullOrWhiteSpace(targetScreen) ? "ALL" : targetScreen));
                    Console.WriteLine("LEDGER=" + Path.GetFullPath(outputPath));
                    Console.WriteLine("SCREENS=" + hmi.Screens.Count);
                }

                Console.WriteLine("STATUS=PASS");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("STATUS=FAIL");
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static Dictionary<string, FrameSpec> CaptureMaster(HmiScreen production)
        {
            Dictionary<string, FrameSpec> result = new Dictionary<string, FrameSpec>(StringComparer.OrdinalIgnoreCase);
            foreach (HmiScreenItemBase item in production.ScreenItems.ToList().Where(IsMasterObject))
            {
                FrameSpec spec = new FrameSpec
                {
                    Name = item.Name,
                    Type = item.GetType().Name,
                    Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                };

                foreach (string propertyName in GeometryProperties.Concat(VisualProperties))
                {
                    string value;
                    if (TryRead(item, propertyName, out value)) spec.Properties[propertyName] = value;
                }

                result.Add(spec.Name, spec);
                Console.WriteLine("MASTER_OBJECT=" + spec.Name + "|TYPE=" + spec.Type + "|" +
                    String.Join(";", spec.Properties.Select(pair => pair.Key + "=" + pair.Value)));
            }
            return result;
        }

        private static bool IsMasterObject(HmiScreenItemBase item)
        {
            string name = item.Name;
            return name.Equals("REV12_Production_User_Header", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("REV12_Mimic_Vacuum_Label_1", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("Line_1", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("Line_2", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("REV13_Common_", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("REV13_Nav_", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("REV14_Nav_", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("REV21_Common_", StringComparison.OrdinalIgnoreCase);
        }

        private static List<string> Audit(HmiScreen screen, IDictionary<string, FrameSpec> master)
        {
            List<string> issues = new List<string>();
            Dictionary<string, HmiScreenItemBase> items = screen.ScreenItems.ToList()
                .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

            foreach (FrameSpec expected in master.Values.OrderBy(value => value.Name))
            {
                HmiScreenItemBase actual;
                if (!items.TryGetValue(expected.Name, out actual))
                {
                    issues.Add(expected.Name + " missing");
                    continue;
                }
                if (!actual.GetType().Name.Equals(expected.Type, StringComparison.Ordinal))
                {
                    issues.Add(expected.Name + " type " + actual.GetType().Name + " expected " + expected.Type);
                    continue;
                }

                bool navigationButton = expected.Name.StartsWith("REV13_Nav_Button_", StringComparison.OrdinalIgnoreCase);
                foreach (KeyValuePair<string, string> pair in expected.Properties)
                {
                    // Each page has one active button, so Enabled and BackColor are page-specific.
                    if (navigationButton &&
                        (pair.Key.Equals("BackColor", StringComparison.OrdinalIgnoreCase) ||
                         pair.Key.Equals("Enabled", StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    string value;
                    if (!TryRead(actual, pair.Key, out value))
                    {
                        issues.Add(expected.Name + "." + pair.Key + " unavailable");
                    }
                    else if (!value.Equals(pair.Value, StringComparison.Ordinal))
                    {
                        issues.Add(expected.Name + "." + pair.Key + "=" + value + " expected " + pair.Value);
                    }
                }
            }

            HmiScreenItemBase page;
            if (items.TryGetValue("REV13_Common_Page", out page))
            {
                string value;
                string[] names = { "Left", "Top", "Width", "Height" };
                string[] expectedValues = { "1010", "10", "335", "34" };
                for (int index = 0; index < names.Length; index++)
                {
                    if (!TryRead(page, names[index], out value) || value != expectedValues[index])
                    {
                        issues.Add("REV13_Common_Page." + names[index] + "=" +
                            (value ?? "<unavailable>") + " expected " + expectedValues[index]);
                    }
                }
            }
            return issues;
        }

        private static bool TryRead(object item, string propertyName, out string value)
        {
            value = null;
            PropertyInfo property = item.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null || !property.CanRead) return false;
            try
            {
                object raw = property.GetValue(item, null);
                value = raw == null ? "<null>" : raw.ToString();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static HmiSoftware FindHmi(Project project)
        {
            foreach (Device device in project.Devices)
            {
                HmiSoftware hmi = FindHmi(device.DeviceItems);
                if (hmi != null) return hmi;
            }
            return null;
        }

        private static HmiSoftware FindHmi(DeviceItemComposition items)
        {
            foreach (DeviceItem item in items)
            {
                SoftwareContainer container = item.GetService<SoftwareContainer>();
                HmiSoftware hmi = container == null ? null : container.Software as HmiSoftware;
                if (hmi != null) return hmi;
                hmi = FindHmi(item.DeviceItems);
                if (hmi != null) return hmi;
            }
            return null;
        }

        private static string Clean(string value)
        {
            return value.Replace("\t", " ").Replace("\r", " ").Replace("\n", " ");
        }
    }
}
