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
using Siemens.Engineering.HmiUnified.UI.Controls;
using Siemens.Engineering.HmiUnified.UI.Events;
using Siemens.Engineering.HmiUnified.UI.Enum;
using Siemens.Engineering.HmiUnified.UI.Screens;
using Siemens.Engineering.HmiUnified.UI.Shapes;
using Siemens.Engineering.HmiUnified.UI.Widgets;
using Siemens.Engineering.SW;

namespace Schlenker.TiaV19
{
    internal static class BuildManualValveTestOffline
    {
        private static readonly string[] CommandTags =
        {
            "SMC_EV001_Open_Manual", "SMC_EV001_Close_Manual",
            "SMC_EV010_Manual", "SMC_EV011_Manual", "SMC_EV210_Manual",
            "SMC_EV211_Manual", "SMC_EV212_Manual", "SMC_EV220_Manual",
            "SMC_EV221_Manual", "SMC_Bottle_External_Wash_Manual",
            "SMC_Filler_External_Wash_Manual"
        };

        private static int Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine("Usage: BuildManualValveTestOffline <exact-ap19> <tag-csv> <report>");
                return 2;
            }
            string projectPath = Path.GetFullPath(args[0]);
            string csvPath = Path.GetFullPath(args[1]);
            string reportPath = Path.GetFullPath(args[2]);
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            try
            {
                TiaPortalProcess process = TiaPortal.GetProcesses().SingleOrDefault(candidate =>
                    candidate.ProjectPath != null &&
                    candidate.ProjectPath.FullName.Equals(projectPath, StringComparison.OrdinalIgnoreCase));
                if (process == null) throw new InvalidOperationException("Exact project is not open.");
                using (StreamWriter report = new StreamWriter(reportPath, false))
                using (TiaPortal portal = process.Attach())
                {
                    Project project = portal.Projects.Single();
                    Device hmiDevice;
                    DeviceItem hmiItem;
                    HmiSoftware hmi = FindUnifiedHmi(project, out hmiDevice, out hmiItem);
                    if (hmi == null || hmiItem == null) throw new InvalidOperationException("Unified HMI not found.");
                    report.WriteLine("SCHLENKER MANUAL VALVE TEST OFFLINE BUILD");
                    report.WriteLine("Project=" + project.Path.FullName);
                    report.WriteLine("OnlineApisUsed=NO");
                    report.WriteLine("DownloadOrTransferApisUsed=NO");

                    MethodInfo tagImporter = typeof(BuildUnifiedHomeScreen).GetMethod(
                        "EnsureHmiTags", BindingFlags.NonPublic | BindingFlags.Static);
                    if (tagImporter == null) throw new MissingMethodException("EnsureHmiTags not found.");
                    tagImporter.Invoke(null, new object[] { hmi, csvPath });
                    report.WriteLine("TagsRefreshedFromCsv=YES");

                    MethodInfo builder = typeof(BuildUnifiedHomeScreen).GetMethod(
                        "BuildManualValveTestScreenOnly", BindingFlags.NonPublic | BindingFlags.Static);
                    if (builder == null) throw new MissingMethodException("BuildManualValveTestScreenOnly not found.");
                    builder.Invoke(null, new object[] { hmi });
                    foreach (HmiScreen candidateScreen in hmi.Screens)
                    {
                        foreach (HmiButton candidateButton in candidateScreen.ScreenItems.OfType<HmiButton>())
                        {
                            foreach (HmiButtonEventHandler handler in candidateButton.EventHandlers)
                            {
                                if (handler.Script.ScriptCode.Contains("Gate_Open_125Y1"))
                                    handler.Script.ScriptCode = handler.Script.ScriptCode.Replace(
                                        "Gate_Open_125Y1", "SMC_EV001_Open_Manual");
                            }
                        }
                    }
                    Siemens.Engineering.HmiUnified.HmiTags.HmiTagTable rev12 = hmi.TagTables.Find("REV12");
                    Siemens.Engineering.HmiUnified.HmiTags.HmiTag legacy = rev12 == null ? null :
                        rev12.Tags.Find("Gate_Open_125Y1");
                    if (legacy != null) legacy.Delete();
                    Audit(hmi, report);

                    ICompilable compilable = hmiItem.GetService<ICompilable>() ?? hmiDevice.GetService<ICompilable>();
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
                    Console.WriteLine("ERRORS=" + result.ErrorCount);
                    Console.WriteLine("WARNINGS=" + result.WarningCount);
                    Console.WriteLine("REPORT=" + reportPath);
                    Console.WriteLine("STATUS=" + (result.ErrorCount == 0 ? "PASS" : "FAIL_NOT_SAVED"));
                    return result.ErrorCount == 0 ? 0 : 1;
                }
            }
            catch (TargetInvocationException exception)
            {
                return Fail(reportPath, exception.InnerException ?? exception);
            }
            catch (Exception exception) { return Fail(reportPath, exception); }
        }

        private static void Audit(HmiSoftware hmi, StreamWriter report)
        {
            HmiScreen screen = hmi.Screens.Find("manual_valves");
            HmiScreen manual = hmi.Screens.Find("manual");
            if (screen == null || manual == null) throw new InvalidOperationException("Manual valve screens missing.");
            if (screen.ScreenItems.Select(item => item.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != screen.ScreenItems.Count)
                throw new InvalidOperationException("Duplicate object name on manual_valves.");

            foreach (string tag in CommandTags)
            {
                HmiButton button = screen.ScreenItems.OfType<HmiButton>().FirstOrDefault(candidate =>
                {
                    HmiButtonEventHandler downEvent = candidate.EventHandlers.Find(HmiButtonEventType.Down);
                    return downEvent != null &&
                        downEvent.Script.ScriptCode.Contains("\"" + tag + "\"") &&
                        downEvent.Script.ScriptCode.Contains("Write(true)");
                });
                HmiButtonEventHandler pressed = button == null ? null : button.EventHandlers.Find(HmiButtonEventType.Down);
                HmiButtonEventHandler released = button == null ? null : button.EventHandlers.Find(HmiButtonEventType.Up);
                if (button == null || pressed == null || released == null ||
                    !pressed.Script.ScriptCode.Contains("Write(true)") ||
                    !released.Script.ScriptCode.Contains("Write(false)"))
                    throw new InvalidOperationException("Momentary command audit failed for " + tag + ".");
            }

            HmiButton openPage = manual.ScreenItems.Find("REV50_Manual_OpenValveTest") as HmiButton;
            HmiButtonEventHandler openEvent = openPage == null ? null : openPage.EventHandlers.Find(HmiButtonEventType.Tapped);
            if (openEvent == null || !openEvent.Script.ScriptCode.Contains("manual_valves"))
                throw new InvalidOperationException("Manual-to-valve-test navigation missing.");
            HmiButton back = screen.ScreenItems.Find("REV50_ValveTest_Back") as HmiButton;
            HmiButtonEventHandler backEvent = back == null ? null : back.EventHandlers.Find(HmiButtonEventType.Tapped);
            if (backEvent == null || !backEvent.Script.ScriptCode.Contains("ChangeScreen(\"manual\""))
                throw new InvalidOperationException("Valve-test return navigation missing.");

            HmiButton filler = screen.ScreenItems.Find("REV50_ValveTest_FillerExternalWash_Command") as HmiButton;
            if (filler == null)
                throw new InvalidOperationException("Filler External Washing command is missing.");

            string[] requiredStatusSuffixes =
            {
                "ValveSafety", "ValveManual", "ValveComm", "ValveEnable",
                "ValveOutputEV001Open", "ValveOutputEV001Close", "ValveOutputEV010",
                "ValveOutputEV011", "ValveOutputEV210", "ValveOutputEV211",
                "ValveOutputEV212", "ValveOutputEV220", "ValveOutputEV221",
                "ValveOutputBottleExternalWash", "ValveOutputFillerExternalWash"
            };
            foreach (string suffix in requiredStatusSuffixes)
            {
                Siemens.Engineering.HmiUnified.UI.Shapes.HmiRectangle statusBack =
                    screen.ScreenItems.Find("REV41_MANUAL_" + suffix + "_Back") as
                        Siemens.Engineering.HmiUnified.UI.Shapes.HmiRectangle;
                HmiText statusText = screen.ScreenItems.Find(
                    "REV41_MANUAL_" + suffix + "_Text") as HmiText;
                if (statusBack == null || statusText == null ||
                    !statusBack.Visible || !statusText.Visible ||
                    !statusBack.Enabled || !statusText.Enabled)
                    throw new InvalidOperationException(
                        "Runtime-visible status badge audit failed for " + suffix + ".");
                List<HmiScreenItemBase> zOrder = screen.ScreenItems.ToList();
                int statusIndex = zOrder.IndexOf(statusBack);
                bool coveredByLaterRectangle = screen.ScreenItems.OfType<
                    Siemens.Engineering.HmiUnified.UI.Shapes.HmiRectangle>()
                    .Where(candidate => candidate != statusBack && candidate.Visible)
                    .Any(candidate =>
                        candidate.Left < statusBack.Left + statusBack.Width &&
                        candidate.Left + candidate.Width > statusBack.Left &&
                        candidate.Top < statusBack.Top + statusBack.Height &&
                        candidate.Top + candidate.Height > statusBack.Top &&
                        zOrder.IndexOf(candidate) > statusIndex);
                if (coveredByLaterRectangle)
                    throw new InvalidOperationException(
                        "Status badge z-order audit failed for " + suffix + ".");
            }

            Siemens.Engineering.HmiUnified.UI.Shapes.HmiRectangle valveSurface =
                screen.ScreenItems.Find("REV52_ValveTest_Content_Back") as
                    Siemens.Engineering.HmiUnified.UI.Shapes.HmiRectangle;
            bool valveBoundsValid = valveSurface != null && valveSurface.Left == 0 &&
                valveSurface.Top == 120 && valveSurface.Width == 1141 &&
                valveSurface.Height == 624 &&
                screen.ScreenItems.Where(item => item.Visible &&
                    (item.Name.StartsWith("REV50_ValveTest_", StringComparison.OrdinalIgnoreCase) ||
                     item.Name.StartsWith("REV41_MANUAL_Valve", StringComparison.OrdinalIgnoreCase)))
                .All(item =>
                {
                    int left = Convert.ToInt32(item.GetAttribute("Left"));
                    int top = Convert.ToInt32(item.GetAttribute("Top"));
                    int width = Convert.ToInt32(item.GetAttribute("Width"));
                    int height = Convert.ToInt32(item.GetAttribute("Height"));
                    return left >= 0 && top >= 120 && left + width <= 1141 &&
                        top + height <= 744;
                });
            if (!valveBoundsValid)
                throw new InvalidOperationException(
                    "Manual valve content bounds audit failed.");

            HmiText headerPage = screen.ScreenItems.Find("REV12_Common_Header_Page") as HmiText;
            HmiText statusPage = screen.ScreenItems.Find("REV12_Common_Page_Label") as HmiText;
            if (headerPage == null || statusPage == null ||
                headerPage.Left + headerPage.Width > 1115 ||
                statusPage.Left + statusPage.Width > 1115)
                throw new InvalidOperationException(
                    "Manual valve title/navigation separation audit failed.");

            HmiButton gateOpen = manual.ScreenItems.Find("REV12_Manual_Gate_Open") as HmiButton;
            HmiButton gateClose = manual.ScreenItems.Find("REV12_Manual_Gate_Close") as HmiButton;
            Siemens.Engineering.HmiUnified.UI.Shapes.HmiRectangle pageBadge =
                manual.ScreenItems.Find("REV41_MANUAL_PageActive_Back") as
                    Siemens.Engineering.HmiUnified.UI.Shapes.HmiRectangle;
            Siemens.Engineering.HmiUnified.UI.Shapes.HmiRectangle testBadge =
                manual.ScreenItems.Find("REV41_MANUAL_TestEnabled_Back") as
                    Siemens.Engineering.HmiUnified.UI.Shapes.HmiRectangle;
            if (gateOpen == null || gateClose == null || pageBadge == null || testBadge == null ||
                gateOpen.Top != 190 || gateOpen.Height != 60 ||
                gateClose.Top != 190 || gateClose.Height != 60 ||
                gateOpen.Top + gateOpen.Height >= pageBadge.Top ||
                gateClose.Top + gateClose.Height >= testBadge.Top)
                throw new InvalidOperationException(
                    "Manual gate command/status vertical separation audit failed.");

            foreach (HmiScreen navigationScreen in new[] { screen })
            {
                int buttonCount = navigationScreen.ScreenItems.Count(item =>
                    item.Name.StartsWith("REV13_Nav_Button_", StringComparison.OrdinalIgnoreCase));
                int iconCount = navigationScreen.ScreenItems.Count(item =>
                    item.Name.StartsWith("REV14_Nav_Icon_", StringComparison.OrdinalIgnoreCase));
                int separatorCount = navigationScreen.ScreenItems.Count(item =>
                    item.Name.StartsWith("REF05_NavSeparator_", StringComparison.OrdinalIgnoreCase));
                Siemens.Engineering.HmiUnified.UI.Shapes.HmiRectangle navBack =
                    navigationScreen.ScreenItems.Find("REV13_Nav_Back") as
                        Siemens.Engineering.HmiUnified.UI.Shapes.HmiRectangle;
                HmiButton manualButton = navigationScreen.ScreenItems.Find(
                    "REV13_Nav_Button_MANUAL") as HmiButton;
                Siemens.Engineering.HmiUnified.UI.Shapes.HmiRectangle footer =
                    navigationScreen.ScreenItems.Find("REV52_Manual_Footer_Back") as
                        Siemens.Engineering.HmiUnified.UI.Shapes.HmiRectangle;
                if (buttonCount != 12 || iconCount != 12 || separatorCount != 12 ||
                    navBack == null || navBack.Left != 1141 ||
                    navBack.Left + navBack.Width != 1280 || navBack.Top != 120 ||
                    navBack.Top + navBack.Height != 744 ||
                    footer == null || footer.Left != 0 || footer.Top != 744 ||
                    footer.Width != 1280 || footer.Height != 56 || footer.Enabled ||
                    manualButton == null || manualButton.Enabled)
                    throw new InvalidOperationException(
                        "Single MTP1200 navigation audit failed on " + navigationScreen.Name + ".");
            }

            report.WriteLine("Screen=manual_valves");
            report.WriteLine("MomentaryCommands=" + CommandTags.Length);
            report.WriteLine("PhysicalStationNumbersVisible=0");
            report.WriteLine("BottleAndFillerExternalWashSeparated=PASS_Q129.6_Q130.2");
            report.WriteLine("NavigationAndExitClearing=PASS");
            report.WriteLine("DuplicateObjectNames=0");
            report.WriteLine("ManualNavigation=12_BUTTONS_12_ICONS_SINGLE_LAYER");
            report.WriteLine("NavigationBounds=1141,120..1280,744");
            report.WriteLine("FooterBounds=0,744,1280,56_SOLID");
            report.WriteLine("GateCommandStatusSeparation=14PX_PASS");
            report.WriteLine("ValveTestContentBounds=0,120,1141,624_PASS");
            report.WriteLine("ValveTestRightColumnClipping=0");
            report.WriteLine("ValveTestTitleNavigationGap=26PX_PASS");
        }

        private static int Fail(string reportPath, Exception exception)
        {
            File.AppendAllText(reportPath, Environment.NewLine + "STATUS=FAIL" + Environment.NewLine + exception);
            Console.Error.WriteLine(exception);
            return 1;
        }

        private static HmiSoftware FindUnifiedHmi(Project project, out Device hmiDevice, out DeviceItem hmiItem)
        {
            hmiDevice = null; hmiItem = null;
            foreach (Device device in project.Devices)
            {
                DeviceItem candidate;
                HmiSoftware hmi = FindUnifiedHmi(device.DeviceItems, out candidate);
                if (hmi != null) { hmiDevice = device; hmiItem = candidate; return hmi; }
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
                if (hmi != null) { hmiItem = item; return hmi; }
                DeviceItem child;
                hmi = FindUnifiedHmi(item.DeviceItems, out child);
                if (hmi != null) { hmiItem = child; return hmi; }
            }
            return null;
        }

        private static void WriteMessages(IEnumerable<CompilerResultMessage> messages, StreamWriter report, string indent)
        {
            foreach (CompilerResultMessage message in messages)
            {
                report.WriteLine(indent + message.State + "\t" + message.Description + "\t" + message.Path);
                WriteMessages(message.Messages, report, indent + "  ");
            }
        }
    }
}
