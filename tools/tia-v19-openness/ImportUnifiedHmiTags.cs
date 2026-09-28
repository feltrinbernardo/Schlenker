using System;
using System.Collections.Generic;
using System.IO;
using Siemens.Engineering;
using Siemens.Engineering.Compiler;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.HmiUnified.HmiTags;

namespace Schlenker.TiaV19
{
    internal static class ImportUnifiedHmiTags
    {
        private static int Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine(
                    "Usage: ImportUnifiedHmiTags <exact-ap19-path> <tag-csv-path> <report-path>");
                return 2;
            }

            string projectPath = Path.GetFullPath(args[0]);
            string csvPath = Path.GetFullPath(args[1]);
            string reportPath = Path.GetFullPath(args[2]);
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));

            try
            {
                using (StreamWriter report = new StreamWriter(reportPath, false))
                using (TiaPortal portal = new TiaPortal(TiaPortalMode.WithoutUserInterface))
                {
                    Project project = portal.Projects.Open(new FileInfo(projectPath));
                    Device hmiDevice;
                    DeviceItem hmiItem;
                    HmiSoftware hmi = FindUnifiedHmi(project, out hmiDevice, out hmiItem);
                    if (hmi == null || hmiItem == null)
                        throw new InvalidOperationException("Unified HMI software was not found.");

                    report.WriteLine("SCHLENKER UNIFIED HMI TAG IMPORT");
                    report.WriteLine("Project=" + project.Path.FullName);
                    report.WriteLine("TagCsv=" + csvPath);
                    report.WriteLine("Timestamp=" + DateTimeOffset.Now.ToString("o"));
                    report.WriteLine("OnlineApisUsed=NO");
                    report.WriteLine("DownloadOrTransferApisUsed=NO");

                    int created;
                    int updated;
                    EnsureHmiTags(hmi, csvPath, out created, out updated);
                    report.WriteLine("TagsCreated=" + created);
                    report.WriteLine("TagsUpdated=" + updated);

                    ICompilable compilable = hmiItem.GetService<ICompilable>() ??
                        hmiDevice.GetService<ICompilable>();
                    if (compilable == null)
                        throw new InvalidOperationException("Unified HMI compile service is unavailable.");

                    CompilerResult result = compilable.Compile();
                    report.WriteLine("RootErrors=" + result.ErrorCount);
                    report.WriteLine("RootWarnings=" + result.WarningCount);
                    WriteMessages(result.Messages, report, "  ");
                    if (result.ErrorCount == 0)
                    {
                        project.Save();
                        report.WriteLine("SaveInvoked=YES");
                        report.WriteLine("STATUS=PASS");
                    }
                    else
                    {
                        report.WriteLine("SaveInvoked=NO");
                        report.WriteLine("STATUS=FAIL_NOT_SAVED");
                    }
                    report.Flush();
                    project.Close();

                    Console.WriteLine("CREATED=" + created);
                    Console.WriteLine("UPDATED=" + updated);
                    Console.WriteLine("ERRORS=" + result.ErrorCount);
                    Console.WriteLine("WARNINGS=" + result.WarningCount);
                    Console.WriteLine("REPORT=" + reportPath);
                    Console.WriteLine("STATUS=" + (result.ErrorCount == 0 ? "PASS" : "FAIL_NOT_SAVED"));
                    return result.ErrorCount == 0 ? 0 : 1;
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

        private static void EnsureHmiTags(HmiSoftware hmi, string csvPath,
            out int created, out int updated)
        {
            if (!File.Exists(csvPath))
                throw new FileNotFoundException("HMI tag CSV was not found.", csvPath);

            HmiTagTable table = hmi.TagTables.Find("REV12") ?? hmi.TagTables.Create("REV12");
            string[] lines = File.ReadAllLines(csvPath);
            created = 0;
            updated = 0;
            for (int index = 1; index < lines.Length; index++)
            {
                if (String.IsNullOrWhiteSpace(lines[index])) continue;
                List<string> fields = ParseCsvLine(lines[index]);
                if (fields.Count < 6)
                    throw new InvalidDataException("Invalid HMI tag row " + (index + 1));

                HmiTag tag = table.Tags.Find(fields[0]);
                if (tag == null)
                {
                    tag = table.Tags.Create(fields[0]);
                    created++;
                }
                else
                {
                    updated++;
                }

                tag.Connection = "HMI_Connection_1";
                tag.PlcTag = fields[1];
                tag.AcquisitionMode = HmiAcquisitionMode.CyclicOnUse;
                tag.AcquisitionCycle = "T1s";
                SetText(tag.Comment,
                    "[" + fields[4] + "] " + fields[5] + " | Intended access: " + fields[3]);
            }
        }

        private static List<string> ParseCsvLine(string line)
        {
            List<string> fields = new List<string>();
            string current = "";
            bool quoted = false;
            for (int index = 0; index < line.Length; index++)
            {
                char ch = line[index];
                if (ch == '"')
                {
                    if (quoted && index + 1 < line.Length && line[index + 1] == '"')
                    {
                        current += '"';
                        index++;
                    }
                    else
                    {
                        quoted = !quoted;
                    }
                }
                else if (ch == ',' && !quoted)
                {
                    fields.Add(current);
                    current = "";
                }
                else
                {
                    current += ch;
                }
            }
            fields.Add(current);
            return fields;
        }

        private static HmiSoftware FindUnifiedHmi(Project project,
            out Device hmiDevice, out DeviceItem hmiItem)
        {
            hmiDevice = null;
            hmiItem = null;
            foreach (Device device in project.Devices)
            {
                DeviceItem candidateItem;
                HmiSoftware hmi = FindUnifiedHmi(device.DeviceItems, out candidateItem);
                if (hmi != null)
                {
                    hmiDevice = device;
                    hmiItem = candidateItem;
                    return hmi;
                }
            }
            return null;
        }

        private static HmiSoftware FindUnifiedHmi(DeviceItemComposition items, out DeviceItem hmiItem)
        {
            hmiItem = null;
            foreach (DeviceItem item in items)
            {
                SoftwareContainer container = item.GetService<SoftwareContainer>();
                HmiSoftware hmi = container == null ? null : container.Software as HmiSoftware;
                if (hmi != null)
                {
                    hmiItem = item;
                    return hmi;
                }

                hmi = FindUnifiedHmi(item.DeviceItems, out hmiItem);
                if (hmi != null) return hmi;
            }
            return null;
        }

        private static void SetText(MultilingualText text, string value)
        {
            string rich = "<body><p>" + value.Replace("&", "&amp;")
                .Replace("<", "&lt;").Replace(">", "&gt;") + "</p></body>";
            foreach (MultilingualTextItem item in text.Items)
                item.Text = rich;
        }

        private static void WriteMessages(IEnumerable<CompilerResultMessage> messages,
            StreamWriter report, string indent)
        {
            foreach (CompilerResultMessage message in messages)
            {
                report.WriteLine(indent + message.State + "\t" + message.Description + "\t" + message.Path);
                WriteMessages(message.Messages, report, indent + "  ");
            }
        }
    }
}
