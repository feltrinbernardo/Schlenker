using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Siemens.Engineering;
using Siemens.Engineering.Compiler;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.HmiUnified.UI.Controls;
using Siemens.Engineering.HmiUnified.UI.Dynamization;
using Siemens.Engineering.HmiUnified.UI.Enum;
using Siemens.Engineering.HmiUnified.UI.Events;
using Siemens.Engineering.HmiUnified.UI.Parts;
using Siemens.Engineering.HmiUnified.UI.Screens;
using Siemens.Engineering.HmiUnified.UI.Widgets;
using Siemens.Engineering.SW;

namespace Schlenker.TiaV19
{
    internal static class BuildConveyorSettingsOffline
    {
        private static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine(
                    "Usage: BuildConveyorSettingsOffline <exact-ap19-path> <report-path>");
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
                    Device hmiDevice;
                    DeviceItem hmiItem;
                    HmiSoftware hmi = FindUnifiedHmi(project, out hmiDevice, out hmiItem);
                    if (hmi == null || hmiItem == null)
                        throw new InvalidOperationException("Unified HMI software was not found.");

                    report.WriteLine("SCHLENKER CONVEYOR SETTINGS OFFLINE BUILD");
                    report.WriteLine("Project=" + project.Path.FullName);
                    report.WriteLine("Timestamp=" + DateTimeOffset.Now.ToString("o"));
                    report.WriteLine("OnlineApisUsed=NO");
                    report.WriteLine("DownloadOrTransferApisUsed=NO");

                    MethodInfo builder = typeof(BuildUnifiedHomeScreen).GetMethod(
                        "BuildConveyorSettings",
                        BindingFlags.NonPublic | BindingFlags.Static);
                    if (builder == null)
                        throw new MissingMethodException(
                            "BuildUnifiedHomeScreen.BuildConveyorSettings was not found.");
                    builder.Invoke(null, new object[] { hmi });

                    AuditScreen(hmi, report);

                    ICompilable compilable = hmiItem.GetService<ICompilable>() ??
                        hmiDevice.GetService<ICompilable>();
                    if (compilable == null)
                        throw new InvalidOperationException(
                            "Unified HMI compile service is unavailable.");

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

                    Console.WriteLine("ERRORS=" + result.ErrorCount);
                    Console.WriteLine("WARNINGS=" + result.WarningCount);
                    Console.WriteLine("REPORT=" + reportPath);
                    Console.WriteLine("STATUS=" +
                        (result.ErrorCount == 0 ? "PASS" : "FAIL_NOT_SAVED"));
                    return result.ErrorCount == 0 ? 0 : 1;
                }
            }
            catch (TargetInvocationException exception)
            {
                Exception root = exception.InnerException ?? exception;
                File.AppendAllText(reportPath,
                    Environment.NewLine + "STATUS=FAIL" + Environment.NewLine +
                    root + Environment.NewLine);
                Console.Error.WriteLine("STATUS=FAIL");
                Console.Error.WriteLine(root);
                return 1;
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

        private static void AuditScreen(HmiSoftware hmi, StreamWriter report)
        {
            HmiScreen screen = hmi.Screens.Find("conveyor_settings");
            if (screen == null)
                throw new InvalidOperationException(
                    "The conveyor_settings screen was not created.");

            if (screen.ScreenItems.Select(item => item.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase).Count() !=
                screen.ScreenItems.Count)
                throw new InvalidOperationException(
                    "The conveyor_settings screen contains duplicate object names.");

            string[] bindings =
            {
                "ROB_Conveyor_Base_Value|Conveyor_Base_Speed_Pct",
                "ROB_Conveyor_Offset_Value|Conveyor_Speed_Offset_Pct",
                "REV12_TimerValue_ConveyorPrestart|Conveyor_Prestart_Time",
                "REV12_TimerValue_ConveyorRunOn|Conveyor_RunOn_Time"
            };
            foreach (string entry in bindings)
            {
                string[] parts = entry.Split('|');
                HmiIOField field = screen.ScreenItems.Find(parts[0]) as HmiIOField;
                TagDynamization binding = field == null ? null :
                    field.Dynamizations.Find("ProcessValue") as TagDynamization;
                if (binding == null ||
                    !String.Equals(binding.Tag, parts[1],
                        StringComparison.OrdinalIgnoreCase) || binding.ReadOnly)
                    throw new InvalidOperationException(
                        "Editable binding audit failed for " + parts[0] + ".");
                report.WriteLine("EditableBinding=" + parts[0] + "->" + binding.Tag);
            }

            HmiButton toggle = screen.ScreenItems.Find(
                "ROB_Conveyor_Sync_Toggle") as HmiButton;
            HmiButtonEventHandler toggleEvent = toggle == null ? null :
                toggle.EventHandlers.Find(HmiButtonEventType.Tapped);
            if (toggleEvent == null ||
                !toggleEvent.Script.ScriptCode.Contains("Conveyor_Sync_Enable"))
                throw new InvalidOperationException(
                    "The conveyor synchronisation toggle event is missing.");

            HmiScreen production = hmi.Screens.Find("production");
            HmiButton link = production == null ? null :
                production.ScreenItems.Find(
                    "ROB_Production_Open_Conveyor_Settings") as HmiButton;
            HmiButtonEventHandler linkEvent = link == null ? null :
                link.EventHandlers.Find(HmiButtonEventType.Tapped);
            if (linkEvent == null ||
                !linkEvent.Script.ScriptCode.Contains("conveyor_settings") ||
                !linkEvent.Script.ScriptCode.Contains("Main screen window_1"))
                throw new InvalidOperationException(
                    "The Production-to-conveyor navigation event is missing.");

            report.WriteLine("Screen=conveyor_settings");
            report.WriteLine("ScreenItemCount=" + screen.ScreenItems.Count);
            report.WriteLine("UniqueObjectNames=YES");
            report.WriteLine("SyncToggleEvent=PASS");
            report.WriteLine("ProductionNavigationEvent=PASS");
        }

        private static HmiSoftware FindUnifiedHmi(Project project,
            out Device hmiDevice, out DeviceItem hmiItem)
        {
            hmiDevice = null;
            hmiItem = null;
            foreach (Device device in project.Devices)
            {
                DeviceItem candidate;
                HmiSoftware software = FindUnifiedHmi(device.DeviceItems, out candidate);
                if (software != null)
                {
                    hmiDevice = device;
                    hmiItem = candidate;
                    return software;
                }
            }
            return null;
        }

        private static HmiSoftware FindUnifiedHmi(
            DeviceItemComposition items, out DeviceItem hmiItem)
        {
            hmiItem = null;
            foreach (DeviceItem item in items)
            {
                SoftwareContainer container = item.GetService<SoftwareContainer>();
                HmiSoftware software = container == null ? null :
                    container.Software as HmiSoftware;
                if (software != null)
                {
                    hmiItem = item;
                    return software;
                }

                DeviceItem child;
                software = FindUnifiedHmi(item.DeviceItems, out child);
                if (software != null)
                {
                    hmiItem = child;
                    return software;
                }
            }
            return null;
        }

        private static void WriteMessages(CompilerResultMessageComposition messages,
            StreamWriter report, string indent)
        {
            foreach (CompilerResultMessage message in messages)
            {
                report.WriteLine(indent + message.State + "|" + message.Path + "|" +
                    message.Description.Replace("\r", " ").Replace("\n", " "));
                WriteMessages(message.Messages, report, indent + "  ");
            }
        }
    }
}
