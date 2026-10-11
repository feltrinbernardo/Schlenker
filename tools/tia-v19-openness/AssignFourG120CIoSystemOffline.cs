using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;

namespace Schlenker.TiaV19
{
    internal static class AssignFourG120CIoSystemOffline
    {
        private static readonly string[] DriveNames =
        {
            "VSD_MAIN_MACHINE",
            "VSD_BOTTLE_CONVEYOR",
            "VSD_PRODUCT_PUMP",
            "VSD_CAP_DISTRIBUTOR"
        };

        private static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("Usage: AssignFourG120CIoSystemOffline <exact-ap19-path>");
                return 2;
            }

            string projectPath = Path.GetFullPath(args[0]);
            try
            {
                using (TiaPortal portal = new TiaPortal(TiaPortalMode.WithoutUserInterface))
                {
                    Project project = portal.Projects.Open(new FileInfo(projectPath));
                    try
                    {
                        List<Device> devices = AllDevices(project).ToList();
                        IoSystem ioSystem = devices
                            .SelectMany(device => FindNetworks(device.DeviceItems))
                            .SelectMany(network => network.IoControllers)
                            .Select(controller => controller.IoSystem)
                            .Single(system => system != null &&
                                system.Name.Equals("PROFINET IO-System_1", StringComparison.OrdinalIgnoreCase));

                        foreach (string driveName in DriveNames)
                        {
                            Device drive = devices.Single(device =>
                                device.Name.Equals(driveName, StringComparison.OrdinalIgnoreCase));
                            NetworkInterface network = FindNetworks(drive.DeviceItems).Single(item =>
                                item.InterfaceOperatingMode.ToString().Equals("IoDevice",
                                    StringComparison.OrdinalIgnoreCase));
                            IoConnector connector = network.IoConnectors.First();
                            if (connector.ConnectedToIoSystem == null)
                                connector.ConnectToIoSystem(ioSystem);
                            if (connector.ConnectedToIoSystem == null ||
                                !connector.ConnectedToIoSystem.Name.Equals(ioSystem.Name,
                                    StringComparison.OrdinalIgnoreCase))
                                throw new InvalidOperationException(driveName +
                                    " did not connect to the intended IO system.");
                            Console.WriteLine("ASSIGNED device={0}; iosystem={1}",
                                driveName, connector.ConnectedToIoSystem.Name);
                        }

                        project.Save();
                        Console.WriteLine("PROJECT=" + project.Path.FullName);
                        Console.WriteLine("MODE=OFFLINE_APPLY; ONLINE=NO; DOWNLOAD=NO; SAVE=YES");
                        Console.WriteLine("STATUS=PASS_APPLIED");
                    }
                    finally
                    {
                        project.Close();
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

        private static IEnumerable<NetworkInterface> FindNetworks(DeviceItemComposition items)
        {
            foreach (DeviceItem item in items)
            {
                NetworkInterface network = item.GetService<NetworkInterface>();
                if (network != null) yield return network;
                foreach (NetworkInterface child in FindNetworks(item.DeviceItems)) yield return child;
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
    }
}
