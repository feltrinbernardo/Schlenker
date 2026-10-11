using System;
using System.IO;
using Siemens.Engineering;

namespace Schlenker.TiaV19
{
    internal static class UpgradeCopiedV16Project
    {
        private static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine(
                    "Usage: UpgradeCopiedV16Project <copied-ap16-path>");
                return 2;
            }

            try
            {
                string sourcePath = Path.GetFullPath(args[0]);
                if (!sourcePath.EndsWith(".ap16", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        "Only an explicit copied TIA V16 project is accepted.");

                using (TiaPortal portal = new TiaPortal(
                    TiaPortalMode.WithoutUserInterface))
                {
                    Project project = portal.Projects.OpenWithUpgrade(
                        new FileInfo(sourcePath));
                    if (project == null)
                        throw new InvalidOperationException(
                            "TIA V19 did not return an upgraded project.");

                    project.Save();
                    Console.WriteLine("STATUS=PASS");
                    Console.WriteLine("SOURCE=" + sourcePath);
                    Console.WriteLine("UPGRADED_PROJECT=" + project.Path.FullName);
                    project.Close();
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
