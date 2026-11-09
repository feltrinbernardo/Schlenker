using System;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.Online;

namespace Schlenker.TiaV19
{
    internal static class SetEngineeringOffline
    {
        private static int Main(string[] args)
        {
            if (args.Length != 1) return 2;
            string expected = Path.GetFullPath(args[0]);
            try
            {
                TiaPortalProcess process = TiaPortal.GetProcesses().SingleOrDefault(candidate =>
                    candidate.ProjectPath != null &&
                    candidate.ProjectPath.FullName.Equals(expected, StringComparison.OrdinalIgnoreCase));
                if (process == null) throw new InvalidOperationException("Exact project is not open.");
                using (TiaPortal portal = process.Attach())
                {
                    Project project = portal.Projects.Single();
                    Device plcDevice = project.Devices.Single(device =>
                        device.Name.Equals("S7-1500/ET200MP station_1", StringComparison.OrdinalIgnoreCase));
                    DeviceItem plcItem = Find(plcDevice.DeviceItems, "PLC_1");
                    OnlineProvider provider = plcItem.GetService<OnlineProvider>();
                    if (provider == null) throw new InvalidOperationException("PLC online provider missing.");
                    Console.WriteLine("STATE_BEFORE=" + provider.State);
                    if (provider.State != OnlineState.Offline)
                        provider.GoOffline();
                    Console.WriteLine("STATE_AFTER=" + provider.State);
                    Console.WriteLine("PLC_CONTROL_APIS_USED=NO");
                    Console.WriteLine("DOWNLOAD_APIS_USED=NO");
                }
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static DeviceItem Find(DeviceItemComposition items, string name)
        {
            foreach (DeviceItem item in items)
            {
                if (item.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) return item;
                DeviceItem child = Find(item.DeviceItems, name);
                if (child != null) return child;
            }
            return null;
        }
    }
}
