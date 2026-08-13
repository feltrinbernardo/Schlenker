using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.HmiUnified.UI.Base;
using Siemens.Engineering.HmiUnified.UI.Controls;
using Siemens.Engineering.HmiUnified.UI.Dynamization;
using Siemens.Engineering.HmiUnified.UI.Dynamization.Script;
using Siemens.Engineering.HmiUnified.UI.Enum;
using Siemens.Engineering.HmiUnified.UI.Events;
using Siemens.Engineering.HmiUnified.UI.Parts;
using Siemens.Engineering.HmiUnified.UI.Screens;
using Siemens.Engineering.HmiUnified.UI.Shapes;
using Siemens.Engineering.HmiUnified.UI.Widgets;
using Siemens.Engineering.SW;

namespace Schlenker.TiaV19
{
    internal static class VerifyUnifiedHmi
    {
        private static readonly string[] Screens =
        {
            "home", "safety", "operate", "production", "cip", "function", "alarms", "recipe", "setup", "manual", "efficiency", "diagnostics"
        };
        private static readonly string[] NavLabels =
        {
            "HOME", "SAFETY", "OPERATE", "PRODUCTION", "CIP", "FUNCTION", "ALARMS", "RECIPE", "SETUP", "MANUAL", "EFFICIENCY", "DIAGNOSTICS"
        };
        private static readonly string[] NavDestinations =
        {
            "home", "safety", "operate", "production", "cip", "function", "alarms", "recipe", "setup", "manual", "efficiency", "diagnostics"
        };

        private static readonly Dictionary<string, string> Commands =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "home|REV12_Lights_On", "Lights_On" },
                { "home|REV12_Lights_Off", "Lights_Off" },
                { "home|REV12_Filter_On", "AirFilter_On" },
                { "home|REV12_Filter_Off", "AirFilter_Off" },
                { "safety|REV12_Safety_Reset", "Cmd_Reset" },
                { "operate|REV12_Operate_AutoStart", "Cmd_ProductionOn" },
                { "operate|REV12_Operate_Stop", "Cmd_ProductionOff" },
                { "operate|REV12_Operate_Reset", "Cmd_Reset" },
                { "operate|REV12_Operate_Manual", "Run_Out_Product_Start" },
                { "production|REV12_Operate_AutoStart", "Cmd_ProductionOn" },
                { "production|REV12_Operate_Stop", "Cmd_ProductionOff" },
                { "production|REV12_Operate_Reset", "Cmd_Reset" },
                { "production|REV12_Operate_Manual", "Run_Out_Product_Start" },
                { "cip|REV18_CIP_Start", "Cmd_CIPStart" },
                { "cip|REV18_CIP_Stop", "Cmd_CIPStop" },
                { "cip|REV18_CIP_Reset", "Cmd_Reset" },
                { "cip|REV18_CIP_Pause", "Cmd_CIPPause" },
                { "cip|REV18_CIP_Resume", "Cmd_CIPResume" },
                { "cip|REV18_CIP_Abort", "Cmd_CIPAbort" },
                { "alarms|REV12_Alarms_Reset", "Cmd_Reset" },
                { "recipe|REV12_Recipe_Filler_Enable", "Filler_Height_Enable" },
                { "recipe|REV12_Recipe_Capper_Enable", "Capper_Height_Enable" },
                { "manual|REV12_Manual_Gate_Open", "Gate_Open_125Y1" },
                { "manual|REV12_Manual_Gate_Close", "Gate_Close" },
                { "manual|REV12_Manual_Test_Enable", "Gate_Manual_TestEnable" },
                { "manual|REV12_Manual_Up", "Pendant_Up" },
                { "manual|REV12_Manual_Down", "Pendant_Down" },
                { "manual|REV12_Manual_Jog_Enable", "Jog_PB" },
                { "manual|REV12_Manual_Filler", "Filler_Height_Enable" },
                { "manual|REV12_Manual_Capper", "Capper_Height_Enable" }
            };

        private static readonly Dictionary<string, string> TappedCommands =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "home|REV12_Gate_Auto_Off", "HMIRuntime.Tags(\"Gate_Auto_Request_Enable\").Write(0);" },
                { "home|REV12_Gate_Auto_On", "HMIRuntime.Tags(\"Gate_Auto_Request_Enable\").Write(1);" },
                { "home|REV12_Pump_Off", "HMIRuntime.Tags(\"Product_Pump_Enable\").Write(0);" },
                { "home|REV12_Pump_On", "HMIRuntime.Tags(\"Product_Pump_Enable\").Write(1);" },
                { "manual|REV12_Manual_Pump_Off", "HMIRuntime.Tags(\"Cmd_ProductPumpManualEnable\").Write(0);" },
                { "manual|REV12_Manual_Pump_On", "HMIRuntime.Tags(\"Cmd_ProductPumpManualEnable\").Write(1);" }
            };

        private static readonly Dictionary<string, string> NamedFields =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "home|Symbolic IO field_1", "Machine_State" },
                { "safety|REV12_Common_State", "Machine_State" },
                { "operate|REV12_Operate_State", "Machine_State" },
                { "production|REV12_Common_State", "Machine_State" },
                { "cip|REV12_Common_State", "Machine_State" },
                { "function|REV12_Common_State", "Machine_State" },
                { "alarms|REV12_Common_State", "Machine_State" },
                { "recipe|REV12_Common_State", "Machine_State" },
                { "setup|REV12_Common_State", "Machine_State" },
                { "manual|REV12_Common_State", "Machine_State" },
                { "efficiency|REV12_Common_State", "Machine_State" },
                { "diagnostics|REV12_Common_State", "Machine_State" },
                { "home|REV12_Speed_Field", "Speed_Setpoint_BPH" },
                { "home|REV12_Actual_Field", "Speed_Actual_Pct" },
                { "home|REV12_Tank_Field", "Tank_Level_Pct" },
                { "home|REV12_Filter_Warn_Field", "AirFilter_ChangeRequired" },
                { "home|REV12_Active_Mode_Code", "Mode_ActiveCode" },
                { "home|REV12_Home_Vacuum_Status", "Vacuum_Pump_Run" },
                { "home|REV12_Home_Wash_Status", "External_Wash_Active" },
                { "operate|REV12_Operate_Speed_SP", "Speed_Setpoint_BPH" },
                { "operate|REV12_Operate_Speed_PV", "Speed_Actual_Pct" },
                { "operate|REV12_Operate_Pump_PV", "Vacuum_Actual_mbar" },
                { "operate|REV12_Operate_Tank_PV", "Vacuum_Min_mbar" },
                { "production|REV12_Operate_Speed_SP", "Speed_Setpoint_BPH" },
                { "production|REV12_Operate_Speed_PV", "Speed_Actual_Pct" },
                { "production|REV12_Mimic_Tank_Level", "Tank_Level_Pct" },
                { "production|REV12_Mimic_ProductPresence", "Product_Present_At_Pump" },
                { "production|REV12_Mimic_Vacuum", "Vacuum_Actual_mbar" },
                { "cip|REV18_CIP_State", "CIP_Sequence_State" },
                { "cip|REV18_CIP_Phase", "CIP_Sequence_Phase" },
                { "cip|REV18_CIP_Progress", "CIP_Progress_Pct" },
                { "cip|REV18_CIP_Elapsed", "CIP_Phase_Elapsed" },
                { "cip|REV18_CIP_Duration", "CIP_Phase_Duration" },
                { "cip|REV18_CIP_FaultCode", "CIP_Sequence_FaultCode" },
                { "recipe|REV12_Recipe_Speed", "Speed_Setpoint_BPH" },
                { "recipe|REV12_Recipe_Tank", "Tank_Setpoint_Pct" },
                { "recipe|REV12_Recipe_Actual", "Speed_Actual_Pct" },
                { "setup|REV12_Setup_Encoder", "Encoder_Count" },
                { "setup|REV12_Setup_Valve", "SMC_ValveBits" },
                { "setup|REV12_Setup_Mask", "Network_Faulty_Device_Mask" },
                { "efficiency|REV12_Efficiency_Run_Value", "Efficiency_Run_Seconds" },
                { "efficiency|REV12_Efficiency_Stop_Value", "Efficiency_Stop_Seconds" },
                { "efficiency|REV12_Efficiency_Waiting_Value", "Efficiency_Waiting_Seconds" },
                { "efficiency|REV12_Efficiency_Breakdown_Value", "Efficiency_Breakdown_Seconds" },
                { "efficiency|REV12_Eff_Total", "Efficiency_Total_Seconds" },
                { "efficiency|REV12_Eff_SpeedSP", "Speed_Setpoint_BPH" },
                { "efficiency|REV12_Eff_SpeedPV", "Speed_Actual_Pct" },
                { "efficiency|REV12_Eff_Bottles", "Bottle_Count_In_Machine" },
                { "efficiency|REV12_Eff_Tank", "Tank_Level_Pct" },
                { "efficiency|REV12_Eff_Encoder", "Encoder_Count" },
                { "diagnostics|REV12_Diagnostics_Tank", "Tank_Level_Pct" },
                { "diagnostics|REV12_Diagnostics_Vacuum", "Vacuum_Actual_mbar" },
                { "diagnostics|REV12_Diagnostics_Speed", "Speed_Actual_Pct" }
            };

        private static int failures;
        private static int navChecks;
        private static int commandChecks;
        private static int fieldChecks;
        private static int layoutChecks;
        private static int wordChecks;
        private static StreamWriter report;

        private static int Main(string[] args)
        {
            string projectHint = args.Length > 0 ? args[0] : "schlenkers 36-10 190036.ap19";
            string expectedTagsPath = args.Length > 1 ? args[1] : "expected-tags.txt";
            string reportPath = args.Length > 2 ? args[2] : "hmi-openness-audit.tsv";
            bool allNonProductionLayoutOnly = args.Length > 3 &&
                args[3].Equals("--all-nonproduction-layout", StringComparison.OrdinalIgnoreCase);
            bool commonHeaderGeometryOnly = args.Length > 3 &&
                args[3].Equals("--common-header-geometry", StringComparison.OrdinalIgnoreCase);

            try
            {
                IEnumerable<string> expectedTagLines = File.ReadAllLines(expectedTagsPath)
                    .Where(line => !String.IsNullOrWhiteSpace(line));
                if (Path.GetExtension(expectedTagsPath).Equals(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    expectedTagLines = expectedTagLines.Skip(1).Select(line => line.Split(',')[0].Trim('"'));
                }
                HashSet<string> expectedTags = new HashSet<string>(expectedTagLines.Select(line => line.Trim()),
                    StringComparer.OrdinalIgnoreCase);

                using (report = new StreamWriter(reportPath, false))
                {
                    report.WriteLine("Category\tScreen\tObject\tExpected\tActual\tResult\tDetail");

                    TiaPortalProcess selected = TiaPortal.GetProcesses().FirstOrDefault(
                        process => process.ProjectPath != null &&
                            process.ProjectPath.FullName.EndsWith(projectHint, StringComparison.OrdinalIgnoreCase));
                    if (selected == null)
                    {
                        Fail("PROJECT", "", "", projectHint, "not open", "No matching TIA project is open.");
                        return Finish();
                    }

                    using (TiaPortal portal = selected.Attach())
                    {
                        Project project = portal.Projects.FirstOrDefault();
                        HmiSoftware hmi = project == null ? null : FindUnifiedHmi(project);
                        if (hmi == null)
                        {
                            Fail("PROJECT", "", "", "WinCC Unified HMI", "not found", "No Unified HMI software was found.");
                            return Finish();
                        }

                        if (commonHeaderGeometryOnly)
                        {
                            foreach (HmiScreen screen in hmi.Screens.OrderBy(candidate => candidate.Name))
                            {
                                VerifyCommonHeaderGeometry(screen);
                            }
                            return Finish();
                        }

                        if (allNonProductionLayoutOnly)
                        {
                            foreach (HmiScreen screen in hmi.Screens.OrderBy(candidate => candidate.Name))
                            {
                                if (!screen.Name.Equals("production", StringComparison.OrdinalIgnoreCase))
                                {
                                    VerifyLayout(screen);
                                }
                            }
                            return Finish();
                        }

                        foreach (string screenName in Screens)
                        {
                            HmiScreen screen = hmi.Screens.Find(screenName);
                            if (screen == null)
                            {
                                Fail("SCREEN", screenName, "", "present", "missing", "Required screen missing.");
                                continue;
                            }
                            VerifyNavigation(screen);
                            VerifyCommands(screen);
                            VerifyFields(screen, expectedTags);
                            VerifyLayout(screen);
                            if (screenName.Equals("alarms", StringComparison.OrdinalIgnoreCase))
                            {
                                VerifyAlarmReset(screen);
                            }
                        }
                        VerifyEfficiencyNaming(hmi);
                    }

                    return Finish();
                }
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("STATUS=FAIL");
                Console.Error.WriteLine(exception);
                return 2;
            }
        }

        private static void VerifyAlarmReset(HmiScreen screen)
        {
            HmiButton button = screen.ScreenItems.Find("REV12_Alarms_Reset") as HmiButton;
            HmiText label = screen.ScreenItems.Find("REV12_Alarms_Reset_Label") as HmiText;
            HmiText instruction = screen.ScreenItems.Find("REV12_Alarms_Reset_Note") as HmiText;
            HmiText footer = screen.ScreenItems.Find("REV12_Common_Footer") as HmiText;
            HmiRectangle actionFooter = screen.ScreenItems.Find("REV12_Alarms_Action_Footer") as HmiRectangle;
            HmiAlarmControl alarmControl = screen.ScreenItems.Find("REV12_Active_Alarm_Control") as HmiAlarmControl;
            HmiScreenItemBase warningDuplicate = screen.ScreenItems.Find("REV12_Alarms_Reset_Warning");
            HmiScreenItemBase criticalDuplicate = screen.ScreenItems.Find("REV12_Alarms_Reset_Critical");

            layoutChecks++;
            if (button != null && button.Left == 1000 && button.Top == 432 && button.Width == 120 && button.Height == 84)
            {
                Pass("LAYOUT", screen.Name, "REV12_Alarms_Reset", "1000,432,120,84",
                    button.Left + "," + button.Top + "," + button.Width + "," + button.Height,
                    "Single reset icon is at the requested position and size.");
            }
            else
            {
                Fail("LAYOUT", screen.Name, "REV12_Alarms_Reset", "1000,432,120,84", button == null ? "missing" :
                    button.Left + "," + button.Top + "," + button.Width + "," + button.Height,
                    "Reset icon bounds do not match the requested layout.");
            }

            layoutChecks++;
            if (label != null && label.Left == 882 && label.Top == 450 && label.Width == 118 && label.Height == 55)
            {
                Pass("LAYOUT", screen.Name, "REV12_Alarms_Reset_Label", "882,450,118,55",
                    label.Left + "," + label.Top + "," + label.Width + "," + label.Height,
                    "Label retains the requested anchor and stops exactly at the reset-button boundary.");
            }
            else
            {
                Fail("LAYOUT", screen.Name, "REV12_Alarms_Reset_Label", "882,450,118,55", label == null ? "missing" :
                    label.Left + "," + label.Top + "," + label.Width + "," + label.Height,
                    "Reset label is missing or misaligned.");
            }

            layoutChecks++;
            if (warningDuplicate == null && criticalDuplicate == null)
            {
                Pass("LAYOUT", screen.Name, "alarm reset duplicates", "none", "none",
                    "Warning and critical overlay buttons were deleted; only one reset icon remains.");
            }
            else
            {
                Fail("LAYOUT", screen.Name, "alarm reset duplicates", "none", "present",
                    "A legacy layered reset button still exists.");
            }

            layoutChecks++;
            ScriptDynamization color = button == null ? null : button.Dynamizations.Find("BackColor") as ScriptDynamization;
            string script = color == null ? "" : color.ScriptCode;
            bool colorsOk = script.Contains("Alarm_Critical") && script.Contains("Alarm_Warning") &&
                script.Contains("195, 45, 55") && script.Contains("235, 160, 30") && script.Contains("28, 145, 82");
            if (colorsOk)
            {
                Pass("DYNAMIC", screen.Name, "REV12_Alarms_Reset.BackColor", "red/orange/green",
                    "critical/warning/normal", "One icon uses alarm-priority colour dynamization.");
            }
            else
            {
                Fail("DYNAMIC", screen.Name, "REV12_Alarms_Reset.BackColor", "red/orange/green",
                    color == null ? "missing" : script, "Reset icon colour dynamization is incomplete.");
            }

            layoutChecks++;
            bool footerReservesResetArea = alarmControl != null && actionFooter != null && button != null && label != null &&
                actionFooter.Left == 25 && actionFooter.Top == 420 && actionFooter.Width == 1165 && actionFooter.Height == 116 &&
                screen.ScreenItems.IndexOf(actionFooter) > screen.ScreenItems.IndexOf(alarmControl) &&
                screen.ScreenItems.IndexOf(button) > screen.ScreenItems.IndexOf(actionFooter) &&
                screen.ScreenItems.IndexOf(label) > screen.ScreenItems.IndexOf(actionFooter);
            if (footerReservesResetArea)
            {
                Pass("LAYOUT", screen.Name, "REV12_Alarms_Action_Footer", "25,420,1165,116 behind reset", "reserved",
                    "A dedicated foreground footer prevents alarm rows from overlapping RESET ALARMS.");
            }
            else
            {
                Fail("LAYOUT", screen.Name, "REV12_Alarms_Action_Footer", "25,420,1165,116 behind reset", "missing or wrong Z-order",
                    "The alarm grid can render behind the reset controls.");
            }

            layoutChecks++;
            bool controlContainsReset = alarmControl != null && button != null && label != null &&
                button.Left >= alarmControl.Left && button.Top >= alarmControl.Top &&
                button.Left + button.Width <= alarmControl.Left + alarmControl.Width &&
                button.Top + button.Height <= alarmControl.Top + alarmControl.Height &&
                label.Left >= alarmControl.Left && label.Top >= alarmControl.Top &&
                label.Left + label.Width <= alarmControl.Left + alarmControl.Width &&
                label.Top + label.Height <= alarmControl.Top + alarmControl.Height;
            if (controlContainsReset)
            {
                Pass("LAYOUT", screen.Name, "REV12_Active_Alarm_Control / REV12_Alarms_Reset",
                    "reset contained in alarm panel", "contained", "Reset label and icon are inside the Alarm Control panel.");
            }
            else
            {
                Fail("LAYOUT", screen.Name, "REV12_Active_Alarm_Control / REV12_Alarms_Reset",
                    "reset contained in alarm panel", "outside", "Reset label or icon is outside the Alarm Control panel.");
            }

            layoutChecks++;
            bool resetOnTop = alarmControl != null && button != null && label != null &&
                screen.ScreenItems.IndexOf(button) > screen.ScreenItems.IndexOf(alarmControl) &&
                screen.ScreenItems.IndexOf(label) > screen.ScreenItems.IndexOf(alarmControl);
            if (resetOnTop)
            {
                Pass("LAYOUT", screen.Name, "alarm reset Z-order", "above Alarm Control", "above",
                    "Reset label and icon render above the Alarm Control toolbar area.");
            }
            else
            {
                Fail("LAYOUT", screen.Name, "alarm reset Z-order", "above Alarm Control", "behind",
                    "Alarm Control would hide the reset label or icon.");
            }

            layoutChecks++;
            bool instructionsOnTop = alarmControl != null && instruction != null && footer != null &&
                screen.ScreenItems.IndexOf(instruction) > screen.ScreenItems.IndexOf(alarmControl) &&
                screen.ScreenItems.IndexOf(footer) > screen.ScreenItems.IndexOf(alarmControl);
            if (instructionsOnTop)
            {
                Pass("LAYOUT", screen.Name, "alarm instruction Z-order", "above Alarm Control", "above",
                    "Both reset-condition instructions remain at their coordinates in the foreground layer.");
            }
            else
            {
                Fail("LAYOUT", screen.Name, "alarm instruction Z-order", "above Alarm Control", "behind",
                    "An Alarm-page instruction could be hidden behind the Alarm Control.");
            }

            layoutChecks++;
            bool statusIconRemoved = alarmControl != null &&
                !alarmControl.StatusBar.Elements.OfType<HmiControlBarDisplayPart>().Any(element => element.Visible);
            if (statusIconRemoved)
            {
                Pass("LAYOUT", screen.Name, "Alarm Control status display icon", "hidden", "hidden",
                    "The requested lower-left status icon is removed; toolbar controls remain unchanged.");
            }
            else
            {
                Fail("LAYOUT", screen.Name, "Alarm Control status display icon", "hidden", "visible",
                    "A status display icon remains visible.");
            }

            layoutChecks++;
            bool toolbarIconsHidden = alarmControl != null && alarmControl.ToolBar.Visible && !alarmControl.ToolBar.Enabled &&
                !alarmControl.ToolBar.Elements.OfType<HmiControlBarButtonPart>().Any(element => element.Visible) &&
                !alarmControl.ToolBar.Elements.OfType<HmiControlBarToggleSwitchPart>().Any(element => element.Visible);
            if (toolbarIconsHidden)
            {
                Pass("LAYOUT", screen.Name, "Alarm Control toolbar icons", "hidden with reserved footer", "hidden",
                    "All built-in icons are hidden while the blank toolbar band reserves space for RESET ALARMS.");
            }
            else
            {
                Fail("LAYOUT", screen.Name, "Alarm Control toolbar icons", "hidden with reserved footer", "visible or no footer",
                    "Toolbar icons remain visible or the reserved reset footer is missing.");
            }

        }

        private static void VerifyEfficiencyNaming(HmiSoftware hmi)
        {
            wordChecks++;
            if (hmi.Screens.Find("trends") == null)
            {
                Pass("WORDS", "efficiency", "screen name", "efficiency", "efficiency", "Legacy trends screen name is absent.");
            }
            else
            {
                Fail("WORDS", "efficiency", "screen name", "no trends screen", "trends", "Legacy screen name still exists.");
            }

            foreach (string screenName in Screens)
            {
                HmiScreen screen = hmi.Screens.Find(screenName);
                if (screen == null) continue;
                foreach (HmiScreenItemBase item in screen.ScreenItems)
                {
                    if (!item.Visible) continue;
                    string visibleText = "";
                    HmiText text = item as HmiText;
                    HmiButton button = item as HmiButton;
                    if (text != null) visibleText = String.Join(" ", text.Text.Items.Select(entry => entry.Text));
                    if (button != null) visibleText = String.Join(" ", button.Text.Items.Select(entry => entry.Text));
                    if (String.IsNullOrWhiteSpace(visibleText)) continue;

                    wordChecks++;
                    if (visibleText.IndexOf("TREND", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Fail("WORDS", screen.Name, item.Name, "no visible trend wording", visibleText,
                            "Legacy Trends wording remains visible.");
                    }
                    else
                    {
                        Pass("WORDS", screen.Name, item.Name, "no visible trend wording", visibleText,
                            "Visible wording is clear of the legacy page name.");
                    }
                }
            }
        }

        private static void VerifyNavigation(HmiScreen screen)
        {
            for (int index = 0; index < NavLabels.Length; index++)
            {
                string label = NavLabels[index];
                string destination = NavDestinations[index];
                navChecks++;
                string objectName = "REV12_Nav_" + label;
                HmiButton button = screen.ScreenItems.Find(objectName) as HmiButton;
                if (button == null)
                {
                    Fail("NAV", screen.Name, objectName, "button", "missing", "Navigation button missing.");
                    continue;
                }

                bool active = screen.Name.Equals(destination, StringComparison.OrdinalIgnoreCase);
                if (button.Enabled == active)
                {
                    Fail("NAV", screen.Name, objectName, active ? "disabled" : "enabled", button.Enabled.ToString(), "Incorrect enabled state.");
                    continue;
                }

                if (active)
                {
                    Pass("NAV", screen.Name, objectName, "disabled active page", "disabled", "Active-page button is correctly inhibited.");
                    continue;
                }

                HmiButtonEventHandler tapped = button.EventHandlers.Find(HmiButtonEventType.Tapped);
                string script = tapped == null ? "" : tapped.Script.ScriptCode;
                bool destinationOk = script.Contains("ChangeScreen(\"" + destination + "\", \"/\")");
                bool manualEnterOk = destination != "manual" || script.Contains("Gate_Manual_PageActive\").Write(1)");
                bool manualExitOk = !screen.Name.Equals("manual", StringComparison.OrdinalIgnoreCase) ||
                    (script.Contains("Gate_Manual_PageActive\").Write(0)") &&
                     script.Contains("Gate_Open_125Y1\").Write(0)") &&
                     script.Contains("Gate_Close\").Write(0)") &&
                     script.Contains("Jog_PB\").Write(0)") &&
                     script.Contains("Cmd_ManualSelect\").Write(0)") &&
                     script.Contains("Cmd_ProductPumpManualEnable\").Write(0)"));

                if (destinationOk && manualEnterOk && manualExitOk)
                {
                    Pass("NAV", screen.Name, objectName, destination, destination, "Tapped script and manual-page clearing are correct.");
                }
                else
                {
                    Fail("NAV", screen.Name, objectName, destination, script, "Navigation or manual clear script mismatch.");
                }
            }
        }

        private static void VerifyCommonHeaderGeometry(HmiScreen screen)
        {
            HmiRectangle header = screen.ScreenItems.OfType<HmiRectangle>()
                .FirstOrDefault(item => item.Left == 0 && item.Top == 0 &&
                    item.Name.IndexOf("Header", StringComparison.OrdinalIgnoreCase) >= 0);
            CheckHeaderBounds(screen, header, "HEADER", 0, 0, 1366, 50);

            HmiText title = screen.ScreenItems.OfType<HmiText>()
                .FirstOrDefault(item => item.Name.IndexOf("Common_Title", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.Name.IndexOf("Header_Title", StringComparison.OrdinalIgnoreCase) >= 0);
            CheckHeaderBounds(screen, title, "TITLE", 16, 8, 610, 36);

            HmiText page = screen.ScreenItems.OfType<HmiText>()
                .FirstOrDefault(item => item.Name.IndexOf("Common_Page", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.Name.IndexOf("Header_Subtitle", StringComparison.OrdinalIgnoreCase) >= 0);
            CheckHeaderBounds(screen, page, "PAGE", 1010, 10, 335, 34);

            HmiRectangle status = screen.ScreenItems.OfType<HmiRectangle>()
                .FirstOrDefault(item => item.Left == 0 && item.Width == 1366 && item.Top >= 40 && item.Top <= 80 &&
                    item.Name.IndexOf("Status", StringComparison.OrdinalIgnoreCase) >= 0);
            CheckHeaderBounds(screen, status, "STATUS", 0, 50, 1366, 50);

            HmiText user = screen.ScreenItems.Find("REV12_Production_User_Header") as HmiText;
            CheckHeaderBounds(screen, user, "USER", 1112, 61, 95, 28);

            HmiGraphicView userIcon = screen.ScreenItems.Find("REV21_Common_User_Profile_Icon") as HmiGraphicView;
            CheckHeaderBounds(screen, userIcon, "USER_ICON", 1087, 64, 22, 22);
        }

        private static void CheckHeaderBounds(
            HmiScreen screen,
            HmiScreenItemBase item,
            string role,
            int expectedLeft,
            int expectedTop,
            uint expectedWidth,
            uint expectedHeight)
        {
            string expected = expectedLeft + "," + expectedTop + "," + expectedWidth + "," + expectedHeight;
            layoutChecks++;
            if (item == null)
            {
                Fail("COMMON_HEADER", screen.Name, role, expected, "MISSING", "Required common header object was not found.");
                return;
            }

            int actualLeft;
            int actualTop;
            uint actualWidth;
            uint actualHeight;
            if (!TryGetBounds(item, out actualLeft, out actualTop, out actualWidth, out actualHeight))
            {
                Fail("COMMON_HEADER", screen.Name, item.Name, expected, "NO_BOUNDS", "Object bounds could not be read.");
                return;
            }
            string actual = actualLeft + "," + actualTop + "," + actualWidth + "," + actualHeight;
            if (actual.Equals(expected, StringComparison.Ordinal))
            {
                Pass("COMMON_HEADER", screen.Name, item.Name, expected, actual, "Matches Production master geometry.");
            }
            else
            {
                Fail("COMMON_HEADER", screen.Name, item.Name, expected, actual, "Does not match Production master geometry.");
            }
        }

        private static void VerifyLayout(HmiScreen screen)
        {
            List<HmiButton> buttons = new List<HmiButton>();
            List<HmiScreenItemBase> foregroundItems = new List<HmiScreenItemBase>();
            foreach (HmiScreenItemBase item in screen.ScreenItems)
            {
                if (!item.Visible)
                {
                    continue;
                }

                int left;
                int top;
                uint width;
                uint height;
                if (!TryGetBounds(item, out left, out top, out width, out height))
                {
                    continue;
                }

                layoutChecks++;
                bool inside = left >= 0 && top >= 0 &&
                    (long)left + width <= 1366 && (long)top + height <= 768;
                if (inside)
                {
                    Pass("LAYOUT", screen.Name, item.Name, "inside 1366x768", left + "," + top + "," + width + "," + height,
                        "Visible object remains inside the screen boundary.");
                }
                else
                {
                    Fail("LAYOUT", screen.Name, item.Name, "inside 1366x768", left + "," + top + "," + width + "," + height,
                        "Visible object exceeds the screen boundary.");
                }

                HmiButton button = item as HmiButton;
                if (button != null)
                {
                    buttons.Add(button);
                }
                else if (item is HmiText || item is HmiIOField || item is HmiSymbolicIOField)
                {
                    foregroundItems.Add(item);
                }
            }

            foreach (HmiButton button in buttons)
            {
                foreach (HmiScreenItemBase foreground in foregroundItems)
                {
                    int left;
                    int top;
                    uint width;
                    uint height;
                    if (!TryGetBounds(foreground, out left, out top, out width, out height))
                    {
                        continue;
                    }
                    bool overlaps = button.Left < left + width && button.Left + button.Width > left &&
                        button.Top < top + height && button.Top + button.Height > top;
                    layoutChecks++;
                    if (overlaps)
                    {
                        Fail("LAYOUT", screen.Name, button.Name + " / " + foreground.Name,
                            "button separate from labels and fields", "overlap",
                            "Interactive button overlaps a visible text or value object.");
                    }
                    else
                    {
                        Pass("LAYOUT", screen.Name, button.Name + " / " + foreground.Name,
                            "button separate from labels and fields", "separate",
                            "Interactive button is separate from visible text and value objects.");
                    }
                }
            }

            for (int first = 0; first < foregroundItems.Count; first++)
            {
                for (int second = first + 1; second < foregroundItems.Count; second++)
                {
                    HmiScreenItemBase a = foregroundItems[first];
                    HmiScreenItemBase b = foregroundItems[second];
                    int aLeft;
                    int aTop;
                    uint aWidth;
                    uint aHeight;
                    int bLeft;
                    int bTop;
                    uint bWidth;
                    uint bHeight;
                    if (!TryGetBounds(a, out aLeft, out aTop, out aWidth, out aHeight) ||
                        !TryGetBounds(b, out bLeft, out bTop, out bWidth, out bHeight))
                    {
                        continue;
                    }
                    bool overlaps = aLeft < bLeft + bWidth && aLeft + aWidth > bLeft &&
                        aTop < bTop + bHeight && aTop + aHeight > bTop;
                    layoutChecks++;
                    if (overlaps)
                    {
                        Fail("LAYOUT", screen.Name, a.Name + " / " + b.Name,
                            "foreground objects separate", "overlap",
                            "Visible text/value objects overlap.");
                    }
                    else
                    {
                        Pass("LAYOUT", screen.Name, a.Name + " / " + b.Name,
                            "foreground objects separate", "separate",
                            "Visible text/value objects are separate.");
                    }
                }
            }

            for (int first = 0; first < buttons.Count; first++)
            {
                for (int second = first + 1; second < buttons.Count; second++)
                {
                    HmiButton a = buttons[first];
                    HmiButton b = buttons[second];
                    bool overlaps = a.Left < b.Left + b.Width && a.Left + a.Width > b.Left &&
                        a.Top < b.Top + b.Height && a.Top + a.Height > b.Top;
                    layoutChecks++;
                    if (overlaps)
                    {
                        Fail("LAYOUT", screen.Name, a.Name + " / " + b.Name, "no button overlap", "overlap",
                            "Interactive button hit areas overlap.");
                    }
                    else
                    {
                        Pass("LAYOUT", screen.Name, a.Name + " / " + b.Name, "no button overlap", "separate",
                            "Interactive button hit areas are separate.");
                    }
                }
            }

            if (screen.Name.Equals("home", StringComparison.OrdinalIgnoreCase))
            {
                foreach (string name in new[] { "REV12_Infeed_Block", "REV12_Filler_Block", "REV12_Capper_Block", "REV12_Outfeed_Block", "REV12_Filter_Warn_Back" })
                {
                    HmiRectangle rectangle = screen.ScreenItems.Find(name) as HmiRectangle;
                    layoutChecks++;
                    bool rounded = rectangle != null && rectangle.Corners.TopLeftRadius == 12 &&
                        rectangle.Corners.TopRightRadius == 12 && rectangle.Corners.BottomLeftRadius == 12 &&
                        rectangle.Corners.BottomRightRadius == 12;
                    if (rounded)
                    {
                        Pass("LAYOUT", screen.Name, name, "12 px rounded corners", "12 px", "Rounded style is consistent.");
                    }
                    else
                    {
                        Fail("LAYOUT", screen.Name, name, "12 px rounded corners", rectangle == null ? "missing" : "different radius",
                            "Rounded style is missing or inconsistent.");
                    }
                }
            }
        }

        private static bool TryGetBounds(HmiScreenItemBase item, out int left, out int top, out uint width, out uint height)
        {
            left = 0;
            top = 0;
            width = 0;
            height = 0;
            System.Reflection.PropertyInfo leftProperty = item.GetType().GetProperty("Left");
            System.Reflection.PropertyInfo topProperty = item.GetType().GetProperty("Top");
            System.Reflection.PropertyInfo widthProperty = item.GetType().GetProperty("Width");
            System.Reflection.PropertyInfo heightProperty = item.GetType().GetProperty("Height");
            if (leftProperty == null || topProperty == null || widthProperty == null || heightProperty == null)
            {
                return false;
            }
            left = Convert.ToInt32(leftProperty.GetValue(item, null));
            top = Convert.ToInt32(topProperty.GetValue(item, null));
            width = Convert.ToUInt32(widthProperty.GetValue(item, null));
            height = Convert.ToUInt32(heightProperty.GetValue(item, null));
            return true;
        }

        private static void VerifyCommands(HmiScreen screen)
        {
            foreach (KeyValuePair<string, string> expected in Commands.Where(pair => pair.Key.StartsWith(screen.Name + "|", StringComparison.OrdinalIgnoreCase)))
            {
                commandChecks++;
                string objectName = expected.Key.Split('|')[1];
                HmiButton button = screen.ScreenItems.Find(objectName) as HmiButton;
                if (button == null)
                {
                    Fail("COMMAND", screen.Name, objectName, expected.Value, "missing", "Command button missing.");
                    continue;
                }

                HmiButtonEventHandler down = button.EventHandlers.Find(HmiButtonEventType.Down);
                HmiButtonEventHandler up = button.EventHandlers.Find(HmiButtonEventType.Up);
                string downScript = down == null ? "" : down.Script.ScriptCode;
                string upScript = up == null ? "" : up.Script.ScriptCode;
                string expectedDown = "HMIRuntime.Tags(\"" + expected.Value + "\").Write(1);";
                string expectedUp = "HMIRuntime.Tags(\"" + expected.Value + "\").Write(0);";
                if (button.Enabled && downScript == expectedDown && upScript == expectedUp)
                {
                    Pass("COMMAND", screen.Name, objectName, expected.Value, expected.Value, "Enabled; Down writes 1 and Up writes 0.");
                }
                else
                {
                    Fail("COMMAND", screen.Name, objectName, expected.Value, downScript + " | " + upScript, "Enabled state or momentary scripts mismatch.");
                }
            }

            foreach (KeyValuePair<string, string> expected in TappedCommands.Where(pair => pair.Key.StartsWith(screen.Name + "|", StringComparison.OrdinalIgnoreCase)))
            {
                commandChecks++;
                string objectName = expected.Key.Split('|')[1];
                HmiButton button = screen.ScreenItems.Find(objectName) as HmiButton;
                HmiButtonEventHandler tapped = button == null ? null : button.EventHandlers.Find(HmiButtonEventType.Tapped);
                string script = tapped == null ? "" : tapped.Script.ScriptCode;
                if (button != null && button.Enabled && script.Contains(expected.Value))
                {
                    Pass("COMMAND", screen.Name, objectName, expected.Value, script, "Enabled; tapped script contains the required write.");
                }
                else
                {
                    Fail("COMMAND", screen.Name, objectName, expected.Value, script, "Missing button, disabled state, or tapped script mismatch.");
                }
            }
        }

        private static void VerifyFields(HmiScreen screen, HashSet<string> expectedTags)
        {
            foreach (HmiScreenItemBase item in screen.ScreenItems)
            {
                if (!item.Visible) continue;
                DynamizationBaseComposition dynamizations = null;
                string ioType = "";
                bool isSymbolic = false;
                HmiIOField numeric = item as HmiIOField;
                if (numeric != null)
                {
                    dynamizations = numeric.Dynamizations;
                    ioType = numeric.IOFieldType.ToString();
                }
                else
                {
                    HmiSymbolicIOField symbolic = item as HmiSymbolicIOField;
                    if (symbolic == null)
                    {
                        continue;
                    }
                    dynamizations = symbolic.Dynamizations;
                    ioType = symbolic.IOFieldType.ToString();
                    isSymbolic = true;
                }

                fieldChecks++;
                TagDynamization binding = dynamizations.Find("ProcessValue") as TagDynamization;
                string actualTag = binding == null ? "" : binding.Tag;
                string key = screen.Name + "|" + item.Name;
                string expectedTag;
                bool named = NamedFields.TryGetValue(key, out expectedTag);
                if (!named && item.Name.StartsWith("REV12_Value_", StringComparison.OrdinalIgnoreCase))
                {
                    expectedTag = actualTag;
                    string safe = new string(actualTag.Where(char.IsLetterOrDigit).ToArray());
                    named = item.Name.Equals("REV12_Value_" + safe, StringComparison.OrdinalIgnoreCase);
                }

                bool tagExists = !string.IsNullOrWhiteSpace(actualTag) && expectedTags.Contains(actualTag);
                bool semanticMatch = named && expectedTag.Equals(actualTag, StringComparison.OrdinalIgnoreCase);
                bool directionOk = binding != null &&
                    ((ioType == HmiIOFieldType.Output.ToString() && binding.ReadOnly) ||
                     (ioType == HmiIOFieldType.InputOutput.ToString() && !binding.ReadOnly));

                if (tagExists && semanticMatch && directionOk)
                {
                    Pass("FIELD", screen.Name, item.Name, expectedTag, actualTag,
                        (isSymbolic ? "symbolic; " : "numeric; ") + ioType + "; tag exists in exported HMI table.");
                }
                else
                {
                    Fail("FIELD", screen.Name, item.Name, named ? expectedTag : "known semantic binding", actualTag,
                        "tagExists=" + tagExists + "; semanticMatch=" + semanticMatch + "; directionOk=" + directionOk + "; " + ioType);
                }
            }

            foreach (KeyValuePair<string, string> expected in NamedFields.Where(pair => pair.Key.StartsWith(screen.Name + "|", StringComparison.OrdinalIgnoreCase)))
            {
                string objectName = expected.Key.Split('|')[1];
                if (screen.ScreenItems.Find(objectName) == null)
                {
                    fieldChecks++;
                    Fail("FIELD", screen.Name, objectName, expected.Value, "missing", "Expected named field missing.");
                }
            }
        }

        private static HmiSoftware FindUnifiedHmi(Project project)
        {
            foreach (Device device in project.Devices)
            {
                HmiSoftware hmi = FindUnifiedHmi(device.DeviceItems);
                if (hmi != null) return hmi;
            }
            return null;
        }

        private static HmiSoftware FindUnifiedHmi(DeviceItemComposition items)
        {
            foreach (DeviceItem item in items)
            {
                SoftwareContainer container = item.GetService<SoftwareContainer>();
                HmiSoftware hmi = container == null ? null : container.Software as HmiSoftware;
                if (hmi != null) return hmi;
                hmi = FindUnifiedHmi(item.DeviceItems);
                if (hmi != null) return hmi;
            }
            return null;
        }

        private static void Pass(string category, string screen, string objectName, string expected, string actual, string detail)
        {
            Write(category, screen, objectName, expected, actual, "PASS", detail);
        }

        private static void Fail(string category, string screen, string objectName, string expected, string actual, string detail)
        {
            failures++;
            Write(category, screen, objectName, expected, actual, "FAIL", detail);
        }

        private static void Write(string category, string screen, string objectName, string expected, string actual, string result, string detail)
        {
            report.WriteLine(string.Join("\t", new[]
            {
                Clean(category), Clean(screen), Clean(objectName), Clean(expected), Clean(actual), Clean(result), Clean(detail)
            }));
        }

        private static string Clean(string value)
        {
            return (value ?? "").Replace("\t", " ").Replace("\r", " ").Replace("\n", " ");
        }

        private static int Finish()
        {
            report.WriteLine("SUMMARY\t\t\t\t\t" + (failures == 0 ? "PASS" : "FAIL") +
                "\tNavigation=" + navChecks + "; Commands=" + commandChecks + "; Fields=" + fieldChecks + "; Layout=" + layoutChecks + "; Words=" + wordChecks + "; Failures=" + failures);
            report.Flush();
            Console.WriteLine("NAVIGATION_CHECKS=" + navChecks);
            Console.WriteLine("COMMAND_CHECKS=" + commandChecks);
            Console.WriteLine("FIELD_CHECKS=" + fieldChecks);
            Console.WriteLine("LAYOUT_CHECKS=" + layoutChecks);
            Console.WriteLine("WORD_CHECKS=" + wordChecks);
            Console.WriteLine("FAILURES=" + failures);
            Console.WriteLine("STATUS=" + (failures == 0 ? "PASS" : "FAIL"));
            return failures == 0 ? 0 : 1;
        }
    }
}
