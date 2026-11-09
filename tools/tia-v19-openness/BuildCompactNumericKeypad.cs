using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using Siemens.Engineering;
using Siemens.Engineering.Compiler;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.Hmi.Globalization;
using Siemens.Engineering.HmiUnified.UI.Base;
using Siemens.Engineering.HmiUnified.UI.Dynamization;
using Siemens.Engineering.HmiUnified.UI.Dynamization.Script;
using Siemens.Engineering.HmiUnified.UI.Enum;
using Siemens.Engineering.HmiUnified.UI.Events;
using Siemens.Engineering.HmiUnified.UI.Screens;
using Siemens.Engineering.HmiUnified.UI.Shapes;
using Siemens.Engineering.HmiUnified.UI.Widgets;
using Siemens.Engineering.SW;

namespace Schlenker.TiaV19
{
    internal static class BuildCompactNumericKeypad
    {
        private static readonly Color Navy = Color.FromArgb(18, 51, 84);
        private static readonly Color Blue = Color.FromArgb(25, 111, 180);
        private static readonly Color Green = Color.FromArgb(28, 145, 82);
        private static readonly Color Red = Color.FromArgb(195, 45, 55);
        private static readonly Color Dark = Color.FromArgb(35, 50, 65);
        private static readonly Color Light = Color.FromArgb(242, 246, 249);
        private static readonly Color Border = Color.FromArgb(174, 188, 202);

        private sealed class Target
        {
            internal string Field;
            internal string Overlay;
            internal string Screen;
            internal string Tag;
            internal string Caption;
        }

        private static readonly Target[] Targets =
        {
            new Target { Field="REV52_Manual_Conveyor_Hz_Value", Overlay="REV71_Keypad_Conveyor", Screen="keypad_conveyor_hz", Tag="Conveyor_Manual_Frequency_Hz", Caption="CONVEYOR SETPOINT" },
            new Target { Field="REV52_Manual_Conveyor_Hz_Value_1", Overlay="REV71_Keypad_Cap", Screen="keypad_cap_hz", Tag="CapDrive_Manual_Frequency_Hz", Caption="CAP DISTRIBUTOR SETPOINT" },
            new Target { Field="REV43_Manual_Pump_Hz_Value", Overlay="REV71_Keypad_ProductPump", Screen="keypad_product_pump_hz", Tag="Product_Pump_Manual_Frequency_Hz", Caption="PRODUCT PUMP SETPOINT" }
        };

        private static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("Usage: BuildCompactNumericKeypad <exact-ap19> <report>");
                return 2;
            }
            string projectPath = Path.GetFullPath(args[0]);
            string reportPath = Path.GetFullPath(args[1]);
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            try
            {
                TiaPortalProcess process = TiaPortal.GetProcesses().SingleOrDefault(p => p.ProjectPath != null &&
                    Path.GetFullPath(p.ProjectPath.FullName).Equals(projectPath, StringComparison.OrdinalIgnoreCase));
                if (process == null) throw new InvalidOperationException("Exact TIA project is not open.");
                using (TiaPortal portal = process.Attach())
                using (StreamWriter report = new StreamWriter(reportPath, false))
                {
                    Project project = portal.Projects.Single();
                    DeviceItem hmiItem;
                    HmiSoftware hmi = FindHmi(project, out hmiItem);
                    if (hmi == null) throw new InvalidOperationException("Unified HMI was not found.");
                    HmiScreen manual = hmi.Screens.Find("manual");
                    if (manual == null) throw new InvalidOperationException("Manual screen was not found.");

                    report.WriteLine("SCHLENKER COMPACT NUMERIC KEYPAD");
                    report.WriteLine("Project=" + project.Path.FullName);
                    report.WriteLine("ManualGeometryChanged=NO");
                    report.WriteLine("Range=10.0..50.0Hz");

                    foreach (Target target in Targets)
                    {
                        HmiIOField field = manual.ScreenItems.Find(target.Field) as HmiIOField;
                        if (field == null) throw new InvalidOperationException("Required field missing: " + target.Field);
                        field.IOFieldType = HmiIOFieldType.Output;
                        HmiButton overlay = GetOrCreate<HmiButton>(manual, target.Overlay);
                        SetBounds(overlay, field.Left, field.Top, field.Width, field.Height);
                        // Avoid a numeric-only design-time caption: Unified parses it as a
                        // multilingual text format. The live tag dynamic replaces this caption.
                        StyleButton(overlay, "--", Color.White, Dark, 12);
                        overlay.BorderColor = Border;
                        ConfigureTextValue(overlay, target.Tag);
                        SetTap(overlay,
                            "HMIRuntime.UI.SysFct.OpenScreenInPopup(\"SchlenkerKeypad\",\"" + target.Screen +
                            "\",true,\"" + target.Caption + " 10-50 Hz\",470,155,true,undefined);");
                        BuildKeypad(hmi, target);
                        report.WriteLine(target.Caption + "=" + target.Tag + ";FIELD=" + target.Field +
                            ";BOUNDS=" + field.Left + "," + field.Top + "," + field.Width + "," + field.Height);
                    }

                    Device hmiDevice = FindHmiDevice(project);
                    ICompilable compiler = hmiDevice == null ? null : hmiDevice.GetService<ICompilable>();
                    if (compiler == null) throw new InvalidOperationException("HMI compiler unavailable.");
                    CompilerResult result = compiler.Compile();
                    int errors = result.ErrorCount;
                    WriteMessages(result.Messages, report, "", ref errors);
                    report.WriteLine("Errors=" + errors);
                    if (errors != 0) throw new InvalidOperationException("HMI compile failed with " + errors + " errors.");
                    project.Save();
                    report.WriteLine("SaveInvoked=YES");
                    report.WriteLine("STATUS=PASS");
                    Console.WriteLine("HMI_ERRORS=0");
                    Console.WriteLine("STATUS=PASS");
                }
                return 0;
            }
            catch (Exception ex)
            {
                File.AppendAllText(reportPath, Environment.NewLine + "STATUS=FAIL" + Environment.NewLine + ex);
                Console.Error.WriteLine("STATUS=FAIL");
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        private static void BuildKeypad(HmiSoftware hmi, Target target)
        {
            HmiScreen screen = hmi.Screens.Find(target.Screen) ?? hmi.Screens.Create(target.Screen);
            screen.Width = 300;
            screen.Height = 410;
            foreach (HmiScreenItemBase item in screen.ScreenItems.ToArray()) item.Delete();

            HmiRectangle back = screen.ScreenItems.Create<HmiRectangle>("KP_Back");
            SetBounds(back, 0, 0, 300, 410);
            back.BackColor = Light; back.BorderColor = Navy; back.BorderWidth = 2;

            HmiText title = screen.ScreenItems.Create<HmiText>("KP_Title");
            SetBounds(title, 14, 10, 272, 30); SetText(title.Text, target.Caption);
            title.ForeColor = Navy; title.Font.Size = 15; title.Font.Weight = HmiFontWeight.Bold;
            title.HorizontalTextAlignment = HmiHorizontalAlignment.Center;

            HmiText range = screen.ScreenItems.Create<HmiText>("KP_Range");
            SetBounds(range, 14, 40, 272, 22); SetText(range.Text, "VALID RANGE 10.0 - 50.0 Hz");
            range.ForeColor = Blue; range.Font.Size = 9; range.Font.Weight = HmiFontWeight.Bold;
            range.HorizontalTextAlignment = HmiHorizontalAlignment.Center;

            HmiRectangle displayBack = screen.ScreenItems.Create<HmiRectangle>("KP_DisplayBack");
            SetBounds(displayBack, 20, 70, 260, 52);
            displayBack.BackColor = Color.White; displayBack.BorderColor = Border; displayBack.BorderWidth = 2;
            HmiText display = screen.ScreenItems.Create<HmiText>("KP_Display");
            SetBounds(display, 20, 70, 260, 52); SetText(display.Text, "");
            display.ForeColor = Dark; display.Font.Size = 24; display.Font.Weight = HmiFontWeight.Bold;
            display.HorizontalTextAlignment = HmiHorizontalAlignment.Right;
            display.VerticalTextAlignment = HmiVerticalAlignment.Center;

            string[] labels = { "7","8","9","4","5","6","1","2","3","C","0","." };
            for (int i = 0; i < labels.Length; i++)
            {
                int row = i / 3, col = i % 3;
                HmiButton button = screen.ScreenItems.Create<HmiButton>("KP_" + (labels[i] == "." ? "Dot" : labels[i]));
                SetBounds(button, 20 + col * 88, 135 + row * 55, 80, 47);
                StyleButton(button, labels[i], labels[i] == "C" ? Red : Navy, Color.White, 18);
                if (labels[i] == "C") SetTap(button, "Screen.Items(\"KP_Display\").Text=\"\";");
                else if (labels[i] == ".") SetTap(button,
                    "let d=Screen.Items(\"KP_Display\");let s=String(d.Text);if(s.indexOf(\".\")<0&&s.length<5){d.Text=s.length?s+\".\":\"0.\";}");
                else SetTap(button,
                    "let d=Screen.Items(\"KP_Display\");let s=String(d.Text);if(s.length<5){d.Text=s+\"" + labels[i] + "\";}");
            }

            HmiButton cancel = screen.ScreenItems.Create<HmiButton>("KP_Cancel");
            SetBounds(cancel, 20, 360, 124, 40); StyleButton(cancel, "CANCEL", Color.FromArgb(100,110,120), Color.White, 12);
            SetTap(cancel, "Screen.Items(\"KP_Display\").Text=\"\";HMIRuntime.UI.SysFct.ClosePopup(\"/SchlenkerKeypad\");");
            HmiButton enter = screen.ScreenItems.Create<HmiButton>("KP_Enter");
            SetBounds(enter, 156, 360, 124, 40); StyleButton(enter, "ENTER", Green, Color.White, 12);
            SetTap(enter,
                "let d=Screen.Items(\"KP_Display\");let v=Number(String(d.Text));" +
                "if(Number.isFinite(v)&&v>=10&&v<=50){HMIRuntime.Tags(\"" + target.Tag + "\").Write(v);d.Text=\"\";HMIRuntime.UI.SysFct.ClosePopup(\"/SchlenkerKeypad\");}" +
                "else{d.Text=\"10.0\";}");
        }

        private static void ConfigureTextValue(HmiButton button, string tag)
        {
            DynamizationBase old = button.Dynamizations.Find("Text"); if (old != null) old.Delete();
            ScriptDynamization dyn = button.Dynamizations.Create<ScriptDynamization>("Text");
            dyn.Async = false; dyn.Trigger.Type = TriggerType.T500ms;
            dyn.ScriptCode = "let t=HMIRuntime.Tags(\"" + tag + "\");let v=Number(t.Read());return Number.isFinite(v)?v.toFixed(1):\"NO DATA\";";
        }

        private static void SetTap(HmiButton button, string script)
        {
            HmiButtonEventHandler tap = button.EventHandlers.Find(HmiButtonEventType.Tapped) ?? button.EventHandlers.Create(HmiButtonEventType.Tapped);
            tap.Script.ScriptCode = script;
        }

        private static void StyleButton(HmiButton button, string text, Color back, Color fore, int font)
        {
            SetText(button.Text, text); button.BackColor = back; button.ForeColor = fore;
            button.BorderColor = Color.White; button.BorderWidth = 1; button.Font.Size = font;
            button.Font.Weight = HmiFontWeight.Bold; button.Content.HorizontalTextAlignment = HmiHorizontalAlignment.Center;
            button.Content.VerticalTextAlignment = HmiVerticalAlignment.Center; button.Visible = true; button.Enabled = true;
            button.RequireExplicitUnlock = false;
        }

        private static T GetOrCreate<T>(HmiScreen screen, string name) where T : HmiScreenItemBase
        {
            T item = screen.ScreenItems.Find(name) as T;
            return item ?? screen.ScreenItems.Create<T>(name);
        }
        private static void SetBounds(HmiScreenItemBase item, int left, int top, uint width, uint height)
        {
            SetProperty(item, "Left", left); SetProperty(item, "Top", top);
            SetProperty(item, "Width", width); SetProperty(item, "Height", height);
            item.Visible = true; item.Enabled = true;
        }
        private static void SetProperty(object target, string name, object value)
        {
            PropertyInfo property = target.GetType().GetProperty(name);
            if (property == null || !property.CanWrite) throw new InvalidOperationException("Property not writable: " + name);
            property.SetValue(target, value, null);
        }
        private static void SetText(MultilingualText text, string value)
        {
            string rich = "<body><p>" + value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;") + "</p></body>";
            foreach (MultilingualTextItem item in text.Items) item.Text = rich;
        }

        private static HmiSoftware FindHmi(Project project, out DeviceItem hmiItem)
        {
            hmiItem = null;
            foreach (Device device in project.Devices)
            {
                HmiSoftware hmi = FindHmi(device.DeviceItems, out hmiItem); if (hmi != null) return hmi;
            }
            return null;
        }

        private static Device FindHmiDevice(Project project)
        {
            foreach (Device device in project.Devices)
            {
                DeviceItem ignored;
                if (FindHmi(device.DeviceItems, out ignored) != null) return device;
            }
            return null;
        }
        private static HmiSoftware FindHmi(DeviceItemComposition items, out DeviceItem found)
        {
            found = null;
            foreach (DeviceItem item in items)
            {
                SoftwareContainer c = item.GetService<SoftwareContainer>(); HmiSoftware h = c == null ? null : c.Software as HmiSoftware;
                if (h != null) { found = item; return h; }
                h = FindHmi(item.DeviceItems, out found); if (h != null) return h;
            }
            return null;
        }
        private static void WriteMessages(IEnumerable<CompilerResultMessage> messages, StreamWriter report, string indent, ref int errors)
        {
            foreach (CompilerResultMessage message in messages)
            {
                report.WriteLine(indent + message.State + " | " + message.Description + " | " + message.Path);
                if (message.State.ToString().Equals("Error", StringComparison.OrdinalIgnoreCase)) errors++;
                WriteMessages(message.Messages, report, indent + "  ", ref errors);
            }
        }
    }
}
