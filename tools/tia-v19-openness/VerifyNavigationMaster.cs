using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.HmiUnified.UI.Base;
using Siemens.Engineering.HmiUnified.UI.Events;
using Siemens.Engineering.HmiUnified.UI.Enum;
using Siemens.Engineering.HmiUnified.UI.Screens;
using Siemens.Engineering.HmiUnified.UI.Shapes;
using Siemens.Engineering.HmiUnified.UI.Widgets;
using Siemens.Engineering.SW;

namespace Schlenker.TiaV19
{
    internal static class VerifyNavigationMaster
    {
        private static readonly string[] Labels =
        {
            "HOME", "SAFETY", "OPERATE", "PRODUCTION", "CIP", "FUNCTION",
            "ALARMS", "RECIPE", "SETUP", "MANUAL", "EFFICIENCY", "DIAGNOSTICS"
        };

        private static readonly string[] Assets =
        {
            "nav_home", "nav_safety", "nav_operate", "nav_production", "nav_cip", "nav_function",
            "nav_alarms", "nav_recipe", "nav_settings", "nav_manual", "nav_efficiency", "nav_diagnostics"
        };

        private static readonly int[] ButtonTops =
            { 105, 154, 203, 252, 301, 350, 399, 448, 497, 546, 595, 644 };
        private static readonly int[] IconTops =
            { 115, 164, 213, 262, 310, 361, 408, 458, 508, 556, 604, 654 };

        internal static int Run(string[] args)
        {
            string projectHint = args.Length > 0 ? args[0] : "schlenkers 36-10 190036.ap19";
            string target = args.Length > 1 ? args[1] : null;
            try
            {
                TiaPortalProcess selected = TiaPortal.GetProcesses().FirstOrDefault(
                    process => process.ProjectPath != null &&
                        process.ProjectPath.FullName.EndsWith(projectHint, StringComparison.OrdinalIgnoreCase));
                if (selected == null)
                {
                    Console.Error.WriteLine("STATUS=FAIL\tREASON=PROJECT_NOT_OPEN");
                    return 2;
                }

                using (TiaPortal portal = selected.Attach())
                {
                    Project project = portal.Projects.FirstOrDefault();
                    HmiSoftware hmi = FindUnifiedHmi(project);
                    if (hmi == null)
                    {
                        Console.Error.WriteLine("STATUS=FAIL\tREASON=HMI_NOT_FOUND");
                        return 3;
                    }

                    Dictionary<string, string> activePages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "home", "HOME" }, { "safety", "SAFETY" }, { "operate", "OPERATE" },
                        { "production", "PRODUCTION" }, { "cip", "CIP" }, { "function", "FUNCTION" },
                        { "alarms", "ALARMS" }, { "recipe", "RECIPE" }, { "cip_edit", "CIP" }, { "setup", "SETUP" },
                        { "settings_inputs", "SETUP" }, { "settings_outputs", "SETUP" },
                        { "settings_timers", "SETUP" }, { "settings_external_wash", "SETUP" },
                        { "manual", "MANUAL" }, { "lift_warning", "MANUAL" },
                        { "efficiency", "EFFICIENCY" }, { "diagnostics", "DIAGNOSTICS" },
                        { "io_diagnostics", "DIAGNOSTICS" },
                        { "io_link_overview", "DIAGNOSTICS" },
                        { "io_link_al100", "DIAGNOSTICS" },
                        { "io_link_al101", "DIAGNOSTICS" },
                        { "io_link_al102", "DIAGNOSTICS" },
                        { "io_link_al103", "DIAGNOSTICS" },
                        { "io_link_al104", "DIAGNOSTICS" },
                        { "safety_pilz_diagnostics", "DIAGNOSTICS" }
                    };

                    bool allPass = true;
                    foreach (KeyValuePair<string, string> entry in activePages)
                    {
                        if (!String.IsNullOrWhiteSpace(target) &&
                            !entry.Key.Equals(target, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        HmiScreen screen = hmi.Screens.Find(entry.Key);
                        List<string> faults = VerifyScreen(screen, entry.Value);
                        bool pass = faults.Count == 0;
                        allPass &= pass;
                        Console.WriteLine(
                            "SCREEN={0}\tSTATUS={1}\tACTIVE={2}\tFAULTS={3}",
                            entry.Key, pass ? "VALIDATED" : "FAILED", entry.Value,
                            faults.Count == 0 ? "NONE" : String.Join(" | ", faults));
                    }
                    Console.WriteLine("STATUS=" + (allPass ? "PASS" : "FAIL"));
                    return allPass ? 0 : 1;
                }
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("STATUS=FAIL");
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static List<string> VerifyScreen(HmiScreen screen, string activePage)
        {
            List<string> faults = new List<string>();
            if (screen == null)
            {
                faults.Add("SCREEN_MISSING");
                return faults;
            }

            HmiRectangle back = screen.ScreenItems.Find("REV13_Nav_Back") as HmiRectangle;
            if (back == null || back.Left != 1215 || back.Top != 105 ||
                back.Width != 151 || back.Height != 612 ||
                back.BackColor.ToArgb() != Color.FromArgb(3, 39, 86).ToArgb())
            {
                faults.Add("BACKDROP_MISMATCH");
            }

            for (int index = 0; index < Labels.Length; index++)
            {
                string label = Labels[index];
                HmiButton button = screen.ScreenItems.Find("REV13_Nav_Button_" + label) as HmiButton;
                bool isActive = label.Equals(activePage, StringComparison.OrdinalIgnoreCase);
                Color expectedBack = isActive ? Color.FromArgb(0, 90, 205) : Color.FromArgb(8, 55, 105);
                if (button == null)
                {
                    faults.Add("BUTTON_MISSING_" + label);
                }
                else
                {
                    if (button.Left != 1224 || button.Top != ButtonTops[index] ||
                        button.Width != 133 || button.Height != 43)
                        faults.Add("BUTTON_BOUNDS_" + label);
                    if (button.BackColor.ToArgb() != expectedBack.ToArgb() ||
                        button.ForeColor.ToArgb() != Color.White.ToArgb() ||
                        button.BorderColor.ToArgb() != Color.White.ToArgb() || button.BorderWidth != 1)
                        faults.Add("BUTTON_STYLE_" + label);
                    if (button.Enabled == isActive || !button.Visible)
                        faults.Add("BUTTON_STATE_" + label);
                    if (!isActive && button.EventHandlers.Find(HmiButtonEventType.Tapped) == null)
                        faults.Add("BUTTON_EVENT_" + label);
                }

                HmiGraphicView icon = screen.ScreenItems.Find("REV14_Nav_Icon_" + label) as HmiGraphicView;
                string expectedGraphic = "REV14_" + Assets[index];
                if (icon == null)
                {
                    faults.Add("ICON_MISSING_" + label);
                }
                else
                {
                    if (icon.Left != 1227 || icon.Top != IconTops[index] ||
                        icon.Width != 22 || icon.Height != 22)
                        faults.Add("ICON_BOUNDS_" + label);
                    if (!String.Equals(icon.Graphic, expectedGraphic, StringComparison.OrdinalIgnoreCase) ||
                        icon.BackColor.A != 0 || icon.Enabled || !icon.Visible)
                        faults.Add("ICON_STYLE_" + label);
                }
            }

            foreach (HmiScreenItemBase item in screen.ScreenItems)
            {
                bool approvedNav = item.Name.Equals("REV13_Nav_Back", StringComparison.OrdinalIgnoreCase) ||
                    item.Name.StartsWith("REV13_Nav_Button_", StringComparison.OrdinalIgnoreCase) ||
                    item.Name.StartsWith("REV14_Nav_Icon_", StringComparison.OrdinalIgnoreCase) ||
                    item.Name.Equals("REV13_Common_Bottom", StringComparison.OrdinalIgnoreCase) ||
                    item.Name.Equals("REV13_Common_BottomInfo", StringComparison.OrdinalIgnoreCase);
                bool obsoleteNav = item.Name.Equals("REV12_Nav_Back", StringComparison.OrdinalIgnoreCase) ||
                    item.Name.StartsWith("REV12_Nav_", StringComparison.OrdinalIgnoreCase) ||
                    item.Name.StartsWith("REV14_Nav_IconBack_", StringComparison.OrdinalIgnoreCase) ||
                    item.Name.StartsWith("REV13_Nav_Icon_", StringComparison.OrdinalIgnoreCase);
                if (obsoleteNav)
                    faults.Add("OBSOLETE_NAV_" + item.Name);
                int left, top;
                uint width;
                if (!approvedNav && TryGetBounds(item, out left, out top, out width) &&
                    top >= 105 && left < 1215 && ((long)left + (long)width) > 1215)
                    faults.Add("CENTRAL_OVERLAP_" + item.Name);
            }

            int buttonCount = screen.ScreenItems.Count(item =>
                item.Name.StartsWith("REV13_Nav_Button_", StringComparison.OrdinalIgnoreCase));
            int iconCount = screen.ScreenItems.Count(item =>
                item.Name.StartsWith("REV14_Nav_Icon_", StringComparison.OrdinalIgnoreCase));
            if (buttonCount != 12) faults.Add("BUTTON_COUNT_" + buttonCount);
            if (iconCount != 12) faults.Add("ICON_COUNT_" + iconCount);
            return faults.Distinct().ToList();
        }

        private static bool TryGetBounds(object item, out int left, out int top, out uint width)
        {
            left = 0;
            top = 0;
            width = 0;
            PropertyInfo leftProperty = item.GetType().GetProperty("Left");
            PropertyInfo topProperty = item.GetType().GetProperty("Top");
            PropertyInfo widthProperty = item.GetType().GetProperty("Width");
            if (leftProperty == null || topProperty == null || widthProperty == null) return false;
            left = Convert.ToInt32(leftProperty.GetValue(item, null));
            top = Convert.ToInt32(topProperty.GetValue(item, null));
            width = Convert.ToUInt32(widthProperty.GetValue(item, null));
            return true;
        }

        private static HmiSoftware FindUnifiedHmi(Project project)
        {
            if (project == null) return null;
            foreach (Device device in project.Devices)
            {
                HmiSoftware found = FindInItems(device.DeviceItems);
                if (found != null) return found;
            }
            return null;
        }

        private static HmiSoftware FindInItems(DeviceItemComposition items)
        {
            foreach (DeviceItem item in items)
            {
                SoftwareContainer container = item.GetService<SoftwareContainer>();
                HmiSoftware hmi = container == null ? null : container.Software as HmiSoftware;
                if (hmi != null) return hmi;
                HmiSoftware nested = FindInItems(item.DeviceItems);
                if (nested != null) return nested;
            }
            return null;
        }
    }
}
