using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.Compiler;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.SW.ExternalSources;

namespace Schlenker.TiaV19
{
    internal static class ImportRev12Sources
    {
        private static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Usage: ImportRev12Sources.exe <project-file-name> <source-directory> [report-path]");
                return 2;
            }

            string projectFileName = args[0];
            string sourceDirectory = Path.GetFullPath(args[1]);
            string reportPath = args.Length > 2
                ? Path.GetFullPath(args[2])
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ImportRev12Sources.report.txt");

            if (!Directory.Exists(sourceDirectory))
            {
                Console.Error.WriteLine("Source directory not found: " + sourceDirectory);
                return 2;
            }

            string[] files = Directory.GetFiles(sourceDirectory, "*.scl", SearchOption.TopDirectoryOnly)
                .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (files.Length == 0)
            {
                Console.Error.WriteLine("No SCL files found.");
                return 2;
            }

            try
            {
                string projectStem = Path.GetFileNameWithoutExtension(projectFileName);
                TiaPortalProcess[] openProjects = TiaPortal.GetProcesses()
                    .Where(p => p.ProjectPath != null).ToArray();
                TiaPortalProcess process = openProjects.FirstOrDefault(p =>
                    p.ProjectPath.FullName.IndexOf(projectStem, StringComparison.OrdinalIgnoreCase) >= 0);
                if (process == null)
                {
                    throw new InvalidOperationException("The requested TIA project is not open: " + projectFileName +
                        ". Open projects: " + string.Join("; ", openProjects.Select(p => p.ProjectPath.FullName)));
                }

                using (StreamWriter report = new StreamWriter(reportPath, false))
                using (TiaPortal portal = process.Attach())
                {
                    Project project = portal.Projects.FirstOrDefault();
                    PlcSoftware plc = project == null ? null : FindPlc(project);
                    if (plc == null) throw new InvalidOperationException("PLC software not found.");

                    report.WriteLine("REV12.1 OFFLINE SOURCE IMPORT");
                    report.WriteLine("Project=" + project.Path.FullName);
                    report.WriteLine("SourceDirectory=" + sourceDirectory);
                    report.WriteLine("No online, download, CPU-control, or device-memory APIs are used.");

                    foreach (string file in files)
                    {
                        string name = Path.GetFileName(file);
                        PlcExternalSource existing = plc.ExternalSourceGroup.ExternalSources.Find(name);
                        if (existing != null) existing.Delete();
                        PlcExternalSource source = plc.ExternalSourceGroup.ExternalSources.CreateFromFile(name, file);
                        source.GenerateBlocksFromSource();
                        report.WriteLine("GENERATED=" + name);
                    }

                    ICompilable compilable = plc.GetService<ICompilable>();
                    CompilerResult result = compilable.Compile();
                    int errors = 0;
                    int warnings = 0;
                    WriteMessages(result.Messages, report, ref errors, ref warnings, "");
                    report.WriteLine("ERRORS=" + errors);
                    report.WriteLine("WARNINGS=" + warnings);

                    project.Save();
                    Console.WriteLine("SOURCES=" + files.Length);
                    Console.WriteLine("ERRORS=" + errors);
                    Console.WriteLine("WARNINGS=" + warnings);
                    Console.WriteLine("REPORT=" + reportPath);
                    Console.WriteLine("STATUS=" + (errors == 0 ? "PASS" : "FAIL"));
                    return errors == 0 ? 0 : 1;
                }
            }
            catch (Exception exception)
            {
                File.WriteAllText(reportPath, exception.ToString());
                Console.Error.WriteLine("STATUS=FAIL");
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static void WriteMessages(IEnumerable<CompilerResultMessage> messages, StreamWriter report,
            ref int errors, ref int warnings, string indent)
        {
            foreach (CompilerResultMessage message in messages)
            {
                string state = message.State.ToString();
                if (state.Equals("Error", StringComparison.OrdinalIgnoreCase)) errors++;
                if (state.Equals("Warning", StringComparison.OrdinalIgnoreCase)) warnings++;
                report.WriteLine(indent + state + "\t" + message.Description + "\t" + message.Path);
                WriteMessages(message.Messages, report, ref errors, ref warnings, indent + "  ");
            }
        }

        private static PlcSoftware FindPlc(Project project)
        {
            foreach (Device device in project.Devices)
            {
                PlcSoftware plc = FindPlc(device.DeviceItems);
                if (plc != null) return plc;
            }
            return null;
        }

        private static PlcSoftware FindPlc(DeviceItemComposition items)
        {
            foreach (DeviceItem item in items)
            {
                SoftwareContainer container = item.GetService<SoftwareContainer>();
                PlcSoftware plc = container == null ? null : container.Software as PlcSoftware;
                if (plc != null) return plc;
                plc = FindPlc(item.DeviceItems);
                if (plc != null) return plc;
            }
            return null;
        }
    }
}
