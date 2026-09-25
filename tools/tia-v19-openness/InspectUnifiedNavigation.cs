using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.HmiUnified.UI.Base;
using Siemens.Engineering.HmiUnified.UI.Enum;
using Siemens.Engineering.HmiUnified.UI.Events;
using Siemens.Engineering.HmiUnified.UI.Screens;
using Siemens.Engineering.HmiUnified.UI.Widgets;
using Siemens.Engineering.SW;

namespace Schlenker.TiaV19
{
    internal static class InspectUnifiedNavigation
    {
        private static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("Usage: InspectUnifiedNavigation <exact-ap19-path>");
                return 2;
            }

            string projectPath = Path.GetFullPath(args[0]);
            try
            {
                TiaPortalProcess process = TiaPortal.GetProcesses().Single(candidate =>
                    candidate.ProjectPath != null &&
                    Path.GetFullPath(candidate.ProjectPath.FullName).Equals(
                        projectPath, StringComparison.OrdinalIgnoreCase));

                using (TiaPortal portal = process.Attach())
                {
                    Project project = portal.Projects.Single();
                    HmiSoftware hmi = FindUnifiedHmi(project);
                    if (hmi == null) throw new InvalidOperationException("Unified HMI not found.");

                    foreach (HmiScreen screen in hmi.Screens.OrderBy(item => item.Name))
                    {
                        List<HmiScreenItemBase> items = screen.ScreenItems.Cast<HmiScreenItemBase>().ToList();
                        foreach (HmiButton button in items.OfType<HmiButton>()
                            .Where(item => item.Name.IndexOf("Nav", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                HasChangeScreen(item)))
                        {
                            int index = items.IndexOf(button);
                            Console.WriteLine(
                                "BUTTON\t{0}\t{1}\tINDEX={2}\tBOUNDS={3},{4},{5},{6}\tVISIBLE={7}\tENABLED={8}\tAUTH={9}",
                                screen.Name, button.Name, index, button.Left, button.Top,
                                button.Width, button.Height, button.Visible, button.Enabled,
                                button.Authorization ?? "<null>");

                            foreach (HmiButtonEventType eventType in Enum.GetValues(typeof(HmiButtonEventType)))
                            {
                                HmiButtonEventHandler handler = button.EventHandlers.Find(eventType);
                                if (handler != null && handler.Script != null)
                                {
                                    string script = (handler.Script.ScriptCode ?? String.Empty)
                                        .Replace("\r", " ").Replace("\n", " ");
                                    Console.WriteLine("EVENT\t{0}\t{1}\t{2}\t{3}",
                                        screen.Name, button.Name, eventType, script);
                                }
                            }

                            foreach (HmiScreenItemBase overlay in items.Skip(index + 1)
                                .Where(item => item.Visible && item.Enabled && Overlaps(button, item)))
                            {
                                Console.WriteLine("OVERLAY\t{0}\t{1}\t{2}\tTYPE={3}\tINDEX={4}\tBOUNDS={5},{6},{7},{8}",
                                    screen.Name, button.Name, overlay.Name, overlay.GetType().Name,
                                    items.IndexOf(overlay), GetCoordinate(overlay, "Left"),
                                    GetCoordinate(overlay, "Top"), GetCoordinate(overlay, "Width"),
                                    GetCoordinate(overlay, "Height"));
                            }
                        }
                    }
                }

                Console.WriteLine("STATUS=PASS");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("STATUS=FAIL");
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static bool HasChangeScreen(HmiButton button)
        {
            foreach (HmiButtonEventType eventType in Enum.GetValues(typeof(HmiButtonEventType)))
            {
                HmiButtonEventHandler handler = button.EventHandlers.Find(eventType);
                if (handler != null && handler.Script != null &&
                    (handler.Script.ScriptCode ?? String.Empty).Contains("ChangeScreen"))
                    return true;
            }
            return false;
        }

        private static bool Overlaps(HmiScreenItemBase first, HmiScreenItemBase second)
        {
            long firstLeft = GetCoordinate(first, "Left");
            long firstTop = GetCoordinate(first, "Top");
            long secondLeft = GetCoordinate(second, "Left");
            long secondTop = GetCoordinate(second, "Top");
            long firstRight = firstLeft + GetCoordinate(first, "Width");
            long firstBottom = firstTop + GetCoordinate(first, "Height");
            long secondRight = secondLeft + GetCoordinate(second, "Width");
            long secondBottom = secondTop + GetCoordinate(second, "Height");
            return firstLeft < secondRight && firstRight > secondLeft &&
                firstTop < secondBottom && firstBottom > secondTop;
        }

        private static long GetCoordinate(object item, string propertyName)
        {
            object value = item.GetType().GetProperty(propertyName).GetValue(item, null);
            return Convert.ToInt64(value);
        }

        private static HmiSoftware FindUnifiedHmi(Project project)
        {
            foreach (Device device in project.Devices)
            {
                HmiSoftware hmi = FindInItems(device.DeviceItems);
                if (hmi != null) return hmi;
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
