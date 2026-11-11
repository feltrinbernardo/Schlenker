using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.Compiler;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.HmiUnified.UI.Enum;
using Siemens.Engineering.HmiUnified.UI.Events;
using Siemens.Engineering.HmiUnified.UI.Screens;
using Siemens.Engineering.HmiUnified.UI.Widgets;

namespace Schlenker.TiaV19
{
    internal static class FixManualAlarmNavigation
    {
        private const string ButtonName = "REV13_Nav_Button_ALARMS";
        private const string ExpectedDestination =
            "HMIRuntime.UI.SysFct.ChangeScreen(\"alarms\", \"/Main screen window_1\");";

        private static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine(
                    "Usage: FixManualAlarmNavigation <exact-ap19-path> <report-path>");
                return 2;
            }

            string projectPath = Path.GetFullPath(args[0]);
            string reportPath = Path.GetFullPath(args[1]);
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));

            try
            {
                TiaPortalProcess process = TiaPortal.GetProcesses().Single(candidate =>
                    candidate.ProjectPath != null &&
                    Path.GetFullPath(candidate.ProjectPath.FullName).Equals(
                        projectPath, StringComparison.OrdinalIgnoreCase));

                using (TiaPortal portal = process.Attach())
                using (StreamWriter report = new StreamWriter(reportPath, false))
                {
                    Project project = portal.Projects.Single();
                    Location location = FindHmi(project);
                    HmiScreen sourceScreen = location.Hmi.Screens.Find("manual_valves");
                    HmiScreen targetScreen = location.Hmi.Screens.Find("manual");
                    if (sourceScreen == null || targetScreen == null)
                        throw new InvalidOperationException(
                            "Required manual/manual_valves screens were not found.");

                    HmiButton sourceButton = sourceScreen.ScreenItems.Find(ButtonName) as HmiButton;
                    HmiButton targetButton = targetScreen.ScreenItems.Find(ButtonName) as HmiButton;
                    if (sourceButton == null || targetButton == null)
                        throw new InvalidOperationException("ALARMS navigation button was not found.");

                    HmiButtonEventHandler sourceTapped =
                        sourceButton.EventHandlers.Find(HmiButtonEventType.Tapped);
                    if (sourceTapped == null || sourceTapped.Script == null)
                        throw new InvalidOperationException(
                            "Reference ALARMS Tapped event is missing on manual_valves.");

                    string referenceScript = sourceTapped.Script.ScriptCode ?? String.Empty;
                    RequireReferenceSafety(referenceScript);

                    HmiButtonEventHandler targetTapped =
                        targetButton.EventHandlers.Find(HmiButtonEventType.Tapped);
                    string previousScript = targetTapped == null || targetTapped.Script == null
                        ? String.Empty
                        : targetTapped.Script.ScriptCode ?? String.Empty;
                    report.WriteLine("PROJECT=" + projectPath);
                    report.WriteLine("SOURCE=manual_valves/" + ButtonName);
                    report.WriteLine("TARGET=manual/" + ButtonName);
                    report.WriteLine("BEFORE_EVENT=" + (targetTapped == null ? "ABSENT" : "PRESENT"));

                    bool created = targetTapped == null;
                    if (created)
                        targetTapped = targetButton.EventHandlers.Create(HmiButtonEventType.Tapped);
                    targetTapped.Script.ScriptCode = referenceScript;

                    if (targetTapped.Script.ScriptCode != referenceScript)
                        throw new InvalidOperationException("Written event differs from reference event.");

                    ICompilable compilable = location.Item.GetService<ICompilable>() ??
                        location.Device.GetService<ICompilable>();
                    if (compilable == null)
                        throw new InvalidOperationException("HMI compiler service is unavailable.");

                    CompilerResult result = compilable.Compile();
                    int nestedErrors = 0;
                    int nestedWarnings = 0;
                    WriteMessages(result.Messages, report, ref nestedErrors, ref nestedWarnings, "");
                    report.WriteLine("ROOT_ERRORS=" + result.ErrorCount);
                    report.WriteLine("ROOT_WARNINGS=" + result.WarningCount);
                    report.WriteLine("NESTED_ERRORS=" + nestedErrors);
                    report.WriteLine("NESTED_WARNINGS=" + nestedWarnings);

                    if (result.ErrorCount != 0 || nestedErrors != 0)
                    {
                        if (created)
                            targetTapped.Delete();
                        else
                            targetTapped.Script.ScriptCode = previousScript;
                        project.Save();
                        throw new InvalidOperationException(
                            "HMI compile failed; the navigation edit was rolled back.");
                    }

                    project.Save();
                    report.WriteLine("AFTER_EVENT=PRESENT");
                    report.WriteLine("DESTINATION=alarms");
                    report.WriteLine("STATUS=PASS");
                    Console.WriteLine("SCREEN=manual");
                    Console.WriteLine("BUTTON=" + ButtonName);
                    Console.WriteLine("DESTINATION=alarms");
                    Console.WriteLine("COMPILE_ERRORS=" + result.ErrorCount);
                    Console.WriteLine("COMPILE_WARNINGS=" + result.WarningCount);
                    Console.WriteLine("STATUS=PASS");
                    return 0;
                }
            }
            catch (Exception exception)
            {
                File.AppendAllText(reportPath,
                    Environment.NewLine + "STATUS=FAIL" + Environment.NewLine + exception);
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static void RequireReferenceSafety(string script)
        {
            string[] required =
            {
                "Gate_Manual_PageActive\").Write(false)",
                "Gate_Manual_TestEnable\").Write(false)",
                "SMC_EV001_Open_Manual\").Write(false)",
                "SMC_EV001_Close_Manual\").Write(false)",
                "Jog_PB\").Write(false)",
                "Cmd_ManualSelect\").Write(false)",
                ExpectedDestination
            };
            foreach (string token in required)
                if (!script.Contains(token))
                    throw new InvalidOperationException(
                        "Reference navigation event is incomplete: " + token);
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
                    if (hmi != null)
                        return new Location { Device = device, Item = item, Hmi = hmi };
                }
            throw new InvalidOperationException("Unified HMI was not found.");
        }

        private static IEnumerable<DeviceItem> Flatten(DeviceItemComposition items)
        {
            foreach (DeviceItem item in items)
            {
                yield return item;
                foreach (DeviceItem child in Flatten(item.DeviceItems))
                    yield return child;
            }
        }
    }
}
