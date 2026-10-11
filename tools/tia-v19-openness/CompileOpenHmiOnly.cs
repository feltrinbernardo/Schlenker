using System;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.Compiler;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.SW;

namespace Schlenker.TiaV19
{
    internal static class CompileOpenHmiOnly
    {
        private static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("Usage: CompileOpenHmiOnly <open-project-file-hint> <report-path>");
                return 2;
            }

            string projectHint = args[0];
            string reportPath = Path.GetFullPath(args[1]);
            try
            {
                TiaPortalProcess process = TiaPortal.GetProcesses().SingleOrDefault(candidate =>
                    candidate.ProjectPath != null && candidate.ProjectPath.FullName.EndsWith(
                        projectHint, StringComparison.OrdinalIgnoreCase));
                if (process == null)
                    throw new InvalidOperationException("No unique open TIA project matched " + projectHint + ".");

                Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
                using (TiaPortal portal = process.Attach())
                using (StreamWriter report = new StreamWriter(reportPath, false))
                {
                    Project project = portal.Projects.FirstOrDefault();
                    if (project == null)
                        throw new InvalidOperationException("The selected TIA process has no open project.");

                    Device hmiDevice = FindHmiDevice(project);
                    if (hmiDevice == null)
                        throw new InvalidOperationException("No WinCC Unified HMI device was found.");
                    ICompilable compiler = hmiDevice.GetService<ICompilable>();
                    if (compiler == null)
                        throw new InvalidOperationException("The HMI compiler service is unavailable.");

                    CompilerResult result = compiler.Compile();
                    WriteMessages(report, result.Messages, String.Empty);
                    report.WriteLine("CompileErrors=" + result.ErrorCount);
                    report.WriteLine("CompileWarnings=" + result.WarningCount);
                    report.WriteLine("DownloadOrTransferApisUsed=NO");
                    report.WriteLine("Status=" + (result.ErrorCount == 0 ? "PASS" : "FAIL"));
                    report.Flush();
                    if (result.ErrorCount != 0)
                        throw new InvalidOperationException(
                            "HMI compilation failed with " + result.ErrorCount + " errors.");

                    project.Save();
                    Console.WriteLine("PROJECT=" + project.Path.FullName);
                    Console.WriteLine("HMI_ERRORS=" + result.ErrorCount);
                    Console.WriteLine("HMI_WARNINGS=" + result.WarningCount);
                    Console.WriteLine("DOWNLOAD_OR_TRANSFER=NO");
                    Console.WriteLine("REPORT=" + reportPath);
                    Console.WriteLine("STATUS=PASS");
                }
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("STATUS=FAIL");
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static Device FindHmiDevice(Project project)
        {
            foreach (Device device in project.Devices)
            {
                foreach (DeviceItem item in device.DeviceItems)
                {
                    if (FindHmi(item) != null)
                        return device;
                }
            }
            return null;
        }

        private static HmiSoftware FindHmi(DeviceItem item)
        {
            SoftwareContainer container = item.GetService<SoftwareContainer>();
            HmiSoftware hmi = container == null ? null : container.Software as HmiSoftware;
            if (hmi != null)
                return hmi;
            foreach (DeviceItem child in item.DeviceItems)
            {
                hmi = FindHmi(child);
                if (hmi != null)
                    return hmi;
            }
            return null;
        }

        private static void WriteMessages(
            StreamWriter report, CompilerResultMessageComposition messages, string indent)
        {
            foreach (CompilerResultMessage message in messages)
            {
                report.WriteLine(indent + message.State + " | " + message.Description + " | " + message.Path);
                WriteMessages(report, message.Messages, indent + "  ");
            }
        }
    }
}
