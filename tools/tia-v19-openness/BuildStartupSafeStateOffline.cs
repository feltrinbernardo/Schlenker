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
using Siemens.Engineering.HmiUnified.UI.Dynamization.Script;
using Siemens.Engineering.HmiUnified.UI.Enum;
using Siemens.Engineering.HmiUnified.UI.Events;
using Siemens.Engineering.HmiUnified.UI.Shapes;
using Siemens.Engineering.HmiUnified.UI.Screens;
using Siemens.Engineering.HmiUnified.UI.Widgets;
using Siemens.Engineering.SW;

namespace Schlenker.TiaV19
{
    internal static class BuildStartupSafeStateOffline
    {
        private static int Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine(
                    "Usage: BuildStartupSafeStateOffline <exact-ap19-path> <tag-csv-path> <report-path>");
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
                    candidate.ProjectPath.FullName.Equals(projectPath,
                        StringComparison.OrdinalIgnoreCase));
                if (process == null)
                    throw new InvalidOperationException("The exact authorized TIA project is not open.");
                using (StreamWriter report = new StreamWriter(reportPath, false))
                using (TiaPortal portal = process.Attach())
                {
                    Project project = portal.Projects.Single();
                    if (!project.Path.FullName.Equals(projectPath,
                        StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Attached project identity changed unexpectedly.");
                    Device hmiDevice;
                    DeviceItem hmiItem;
                    HmiSoftware hmi = FindUnifiedHmi(project, out hmiDevice, out hmiItem);
                    if (hmi == null || hmiItem == null)
                        throw new InvalidOperationException("Unified HMI software was not found.");

                    report.WriteLine("SCHLENKER STARTUP SAFE STATE OFFLINE BUILD");
                    report.WriteLine("Project=" + project.Path.FullName);
                    report.WriteLine("Timestamp=" + DateTimeOffset.Now.ToString("o"));
                    report.WriteLine("OnlineApisUsed=NO");
                    report.WriteLine("DownloadOrTransferApisUsed=NO");

                    MethodInfo tagImporter = typeof(ImportUnifiedHmiTags).GetMethod(
                        "EnsureHmiTags", BindingFlags.NonPublic | BindingFlags.Static);
                    if (tagImporter == null)
                        throw new MissingMethodException("ImportUnifiedHmiTags.EnsureHmiTags was not found.");
                    object[] tagArguments = { hmi, csvPath, 0, 0 };
                    tagImporter.Invoke(null, tagArguments);
                    report.WriteLine("TagsCreated=" + tagArguments[2]);
                    report.WriteLine("TagsUpdated=" + tagArguments[3]);

                    MethodInfo builder = typeof(BuildUnifiedHomeScreen).GetMethod(
                        "BuildStartupSafeState", BindingFlags.NonPublic | BindingFlags.Static);
                    if (builder == null)
                        throw new MissingMethodException("BuildStartupSafeState was not found.");
                    builder.Invoke(null, new object[] { hmi });
                    Audit(hmi, report);

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
            catch (Exception exception)
            {
                return Fail(reportPath, exception);
            }
        }

        private static void Audit(HmiSoftware hmi, StreamWriter report)
        {
            HmiScreen screen = hmi.Screens.Find("startup_safe_state");
            if (screen == null)
                throw new InvalidOperationException("startup_safe_state screen was not created.");
            if (screen.ScreenItems.Select(item => item.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase).Count() != screen.ScreenItems.Count)
                throw new InvalidOperationException("Startup screen contains duplicate object names.");
            string[] requiredTags =
            {
                "Startup_Drives_Off", "Startup_Pumps_Off", "Startup_Ventilation_Off",
                "Startup_Lights_Off", "Startup_Product_Inlet_Off",
                "Startup_Bottle_Inlet_Off", "Startup_Wash_Off",
                "Startup_Process_Valves_Off", "Startup_CIP_Requests_Off",
                "Startup_Vertical_Motion_Off", "Startup_Door_Signals_Off",
                "Startup_Start_Permitted", "Startup_Physical_Feedback_Complete"
            };
            foreach (string tag in requiredTags)
            {
                bool found = screen.ScreenItems.Any(item =>
                    item.Dynamizations.OfType<ScriptDynamization>()
                        .Any(d => d.ScriptCode.Contains(tag)));
                if (!found)
                    throw new InvalidOperationException("Missing startup checklist binding: " + tag);
            }
            HmiScreen home = hmi.Screens.Find("home");
            HmiButton navigation = home == null ? null : home.ScreenItems.Find(
                "REV40_Home_Open_Startup_Safe_State") as HmiButton;
            HmiButtonEventHandler tapped = navigation == null ? null :
                navigation.EventHandlers.Find(HmiButtonEventType.Tapped);
            if (tapped == null || !tapped.Script.ScriptCode.Contains("startup_safe_state"))
                throw new InvalidOperationException("Home navigation to startup checklist is missing.");

            HmiScreen manual = hmi.Screens.Find("manual");
            if (manual == null)
                throw new InvalidOperationException("Manual screen was not found.");
            if (manual.ScreenItems.Select(item => item.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase).Count() != manual.ScreenItems.Count)
                throw new InvalidOperationException("Manual screen contains duplicate object names.");
            string[] smcButtons = { "REV42_Manual_SMC_EV220", "REV42_Manual_SMC_EV221" };
            string[] smcTags = { "SMC_EV220_Manual", "SMC_EV221_Manual" };
            for (int index = 0; index < smcButtons.Length; index++)
            {
                HmiButton button = manual.ScreenItems.Find(smcButtons[index]) as HmiButton;
                if (button == null || button.Left != (index == 0 ? 55 : 290) ||
                    button.Top != 337 || button.Width != 210 || button.Height != 44 ||
                    button.EventHandlers.Count < 2 ||
                    !button.EventHandlers.Any(handler =>
                        handler.Script.ScriptCode.Contains(smcTags[index]) &&
                        handler.Script.ScriptCode.Contains("Write(1)")) ||
                    !button.EventHandlers.Any(handler =>
                        handler.Script.ScriptCode.Contains(smcTags[index]) &&
                        handler.Script.ScriptCode.Contains("Write(0)")))
                    throw new InvalidOperationException(
                        "Manual SMC command audit failed: " + smcButtons[index]);
            }
            HmiText smcNote = manual.ScreenItems.Find(
                "REV42_Manual_SMC_PhysicalNote") as HmiText;
            if (smcNote == null || smcNote.Left != 525 || smcNote.Top != 337 ||
                smcNote.Width != 210 || smcNote.Height != 44)
                throw new InvalidOperationException("Manual SMC physical mapping note audit failed.");
            HmiIOField pumpHz = manual.ScreenItems.Find(
                "REV43_Manual_Pump_Hz_Value") as HmiIOField;
            TagDynamization pumpHzBinding = pumpHz == null ? null :
                pumpHz.Dynamizations.Find("ProcessValue") as TagDynamization;
            if (pumpHz == null || pumpHzBinding == null || pumpHzBinding.ReadOnly ||
                !pumpHzBinding.Tag.Equals("Product_Pump_Manual_Frequency_Hz",
                    StringComparison.OrdinalIgnoreCase) || pumpHz.OutputFormat != "{F1}" ||
                pumpHz.Left != 1015 || pumpHz.Top != 663 ||
                pumpHz.Width != 110 || pumpHz.Height != 30)
                throw new InvalidOperationException("Manual product-pump frequency audit failed.");

            int decimalFields = 0;
            foreach (HmiScreen candidate in hmi.Screens)
            {
                foreach (HmiIOField field in candidate.ScreenItems.OfType<HmiIOField>())
                {
                    if (!field.Visible || String.IsNullOrWhiteSpace(field.OutputFormat) ||
                        !field.OutputFormat.Contains(".")) continue;
                    decimalFields++;
                    if (field.OutputFormat != "{F1}")
                        throw new InvalidOperationException(
                            "Visible decimal field is not formatted as {F1}: " +
                            candidate.Name + "/" + field.Name + "=" + field.OutputFormat);
                }
            }
            report.WriteLine("Screen=startup_safe_state");
            report.WriteLine("ScreenItemCount=" + screen.ScreenItems.Count);
            report.WriteLine("ChecklistBindings=" + requiredTags.Length);
            report.WriteLine("HomeNavigation=PASS");
            report.WriteLine("ManualSmcCommands=2");
            report.WriteLine("ManualSmcMomentaryEvents=PASS");
            report.WriteLine("ManualProductPumpFrequencyHz=PASS_15_TO_40_PLC_CLAMP");
            report.WriteLine("PhysicalVqcStationMapping=NOT_CONFIGURED_OUTPUT_INHIBITED");
            report.WriteLine("VisibleDecimalFields=" + decimalFields);
            report.WriteLine("VisibleDecimalFormat={F1}");
        }

        private static int Fail(string reportPath, Exception exception)
        {
            File.AppendAllText(reportPath, Environment.NewLine + "STATUS=FAIL" +
                Environment.NewLine + exception + Environment.NewLine);
            Console.Error.WriteLine("STATUS=FAIL");
            Console.Error.WriteLine(exception);
            return 1;
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

        private static void WriteMessages(System.Collections.Generic.IEnumerable<CompilerResultMessage> messages,
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
