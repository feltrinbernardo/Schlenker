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
using Siemens.Engineering.HmiUnified.UI.Widgets;
using Siemens.Engineering.SW;

namespace Schlenker.TiaV19
{
    internal static class ConnectAl102VacuumHmi
    {
        private const string TextScript =
            "let t=HMIRuntime.Tags(\"IOL_AL102_P1_RAW\");let w=Number(t.Read())&65535;" +
            "let q=Number(t.QualityCode);if(t.LastError!==0||(q&192)<128){return \"NO DATA\";}" +
            "let p=(w>>2)&16383;if(p>=8192){p-=16384;}return (p*10).toFixed(1);";

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
                    int updated = 0;
                    updated += UpdateFields(hmi, "production", new[] {
                        "REF09_MimicVacuumValue", "REV12_Exact_Kpi_Value_Vacuum" }, true);
                    updated += UpdateTexts(hmi, "production", new[] {
                        "REV14_Mimic_Vacuum", "REV12_Mimic_Vacuum" });
                    updated += UpdateTexts(hmi, "home", new[] { "REV14_Home_Vacuum" });
                    if (updated < 1) throw new InvalidOperationException("The production VS100 vacuum value was not found.");
                    project.Save();
                    Console.WriteLine("SOURCE=AL102_X1_PN7094;%IW355;PRESSURE_BITS=%X2..%X15");
                    Console.WriteLine("DESTINATION=PRODUCTION_VS100_VACUUM");
                    Console.WriteLine("DISPLAY=mbar_signed");
                    Console.WriteLine("UPDATED=" + updated);
                    Console.WriteLine("STATUS=PASS");
                }
                return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine("STATUS=FAIL"); Console.Error.WriteLine(ex); return 1; }
        }

        private static int UpdateFields(HmiSoftware hmi, string screenName, IEnumerable<string> names, bool required)
        {
            HmiScreen screen = hmi.Screens.Find(screenName);
            if (screen == null) { if (required) throw new InvalidOperationException("Missing screen: " + screenName); return 0; }
            int count = 0;
            foreach (string name in names)
            {
                HmiIOField item = screen.ScreenItems.Find(name) as HmiIOField;
                if (item == null) continue;
                DynamizationBase old = item.Dynamizations.Find("ProcessValue");
                if (old != null) old.Delete();
                TagDynamization dynamic = item.Dynamizations.Create<TagDynamization>("ProcessValue");
                dynamic.Tag = "Vacuum_Actual_mbar";
                dynamic.ReadOnly = true;
                dynamic.UseIndirectAddressing = false;
                count++;
            }
            if (required && count == 0) throw new InvalidOperationException("Missing VS100 vacuum value on " + screenName);
            return count;
        }

        private static int UpdateTexts(HmiSoftware hmi, string screenName, IEnumerable<string> names)
        {
            HmiScreen screen = hmi.Screens.Find(screenName);
            if (screen == null) return 0;
            int count = 0;
            foreach (string name in names)
            {
                HmiText item = screen.ScreenItems.Find(name) as HmiText;
                if (item == null) continue;
                DynamizationBase old = item.Dynamizations.Find("Text");
                if (old != null) old.Delete();
                ScriptDynamization dynamic = item.Dynamizations.Create<ScriptDynamization>("Text");
                dynamic.Async = false;
                dynamic.Trigger.Type = TriggerType.T500ms;
                dynamic.ScriptCode = TextScript;
                count++;
            }
            return count;
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
                SoftwareContainer c = item.GetService<SoftwareContainer>();
                if (c != null && c.Software is HmiSoftware) return (HmiSoftware)c.Software;
                HmiSoftware hmi = FindHmi(item.DeviceItems);
                if (hmi != null) return hmi;
            }
            return null;
        }
    }
}
