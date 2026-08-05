using System;
using System.IO;
using System.Linq;
using Siemens.Engineering;

namespace Schlenker.TiaV19
{
    internal static class CreateOfflineProject
    {
        private static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine(
                    "Usage: CreateOfflineProject.exe <target-directory> <project-name>");
                return 2;
            }

            string targetDirectory = Path.GetFullPath(args[0]);
            string projectName = args[1].Trim();

            if (projectName.Length == 0 ||
                projectName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                Console.Error.WriteLine("Invalid project name.");
                return 2;
            }

            Directory.CreateDirectory(targetDirectory);

            if (Directory.EnumerateFiles(
                    targetDirectory,
                    "*.ap19",
                    SearchOption.AllDirectories).Any())
            {
                Console.Error.WriteLine(
                    "Refusing to overwrite an existing TIA V19 project under: " +
                    targetDirectory);
                return 3;
            }

            try
            {
                using (var portal = new TiaPortal(
                    TiaPortalMode.WithoutUserInterface))
                {
                    Project project = portal.Projects.Create(
                        new DirectoryInfo(targetDirectory),
                        projectName);
                    project.Save();
                    Console.WriteLine("STATUS=PASS");
                    Console.WriteLine("PROJECT=" + project.Path.FullName);
                    project.Close();
                }
                return 0;
            }
            catch (Exception exception)
            {
                File.WriteAllText(
                    Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "CreateOfflineProject.last-error.txt"),
                    exception.ToString());
                Console.Error.WriteLine("STATUS=FAIL");
                Console.Error.WriteLine(exception.ToString());
                return 1;
            }
        }
    }
}
