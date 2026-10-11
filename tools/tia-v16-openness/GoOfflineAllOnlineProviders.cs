using System;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.Online;

namespace Schlenker.TiaV16
{
    internal static class GoOfflineAllOnlineProviders
    {
        private static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("Usage: GoOfflineAllOnlineProviders <exact-ap16-path>");
                return 2;
            }

            try
            {
                string expectedProject = Path.GetFullPath(args[0]);
                TiaPortalProcess process = TiaPortal.GetProcesses().SingleOrDefault(candidate =>
                    candidate.ProjectPath != null &&
                    candidate.ProjectPath.FullName.Equals(expectedProject, StringComparison.OrdinalIgnoreCase));
                if (process == null)
                    throw new InvalidOperationException("The exact TIA V16 project is not open.");

                using (TiaPortal portal = process.Attach())
                {
                    Project project = portal.Projects.Single();
                    int providerCount = 0;
                    int disconnectedCount = 0;

                    foreach (Device device in project.Devices)
                    {
                        Console.WriteLine("DEVICE=" + device.Name);
                        Visit(device.DeviceItems, ref providerCount, ref disconnectedCount);
                    }

                    Console.WriteLine("PROVIDERS=" + providerCount);
                    Console.WriteLine("DISCONNECTED=" + disconnectedCount);
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

        private static void Visit(
            DeviceItemComposition items,
            ref int providerCount,
            ref int disconnectedCount)
        {
            foreach (DeviceItem item in items)
            {
                OnlineProvider provider = item.GetService<OnlineProvider>();
                if (provider != null)
                {
                    providerCount++;
                    OnlineState before = provider.State;
                    if (before != OnlineState.Offline)
                    {
                        provider.GoOffline();
                        disconnectedCount++;
                    }

                    Console.WriteLine(
                        "ITEM=" + item.Name +
                        ";STATE_BEFORE=" + before +
                        ";STATE_AFTER=" + provider.State);
                }

                Visit(item.DeviceItems, ref providerCount, ref disconnectedCount);
            }
        }
    }
}
