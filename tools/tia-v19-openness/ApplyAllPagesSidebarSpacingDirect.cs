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
using Siemens.Engineering.HmiUnified.UI.Shapes;
using Siemens.Engineering.HmiUnified.UI.Widgets;
using Siemens.Engineering.SW;

namespace Schlenker.TiaV19
{
    internal static class ApplyAllPagesSidebarSpacingDirect
    {
        private const int FirstTop = 109;
        private const int Height = 44;
        private const int Gap = 9;
        private const int Step = Height + Gap;

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
                    "Usage: ApplyAllPagesSidebarSpacingDirect <project-hint> <archive> <report>");
                return 2;
            }

            string hint = args[0];
            string archive = Path.GetFullPath(args[1]);
            string reportPath = Path.GetFullPath(args[2]);
            if (!File.Exists(archive))
            {
                Console.Error.WriteLine("STATUS=FAIL\nMissing pre-change archive: " + archive);
                return 1;
            }

            try
            {
                TiaPortalProcess process = TiaPortal.GetProcesses().SingleOrDefault(candidate =>
                    candidate.ProjectPath != null && candidate.ProjectPath.FullName.EndsWith(
                        hint, StringComparison.OrdinalIgnoreCase));
                if (process == null)
                    throw new InvalidOperationException("No unique open TIA project matched " + hint + ".");

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
                        throw new InvalidOperationException("No WinCC Unified HMI was found.");

                    HmiScreen[] screens = hmi.Screens
                        .Where(screen => screen.ScreenItems.Find("REF05_NavActiveBar") != null)
                        .ToArray();
                    if (screens.Length != 29)
                        throw new InvalidOperationException(
                            "Expected 29 sidebar screens, found " + screens.Length + ".");

                    foreach (HmiScreen screen in screens)
                    {
                        List<HmiButton> buttons = new List<HmiButton>();
                        List<HmiGraphicView> icons = new List<HmiGraphicView>();
                        List<HmiRectangle> separators = new List<HmiRectangle>();

                        foreach (string label in Labels)
                        {
                            HmiButton button = screen.ScreenItems.Find(
                                "REV13_Nav_Button_" + label) as HmiButton;
                            HmiGraphicView icon = screen.ScreenItems.Find(
                                "REV14_Nav_Icon_" + label) as HmiGraphicView;
                            if (button == null || icon == null)
                                throw new InvalidOperationException(screen.Name +
                                    ": missing button or icon for " + label + ".");
                            if (button.Enabled)
                            {
                                HmiButtonEventHandler tapped = button.EventHandlers.Find(
                                    HmiButtonEventType.Tapped);
                                if (tapped == null || tapped.Script == null ||
                                    String.IsNullOrWhiteSpace(tapped.Script.ScriptCode))
                                    throw new InvalidOperationException(screen.Name +
                                        ": navigation event is missing for " + label + ".");
                            }
                            buttons.Add(button);
                            icons.Add(icon);
                            HmiRectangle separator = screen.ScreenItems.Find(
                                "REF05_NavSeparator_" + label) as HmiRectangle;
                            if (separator != null) separators.Add(separator);
                        }

                        HmiButton[] active = buttons
                            .Where(button => button.Visible && !button.Enabled).ToArray();
                        if (active.Length != 1)
                            throw new InvalidOperationException(screen.Name +
                                ": expected one active navigation button, found " + active.Length + ".");
                        HmiRectangle marker = screen.ScreenItems.Find(
                            "REF05_NavActiveBar") as HmiRectangle;
                        if (marker == null)
                            throw new InvalidOperationException(screen.Name + ": active marker is missing.");

                        for (int index = 0; index < Labels.Length; index++)
                        {
                            int top = FirstTop + index * Step;
                            buttons[index].Top = top;
                            buttons[index].Height = Height;
                            icons[index].Top = top + (Height - (int)icons[index].Height) / 2;
                            HmiRectangle separator = screen.ScreenItems.Find(
                                "REF05_NavSeparator_" + Labels[index]) as HmiRectangle;
                            if (separator != null)
                                separator.Top = top + Height + Gap / 2;
                        }
                        marker.Top = active[0].Top;
                        marker.Height = Height;

                        for (int index = 0; index < Labels.Length; index++)
                        {
                            int expectedTop = FirstTop + index * Step;
                            if (buttons[index].Top != expectedTop || buttons[index].Height != Height)
                                throw new InvalidOperationException(screen.Name +
                                    ": verification failed for " + Labels[index] + ".");
                            if (index < Labels.Length - 1 &&
                                buttons[index + 1].Top -
                                    (buttons[index].Top + buttons[index].Height) != Gap)
                                throw new InvalidOperationException(screen.Name +
                                    ": non-uniform gap after " + Labels[index] + ".");
                        }
                        if (marker.Top != active[0].Top || marker.Height != active[0].Height)
                            throw new InvalidOperationException(screen.Name +
                                ": active marker verification failed.");

                        project.Save();
                        report.WriteLine("SCREEN=" + screen.Name +
                            "|ACTIVE=" + active[0].Name +
                            "|BUTTONS=12|HEIGHT=44|GAP=9|SEPARATORS=" + separators.Count +
                            "|EVENTS=VERIFIED|STATUS=PASS");
                        report.Flush();
                    }

                    ICompilable compiler = hmiDevice.GetService<ICompilable>();
                    if (compiler == null)
                        throw new InvalidOperationException("The HMI compiler service is unavailable.");
                    CompilerResult result = compiler.Compile();
                    WriteMessages(report, result.Messages, String.Empty);
                    report.WriteLine("CompileErrors=" + result.ErrorCount);
                    report.WriteLine("CompileWarnings=" + result.WarningCount);
                    report.WriteLine("ScreensChanged=" + screens.Length);
                    report.WriteLine("ObjectsCreatedOrDeleted=0");
                    report.WriteLine("DownloadOrTransferApisUsed=NO");
                    report.WriteLine("Status=" + (result.ErrorCount == 0 ? "PASS" : "FAIL"));
                    report.Flush();
                    if (result.ErrorCount != 0)
                        throw new InvalidOperationException(
                            "HMI compilation failed with " + result.ErrorCount + " errors.");
                    project.Save();

                    Console.WriteLine("PROJECT=" + project.Path.FullName);
                    Console.WriteLine("SCREENS_CHANGED=" + screens.Length);
                    Console.WriteLine("BUTTON_HEIGHT=44");
                    Console.WriteLine("UNIFORM_GAP=9");
                    Console.WriteLine("HMI_ERRORS=" + result.ErrorCount);
                    Console.WriteLine("HMI_WARNINGS=" + result.WarningCount);
                    Console.WriteLine("DOWNLOAD_OR_TRANSFER=NO");
                    Console.WriteLine("REPORT=" + reportPath);
                    Console.WriteLine("STATUS=PASS");
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
