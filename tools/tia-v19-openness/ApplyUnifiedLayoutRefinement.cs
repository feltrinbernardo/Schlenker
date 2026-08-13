using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.Compiler;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.HmiUnified.UI.Base;
using Siemens.Engineering.HmiUnified.UI.Screens;
using Siemens.Engineering.HmiUnified.UI.Shapes;

namespace Schlenker.TiaV19
{
    internal static class ApplyUnifiedLayoutRefinement
    {
        private static readonly string[] Screens =
        {
            "home", "operate", "cip", "function", "alarms", "recipe", "setup", "manual", "trends", "diagnostics"
        };

        private static int Main(string[] args)
        {
            string projectHint = args.Length > 0 ? args[0] : "schlenkers 36-10 190036.ap19";
            try
            {
                TiaPortalProcess selected = TiaPortal.GetProcesses().FirstOrDefault(
                    process => process.ProjectPath != null &&
                        process.ProjectPath.FullName.EndsWith(projectHint, StringComparison.OrdinalIgnoreCase));
                if (selected == null)
                {
                    Console.Error.WriteLine("STATUS=FAIL");
                    Console.Error.WriteLine("No open TIA project matched: " + projectHint);
                    return 2;
                }

                using (TiaPortal portal = selected.Attach())
                {
                    Project project = portal.Projects.FirstOrDefault();
                    Device hmiDevice = null;
                    DeviceItem hmiItem = null;
                    HmiSoftware hmi = project == null ? null : FindUnifiedHmi(project, out hmiDevice, out hmiItem);
                    if (hmi == null)
                    {
                        throw new InvalidOperationException("No WinCC Unified HMI software found.");
                    }

                    int hidden = 0;
                    foreach (string screenName in Screens)
                    {
                        HmiScreen screen = hmi.Screens.Find(screenName);
                        if (screen == null)
                        {
                            throw new InvalidOperationException("Required screen is missing: " + screenName);
                        }
                        HmiScreenItemBase legacy = screen.ScreenItems.Find("REV12_Nav_OPERATE");
                        if (legacy != null)
                        {
                            legacy.Visible = false;
                            legacy.Enabled = false;
                            hidden++;
                        }
                    }

                    HmiScreen home = hmi.Screens.Find("home");
                    foreach (string name in new[]
                    {
                        "REV12_Label_AlarmCritical", "REV12_Value_AlarmCritical",
                        "REV12_Label_AlarmWarning", "REV12_Value_AlarmWarning",
                        "REV12_Label_NetworkOK", "REV12_Value_NetworkOK",
                        "REV12_Label_CapperOutfeedCrash", "REV12_Value_CapperOutfeedCrash",
                        "REV12_Alarm_Note"
                    })
                    {
                        HmiScreenItemBase stale = home.ScreenItems.Find(name);
                        if (stale != null)
                        {
                            stale.Visible = false;
                            stale.Enabled = false;
                        }
                    }

                    SetBounds(home.ScreenItems.Find("REV12_Label_RunOutActive"), 1020, 604, 95, 28);
                    SetBounds(home.ScreenItems.Find("REV12_Value_RunOutActive"), 1120, 602, 55, 32);
                    SetBounds(home.ScreenItems.Find("REV12_Label_RunOutCompleted"), 1020, 642, 95, 28);
                    SetBounds(home.ScreenItems.Find("REV12_Value_RunOutCompleted"), 1120, 640, 55, 32);

                    foreach (string name in new[] { "REV12_Infeed_Block", "REV12_Filler_Block", "REV12_Capper_Block", "REV12_Outfeed_Block", "REV12_Filter_Warn_Back" })
                    {
                        HmiRectangle rectangle = home.ScreenItems.Find(name) as HmiRectangle;
                        if (rectangle == null)
                        {
                            throw new InvalidOperationException("Rounded layout object is missing: " + name);
                        }
                        rectangle.Corners.TopLeftRadius = 12;
                        rectangle.Corners.TopRightRadius = 12;
                        rectangle.Corners.BottomLeftRadius = 12;
                        rectangle.Corners.BottomRightRadius = 12;
                    }

                    ICompilable compilable = hmiDevice.GetService<ICompilable>() ?? hmiItem.GetService<ICompilable>();
                    if (compilable == null)
                    {
                        throw new InvalidOperationException("TIA did not expose the HMI compiler service.");
                    }
                    CompilerResult result = compilable.Compile();
                    Console.WriteLine("LEGACY_BUTTONS_HIDDEN=" + hidden);
                    Console.WriteLine("COMPILE_ERRORS=" + result.ErrorCount);
                    Console.WriteLine("COMPILE_WARNINGS=" + result.WarningCount);
                    if (result.ErrorCount != 0 || result.WarningCount != 0)
                    {
                        Console.Error.WriteLine("STATUS=FAIL_NOT_SAVED");
                        return 3;
                    }
                    project.Save();
                    Console.WriteLine("PROJECT=" + project.Path.FullName);
                    Console.WriteLine("STATUS=PASS");
                    return 0;
                }
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("STATUS=FAIL");
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static HmiSoftware FindUnifiedHmi(Project project, out Device hmiDevice, out DeviceItem hmiItem)
        {
            hmiDevice = null;
            hmiItem = null;
            foreach (Device device in project.Devices)
            {
                HmiSoftware hmi = FindUnifiedHmi(device.DeviceItems, out hmiItem);
                if (hmi != null)
                {
                    hmiDevice = device;
                    return hmi;
                }
            }
            return null;
        }

        private static void SetBounds(HmiScreenItemBase item, int left, int top, uint width, uint height)
        {
            if (item == null)
            {
                throw new InvalidOperationException("Required Home layout object is missing.");
            }
            SetProperty(item, "Left", left);
            SetProperty(item, "Top", top);
            SetProperty(item, "Width", width);
            SetProperty(item, "Height", height);
        }

        private static void SetProperty(object target, string name, object value)
        {
            System.Reflection.PropertyInfo property = target.GetType().GetProperty(name);
            if (property == null || !property.CanWrite)
            {
                throw new InvalidOperationException("Property is not writable: " + name);
            }
            property.SetValue(target, value, null);
        }

        private static HmiSoftware FindUnifiedHmi(IEnumerable<DeviceItem> items, out DeviceItem found)
        {
            found = null;
            foreach (DeviceItem item in items)
            {
                SoftwareContainer container = item.GetService<SoftwareContainer>();
                HmiSoftware hmi = container == null ? null : container.Software as HmiSoftware;
                if (hmi != null)
                {
                    found = item;
                    return hmi;
                }
                HmiSoftware nested = FindUnifiedHmi(item.DeviceItems, out found);
                if (nested != null)
                {
                    return nested;
                }
            }
            return null;
        }
    }
}
