using System;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.Connection;
using Siemens.Engineering.Download;
using Siemens.Engineering.HW;

namespace Schlenker.TiaV19
{
    internal static class InspectDownloadConfigurationReadOnly
    {
        private const string ExpectedProject =
            @"C:\TIA Projects\Schlenkers 36-10 190036-7-8v2.12-MTP1200-DeployWorking-20260918\Backup Schlenkers 36-10 190036-7-8v2.12.ap19";

        private static int Main()
        {
            try
            {
                TiaPortalProcess selected = TiaPortal.GetProcesses().SingleOrDefault(process =>
                    process.ProjectPath != null && process.ProjectPath.FullName.Equals(
                        ExpectedProject, StringComparison.OrdinalIgnoreCase));
                if (selected == null)
                    throw new InvalidOperationException("Exact authorized project is not open.");
                using (TiaPortal portal = selected.Attach())
                {
                    Project project = portal.Projects.Single();
                    Console.WriteLine("MODE=READ_ONLY; SAVE=NO; DOWNLOAD=NO; ONLINE_WRITE=NO");
                    Console.WriteLine("PROJECT=" + project.Path.FullName);
                    foreach (Device device in project.Devices.Where(device =>
                        device.Name.Equals("S7-1500/ET200MP station_1", StringComparison.OrdinalIgnoreCase) ||
                        device.Name.Equals("HMI_1", StringComparison.OrdinalIgnoreCase)))
                    {
                        Console.WriteLine("DEVICE=" + device.Name);
                        InspectItems(device.DeviceItems, "  ");
                    }
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

        private static void InspectItems(DeviceItemComposition items, string indent)
        {
            foreach (DeviceItem item in items)
            {
                DownloadProvider provider = item.GetService<DownloadProvider>();
                if (provider != null)
                {
                    Console.WriteLine(indent + "PROVIDER_ITEM=" + item.Name);
                    ConnectionConfiguration configuration = provider.Configuration;
                    Console.WriteLine(indent + "IS_CONFIGURED=" + configuration.IsConfigured);
                    foreach (ConfigurationMode mode in configuration.Modes)
                    {
                        Console.WriteLine(indent + "MODE=" + mode.Name);
                        foreach (ConfigurationPcInterface pc in mode.PcInterfaces)
                        {
                            Console.WriteLine(indent + "  PC=" + pc.Name + "; NUMBER=" + pc.Number);
                            foreach (ConfigurationTargetInterface target in pc.TargetInterfaces)
                            {
                                Console.WriteLine(indent + "    TARGET=" + target.Name);
                                foreach (ConfigurationAddress address in target.Addresses)
                                    Console.WriteLine(indent + "      ADDRESS=" + address.Name + "; VALUE=" + address.Address);
                                if (pc.Name.Equals("Intel(R) Ethernet Connection (2) I219-LM",
                                        StringComparison.OrdinalIgnoreCase) &&
                                    target.Name.EndsWith("X1", StringComparison.OrdinalIgnoreCase))
                                {
                                    configuration.ApplyConfiguration(target);
                                    Console.WriteLine(indent + "      SELECTED_FOR_PREFLIGHT=YES");
                                    foreach (ConfigurationAccessibleDevice accessible in pc.GetAccessibleDevices())
                                        Console.WriteLine(indent + "      ACCESSIBLE=" + accessible.Name +
                                            "; ADDRESS=" + accessible.Address +
                                            "; MAC=" + accessible.MACAddress +
                                            "; SERIES=" + accessible.DeviceSeries);
                                }
                            }
                        }
                    }
                }
                InspectItems(item.DeviceItems, indent + "  ");
            }
        }
    }
}
