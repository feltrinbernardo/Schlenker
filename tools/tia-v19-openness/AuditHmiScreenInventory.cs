using System;
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

internal static class AuditHmiScreenInventory
{
    private static int Main(string[] args)
    {
        if (args.Length != 3)
        {
            Console.Error.WriteLine("Usage: AuditHmiScreenInventory <exact-ap19> <screen> <report>");
            return 2;
        }
        string projectPath = Path.GetFullPath(args[0]);
        string reportPath = Path.GetFullPath(args[2]);
        try
        {
            TiaPortalProcess process = TiaPortal.GetProcesses().Single(candidate =>
                candidate.ProjectPath != null && Path.GetFullPath(candidate.ProjectPath.FullName)
                    .Equals(projectPath, StringComparison.OrdinalIgnoreCase));
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            using (TiaPortal portal = process.Attach())
            using (StreamWriter writer = new StreamWriter(reportPath, false))
            {
                HmiSoftware hmi = FindHmi(portal.Projects.Single());
                HmiScreen screen = hmi == null ? null : hmi.Screens.Find(args[1]);
                if (screen == null) throw new InvalidOperationException("Screen not found: " + args[1]);
                writer.WriteLine("Index\tName\tType\tVisible\tEnabled\tLeft\tTop\tWidth\tHeight");
                for (int index = 0; index < screen.ScreenItems.Count; index++)
                {
                    HmiScreenItemBase item = screen.ScreenItems[index];
                    writer.WriteLine(String.Join("\t", index, Clean(item.Name), item.GetType().Name,
                        item.Visible, item.Enabled, Read(item, "Left"), Read(item, "Top"),
                        Read(item, "Width"), Read(item, "Height")));
                }
                writer.WriteLine("COUNT=" + screen.ScreenItems.Count);
                writer.WriteLine("STATUS=PASS_READ_ONLY");
            }
            Console.WriteLine("STATUS=PASS_READ_ONLY");
            Console.WriteLine("REPORT=" + reportPath);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("STATUS=FAIL");
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static string Read(object value, string name)
    {
        try
        {
            PropertyInfo property = value.GetType().GetProperty(name,
                BindingFlags.Instance | BindingFlags.Public);
            object raw = property == null ? null : property.GetValue(value, null);
            return raw == null ? "" : Convert.ToString(raw, CultureInfo.InvariantCulture);
        }
        catch { return "UNSUPPORTED"; }
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
