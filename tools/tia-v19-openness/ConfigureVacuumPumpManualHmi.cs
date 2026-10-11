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
using Siemens.Engineering.Hmi.Globalization;
using Siemens.Engineering.HmiUnified.HmiTags;
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
    internal static class ConfigureVacuumPumpManualHmi
    {
        private static readonly Color Navy = Color.FromArgb(18, 51, 84);
        private static readonly Color Blue = Color.FromArgb(25, 111, 180);
        private static readonly Color Green = Color.FromArgb(28, 145, 82);
        private static readonly Color Red = Color.FromArgb(195, 45, 55);
        private static readonly Color Grey = Color.FromArgb(145, 155, 165);
        private static readonly Color Dark = Color.FromArgb(35, 50, 65);
        private static readonly Color Border = Color.FromArgb(174, 188, 202);

        private static int Main(string[] args)
        {
            if (args.Length != 4 ||
                !(args[0].Equals("audit", StringComparison.OrdinalIgnoreCase) ||
                  args[0].Equals("apply", StringComparison.OrdinalIgnoreCase) ||
                  args[0].Equals("fix-cap-selection", StringComparison.OrdinalIgnoreCase) ||
                  args[0].Equals("validate-save", StringComparison.OrdinalIgnoreCase)))
            {
                Console.Error.WriteLine(
                    "Usage: ConfigureVacuumPumpManualHmi audit|apply|fix-cap-selection|validate-save <exact-ap19> <tag-csv> <report>");
                return 2;
            }

            bool apply = args[0].Equals("apply", StringComparison.OrdinalIgnoreCase);
            bool fixCapSelection = args[0].Equals("fix-cap-selection", StringComparison.OrdinalIgnoreCase);
            bool validateSave = args[0].Equals("validate-save", StringComparison.OrdinalIgnoreCase);
            string projectPath = Path.GetFullPath(args[1]);
            string tagCsv = Path.GetFullPath(args[2]);
            string reportPath = Path.GetFullPath(args[3]);
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));

            try
            {
                TiaPortalProcess process = TiaPortal.GetProcesses().SingleOrDefault(candidate =>
                    candidate.ProjectPath != null &&
                    Path.GetFullPath(candidate.ProjectPath.FullName)
                        .Equals(projectPath, StringComparison.OrdinalIgnoreCase));
                if (process == null)
                    throw new InvalidOperationException("The exact TIA project is not open.");

                using (StreamWriter report = new StreamWriter(reportPath, false))
                using (TiaPortal portal = process.Attach())
                {
                    Project project = portal.Projects.Single();
                    if (!Path.GetFullPath(project.Path.FullName)
                        .Equals(projectPath, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Attached project identity changed.");

                    Device hmiDevice;
                    DeviceItem hmiItem;
                    HmiSoftware hmi = FindUnifiedHmi(project, out hmiDevice, out hmiItem);
                    if (hmi == null || hmiItem == null)
                        throw new InvalidOperationException("Unified HMI was not found.");
                    HmiScreen manual = hmi.Screens.Find("manual");
                    if (manual == null)
                        throw new InvalidOperationException("Manual screen was not found.");

                    report.WriteLine("SCHLENKER M103 MANUAL HMI CONFIGURATION");
                    report.WriteLine("Project=" + project.Path.FullName);
                    report.WriteLine("Mode=" + (apply ? "OFFLINE_APPLY" :
                        (fixCapSelection ? "FIX_CAP_COMMAND_SELECTION_COLOR" :
                        (validateSave ? "VALIDATE_USER_LAYOUT_AND_SAVE" : "READ_ONLY_AUDIT"))));
                    report.WriteLine("OnlineApisUsed=NO");
                    report.WriteLine("DownloadOrTransferApisUsed=NO");
                    report.WriteLine("Screen=manual");
                    WriteRegionAudit(manual, report, "BEFORE");

                    if (!apply && !fixCapSelection)
                    {
                        report.WriteLine("SaveInvoked=NO");
                        report.WriteLine("CompileInvoked=NO");
                        report.WriteLine("STATUS=PASS_READ_ONLY");
                        Console.WriteLine("REPORT=" + reportPath);
                        Console.WriteLine("STATUS=PASS_READ_ONLY");
                        return 0;
                    }

                    if (apply)
                    {
                        HmiButton copiedVacuumOff = Require<HmiButton>(manual,
                            "REV12_Manual_Pump_Off_1");
                        HmiButton copiedVacuumOn = Require<HmiButton>(manual,
                            "REV12_Manual_Pump_Off_2");
                        HmiButton productOff = Require<HmiButton>(manual,
                            "REV12_Manual_Pump_Off");
                        HmiButton productOn = Require<HmiButton>(manual,
                            "REV12_Manual_Pump_On");
                        string vacuumOffBoundsBefore = BoundsKey(copiedVacuumOff);
                        string vacuumOnBoundsBefore = BoundsKey(copiedVacuumOn);
                        string productOffBoundsBefore = BoundsKey(productOff);
                        string productOnBoundsBefore = BoundsKey(productOn);

                        EnsureTag(hmi, tagCsv, "Cmd_VacuumPumpManualEnable");
                        EnsureTag(hmi, tagCsv, "Cmd_ProductPumpManualEnable");
                        EnsureTag(hmi, tagCsv, "Vacuum_Pump_Run");
                        EnsureTag(hmi, tagCsv, "Product_Pump_Run");
                        EnsureTag(hmi, tagCsv, "Cmd_ConveyorManualEnable");
                        EnsureTag(hmi, tagCsv, "Conveyor_Manual_Frequency_Hz");
                        EnsureTag(hmi, tagCsv, "Cmd_CapDriveManualEnable");
                        EnsureTag(hmi, tagCsv, "CapDrive_Manual_Frequency_Hz");
                        EnsureTag(hmi, tagCsv, "VSD_Cap_Running");
                        ApplyBindingsPreservingLayout(manual);
                        ApplyConveyorManualControls(manual);
                        if (vacuumOffBoundsBefore != BoundsKey(copiedVacuumOff) ||
                            vacuumOnBoundsBefore != BoundsKey(copiedVacuumOn) ||
                            productOffBoundsBefore != BoundsKey(productOff) ||
                            productOnBoundsBefore != BoundsKey(productOn))
                            throw new InvalidOperationException(
                                "The operator-selected vacuum-pump button positions changed.");
                    }
                    else if (fixCapSelection)
                    {
                        HmiButton capOff = Require<HmiButton>(manual,
                            "REV52_Manual_Conveyor_Off_1");
                        HmiButton capOn = Require<HmiButton>(manual,
                            "REV52_Manual_Conveyor_On_1");
                        string capOffBoundsBefore = BoundsKey(capOff);
                        string capOnBoundsBefore = BoundsKey(capOn);
                        ConfigureCommandSelectionColors(capOn, capOff,
                            "Cmd_CapDriveManualEnable");
                        if (capOffBoundsBefore != BoundsKey(capOff) ||
                            capOnBoundsBefore != BoundsKey(capOn))
                            throw new InvalidOperationException(
                                "The operator-selected cap-drive button positions changed.");
                    }
                    AuditResult(manual, hmi, report);
                    WriteRegionAudit(manual, report, "AFTER");

                    ICompilable compilable = hmiItem.GetService<ICompilable>() ??
                        hmiDevice.GetService<ICompilable>();
                    if (compilable == null)
                        throw new InvalidOperationException("Unified HMI compiler service was not found.");
                    CompilerResult result = compilable.Compile();
                    int nestedErrors = 0;
                    int nestedWarnings = 0;
                    WriteMessages(result.Messages, report, "", ref nestedErrors, ref nestedWarnings);
                    report.WriteLine("RootErrors=" + result.ErrorCount);
                    report.WriteLine("RootWarnings=" + result.WarningCount);
                    report.WriteLine("NestedErrors=" + nestedErrors);
                    report.WriteLine("NestedWarnings=" + nestedWarnings);
                    if (result.ErrorCount == 0 && nestedErrors == 0)
                    {
                        project.Save();
                        report.WriteLine("SaveInvoked=YES");
                        report.WriteLine("STATUS=PASS");
                    }
                    else
                    {
                        report.WriteLine("SaveInvoked=NO");
                        report.WriteLine("STATUS=FAIL_NOT_SAVED");
                    }
                    report.Flush();
                    Console.WriteLine("ERRORS=" + result.ErrorCount);
                    Console.WriteLine("WARNINGS=" + result.WarningCount);
                    Console.WriteLine("REPORT=" + reportPath);
                    Console.WriteLine("STATUS=" +
                        (result.ErrorCount == 0 && nestedErrors == 0 ? "PASS" : "FAIL_NOT_SAVED"));
                    return result.ErrorCount == 0 && nestedErrors == 0 ? 0 : 1;
                }
            }
            catch (Exception exception)
            {
                File.AppendAllText(reportPath, Environment.NewLine + "STATUS=FAIL" +
                    Environment.NewLine + exception + Environment.NewLine);
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static void ApplyBindingsPreservingLayout(HmiScreen manual)
        {
            HmiButton vacuumOff = Require<HmiButton>(manual, "REV12_Manual_Pump_Off_1");
            HmiButton vacuumOn = Require<HmiButton>(manual, "REV12_Manual_Pump_Off_2");
            ConfigureButtonBindingOnly(vacuumOff, "VACUUM PUMP OFF", Red,
                "Cmd_VacuumPumpManualEnable", false);
            ConfigureButtonBindingOnly(vacuumOn, "VACUUM PUMP ON", Green,
                "Cmd_VacuumPumpManualEnable", true);
            HmiButton productOff = Require<HmiButton>(manual, "REV12_Manual_Pump_Off");
            HmiButton productOn = Require<HmiButton>(manual, "REV12_Manual_Pump_On");
            productOff.BackColor = Red;
            productOn.BackColor = Green;
            ConfigureCommandSelectionColors(vacuumOn, vacuumOff, "Vacuum_Pump_Run");
            // The maintained selector must show the operator's selected command.
            // Product_Pump_Run remains the separate, interlocked run indication.
            ConfigureCommandSelectionColors(productOn, productOff,
                "Cmd_ProductPumpManualEnable");
        }

        private static void ApplyConveyorManualControls(HmiScreen manual)
        {
            HmiText movementNote = manual.ScreenItems.Find(
                "REV41_MANUAL_MovementNote") as HmiText;
            if (movementNote != null)
                ConfigureText(movementNote, 52, 628, 656, 16,
                    "MANUAL DRIVE TEST — VERIFY PERSONNEL CLEARANCE BEFORE START",
                    Red, 9);

            ConfigureText(GetOrCreate<HmiText>(manual, "REV52_MANUAL_ConveyorGroup"),
                59, 644, 245, 18, "BOTTLE CONVEYOR", Blue, 9);
            HmiButton off = GetOrCreate<HmiButton>(manual, "REV52_Manual_Conveyor_Off");
            HmiButton on = GetOrCreate<HmiButton>(manual, "REV52_Manual_Conveyor_On");
            ConfigureButton(off, 54, 663, 122, 48, "CONVEYOR OFF", Red,
                "Cmd_ConveyorManualEnable", false);
            ConfigureButton(on, 182, 662, 122, 48, "CONVEYOR ON", Green,
                "Cmd_ConveyorManualEnable", true);
            ConfigureCommandSelectionColors(on, off, "Cmd_ConveyorManualEnable");

            ConfigureText(GetOrCreate<HmiText>(manual, "REV52_Manual_Conveyor_Hz_Label"),
                331, 633, 72, 22, "SETPOINT", Dark, 9);
            ConfigureIOField(GetOrCreate<HmiIOField>(manual,
                "REV52_Manual_Conveyor_Hz_Value"), 313, 669, 76, 34,
                "Conveyor_Manual_Frequency_Hz", false, "{F1}");
            ConfigureText(GetOrCreate<HmiText>(manual, "REV52_Manual_Conveyor_Hz_Unit"),
                395, 684, 28, 22, "Hz", Dark, 10);
            ConfigureText(GetOrCreate<HmiText>(manual, "REV52_Manual_Conveyor_Hz_Range"),
                316, 647, 95, 16, "5–15 Hz", Grey, 8);

            ConfigureText(GetOrCreate<HmiText>(manual, "REV53_MANUAL_CapDriveGroup"),
                409, 644, 250, 18, "CAP DISTRIBUTOR", Blue, 9);
            HmiButton capOff = Require<HmiButton>(manual, "REV52_Manual_Conveyor_Off_1");
            HmiButton capOn = Require<HmiButton>(manual, "REV52_Manual_Conveyor_On_1");
            ConfigureButton(capOff, 409, 662, 122, 48, "CAP DRIVE OFF", Red,
                "Cmd_CapDriveManualEnable", false);
            ConfigureButton(capOn, 537, 662, 122, 48, "CAP DRIVE ON", Green,
                "Cmd_CapDriveManualEnable", true);
            ConfigureCommandSelectionColors(capOn, capOff, "VSD_Cap_Running");
            ConfigureText(Require<HmiText>(manual, "REV52_Manual_Conveyor_Hz_Label_1"),
                683, 635, 72, 22, "SETPOINT", Dark, 9);
            ConfigureText(Require<HmiText>(manual, "REV52_Manual_Conveyor_Hz_Range_1"),
                674, 649, 95, 16, "0–BASE Hz", Grey, 8);
            ConfigureIOField(Require<HmiIOField>(manual,
                "REV52_Manual_Conveyor_Hz_Value_1"), 662, 669, 76, 34,
                "CapDrive_Manual_Frequency_Hz", false, "{F1}");
        }

        private static void AuditResult(HmiScreen manual, HmiSoftware hmi, StreamWriter report)
        {
            if (manual.ScreenItems.Select(item => item.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase).Count() != manual.ScreenItems.Count)
                throw new InvalidOperationException("Duplicate object names exist on manual screen.");

            HmiTagTable table = hmi.TagTables.Find("REV12");
            HmiTag tag = table == null ? null : table.Tags.Find("Cmd_VacuumPumpManualEnable");
            if (tag == null || tag.PlcTag != "DB_Global.Cmd.VacuumPumpManualEnable")
                throw new InvalidOperationException("Vacuum-pump manual HMI tag binding is invalid.");
            HmiTag productCommandTag = table == null ? null :
                table.Tags.Find("Cmd_ProductPumpManualEnable");
            if (productCommandTag == null ||
                productCommandTag.PlcTag != "DB_Global.Cmd.ProductPumpManualEnable")
                throw new InvalidOperationException(
                    "Product-pump manual HMI tag binding is invalid.");
            HmiTag conveyorCommandTag = table == null ? null :
                table.Tags.Find("Cmd_ConveyorManualEnable");
            if (conveyorCommandTag == null ||
                conveyorCommandTag.PlcTag != "DB_Global.Cmd.ConveyorManualEnable")
                throw new InvalidOperationException(
                    "Conveyor manual HMI command tag binding is invalid.");
            HmiTag conveyorFrequencyTag = table == null ? null :
                table.Tags.Find("Conveyor_Manual_Frequency_Hz");
            if (conveyorFrequencyTag == null ||
                conveyorFrequencyTag.PlcTag != "DB_Global.Par.ConveyorManualFrequencyHz")
                throw new InvalidOperationException(
                    "Conveyor manual frequency tag binding is invalid.");
            HmiTag capCommandTag = table == null ? null :
                table.Tags.Find("Cmd_CapDriveManualEnable");
            if (capCommandTag == null ||
                capCommandTag.PlcTag != "DB_Global.Cmd.CapDriveManualEnable")
                throw new InvalidOperationException(
                    "Cap-drive manual command tag binding is invalid.");
            HmiTag capFrequencyTag = table == null ? null :
                table.Tags.Find("CapDrive_Manual_Frequency_Hz");
            if (capFrequencyTag == null ||
                capFrequencyTag.PlcTag != "DB_Global.Par.CapDriveManualFrequencyHz")
                throw new InvalidOperationException(
                    "Cap-drive manual frequency tag binding is invalid.");

            HmiButton off = Require<HmiButton>(manual, "REV12_Manual_Pump_Off_1");
            HmiButton on = Require<HmiButton>(manual, "REV12_Manual_Pump_Off_2");
            HmiButton productOff = Require<HmiButton>(manual, "REV12_Manual_Pump_Off");
            HmiButton productOn = Require<HmiButton>(manual, "REV12_Manual_Pump_On");
            VerifyTap(off, "Cmd_VacuumPumpManualEnable", false);
            VerifyTap(on, "Cmd_VacuumPumpManualEnable", true);
            VerifyTapLiteral(productOff, "Cmd_ProductPumpManualEnable", "0");
            VerifyTapLiteral(productOn, "Cmd_ProductPumpManualEnable", "1");
            if (productOff.BackColor.ToArgb() != Red.ToArgb() ||
                productOn.BackColor.ToArgb() != Green.ToArgb())
                throw new InvalidOperationException("Product-pump command colors are invalid.");
            VerifyCommandSelectionColors(on, off, "Vacuum_Pump_Run");
            VerifyCommandSelectionColors(productOn, productOff,
                "Cmd_ProductPumpManualEnable");
            HmiButton conveyorOff = Require<HmiButton>(manual, "REV52_Manual_Conveyor_Off");
            HmiButton conveyorOn = Require<HmiButton>(manual, "REV52_Manual_Conveyor_On");
            VerifyTap(conveyorOff, "Cmd_ConveyorManualEnable", false);
            VerifyTap(conveyorOn, "Cmd_ConveyorManualEnable", true);
            VerifyCommandSelectionColors(conveyorOn, conveyorOff,
                "Cmd_ConveyorManualEnable");
            HmiIOField conveyorHz = Require<HmiIOField>(manual,
                "REV52_Manual_Conveyor_Hz_Value");
            TagDynamization conveyorHzBinding =
                conveyorHz.Dynamizations.Find("ProcessValue") as TagDynamization;
            if (conveyorHzBinding == null || conveyorHzBinding.ReadOnly ||
                conveyorHzBinding.Tag != "Conveyor_Manual_Frequency_Hz")
                throw new InvalidOperationException(
                    "Conveyor manual frequency field binding is invalid.");
            HmiButton capOff = Require<HmiButton>(manual, "REV52_Manual_Conveyor_Off_1");
            HmiButton capOn = Require<HmiButton>(manual, "REV52_Manual_Conveyor_On_1");
            VerifyTap(capOff, "Cmd_CapDriveManualEnable", false);
            VerifyTap(capOn, "Cmd_CapDriveManualEnable", true);
            VerifyCommandSelectionColors(capOn, capOff,
                "Cmd_CapDriveManualEnable");
            HmiIOField capHz = Require<HmiIOField>(manual,
                "REV52_Manual_Conveyor_Hz_Value_1");
            TagDynamization capHzBinding =
                capHz.Dynamizations.Find("ProcessValue") as TagDynamization;
            if (capHzBinding == null || capHzBinding.ReadOnly ||
                capHzBinding.Tag != "CapDrive_Manual_Frequency_Hz")
                throw new InvalidOperationException(
                    "Cap-drive manual frequency field binding is invalid.");
            HmiScreenItemBase[] driveControls =
            {
                conveyorOff, conveyorOn, conveyorHz,
                capOff, capOn, capHz
            };
            foreach (HmiScreenItemBase control in driveControls)
            {
                if (!control.Visible || !control.Enabled ||
                    GetInt(control, "Left") < 24 || GetInt(control, "Top") < 628 ||
                    GetInt(control, "Left") + GetInt(control, "Width") > 750 ||
                    GetInt(control, "Top") + GetInt(control, "Height") > 721)
                    throw new InvalidOperationException(
                        control.Name + " is outside the manual-drive area.");
            }
            for (int first = 0; first < driveControls.Length; first++)
            for (int second = first + 1; second < driveControls.Length; second++)
                if (Intersects(driveControls[first], driveControls[second], 2))
                    throw new InvalidOperationException("Manual-drive controls overlap: " +
                        driveControls[first].Name + " / " + driveControls[second].Name);

            HmiScreenItemBase[] controls =
            {
                off,
                on,
                productOff,
                productOn
            };
            foreach (HmiScreenItemBase control in controls)
            {
                if (!control.Visible || !control.Enabled)
                    throw new InvalidOperationException(control.Name + " is not runtime-visible and enabled.");
                if (GetInt(control, "Left") < 769 || GetInt(control, "Top") < 126 ||
                    GetInt(control, "Left") + GetInt(control, "Width") > 1116 ||
                    GetInt(control, "Top") + GetInt(control, "Height") > 722)
                    throw new InvalidOperationException(control.Name + " is outside the manual status card.");
            }
            for (int first = 0; first < controls.Length; first++)
            for (int second = first + 1; second < controls.Length; second++)
            {
                if (Intersects(controls[first], controls[second], 2))
                    throw new InvalidOperationException("Manual pump controls overlap: " +
                        controls[first].Name + " / " + controls[second].Name);
            }

            report.WriteLine("TagBinding=DB_Global.Cmd.VacuumPumpManualEnable");
            report.WriteLine("VacuumOffEvent=Write(false)");
            report.WriteLine("VacuumOnEvent=Write(true)");
            report.WriteLine("VacuumOffBounds=" + BoundsKey(off));
            report.WriteLine("VacuumOnBounds=" + BoundsKey(on));
            report.WriteLine("OperatorSelectedPositionsPreserved=YES");
            report.WriteLine("ProductPumpBindingsPreserved=YES");
            report.WriteLine("ProductPumpColors=OFF_RED_ON_GREEN");
            report.WriteLine("VacuumPumpRuntimeColors=ON_GREEN_WHEN_OUTPUT_TRUE_OTHERWISE_GREY;OFF_RED_WHEN_OUTPUT_FALSE_OTHERWISE_GREY");
            report.WriteLine("ProductPumpRuntimeColors=ON_GREEN_WHEN_COMMAND_SELECTED_OTHERWISE_GREY;OFF_RED_WHEN_COMMAND_NOT_SELECTED_OTHERWISE_GREY");
            report.WriteLine("ProductPumpRunIndicationRemainsSeparate=Product_Pump_Run");
            report.WriteLine("ConveyorManualCommand=Cmd_ConveyorManualEnable");
            report.WriteLine("ConveyorManualFrequency=Conveyor_Manual_Frequency_Hz;LIMIT=5.0..15.0Hz");
            report.WriteLine("ConveyorManualControlsBounds=52,628..696,696");
            report.WriteLine("CapDriveManualCommand=Cmd_CapDriveManualEnable");
            report.WriteLine("CapDriveManualFrequency=CapDrive_Manual_Frequency_Hz;STARTUP=0.0Hz;MAX=DriveBaseFrequencyHz");
            report.WriteLine("CapDriveCommandSelectionColors=Cmd_CapDriveManualEnable");
            report.WriteLine("CapDriveRunIndicationRemainsSeparate=VSD_Cap_Running");
            report.WriteLine("ManualDriveControlsBounds=52,628..730,706");
            report.WriteLine("ManualDriveControlOverlaps=0");
            report.WriteLine("ManualCardBounds=769,126,347,596");
            report.WriteLine("PumpControlOverlaps=0");
            report.WriteLine("DuplicateObjectNames=0");
        }

        private static void WriteRegionAudit(HmiScreen manual, StreamWriter report, string phase)
        {
            report.WriteLine("REGION_" + phase + "=760,500..1120,730");
            foreach (HmiScreenItemBase item in manual.ScreenItems
                .Where(candidate => candidate.Visible && Intersects(candidate, 760, 500, 1120, 730))
                .OrderBy(candidate => GetInt(candidate, "Top"))
                .ThenBy(candidate => GetInt(candidate, "Left")))
            {
                report.WriteLine(String.Format("{0}\t{1}\t{2},{3},{4},{5}",
                    item.Name, item.GetType().Name, GetInt(item, "Left"),
                    GetInt(item, "Top"), GetInt(item, "Width"), GetInt(item, "Height")));
            }
        }

        private static void EnsureTag(HmiSoftware hmi, string csvPath, string name)
        {
            if (!File.Exists(csvPath)) throw new FileNotFoundException("HMI tag CSV missing.", csvPath);
            string line = File.ReadLines(csvPath).Skip(1).FirstOrDefault(value =>
                value.StartsWith(name + ",", StringComparison.OrdinalIgnoreCase));
            if (line == null) throw new InvalidDataException("HMI tag row missing for " + name + ".");
            string[] fields = line.Split(',');
            if (fields.Length < 2) throw new InvalidDataException("Invalid HMI tag row for " + name + ".");

            HmiTagTable table = hmi.TagTables.Find("REV12") ?? hmi.TagTables.Create("REV12");
            HmiTag tag = table.Tags.Find(name) ?? table.Tags.Create(name);
            tag.Connection = "HMI_Connection_1";
            tag.PlcTag = fields[1];
            tag.AcquisitionMode = HmiAcquisitionMode.CyclicOnUse;
            tag.AcquisitionCycle = "T1s";
        }

        private static void VerifyTap(HmiButton button, string tag, bool value)
        {
            HmiButtonEventHandler tapped = button.EventHandlers.Find(HmiButtonEventType.Tapped);
            string expected = "HMIRuntime.Tags(\"" + tag + "\").Write(" +
                (value ? "true" : "false") + ");";
            if (tapped == null || tapped.Script.ScriptCode != expected)
                throw new InvalidOperationException("Tapped event is invalid on " + button.Name + ".");
        }

        private static void VerifyTapLiteral(HmiButton button, string tag, string value)
        {
            HmiButtonEventHandler tapped = button.EventHandlers.Find(HmiButtonEventType.Tapped);
            string expected = "HMIRuntime.Tags(\"" + tag + "\").Write(" + value + ");";
            if (tapped == null || tapped.Script.ScriptCode != expected)
                throw new InvalidOperationException("Tapped event is invalid on " + button.Name + ".");
        }

        private static void ConfigureCommandSelectionColors(
            HmiButton onButton, HmiButton offButton, string stateTag)
        {
            int onEvents = onButton.EventHandlers.Count;
            int offEvents = offButton.EventHandlers.Count;
            ConfigureBackColorDynamization(onButton,
                "let v=Boolean(HMIRuntime.Tags(\"" + stateTag + "\").Read());" +
                "if(v){return HMIRuntime.Math.RGB(28,145,82);}" +
                "return HMIRuntime.Math.RGB(145,155,165);");
            ConfigureBackColorDynamization(offButton,
                "let v=Boolean(HMIRuntime.Tags(\"" + stateTag + "\").Read());" +
                "if(v){return HMIRuntime.Math.RGB(145,155,165);}" +
                "return HMIRuntime.Math.RGB(195,45,55);");
            if (onButton.EventHandlers.Count != onEvents ||
                offButton.EventHandlers.Count != offEvents)
                throw new InvalidOperationException(
                    "Pump command events changed while adding runtime colors.");
        }

        private static void ConfigureBackColorDynamization(
            HmiButton button, string scriptCode)
        {
            DynamizationBase existing = button.Dynamizations.Find("BackColor");
            if (existing != null) existing.Delete();
            ScriptDynamization color =
                button.Dynamizations.Create<ScriptDynamization>("BackColor");
            color.Async = false;
            color.Trigger.Type = TriggerType.T500ms;
            color.ScriptCode = scriptCode;
        }

        private static void VerifyCommandSelectionColors(
            HmiButton onButton, HmiButton offButton, string stateTag)
        {
            ScriptDynamization onColor =
                onButton.Dynamizations.Find("BackColor") as ScriptDynamization;
            ScriptDynamization offColor =
                offButton.Dynamizations.Find("BackColor") as ScriptDynamization;
            if (onColor == null || offColor == null ||
                !onColor.ScriptCode.Contains("Tags(\"" + stateTag + "\")") ||
                !offColor.ScriptCode.Contains("Tags(\"" + stateTag + "\")"))
                throw new InvalidOperationException(
                    "Runtime command colors are invalid for " + stateTag + ".");
        }

        private static void ConfigureButton(HmiButton item, int left, int top, uint width,
            uint height, string text, Color back, string tag, bool value)
        {
            SetBounds(item, left, top, width, height);
            SetText(item.Text, text);
            item.BackColor = back;
            item.ForeColor = Color.White;
            item.BorderColor = Color.White;
            item.BorderWidth = 1;
            item.Font.Name = HmiFontName.SiemensSans;
            item.Font.Size = 11;
            item.Font.Weight = HmiFontWeight.Bold;
            item.Content.HorizontalTextAlignment = HmiHorizontalAlignment.Center;
            item.Content.VerticalTextAlignment = HmiVerticalAlignment.Center;
            item.Visible = true;
            item.Enabled = true;
            item.RequireExplicitUnlock = false;
            HmiButtonEventHandler tapped = item.EventHandlers.Find(HmiButtonEventType.Tapped) ??
                item.EventHandlers.Create(HmiButtonEventType.Tapped);
            tapped.Script.ScriptCode = "HMIRuntime.Tags(\"" + tag + "\").Write(" +
                (value ? "true" : "false") + ");";
        }

        private static void ConfigureButtonBindingOnly(HmiButton item, string text,
            Color back, string tag, bool value)
        {
            SetText(item.Text, text);
            item.BackColor = back;
            item.ForeColor = Color.White;
            item.BorderColor = Color.White;
            item.BorderWidth = 1;
            item.Visible = true;
            item.Enabled = true;
            item.RequireExplicitUnlock = false;
            HmiButtonEventHandler tapped = item.EventHandlers.Find(HmiButtonEventType.Tapped) ??
                item.EventHandlers.Create(HmiButtonEventType.Tapped);
            tapped.Script.ScriptCode = "HMIRuntime.Tags(\"" + tag + "\").Write(" +
                (value ? "true" : "false") + ");";
        }

        private static void ConfigureText(HmiText item, int left, int top, uint width,
            uint height, string text, Color fore, float fontSize)
        {
            SetBounds(item, left, top, width, height);
            SetText(item.Text, text);
            item.ForeColor = fore;
            item.Font.Name = HmiFontName.SiemensSans;
            item.Font.Size = fontSize;
            item.Font.Weight = HmiFontWeight.Bold;
            item.HorizontalTextAlignment = HmiHorizontalAlignment.Left;
            item.VerticalTextAlignment = HmiVerticalAlignment.Center;
            item.Visible = true;
            item.Enabled = true;
        }

        private static void ConfigureIOField(HmiIOField item, int left, int top,
            uint width, uint height, string processValue, bool outputOnly, string format)
        {
            SetBounds(item, left, top, width, height);
            item.ProcessValue = "0";
            BindTag(item.Dynamizations, "ProcessValue", processValue, outputOnly);
            item.IOFieldType = outputOnly ? HmiIOFieldType.Output : HmiIOFieldType.InputOutput;
            item.OutputFormat = format;
            item.BackColor = Color.White;
            item.ForeColor = Dark;
            item.BorderColor = Border;
            item.BorderWidth = 1;
            item.Font.Name = HmiFontName.SiemensSans;
            item.Font.Size = 12;
            item.Font.Weight = HmiFontWeight.Bold;
            item.HorizontalTextAlignment = HmiHorizontalAlignment.Center;
            item.VerticalTextAlignment = HmiVerticalAlignment.Center;
            item.Visible = true;
            item.Enabled = true;
            item.RequireExplicitUnlock = false;
        }

        private static void BindTag(DynamizationBaseComposition dynamizations,
            string propertyName, string tag, bool readOnly)
        {
            DynamizationBase existing = dynamizations.Find(propertyName);
            TagDynamization binding;
            if (existing == null)
                binding = dynamizations.Create<TagDynamization>(propertyName);
            else
            {
                binding = existing as TagDynamization;
                if (binding == null)
                    throw new InvalidOperationException(
                        "Property '" + propertyName + "' already has a non-tag dynamization.");
            }
            binding.Tag = tag;
            binding.ReadOnly = readOnly;
            binding.UseIndirectAddressing = false;
        }

        private static void SetBounds(HmiScreenItemBase item, int left, int top,
            uint width, uint height)
        {
            SetProperty(item, "Left", left);
            SetProperty(item, "Top", top);
            SetProperty(item, "Width", width);
            SetProperty(item, "Height", height);
        }

        private static void SetProperty(object target, string name, object value)
        {
            System.Reflection.PropertyInfo property = target.GetType().GetProperty(name);
            if (property == null || !property.CanWrite)
                throw new InvalidOperationException("Property '" + name +
                    "' is not writable on " + target.GetType().FullName + ".");
            property.SetValue(target, value, null);
        }

        private static int GetInt(HmiScreenItemBase item, string name)
        {
            System.Reflection.PropertyInfo property = item.GetType().GetProperty(name);
            if (property != null && property.CanRead)
                return Convert.ToInt32(property.GetValue(item, null));
            return Convert.ToInt32(item.GetAttribute(name));
        }

        private static string BoundsKey(HmiScreenItemBase item)
        {
            return String.Format("{0},{1},{2},{3}", GetInt(item, "Left"),
                GetInt(item, "Top"), GetInt(item, "Width"), GetInt(item, "Height"));
        }

        private static void SetText(MultilingualText text, string value)
        {
            string rich = "<body><p>" + value.Replace("&", "&amp;")
                .Replace("<", "&lt;").Replace(">", "&gt;") + "</p></body>";
            foreach (MultilingualTextItem item in text.Items) item.Text = rich;
        }

        private static T GetOrCreate<T>(HmiScreen screen, string name)
            where T : HmiScreenItemBase
        {
            HmiScreenItemBase existing = screen.ScreenItems.Find(name);
            if (existing == null) return screen.ScreenItems.Create<T>(name);
            T typed = existing as T;
            if (typed == null)
                throw new InvalidOperationException(name + " exists as " + existing.GetType().Name + ".");
            return typed;
        }

        private static T Require<T>(HmiScreen screen, string name)
            where T : HmiScreenItemBase
        {
            T item = screen.ScreenItems.Find(name) as T;
            if (item == null) throw new InvalidOperationException("Required object missing: " + name);
            return item;
        }

        private static bool Intersects(HmiScreenItemBase first, HmiScreenItemBase second,
            int tolerance)
        {
            int firstLeft = GetInt(first, "Left");
            int firstTop = GetInt(first, "Top");
            int firstWidth = GetInt(first, "Width");
            int firstHeight = GetInt(first, "Height");
            int secondLeft = GetInt(second, "Left");
            int secondTop = GetInt(second, "Top");
            int secondWidth = GetInt(second, "Width");
            int secondHeight = GetInt(second, "Height");
            return firstLeft < secondLeft + secondWidth - tolerance &&
                firstLeft + firstWidth > secondLeft + tolerance &&
                firstTop < secondTop + secondHeight - tolerance &&
                firstTop + firstHeight > secondTop + tolerance;
        }

        private static bool Intersects(HmiScreenItemBase item, int left, int top,
            int right, int bottom)
        {
            try
            {
                int itemLeft = GetInt(item, "Left");
                int itemTop = GetInt(item, "Top");
                int itemWidth = GetInt(item, "Width");
                int itemHeight = GetInt(item, "Height");
                return itemLeft < right && itemLeft + itemWidth > left &&
                    itemTop < bottom && itemTop + itemHeight > top;
            }
            catch (EngineeringNotSupportedException)
            {
                return false;
            }
        }

        private static HmiSoftware FindUnifiedHmi(Project project, out Device hmiDevice,
            out DeviceItem hmiItem)
        {
            hmiDevice = null;
            hmiItem = null;
            foreach (Device device in project.Devices)
            {
                DeviceItem candidate;
                HmiSoftware hmi = FindUnifiedHmi(device.DeviceItems, out candidate);
                if (hmi != null)
                {
                    hmiDevice = device;
                    hmiItem = candidate;
                    return hmi;
                }
            }
            return null;
        }

        private static HmiSoftware FindUnifiedHmi(DeviceItemComposition items,
            out DeviceItem hmiItem)
        {
            foreach (DeviceItem item in items)
            {
                SoftwareContainer container = item.GetService<SoftwareContainer>();
                HmiSoftware hmi = container == null ? null : container.Software as HmiSoftware;
                if (hmi != null)
                {
                    hmiItem = item;
                    return hmi;
                }
                HmiSoftware nested = FindUnifiedHmi(item.DeviceItems, out hmiItem);
                if (nested != null) return nested;
            }
            hmiItem = null;
            return null;
        }

        private static void WriteMessages(IEnumerable<CompilerResultMessage> messages,
            StreamWriter report, string indent, ref int errors, ref int warnings)
        {
            foreach (CompilerResultMessage message in messages)
            {
                report.WriteLine(indent + message.State + "\t" + message.Description +
                    "\t" + message.Path);
                if (message.State == CompilerResultState.Error) errors++;
                if (message.State == CompilerResultState.Warning) warnings++;
                WriteMessages(message.Messages, report, indent + "  ", ref errors, ref warnings);
            }
        }
    }
}
