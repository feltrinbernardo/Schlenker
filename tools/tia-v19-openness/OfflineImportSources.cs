using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.Compiler;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.ExternalSources;

namespace Schlenker.TiaV19
{
    internal static class OfflineImportSources
    {
        private static int Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine("Usage: OfflineImportSources <exact-ap19-path> <source-directory> <report-path>");
                return 2;
            }
            string projectPath = Path.GetFullPath(args[0]);
            string sourceDirectory = Path.GetFullPath(args[1]);
            string reportPath = Path.GetFullPath(args[2]);
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            string[] files = Directory.GetFiles(sourceDirectory, "*.scl", SearchOption.TopDirectoryOnly)
                .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase).ToArray();
            if (files.Length == 0) return 2;

            try
            {
                using (StreamWriter report = new StreamWriter(reportPath, false))
                using (TiaPortal portal = new TiaPortal(TiaPortalMode.WithoutUserInterface))
                {
                    Project project = portal.Projects.Open(new FileInfo(projectPath));
                    PlcSoftware plc = project == null ? null : FindPlc(project);
                    if (plc == null) throw new InvalidOperationException("PLC software not found.");
                    report.WriteLine("SCHLENKER OFFLINE SOURCE IMPORT");
                    report.WriteLine("Project=" + project.Path.FullName);
                    report.WriteLine("SourceDirectory=" + sourceDirectory);
                    report.WriteLine("Timestamp=" + DateTimeOffset.Now.ToString("o"));
                    report.WriteLine("OnlineApisUsed=NO");
                    report.WriteLine("DownloadOrTransferApisUsed=NO");
                    report.WriteLine("CpuControlApisUsed=NO");

                    foreach (string file in files)
                    {
                        string name = Path.GetFileName(file);
                        PlcExternalSource existing = plc.ExternalSourceGroup.ExternalSources.Find(name);
                        if (existing != null) existing.Delete();
                        PlcExternalSource source = plc.ExternalSourceGroup.ExternalSources.CreateFromFile(name, file);
                        source.GenerateBlocksFromSource();
                        report.WriteLine("GENERATED=" + name);
                        report.Flush();
                    }

                    CompilerResult result = plc.GetService<ICompilable>().Compile();
                    int errors = 0;
                    int warnings = 0;
                    WriteMessages(result.Messages, report, ref errors, ref warnings, "");
                    report.WriteLine("ROOT_ERRORS=" + result.ErrorCount);
                    report.WriteLine("ROOT_WARNINGS=" + result.WarningCount);
                    report.WriteLine("NESTED_ERRORS=" + errors);
                    report.WriteLine("NESTED_WARNINGS=" + warnings);
                    if (result.ErrorCount == 0)
                    {
                        project.Save();
                        report.WriteLine("SAVE_INVOKED=YES");
                        report.WriteLine("STATUS=PASS");
                    }
                    else
                    {
                        report.WriteLine("SAVE_INVOKED=NO");
                        report.WriteLine("STATUS=FAIL_NOT_SAVED");
                    }
                    report.Flush();
                    project.Close();
                    Console.WriteLine("SOURCES=" + files.Length);
                    Console.WriteLine("ERRORS=" + result.ErrorCount);
                    Console.WriteLine("WARNINGS=" + result.WarningCount);
                    Console.WriteLine("REPORT=" + reportPath);
                    Console.WriteLine("STATUS=" + (result.ErrorCount == 0 ? "PASS" : "FAIL_NOT_SAVED"));
                    return result.ErrorCount == 0 ? 0 : 1;
                }
            }
            catch (Exception exception)
            {
                File.AppendAllText(reportPath, Environment.NewLine + "STATUS=FAIL" + Environment.NewLine + exception);
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
