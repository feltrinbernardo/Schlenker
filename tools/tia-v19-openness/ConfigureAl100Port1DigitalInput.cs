using System;
using System.Collections.Generic;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;

namespace Schlenker.TiaV19
{
    internal static class ConfigureAl100Port1DigitalInput
    {
        private const string ExpectedProject =
            @"C:\TIA Projects\Schlenkers 36-10 190036-7-8v2.12-MTP1200-DeployWorking-20260918\Backup Schlenkers 36-10 190036-7-8v2.12.ap19";
        private const string DigitalInputType =
            "GSD:GSDML-V2.35-IFM-AL1X0X-20241112-144500.XML/SM/IDS DI";
        private const string PortItemName = "AL100_X1_O6P301_DI";

        private static int Main(string[] args)
        {
            string mode = args.Length == 1 ? args[0].Trim().ToLowerInvariant() : "audit";
            if (mode != "audit" && mode != "apply") return 2;

            try
            {
                TiaPortalProcess selected = TiaPortal.GetProcesses().SingleOrDefault(process =>
                    process.ProjectPath != null && process.ProjectPath.FullName.Equals(
                        ExpectedProject, StringComparison.OrdinalIgnoreCase));
                if (selected == null)
                    throw new InvalidOperationException("The exact authorized TIA project is not open.");

                using (TiaPortal portal = selected.Attach())
                {
                    Project project = portal.Projects.Single();
                    if (!project.Path.FullName.Equals(ExpectedProject, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Attached project identity changed.");

                    Device al100 = AllDevices(project).Single(device =>
                        device.Name.Equals("al100", StringComparison.OrdinalIgnoreCase));
                    DeviceItem module = FindItems(al100.DeviceItems).Single(item =>
                        item.TypeIdentifier != null &&
                        item.TypeIdentifier.IndexOf("IOLM_MODULE_8P_ENMO", StringComparison.OrdinalIgnoreCase) >= 0);

                    Console.WriteLine("PROJECT=" + project.Path.FullName);
                    Console.WriteLine("DEVICE=" + al100.Name);
                    Console.WriteLine("MODULE=" + module.Name);
                    Console.WriteLine("TARGET=AL100/X1; API_POSITION=1; MODE=Digital Input; SENSOR=O6P301");

                    DeviceItem existing = FindItems(al100.DeviceItems).FirstOrDefault(item =>
                        item.Name.Equals(PortItemName, StringComparison.OrdinalIgnoreCase) ||
                        (item.PositionNumber == 1 && item.TypeIdentifier != null &&
                         item.TypeIdentifier.IndexOf("/SM/IDS DI", StringComparison.OrdinalIgnoreCase) >= 0));
                    if (existing != null)
                    {
                        Console.WriteLine("EXISTING=" + existing.Name + "; TYPE=" + existing.TypeIdentifier);
                        Console.WriteLine("STATUS=PASS_ALREADY_CONFIGURED");
                        return 0;
                    }

                    bool canPlug = module.CanPlugNew(DigitalInputType, PortItemName, 1);
                    Console.WriteLine("CAN_PLUG=" + canPlug);
                    foreach (Siemens.Engineering.HW.Extensions.PlugLocation location in module.GetPlugLocations())
                        Console.WriteLine("PLUG_LOCATION=" + location.PositionNumber + "; LABEL=" + location.Label);

                    if (mode == "audit")
                    {
                        Console.WriteLine("SAVE=NO");
                        Console.WriteLine("STATUS=" + (canPlug ? "PASS_READY" : "BLOCKED"));
                        return canPlug ? 0 : 3;
                    }
                    if (!canPlug)
                        throw new InvalidOperationException("TIA does not allow the Digital Input submodule in AL100/X1.");

                    DeviceItem created = null;
                    try
                    {
                        created = module.PlugNew(DigitalInputType, PortItemName, 1);
                        if (created == null || created.PositionNumber != 1 ||
                            (created.TypeIdentifier ?? String.Empty).IndexOf("/SM/IDS DI", StringComparison.OrdinalIgnoreCase) < 0)
                            throw new InvalidOperationException("Created port item did not validate as AL100/X1 Digital Input.");

                        project.Save();
                        Console.WriteLine("CREATED=" + created.Name + "; TYPE=" + created.TypeIdentifier +
                            "; POSITION=" + created.PositionNumber);
                        Console.WriteLine("SAVE=YES");
                        Console.WriteLine("STATUS=PASS_APPLIED");
                    }
                    catch
                    {
                        if (created != null)
                        {
                            try { created.Delete(); }
                            catch { }
                        }
                        throw;
                    }
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

        private static IEnumerable<Device> AllDevices(Project project)
        {
            HashSet<Device> seen = new HashSet<Device>();
            foreach (Device device in project.Devices)
                if (seen.Add(device)) yield return device;
            foreach (Device device in project.UngroupedDevicesGroup.Devices)
                if (seen.Add(device)) yield return device;
            foreach (DeviceUserGroup group in project.DeviceGroups)
                foreach (Device device in DevicesInGroup(group))
                    if (seen.Add(device)) yield return device;
        }

        private static IEnumerable<Device> DevicesInGroup(DeviceUserGroup group)
        {
            foreach (Device device in group.Devices) yield return device;
            foreach (DeviceUserGroup child in group.Groups)
                foreach (Device device in DevicesInGroup(child)) yield return device;
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
