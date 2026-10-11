using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.Compiler;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.SW;

namespace Schlenker.TiaV19
{
    internal static class AttachCompilePlc
    {
        private static int Main(string[] args)
        {
            if (args.Length != 2) return 2;
            string expected = Path.GetFullPath(args[0]);
            string reportPath = Path.GetFullPath(args[1]);
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            try
            {
                TiaPortalProcess process = TiaPortal.GetProcesses().SingleOrDefault(p =>
                    p.ProjectPath != null && p.ProjectPath.FullName.Equals(expected,
                        StringComparison.OrdinalIgnoreCase));
                if (process == null) throw new InvalidOperationException("Exact project is not open.");
                using (StreamWriter report = new StreamWriter(reportPath, false))
                using (TiaPortal portal = process.Attach())
                {
                    Project project = portal.Projects.Single();
                    Device plcDevice = project.Devices.Single(d =>
                        d.Name.Equals("S7-1500/ET200MP station_1", StringComparison.OrdinalIgnoreCase));
                    PlcSoftware plc = FindPlc(plcDevice.DeviceItems);
                    if (plc == null) throw new InvalidOperationException("PLC software not found.");
                    report.WriteLine("MODE=ATTACHED_OFFLINE_COMPILE_ONLY");
                    report.WriteLine("PROJECT=" + project.Path.FullName);
                    int errors = 0, warnings = 0;
                    Compile("PLC_SOFTWARE", plc.GetService<ICompilable>(), report, ref errors, ref warnings);
                    Compile("PLC_HARDWARE", plcDevice.GetService<ICompilable>(), report, ref errors, ref warnings);
                    report.WriteLine("ERRORS=" + errors);
                    report.WriteLine("WARNINGS=" + warnings);
                    report.WriteLine("STATUS=" + (errors == 0 ? "PASS" : "FAIL"));
                    report.Flush();
                    if (errors == 0) project.Save();
                    Console.WriteLine("ERRORS=" + errors);
                    Console.WriteLine("WARNINGS=" + warnings);
                    Console.WriteLine("REPORT=" + reportPath);
                    Console.WriteLine("STATUS=" + (errors == 0 ? "PASS" : "FAIL"));
                    return errors == 0 ? 0 : 1;
                }
            }
            catch (Exception exception)
            {
                File.AppendAllText(reportPath, Environment.NewLine + "STATUS=FAIL" +
                    Environment.NewLine + exception + Environment.NewLine);
                Console.Error.WriteLine("STATUS=FAIL");
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static void Compile(string label, ICompilable compilable, StreamWriter report,
            ref int errors, ref int warnings)
        {
            if (compilable == null) throw new InvalidOperationException(label + " compiler missing.");
            CompilerResult result = compilable.Compile();
            report.WriteLine(label + "_ROOT_ERRORS=" + result.ErrorCount);
            report.WriteLine(label + "_ROOT_WARNINGS=" + result.WarningCount);
            Write(result.Messages, report, ref errors, ref warnings, "  ");
        }

        private static void Write(IEnumerable<CompilerResultMessage> messages, StreamWriter report,
            ref int errors, ref int warnings, string indent)
        {
            foreach (CompilerResultMessage message in messages)
            {
                string state = message.State.ToString();
                if (state.Equals("Error", StringComparison.OrdinalIgnoreCase)) errors++;
                if (state.Equals("Warning", StringComparison.OrdinalIgnoreCase)) warnings++;
                report.WriteLine(indent + state + "\t" + message.Description + "\t" + message.Path);
                Write(message.Messages, report, ref errors, ref warnings, indent + "  ");
            }
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
