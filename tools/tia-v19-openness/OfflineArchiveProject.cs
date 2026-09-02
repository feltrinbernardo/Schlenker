using System;
using System.IO;
using System.Linq;
using Siemens.Engineering;

namespace Schlenker.TiaV19
{
    internal static class OfflineArchiveProject
    {
        private static int Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.Error.WriteLine("Usage: OfflineArchiveProject <exact-ap19-path> <output-directory> <archive-name>");
                return 2;
            }

            string projectPath = Path.GetFullPath(args[0]);
            DirectoryInfo output = new DirectoryInfo(Path.GetFullPath(args[1]));
            string archiveName = args[2];
            try
            {
                if (!File.Exists(projectPath)) throw new FileNotFoundException("Project not found.", projectPath);
                if (!projectPath.EndsWith(".ap19", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Only an exact TIA Portal V19 .ap19 project is accepted.");
                if (!output.Exists) output.Create();

                using (TiaPortal portal = new TiaPortal(TiaPortalMode.WithoutUserInterface))
                {
                    Project project = portal.Projects.Open(new FileInfo(projectPath));
                    try
                    {
                        project.Save();
                        project.Archive(output, archiveName, ProjectArchivationMode.Compressed);
                    }
                    finally
                    {
                        project.Close();
                    }
                }

                string archivePath = Path.Combine(output.FullName, archiveName);
                if (!File.Exists(archivePath)) archivePath += ".zap19";
                if (!File.Exists(archivePath)) throw new FileNotFoundException("TIA archive output was not found.", archivePath);
                Console.WriteLine("PROJECT=" + projectPath);
                Console.WriteLine("ARCHIVE=" + archivePath);
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
