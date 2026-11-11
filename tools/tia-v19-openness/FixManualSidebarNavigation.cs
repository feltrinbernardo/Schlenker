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
using Siemens.Engineering.HmiUnified.UI.Shapes;
using Siemens.Engineering.HmiUnified.UI.Screens;
using Siemens.Engineering.HmiUnified.UI.Widgets;

namespace Schlenker.TiaV19
{
    internal static class FixManualSidebarNavigation
    {
        private static readonly string[] Labels =
        {
            "FUNCTION", "ALARMS", "RECIPE", "SETUP", "EFFICIENCY", "DIAGNOSTICS"
        };

        private static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine(
                    "Usage: FixManualSidebarNavigation <exact-ap19-path> <report-path>");
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

                    Dictionary<string, string> previousScripts =
                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    List<string> createdHandlers = new List<string>();

                    report.WriteLine("PROJECT=" + projectPath);
                    report.WriteLine("REFERENCE_SCREEN=manual_valves");
                    report.WriteLine("TARGET_SCREEN=manual");

                    foreach (string label in Labels)
                    {
                        string name = "REV13_Nav_Button_" + label;
                        HmiButton source = sourceScreen.ScreenItems.Find(name) as HmiButton;
                        HmiButton target = targetScreen.ScreenItems.Find(name) as HmiButton;
                        if (source == null || target == null)
                            throw new InvalidOperationException(
                                "Navigation button was not found: " + label);

                        HmiButtonEventHandler sourceTapped =
                            source.EventHandlers.Find(HmiButtonEventType.Tapped);
                        if (sourceTapped == null || sourceTapped.Script == null)
                            throw new InvalidOperationException(
                                "Reference Tapped event is missing: " + label);

                        string referenceScript = sourceTapped.Script.ScriptCode ?? String.Empty;
                        RequireReferenceSafety(label, referenceScript);

                        HmiButtonEventHandler targetTapped =
                            target.EventHandlers.Find(HmiButtonEventType.Tapped);
                        if (targetTapped == null)
                        {
                            targetTapped = target.EventHandlers.Create(HmiButtonEventType.Tapped);
                            createdHandlers.Add(label);
                            report.WriteLine("BEFORE_" + label + "=ABSENT");
                        }
                        else
                        {
                            previousScripts[label] = targetTapped.Script == null
                                ? String.Empty
                                : targetTapped.Script.ScriptCode ?? String.Empty;
                            report.WriteLine("BEFORE_" + label + "=PRESENT");
                        }

                        targetTapped.Script.ScriptCode = referenceScript;
                        if (targetTapped.Script.ScriptCode != referenceScript)
                            throw new InvalidOperationException(
                                "Written event differs from reference: " + label);
                        report.WriteLine("DESTINATION_" + label + "=" + label.ToLowerInvariant());
                    }

                    Dictionary<string, int> previousBarTops =
                        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    List<string> createdBars = new List<string>();
                    int markerScreens = 0;
                    int markerCorrections = 0;
                    HmiScreen markerReferenceScreen = location.Hmi.Screens.Find("production");
                    HmiRectangle markerReference = markerReferenceScreen == null ? null :
                        markerReferenceScreen.ScreenItems.Find("REF05_NavActiveBar") as HmiRectangle;
                    if (markerReference == null)
                        throw new InvalidOperationException(
                            "Production active marker reference is missing.");
                    foreach (HmiScreen screen in location.Hmi.Screens)
                    {
                        HmiButton[] navButtons = screen.ScreenItems.OfType<HmiButton>()
                            .Where(button => button.Name.StartsWith(
                                "REV13_Nav_Button_", StringComparison.OrdinalIgnoreCase)).ToArray();
                        if (navButtons.Length == 0) continue;
                        HmiButton[] activeButtons = navButtons
                            .Where(button => button.Visible && !button.Enabled).ToArray();
                        if (activeButtons.Length != 1)
                            throw new InvalidOperationException(screen.Name +
                                ": expected one visible active navigation button, found " +
                                activeButtons.Length + ".");
                        HmiButton active = activeButtons[0];
                        HmiRectangle bar = screen.ScreenItems.Find(
                            "REF05_NavActiveBar") as HmiRectangle;
                        bool createdBar = bar == null;
                        if (createdBar)
                        {
                            bar = screen.ScreenItems.Create<HmiRectangle>("REF05_NavActiveBar");
                            bar.Left = markerReference.Left;
                            bar.Width = markerReference.Width;
                            bar.Height = markerReference.Height;
                            bar.BackColor = markerReference.BackColor;
                            bar.BorderColor = markerReference.BorderColor;
                            bar.BorderWidth = markerReference.BorderWidth;
                            bar.BackFillPattern = markerReference.BackFillPattern;
                            bar.Visible = true;
                            bar.Enabled = false;
                            createdBars.Add(screen.Name);
                        }
                        else
                        {
                            previousBarTops[screen.Name] = bar.Top;
                        }
                        markerScreens++;
                        bool alignedBefore = bar.Top == active.Top;
                        if (createdBar || !alignedBefore)
                        {
                            bar.Top = active.Top;
                            markerCorrections++;
                        }
                        if (bar.Top != active.Top)
                            throw new InvalidOperationException(screen.Name +
                                ": active marker did not align with " + active.Name + ".");
                        report.WriteLine("MARKER=" + screen.Name +
                            "|ACTIVE=" + active.Name.Substring("REV13_Nav_Button_".Length) +
                            "|BEFORE_TOP=" + (createdBar ? "MISSING" :
                                previousBarTops[screen.Name].ToString()) +
                            "|AFTER_TOP=" + bar.Top +
                            "|BUTTON_TOP=" + active.Top +
                            "|CHANGED=" + (createdBar || !alignedBefore));
                    }
                    report.WriteLine("MARKER_SCREENS=" + markerScreens);
                    report.WriteLine("MARKER_CORRECTIONS=" + markerCorrections);

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
                        foreach (string label in Labels)
                        {
                            HmiButton target = targetScreen.ScreenItems.Find(
                                "REV13_Nav_Button_" + label) as HmiButton;
                            HmiButtonEventHandler handler =
                                target.EventHandlers.Find(HmiButtonEventType.Tapped);
                            if (createdHandlers.Contains(label))
                                handler.Delete();
                            else
                                handler.Script.ScriptCode = previousScripts[label];
                        }
                        foreach (KeyValuePair<string, int> pair in previousBarTops)
                        {
                            HmiScreen screen = location.Hmi.Screens.Find(pair.Key);
                            HmiRectangle bar = screen == null ? null :
                                screen.ScreenItems.Find("REF05_NavActiveBar") as HmiRectangle;
                            if (bar != null) bar.Top = pair.Value;
                        }
                        foreach (string screenName in createdBars)
                        {
                            HmiScreen screen = location.Hmi.Screens.Find(screenName);
                            HmiRectangle bar = screen == null ? null :
                                screen.ScreenItems.Find("REF05_NavActiveBar") as HmiRectangle;
                            if (bar != null) bar.Delete();
                        }
                        project.Save();
                        throw new InvalidOperationException(
                            "HMI compile failed; all sidebar edits were rolled back.");
                    }

                    project.Save();
                    report.WriteLine("STATUS=PASS");
                    Console.WriteLine("SCREEN=manual");
                    Console.WriteLine("BUTTONS=" + String.Join(",", Labels));
                    Console.WriteLine("MARKER_SCREENS=" + markerScreens);
                    Console.WriteLine("MARKER_CORRECTIONS=" + markerCorrections);
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

        private static void RequireReferenceSafety(string label, string script)
        {
            string[] required =
            {
                "Gate_Manual_PageActive\").Write(false)",
                "Gate_Manual_TestEnable\").Write(false)",
                "SMC_EV001_Open_Manual\").Write(false)",
                "SMC_EV001_Close_Manual\").Write(false)",
                "Jog_PB\").Write(false)",
                "Cmd_ManualSelect\").Write(false)",
                "ChangeScreen(\"" + label.ToLowerInvariant() + "\", \"/Main screen window_1\")"
            };
            foreach (string token in required)
                if (!script.Contains(token))
                    throw new InvalidOperationException(
                        "Reference event is incomplete for " + label + ": " + token);
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
