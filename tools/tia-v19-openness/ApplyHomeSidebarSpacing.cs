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
using Siemens.Engineering.HmiUnified.UI.Base;
using Siemens.Engineering.HmiUnified.UI.Enum;
using Siemens.Engineering.HmiUnified.UI.Events;
using Siemens.Engineering.HmiUnified.UI.Screens;
using Siemens.Engineering.HmiUnified.UI.Shapes;
using Siemens.Engineering.HmiUnified.UI.Widgets;
using Siemens.Engineering.SW;

namespace Schlenker.TiaV19
{
    internal static class ApplyHomeSidebarSpacing
    {
        private const int FirstTop = 109;
        private const int ButtonHeight = 44;
        private const int VerticalGap = 9;
        private const int Step = ButtonHeight + VerticalGap;

        private static readonly string[] Labels =
        {
            "HOME", "SAFETY", "OPERATE", "PRODUCTION", "CIP", "FUNCTION",
            "ALARMS", "RECIPE", "SETUP", "MANUAL", "EFFICIENCY", "DIAGNOSTICS"
        };

        private static int Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine(
                    "Usage: ApplyHomeSidebarSpacing <project-file-hint> <prechange-zap19> <report-path>");
                return 2;
            }

            string projectHint = args[0];
            string archivePath = Path.GetFullPath(args[1]);
            string reportPath = Path.GetFullPath(args[2]);
            if (!File.Exists(archivePath))
            {
                Console.Error.WriteLine("STATUS=FAIL");
                Console.Error.WriteLine("Required pre-change archive is missing: " + archivePath);
                return 1;
            }

            try
            {
                TiaPortalProcess process = TiaPortal.GetProcesses().SingleOrDefault(candidate =>
                    candidate.ProjectPath != null && candidate.ProjectPath.FullName.EndsWith(
                        projectHint, StringComparison.OrdinalIgnoreCase));
                if (process == null)
                    throw new InvalidOperationException(
                        "No unique open TIA project matched " + projectHint + ".");

                Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
                using (TiaPortal portal = process.Attach())
                using (StreamWriter report = new StreamWriter(reportPath, false))
                {
                    Project project = portal.Projects.FirstOrDefault();
                    if (project == null)
                        throw new InvalidOperationException("The selected TIA process has no project.");

                    HmiSoftware hmi;
                    Device hmiDevice;
                    FindHmi(project, out hmi, out hmiDevice);
                    if (hmi == null || hmiDevice == null)
                        throw new InvalidOperationException("No WinCC Unified HMI software was found.");
                    HmiScreen home = hmi.Screens.Find("home");
                    if (home == null)
                        throw new InvalidOperationException("Home screen was not found.");

                    Dictionary<string, string> geometryBefore = CaptureGeometry(home);
                    Dictionary<string, int> dynamicsBefore = CaptureDynamizationCounts(home);
                    Dictionary<string, string> eventsBefore = CaptureNavigationEvents(home);
                    int itemCountBefore = home.ScreenItems.Count;
                    HashSet<string> changedNames = BuildChangedNames();

                    try
                    {
                        ApplySpacing(home, report);
                        VerifyResult(home, geometryBefore, dynamicsBefore, eventsBefore,
                            itemCountBefore, changedNames);

                        ICompilable compiler = hmiDevice.GetService<ICompilable>();
                        if (compiler == null)
                            throw new InvalidOperationException("The HMI compiler service is unavailable.");
                        CompilerResult result = compiler.Compile();
                        WriteMessages(report, result.Messages, String.Empty);
                        report.WriteLine("CompileErrors=" + result.ErrorCount);
                        report.WriteLine("CompileWarnings=" + result.WarningCount);
                        if (result.ErrorCount != 0)
                            throw new InvalidOperationException(
                                "HMI compilation failed with " + result.ErrorCount + " errors.");

                        project.Save();
                        report.WriteLine("Project=" + project.Path.FullName);
                        report.WriteLine("Screen=home");
                        report.WriteLine("Buttons=" + Labels.Length);
                        report.WriteLine("ButtonHeight=" + ButtonHeight);
                        report.WriteLine("UniformVerticalGap=" + VerticalGap);
                        report.WriteLine("OtherScreensChanged=NO");
                        report.WriteLine("NavigationEventsPreserved=YES");
                        report.WriteLine("DynamizationCountsPreserved=YES");
                        report.WriteLine("ObjectsCreatedOrDeleted=0");
                        report.WriteLine("DownloadOrTransferApisUsed=NO");
                        report.WriteLine("Status=PASS");

                        Console.WriteLine("PROJECT=" + project.Path.FullName);
                        Console.WriteLine("SCREEN=home");
                        Console.WriteLine("BUTTONS=" + Labels.Length);
                        Console.WriteLine("BUTTON_HEIGHT=" + ButtonHeight);
                        Console.WriteLine("UNIFORM_GAP=" + VerticalGap);
                        Console.WriteLine("NAVIGATION_EVENTS_PRESERVED=YES");
                        Console.WriteLine("HMI_ERRORS=" + result.ErrorCount);
                        Console.WriteLine("HMI_WARNINGS=" + result.WarningCount);
                        Console.WriteLine("DOWNLOAD_OR_TRANSFER=NO");
                        Console.WriteLine("REPORT=" + reportPath);
                        Console.WriteLine("STATUS=PASS");
                    }
                    catch
                    {
                        RestoreGeometry(home, geometryBefore, changedNames);
                        project.Save();
                        report.WriteLine("Rollback=HOME_SIDEBAR_GEOMETRY_RESTORED");
                        report.WriteLine("Status=FAIL");
                        throw;
                    }
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

        private static void ApplySpacing(HmiScreen home, StreamWriter report)
        {
            for (int index = 0; index < Labels.Length; index++)
            {
                string label = Labels[index];
                int top = FirstTop + index * Step;
                HmiButton button = Require<HmiButton>(home, "REV13_Nav_Button_" + label);
                HmiGraphicView icon = Require<HmiGraphicView>(home, "REV14_Nav_Icon_" + label);
                HmiRectangle separator = Require<HmiRectangle>(home, "REF05_NavSeparator_" + label);

                button.Top = top;
                button.Height = ButtonHeight;
                icon.Top = top + (ButtonHeight - (int)icon.Height) / 2;
                separator.Top = top + ButtonHeight + VerticalGap / 2;

                report.WriteLine("NAV=" + label +
                    "|Top=" + button.Top +
                    "|Height=" + button.Height +
                    "|IconTop=" + icon.Top +
                    "|SeparatorTop=" + separator.Top);
            }

            HmiRectangle marker = Require<HmiRectangle>(home, "REF05_NavActiveBar");
            marker.Top = FirstTop;
            marker.Height = ButtonHeight;
        }

        private static void VerifyResult(HmiScreen home,
            Dictionary<string, string> geometryBefore,
            Dictionary<string, int> dynamicsBefore,
            Dictionary<string, string> eventsBefore,
            int itemCountBefore,
            HashSet<string> changedNames)
        {
            if (home.ScreenItems.Count != itemCountBefore)
                throw new InvalidOperationException("The Home object count changed.");

            for (int index = 0; index < Labels.Length; index++)
            {
                string label = Labels[index];
                int expectedTop = FirstTop + index * Step;
                HmiButton button = Require<HmiButton>(home, "REV13_Nav_Button_" + label);
                HmiGraphicView icon = Require<HmiGraphicView>(home, "REV14_Nav_Icon_" + label);
                HmiRectangle separator = Require<HmiRectangle>(home, "REF05_NavSeparator_" + label);
                if (button.Top != expectedTop || button.Height != ButtonHeight)
                    throw new InvalidOperationException(label + ": button geometry differs from plan.");
                if (icon.Top != expectedTop + (ButtonHeight - (int)icon.Height) / 2)
                    throw new InvalidOperationException(label + ": icon is not vertically centered.");
                if (separator.Top != expectedTop + ButtonHeight + VerticalGap / 2)
                    throw new InvalidOperationException(label + ": separator is not centered in the gap.");
                if (index < Labels.Length - 1)
                {
                    HmiButton next = Require<HmiButton>(home,
                        "REV13_Nav_Button_" + Labels[index + 1]);
                    int gap = (int)(next.Top - (button.Top + button.Height));
                    if (gap != VerticalGap)
                        throw new InvalidOperationException(label + ": gap is " + gap + " instead of " + VerticalGap + ".");
                }
            }

            HmiRectangle marker = Require<HmiRectangle>(home, "REF05_NavActiveBar");
            HmiButton homeButton = Require<HmiButton>(home, "REV13_Nav_Button_HOME");
            if (marker.Top != homeButton.Top || marker.Height != homeButton.Height)
                throw new InvalidOperationException("Home active marker is not aligned.");

            foreach (KeyValuePair<string, string> pair in geometryBefore)
            {
                if (changedNames.Contains(pair.Key)) continue;
                HmiScreenItemBase item = home.ScreenItems.Find(pair.Key);
                if (item == null || GeometryValue(item) != pair.Value)
                    throw new InvalidOperationException(
                        "Unexpected geometry change outside the Home navigation: " + pair.Key + ".");
            }

            foreach (KeyValuePair<string, int> pair in dynamicsBefore)
            {
                HmiScreenItemBase item = home.ScreenItems.Find(pair.Key);
                if (item == null || item.Dynamizations.Count != pair.Value)
                    throw new InvalidOperationException("Dynamization count changed: " + pair.Key + ".");
            }

            Dictionary<string, string> eventsAfter = CaptureNavigationEvents(home);
            foreach (KeyValuePair<string, string> pair in eventsBefore)
                if (!eventsAfter.ContainsKey(pair.Key) || eventsAfter[pair.Key] != pair.Value)
                    throw new InvalidOperationException("Navigation event changed: " + pair.Key + ".");
        }

        private static Dictionary<string, string> CaptureNavigationEvents(HmiScreen home)
        {
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string label in Labels)
            {
                HmiButton button = Require<HmiButton>(home, "REV13_Nav_Button_" + label);
                foreach (HmiButtonEventType eventType in new[]
                    { HmiButtonEventType.Down, HmiButtonEventType.Up, HmiButtonEventType.Tapped })
                {
                    HmiButtonEventHandler handler = button.EventHandlers.Find(eventType);
                    string script = handler == null || handler.Script == null
                        ? "<ABSENT>"
                        : handler.Script.ScriptCode ?? String.Empty;
                    result[label + ":" + eventType] = script;
                }
            }
            return result;
        }

        private static HashSet<string> BuildChangedNames()
        {
            HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string label in Labels)
            {
                names.Add("REV13_Nav_Button_" + label);
                names.Add("REV14_Nav_Icon_" + label);
                names.Add("REF05_NavSeparator_" + label);
            }
            names.Add("REF05_NavActiveBar");
            return names;
        }

        private static Dictionary<string, string> CaptureGeometry(HmiScreen screen)
        {
            return screen.ScreenItems.ToDictionary(
                item => item.Name, item => GeometryValue(item), StringComparer.OrdinalIgnoreCase);
        }

        private static Dictionary<string, int> CaptureDynamizationCounts(HmiScreen screen)
        {
            return screen.ScreenItems.ToDictionary(
                item => item.Name, item => item.Dynamizations.Count, StringComparer.OrdinalIgnoreCase);
        }

        private static string GeometryValue(HmiScreenItemBase item)
        {
            string[] properties =
            {
                "Left", "Top", "Width", "Height", "CenterX", "CenterY", "RadiusX", "RadiusY",
                "X1", "Y1", "X2", "Y2", "Visible", "Enabled"
            };
            List<string> values = new List<string>();
            foreach (string name in properties)
            {
                PropertyInfo property = item.GetType().GetProperty(name);
                if (property != null && property.CanRead)
                    values.Add(name + "=" + Convert.ToString(property.GetValue(item, null)));
            }
            return String.Join(";", values);
        }

        private static void RestoreGeometry(HmiScreen home,
            Dictionary<string, string> before, HashSet<string> changedNames)
        {
            foreach (string name in changedNames)
            {
                HmiScreenItemBase item = home.ScreenItems.Find(name);
                if (item == null || !before.ContainsKey(name)) continue;
                Dictionary<string, int> values = new Dictionary<string, int>();
                foreach (string part in before[name].Split(';'))
                {
                    string[] parts = part.Split('=');
                    int parsed;
                    if (parts.Length == 2 && Int32.TryParse(parts[1], out parsed))
                        values[parts[0]] = parsed;
                }
                foreach (KeyValuePair<string, int> pair in values)
                {
                    PropertyInfo property = item.GetType().GetProperty(pair.Key);
                    if (property != null && property.CanWrite &&
                        (pair.Key == "Left" || pair.Key == "Top" || pair.Key == "Width" || pair.Key == "Height"))
                        property.SetValue(item, pair.Value, null);
                }
            }
        }

        private static T Require<T>(HmiScreen screen, string name) where T : HmiScreenItemBase
        {
            T item = screen.ScreenItems.Find(name) as T;
            if (item == null)
                throw new InvalidOperationException("Required Home object is missing: " + name + ".");
            return item;
        }

        private static void FindHmi(Project project, out HmiSoftware hmi, out Device hmiDevice)
        {
            hmi = null;
            hmiDevice = null;
            foreach (Device device in project.Devices)
            foreach (DeviceItem item in device.DeviceItems)
            {
                HmiSoftware found = FindHmi(item);
                if (found != null)
                {
                    hmi = found;
                    hmiDevice = device;
                    return;
                }
            }
        }

        private static HmiSoftware FindHmi(DeviceItem item)
        {
            SoftwareContainer container = item.GetService<SoftwareContainer>();
            HmiSoftware hmi = container == null ? null : container.Software as HmiSoftware;
            if (hmi != null) return hmi;
            foreach (DeviceItem child in item.DeviceItems)
            {
                hmi = FindHmi(child);
                if (hmi != null) return hmi;
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
