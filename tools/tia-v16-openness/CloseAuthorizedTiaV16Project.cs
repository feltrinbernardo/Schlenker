using System;
using System.IO;
using System.Linq;
using Siemens.Engineering;

namespace Schlenker.TiaV16
{
    internal static class CloseAuthorizedTiaV16Project
    {
        private const string AuthorizationSwitch =
            "--authorized-close-reset-project";

        private static int Main(string[] args)
        {
            if (args.Length != 2 ||
                !args[0].Equals(AuthorizationSwitch, StringComparison.Ordinal))
            {
                Console.Error.WriteLine(
                    "Usage: CloseAuthorizedTiaV16Project " +
                    AuthorizationSwitch + " <exact-ap16-path>");
                return 2;
            }

            try
            {
                string projectPath = Path.GetFullPath(args[1]);
                TiaPortalProcess process = TiaPortal.GetProcesses().SingleOrDefault(candidate =>
                    candidate.ProjectPath != null &&
                    candidate.ProjectPath.FullName.Equals(
                        projectPath, StringComparison.OrdinalIgnoreCase));
                if (process == null)
                    throw new InvalidOperationException(
                        "The exact authorized TIA V16 project is not open.");

                using (TiaPortal portal = process.Attach())
                {
                    Project project = portal.Projects.Single();
                    if (!Path.GetFullPath(project.Path.FullName).Equals(
                            projectPath, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException(
                            "Authorized project identity changed.");

                    project.Save();
                    Console.WriteLine("PROJECT_SAVE=YES");
                    project.Close();
                    Console.WriteLine("PROJECT_CLOSE=YES");
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
    }
}
