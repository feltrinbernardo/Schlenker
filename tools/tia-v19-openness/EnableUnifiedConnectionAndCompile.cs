using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Siemens.Engineering;
using Siemens.Engineering.Compiler;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.HmiUnified;

namespace Schlenker.TiaV19
{
    internal static class EnableUnifiedConnectionAndCompile
    {
        private static int Main(string[] args)
        {
            if (args.Length < 2) return 2;
            string reportPath = Path.GetFullPath(args[args.Length - 1]);
            string expected = Path.GetFullPath(String.Join(" ", args.Take(args.Length - 1)));
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            try
            {
                TiaPortalProcess process = TiaPortal.GetProcesses().SingleOrDefault(candidate =>
                    candidate.ProjectPath != null && candidate.ProjectPath.FullName.Equals(
                        expected, StringComparison.OrdinalIgnoreCase));
                if (process == null) throw new InvalidOperationException("Exact TIA project is not open.");
                using (TiaPortal portal = process.Attach())
                using (StreamWriter report = new StreamWriter(reportPath, false))
                {
                    Project project = portal.Projects.Single();
                    Location location = FindHmi(project);
                    object connection = location.Hmi.Connections.Cast<object>().Single(item =>
                        String.Equals(Convert.ToString(item.GetType().GetProperty("Name").GetValue(item, null)),
                            "HMI_Connection_1", StringComparison.OrdinalIgnoreCase));
                    PropertyInfo disabled = connection.GetType().GetProperty("DisabledAtStartup");
                    if (disabled == null || !disabled.CanWrite)
                        throw new InvalidOperationException("DisabledAtStartup is not writable.");
                    report.WriteLine("Before.DisabledAtStartup=" + disabled.GetValue(connection, null));
                    disabled.SetValue(connection, true, null);
                    report.WriteLine("After.DisabledAtStartup=" + disabled.GetValue(connection, null));

                    ICompilable compilable = location.Item.GetService<ICompilable>() ??
                        location.Device.GetService<ICompilable>();
                    if (compilable == null) throw new InvalidOperationException("HMI compiler unavailable.");
                    CompilerResult result = compilable.Compile();
                    int nestedErrors = 0;
                    int nestedWarnings = 0;
                    WriteMessages(result.Messages, report, ref nestedErrors, ref nestedWarnings, "");
                    report.WriteLine("ROOT_ERRORS=" + result.ErrorCount);
                    report.WriteLine("ROOT_WARNINGS=" + result.WarningCount);
                    report.WriteLine("NESTED_ERRORS=" + nestedErrors);
                    report.WriteLine("NESTED_WARNINGS=" + nestedWarnings);
                    if (result.ErrorCount != 0 || nestedErrors != 0)
                        throw new InvalidOperationException("HMI compile failed; project not saved.");
                    project.Save();
                    report.WriteLine("STATUS=PASS");
                    Console.WriteLine("DISABLED_AT_STARTUP=" + disabled.GetValue(connection, null));
                    Console.WriteLine("ERRORS=" + result.ErrorCount);
                    Console.WriteLine("WARNINGS=" + result.WarningCount);
                    Console.WriteLine("STATUS=PASS");
                    return 0;
                }
            }
            catch (Exception exception)
            {
                File.AppendAllText(reportPath, Environment.NewLine + "STATUS=FAIL" + Environment.NewLine + exception);
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static void WriteMessages(IEnumerable<CompilerResultMessage> messages,
            StreamWriter report, ref int errors, ref int warnings, string indent)
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

        private sealed class Location
        {
            internal Device Device;
            internal DeviceItem Item;
            internal HmiSoftware Hmi;
        }

        private static Location FindHmi(Project project)
        {
            foreach (Device device in project.Devices)
                foreach (DeviceItem item in Flatten(device.DeviceItems))
                {
                    SoftwareContainer container = item.GetService<SoftwareContainer>();
                    HmiSoftware hmi = container == null ? null : container.Software as HmiSoftware;
                    if (hmi != null) return new Location { Device = device, Item = item, Hmi = hmi };
                }
            throw new InvalidOperationException("Unified HMI not found.");
        }

        private static IEnumerable<DeviceItem> Flatten(DeviceItemComposition items)
        {
            foreach (DeviceItem item in items)
            {
                yield return item;
                foreach (DeviceItem child in Flatten(item.DeviceItems)) yield return child;
            }
        }
    }
}
