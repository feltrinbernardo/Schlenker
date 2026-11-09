using System;
using System.Drawing;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.Hmi.Globalization;
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
    internal static class RefineProductionTemperaturePresence
    {
        private const string TemperatureTag = "IOL_AL102_P7_RAW";
        private const string PresenceTag = "Product_Present_At_Pump";

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
                    if (hmi == null) throw new InvalidOperationException("HMI software not found.");
                    HmiScreen screen = hmi.Screens.Find("production");
                    if (screen == null) throw new InvalidOperationException("Production screen not found.");

                    DeleteIfPresent(screen, "REV72_Mimic_ProductTemperature_Label");
                    DeleteIfPresent(screen, "REV72_Mimic_ProductTemperature_Value");

                    HmiRectangle tank = screen.ScreenItems.Find("REV12_Mimic_Tank") as HmiRectangle;
                    if (tank == null) throw new InvalidOperationException("Production tank object not found.");

                    HmiText label = GetOrCreateText(screen, "REV72_ProductTempLabel");
                    ConfigureText(label, tank.Left + 8, tank.Top + 43, tank.Width - 16, 19,
                        "PRODUCT TEMP", Color.FromArgb(23, 46, 77), 9, HmiFontWeight.Bold);

                    HmiText value = GetOrCreateText(screen, "REV72_ProductTempValue");
                    ConfigureText(value, tank.Left + 8, tank.Top + 62, tank.Width - 16, 31,
                        "NO DATA", Color.FromArgb(23, 46, 77), 15, HmiFontWeight.Bold);
                    ReplaceScript(value, "Text",
                        "let t=HMIRuntime.Tags(\"" + TemperatureTag + "\");let v=Number(t.Read())&65535;" +
                        "let q=Number(t.QualityCode);if(t.LastError!==0||(q&192)<128){return \"NO DATA\";}" +
                        "if(v>=32768){v-=65536;}return (v*0.1).toFixed(1)+\" °C\";");

                    HmiIOField presence = screen.ScreenItems.Find("REV12_Mimic_ProductPresence") as HmiIOField;
                    if (presence == null) throw new InvalidOperationException("Product-presence indicator not found.");
                    string colorScript =
                        "let t=HMIRuntime.Tags(\"" + PresenceTag + "\");let v=Boolean(t.Read());" +
                        "let q=Number(t.QualityCode);if(t.LastError!==0||(q&192)<128){return HMIRuntime.Math.RGB(145,155,165);}" +
                        "return v?HMIRuntime.Math.RGB(28,145,82):HMIRuntime.Math.RGB(145,155,165);";
                    ReplaceScript(presence, "BackColor", colorScript);
                    ReplaceScript(presence, "ForeColor", colorScript);
                    presence.BorderColor = Color.FromArgb(145, 155, 165);

                    project.Save();
                    Console.WriteLine("SCREEN=production");
                    Console.WriteLine("TEMPERATURE_TAG=" + TemperatureTag);
                    Console.WriteLine("TEMPERATURE_FORMAT=SIGNED_INT16_X_0.1_C");
                    Console.WriteLine("TEMPERATURE_BOUNDS=" + value.Left + "," + value.Top + "," + value.Width + "," + value.Height);
                    Console.WriteLine("PRESENCE_TAG=" + PresenceTag);
                    Console.WriteLine("PRESENCE_DISPLAY=COLOR_ONLY");
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

        private static HmiText GetOrCreateText(HmiScreen screen, string name)
        {
            HmiScreenItemBase existing = screen.ScreenItems.Find(name);
            if (existing != null)
            {
                HmiText text = existing as HmiText;
                if (text == null) throw new InvalidOperationException(name + " exists with the wrong type.");
                return text;
            }
            return screen.ScreenItems.Create<HmiText>(name);
        }

        private static void DeleteIfPresent(HmiScreen screen, string name)
        {
            HmiScreenItemBase item = screen.ScreenItems.Find(name);
            if (item != null) item.Delete();
        }

        private static void ConfigureText(HmiText item, int left, int top, uint width, uint height,
            string text, Color fore, float size, HmiFontWeight weight)
        {
            item.Left = left; item.Top = top; item.Width = width; item.Height = height;
            string rich = "<body><p>" + text + "</p></body>";
            foreach (MultilingualTextItem language in item.Text.Items) language.Text = rich;
            item.ForeColor = fore;
            item.Font.Name = HmiFontName.SiemensSans;
            item.Font.Size = size;
            item.Font.Weight = weight;
            item.HorizontalTextAlignment = HmiHorizontalAlignment.Center;
            item.VerticalTextAlignment = HmiVerticalAlignment.Center;
            item.Visible = true;
            item.Enabled = false;
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
                HmiSoftware hmi = container == null ? null : container.Software as HmiSoftware;
                if (hmi != null) return hmi;
                hmi = FindHmi(item.DeviceItems);
                if (hmi != null) return hmi;
            }
            return null;
        }
    }
}
