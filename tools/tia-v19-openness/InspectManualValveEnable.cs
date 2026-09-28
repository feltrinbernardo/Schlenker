using System;
using System.Linq;
using System.Reflection;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.HmiUnified.HmiTags;
using Siemens.Engineering.HmiUnified.UI.Controls;
using Siemens.Engineering.HmiUnified.UI.Enum;
using Siemens.Engineering.HmiUnified.UI.Events;
using Siemens.Engineering.HmiUnified.UI.Screens;
using Siemens.Engineering.HmiUnified.UI.Widgets;

namespace Schlenker.TiaV19
{
    internal static class InspectManualValveEnable
    {
        private static int Main(string[] args)
        {
            if (args.Length != 1) return 2;
            string expected = System.IO.Path.GetFullPath(args[0]);
            TiaPortalProcess process = TiaPortal.GetProcesses().SingleOrDefault(p =>
                p.ProjectPath != null && p.ProjectPath.FullName.Equals(expected,
                    StringComparison.OrdinalIgnoreCase));
            if (process == null) throw new InvalidOperationException("Exact project is not open.");

            using (TiaPortal portal = process.Attach())
            {
                Project project = portal.Projects.Single();
                HmiSoftware hmi = FindHmi(project);
                HmiScreen screen = hmi.Screens.Find("manual_valves");
                InspectButton(screen, "REV50_ValveTest_EnableOn");
                InspectButton(screen, "REV50_ValveTest_EnableOff");
                InspectEnableButtonOverlaps(screen);
                foreach (string name in new[]
                {
                    "REV41_MANUAL_ValveSafety_Back", "REV41_MANUAL_ValveSafety_Text",
                    "REV41_MANUAL_ValveManual_Back", "REV41_MANUAL_ValveManual_Text",
                    "REV41_MANUAL_ValveComm_Back", "REV41_MANUAL_ValveComm_Text",
                    "REV41_MANUAL_ValveEnable_Back", "REV41_MANUAL_ValveEnable_Text"
                })
                {
                    object item = screen.ScreenItems.Find(name);
                    if (item == null) { Console.WriteLine("STATUS_ITEM=" + name + "; MISSING"); continue; }
                    PropertyInfo visible = item.GetType().GetProperty("Visible");
                    PropertyInfo enabled = item.GetType().GetProperty("Enabled");
                    Console.WriteLine("STATUS_ITEM=" + name + "; VISIBLE=" +
                        visible.GetValue(item, null) + "; ENABLED=" + enabled.GetValue(item, null));
                }
                InspectTag(hmi, "Gate_Manual_TestEnable");
                InspectTag(hmi, "Gate_Manual_PageActive");
                InspectTag(hmi, "Cmd_ManualSelect");
                foreach (object connection in hmi.Connections)
                {
                    Console.WriteLine("CONNECTION_TYPE=" + connection.GetType().FullName);
                    foreach (PropertyInfo property in connection.GetType().GetProperties()
                        .OrderBy(item => item.Name))
                    {
                        if (property.GetIndexParameters().Length != 0) continue;
                        try
                        {
                            object value = property.GetValue(connection, null);
                            Console.WriteLine("CONNECTION_PROPERTY=" + property.Name +
                                "; VALUE=" + (value == null ? "<null>" : value.ToString()) +
                                "; TYPE=" + property.PropertyType.FullName);
                        }
                        catch (Exception exception)
                        {
                            Console.WriteLine("CONNECTION_PROPERTY=" + property.Name +
                                "; ERROR=" + exception.GetType().Name);
                        }
                    }
                }
            }
            return 0;
        }

        private static void InspectEnableButtonOverlaps(HmiScreen screen)
        {
            const int left = 970, top = 143, right = 1115, bottom = 177;
            int index = 0;
            foreach (object item in screen.ScreenItems)
            {
                Type type = item.GetType();
                PropertyInfo leftProperty = type.GetProperty("Left");
                PropertyInfo topProperty = type.GetProperty("Top");
                PropertyInfo widthProperty = type.GetProperty("Width");
                PropertyInfo heightProperty = type.GetProperty("Height");
                PropertyInfo visibleProperty = type.GetProperty("Visible");
                PropertyInfo nameProperty = type.GetProperty("Name");
                if (leftProperty == null || topProperty == null || widthProperty == null ||
                    heightProperty == null || nameProperty == null) { index++; continue; }
                bool visible = visibleProperty == null || Convert.ToBoolean(visibleProperty.GetValue(item, null));
                int itemLeft = Convert.ToInt32(leftProperty.GetValue(item, null));
                int itemTop = Convert.ToInt32(topProperty.GetValue(item, null));
                int itemRight = itemLeft + Convert.ToInt32(widthProperty.GetValue(item, null));
                int itemBottom = itemTop + Convert.ToInt32(heightProperty.GetValue(item, null));
                if (visible && itemLeft < right && itemRight > left && itemTop < bottom && itemBottom > top)
                    Console.WriteLine("ENABLE_OVERLAP_INDEX=" + index + "; NAME=" +
                        nameProperty.GetValue(item, null) + "; TYPE=" + type.Name +
                        "; BOUNDS=" + itemLeft + "," + itemTop + "," + itemRight + "," + itemBottom);
                index++;
            }
        }

        private static void InspectButton(HmiScreen screen, string name)
        {
            HmiButton button = screen.ScreenItems.Find(name) as HmiButton;
            if (button == null) { Console.WriteLine("BUTTON=" + name + "; MISSING"); return; }
            HmiButtonEventHandler tapped = button.EventHandlers.Find(HmiButtonEventType.Tapped);
            Console.WriteLine("BUTTON=" + name + "; ENABLED=" + button.Enabled +
                "; AUTHORIZATION=" + (String.IsNullOrWhiteSpace(button.Authorization) ? "<none>" : button.Authorization) +
                "; REQUIRE_UNLOCK=" + button.RequireExplicitUnlock +
                "; EVENT_COUNT=" + button.EventHandlers.Count +
                "; TAPPED=" + (tapped == null ? "MISSING" : tapped.Script.ScriptCode));
        }

        private static void InspectTag(HmiSoftware hmi, string name)
        {
            HmiTag tag = hmi.TagTables.SelectMany(t => t.Tags)
                .FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            Console.WriteLine(tag == null ? "TAG=" + name + "; MISSING" :
                "TAG=" + name + "; PLC=" + tag.PlcTag + "; CONNECTION=" + tag.Connection +
                "; TYPE=" + tag.DataType);
            if (tag == null) return;
            foreach (PropertyInfo property in tag.GetType().GetProperties()
                .OrderBy(item => item.Name))
            {
                if (property.GetIndexParameters().Length != 0) continue;
                try
                {
                    object value = property.GetValue(tag, null);
                    Console.WriteLine("TAG_PROPERTY=" + name + "." + property.Name +
                        "; VALUE=" + (value == null ? "<null>" : value.ToString()) +
                        "; TYPE=" + property.PropertyType.FullName);
                }
                catch (Exception exception)
                {
                    Console.WriteLine("TAG_PROPERTY=" + name + "." + property.Name +
                        "; ERROR=" + exception.GetType().Name);
                }
            }
        }

        private static HmiSoftware FindHmi(Project project)
        {
            foreach (Device device in project.Devices)
                foreach (DeviceItem item in Flatten(device.DeviceItems))
                {
                    SoftwareContainer container = item.GetService<SoftwareContainer>();
                    HmiSoftware hmi = container == null ? null : container.Software as HmiSoftware;
                    if (hmi != null) return hmi;
                }
            throw new InvalidOperationException("Unified HMI not found.");
        }

        private static System.Collections.Generic.IEnumerable<DeviceItem> Flatten(DeviceItemComposition items)
        {
            foreach (DeviceItem item in items)
            {
                yield return item;
                foreach (DeviceItem child in Flatten(item.DeviceItems)) yield return child;
            }
        }
    }
}
