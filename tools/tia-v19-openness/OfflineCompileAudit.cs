using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.Compiler;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;

namespace Schlenker.TiaV19
{
    internal static class OfflineCompileAudit
    {
        private sealed class SoftwareLocation
        {
            public Device Device;
            public DeviceItem Item;
            public PlcSoftware Plc;
            public HmiSoftware Hmi;
        }

        private sealed class Counts
        {
            public int Errors;
            public int Warnings;
            public int Info;
        }

        private static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("Usage: OfflineCompileAudit <exact-ap19-path> <report-path>");
                return 2;
            }

            string projectPath = Path.GetFullPath(args[0]);
            string reportPath = Path.GetFullPath(args[1]);
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));

            try
            {
                using (StreamWriter report = new StreamWriter(reportPath, false))
                using (TiaPortal portal = new TiaPortal(TiaPortalMode.WithoutUserInterface))
                {
                    Project project = portal.Projects.Open(new FileInfo(projectPath));
                    if (project == null)
                        throw new InvalidOperationException("TIA V19 did not open the requested project.");

                    string actualPath = Path.GetFullPath(project.Path.FullName);
                    if (!actualPath.Equals(projectPath, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Attached project path changed: " + actualPath);

                    report.WriteLine("SCHLENKER OFFLINE BASELINE COMPILE AND INVENTORY");
                    report.WriteLine("Project=" + actualPath);
                    report.WriteLine("PortalMode=WithoutUserInterface");
                    report.WriteLine("LocalTimestamp=" + DateTimeOffset.Now.ToString("o"));
                    report.WriteLine("UtcTimestamp=" + DateTimeOffset.UtcNow.ToString("o"));
                    report.WriteLine("Mode=OFFLINE ONLY");
                    report.WriteLine("OnlineApisUsed=NO");
                    report.WriteLine("DownloadOrTransferApisUsed=NO");
                    report.WriteLine("CpuControlApisUsed=NO");
                    report.WriteLine("DeviceMemoryWriteApisUsed=NO");

                    List<SoftwareLocation> locations = new List<SoftwareLocation>();
                    report.WriteLine("Devices=" + project.Devices.Count);
                    foreach (Device device in project.Devices)
                    {
                        report.WriteLine("DEVICE\t" + Clean(device.Name));
                        InspectItems(device, device.DeviceItems, "  ", locations, report);
                    }

                    SoftwareLocation plcLocation = locations.SingleOrDefault(location => location.Plc != null);
                    SoftwareLocation hmiLocation = locations.SingleOrDefault(location => location.Hmi != null);
                    if (plcLocation == null)
                        throw new InvalidOperationException("Expected one PLC software container.");
                    if (hmiLocation == null)
                        throw new InvalidOperationException("Expected one HMI software container.");

                    report.WriteLine("PLCBlocks=" + CountBlocks(plcLocation.Plc.BlockGroup));
                    report.WriteLine("PLCTagTables=" + CountPlcTagTables(plcLocation.Plc));
                    report.WriteLine("HMITagTables=" + hmiLocation.Hmi.TagTables.Count);
                    report.WriteLine("HMITags=" + hmiLocation.Hmi.TagTables.Sum(table => table.Tags.Count));
                    report.WriteLine("HMIScreens=" + hmiLocation.Hmi.Screens.Count);
                    report.Flush();

                    Counts plcCounts = Compile("PLC_SOFTWARE_REBUILD", plcLocation.Plc.GetService<ICompilable>(), report);
                    Counts hardwareCounts = Compile("PLC_HARDWARE_COMPILE", plcLocation.Device.GetService<ICompilable>(), report);
                    ICompilable hmiCompilable = hmiLocation.Item.GetService<ICompilable>() ??
                        hmiLocation.Device.GetService<ICompilable>();
                    Counts hmiCounts = Compile("HMI_FULL_REBUILD", hmiCompilable, report);

                    int totalErrors = plcCounts.Errors + hardwareCounts.Errors + hmiCounts.Errors;
                    int totalWarnings = plcCounts.Warnings + hardwareCounts.Warnings + hmiCounts.Warnings;
                    report.WriteLine("TOTAL_ERRORS=" + totalErrors);
                    report.WriteLine("TOTAL_WARNINGS=" + totalWarnings);
                    report.WriteLine("STATUS=" + (totalErrors == 0 ? "PASS" : "FAIL"));
                    report.Flush();

                    project.Save();
                    Console.WriteLine("PROJECT=" + actualPath);
                    Console.WriteLine("PLC_ERRORS=" + plcCounts.Errors);
                    Console.WriteLine("PLC_WARNINGS=" + plcCounts.Warnings);
                    Console.WriteLine("HARDWARE_ERRORS=" + hardwareCounts.Errors);
                    Console.WriteLine("HARDWARE_WARNINGS=" + hardwareCounts.Warnings);
                    Console.WriteLine("HMI_ERRORS=" + hmiCounts.Errors);
                    Console.WriteLine("HMI_WARNINGS=" + hmiCounts.Warnings);
                    Console.WriteLine("REPORT=" + reportPath);
                    Console.WriteLine("STATUS=" + (totalErrors == 0 ? "PASS" : "FAIL"));
                    return totalErrors == 0 ? 0 : 1;
                }
            }
            catch (Exception exception)
            {
                File.AppendAllText(reportPath,
                    Environment.NewLine + "STATUS=FAIL" + Environment.NewLine + exception + Environment.NewLine);
                Console.Error.WriteLine("STATUS=FAIL");
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static void InspectItems(Device device, DeviceItemComposition items, string indent,
            IList<SoftwareLocation> locations, StreamWriter report)
        {
            foreach (DeviceItem item in items)
            {
                report.WriteLine(indent + "ITEM\t" + Clean(item.Name) + "\t" +
                    Clean(item.TypeIdentifier) + "\tPosition=" + Clean(item.PositionNumber));
                foreach (HwIdentifier identifier in item.HwIdentifiers)
                    report.WriteLine(indent + "  HW_ID\t" + identifier.Identifier);
                foreach (Address address in item.Addresses)
                    report.WriteLine(indent + "  ADDRESS\t" + address.IoType + "\tStart=" +
                        address.StartAddress + "\tLength=" + address.Length);

                SoftwareContainer container = item.GetService<SoftwareContainer>();
                if (container != null)
                {
                    PlcSoftware plc = container.Software as PlcSoftware;
                    HmiSoftware hmi = container.Software as HmiSoftware;
                    if (plc != null || hmi != null)
                    {
                        locations.Add(new SoftwareLocation
                        {
                            Device = device,
                            Item = item,
                            Plc = plc,
                            Hmi = hmi
                        });
                        report.WriteLine(indent + "  SOFTWARE\t" + Clean(container.Software.Name) +
                            "\t" + container.Software.GetType().Name);
                    }
                }
                InspectItems(device, item.DeviceItems, indent + "  ", locations, report);
            }
        }

        private static Counts Compile(string label, ICompilable compilable, StreamWriter report)
        {
            if (compilable == null)
                throw new InvalidOperationException(label + " compile service is unavailable.");
            report.WriteLine(label + "_START_LOCAL=" + DateTimeOffset.Now.ToString("o"));
            report.Flush();
            CompilerResult result = compilable.Compile();
            Counts counts = new Counts();
            WriteMessages(result.Messages, report, counts, "  ");
            report.WriteLine(label + "_ERRORS=" + counts.Errors);
            report.WriteLine(label + "_WARNINGS=" + counts.Warnings);
            report.WriteLine(label + "_INFO=" + counts.Info);
            report.WriteLine(label + "_END_LOCAL=" + DateTimeOffset.Now.ToString("o"));
            report.Flush();
            return counts;
        }

        private static void WriteMessages(IEnumerable<CompilerResultMessage> messages, StreamWriter report,
            Counts counts, string indent)
        {
            foreach (CompilerResultMessage message in messages)
            {
                string state = message.State.ToString();
                if (state.Equals("Error", StringComparison.OrdinalIgnoreCase)) counts.Errors++;
                else if (state.Equals("Warning", StringComparison.OrdinalIgnoreCase)) counts.Warnings++;
                else counts.Info++;
                report.WriteLine(indent + state + "\t" + Clean(message.Description) + "\t" + Clean(message.Path));
                WriteMessages(message.Messages, report, counts, indent + "  ");
            }
        }

        private static int CountBlocks(PlcBlockGroup group)
        {
            int count = group.Blocks.Count;
            foreach (PlcBlockUserGroup child in group.Groups)
                count += CountBlocks(child);
            return count;
        }

        private static int CountBlocks(PlcBlockUserGroup group)
        {
            int count = group.Blocks.Count;
            foreach (PlcBlockUserGroup child in group.Groups)
                count += CountBlocks(child);
            return count;
        }

        private static int CountPlcTagTables(PlcSoftware plc)
        {
            int count = plc.TagTableGroup.TagTables.Count;
            foreach (Siemens.Engineering.SW.Tags.PlcTagTableUserGroup child in plc.TagTableGroup.Groups)
                count += CountPlcTagTables(child);
            return count;
        }

        private static int CountPlcTagTables(Siemens.Engineering.SW.Tags.PlcTagTableUserGroup group)
        {
            int count = group.TagTables.Count;
            foreach (Siemens.Engineering.SW.Tags.PlcTagTableUserGroup child in group.Groups)
                count += CountPlcTagTables(child);
            return count;
        }

        private static string Clean(object value)
        {
            return value == null ? String.Empty : value.ToString().Replace('\r', ' ').Replace('\n', ' ');
        }
    }
}
