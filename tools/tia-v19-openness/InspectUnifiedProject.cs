using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.Hmi.Globalization;
using Siemens.Engineering.HmiUnified.UI.Base;
using Siemens.Engineering.HmiUnified.UI.Controls;
using Siemens.Engineering.HmiUnified.UI.Parts;
using Siemens.Engineering.HmiUnified.UI.Screens;
using Siemens.Engineering.SW;

namespace Schlenker.TiaV19
{
    internal static class InspectUnifiedProject
    {
        private static readonly string[] InterestingProperties =
        {
            "Name", "Left", "Top", "Width", "Height", "Text",
            "ProcessValue", "ResourceList", "IOFieldType", "BackColor",
            "ForeColor", "BorderColor", "BorderWidth", "Enabled", "Visible",
            "CustomID", "Graphic", "ToolTipText", "Authorization",
            "X1", "Y1", "X2", "Y2", "LineColor", "LineWidth"
        };

        private static int Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("--types", StringComparison.OrdinalIgnoreCase))
            {
                PrintTypeProperties(typeof(HmiSoftware));
                PrintTypeProperties(typeof(Project));
                PrintTypeProperties(typeof(Siemens.Engineering.HmiUnified.RuntimeSettings.HmiRuntimeSetting));
                PrintTypeProperties(typeof(Siemens.Engineering.HmiUnified.RuntimeSettings.HmiMaxLoginRuntimeSettings));
                PrintTypeProperties(typeof(Siemens.Engineering.HmiUnified.RuntimeSettings.HmiRuntimeResourceSettings));
                PrintTypeProperties(typeof(HmiControlBarButtonPart));
                PrintTypeProperties(typeof(HmiControlBarToggleSwitchPart));
                PrintTypeProperties(typeof(HmiDataGridViewPart));
                PrintTypeProperties(typeof(MultiLingualGraphic));
                PrintTypeMethods(typeof(MultiLingualGraphic));
                PrintTypeMethods(typeof(MultiLingualGraphicComposition));
                Console.WriteLine("ENUM HmiScrollBarVisibility=" + String.Join(",",
                    System.Enum.GetNames(typeof(Siemens.Engineering.HmiUnified.UI.Enum.HmiScrollBarVisibility))));
                return 0;
            }

            bool exportGraphics = args.Length > 0 &&
                args[0].Equals("--graphics-export", StringComparison.OrdinalIgnoreCase);
            string projectHint = exportGraphics
                ? (args.Length > 1 ? args[1] : "schlenkers 36-10 190036.ap19")
                : (args.Length > 0 ? args[0] : "schlenkers 36-10 190036.ap19");
            string screenName = exportGraphics
                ? "home"
                : (args.Length > 1 ? args[1] : "home");
            string graphicsExportDirectory = exportGraphics && args.Length > 2
                ? args[2]
                : Path.Combine(Environment.CurrentDirectory, "graphics-export");

            try
            {
                IList<TiaPortalProcess> processes = TiaPortal.GetProcesses();
                foreach (TiaPortalProcess process in processes)
                {
                    Console.WriteLine(
                        "PROCESS id={0} mode={1} project={2}",
                        process.Id,
                        process.Mode,
                        process.ProjectPath == null ? "<none>" : process.ProjectPath.FullName);
                }

                TiaPortalProcess selected = processes.FirstOrDefault(
                    process => process.ProjectPath != null &&
                        process.ProjectPath.FullName.EndsWith(
                            projectHint,
                            StringComparison.OrdinalIgnoreCase));

                if (selected == null)
                {
                    Console.Error.WriteLine("STATUS=FAIL");
                    Console.Error.WriteLine("No open TIA project matched: " + projectHint);
                    return 2;
                }

                using (TiaPortal portal = selected.Attach())
                {
                    Project project = portal.Projects.FirstOrDefault();
                    if (project == null)
                    {
                        Console.Error.WriteLine("STATUS=FAIL");
                        Console.Error.WriteLine("Attached TIA process has no open project.");
                        return 3;
                    }

                    Console.WriteLine("PROJECT=" + project.Path.FullName);
                    if (exportGraphics)
                    {
                        Directory.CreateDirectory(graphicsExportDirectory);
                        Console.WriteLine("GRAPHICS_COUNT=" + project.Graphics.Count);
                        foreach (MultiLingualGraphic graphic in project.Graphics)
                        {
                            string safeName = string.Concat(graphic.Name.Select(character =>
                                Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));
                            FileInfo target = new FileInfo(Path.Combine(
                                graphicsExportDirectory,
                                safeName + ".xml"));
                            graphic.Export(target, ExportOptions.WithDefaults);
                            Console.WriteLine("GRAPHIC_EXPORTED name={0} path={1}",
                                graphic.Name,
                                target.FullName);
                        }
                        Console.WriteLine("STATUS=PASS");
                        return 0;
                    }
                    foreach (Device device in project.Devices)
                    {
                        InspectDeviceItems(device.Name, device.DeviceItems, screenName);
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

        private static void PrintTypeProperties(Type type)
        {
            Console.WriteLine("TYPE=" + type.FullName);
            foreach (PropertyInfo property in type.GetProperties().OrderBy(property => property.Name))
            {
                Console.WriteLine("PROPERTY={0}; TYPE={1}; WRITE={2}",
                    property.Name, property.PropertyType.FullName, property.CanWrite);
            }
        }

        private static void PrintTypeMethods(Type type)
        {
            Console.WriteLine("METHODS_TYPE=" + type.FullName);
            foreach (MethodInfo method in type.GetMethods().OrderBy(method => method.Name))
            {
                Console.WriteLine("METHOD={0}; RETURN={1}; PARAMETERS={2}",
                    method.Name,
                    method.ReturnType.FullName,
                    string.Join(",", method.GetParameters().Select(parameter =>
                        parameter.ParameterType.FullName + " " + parameter.Name)));
            }
        }

        private static void InspectDeviceItems(
            string deviceName,
            DeviceItemComposition items,
            string screenName)
        {
            foreach (DeviceItem item in items)
            {
                SoftwareContainer container = item.GetService<SoftwareContainer>();
                HmiSoftware hmi = container == null ? null : container.Software as HmiSoftware;
                if (hmi != null)
                {
                    Console.WriteLine("HMI device={0} item={1}", deviceName, item.Name);
                    Console.WriteLine("SCREENS=" + hmi.Screens.Count);
                    foreach (HmiScreen screen in hmi.Screens)
                    {
                        Console.WriteLine(
                            "SCREEN name={0} number={1} size={2}x{3} items={4}",
                            screen.Name,
                            screen.ScreenNumber,
                            screen.Width,
                            screen.Height,
                            screen.ScreenItems.Count);

                        if (screen.Name.Equals(screenName, StringComparison.OrdinalIgnoreCase))
                        {
                            foreach (HmiScreenItemBase screenItem in screen.ScreenItems)
                            {
                                Console.WriteLine(
                                    "ITEM type={0} {1}",
                                    screenItem.GetType().FullName,
                                    FormatProperties(screenItem));

                                HmiAlarmControl alarmControl = screenItem as HmiAlarmControl;
                                if (alarmControl != null)
                                {
                                    Console.WriteLine("TOOLBAR_VISIBLE={0}; TOOLBAR_ENABLED={1}",
                                        alarmControl.ToolBar.Visible, alarmControl.ToolBar.Enabled);
                                    Console.WriteLine("TOOLBAR_VISIBLE_BUTTONS={0}; TOOLBAR_VISIBLE_TOGGLES={1}",
                                        alarmControl.ToolBar.Elements.OfType<HmiControlBarButtonPart>().Count(element => element.Visible),
                                        alarmControl.ToolBar.Elements.OfType<HmiControlBarToggleSwitchPart>().Count(element => element.Visible));
                                    int toolbarIndex = 0;
                                    foreach (object element in alarmControl.ToolBar.Elements)
                                    {
                                        Console.WriteLine("TOOLBAR_ELEMENT index={0} type={1} {2}",
                                            toolbarIndex++, element.GetType().FullName, FormatAllProperties(element));
                                    }
                                    foreach (object element in alarmControl.StatusBar.Elements)
                                    {
                                        Console.WriteLine("STATUS_ELEMENT type={0} {1}",
                                            element.GetType().FullName, FormatProperties(element));
                                    }
                                }
                            }
                        }
                    }
                }

                InspectDeviceItems(deviceName, item.DeviceItems, screenName);
            }
        }

        private static string FormatProperties(object value)
        {
            List<string> parts = new List<string>();
            Type type = value.GetType();
            foreach (string propertyName in InterestingProperties)
            {
                PropertyInfo property = type.GetProperty(propertyName);
                if (property == null || !property.CanRead)
                {
                    continue;
                }

                object propertyValue;
                try
                {
                    propertyValue = property.GetValue(value, null);
                }
                catch
                {
                    continue;
                }

                if (propertyValue is MultilingualText)
                {
                    MultilingualText text = (MultilingualText)propertyValue;
                    propertyValue = string.Join(
                        " | ",
                        text.Items.Select(item => item.Language.Culture.Name + ":" + item.Text));
                }

                parts.Add(propertyName + "=" + (propertyValue ?? "<null>"));
            }
            return string.Join("; ", parts);
        }

        private static string FormatAllProperties(object value)
        {
            HmiControlBarToggleSwitchPart toggle = value as HmiControlBarToggleSwitchPart;
            if (toggle != null)
            {
                return FormatProperties(value) + string.Format(
                    "; IsAlternateState={0}; AlternateGraphic={1}; AlternateBackColor={2}",
                    toggle.IsAlternateState,
                    toggle.AlternateGraphic,
                    toggle.AlternateBackColor);
            }
            return FormatProperties(value);
        }
    }
}
