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
    internal static class ApplyAllPagesSidebarSpacing
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

        private sealed class Snapshot
        {
            public int ItemCount;
            public Dictionary<string, string> Geometry;
            public Dictionary<string, int> Dynamics;
            public Dictionary<string, string> Events;
        }

        private static int Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine(
                    "Usage: ApplyAllPagesSidebarSpacing <project-file-hint> <prechange-zap19> <report-path>");
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

                    HmiScreen[] screens = hmi.Screens
                        .Where(screen => screen.ScreenItems.Find("REF05_NavActiveBar") != null)
                        .ToArray();
                    if (screens.Length == 0)
                        throw new InvalidOperationException("No HMI screen with sidebar navigation was found.");

                    Dictionary<string, Snapshot> before = screens.ToDictionary(
                        screen => screen.Name,
                        screen => Capture(screen),
                        StringComparer.OrdinalIgnoreCase);
                    HashSet<string> changedNames = BuildChangedNames();

                    try
                    {
                        foreach (HmiScreen screen in screens)
                        {
                            ApplySpacing(screen);
                            VerifyScreen(screen, before[screen.Name], changedNames);
                            project.Save();
                            report.WriteLine("SCREEN=" + screen.Name +
                                "|Buttons=" + Labels.Length +
                                "|Height=" + ButtonHeight +
                                "|Gap=" + VerticalGap +
                                "|Status=VERIFIED_AND_SAVED");
                        }

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
                        report.WriteLine("ScreensChanged=" + screens.Length);
                        report.WriteLine("ButtonHeight=" + ButtonHeight);
                        report.WriteLine("UniformVerticalGap=" + VerticalGap);
                        report.WriteLine("NavigationEventsPreserved=YES");
                        report.WriteLine("DynamizationCountsPreserved=YES");
                        report.WriteLine("NonNavigationGeometryPreserved=BY_SCOPED_WRITE");
                        report.WriteLine("ObjectsCreatedOrDeleted=0");
                        report.WriteLine("DownloadOrTransferApisUsed=NO");
                        report.WriteLine("Status=PASS");

                        Console.WriteLine("PROJECT=" + project.Path.FullName);
                        Console.WriteLine("SCREENS_CHANGED=" + screens.Length);
                        Console.WriteLine("BUTTONS_PER_SCREEN=" + Labels.Length);
                        Console.WriteLine("BUTTON_HEIGHT=" + ButtonHeight);
                        Console.WriteLine("UNIFORM_GAP=" + VerticalGap);
                        Console.WriteLine("NAVIGATION_EVENTS_PRESERVED=YES");
                        Console.WriteLine("NON_NAVIGATION_GEOMETRY_PRESERVED=BY_SCOPED_WRITE");
                        Console.WriteLine("HMI_ERRORS=" + result.ErrorCount);
                        Console.WriteLine("HMI_WARNINGS=" + result.WarningCount);
                        Console.WriteLine("DOWNLOAD_OR_TRANSFER=NO");
                        Console.WriteLine("REPORT=" + reportPath);
                        Console.WriteLine("STATUS=PASS");
                    }
                    catch
                    {
                        foreach (HmiScreen screen in screens)
                            RestoreNavigationGeometry(screen, before[screen.Name]);
                        project.Save();
                        report.WriteLine("Rollback=ALL_SIDEBAR_GEOMETRY_RESTORED");
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

        private static void ApplySpacing(HmiScreen screen)
        {
            HmiButton[] active = Labels
                .Select(label => Require<HmiButton>(screen, "REV13_Nav_Button_" + label))
                .Where(button => button.Visible && !button.Enabled)
                .ToArray();
            if (active.Length != 1)
                throw new InvalidOperationException(screen.Name +
                    ": expected one active navigation button, found " + active.Length + ".");

            for (int index = 0; index < Labels.Length; index++)
            {
                string label = Labels[index];
                int top = FirstTop + index * Step;
                HmiButton button = Require<HmiButton>(screen, "REV13_Nav_Button_" + label);
                HmiGraphicView icon = Require<HmiGraphicView>(screen, "REV14_Nav_Icon_" + label);
                HmiRectangle separator = screen.ScreenItems.Find(
                    "REF05_NavSeparator_" + label) as HmiRectangle;
                button.Top = top;
                button.Height = ButtonHeight;
                icon.Top = top + (ButtonHeight - (int)icon.Height) / 2;
                if (separator != null)
                    separator.Top = top + ButtonHeight + VerticalGap / 2;
            }

            HmiRectangle marker = Require<HmiRectangle>(screen, "REF05_NavActiveBar");
            marker.Top = active[0].Top;
            marker.Height = ButtonHeight;
        }

        private static void VerifyScreen(
            HmiScreen screen, Snapshot before, HashSet<string> changedNames)
        {
            if (screen.ScreenItems.Count != before.ItemCount)
                throw new InvalidOperationException(screen.Name + ": object count changed.");

            for (int index = 0; index < Labels.Length; index++)
            {
                string label = Labels[index];
                int expectedTop = FirstTop + index * Step;
                HmiButton button = Require<HmiButton>(screen, "REV13_Nav_Button_" + label);
                HmiGraphicView icon = Require<HmiGraphicView>(screen, "REV14_Nav_Icon_" + label);
                HmiRectangle separator = screen.ScreenItems.Find(
                    "REF05_NavSeparator_" + label) as HmiRectangle;
                if (button.Top != expectedTop || button.Height != ButtonHeight)
                    throw new InvalidOperationException(screen.Name + "/" + label +
                        ": button geometry differs from plan.");
                if (icon.Top != expectedTop + (ButtonHeight - (int)icon.Height) / 2)
                    throw new InvalidOperationException(screen.Name + "/" + label +
                        ": icon is not vertically centered.");
                if (separator != null &&
                    separator.Top != expectedTop + ButtonHeight + VerticalGap / 2)
                    throw new InvalidOperationException(screen.Name + "/" + label +
                        ": separator is not centered in the gap.");
                if (index < Labels.Length - 1)
                {
                    HmiButton next = Require<HmiButton>(screen,
                        "REV13_Nav_Button_" + Labels[index + 1]);
                    long gap = next.Top - (button.Top + button.Height);
                    if (gap != VerticalGap)
                        throw new InvalidOperationException(screen.Name + "/" + label +
                            ": gap is " + gap + " instead of " + VerticalGap + ".");
                }
            }

            HmiButton[] active = Labels
                .Select(label => Require<HmiButton>(screen, "REV13_Nav_Button_" + label))
                .Where(button => button.Visible && !button.Enabled)
                .ToArray();
            HmiRectangle marker = Require<HmiRectangle>(screen, "REF05_NavActiveBar");
            if (active.Length != 1 || marker.Top != active[0].Top || marker.Height != active[0].Height)
                throw new InvalidOperationException(screen.Name + ": active marker is not aligned.");

            foreach (KeyValuePair<string, int> pair in before.Dynamics)
            {
                HmiScreenItemBase item = screen.ScreenItems.Find(pair.Key);
                if (item == null || item.Dynamizations.Count != pair.Value)
                    throw new InvalidOperationException(screen.Name +
                        ": dynamization count changed: " + pair.Key + ".");
            }

            Dictionary<string, string> eventsAfter = CaptureEvents(screen);
            foreach (KeyValuePair<string, string> pair in before.Events)
                if (!eventsAfter.ContainsKey(pair.Key) || eventsAfter[pair.Key] != pair.Value)
                    throw new InvalidOperationException(screen.Name +
                        ": navigation event changed: " + pair.Key + ".");
        }

        private static Snapshot Capture(HmiScreen screen)
        {
            Dictionary<string, string> geometry = new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);
            Dictionary<string, int> dynamics = new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase);
            foreach (string label in Labels)
            {
                HmiButton button = Require<HmiButton>(screen, "REV13_Nav_Button_" + label);
                HmiGraphicView icon = Require<HmiGraphicView>(screen, "REV14_Nav_Icon_" + label);
                HmiRectangle separator = screen.ScreenItems.Find(
                    "REF05_NavSeparator_" + label) as HmiRectangle;
                geometry[button.Name] = PositionValue(button);
                geometry[icon.Name] = PositionValue(icon);
                if (separator != null)
                    geometry[separator.Name] = PositionValue(separator);
                dynamics[button.Name] = button.Dynamizations.Count;
            }
            HmiRectangle marker = Require<HmiRectangle>(screen, "REF05_NavActiveBar");
            geometry[marker.Name] = PositionValue(marker);
            return new Snapshot
            {
                ItemCount = screen.ScreenItems.Count,
                Geometry = geometry,
                Dynamics = dynamics,
                Events = CaptureEvents(screen)
            };
        }

        private static Dictionary<string, string> CaptureEvents(HmiScreen screen)
        {
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string label in Labels)
            {
                HmiButton button = Require<HmiButton>(screen, "REV13_Nav_Button_" + label);
                foreach (HmiButtonEventType eventType in new[]
                    { HmiButtonEventType.Down, HmiButtonEventType.Up, HmiButtonEventType.Tapped })
                {
                    HmiButtonEventHandler handler = button.EventHandlers.Find(eventType);
                    result[label + ":" + eventType] = handler == null || handler.Script == null
                        ? "<ABSENT>"
                        : handler.Script.ScriptCode ?? String.Empty;
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

        private static string PositionValue(HmiScreenItemBase item)
        {
            PropertyInfo top = item.GetType().GetProperty("Top");
            PropertyInfo height = item.GetType().GetProperty("Height");
            return "Top=" + Convert.ToString(top.GetValue(item, null)) +
                ";Height=" + Convert.ToString(height.GetValue(item, null));
        }

        private static void RestoreNavigationGeometry(HmiScreen screen, Snapshot before)
        {
            foreach (string name in BuildChangedNames())
            {
                HmiScreenItemBase item = screen.ScreenItems.Find(name);
                if (item == null || !before.Geometry.ContainsKey(name)) continue;
                foreach (string part in before.Geometry[name].Split(';'))
                {
                    string[] pieces = part.Split('=');
                    long value;
                    if (pieces.Length != 2 || !Int64.TryParse(pieces[1], out value)) continue;
                    if (pieces[0] != "Left" && pieces[0] != "Top" &&
                        pieces[0] != "Width" && pieces[0] != "Height") continue;
                    PropertyInfo property = item.GetType().GetProperty(pieces[0]);
                    if (property != null && property.CanWrite)
                        property.SetValue(item, Convert.ChangeType(value, property.PropertyType), null);
                }
            }
        }

        private static T Require<T>(HmiScreen screen, string name) where T : HmiScreenItemBase
        {
            T item = screen.ScreenItems.Find(name) as T;
            if (item == null)
                throw new InvalidOperationException(screen.Name +
                    ": required navigation object is missing: " + name + ".");
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
