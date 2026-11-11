using System;
using System.IO;
using System.Threading;
using Siemens.Engineering;

namespace Schlenker.TiaV19
{
    internal static class OpenExactProjectVisibleReadOnly
    {
        private static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("Usage: OpenExactProjectVisibleReadOnly <exact-ap19-path>");
                return 2;
            }

            string projectPath = Path.GetFullPath(args[0]);
            using (TiaPortal portal = new TiaPortal(TiaPortalMode.WithUserInterface))
            {
                Project project = portal.Projects.Open(new FileInfo(projectPath));
                Console.WriteLine("MODE=VISIBLE_READ_ONLY; SAVE=NO; DOWNLOAD=NO; ONLINE_WRITE=NO");
                Console.WriteLine("PROJECT=" + project.Path.FullName);
                Console.WriteLine("STATUS=OPEN");
                Console.Out.Flush();
                while (true) Thread.Sleep(1000);
            }
        }
    }
}
