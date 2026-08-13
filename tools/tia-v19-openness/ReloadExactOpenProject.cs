using System;
using System.IO;
using System.Linq;
using Siemens.Engineering;

namespace Schlenker.TiaV19
{
    internal static class ReloadExactOpenProject
    {
        private static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("Usage: ReloadExactOpenProject <exact-ap19-path>");
                return 2;
            }

            string projectPath = Path.GetFullPath(args[0]);
            try
            {
                TiaPortalProcess[] matches = TiaPortal.GetProcesses()
                    .Where(candidate => candidate.ProjectPath != null &&
                        Path.GetFullPath(candidate.ProjectPath.FullName)
                            .Equals(projectPath, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                if (matches.Length != 1)
                    throw new InvalidOperationException(String.Format(
                        "Expected exactly one open TIA project at '{0}', found {1}.",
                        projectPath, matches.Length));

                using (TiaPortal portal = matches[0].Attach())
                {
                    Project openProject = portal.Projects.FirstOrDefault();
                    if (openProject == null)
                        throw new InvalidOperationException("Attached TIA process has no project.");
                    string openPath = Path.GetFullPath(openProject.Path.FullName);
                    if (!openPath.Equals(projectPath, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Attached project path changed: " + openPath);

                    // Intentionally do not call Save: this reload is the transaction rollback.
                    openProject.Close();
                    Project reopened = portal.Projects.Open(new FileInfo(projectPath));
                    if (reopened == null)
                        throw new InvalidOperationException("TIA did not reopen the project.");
                    string reopenedPath = Path.GetFullPath(reopened.Path.FullName);
                    if (!reopenedPath.Equals(projectPath, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("TIA reopened a different project: " + reopenedPath);

                    Console.WriteLine("PROJECT=" + reopenedPath);
                    Console.WriteLine("PROCESS_ID=" + matches[0].Id);
                    Console.WriteLine("SAVE_INVOKED=NO");
                    Console.WriteLine("STATUS=PASS");
                    return 0;
                }
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
