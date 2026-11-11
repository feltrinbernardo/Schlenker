using System;
using System.IO;
using Siemens.Engineering;

namespace Schlenker.TiaV19
{
    internal static class RetrieveArchiveReadOnly
    {
        private static int Main(string[] args)
        {
            if (args.Length != 2) return 2;
            string archive = Path.GetFullPath(args[0]);
            string target = Path.GetFullPath(args[1]);
            Directory.CreateDirectory(target);
            try
            {
                using (TiaPortal portal = new TiaPortal(TiaPortalMode.WithoutUserInterface))
                {
                    Project project = portal.Projects.Retrieve(
                        new FileInfo(archive), new DirectoryInfo(target));
                    Console.WriteLine("PROJECT=" + project.Path.FullName);
                    project.Close();
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
