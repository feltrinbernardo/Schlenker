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
using Siemens.Engineering.Hmi.Globalization;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.HmiUnified.HmiTags;
using Siemens.Engineering.HmiUnified.UI.Base;
using Siemens.Engineering.HmiUnified.UI.Dynamization;
using Siemens.Engineering.HmiUnified.UI.Dynamization.Script;
using Siemens.Engineering.HmiUnified.UI.Enum;
using Siemens.Engineering.HmiUnified.UI.Screens;
using Siemens.Engineering.HmiUnified.UI.Shapes;
using Siemens.Engineering.SW;

namespace Schlenker.TiaV19
{
    internal static class BuildHomeDynamicBottleMimic
    {
        private const string Prefix = "REV55_Home_Flow_";
        private static readonly Color Navy = Color.FromArgb(18, 51, 84);
        private static readonly Color Blue = Color.FromArgb(25, 111, 180);
        private static readonly Color PaleBlue = Color.FromArgb(232, 240, 248);
        private static readonly Color Grey = Color.FromArgb(205, 212, 219);
        private static readonly Color DarkGrey = Color.FromArgb(125, 145, 160);
        private static readonly Color Green = Color.FromArgb(28, 145, 82);
        private static readonly Color Amber = Color.FromArgb(235, 160, 30);
        private static readonly Color Red = Color.FromArgb(195, 45, 55);
        private static readonly Color Yellow = Color.FromArgb(250, 205, 55);

        private sealed class WheelGeometry
        {
            internal readonly int X;
            internal readonly int Y;
            internal readonly int Radius;

            internal WheelGeometry(int x, int y, int radius)
            {
                X = x;
                Y = y;
                Radius = radius;
            }
        }

        private sealed class MimicLayout
        {
            internal WheelGeometry Filler;
            internal WheelGeometry Transfer;
            internal WheelGeometry Capper;
            internal WheelGeometry Infeed;
            internal WheelGeometry Outfeed;
        }

        private static int Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine(
                    "Usage: BuildHomeDynamicBottleMimic <open-project-file-hint> <existing-prechange-zap19> <report-path>");
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
                    if (project == null) throw new InvalidOperationException("The selected TIA process has no project.");
                    HmiSoftware hmi;
                    Device hmiDevice;
                    FindHmi(project, out hmi, out hmiDevice);
                    if (hmi == null || hmiDevice == null)
                        throw new InvalidOperationException("No WinCC Unified HMI software was found.");

                    HmiScreen home = hmi.Screens.Find("home");
                    if (home == null) throw new InvalidOperationException("Home screen was not found.");

                    Dictionary<string, string> preserved = CapturePreservedGeometry(home);
                    MimicLayout operatorLayout = CaptureOperatorLayout(home);
                    EnsureTag(hmi, "Bottle_Tracking_Valid", "DB_Global.Machine.TrackingValid",
                        "[Home] Encoder-based bottle tracking valid; false until commissioning is complete.");
                    EnsureTag(hmi, "Bottle_Tracking_Count", "DB_Global.OpenPoints.Tracking.Count",
                        "[Home] Number of active records in the commissioned encoder-based bottle tracker.");

                    int deletedOverviewObjects = ClearOverview(home);
                    BuildMimic(home, operatorLayout);
                    VerifyPreservedGeometry(home, preserved);
                    VerifyMimic(home, operatorLayout);

                    ICompilable compiler = hmiDevice.GetService<ICompilable>();
                    if (compiler == null)
                        throw new InvalidOperationException("The HMI compiler service is unavailable.");
                    CompilerResult result = compiler.Compile();
                    WriteCompilerMessages(report, result.Messages, "");
                    report.WriteLine("CompileErrors=" + result.ErrorCount);
                    report.WriteLine("CompileWarnings=" + result.WarningCount);
                    if (result.ErrorCount != 0)
                        throw new InvalidOperationException("HMI compilation failed with " + result.ErrorCount + " errors.");

                    project.Save();
                    report.WriteLine("Project=" + project.Path.FullName);
                    report.WriteLine("Screen=home");
                    report.WriteLine("ManualAlignmentPreservedObjects=" + preserved.Count);
                    report.WriteLine("OverviewObjectsDeletedBeforeCleanRebuild=" + deletedOverviewObjects);
                    WriteLayout(report, operatorLayout);
                    report.WriteLine("FillerRadiusDerivedFromInfeedAndTransferPitch=YES");
                    report.WriteLine("PocketPitchGeometry=FILLER_RING:79/36;TEN_POCKET_RING:22/10");
                    report.WriteLine("DynamicPhaseSynchronization=NOT_COMMISSIONED_REQUIRES_ENCODER_ANGLE_AND_OFFSETS");
                    report.WriteLine("PocketCounts=FILLER:36;INFEED:10;TRANSFER:10;CAPPER:10;OUTFEED:10");
                    report.WriteLine("DownloadOrTransferApisUsed=NO");
                    report.WriteLine("PhysicalBottlePositionClaimed=NO");
                    report.WriteLine("TrackingState=GATED_BY_Bottle_Tracking_Valid");
                    report.WriteLine("Status=PASS");

                    Console.WriteLine("PROJECT=" + project.Path.FullName);
                    Console.WriteLine("SCREEN=home");
                    Console.WriteLine("PRESERVED_HOME_OBJECTS=" + preserved.Count);
                    Console.WriteLine("HMI_TAGS_ADDED_OR_CONFIRMED=2");
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

        private static MimicLayout CaptureOperatorLayout(HmiScreen screen)
        {
            MimicLayout layout = new MimicLayout
            {
                Filler = ReadWheel(screen, Prefix + "FillerWheel", 620, 260, 78),
                Transfer = ReadFirstWheel(screen,
                    new[] { Prefix + "TransferStar", Prefix + "InfeedStar_1" }, 493, 275, 30),
                Capper = ReadWheel(screen, Prefix + "CapperWheel", 400, 260, 47),
                Infeed = ReadWheel(screen, Prefix + "InfeedStar", 565, 365, 30),
                Outfeed = ReadWheel(screen, Prefix + "OutfeedStar", 405, 365, 30)
            };
            int fillerRadius = CalculateFillerPitchRadius(
                layout.Filler, layout.Infeed, layout.Transfer);
            layout.Filler = new WheelGeometry(layout.Filler.X, layout.Filler.Y, fillerRadius);
            return layout;
        }

        private static int CalculateFillerPitchRadius(
            WheelGeometry filler, WheelGeometry infeed, WheelGeometry transfer)
        {
            double toInfeed = Distance(filler, infeed) - infeed.Radius;
            double toTransfer = Distance(filler, transfer) - transfer.Radius;
            int radius = (int)Math.Round(Math.Min(toInfeed, toTransfer));
            if (radius < 60 || radius > 100)
                throw new InvalidOperationException(
                    "The operator-locked centers imply an invalid filler pitch radius: " + radius + ".");
            return radius;
        }

        private static double Distance(WheelGeometry first, WheelGeometry second)
        {
            double dx = second.X - first.X;
            double dy = second.Y - first.Y;
            return Math.Sqrt((dx * dx) + (dy * dy));
        }

        private static WheelGeometry ReadFirstWheel(
            HmiScreen screen, IEnumerable<string> names, int fallbackX, int fallbackY, int fallbackRadius)
        {
            foreach (string name in names)
            {
                HmiEllipse ellipse = screen.ScreenItems.Find(name) as HmiEllipse;
                if (ellipse != null)
                    return ValidateWheel(name, ellipse, fallbackRadius);
            }
            return new WheelGeometry(fallbackX, fallbackY, fallbackRadius);
        }

        private static WheelGeometry ReadWheel(
            HmiScreen screen, string name, int fallbackX, int fallbackY, int fallbackRadius)
        {
            HmiEllipse ellipse = screen.ScreenItems.Find(name) as HmiEllipse;
            return ellipse == null
                ? new WheelGeometry(fallbackX, fallbackY, fallbackRadius)
                : ValidateWheel(name, ellipse, fallbackRadius);
        }

        private static WheelGeometry ValidateWheel(string name, HmiEllipse ellipse, int standardRadius)
        {
            if (ellipse.CenterX < 267 || ellipse.CenterX > 750 ||
                ellipse.CenterY < 135 || ellipse.CenterY > 468 ||
                ellipse.RadiusX < 15 || ellipse.RadiusX > 100 || ellipse.RadiusX != ellipse.RadiusY)
                throw new InvalidOperationException("Operator wheel geometry is invalid: " + name + ".");
            return new WheelGeometry(ellipse.CenterX, ellipse.CenterY, standardRadius);
        }

        private static void WriteLayout(StreamWriter report, MimicLayout layout)
        {
            report.WriteLine("OperatorLockedFiller=" + FormatWheel(layout.Filler));
            report.WriteLine("OperatorLockedTransfer=" + FormatWheel(layout.Transfer));
            report.WriteLine("OperatorLockedCapper=" + FormatWheel(layout.Capper));
            report.WriteLine("OperatorLockedInfeed=" + FormatWheel(layout.Infeed));
            report.WriteLine("OperatorLockedOutfeed=" + FormatWheel(layout.Outfeed));
        }

        private static string FormatWheel(WheelGeometry wheel)
        {
            return "X:" + wheel.X + ";Y:" + wheel.Y + ";R:" + wheel.Radius;
        }

        private static void BuildMimic(HmiScreen screen, MimicLayout layout)
        {
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, Prefix + "Card"),
                267, 135, 483, 333, Color.White, Color.FromArgb(225, 230, 235), 0);
            ConfigureText(GetOrCreate<HmiText>(screen, Prefix + "Title"),
                286, 152, 445, 31, "SCHLENKER MONOBLOCK OVERVIEW", Navy, 17,
                HmiFontWeight.Bold, HmiHorizontalAlignment.Left);

            // Final physical arrangement supplied by the operator: flow enters from
            // the right through INFEED, then FILLER, TRANSFER, CAPPER and OUTFEED,
            // leaving towards the left.
            ConfigureText(GetOrCreate<HmiText>(screen, Prefix + "FlowDirection"),
                646, layout.Infeed.Y + layout.Infeed.Radius + 3, 76, 20, "FLOW  ←", Navy, 10,
                HmiFontWeight.Bold, HmiHorizontalAlignment.Right);
            ConfigureText(GetOrCreate<HmiText>(screen, Prefix + "ExitDirection"),
                275, layout.Outfeed.Y + layout.Outfeed.Radius + 3, 72, 18, "←  OUT", Navy, 9,
                HmiFontWeight.Bold, HmiHorizontalAlignment.Left);

            ConfigureLine(GetOrCreate<HmiLine>(screen, Prefix + "ConveyorRight"),
                layout.Infeed.X + layout.Infeed.Radius - 5,
                layout.Infeed.Y + layout.Infeed.Radius - 1,
                722, layout.Infeed.Y + layout.Infeed.Radius - 1, DarkGrey, 5);
            ConfigureConnectorBetweenCircles(
                GetOrCreate<HmiLine>(screen, Prefix + "TransferRight"),
                layout.Infeed, layout.Filler, DarkGrey, 4);
            ConfigureConnectorBetweenCircles(
                GetOrCreate<HmiLine>(screen, Prefix + "TransferLeft"),
                layout.Filler, layout.Transfer, DarkGrey, 4);
            ConfigureConnectorBetweenCircles(
                GetOrCreate<HmiLine>(screen, Prefix + "CapperTransfer"),
                layout.Transfer, layout.Capper, DarkGrey, 4);
            ConfigureConnectorBetweenCircles(
                GetOrCreate<HmiLine>(screen, Prefix + "CapperOutfeedTransfer"),
                layout.Capper, layout.Outfeed, DarkGrey, 4);
            ConfigureLine(GetOrCreate<HmiLine>(screen, Prefix + "ConveyorLeft"),
                280, layout.Outfeed.Y + layout.Outfeed.Radius + 4,
                layout.Outfeed.X - layout.Outfeed.Radius + 2,
                layout.Outfeed.Y + Math.Min(20, layout.Outfeed.Radius - 3), DarkGrey, 5);

            ConfigureDriveLineDynamization(screen.ScreenItems.Find(Prefix + "ConveyorRight"),
                "VSD_Conveyor_Feedback_Valid", "VSD_Conveyor_Running", "VSD_Conveyor_Fault");
            ConfigureDriveLineDynamization(screen.ScreenItems.Find(Prefix + "ConveyorLeft"),
                "VSD_Conveyor_Feedback_Valid", "VSD_Conveyor_Running", "VSD_Conveyor_Fault");
            foreach (string internalLine in new[]
                { "TransferRight", "TransferLeft", "CapperTransfer", "CapperOutfeedTransfer" })
            {
                ConfigureDriveLineDynamization(screen.ScreenItems.Find(Prefix + internalLine),
                    "VSD_Main_Feedback_Valid", "VSD_Main_Running", "VSD_Main_Fault");
            }

            ConfigureWheel(GetOrCreate<HmiEllipse>(screen, Prefix + "FillerWheel"),
                layout.Filler.X, layout.Filler.Y, layout.Filler.Radius, PaleBlue, Blue, 5);
            ConfigureWheel(GetOrCreate<HmiEllipse>(screen, Prefix + "TransferStar"),
                layout.Transfer.X, layout.Transfer.Y, layout.Transfer.Radius, PaleBlue, Blue, 4);
            ConfigureWheel(GetOrCreate<HmiEllipse>(screen, Prefix + "CapperWheel"),
                layout.Capper.X, layout.Capper.Y, layout.Capper.Radius, PaleBlue, Blue, 4);
            ConfigureWheel(GetOrCreate<HmiEllipse>(screen, Prefix + "InfeedStar"),
                layout.Infeed.X, layout.Infeed.Y, layout.Infeed.Radius, PaleBlue, Blue, 3);
            ConfigureWheel(GetOrCreate<HmiEllipse>(screen, Prefix + "OutfeedStar"),
                layout.Outfeed.X, layout.Outfeed.Y, layout.Outfeed.Radius, PaleBlue, Blue, 3);

            ConfigureDriveWheelDynamization(screen.ScreenItems.Find(Prefix + "FillerWheel"),
                "VSD_Main_Feedback_Valid", "VSD_Main_Running", "VSD_Main_Fault");
            ConfigureDriveWheelDynamization(screen.ScreenItems.Find(Prefix + "TransferStar"),
                "VSD_Main_Feedback_Valid", "VSD_Main_Running", "VSD_Main_Fault");
            ConfigureDriveWheelDynamization(screen.ScreenItems.Find(Prefix + "CapperWheel"),
                "VSD_Main_Feedback_Valid", "VSD_Main_Running", "VSD_Main_Fault");
            ConfigureDriveWheelDynamization(screen.ScreenItems.Find(Prefix + "InfeedStar"),
                "VSD_Conveyor_Feedback_Valid", "VSD_Conveyor_Running", "VSD_Conveyor_Fault");
            ConfigureDriveWheelDynamization(screen.ScreenItems.Find(Prefix + "OutfeedStar"),
                "VSD_Conveyor_Feedback_Valid", "VSD_Conveyor_Running", "VSD_Conveyor_Fault");

            int commonTenPocketRadius = layout.Infeed.Radius - 8;
            int fillerPocketRadius = (int)Math.Round(commonTenPocketRadius * 36.0 / 10.0);
            AddPockets(screen, "Filler", layout.Filler.X, layout.Filler.Y,
                fillerPocketRadius, 36, 3);
            AddPockets(screen, "Transfer", layout.Transfer.X, layout.Transfer.Y,
                commonTenPocketRadius, 10, 4);
            AddPockets(screen, "Capper", layout.Capper.X, layout.Capper.Y,
                commonTenPocketRadius, 10, 4);
            AddPockets(screen, "Infeed", layout.Infeed.X, layout.Infeed.Y,
                commonTenPocketRadius, 10, 4);
            AddPockets(screen, "Outfeed", layout.Outfeed.X, layout.Outfeed.Y,
                commonTenPocketRadius, 10, 4);

            ConfigureText(GetOrCreate<HmiText>(screen, Prefix + "FillerLabel"),
                layout.Filler.X - 42, layout.Filler.Y - 11, 84, 22, "FILLER 36", Navy, 12,
                HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureText(GetOrCreate<HmiText>(screen, Prefix + "CapperLabel"),
                layout.Capper.X - 35, layout.Capper.Y - 11, 70, 22, "CAPPER", Navy, 10,
                HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureText(GetOrCreate<HmiText>(screen, Prefix + "TransferLabel"),
                layout.Transfer.X - 32, layout.Transfer.Y - 9, 64, 18, "TRANSFER", Navy, 8,
                HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureText(GetOrCreate<HmiText>(screen, Prefix + "InfeedLabel"),
                layout.Infeed.X - 38, layout.Infeed.Y - 10, 76, 20, "INFEED", Navy, 10,
                HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureText(GetOrCreate<HmiText>(screen, Prefix + "OutfeedLabel"),
                layout.Outfeed.X - 39, layout.Outfeed.Y - 10, 78, 20, "OUTFEED", Navy, 9,
                HmiFontWeight.Bold, HmiHorizontalAlignment.Center);

            HmiEllipse countBack = GetOrCreate<HmiEllipse>(screen, Prefix + "TrackingCountBack");
            ConfigureWheel(countBack, layout.Filler.X, layout.Filler.Y, 36, Color.White, Grey, 2);
            HmiText count = GetOrCreate<HmiText>(screen, Prefix + "TrackingCount");
            ConfigureText(count, layout.Filler.X - 34, layout.Filler.Y - 17, 68, 30, "N/C", Navy, 18,
                HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureScript(count, "Text",
                "let link=Boolean(HMIRuntime.Tags(\"Network_HMI_OK\").Read());" +
                "let valid=Boolean(HMIRuntime.Tags(\"Bottle_Tracking_Valid\").Read());" +
                "if(!link){return \"---\";} if(!valid){return \"N/C\";}" +
                "return String(Number(HMIRuntime.Tags(\"Bottle_Tracking_Count\").Read()));");
            ConfigureText(GetOrCreate<HmiText>(screen, Prefix + "TrackingCountLabel"),
                layout.Filler.X - 34, layout.Filler.Y + 12, 68, 22, "TRACKED", Navy, 8,
                HmiFontWeight.Bold, HmiHorizontalAlignment.Center);

            HmiEllipse infeedBottle = GetOrCreate<HmiEllipse>(screen, Prefix + "InfeedBottleSensor");
            ConfigureWheel(infeedBottle,
                layout.Infeed.X + layout.Infeed.Radius + 52,
                layout.Infeed.Y + 7, 8, Color.White, DarkGrey, 2);
            ConfigureSensorColor(infeedBottle, "DI_Bottle_Infeed", Yellow);
            ConfigureText(GetOrCreate<HmiText>(screen, Prefix + "InfeedSensorLabel"),
                layout.Infeed.X + layout.Infeed.Radius + 21,
                layout.Infeed.Y + 14, 62, 17, "SPB100", DarkGrey, 8,
                HmiFontWeight.Bold, HmiHorizontalAlignment.Center);

            HmiEllipse outfeed1 = GetOrCreate<HmiEllipse>(screen, Prefix + "OutfeedAccumulation1");
            ConfigureWheel(outfeed1, layout.Outfeed.X - 52,
                layout.Outfeed.Y + 20, 7, Color.White, DarkGrey, 2);
            ConfigureSensorColor(outfeed1, "DI_Accumulation_1", Amber);
            HmiEllipse outfeed2 = GetOrCreate<HmiEllipse>(screen, Prefix + "OutfeedAccumulation2");
            ConfigureWheel(outfeed2, layout.Outfeed.X - 103,
                layout.Outfeed.Y + 19, 7, Color.White, DarkGrey, 2);
            ConfigureSensorColor(outfeed2, "DI_Accumulation_2", Red);

            ConfigureLine(GetOrCreate<HmiLine>(screen, Prefix + "CapFeed"),
                layout.Capper.X, layout.Capper.Y - layout.Capper.Radius - 29,
                layout.Capper.X, layout.Capper.Y - layout.Capper.Radius - 7, DarkGrey, 2);
            HmiEllipse cap = GetOrCreate<HmiEllipse>(screen, Prefix + "CapSensor");
            ConfigureWheel(cap, layout.Capper.X,
                layout.Capper.Y - layout.Capper.Radius - 14, 7, Color.White, DarkGrey, 2);
            ConfigureSensorColor(cap, "DI_Cap_Present", Yellow);
            ConfigureText(GetOrCreate<HmiText>(screen, Prefix + "CapLabel"),
                layout.Capper.X - 33,
                layout.Capper.Y - layout.Capper.Radius - 32, 66, 17, "CAP SENSOR", DarkGrey, 8,
                HmiFontWeight.Bold, HmiHorizontalAlignment.Center);

            HmiRectangle statusBack = GetOrCreate<HmiRectangle>(screen, Prefix + "TrackingStatusBack");
            ConfigureRectangle(statusBack, 282, 438, 440, 24, Amber, Amber, 1);
            SetRoundedCorners(statusBack, 5);
            ConfigureScript(statusBack, "BackColor",
                "let link=Boolean(HMIRuntime.Tags(\"Network_HMI_OK\").Read());" +
                "let valid=Boolean(HMIRuntime.Tags(\"Bottle_Tracking_Valid\").Read());" +
                "if(!link){return HMIRuntime.Math.RGB(145,150,155);}" +
                "return valid?HMIRuntime.Math.RGB(28,145,82):HMIRuntime.Math.RGB(235,160,30);");
            HmiText statusText = GetOrCreate<HmiText>(screen, Prefix + "TrackingStatusText");
            ConfigureText(statusText, 282, 438, 440, 24, "POSITION TRACKING - NOT COMMISSIONED",
                Color.White, 9, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureScript(statusText, "Text",
                "let link=Boolean(HMIRuntime.Tags(\"Network_HMI_OK\").Read());" +
                "let valid=Boolean(HMIRuntime.Tags(\"Bottle_Tracking_Valid\").Read());" +
                "if(!link){return \"POSITION TRACKING - NO DATA\";}" +
                "return valid?\"POSITION TRACKING - VALID\":\"POSITION TRACKING - NOT COMMISSIONED\";");

            HmiText truthNote = GetOrCreate<HmiText>(screen, Prefix + "TruthNote");
            ConfigureText(truthNote, 0, 937, 1, 1, "Exact pocket position requires commissioned encoder offsets.",
                DarkGrey, 7, HmiFontWeight.Normal, HmiHorizontalAlignment.Center);
            truthNote.Visible = false;
            truthNote.Enabled = false;
        }

        private static void AddPockets(
            HmiScreen screen, string group, int centerX, int centerY, int radius, int count, int pocketRadius)
        {
            for (int index = 0; index < count; index++)
            {
                double angle = (index * 2.0 * Math.PI / count) - (Math.PI / 2.0);
                int x = centerX + (int)Math.Round(Math.Cos(angle) * radius);
                int y = centerY + (int)Math.Round(Math.Sin(angle) * radius);
                HmiEllipse pocket = GetOrCreate<HmiEllipse>(screen,
                    Prefix + group + "Pocket" + (index + 1).ToString("00"));
                ConfigureWheel(pocket, x, y, pocketRadius, Color.White, Grey, 1);
            }
        }

        private static int ClearOverview(HmiScreen screen)
        {
            List<HmiScreenItemBase> targets = screen.ScreenItems
                .Where(item => IsOverviewObject(item.Name))
                .ToList();
            foreach (HmiScreenItemBase item in targets)
            {
                item.Delete();
            }
            return targets.Count;
        }

        private static bool IsOverviewObject(string name)
        {
            return name.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase) ||
                name.Equals("REV13_Home_MachineCard", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("REV13_Home_MachineTitle", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("REV13_Home_Conveyor", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("REV13_Home_Module_", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("REV13_Home_ModuleTop_", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("REV13_Home_ModuleLabel_", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("REV13_Home_ModuleLamp_", StringComparison.OrdinalIgnoreCase);
        }

        private static Dictionary<string, string> CapturePreservedGeometry(HmiScreen screen)
        {
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (HmiScreenItemBase item in screen.ScreenItems)
            {
                if (IsOverviewObject(item.Name))
                    continue;
                result[item.Name] = GeometryValue(item);
            }
            return result;
        }

        private static void VerifyPreservedGeometry(HmiScreen screen, Dictionary<string, string> before)
        {
            foreach (KeyValuePair<string, string> pair in before)
            {
                HmiScreenItemBase item = screen.ScreenItems.Find(pair.Key);
                if (item == null)
                    throw new InvalidOperationException("A preserved Home object disappeared: " + pair.Key);
                string after = GeometryValue(item);
                if (!String.Equals(pair.Value, after, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "A preserved Home object changed geometry/state: " + pair.Key +
                        " before=" + pair.Value + " after=" + after);
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

        private static void VerifyMimic(HmiScreen screen, MimicLayout layout)
        {
            string[] required =
            {
                Prefix + "Card", Prefix + "Title", Prefix + "FillerWheel",
                Prefix + "CapperWheel", Prefix + "InfeedStar",
                Prefix + "TransferStar", Prefix + "OutfeedStar", Prefix + "InfeedBottleSensor",
                Prefix + "OutfeedAccumulation1", Prefix + "OutfeedAccumulation2",
                Prefix + "CapSensor", Prefix + "TrackingCount", Prefix + "TrackingStatusText"
            };
            foreach (string name in required)
            {
                HmiScreenItemBase item = screen.ScreenItems.Find(name);
                if (item == null || !item.Visible)
                    throw new InvalidOperationException("Required dynamic mimic object is missing: " + name);
            }
            if (screen.ScreenItems.Find(Prefix + "TrackingCount").Dynamizations.Find("Text") == null ||
                screen.ScreenItems.Find(Prefix + "TrackingStatusText").Dynamizations.Find("Text") == null ||
                screen.ScreenItems.Find(Prefix + "InfeedBottleSensor").Dynamizations.Find("BackColor") == null)
                throw new InvalidOperationException("Required dynamic bindings are missing from the Home mimic.");

            VerifyWheelPosition(screen, Prefix + "FillerWheel", layout.Filler);
            VerifyWheelPosition(screen, Prefix + "TransferStar", layout.Transfer);
            VerifyWheelPosition(screen, Prefix + "CapperWheel", layout.Capper);
            VerifyWheelPosition(screen, Prefix + "InfeedStar", layout.Infeed);
            VerifyWheelPosition(screen, Prefix + "OutfeedStar", layout.Outfeed);
            VerifyPocketCount(screen, "Filler", 36);
            VerifyPocketCount(screen, "Transfer", 10);
            VerifyPocketCount(screen, "Capper", 10);
            VerifyPocketCount(screen, "Infeed", 10);
            VerifyPocketCount(screen, "Outfeed", 10);
            if (screen.ScreenItems.Find(Prefix + "InfeedStar_1") != null)
                throw new InvalidOperationException("The temporary duplicated transfer star still exists.");
        }

        private static void VerifyWheelPosition(
            HmiScreen screen, string name, WheelGeometry expected)
        {
            HmiEllipse wheel = screen.ScreenItems.Find(name) as HmiEllipse;
            if (wheel == null || wheel.CenterX != expected.X || wheel.CenterY != expected.Y)
                throw new InvalidOperationException("An operator-locked wheel center changed: " + name + ".");
        }

        private static void VerifyPocketCount(HmiScreen screen, string group, int expected)
        {
            string pocketPrefix = Prefix + group + "Pocket";
            int actual = screen.ScreenItems.Count(item =>
                item.Name.StartsWith(pocketPrefix, StringComparison.OrdinalIgnoreCase));
            if (actual != expected)
                throw new InvalidOperationException(
                    group + " pocket count is " + actual + "; expected " + expected + ".");
        }

        private static void EnsureTag(HmiSoftware hmi, string name, string plcTag, string comment)
        {
            HmiTagTable table = hmi.TagTables.Find("REV12") ?? hmi.TagTables.Create("REV12");
            HmiTag tag = table.Tags.Find(name) ?? table.Tags.Create(name);
            tag.Connection = "HMI_Connection_1";
            tag.PlcTag = plcTag;
            tag.AcquisitionMode = HmiAcquisitionMode.CyclicOnUse;
            tag.AcquisitionCycle = "T500ms";
            SetText(tag.Comment, comment);
        }

        private static void ConfigureDriveWheelDynamization(
            HmiScreenItemBase item, string validTag, string runningTag, string faultTag)
        {
            ConfigureScript(item, "BorderColor",
                "let valid=Boolean(HMIRuntime.Tags(\"" + validTag + "\").Read());" +
                "let fault=Boolean(HMIRuntime.Tags(\"" + faultTag + "\").Read());" +
                "let run=Boolean(HMIRuntime.Tags(\"" + runningTag + "\").Read());" +
                "if(!valid){return HMIRuntime.Math.RGB(145,150,155);}" +
                "if(fault){return HMIRuntime.Math.RGB(195,45,55);}" +
                "return run?HMIRuntime.Math.RGB(28,145,82):HMIRuntime.Math.RGB(25,111,180);");
        }

        private static void ConfigureDriveLineDynamization(
            HmiScreenItemBase item, string validTag, string runningTag, string faultTag)
        {
            ConfigureScript(item, "LineColor",
                "let valid=Boolean(HMIRuntime.Tags(\"" + validTag + "\").Read());" +
                "let fault=Boolean(HMIRuntime.Tags(\"" + faultTag + "\").Read());" +
                "let run=Boolean(HMIRuntime.Tags(\"" + runningTag + "\").Read());" +
                "if(!valid){return HMIRuntime.Math.RGB(145,150,155);}" +
                "if(fault){return HMIRuntime.Math.RGB(195,45,55);}" +
                "return run?HMIRuntime.Math.RGB(28,145,82):HMIRuntime.Math.RGB(125,145,160);");
        }

        private static void ConfigureSensorColor(HmiScreenItemBase item, string tag, Color active)
        {
            ConfigureScript(item, "BackColor",
                "let link=Boolean(HMIRuntime.Tags(\"Network_HMI_OK\").Read());" +
                "let state=Boolean(HMIRuntime.Tags(\"" + tag + "\").Read());" +
                "if(!link){return HMIRuntime.Math.RGB(205,212,219);}" +
                "return state?HMIRuntime.Math.RGB(" + active.R + "," + active.G + "," + active.B +
                "):HMIRuntime.Math.RGB(255,255,255);");
        }

        private static void ConfigureScript(HmiScreenItemBase item, string property, string script)
        {
            DynamizationBase existing = item.Dynamizations.Find(property);
            if (existing != null) existing.Delete();
            ScriptDynamization dynamic = item.Dynamizations.Create<ScriptDynamization>(property);
            dynamic.Async = false;
            dynamic.Trigger.Type = TriggerType.T500ms;
            dynamic.ScriptCode = script;
        }

        private static void ConfigureWheel(
            HmiEllipse item, int centerX, int centerY, int radius,
            Color back, Color border, byte borderWidth)
        {
            item.CenterX = centerX;
            item.CenterY = centerY;
            item.RadiusX = (uint)radius;
            item.RadiusY = (uint)radius;
            item.BackColor = back;
            item.BorderColor = border;
            item.BorderWidth = borderWidth;
            item.BackFillPattern = HmiFillPattern.Solid;
            item.Visible = true;
            item.Enabled = false;
        }

        private static void ConfigureConnectorBetweenCircles(
            HmiLine line, WheelGeometry first, WheelGeometry second, Color color, byte width)
        {
            double dx = second.X - first.X;
            double dy = second.Y - first.Y;
            double distance = Math.Sqrt((dx * dx) + (dy * dy));
            if (distance < 1.0)
                throw new InvalidOperationException("Two operator-locked wheel centers overlap exactly.");

            double ux = dx / distance;
            double uy = dy / distance;
            int x1 = first.X + (int)Math.Round(ux * first.Radius);
            int y1 = first.Y + (int)Math.Round(uy * first.Radius);
            int x2 = second.X - (int)Math.Round(ux * second.Radius);
            int y2 = second.Y - (int)Math.Round(uy * second.Radius);
            ConfigureLine(line, x1, y1, x2, y2, color, width);
        }

        private static void ConfigureLine(
            HmiLine line, int x1, int y1, int x2, int y2, Color color, byte width)
        {
            int left = Math.Min(x1, x2);
            int top = Math.Min(y1, y2);
            line.Left = left;
            line.Top = top;
            line.Width = (uint)Math.Max(1, Math.Abs(x2 - x1));
            line.Height = (uint)Math.Max(1, Math.Abs(y2 - y1));
            line.X1 = x1 - left;
            line.Y1 = y1 - top;
            line.X2 = x2 - left;
            line.Y2 = y2 - top;
            line.LineColor = color;
            line.LineWidth = width;
            line.Visible = true;
            line.Enabled = false;
        }

        private static void ConfigureRectangle(
            HmiRectangle item, int left, int top, uint width, uint height,
            Color back, Color border, byte borderWidth)
        {
            SetBounds(item, left, top, width, height);
            item.BackColor = back;
            item.BorderColor = border;
            item.BorderWidth = borderWidth;
            item.BackFillPattern = HmiFillPattern.Solid;
            item.Visible = true;
            item.Enabled = false;
        }

        private static void SetRoundedCorners(HmiRectangle item, uint radius)
        {
            item.Corners.TopLeftRadius = radius;
            item.Corners.TopRightRadius = radius;
            item.Corners.BottomLeftRadius = radius;
            item.Corners.BottomRightRadius = radius;
        }

        private static void ConfigureText(
            HmiText item, int left, int top, uint width, uint height, string text,
            Color fore, float size, HmiFontWeight weight, HmiHorizontalAlignment alignment)
        {
            SetBounds(item, left, top, width, height);
            SetText(item.Text, text);
            item.ForeColor = fore;
            item.Font.Name = HmiFontName.SiemensSans;
            item.Font.Size = size;
            item.Font.Weight = weight;
            item.HorizontalTextAlignment = alignment;
            item.VerticalTextAlignment = HmiVerticalAlignment.Center;
            item.Visible = true;
            item.Enabled = false;
        }

        private static void SetBounds(HmiScreenItemBase item, int left, int top, uint width, uint height)
        {
            item.GetType().GetProperty("Left").SetValue(item, left, null);
            item.GetType().GetProperty("Top").SetValue(item, top, null);
            item.GetType().GetProperty("Width").SetValue(item, width, null);
            item.GetType().GetProperty("Height").SetValue(item, height, null);
        }

        private static void SetText(MultilingualText text, string value)
        {
            string rich = "<body><p>" + value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;") + "</p></body>";
            foreach (MultilingualTextItem item in text.Items) item.Text = rich;
        }

        private static T GetOrCreate<T>(HmiScreen screen, string name) where T : HmiScreenItemBase
        {
            HmiScreenItemBase existing = screen.ScreenItems.Find(name);
            if (existing != null)
            {
                T typed = existing as T;
                if (typed == null)
                    throw new InvalidOperationException("Object " + name + " exists with an unexpected type.");
                return typed;
            }
            return screen.ScreenItems.Create<T>(name);
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
            if (hmi != null) return hmi;
            foreach (DeviceItem child in item.DeviceItems)
            {
                hmi = FindHmi(child);
                if (hmi != null) return hmi;
            }
            return null;
        }

        private static void WriteCompilerMessages(
            StreamWriter writer, CompilerResultMessageComposition messages, string indent)
        {
            foreach (CompilerResultMessage message in messages)
            {
                writer.WriteLine(indent + message.State + " | " + message.Description + " | " + message.Path);
                WriteCompilerMessages(writer, message.Messages, indent + "  ");
            }
        }
    }
}
