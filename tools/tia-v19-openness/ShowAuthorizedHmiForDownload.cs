using System;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;

namespace Schlenker.TiaV19
{
    internal static class ShowAuthorizedHmiForDownload
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
                    Device hmi = project.Devices.Single(device =>
                        device.Name.Equals("HMI_1", StringComparison.OrdinalIgnoreCase));
                    hmi.ShowInEditor(View.Device);
                    Console.WriteLine("STATUS=PASS; DEVICE=HMI_1; VIEW=DEVICE");
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
