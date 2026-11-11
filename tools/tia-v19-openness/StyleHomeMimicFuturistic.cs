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
using Siemens.Engineering.HmiUnified.UI.Base;
using Siemens.Engineering.HmiUnified.UI.Enum;
using Siemens.Engineering.HmiUnified.UI.Screens;
using Siemens.Engineering.HmiUnified.UI.Shapes;
using Siemens.Engineering.HmiUnified.UI.Widgets;
using Siemens.Engineering.SW;

namespace Schlenker.TiaV19
{
    internal static class StyleHomeMimicFuturistic
    {
        private const string Prefix = "REV55_Home_Flow_";
        private static readonly Color DeepNavy = Color.FromArgb(7, 27, 47);
        private static readonly Color MidNavy = Color.FromArgb(15, 52, 82);
        private static readonly Color WheelDark = Color.FromArgb(14, 42, 65);
        private static readonly Color WheelLight = Color.FromArgb(28, 67, 96);
        private static readonly Color Cyan = Color.FromArgb(56, 181, 225);
        private static readonly Color Ice = Color.FromArgb(220, 244, 252);
        private static readonly Color PocketDark = Color.FromArgb(19, 45, 68);
        private static readonly Color PocketLight = Color.FromArgb(34, 73, 103);
        private static readonly Color PocketBorder = Color.FromArgb(105, 145, 175);

        private static int Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine(
                    "Usage: StyleHomeMimicFuturistic <open-project-file-hint> <prechange-zap19> <report-path>");
                return 2;
            }

            string projectHint = args[0];
            string archivePath = Path.GetFullPath(args[1]);
            string reportPath = Path.GetFullPath(args[2]);
            try
            {
                if (!File.Exists(archivePath))
                    throw new FileNotFoundException("The required pre-change archive is missing.", archivePath);

                TiaPortalProcess process = TiaPortal.GetProcesses().SingleOrDefault(candidate =>
                    candidate.ProjectPath != null && candidate.ProjectPath.FullName.EndsWith(
                        projectHint, StringComparison.OrdinalIgnoreCase));
                if (process == null)
                    throw new InvalidOperationException("No unique open TIA project matched " + projectHint + ".");

                Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
                using (TiaPortal portal = process.Attach())
                using (StreamWriter report = new StreamWriter(reportPath, false))
                {
                    Project project = portal.Projects.FirstOrDefault();
                    if (project == null)
                        throw new InvalidOperationException("The selected TIA process has no project.");

                    HmiSoftware hmi;
                    Device hmiDevice;
                    FindHmi(project, out hmi, out hmiDevice);
                    if (hmi == null || hmiDevice == null)
                        throw new InvalidOperationException("No WinCC Unified HMI software was found.");
                    HmiScreen home = hmi.Screens.Find("home");
                    if (home == null)
                        throw new InvalidOperationException("Home screen was not found.");

                    Dictionary<string, string> geometryBefore = CaptureGeometry(home);
                    Dictionary<string, int> dynamicsBefore = CaptureDynamizationCounts(home);
                    int itemCountBefore = home.ScreenItems.Count;

                    ApplyStyle(home);
                    HmiIOField speed = Require<HmiIOField>(home, "REV13_Home_MetricValue_Speed");
                    string speedFormatBefore = speed.OutputFormat;
                    speed.OutputFormat = "{I}";

                    VerifyUnchangedGeometry(home, geometryBefore);
                    VerifyDynamizationCounts(home, dynamicsBefore);
                    if (home.ScreenItems.Count != itemCountBefore)
                        throw new InvalidOperationException("The Home object count changed during style-only refinement.");
                    if (speed.OutputFormat != "{I}")
                        throw new InvalidOperationException("The speed setpoint integer format was not applied.");

                    ICompilable compiler = hmiDevice.GetService<ICompilable>();
                    if (compiler == null)
                        throw new InvalidOperationException("The HMI compiler service is unavailable.");
                    CompilerResult result = compiler.Compile();
                    WriteMessages(report, result.Messages, String.Empty);
                    report.WriteLine("CompileErrors=" + result.ErrorCount);
                    report.WriteLine("CompileWarnings=" + result.WarningCount);
                    if (result.ErrorCount != 0)
                        throw new InvalidOperationException(
                            "HMI compilation failed with " + result.ErrorCount + " errors.");

                    project.Save();
                    report.WriteLine("Project=" + project.Path.FullName);
                    report.WriteLine("Screen=home");
                    report.WriteLine("Style=INDUSTRIAL_FUTURISTIC_DARK_MIMIC");
                    report.WriteLine("HomeItemsPreserved=" + itemCountBefore);
                    report.WriteLine("GeometryObjectsVerified=" + geometryBefore.Count);
                    report.WriteLine("DynamizationCountsVerified=" + dynamicsBefore.Count);
                    report.WriteLine("SpeedSetpointFormatBefore=" + speedFormatBefore);
                    report.WriteLine("SpeedSetpointFormatAfter=" + speed.OutputFormat);
                    report.WriteLine("ObjectsCreatedOrDeleted=0");
                    report.WriteLine("DownloadOrTransferApisUsed=NO");
                    report.WriteLine("Status=PASS");

                    Console.WriteLine("PROJECT=" + project.Path.FullName);
                    Console.WriteLine("SCREEN=home");
                    Console.WriteLine("HOME_ITEMS_PRESERVED=" + itemCountBefore);
                    Console.WriteLine("GEOMETRY_OBJECTS_VERIFIED=" + geometryBefore.Count);
                    Console.WriteLine("DYNAMIZATION_COUNTS_VERIFIED=" + dynamicsBefore.Count);
                    Console.WriteLine("SPEED_SETPOINT_FORMAT={I}");
                    Console.WriteLine("COMPILE_ERRORS=" + result.ErrorCount);
                    Console.WriteLine("COMPILE_WARNINGS=" + result.WarningCount);
                    Console.WriteLine("DOWNLOAD_OR_TRANSFER=NO");
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

        private static void ApplyStyle(HmiScreen home)
        {
            HmiRectangle card = Require<HmiRectangle>(home, Prefix + "Card");
            card.BackColor = DeepNavy;
            card.AlternateBackColor = MidNavy;
            card.BackFillPattern = HmiFillPattern.GradientVertical;
            card.BorderColor = Cyan;
            card.BorderWidth = 2;
            card.Corners.TopLeftRadius = 14;
            card.Corners.TopRightRadius = 14;
            card.Corners.BottomLeftRadius = 14;
            card.Corners.BottomRightRadius = 14;

            StyleText(home, Prefix + "Title", Color.White);
            StyleText(home, Prefix + "FlowDirection", Cyan);
            StyleText(home, Prefix + "ExitDirection", Cyan);
            foreach (string name in new[]
            {
                "FillerLabel", "TransferLabel", "CapperLabel", "InfeedLabel", "OutfeedLabel"
            })
                StyleText(home, Prefix + name, Ice);

            foreach (string name in new[]
            {
                "FillerWheel", "TransferStar", "CapperWheel", "InfeedStar", "OutfeedStar"
            })
            {
                HmiEllipse wheel = Require<HmiEllipse>(home, Prefix + name);
                wheel.BackColor = WheelDark;
                wheel.AlternateBackColor = WheelLight;
                wheel.BackFillPattern = HmiFillPattern.GradientVertical;
            }

            foreach (HmiEllipse pocket in home.ScreenItems.OfType<HmiEllipse>().Where(item =>
                item.Name.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase) &&
                item.Name.IndexOf("Pocket", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                pocket.BackColor = PocketDark;
                pocket.AlternateBackColor = PocketLight;
                pocket.BackFillPattern = HmiFillPattern.GradientVertical;
                pocket.BorderColor = PocketBorder;
                pocket.BorderWidth = 1;
            }

            HmiEllipse countBack = Require<HmiEllipse>(home, Prefix + "TrackingCountBack");
            countBack.BackColor = Color.FromArgb(4, 20, 35);
            countBack.AlternateBackColor = Color.FromArgb(13, 54, 78);
            countBack.BackFillPattern = HmiFillPattern.GradientVertical;
            countBack.BorderColor = Cyan;
            countBack.BorderWidth = 2;
            StyleText(home, Prefix + "TrackingCount", Color.White);
            StyleText(home, Prefix + "TrackingCountLabel", Cyan);
            StyleText(home, Prefix + "InfeedSensorLabel", Ice);
            StyleText(home, Prefix + "CapLabel", Ice);
            StyleText(home, Prefix + "TrackingStatusText", Color.White);

            foreach (string name in new[]
            {
                "InfeedBottleSensor", "OutfeedAccumulation1", "OutfeedAccumulation2", "CapSensor"
            })
            {
                HmiEllipse sensor = Require<HmiEllipse>(home, Prefix + name);
                sensor.BorderColor = Cyan;
                sensor.BorderWidth = 2;
            }

            foreach (HmiLine line in home.ScreenItems.OfType<HmiLine>().Where(item =>
                item.Name.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)))
            {
                line.LineColor = Cyan;
                line.LineWidth = (byte)(line.Name.IndexOf("Conveyor", StringComparison.OrdinalIgnoreCase) >= 0 ? 6 : 4);
            }

            HmiRectangle status = Require<HmiRectangle>(home, Prefix + "TrackingStatusBack");
            status.Corners.TopLeftRadius = 8;
            status.Corners.TopRightRadius = 8;
            status.Corners.BottomLeftRadius = 8;
            status.Corners.BottomRightRadius = 8;
        }

        private static void StyleText(HmiScreen home, string name, Color color)
        {
            HmiText text = Require<HmiText>(home, name);
            text.ForeColor = color;
            text.Font.Name = HmiFontName.SiemensSans;
        }

        private static Dictionary<string, string> CaptureGeometry(HmiScreen home)
        {
            return home.ScreenItems.ToDictionary(
                item => item.Name,
                item => GeometryValue(item),
                StringComparer.OrdinalIgnoreCase);
        }

        private static Dictionary<string, int> CaptureDynamizationCounts(HmiScreen home)
        {
            return home.ScreenItems.ToDictionary(
                item => item.Name,
                item => item.Dynamizations.Count,
                StringComparer.OrdinalIgnoreCase);
        }

        private static void VerifyUnchangedGeometry(
            HmiScreen home, Dictionary<string, string> before)
        {
            foreach (KeyValuePair<string, string> pair in before)
            {
                HmiScreenItemBase item = home.ScreenItems.Find(pair.Key);
                if (item == null)
                    throw new InvalidOperationException("A Home object disappeared: " + pair.Key);
                string after = GeometryValue(item);
                if (!String.Equals(pair.Value, after, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Home geometry changed: " + pair.Key + " before=" + pair.Value + " after=" + after);
            }
        }

        private static void VerifyDynamizationCounts(
            HmiScreen home, Dictionary<string, int> before)
        {
            foreach (KeyValuePair<string, int> pair in before)
            {
                HmiScreenItemBase item = home.ScreenItems.Find(pair.Key);
                if (item == null || item.Dynamizations.Count != pair.Value)
                    throw new InvalidOperationException(
                        "A Home dynamization count changed: " + pair.Key + ".");
            }
        }

        private static string GeometryValue(HmiScreenItemBase item)
        {
            string[] properties =
            {
                "Left", "Top", "Width", "Height", "CenterX", "CenterY", "RadiusX", "RadiusY",
                "X1", "Y1", "X2", "Y2", "Visible", "Enabled"
            };
            List<string> values = new List<string>();
            foreach (string name in properties)
            {
                PropertyInfo property = item.GetType().GetProperty(name);
                if (property != null && property.CanRead)
                    values.Add(name + "=" + Convert.ToString(property.GetValue(item, null)));
            }
            return String.Join(";", values);
        }

        private static T Require<T>(HmiScreen screen, string name) where T : HmiScreenItemBase
        {
            T item = screen.ScreenItems.Find(name) as T;
            if (item == null)
                throw new InvalidOperationException("Required Home object is missing: " + name + ".");
            return item;
        }

        private static void FindHmi(Project project, out HmiSoftware hmi, out Device hmiDevice)
        {
            hmi = null;
            hmiDevice = null;
            foreach (Device device in project.Devices)
            foreach (DeviceItem item in device.DeviceItems)
            {
                HmiSoftware found = FindHmi(item);
                if (found != null)
                {
                    hmi = found;
                    hmiDevice = device;
                    return;
                }
            }
        }

        private static HmiSoftware FindHmi(DeviceItem item)
        {
            SoftwareContainer container = item.GetService<SoftwareContainer>();
            HmiSoftware hmi = container == null ? null : container.Software as HmiSoftware;
            if (hmi != null)
                return hmi;
            foreach (DeviceItem child in item.DeviceItems)
            {
                hmi = FindHmi(child);
                if (hmi != null)
                    return hmi;
            }
            return null;
        }

        private static void WriteMessages(
            StreamWriter report, CompilerResultMessageComposition messages, string indent)
        {
            foreach (CompilerResultMessage message in messages)
            {
                report.WriteLine(indent + message.State + " | " + message.Description + " | " + message.Path);
                WriteMessages(report, message.Messages, indent + "  ");
            }
        }
    }
}
