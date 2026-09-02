using System;
using System.Collections.Generic;
using System.Globalization;
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

internal static class ScreenFrameZoneAudit
{
    private sealed class Bounds
    {
        public double Left;
        public double Top;
        public double Right;
        public double Bottom;
    }

    private static readonly HashSet<string> ApprovedFooterObjects =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "REV12_Settings_Return", "REV12_Settings_Previous", "REV12_Settings_Next",
            "REV18_IOL_Detail_Back", "REV18_IOL_Detail_Previous", "REV18_IOL_Detail_Next",
            "PILZ26_Back", "ROB_CIP_RECIPE_EDIT_Back",
            "REV12_Efficiency_Previous", "REV12_Efficiency_Next"
        };

    private static int Main(string[] args)
    {
        if (args.Length < 2 || args.Length > 3)
        {
            Console.Error.WriteLine("Usage: ScreenFrameZoneAudit <exact-ap19-path> <output-tsv> [screen]");
            return 2;
        }

        string projectPath = Path.GetFullPath(args[0]);
        string outputPath = Path.GetFullPath(args[1]);
        string targetScreen = args.Length == 3 ? args[2] : null;
        try
        {
            TiaPortalProcess process = TiaPortal.GetProcesses().FirstOrDefault(candidate =>
                candidate.ProjectPath != null &&
                Path.GetFullPath(candidate.ProjectPath.FullName)
                    .Equals(projectPath, StringComparison.OrdinalIgnoreCase));
            if (process == null)
                throw new InvalidOperationException("The exact offline project is not open: " + projectPath);

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            int issues = 0;
            using (TiaPortal portal = process.Attach())
            using (StreamWriter writer = new StreamWriter(outputPath, false, new System.Text.UTF8Encoding(false)))
            {
                HmiSoftware hmi = FindHmi(portal.Projects.First());
                if (hmi == null) throw new InvalidOperationException("No WinCC Unified HMI was found.");

                writer.WriteLine("Screen\tObject\tType\tLeft\tTop\tRight\tBottom\tZone\tStatus");
                foreach (HmiScreen screen in hmi.Screens.OrderBy(value => value.Name))
                {
                    if (screen.Name.Equals("production", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!String.IsNullOrWhiteSpace(targetScreen) &&
                        !screen.Name.Equals(targetScreen, StringComparison.OrdinalIgnoreCase)) continue;
                    foreach (HmiScreenItemBase item in screen.ScreenItems.OrderBy(value => value.Name))
                    {
                        if (IsMasterFrameObject(item.Name)) continue;
                        Bounds bounds;
                        if (!TryGetBounds(item, out bounds)) continue;

                        List<string> zones = new List<string>();
                        if (Intersects(bounds, 0, 0, 1366, 100)) zones.Add("HEADER_STATUS");
                        if (Intersects(bounds, 1215, 100, 1366, 712)) zones.Add("RIGHT_NAV");
                        if (Intersects(bounds, 0, 712, 1366, 768)) zones.Add("FOOTER");
                        if (zones.Count == 0) continue;

                        bool approvedFooter = ApprovedFooterObjects.Contains(item.Name) &&
                            zones.Count == 1 && zones[0] == "FOOTER" &&
                            bounds.Top >= 720 && bounds.Bottom <= 768;
                        string status = approvedFooter ? "APPROVED_PAGE_NAV" : "ISSUE";
                        if (!approvedFooter) issues++;
                        writer.WriteLine(String.Join("\t",
                            Clean(screen.Name), Clean(item.Name), item.GetType().Name,
                            Number(bounds.Left), Number(bounds.Top), Number(bounds.Right), Number(bounds.Bottom),
                            String.Join(",", zones), status));
                    }
                }
            }

            Console.WriteLine("FRAME_ZONE_ISSUES=" + issues);
            Console.WriteLine("OUTPUT=" + outputPath);
            Console.WriteLine("STATUS=" + (issues == 0 ? "PASS" : "FAIL"));
            return issues == 0 ? 0 : 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("STATUS=FAIL");
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static bool IsMasterFrameObject(string name)
    {
        return name.Equals("REV12_Production_User_Header", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("REV12_Mimic_Vacuum_Label_1", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("Line_1", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("Line_2", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("REV13_Common_", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("REV13_Nav_", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("REV14_Nav_", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("REV21_Common_", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryGetBounds(object item, out Bounds bounds)
    {
        double left, top, width, height;
        if (TryReadNumber(item, "Left", out left) && TryReadNumber(item, "Top", out top) &&
            TryReadNumber(item, "Width", out width) && TryReadNumber(item, "Height", out height))
        {
            bounds = new Bounds { Left = left, Top = top, Right = left + width, Bottom = top + height };
            return true;
        }

        double centerX, centerY, radiusX, radiusY;
        if (TryReadNumber(item, "CenterX", out centerX) && TryReadNumber(item, "CenterY", out centerY) &&
            TryReadNumber(item, "RadiusX", out radiusX) && TryReadNumber(item, "RadiusY", out radiusY))
        {
            bounds = new Bounds
            {
                Left = centerX - radiusX,
                Top = centerY - radiusY,
                Right = centerX + radiusX,
                Bottom = centerY + radiusY
            };
            return true;
        }

        bounds = null;
        return false;
    }

    private static bool TryReadNumber(object value, string name, out double number)
    {
        PropertyInfo property = value.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        if (property == null || !property.CanRead)
        {
            number = 0;
            return false;
        }
        object raw = property.GetValue(value, null);
        if (raw == null)
        {
            number = 0;
            return false;
        }
        try
        {
            number = Convert.ToDouble(raw, CultureInfo.InvariantCulture);
            return true;
        }
        catch
        {
            number = 0;
            return false;
        }
    }

    private static bool Intersects(Bounds value, double left, double top, double right, double bottom)
    {
        return value.Right > left && value.Left < right && value.Bottom > top && value.Top < bottom;
    }

    private static string Number(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string Clean(string value)
    {
        return (value ?? "").Replace("\t", " ").Replace("\r", " ").Replace("\n", " ");
    }

    private static HmiSoftware FindHmi(Project project)
    {
        foreach (Device device in project.Devices)
        foreach (DeviceItem item in device.DeviceItems)
        {
            HmiSoftware hmi = FindHmi(item);
            if (hmi != null) return hmi;
        }
        return null;
    }

    private static HmiSoftware FindHmi(DeviceItem item)
    {
        SoftwareContainer container = item.GetService<SoftwareContainer>();
        HmiSoftware hmi = container == null ? null : container.Software as HmiSoftware;
        if (hmi != null) return hmi;
        foreach (DeviceItem child in item.DeviceItems)
        {
            hmi = FindHmi(child);
            if (hmi != null) return hmi;
        }
        return null;
    }
}
