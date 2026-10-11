using System;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;

namespace Schlenker.TiaV19
{
    internal static class AddExactG120CCapDistributorOffline
    {
        private const string TypeIdentifier =
            "OrderNumber:6SL3210-1KE11-8AF1/4.7.10/";
        private const string DeviceName = "G120C_CAP_DISTRIBUTOR";
        private const string ItemName = "VSD_CAP_DISTRIBUTOR";

        private static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine(
                    "Usage: AddExactG120CCapDistributorOffline <exact-ap19-path>");
                return 2;
            }

            try
            {
                string projectPath = Path.GetFullPath(args[0]);
                TiaPortalProcess process = TiaPortal.GetProcesses().SingleOrDefault(candidate =>
                    candidate.ProjectPath != null &&
                    candidate.ProjectPath.FullName.Equals(
                        projectPath, StringComparison.OrdinalIgnoreCase));
                if (process == null)
                    throw new InvalidOperationException(
                        "The exact isolated TIA V19 project is not open.");

                using (TiaPortal portal = process.Attach())
                {
                    Project project = portal.Projects.Single();
                    if (project.Devices.Any(device =>
                            device.Name.Equals(DeviceName, StringComparison.OrdinalIgnoreCase)))
                        throw new InvalidOperationException(
                            "The exact G120C device already exists.");

                    Device created = project.Devices.CreateWithItem(
                        TypeIdentifier, ItemName, null);
                    if (created == null)
                        throw new InvalidOperationException(
                            "TIA V19 did not create the exact G120C catalog device.");

                    created.Name = DeviceName;

                    project.Save();
                    Console.WriteLine("STATUS=PASS");
                    Console.WriteLine("MODE=OFFLINE_ONLY; DEVICE_WRITE=NO");
                    Console.WriteLine("PROJECT=" + project.Path.FullName);
                    Console.WriteLine("TYPE_IDENTIFIER=" + TypeIdentifier);
                    Console.WriteLine("DEVICE=" + created.Name);
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
    }
}
