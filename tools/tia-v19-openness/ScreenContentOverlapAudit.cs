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

internal static class ScreenContentOverlapAudit
{
    private sealed class ItemBounds
    {
        public HmiScreenItemBase Item;
        public double Left;
        public double Top;
        public double Right;
        public double Bottom;
        public double Area { get { return Math.Max(0, Right - Left) * Math.Max(0, Bottom - Top); } }
    }

    private static int Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.Error.WriteLine("Usage: ScreenContentOverlapAudit <exact-ap19-path> <output-tsv>");
            return 2;
        }

        string projectPath = Path.GetFullPath(args[0]);
        string outputPath = Path.GetFullPath(args[1]);
        try
        {
            TiaPortalProcess process = TiaPortal.GetProcesses().SingleOrDefault(candidate =>
                candidate.ProjectPath != null &&
                Path.GetFullPath(candidate.ProjectPath.FullName)
                    .Equals(projectPath, StringComparison.OrdinalIgnoreCase));
            if (process == null)
                throw new InvalidOperationException("The exact offline project is not open: " + projectPath);

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            int overlaps = 0;
            using (TiaPortal portal = process.Attach())
            using (StreamWriter writer = new StreamWriter(outputPath, false,
                new System.Text.UTF8Encoding(false)))
            {
                HmiSoftware hmi = FindHmi(portal.Projects.First());
                if (hmi == null) throw new InvalidOperationException("No WinCC Unified HMI was found.");

                writer.WriteLine("Screen\tObjectA\tTypeA\tObjectB\tTypeB\tOverlapWidth\tOverlapHeight\tSmallerAreaCoveredPct");
                foreach (HmiScreen screen in hmi.Screens.OrderBy(value => value.Name))
                {
                    List<ItemBounds> items = screen.ScreenItems
                        .Where(item => item.Visible && IsForegroundContent(item) && !IsSharedFrame(item.Name))
                        .Select(ReadBounds)
                        .Where(value => value != null && value.Area > 0)
                        .ToList();

                    for (int first = 0; first < items.Count; first++)
                    for (int second = first + 1; second < items.Count; second++)
                    {
                        ItemBounds a = items[first];
                        ItemBounds b = items[second];
                        double width = Math.Min(a.Right, b.Right) - Math.Max(a.Left, b.Left);
                        double height = Math.Min(a.Bottom, b.Bottom) - Math.Max(a.Top, b.Top);
                        if (width <= 2 || height <= 2) continue;
                        double area = width * height;
                        double smaller = Math.Min(a.Area, b.Area);
                        if (area < 20 || smaller <= 0) continue;
                        double covered = area / smaller * 100.0;
                        if (covered < 10.0) continue;

                        overlaps++;
                        writer.WriteLine(String.Join("\t",
                            Clean(screen.Name), Clean(a.Item.Name), a.Item.GetType().Name,
                            Clean(b.Item.Name), b.Item.GetType().Name,
                            Number(width), Number(height), Number(covered)));
                    }
                }
            }

            Console.WriteLine("CONTENT_OVERLAPS=" + overlaps);
            Console.WriteLine("OUTPUT=" + outputPath);
            Console.WriteLine("STATUS=PASS_READ_ONLY");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("STATUS=FAIL");
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static bool IsForegroundContent(HmiScreenItemBase item)
    {
        string type = item.GetType().Name;
        return type == "HmiButton" || type == "HmiText" || type == "HmiIOField" ||
            type == "HmiSymbolicIOField" || type == "HmiGraphicView" || type == "HmiEllipse";
    }

    private static bool IsSharedFrame(string name)
    {
        return name.StartsWith("REV12_Common_", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("REV12_Exact_Status_", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("REV12_Nav_", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("REV12_Production_User_Header", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("REV13_Common_", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("REV13_Nav_", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("REV14_Nav_", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("REV21_Common_", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("REV28_Common_", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("REV54_Common_", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("REF01_Footer", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("REF05_Nav", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("REF06_Footer", StringComparison.OrdinalIgnoreCase);
    }

    private static ItemBounds ReadBounds(HmiScreenItemBase item)
    {
        double left, top, width, height;
        if (TryRead(item, "Left", out left) && TryRead(item, "Top", out top) &&
            TryRead(item, "Width", out width) && TryRead(item, "Height", out height))
        {
            return new ItemBounds { Item = item, Left = left, Top = top,
                Right = left + width, Bottom = top + height };
        }

        double centerX, centerY, radiusX, radiusY;
        if (TryRead(item, "CenterX", out centerX) && TryRead(item, "CenterY", out centerY) &&
            TryRead(item, "RadiusX", out radiusX) && TryRead(item, "RadiusY", out radiusY))
        {
            return new ItemBounds { Item = item, Left = centerX - radiusX,
                Top = centerY - radiusY, Right = centerX + radiusX, Bottom = centerY + radiusY };
        }
        return null;
    }

    private static bool TryRead(object value, string propertyName, out double result)
    {
        PropertyInfo property = value.GetType().GetProperty(propertyName,
            BindingFlags.Instance | BindingFlags.Public);
        if (property == null || !property.CanRead)
        {
            result = 0;
            return false;
        }
        object raw = property.GetValue(value, null);
        if (raw == null)
        {
            result = 0;
            return false;
        }
        try
        {
            result = Convert.ToDouble(raw, CultureInfo.InvariantCulture);
            return true;
        }
        catch
        {
            result = 0;
            return false;
        }
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
