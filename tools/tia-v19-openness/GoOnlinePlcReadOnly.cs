using System;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.Online;

namespace Schlenker.TiaV19
{
    internal static class GoOnlinePlcReadOnly
    {
        private static int Main(string[] args)
        {
            if (args.Length < 1 || args.Length > 2)
            {
                Console.Error.WriteLine(
                    "Usage: GoOnlinePlcReadOnly <exact-ap19-path> [--go-offline]");
                return 2;
            }

            string expectedProject = Path.GetFullPath(args[0]);
            bool goOffline = args.Length == 2 &&
                args[1].Equals("--go-offline", StringComparison.Ordinal);
            if (args.Length == 2 && !goOffline)
            {
                Console.Error.WriteLine("Unknown operation: " + args[1]);
                return 2;
            }
            try
            {
                TiaPortalProcess process = TiaPortal.GetProcesses().SingleOrDefault(candidate =>
                    candidate.ProjectPath != null &&
                    candidate.ProjectPath.FullName.Equals(
                        expectedProject,
                        StringComparison.OrdinalIgnoreCase));
                if (process == null)
                    throw new InvalidOperationException("The exact TIA project is not open.");

                using (TiaPortal portal = process.Attach())
                {
                    Project project = portal.Projects.Single();
                    Device plcDevice = project.Devices.Single(device =>
                        device.Name.Equals(
                            "S7-1500/ET200MP station_1",
                            StringComparison.OrdinalIgnoreCase));
                    DeviceItem plcItem = FindDeviceItem(plcDevice.DeviceItems, "PLC_1");
                    if (plcItem == null)
                        throw new InvalidOperationException("PLC_1 was not found in the exact project.");

                    OnlineProvider provider = plcItem.GetService<OnlineProvider>();
                    if (provider == null)
                        throw new InvalidOperationException("PLC_1 has no OnlineProvider service.");
                    if (!provider.Configuration.IsConfigured)
                        throw new InvalidOperationException("PLC_1 online connection is not configured.");

                    Console.WriteLine("MODE=" +
                        (goOffline ? "ENGINEERING_SESSION_GO_OFFLINE" : "ONLINE_READ_ONLY"));
                    Console.WriteLine("PROJECT=" + project.Path.FullName);
                    Console.WriteLine("DEVICE=" + plcDevice.Name);
                    Console.WriteLine("PLC_ITEM=" + plcItem.Name);
                    Console.WriteLine("STATE_BEFORE=" + provider.State);
                    Console.WriteLine("DOWNLOAD_OR_TRANSFER_APIS_USED=NO");
                    Console.WriteLine("PLC_CONTROL_APIS_USED=NO");
                    Console.WriteLine("DEVICE_MEMORY_WRITE_APIS_USED=NO");

                    OnlineState state;
                    if (goOffline)
                    {
                        if (provider.State != OnlineState.Offline)
                            provider.GoOffline();
                        state = provider.State;
                    }
                    else
                    {
                        state = provider.State == OnlineState.Offline
                            ? provider.GoOnline()
                            : provider.State;
                    }
                    Console.WriteLine("STATE_AFTER=" + state);
                    Console.WriteLine("STATUS=" +
                        (goOffline
                            ? (state == OnlineState.Offline ? "PASS" : "FAIL")
                            : (state == OnlineState.Online ? "PASS" : "FAIL")));
                    return goOffline
                        ? (state == OnlineState.Offline ? 0 : 1)
                        : (state == OnlineState.Online ? 0 : 1);
                }
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("STATUS=FAIL");
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static DeviceItem FindDeviceItem(DeviceItemComposition items, string name)
        {
            foreach (DeviceItem item in items)
            {
                if (item.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return item;
                DeviceItem nested = FindDeviceItem(item.DeviceItems, name);
                if (nested != null)
                    return nested;
            }
            return null;
        }
    }
}
