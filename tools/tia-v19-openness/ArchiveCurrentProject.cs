using System;
using System.IO;
using System.Linq;
using Siemens.Engineering;

namespace Schlenker.TiaV19
{
    internal static class ArchiveCurrentProject
    {
        private static int Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.Error.WriteLine("Usage: ArchiveCurrentProject <project-file-hint> <output-directory> <archive-name>");
                return 2;
            }

            try
            {
                string projectHint = args[0];
                DirectoryInfo output = new DirectoryInfo(Path.GetFullPath(args[1]));
                string archiveName = args[2];
                if (!output.Exists)
                {
                    output.Create();
                }

                TiaPortalProcess process = TiaPortal.GetProcesses().FirstOrDefault(
                    candidate => candidate.ProjectPath != null &&
                        candidate.ProjectPath.FullName.EndsWith(projectHint, StringComparison.OrdinalIgnoreCase));
                if (process == null)
                {
                    throw new InvalidOperationException("No open TIA project matched " + projectHint + ".");
                }

                using (TiaPortal portal = process.Attach())
                {
                    Project project = portal.Projects.FirstOrDefault();
                    if (project == null)
                    {
                        throw new InvalidOperationException("The selected TIA process has no open project.");
                    }

                    project.Save();
                    project.Archive(output, archiveName, ProjectArchivationMode.Compressed);
                    string archivePath = Path.Combine(output.FullName, archiveName);
                    if (!File.Exists(archivePath))
                    {
                        string archiveWithExtension = archivePath + ".zap19";
                        if (!File.Exists(archiveWithExtension))
                        {
                            throw new FileNotFoundException("TIA reported success but the archive output was not found.", archivePath);
                        }
                        archivePath = archiveWithExtension;
                    }
                    Console.WriteLine("PROJECT=" + project.Path.FullName);
                    Console.WriteLine("ARCHIVE=" + archivePath);
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
