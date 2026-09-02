using System;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.SW;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.HmiUnified.UI.Base;
using Siemens.Engineering.HmiUnified.UI.Shapes;
using Siemens.Engineering.HmiUnified.UI.Screens;

internal static class InspectScreenNames
{
    private static int Main(string[] args)
    {
        string projectHint = args.Length > 0 ? args[0] : ".ap19";
        string screenName = args.Length > 1 ? args[1] : "production";
        TiaPortalProcess selected = TiaPortal.GetProcesses().FirstOrDefault(process =>
            process.ProjectPath != null &&
            process.ProjectPath.FullName.EndsWith(projectHint, StringComparison.OrdinalIgnoreCase));
        if (selected == null) return 2;

        using (TiaPortal portal = selected.Attach())
        {
            Project project = portal.Projects.First();
            foreach (Device device in project.Devices)
            {
                foreach (DeviceItem item in device.DeviceItems)
                {
                    SoftwareContainer container = item.GetService<SoftwareContainer>();
                    HmiSoftware hmi = container == null ? null : container.Software as HmiSoftware;
                    if (hmi == null) continue;
                    HmiScreen screen = hmi.Screens.Find(screenName);
                    if (screen == null) return 3;
                    if (args.Length > 2 && args[2].Equals("--all", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine(
                            "SCREEN Name={0}; BackColor={1},{2},{3}; AlternateBackColor={4},{5},{6}; BackFillPattern={7}; BackgroundFillMode={8}; ItemCount={9}",
                            screen.Name,
                            screen.BackColor.R, screen.BackColor.G, screen.BackColor.B,
                            screen.AlternateBackColor.R, screen.AlternateBackColor.G, screen.AlternateBackColor.B,
                            screen.BackFillPattern, screen.BackgroundFillMode, screen.ScreenItems.Count);
                        foreach (HmiScreenItemBase screenItem in screen.ScreenItems)
                        {
                            HmiGraphicView allGraphic = screenItem as HmiGraphicView;
                            Console.WriteLine("ITEM type={0}; Name={1}; Left={2}; Top={3}; Width={4}; Height={5}; Graphic={6}",
                                screenItem.GetType().Name, screenItem.Name,
                                Read(screenItem, "Left"), Read(screenItem, "Top"),
                                Read(screenItem, "Width"), Read(screenItem, "Height"),
                                allGraphic == null ? Read(screenItem, "Graphic") : allGraphic.Graphic);
                        }
                        return 0;
                    }

                    string[] names =
                    {
                        "REV13_Common_ReadyLamp", "REV13_Common_ReadyText",
                        "REV13_Common_AlarmText", "REV13_Common_AlarmValue",
                        "REV21_Common_User_Profile_Icon", "REV12_Production_User_Header",
                        "REV21_Common_DateTime", "REV14_Production_BottleSvg",
                        "REV21_SpeedGauge_BPH_Value", "REV21_SpeedGauge_Track",
                        "REV21_SpeedGauge_Fill", "REV21_SpeedGauge_Knob"
                    };
                    foreach (string name in names)
                    {
                        HmiScreenItemBase screenItem = screen.ScreenItems.Find(name);
                        if (screenItem == null)
                        {
                            Console.WriteLine("ITEM Name={0}; Status=MISSING", name);
                            continue;
                        }
                        HmiGraphicView graphic = screenItem as HmiGraphicView;
                        Console.WriteLine("ITEM type={0}; Name={1}; Left={2}; Top={3}; Width={4}; Height={5}; Graphic={6}",
                            screenItem.GetType().Name, screenItem.Name,
                            Read(screenItem, "Left"), Read(screenItem, "Top"),
                            Read(screenItem, "Width"), Read(screenItem, "Height"),
                            graphic == null ? "" : graphic.Graphic);
                    }
                    return 0;
                }
            }
        }
        return 4;
    }

    private static object Read(object value, string propertyName)
    {
        System.Reflection.PropertyInfo property = value.GetType().GetProperty(propertyName);
        return property == null ? "" : property.GetValue(value, null);
    }
}
