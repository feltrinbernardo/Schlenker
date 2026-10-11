using System;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.Connection;
using Siemens.Engineering.Download;
using Siemens.Engineering.HW;

namespace Schlenker.TiaV16
{
    internal static class InspectDriveDownloadTarget
    {
        private static int Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine(
                    "Usage: InspectDriveDownloadTarget <exact-ap16-path> <device-name> <provider-item-name>");
                return 2;
            }

            try
            {
                string projectPath = Path.GetFullPath(args[0]);
                string deviceName = args[1];
                string providerItemName = args[2];
                TiaPortalProcess process = TiaPortal.GetProcesses().SingleOrDefault(candidate =>
                    candidate.ProjectPath != null &&
                    candidate.ProjectPath.FullName.Equals(projectPath, StringComparison.OrdinalIgnoreCase));
                if (process == null)
                    throw new InvalidOperationException("The exact TIA V16 project is not open.");

                using (TiaPortal portal = process.Attach())
                {
                    Project project = portal.Projects.Single();
                    Device device = project.Devices.Single(candidate =>
                        candidate.Name.Equals(deviceName, StringComparison.OrdinalIgnoreCase));
                    DeviceItem providerItem = Find(device.DeviceItems, providerItemName);
                    if (providerItem == null)
                        throw new InvalidOperationException("Download provider item was not found.");

                    DownloadProvider provider = providerItem.GetService<DownloadProvider>();
                    if (provider == null)
                        throw new InvalidOperationException("The selected item has no DownloadProvider.");

                    Console.WriteLine("MODE=READ_ONLY_PREFLIGHT; DOWNLOAD=NO; DEVICE_WRITE=NO");
                    Console.WriteLine("PROJECT=" + project.Path.FullName);
                    Console.WriteLine("DEVICE=" + device.Name);
                    Console.WriteLine("PROVIDER_ITEM=" + providerItem.Name);
                    Console.WriteLine("CONFIGURED=" + provider.Configuration.IsConfigured);

                    foreach (ConfigurationMode mode in provider.Configuration.Modes)
                    {
                        Console.WriteLine("MODE=" + mode.Name);
                        foreach (ConfigurationPcInterface pc in mode.PcInterfaces)
                        {
                            Console.WriteLine("PC=" + pc.Name + ";NUMBER=" + pc.Number);
                            foreach (ConfigurationTargetInterface target in pc.TargetInterfaces)
                            {
                                Console.WriteLine("TARGET=" + target.Name);
                                if (!mode.Name.Equals("PN/IE", StringComparison.OrdinalIgnoreCase) ||
                                    pc.Name.IndexOf("I219-LM", StringComparison.OrdinalIgnoreCase) < 0)
                                    continue;

                                provider.Configuration.ApplyConfiguration(target);
                                Console.WriteLine("SELECTED_FOR_PREFLIGHT=YES");
                            }
                        }
                    }

                    Console.WriteLine("STATUS=PASS");
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

        private static DeviceItem Find(DeviceItemComposition items, string name)
        {
            foreach (DeviceItem item in items)
            {
                if (item.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return item;
                DeviceItem nested = Find(item.DeviceItems, name);
                if (nested != null)
                    return nested;
            }

            return null;
        }
    }
}
