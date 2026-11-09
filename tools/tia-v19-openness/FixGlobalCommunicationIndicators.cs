using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using Siemens.Engineering;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.Hmi.Globalization;
using Siemens.Engineering.HmiUnified.UI.Base;
using Siemens.Engineering.HmiUnified.UI.Controls;
using Siemens.Engineering.HmiUnified.UI.Dynamization;
using Siemens.Engineering.HmiUnified.UI.Dynamization.Script;
using Siemens.Engineering.HmiUnified.UI.Enum;
using Siemens.Engineering.HmiUnified.UI.Screens;
using Siemens.Engineering.HmiUnified.UI.Shapes;
using Siemens.Engineering.HmiUnified.UI.Widgets;
using Siemens.Engineering.SW;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;

namespace Schlenker.TiaV19
{
    internal static class FixGlobalCommunicationIndicators
    {
        private static readonly Color Green = Color.FromArgb(28, 145, 82);
        private static readonly Color Grey = Color.FromArgb(145, 155, 165);
        private static readonly Color Amber = Color.FromArgb(235, 160, 30);

        private static int Main(string[] args)
        {
            if (args.Length != 1) return 2;
            string expected = Path.GetFullPath(args[0]);
            try
            {
                TiaPortalProcess process = TiaPortal.GetProcesses().Single(candidate =>
                    candidate.ProjectPath != null && Path.GetFullPath(candidate.ProjectPath.FullName)
                        .Equals(expected, StringComparison.OrdinalIgnoreCase));
                using (TiaPortal portal = process.Attach())
                {
                    Project project = portal.Projects.Single();
                    HmiSoftware hmi = FindHmi(project);
                    if (hmi == null) throw new InvalidOperationException("HMI software was not found.");

                    int globalScripts = RepairGlobalHmiQualityGates(hmi);
                    int footers = RepairAllFooters(hmi);
                    int detailPages = RepairIoLinkDetailPages(hmi);
                    int overviewCards = RepairIoLinkOverview(hmi);
                    int diagnostics = RepairDiagnostics(hmi);
                    project.Save();

                    Console.WriteLine("GLOBAL_HMI_QUALITY_SCRIPTS_UPDATED=" + globalScripts);
                    Console.WriteLine("GLOBAL_HMI_FOOTERS_UPDATED=" + footers);
                    Console.WriteLine("IOLINK_DETAIL_PAGES_UPDATED=" + detailPages);
                    Console.WriteLine("IOLINK_OVERVIEW_CARDS_UPDATED=" + overviewCards);
                    Console.WriteLine("DIAGNOSTICS_OBJECTS_UPDATED=" + diagnostics);
                    Console.WriteLine("PROJECT_SAVED=YES");
                    Console.WriteLine("DOWNLOAD_PERFORMED=NO");
                    Console.WriteLine("STATUS=PASS");
                }
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("STATUS=FAIL");
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static int RepairGlobalHmiQualityGates(HmiSoftware hmi)
        {
            const string oldFooter = "let st=HMIRuntime.Tags(\"Network_HMI_OK\");let q=Boolean(st.Read());let qc=Number(st.QualityCode);let live=st.LastError===0&&(qc&192)>=128&&q;";
            const string newFooter = "let st=HMIRuntime.Tags(\"Network_HMI_OK\");st.Read();let qc=Number(st.QualityCode);let live=st.LastError===0&&(qc&192)>=128;";
            const string oldGate = "let q=Boolean(HMIRuntime.Tags(\"Network_HMI_OK\").Read());";
            const string newGate = "let st=HMIRuntime.Tags(\"Network_HMI_OK\");st.Read();let q=st.LastError===0&&(Number(st.QualityCode)&192)>=128;";
            int updated = 0;
            foreach (HmiScreen screen in hmi.Screens)
            foreach (HmiScreenItemBase item in screen.ScreenItems)
            foreach (DynamizationBase dynamic in item.Dynamizations)
            {
                ScriptDynamization script = dynamic as ScriptDynamization;
                if (script == null || String.IsNullOrEmpty(script.ScriptCode)) continue;
                string changed = script.ScriptCode.Replace(oldFooter, newFooter).Replace(oldGate, newGate);
                if (changed == script.ScriptCode) continue;
                script.ScriptCode = changed;
                updated++;
            }
            return updated;
        }

        private static int RepairAllFooters(HmiSoftware hmi)
        {
            int updated = 0;
            foreach (HmiScreen screen in hmi.Screens)
            {
                HmiText status = screen.ScreenItems.Find("REV13_Common_BottomInfo") as HmiText;
                if (status == null) continue;
                SetQualityText(status, "Machine_State",
                    "LINK ONLINE     PLC / HMI", "NO DATA / OFFLINE     PLC / HMI");
                ReplaceScript(status, "ForeColor",
                    "let t=HMIRuntime.Tags(\"Machine_State\");t.Read();" +
                    "let live=t.LastError===0&&(Number(t.QualityCode)&192)>=128;" +
                    "return live?HMIRuntime.Math.RGB(80,205,70):HMIRuntime.Math.RGB(235,160,30);");
                HmiEllipse lamp = screen.ScreenItems.Find("REV28_Common_HmiLinkLamp") as HmiEllipse ??
                    screen.ScreenItems.Find("REV28_Home_HmiLinkLamp") as HmiEllipse;
                if (lamp != null)
                {
                    ReplaceScript(lamp, "BackColor",
                        "let t=HMIRuntime.Tags(\"Machine_State\");t.Read();" +
                        "let live=t.LastError===0&&(Number(t.QualityCode)&192)>=128;" +
                        "return live?HMIRuntime.Math.RGB(80,205,70):HMIRuntime.Math.RGB(235,160,30);");
                }
                updated++;
            }
            return updated;
        }

        private static int RepairIoLinkDetailPages(HmiSoftware hmi)
        {
            int updated = 0;
            for (int number = 100; number <= 104; number++)
            {
                string id = "AL" + number;
                HmiScreen screen = hmi.Screens.Find("io_link_al" + number);
                if (screen == null) continue;
                HmiText label = screen.ScreenItems.Find("REV18_IOL_Detail_CommLabel") as HmiText;
                string badge = number == 104 ? "REV38_IOL_AL104_Comm" : "REV70_IOL_" + id + "_Comm";
                HmiText text = screen.ScreenItems.Find(badge + "_Text") as HmiText;
                HmiRectangle back = screen.ScreenItems.Find(badge + "_Back") as HmiRectangle;
                if (label == null || text == null || back == null)
                    throw new InvalidOperationException("Incomplete communication badge on " + screen.Name + ".");
                SetText(label.Text, "PLC/HMI DATA LINK");
                SetQualityText(text, "Network_" + id + "_OK", "ONLINE", "NO DATA");
                SetQualityColor(back, "Network_" + id + "_OK");
                updated++;
            }
            return updated;
        }

        private static int RepairIoLinkOverview(HmiSoftware hmi)
        {
            HmiScreen screen = hmi.Screens.Find("io_link_overview");
            if (screen == null) throw new InvalidOperationException("IO-Link overview screen was not found.");
            int updated = 0;
            for (int number = 100; number <= 104; number++)
            {
                string id = "AL" + number;
                HmiText label = screen.ScreenItems.Find("REV18_IOL_" + id + "_CommLabel") as HmiText;
                HmiIOField legacy = screen.ScreenItems.Find("REV18_IOL_" + id + "_Comm") as HmiIOField;
                HmiText text = screen.ScreenItems.Find("REV39_IOL_OVERVIEW_" + id + "_Comm_Text") as HmiText;
                HmiRectangle back = screen.ScreenItems.Find("REV39_IOL_OVERVIEW_" + id + "_Comm_Back") as HmiRectangle;
                if (text == null) text = screen.ScreenItems.OfType<HmiText>().FirstOrDefault(item =>
                    item.Name.IndexOf(id, StringComparison.OrdinalIgnoreCase) >= 0 &&
                    item.Name.EndsWith("_Comm_Text", StringComparison.OrdinalIgnoreCase));
                if (back == null) back = screen.ScreenItems.OfType<HmiRectangle>().FirstOrDefault(item =>
                    item.Name.IndexOf(id, StringComparison.OrdinalIgnoreCase) >= 0 &&
                    item.Name.EndsWith("_Comm_Back", StringComparison.OrdinalIgnoreCase));
                if ((text == null || back == null) && legacy != null)
                {
                    int left = GetInt(legacy, "Left");
                    int top = GetInt(legacy, "Top");
                    uint width = GetUInt(legacy, "Width");
                    uint height = GetUInt(legacy, "Height");
                    legacy.Visible = false;
                    legacy.Enabled = false;
                    if (back == null) back = screen.ScreenItems.Create<HmiRectangle>("REV39_IOL_OVERVIEW_" + id + "_Comm_Back");
                    if (text == null) text = screen.ScreenItems.Create<HmiText>("REV39_IOL_OVERVIEW_" + id + "_Comm_Text");
                    SetBounds(back, left, top, width, height);
                    back.BackColor = Grey;
                    back.BorderColor = Grey;
                    back.BorderWidth = 0;
                    back.BackFillPattern = HmiFillPattern.Solid;
                    SetBounds(text, left + 2, top, width - 4, height);
                    SetText(text.Text, "NO DATA");
                    text.ForeColor = Color.White;
                    text.Font.Name = HmiFontName.SiemensSans;
                    text.Font.Size = 9;
                    text.Font.Weight = HmiFontWeight.Bold;
                    text.HorizontalTextAlignment = HmiHorizontalAlignment.Center;
                    text.VerticalTextAlignment = HmiVerticalAlignment.Center;
                }
                if (label == null || text == null || back == null) continue;
                SetText(label.Text, "PLC/HMI DATA LINK");
                SetQualityText(text, "Network_" + id + "_OK", "ONLINE", "NO DATA");
                SetQualityColor(back, "Network_" + id + "_OK");
                updated++;
            }
            return updated;
        }

        private static int RepairDiagnostics(HmiSoftware hmi)
        {
            HmiScreen screen = hmi.Screens.Find("diagnostics");
            if (screen == null) throw new InvalidOperationException("Diagnostics screen was not found.");
            string[] unmapped = { "Network", "PilzNetwork", "IOLink1", "IOLink2", "IOLink3", "IOLink4", "G120C" };
            int updated = 0;
            foreach (string suffix in unmapped)
            {
                HmiText text = screen.ScreenItems.Find("REV31_Diagnostics_" + suffix + "_Text") as HmiText;
                HmiRectangle back = screen.ScreenItems.Find("REV31_Diagnostics_" + suffix + "_Back") as HmiRectangle;
                if (text == null || back == null) continue;
                RemoveDynamic(text, "Text");
                RemoveDynamic(back, "BackColor");
                SetText(text.Text, "NOT MAPPED");
                back.BackColor = Amber;
                updated += 2;
            }

            HmiText hmiText = screen.ScreenItems.Find("REV31_Diagnostics_HmiCommunication_Text") as HmiText;
            HmiRectangle hmiBack = screen.ScreenItems.Find("REV31_Diagnostics_HmiCommunication_Back") as HmiRectangle;
            HmiText dataText = screen.ScreenItems.Find("REV31_Diagnostics_DataQuality_Text") as HmiText;
            HmiRectangle dataBack = screen.ScreenItems.Find("REV31_Diagnostics_DataQuality_Back") as HmiRectangle;
            if (hmiText != null && hmiBack != null)
            {
                SetQualityText(hmiText, "Machine_State", "CONNECTED", "NO DATA");
                SetQualityColor(hmiBack, "Machine_State");
                updated += 2;
            }
            if (dataText != null && dataBack != null)
            {
                SetQualityText(dataText, "Machine_State", "LIVE COMMUNICATION", "NO DATA / OFFLINE");
                SetQualityColor(dataBack, "Machine_State");
                updated += 2;
            }
            return updated;
        }

        private static void SetQualityText(HmiText item, string tag, string online, string offline)
        {
            ReplaceScript(item, "Text",
                "let t=HMIRuntime.Tags(\"" + tag + "\");t.Read();" +
                "let live=t.LastError===0&&(Number(t.QualityCode)&192)>=128;" +
                "return live?\"" + online + "\":\"" + offline + "\";");
        }

        private static void SetQualityColor(HmiRectangle item, string tag)
        {
            ReplaceScript(item, "BackColor",
                "let t=HMIRuntime.Tags(\"" + tag + "\");t.Read();" +
                "let live=t.LastError===0&&(Number(t.QualityCode)&192)>=128;" +
                "return live?HMIRuntime.Math.RGB(" + Green.R + "," + Green.G + "," + Green.B + ")" +
                ":HMIRuntime.Math.RGB(" + Grey.R + "," + Grey.G + "," + Grey.B + ");");
        }

        private static void ReplaceScript(HmiScreenItemBase item, string property, string code)
        {
            RemoveDynamic(item, property);
            ScriptDynamization dynamic = item.Dynamizations.Create<ScriptDynamization>(property);
            dynamic.Async = false;
            dynamic.Trigger.Type = TriggerType.T500ms;
            dynamic.ScriptCode = code;
        }

        private static void RemoveDynamic(HmiScreenItemBase item, string property)
        {
            DynamizationBase existing = item.Dynamizations.Find(property);
            if (existing != null) existing.Delete();
        }

        private static void SetText(MultilingualText text, string value)
        {
            string rich = "<body><p>" + value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;") + "</p></body>";
            foreach (MultilingualTextItem item in text.Items) item.Text = rich;
        }

        private static void SetBounds(HmiScreenItemBase item, int left, int top, uint width, uint height)
        {
            SetProperty(item, "Left", left);
            SetProperty(item, "Top", top);
            SetProperty(item, "Width", width);
            SetProperty(item, "Height", height);
        }

        private static void SetProperty(object target, string name, object value)
        {
            PropertyInfo property = target.GetType().GetProperty(name);
            if (property == null || !property.CanWrite) throw new InvalidOperationException("Property is not writable: " + name);
            property.SetValue(target, value, null);
        }

        private static int GetInt(object target, string name)
        {
            return Convert.ToInt32(target.GetType().GetProperty(name).GetValue(target, null));
        }

        private static uint GetUInt(object target, string name)
        {
            return Convert.ToUInt32(target.GetType().GetProperty(name).GetValue(target, null));
        }

        private static HmiSoftware FindHmi(Project project)
        {
            foreach (Device device in project.Devices.Concat(project.UngroupedDevicesGroup.Devices))
            {
                HmiSoftware found = FindHmi(device.DeviceItems);
                if (found != null) return found;
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
