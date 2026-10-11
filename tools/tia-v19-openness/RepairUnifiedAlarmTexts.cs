using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Siemens.Engineering;
using Siemens.Engineering.Compiler;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.HmiUnified.HmiAlarm;
using Siemens.Engineering.HmiUnified.HmiAlarm.HmiAlarmCommon;
using Siemens.Engineering.HmiUnified.UI.Controls;
using Siemens.Engineering.HmiUnified.UI.Enum;
using Siemens.Engineering.HmiUnified.UI.Parts;
using Siemens.Engineering.HmiUnified.UI.Screens;
using Siemens.Engineering.SW;

namespace Schlenker.TiaV19
{
    internal static class RepairUnifiedAlarmTexts
    {
        private sealed class AlarmTextDefinition
        {
            public uint Id;
            public string English;
            public string Italian;
            public string AlarmClass;
            public string Reaction;
        }

        private sealed class AlarmBinding
        {
            public string Name;
            public string TriggerTag;
            public uint Bit;
        }

        private static int Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine(
                    "Usage: RepairUnifiedAlarmTexts <exact-ap19> <alarm-csv> <report>");
                return 2;
            }

            string projectPath = Path.GetFullPath(args[0]);
            string csvPath = Path.GetFullPath(args[1]);
            string reportPath = Path.GetFullPath(args[2]);
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));

            try
            {
                Dictionary<uint, AlarmTextDefinition> definitions = ReadDefinitions(csvPath);
                TiaPortalProcess process = TiaPortal.GetProcesses().SingleOrDefault(candidate =>
                    candidate.ProjectPath != null &&
                    Path.GetFullPath(candidate.ProjectPath.FullName)
                        .Equals(projectPath, StringComparison.OrdinalIgnoreCase));
                if (process == null)
                    throw new InvalidOperationException("The exact TIA project is not open.");

                using (StreamWriter report = new StreamWriter(reportPath, false, Encoding.UTF8))
                using (TiaPortal portal = process.Attach())
                {
                    Project project = portal.Projects.Single();
                    if (!Path.GetFullPath(project.Path.FullName)
                        .Equals(projectPath, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Attached project identity changed.");

                    Device hmiDevice;
                    DeviceItem hmiItem;
                    HmiSoftware hmi = FindUnifiedHmi(project, out hmiDevice, out hmiItem);
                    if (hmi == null || hmiItem == null)
                        throw new InvalidOperationException("Unified HMI was not found.");

                    report.WriteLine("SCHLENKER UNIFIED ALARM TEXT REPAIR");
                    report.WriteLine("Project=" + project.Path.FullName);
                    report.WriteLine("Source=" + csvPath);
                    report.WriteLine("OnlineApisUsed=NO");
                    report.WriteLine("DownloadOrTransferApisUsed=NO");

                    Dictionary<uint, AlarmBinding> requiredBindings =
                        new Dictionary<uint, AlarmBinding>
                        {
                            { 1202U, new AlarmBinding { Name = "ALM_1202_AutoGateSequenceFault", TriggerTag = "Alarm_Word1", Bit = 22U } },
                            { 1303U, new AlarmBinding { Name = "ALM_1303_ProductLevelNotReached", TriggerTag = "Alarm_Word1", Bit = 21U } },
                            { 2104U, new AlarmBinding { Name = "ALM_2104_CIPSequenceFault", TriggerTag = "Alarm_Word1", Bit = 26U } },
                            { 1605U, new AlarmBinding { Name = "ALM_1605_MainDriveMechanicalJam", TriggerTag = "Alarm_Word2", Bit = 19U } },
                            { 1606U, new AlarmBinding { Name = "ALM_1606_MainDriveCommFault", TriggerTag = "Alarm_Word2", Bit = 20U } },
                            { 1607U, new AlarmBinding { Name = "ALM_1607_ConveyorDriveCommFault", TriggerTag = "Alarm_Word2", Bit = 21U } },
                            { 1608U, new AlarmBinding { Name = "ALM_1608_ProductPumpCommFault", TriggerTag = "Alarm_Word2", Bit = 22U } },
                            { 1609U, new AlarmBinding { Name = "ALM_1609_CapDriveCommFault", TriggerTag = "Alarm_Word2", Bit = 23U } }
                        };
                    foreach (KeyValuePair<uint, AlarmBinding> pair in requiredBindings)
                    {
                        AlarmTextDefinition definition = definitions[pair.Key];
                        HmiDiscreteAlarm alarm = hmi.DiscreteAlarms
                            .FirstOrDefault(candidate => candidate.Id == pair.Key);
                        if (alarm == null)
                        {
                            alarm = hmi.DiscreteAlarms.Create(pair.Value.Name);
                            report.WriteLine("CREATED_ALARM id=" + pair.Key +
                                "; name=" + pair.Value.Name);
                        }
                        alarm.Id = pair.Key;
                        alarm.AlarmClass = definition.AlarmClass;
                        alarm.RaisedStateTag = pair.Value.TriggerTag;
                        alarm.RaisedStateTagBitNumber = pair.Value.Bit;
                        alarm.TriggerMode = HmiDiscreteAlarmTriggerMode.OnRisingEdge;
                        foreach (MultilingualTextItem item in alarm.InfoText.Items)
                            item.Text = ToRichText(definition.Reaction);
                    }

                    int matched = 0;
                    int updatedLanguageItems = 0;
                    foreach (HmiDiscreteAlarm alarm in hmi.DiscreteAlarms)
                    {
                        AlarmTextDefinition definition;
                        if (!definitions.TryGetValue(alarm.Id, out definition))
                            continue;
                        matched++;
                        foreach (MultilingualTextItem item in alarm.EventText1.Items)
                        {
                            string culture = item.Language.Culture.Name;
                            string expected = item.Language.Culture.TwoLetterISOLanguageName
                                .Equals("it", StringComparison.OrdinalIgnoreCase)
                                ? definition.Italian : definition.English;
                            string before = item.Text ?? String.Empty;
                            item.Text = ToRichText(expected);
                            updatedLanguageItems++;
                            report.WriteLine(
                                "ALARM id={0}; name={1}; language={2}; beforeEmpty={3}; text={4}",
                                alarm.Id, alarm.Name, culture,
                                String.IsNullOrWhiteSpace(before), expected);
                        }
                        foreach (MultilingualTextItem item in alarm.InfoText.Items)
                            item.Text = ToRichText(definition.Reaction);
                    }

                    report.WriteLine("Definitions=" + definitions.Count);
                    report.WriteLine("MatchedAlarms=" + matched);
                    report.WriteLine("UpdatedLanguageItems=" + updatedLanguageItems);
                    if (matched == 0 || updatedLanguageItems == 0)
                        throw new InvalidOperationException(
                            "No existing Unified alarm text resource matched the approved alarm list.");

                    HmiScreen alarmsScreen = hmi.Screens.Find("alarms");
                    if (alarmsScreen == null)
                        throw new InvalidOperationException("The alarms screen was not found.");
                    HmiAlarmControl alarmControl = alarmsScreen.ScreenItems.Find(
                        "REV12_Active_Alarm_Control") as HmiAlarmControl;
                    if (alarmControl == null)
                        throw new InvalidOperationException("The active-alarm control was not found.");
                    List<HmiAlarmColumnPart> alarmColumns = alarmControl.AlarmView.Columns
                        .OfType<HmiAlarmColumnPart>().ToList();
                    foreach (HmiAlarmColumnPart column in alarmColumns)
                        report.WriteLine("COLUMN_BEFORE name=" + column.Name +
                            "; block=" + column.AlarmBlock +
                            "; visible=" + column.Visible +
                            "; width=" + column.Width);

                    // Composition order is fixed by WinCC Unified. Bind the first text
                    // slot to the alarm description and the last text slot to the
                    // operator action so the visual order is deterministic and
                    // idempotent on every rerun.
                    List<HmiAlarmColumnPart> operatorTextColumns = alarmColumns
                        .Where(column => column.AlarmBlock == HmiAlarmBlock.AlarmText1 ||
                                         column.AlarmBlock == HmiAlarmBlock.Information)
                        .ToList();
                    if (operatorTextColumns.Count < 2)
                        throw new InvalidOperationException(
                            "Two operator text columns are required for description and action.");
                    HmiAlarmColumnPart alarmTextColumn = operatorTextColumns.First();
                    HmiAlarmColumnPart detailColumn = operatorTextColumns.Last();
                    if (Object.ReferenceEquals(alarmTextColumn, detailColumn))
                        throw new InvalidOperationException(
                            "Description and action must use separate alarm columns.");
                    string operatorFilter = "ID >= 1000 AND ID <= 2999";
                    alarmControl.Filter = operatorFilter;
                    if (!String.Equals(alarmControl.Filter, operatorFilter,
                            StringComparison.Ordinal))
                        throw new InvalidOperationException(
                            "The operator alarm filter was rejected by WinCC Unified.");
                    alarmControl.AlarmView.AllowFilter = true;
                    alarmTextColumn.AlarmBlock = HmiAlarmBlock.AlarmText1;
                    alarmTextColumn.Visible = true;
                    alarmTextColumn.Enabled = true;
                    alarmTextColumn.AllowSort = true;
                    alarmTextColumn.UseAlarmColors = true;
                    alarmTextColumn.Width = 300;
                    alarmTextColumn.MinimumWidth = 180;
                    alarmTextColumn.MaximumWidth = 520;
                    foreach (MultilingualTextItem item in alarmTextColumn.Header.Text.Items)
                        item.Text = ToRichText("ALARM DESCRIPTION");
                    detailColumn.AlarmBlock = HmiAlarmBlock.Information;
                    detailColumn.Visible = true;
                    detailColumn.Enabled = true;
                    detailColumn.AllowSort = true;
                    detailColumn.UseAlarmColors = true;
                    detailColumn.Width = 330;
                    detailColumn.MinimumWidth = 200;
                    detailColumn.MaximumWidth = 520;
                    foreach (MultilingualTextItem item in detailColumn.Header.Text.Items)
                        item.Text = ToRichText("DETAIL / ACTION");

                    // EventText duplicates the operator-facing alarm wording in this
                    // project. Keep the underlying data intact, but hide every such
                    // legacy column from the active-alarm table.
                    foreach (HmiAlarmColumnPart column in alarmColumns
                        .Where(candidate => candidate.AlarmBlock == HmiAlarmBlock.EventText))
                    {
                        column.Visible = false;
                        column.Enabled = false;
                    }

                    Dictionary<HmiAlarmBlock, uint> compactWidths =
                        new Dictionary<HmiAlarmBlock, uint>
                        {
                            { HmiAlarmBlock.ID, 60U },
                            { HmiAlarmBlock.Class, 90U },
                            { HmiAlarmBlock.RaiseTime, 155U },
                            { HmiAlarmBlock.AcknowledgmentState, 100U },
                            { HmiAlarmBlock.StateText, 90U }
                        };
                    foreach (HmiAlarmColumnPart column in alarmColumns)
                    {
                        uint width;
                        if (!compactWidths.TryGetValue(column.AlarmBlock, out width))
                            continue;
                        column.Width = width;
                        column.MinimumWidth = 55;
                        column.MaximumWidth = 240;
                    }
                    foreach (HmiAlarmColumnPart column in alarmColumns)
                        report.WriteLine("COLUMN_AFTER name=" + column.Name +
                            "; block=" + column.AlarmBlock +
                            "; visible=" + column.Visible +
                            "; width=" + column.Width);
                    report.WriteLine("ActiveAlarmColumns=AlarmText1,Information");
                    report.WriteLine("LegacyEventTextColumnsVisible=NO");
                    report.WriteLine("VisualTextOrder=ALARM DESCRIPTION,DETAIL / ACTION");
                    report.WriteLine("OperatorAlarmFilter=" + alarmControl.Filter);
                    report.WriteLine("GenericSystemDiagnosticsVisibleInOperatorList=NO");

                    ICompilable compilable = hmiItem.GetService<ICompilable>() ??
                        hmiDevice.GetService<ICompilable>();
                    if (compilable == null)
                        throw new InvalidOperationException("Unified HMI compiler service was not found.");
                    CompilerResult result = compilable.Compile();
                    int nestedErrors = 0;
                    int nestedWarnings = 0;
                    WriteMessages(result.Messages, report, String.Empty,
                        ref nestedErrors, ref nestedWarnings);
                    report.WriteLine("RootErrors=" + result.ErrorCount);
                    report.WriteLine("RootWarnings=" + result.WarningCount);
                    report.WriteLine("NestedErrors=" + nestedErrors);
                    report.WriteLine("NestedWarnings=" + nestedWarnings);
                    if (result.ErrorCount != 0 || nestedErrors != 0)
                    {
                        report.WriteLine("SaveInvoked=NO");
                        report.WriteLine("STATUS=FAIL_NOT_SAVED");
                        return 1;
                    }

                    project.Save();
                    report.WriteLine("SaveInvoked=YES");
                    report.WriteLine("STATUS=PASS");
                    report.Flush();
                    Console.WriteLine("MATCHED_ALARMS=" + matched);
                    Console.WriteLine("UPDATED_LANGUAGE_ITEMS=" + updatedLanguageItems);
                    Console.WriteLine("ERRORS=0");
                    Console.WriteLine("WARNINGS=" + (result.WarningCount + nestedWarnings));
                    Console.WriteLine("REPORT=" + reportPath);
                    Console.WriteLine("STATUS=PASS");
                }
                return 0;
            }
            catch (Exception exception)
            {
                File.AppendAllText(reportPath,
                    Environment.NewLine + "STATUS=FAIL" + Environment.NewLine +
                    exception + Environment.NewLine);
                Console.Error.WriteLine("STATUS=FAIL");
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static Dictionary<uint, AlarmTextDefinition> ReadDefinitions(string path)
        {
            Dictionary<uint, AlarmTextDefinition> result =
                new Dictionary<uint, AlarmTextDefinition>();
            foreach (string line in File.ReadLines(path).Skip(1))
            {
                if (String.IsNullOrWhiteSpace(line)) continue;
                List<string> fields = ParseCsvLine(line);
                uint id;
                if (fields.Count < 3 || !UInt32.TryParse(fields[0],
                    NumberStyles.None, CultureInfo.InvariantCulture, out id))
                    continue;
                result[id] = new AlarmTextDefinition
                {
                    Id = id,
                    English = fields[1].Trim(),
                    Italian = fields[2].Trim(),
                    AlarmClass = fields.Count > 3 ? fields[3].Trim() : "Critical",
                    Reaction = fields.Count > 4 ? fields[4].Trim() : String.Empty
                };
            }
            return result;
        }

        private static List<string> ParseCsvLine(string line)
        {
            List<string> fields = new List<string>();
            StringBuilder field = new StringBuilder();
            bool quoted = false;
            for (int index = 0; index < line.Length; index++)
            {
                char value = line[index];
                if (value == '"')
                {
                    if (quoted && index + 1 < line.Length && line[index + 1] == '"')
                    {
                        field.Append('"');
                        index++;
                    }
                    else quoted = !quoted;
                }
                else if (value == ',' && !quoted)
                {
                    fields.Add(field.ToString());
                    field.Clear();
                }
                else field.Append(value);
            }
            fields.Add(field.ToString());
            return fields;
        }

        private static string ToRichText(string value)
        {
            return "<body><p>" + value.Replace("&", "&amp;")
                .Replace("<", "&lt;").Replace(">", "&gt;") + "</p></body>";
        }

        private static HmiSoftware FindUnifiedHmi(
            Project project, out Device hmiDevice, out DeviceItem hmiItem)
        {
            foreach (Device device in project.Devices)
            {
                foreach (DeviceItem item in device.DeviceItems)
                {
                    DeviceItem foundItem;
                    HmiSoftware hmi = FindUnifiedHmi(item, out foundItem);
                    if (hmi != null)
                    {
                        hmiDevice = device;
                        hmiItem = foundItem;
                        return hmi;
                    }
                }
            }
            hmiDevice = null;
            hmiItem = null;
            return null;
        }

        private static HmiSoftware FindUnifiedHmi(DeviceItem item, out DeviceItem foundItem)
        {
            SoftwareContainer container = item.GetService<SoftwareContainer>();
            HmiSoftware hmi = container == null ? null : container.Software as HmiSoftware;
            if (hmi != null)
            {
                foundItem = item;
                return hmi;
            }
            foreach (DeviceItem child in item.DeviceItems)
            {
                hmi = FindUnifiedHmi(child, out foundItem);
                if (hmi != null) return hmi;
            }
            foundItem = null;
            return null;
        }

        private static void WriteMessages(
            CompilerResultMessageComposition messages, StreamWriter report,
            string indent, ref int errors, ref int warnings)
        {
            foreach (CompilerResultMessage message in messages)
            {
                errors += message.ErrorCount;
                warnings += message.WarningCount;
                report.WriteLine(indent + message.State + " | " +
                    message.Description + " | " + message.Path);
                WriteMessages(message.Messages, report, indent + "  ",
                    ref errors, ref warnings);
            }
        }
    }
}
