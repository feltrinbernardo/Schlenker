using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.HmiUnified.UI.Base;
using Siemens.Engineering.HmiUnified.UI.Dynamization;
using Siemens.Engineering.HmiUnified.UI.Dynamization.Script;
using Siemens.Engineering.HmiUnified.UI.Enum;
using Siemens.Engineering.HmiUnified.UI.Screens;
using Siemens.Engineering.HmiUnified.UI.Shapes;
using Siemens.Engineering.SW;

namespace Schlenker.TiaV19
{
    internal static class FixProductPresenceDisplay
    {
        private const string Tag = "Product_Present_At_Pump";

        private static int Main(string[] args)
        {
            if (args.Length != 1) return 2;
            string expected = Path.GetFullPath(args[0]);
            try
            {
                TiaPortalProcess process = TiaPortal.GetProcesses().Single(p => p.ProjectPath != null &&
                    Path.GetFullPath(p.ProjectPath.FullName).Equals(expected, StringComparison.OrdinalIgnoreCase));
                using (TiaPortal portal = process.Attach())
                {
                    Project project = portal.Projects.Single();
                    HmiSoftware hmi = FindHmi(project);
                    if (hmi == null) throw new InvalidOperationException("HMI software was not found.");

                    ConfigureStatus(hmi.Screens.Find("io_link_al103"),
                        new[] { "REV70_IOL_AL103_X1_Text", "REV71_IOL_AL103_P1_Text", "REV36_IOL_AL103_Port_1_Live_Text", "REV18_IOL_Port_1_LiveText" },
                        new[] { "REV70_IOL_AL103_X1_Back", "REV71_IOL_AL103_P1_Back", "REV36_IOL_AL103_Port_1_Live_Back" },
                        "AL103 P1");
                    ConfigureStatus(hmi.Screens.Find("manual"),
                        new[] { "REV70_Manual_ProductPresenceState" },
                        new[] { "REV70_Manual_ProductPresenceStateBack" },
                        "Manual product presence");

                    project.Save();
                    Console.WriteLine("TAG=" + Tag);
                    Console.WriteLine("AL103_P1_DISPLAY=ON/OFF/NO DATA");
                    Console.WriteLine("MANUAL_DISPLAY=ON/OFF/NO DATA");
                    Console.WriteLine("STATUS=PASS");
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("STATUS=FAIL");
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        private static void ConfigureStatus(HmiScreen screen, IEnumerable<string> textNames,
            IEnumerable<string> backNames, string description)
        {
            if (screen == null) throw new InvalidOperationException("Missing screen for " + description);
            HmiText text = Find<HmiText>(screen, textNames);
            HmiRectangle back = Find<HmiRectangle>(screen, backNames);
            if (text == null || back == null)
                throw new InvalidOperationException("Missing status objects for " + description);

            ReplaceScript(text, "Text",
                "let t=HMIRuntime.Tags(\"" + Tag + "\");let v=Boolean(t.Read());" +
                "let q=Number(t.QualityCode);if(t.LastError!==0||(q&192)<128){return \"NO DATA\";}" +
                "return v?\"ON\":\"OFF\";");
            ReplaceScript(back, "BackColor",
                "let t=HMIRuntime.Tags(\"" + Tag + "\");let v=Boolean(t.Read());" +
                "let q=Number(t.QualityCode);if(t.LastError!==0||(q&192)<128){return HMIRuntime.Math.RGB(145,155,165);}" +
                "return v?HMIRuntime.Math.RGB(28,145,82):HMIRuntime.Math.RGB(145,155,165);");
        }

        private static T Find<T>(HmiScreen screen, IEnumerable<string> names) where T : HmiScreenItemBase
        {
            foreach (string name in names)
            {
                T item = screen.ScreenItems.Find(name) as T;
                if (item != null) return item;
            }
            return null;
        }

        private static void ReplaceScript(HmiScreenItemBase item, string property, string code)
        {
            DynamizationBase old = item.Dynamizations.Find(property);
            if (old != null) old.Delete();
            ScriptDynamization dynamic = item.Dynamizations.Create<ScriptDynamization>(property);
            dynamic.Async = false;
            dynamic.Trigger.Type = TriggerType.T500ms;
            dynamic.ScriptCode = code;
        }

        private static HmiSoftware FindHmi(Project project)
        {
            foreach (Device device in project.Devices.Concat(project.UngroupedDevicesGroup.Devices))
            {
                HmiSoftware hmi = FindHmi(device.DeviceItems);
                if (hmi != null) return hmi;
            }
            return null;
        }

        private static HmiSoftware FindHmi(DeviceItemComposition items)
        {
            foreach (DeviceItem item in items)
            {
                SoftwareContainer container = item.GetService<SoftwareContainer>();
                if (container != null && container.Software is HmiSoftware) return (HmiSoftware)container.Software;
                HmiSoftware nested = FindHmi(item.DeviceItems);
                if (nested != null) return nested;
            }
            return null;
        }
    }
}
