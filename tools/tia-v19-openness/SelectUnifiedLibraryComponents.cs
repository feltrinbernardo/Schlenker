using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Siemens.Engineering;
using Siemens.Engineering.Library;

namespace Schlenker.TiaV19
{
    internal static class SelectUnifiedLibraryComponents
    {
        private static int Main(string[] args)
        {
            if (args.Length < 5)
            {
                Console.Error.WriteLine(
                    "Usage: SelectUnifiedLibraryComponents.exe <project hint> <template .al19> <toolbox .zal19> <retrieve dir> <report path>");
                return 2;
            }

            string projectHint = args[0];
            FileInfo templatePath = new FileInfo(Path.GetFullPath(args[1]));
            FileInfo toolboxArchive = new FileInfo(Path.GetFullPath(args[2]));
            DirectoryInfo retrieveDirectory = new DirectoryInfo(Path.GetFullPath(args[3]));
            string reportPath = Path.GetFullPath(args[4]);

            if (!templatePath.Exists || !toolboxArchive.Exists)
            {
                Console.Error.WriteLine("STATUS=FAIL");
                Console.Error.WriteLine("One or both library packages are missing.");
                return 3;
            }

            Directory.CreateDirectory(retrieveDirectory.FullName);
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));

            try
            {
                TiaPortalProcess selected = TiaPortal.GetProcesses().FirstOrDefault(
                    process => process.ProjectPath != null &&
                        process.ProjectPath.FullName.EndsWith(projectHint, StringComparison.OrdinalIgnoreCase));
                if (selected == null)
                {
                    Console.Error.WriteLine("STATUS=FAIL");
                    Console.Error.WriteLine("No open TIA project matched: " + projectHint);
                    return 4;
                }

                using (TiaPortal portal = selected.Attach())
                using (StreamWriter report = new StreamWriter(reportPath, false))
                {
                    report.WriteLine("# WinCC Unified V19 selective library inventory");
                    report.WriteLine("# Generated=" + DateTimeOffset.Now.ToString("o"));
                    report.WriteLine("# Project=" + selected.ProjectPath.FullName);
                    report.WriteLine("# This inventory is read-only; no project object is imported.");

                    UserGlobalLibrary templateLibrary = null;
                    UserGlobalLibrary toolboxLibrary = null;
                    try
                    {
                        templateLibrary = portal.GlobalLibraries.Open(templatePath, OpenMode.ReadOnly);
                        DumpLibrary(templateLibrary, "TEMPLATE_SUITE", report);

                        toolboxLibrary = portal.GlobalLibraries.Retrieve(
                            toolboxArchive,
                            retrieveDirectory,
                            OpenMode.ReadOnly);
                        DumpLibrary(toolboxLibrary, "UNIFIED_TOOLBOX", report);
                    }
                    finally
                    {
                        if (toolboxLibrary != null)
                        {
                            toolboxLibrary.Close();
                        }
                        if (templateLibrary != null)
                        {
                            templateLibrary.Close();
                        }
                    }
                }

                Console.WriteLine("REPORT=" + reportPath);
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

        private static void DumpLibrary(GlobalLibrary library, string source, TextWriter report)
        {
            report.WriteLine("LIBRARY\t{0}\t{1}\t{2}\tREADONLY={3}",
                source,
                Sanitize(library.Name),
                library.Path.FullName,
                library.IsReadOnly);
            DumpTypeFolder((dynamic)library.TypeFolder, source, "Types", report);
            DumpMasterFolder((dynamic)library.MasterCopyFolder, source, "MasterCopies", report);
        }

        private static void DumpTypeFolder(dynamic folder, string source, string path, TextWriter report)
        {
            foreach (dynamic type in folder.Types)
            {
                string typePath = path + "/" + Convert.ToString(type.Name);
                report.WriteLine("TYPE\t{0}\t{1}\tGUID={2}\tTARGET={3}",
                    source,
                    Sanitize(typePath),
                    GetProperty(type, "Guid"),
                    GetProperty(type, "MinimumTargetDeviceVersion"));
                foreach (dynamic version in type.Versions)
                {
                    report.WriteLine("VERSION\t{0}\t{1}\tNUMBER={2}\tSTATE={3}\tDEFAULT={4}\tOBJECT={5}",
                        source,
                        Sanitize(typePath),
                        GetProperty(version, "VersionNumber"),
                        GetProperty(version, "State"),
                        GetProperty(version, "IsDefault"),
                        GetProperty(version, "TypeObject"));
                    foreach (object dependency in AsEnumerable(GetPropertyObject(version, "Dependencies")))
                    {
                        report.WriteLine("DEPENDENCY\t{0}\t{1}\t{2}",
                            source,
                            Sanitize(typePath),
                            Sanitize(DescribeObject(dependency)));
                    }
                }
            }
            foreach (dynamic child in folder.Folders)
            {
                DumpTypeFolder(child, source, path + "/" + Convert.ToString(child.Name), report);
            }
        }

        private static void DumpMasterFolder(dynamic folder, string source, string path, TextWriter report)
        {
            foreach (dynamic masterCopy in folder.MasterCopies)
            {
                string itemPath = path + "/" + Convert.ToString(masterCopy.Name);
                report.WriteLine("MASTER\t{0}\t{1}\tCREATED={2}",
                    source,
                    Sanitize(itemPath),
                    GetProperty(masterCopy, "CreationDate"));
                foreach (dynamic description in masterCopy.ContentDescriptions)
                {
                    report.WriteLine("CONTENT\t{0}\t{1}\tNAME={2}\tTYPE={3}",
                        source,
                        Sanitize(itemPath),
                        Sanitize(Convert.ToString(description.ContentName)),
                        Sanitize(Convert.ToString(description.ContentType)));
                }
            }
            foreach (dynamic child in folder.Folders)
            {
                DumpMasterFolder(child, source, path + "/" + Convert.ToString(child.Name), report);
            }
        }

        private static IEnumerable AsEnumerable(object value)
        {
            return value as IEnumerable ?? new object[0];
        }

        private static object GetPropertyObject(object value, string propertyName)
        {
            if (value == null)
            {
                return null;
            }
            PropertyInfo property = value.GetType().GetProperty(propertyName);
            return property == null ? null : property.GetValue(value, null);
        }

        private static string GetProperty(object value, string propertyName)
        {
            object result = GetPropertyObject(value, propertyName);
            return Sanitize(result == null ? "" : Convert.ToString(result));
        }

        private static string DescribeObject(object value)
        {
            if (value == null)
            {
                return "";
            }
            string[] names = { "Name", "Guid", "VersionNumber", "TypeObject" };
            return value.GetType().FullName + " " + string.Join(";", names.Select(name =>
                name + "=" + GetProperty(value, name)));
        }

        private static string Sanitize(string value)
        {
            return (value ?? "").Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
        }
    }
}
