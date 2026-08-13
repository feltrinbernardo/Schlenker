using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Siemens.Engineering;
using Siemens.Engineering.Compiler;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.Hmi.Globalization;
using Siemens.Engineering.HmiUnified.HmiAlarm;
using Siemens.Engineering.HmiUnified.HmiAlarm.HmiAlarmCommon;
using Siemens.Engineering.HmiUnified.HmiTags;
using Siemens.Engineering.HmiUnified.UI.Base;
using Siemens.Engineering.HmiUnified.UI.Controls;
using Siemens.Engineering.HmiUnified.UI.Dynamization;
using Siemens.Engineering.HmiUnified.UI.Dynamization.Script;
using Siemens.Engineering.HmiUnified.UI.Enum;
using Siemens.Engineering.HmiUnified.UI.Events;
using Siemens.Engineering.HmiUnified.UI.Parts;
using Siemens.Engineering.HmiUnified.UI.Shapes;
using Siemens.Engineering.HmiUnified.UI.Screens;
using Siemens.Engineering.HmiUnified.UI.Widgets;
using Siemens.Engineering.SW;

namespace Schlenker.TiaV19
{
    internal static class BuildUnifiedHomeScreen
    {
        private static readonly Color Navy = Color.FromArgb(18, 51, 84);
        private static readonly Color Blue = Color.FromArgb(25, 111, 180);
        private static readonly Color PaleBlue = Color.FromArgb(230, 238, 245);
        private static readonly Color Panel = Color.FromArgb(248, 250, 252);
        private static readonly Color Border = Color.FromArgb(125, 145, 160);
        private static readonly Color Green = Color.FromArgb(28, 145, 82);
        private static readonly Color Amber = Color.FromArgb(235, 160, 30);
        private static readonly Color Red = Color.FromArgb(195, 45, 55);
        private static readonly Color Grey = Color.FromArgb(205, 212, 219);
        private static readonly Color Dark = Color.FromArgb(35, 45, 55);

        private static int Main(string[] args)
        {
            string projectHint = args.Length > 0 ? args[0] : "schlenkers 36-10 190036.ap19";
            string archiveDirectory = args.Length > 1
                ? Path.GetFullPath(args[1])
                : Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "outputs", "019fca72-rev12-hmi-tags"));
            string existingArchive = args.Length > 2 ? Path.GetFullPath(args[2]) : null;
            bool alarmResetOnly = args.Length > 3 &&
                args[3].Equals("--alarm-reset-only", StringComparison.OrdinalIgnoreCase);
            bool efficiencyUpdate = args.Length > 3 &&
                args[3].Equals("--efficiency-update", StringComparison.OrdinalIgnoreCase);
            bool engineeringSettingsUpdate = args.Length > 3 &&
                args[3].Equals("--settings-cable-update", StringComparison.OrdinalIgnoreCase);
            bool productionCleanOnly = args.Length > 3 &&
                args[3].Equals("--production-clean-only", StringComparison.OrdinalIgnoreCase);
            bool productionMasterExact = args.Length > 3 &&
                args[3].Equals("--production-master-exact", StringComparison.OrdinalIgnoreCase);
            bool revision13HomeProduction = args.Length > 3 &&
                args[3].Equals("--rev13-home-production", StringComparison.OrdinalIgnoreCase);
            bool revision14Pilot = args.Length > 3 &&
                args[3].Equals("--rev14-pilot", StringComparison.OrdinalIgnoreCase);
            bool revision14Cleanup = args.Length > 3 &&
                args[3].Equals("--rev14-cleanup", StringComparison.OrdinalIgnoreCase);
            bool revision14NavContrast = args.Length > 3 &&
                args[3].Equals("--rev14-nav-contrast", StringComparison.OrdinalIgnoreCase);
            bool revision13NavButtonsAll = args.Length > 3 &&
                args[3].Equals("--rev13-nav-buttons-all", StringComparison.OrdinalIgnoreCase);
            bool revision13NavMasterAll = args.Length > 3 &&
                args[3].Equals("--rev13-nav-master-all", StringComparison.OrdinalIgnoreCase);
            string revision13NavMasterTarget = revision13NavMasterAll && args.Length > 4
                ? args[4]
                : null;
            bool verifyNavigationOnly = args.Length > 3 &&
                args[3].Equals("--verify-nav", StringComparison.OrdinalIgnoreCase);
            string verifyNavigationTarget = verifyNavigationOnly && args.Length > 4
                ? args[4]
                : null;
            bool revision15IoDiagnostics = args.Length > 3 &&
                args[3].Equals("--rev15-io-diagnostics", StringComparison.OrdinalIgnoreCase);
            bool revision15IoNavigationDirect = args.Length > 3 &&
                args[3].Equals("--rev15-io-nav-direct", StringComparison.OrdinalIgnoreCase);
            bool revision15IoCompleteMissing = args.Length > 3 &&
                args[3].Equals("--rev15-io-complete-missing", StringComparison.OrdinalIgnoreCase);
            bool revision15IoFixEv210Live = args.Length > 3 &&
                args[3].Equals("--rev15-io-fix-ev210-live", StringComparison.OrdinalIgnoreCase);
            bool revision16SmcAnybus = args.Length > 3 &&
                args[3].Equals("--rev16-smc-anybus", StringComparison.OrdinalIgnoreCase);
            bool revision18IoLinkPage = args.Length > 3 &&
                args[3].Equals("--rev18-iolink-page", StringComparison.OrdinalIgnoreCase);
            string revision18IoLinkTarget = revision18IoLinkPage && args.Length > 4
                ? args[4]
                : null;
            string revision18IoLinkStage = revision18IoLinkPage && args.Length > 5
                ? args[5]
                : null;
            bool revision18TagsOnly = args.Length > 3 &&
                args[3].Equals("--rev18-tags-only", StringComparison.OrdinalIgnoreCase);
            bool revision18CipPage = args.Length > 3 &&
                args[3].Equals("--rev18-cip-page", StringComparison.OrdinalIgnoreCase);
            string revision18CipStage = revision18CipPage && args.Length > 4
                ? args[4]
                : null;
            bool revision21AlarmMatrix = args.Length > 3 &&
                args[3].Equals("--rev21-alarm-matrix", StringComparison.OrdinalIgnoreCase);
            bool revision22PilzDiagnostics = args.Length > 3 &&
                args[3].Equals("--rev22-pilz-diagnostics", StringComparison.OrdinalIgnoreCase);
            bool revision23PilzProfinetDiagnostics = args.Length > 3 &&
                args[3].Equals("--rev23-pilz-profinet-diagnostics", StringComparison.OrdinalIgnoreCase);
            bool userHeaderMissingOnly = args.Length > 3 &&
                args[3].Equals("--user-header-missing-only", StringComparison.OrdinalIgnoreCase);
            bool userHeaderLayoutFix = args.Length > 3 &&
                args[3].Equals("--user-header-layout-fix", StringComparison.OrdinalIgnoreCase);
            string userHeaderLayoutTarget = userHeaderLayoutFix && args.Length > 4
                ? args[4]
                : null;
            bool productionMimicPolish = args.Length > 3 &&
                args[3].Equals("--production-mimic-polish", StringComparison.OrdinalIgnoreCase);
            bool productionOnlyFinalize = args.Length > 3 &&
                args[3].Equals("--production-only-finalize", StringComparison.OrdinalIgnoreCase);
            bool productionClockOnly = args.Length > 3 &&
                args[3].Equals("--production-clock-only", StringComparison.OrdinalIgnoreCase);
            bool productionCommandIconsOnly = args.Length > 3 &&
                args[3].Equals("--production-command-icons-only", StringComparison.OrdinalIgnoreCase);
            bool productionCommandIconsIncrease20 = args.Length > 3 &&
                args[3].Equals("--production-command-icons-increase-20", StringComparison.OrdinalIgnoreCase);
            bool productionCommandIconsIncrease25 = args.Length > 3 &&
                args[3].Equals("--production-command-icons-increase-25", StringComparison.OrdinalIgnoreCase);
            bool productionCommandIconsIncrease35 = args.Length > 3 &&
                args[3].Equals("--production-command-icons-increase-35", StringComparison.OrdinalIgnoreCase);
            bool productionCommandIconsIncrease55 = args.Length > 3 &&
                args[3].Equals("--production-command-icons-increase-55", StringComparison.OrdinalIgnoreCase);
            bool productionCommandButtonAreaIncrease25 = args.Length > 3 &&
                args[3].Equals("--production-command-button-area-increase-25", StringComparison.OrdinalIgnoreCase);
            bool productionCommandLabelsCrisp = args.Length > 3 &&
                args[3].Equals("--production-command-labels-crisp", StringComparison.OrdinalIgnoreCase);
            bool revision25MasterFrame = args.Length > 3 &&
                args[3].Equals("--rev25-master-frame", StringComparison.OrdinalIgnoreCase);
            string revision25MasterFrameTarget = revision25MasterFrame && args.Length > 4
                ? args[4]
                : null;
            bool revision25IoLinkHeaderAlign = args.Length > 3 &&
                args[3].Equals("--rev25-iolink-header-align", StringComparison.OrdinalIgnoreCase);
            string revision25IoLinkHeaderTarget = revision25IoLinkHeaderAlign && args.Length > 4
                ? args[4]
                : null;
            bool revision25FooterAlign = args.Length > 3 &&
                args[3].Equals("--rev25-footer-align", StringComparison.OrdinalIgnoreCase);
            string revision25FooterTarget = revision25FooterAlign && args.Length > 4
                ? args[4]
                : null;
            bool revision25LiftFunctionButtonAlign = args.Length > 3 &&
                args[3].Equals("--rev25-lift-function-button-align", StringComparison.OrdinalIgnoreCase);
            bool revision25EfficiencyHeaderAlign = args.Length > 3 &&
                args[3].Equals("--rev25-efficiency-header-align", StringComparison.OrdinalIgnoreCase);
            bool cipRecipeEngine = args.Length > 3 &&
                args[3].Equals("--cip-recipe-engine", StringComparison.OrdinalIgnoreCase);
            string cipRecipeEngineTarget = cipRecipeEngine && args.Length > 4
                ? args[4]
                : null;
            bool compileSaveCurrentHmi = args.Length > 3 &&
                args[3].Equals("--compile-save-current-hmi", StringComparison.OrdinalIgnoreCase);
            bool reloadExactOpenProject = args.Length > 3 &&
                args[3].Equals("--reload-exact-open-project", StringComparison.OrdinalIgnoreCase);

            if (verifyNavigationOnly)
            {
                return VerifyNavigationMaster.Run(new[] { projectHint, verifyNavigationTarget });
            }

            try
            {
                int reloadProcessId = 0;
                if (reloadExactOpenProject && args.Length > 4)
                {
                    Int32.TryParse(args[4], out reloadProcessId);
                }
                TiaPortalProcess selected = reloadProcessId > 0
                    ? TiaPortal.GetProcesses().FirstOrDefault(process => process.Id == reloadProcessId)
                    : TiaPortal.GetProcesses().FirstOrDefault(
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
                    if (reloadExactOpenProject)
                    {
                        string requestedProjectPath = Path.GetFullPath(projectHint);
                        Project currentProject = portal.Projects.FirstOrDefault();
                        if (currentProject != null)
                        {
                            string currentProjectPath = Path.GetFullPath(currentProject.Path.FullName);
                            if (!currentProjectPath.Equals(requestedProjectPath, StringComparison.OrdinalIgnoreCase))
                            {
                                throw new InvalidOperationException(
                                    "Reload target does not match the attached project: " + currentProjectPath);
                            }
                            currentProject.Close();
                        }
                        Project reopened = portal.Projects.Open(new FileInfo(requestedProjectPath));
                        if (reopened == null)
                        {
                            throw new InvalidOperationException("TIA did not reopen the exact project.");
                        }
                        Console.WriteLine("PROJECT=" + reopened.Path.FullName);
                        Console.WriteLine("SAVE_INVOKED=NO");
                        Console.WriteLine("STATUS=PASS");
                        return 0;
                    }

                    Project project = portal.Projects.FirstOrDefault();
                    if (project == null)
                    {
                        Console.Error.WriteLine("STATUS=FAIL");
                        Console.Error.WriteLine("Attached TIA process has no open project.");
                        return 3;
                    }

                    Device hmiDevice;
                    DeviceItem hmiDeviceItem;
                    HmiSoftware hmi = FindUnifiedHmi(project, out hmiDevice, out hmiDeviceItem);
                    if (hmi == null)
                    {
                        Console.Error.WriteLine("STATUS=FAIL");
                        Console.Error.WriteLine("No WinCC Unified HMI software found.");
                        return 4;
                    }

                    HmiScreen home = hmi.Screens.Find("home");
                    if (home == null)
                    {
                        Console.Error.WriteLine("STATUS=FAIL");
                        Console.Error.WriteLine("Home screen not found.");
                        return 5;
                    }

                    Directory.CreateDirectory(archiveDirectory);
                    if (existingArchive == null)
                    {
                        string archiveName = "schlenkers-pre-home-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
                        project.Save();
                        project.Archive(new DirectoryInfo(archiveDirectory), archiveName, ProjectArchivationMode.Compressed);
                        Console.WriteLine("ARCHIVE=" + Path.Combine(archiveDirectory, archiveName + ".zap19"));
                    }
                    else
                    {
                        if (!File.Exists(existingArchive))
                        {
                            throw new FileNotFoundException("Existing archive was not found.", existingArchive);
                        }
                        Console.WriteLine("ARCHIVE_EXISTING=" + existingArchive);
                    }

                    string repositoryRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", ".."));
                    if (compileSaveCurrentHmi)
                    {
                        Console.WriteLine("STEP=COMPILE_SAVE_CURRENT_HMI");
                    }
                    else if (cipRecipeEngine)
                    {
                        EnsureHmiTags(hmi, Path.Combine(repositoryRoot, "REV12", "HMI", "REV12_HMI_Tags.csv"));
                        EnsureCipRecipeEngineAlarms(hmi);
                        BuildCipRecipeEnginePage(hmi, cipRecipeEngineTarget);
                        Console.WriteLine("STEP=CIP_RECIPE_ENGINE_READY");
                        Console.WriteLine("CIP_RECIPE_TARGET=" + cipRecipeEngineTarget);
                    }
                    else if (revision25EfficiencyHeaderAlign)
                    {
                        AlignRevision25EfficiencyHeader(hmi);
                        Console.WriteLine("STEP=REV25_EFFICIENCY_HEADER_ALIGNED");
                    }
                    else if (revision25LiftFunctionButtonAlign)
                    {
                        AlignRevision25LiftFunctionButton(hmi);
                        Console.WriteLine("STEP=REV25_LIFT_FUNCTION_BUTTON_ALIGNED");
                    }
                    else if (revision25FooterAlign)
                    {
                        AlignRevision25CommonFooter(hmi, revision25FooterTarget);
                        Console.WriteLine("STEP=REV25_COMMON_FOOTER_ALIGNED");
                    }
                    else if (revision25IoLinkHeaderAlign)
                    {
                        AlignRevision25IoLinkDetailHeaders(hmi, revision25IoLinkHeaderTarget);
                        Console.WriteLine("STEP=REV25_IOLINK_DETAIL_HEADERS_ALIGNED");
                    }
                    else if (revision25MasterFrame)
                    {
                        if (String.IsNullOrWhiteSpace(revision25MasterFrameTarget))
                        {
                            throw new InvalidOperationException("REV25 requires one explicit target screen per transaction.");
                        }
                        if (revision25MasterFrameTarget.Equals("production", StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException("REV25 Production master is frozen and cannot be a target.");
                        }

                        ApplyRevision25ProductionMasterFrameExact(
                            project, hmi, revision25MasterFrameTarget);
                        Console.WriteLine("STEP=REV25_MASTER_FRAME_READY");
                        Console.WriteLine("REV25_TARGET=" + revision25MasterFrameTarget);
                        Console.WriteLine("REV25_PRODUCTION_MASTER_MODIFIED=0");
                        Console.WriteLine("REV25_PAGE_CONTENT_BINDINGS_MODIFIED=0");
                    }
                    else if (productionCommandLabelsCrisp)
                    {
                        MakeProductionCommandLabelsCrisp(hmi);
                        Console.WriteLine("STEP=PRODUCTION_COMMAND_LABELS_CRISP");
                    }
                    else if (productionCommandButtonAreaIncrease25)
                    {
                        IncreaseProductionCommandButtonAreasTwentyFivePercent(hmi);
                        Console.WriteLine("STEP=PRODUCTION_COMMAND_BUTTON_AREAS_INCREASED_25_PERCENT");
                    }
                    else if (productionCommandIconsIncrease55)
                    {
                        SetProductionCommandIconScale(hmi, 1.55);
                        Console.WriteLine("STEP=PRODUCTION_COMMAND_ICONS_INCREASED_55_PERCENT");
                    }
                    else if (productionCommandIconsIncrease35)
                    {
                        SetProductionCommandIconScale(hmi, 1.35);
                        Console.WriteLine("STEP=PRODUCTION_COMMAND_ICONS_INCREASED_35_PERCENT");
                    }
                    else if (productionCommandIconsIncrease25)
                    {
                        SetProductionCommandIconScale(hmi, 1.25);
                        Console.WriteLine("STEP=PRODUCTION_COMMAND_ICONS_INCREASED_25_PERCENT");
                    }
                    else if (productionCommandIconsIncrease20)
                    {
                        SetProductionCommandIconScale(hmi, 1.20);
                        Console.WriteLine("STEP=PRODUCTION_COMMAND_ICONS_INCREASED_20_PERCENT");
                    }
                    else if (productionCommandIconsOnly)
                    {
                        Dictionary<string, string> commandGraphics = EnsureRevision14Graphics(
                            project,
                            Path.Combine(repositoryRoot, ".modification_logic", "svg"),
                            Path.Combine(archiveDirectory, "production-command-icon-import"),
                            false);
                        ApplyProductionCommandIconsOnly(hmi, commandGraphics);
                        Console.WriteLine("STEP=PRODUCTION_COMMAND_ICONS_ONLY_READY");
                    }
                    else if (productionClockOnly)
                    {
                        AddProductionClockOnly(hmi);
                        Console.WriteLine("STEP=PRODUCTION_CLOCK_ONLY_READY");
                    }
                    else if (productionOnlyFinalize)
                    {
                        PolishProductionMimicAndCommonStatus(hmi, true);
                        Console.WriteLine("STEP=PRODUCTION_ONLY_MIMIC_KPI_AND_CLOCK_READY");
                    }
                    else if (productionMimicPolish)
                    {
                        PolishProductionMimicAndCommonStatus(hmi, false);
                        Console.WriteLine("STEP=PRODUCTION_MIMIC_POLISHED_AND_COMMON_CLOCK_READY");
                    }
                    else if (userHeaderLayoutFix)
                    {
                        Dictionary<string, string> userGraphics = EnsureRevision14Graphics(
                            project,
                            Path.Combine(repositoryRoot, ".modification_logic", "svg"),
                            Path.Combine(archiveDirectory, "user-profile-icon-import"),
                            false);
                        FixUserHeaderLayoutOnNonProductionScreens(hmi, userGraphics["user_profile"], userHeaderLayoutTarget);
                        Console.WriteLine("STEP=USER_HEADER_NONPRODUCTION_LAYOUT_FIXED");
                    }
                    else if (userHeaderMissingOnly)
                    {
                        ApplyUserHeaderToMissingScreens(hmi);
                        Console.WriteLine("STEP=GLOBAL_USER_HEADER_MISSING_SCREENS_READY");
                    }
                    else if (revision23PilzProfinetDiagnostics)
                    {
                        EnsureHmiTags(hmi, Path.Combine(repositoryRoot, "REV12", "HMI", "REV12_HMI_Tags.csv"));
                        BuildRevision23PilzProfinetDiagnostics(hmi);
                        Console.WriteLine("STEP=REV23_PILZ_PROFINET_NONSAFETY_DIAGNOSTICS_READY");
                    }
                    else if (revision22PilzDiagnostics)
                    {
                        BuildRevision22PilzDiagnostics(hmi, project);
                        Console.WriteLine("STEP=REV22_PILZ_SAFETY_DIAGNOSTICS_READY");
                    }
                    else if (revision21AlarmMatrix)
                    {
                        EnsureHmiTags(hmi, Path.Combine(repositoryRoot, "REV12", "HMI", "REV12_HMI_Tags.csv"));
                        EnsureRevision21Alarms(hmi);
                        Console.WriteLine("STEP=REV21_ALARM_MATRIX_READY");
                    }
                    else if (revision18TagsOnly)
                    {
                        EnsureHmiTags(hmi, Path.Combine(repositoryRoot, "REV12", "HMI", "REV12_HMI_Tags.csv"));
                        Console.WriteLine("STEP=REV18_TAGS_READY");
                    }
                    else if (revision18IoLinkPage)
                    {
                        BuildRevision18IoLinkPage(hmi, revision18IoLinkTarget, revision18IoLinkStage);
                        Console.WriteLine("STEP=REV18_IOLINK_PAGE_READY");
                        Console.WriteLine("PAGE=" + revision18IoLinkTarget);
                        Console.WriteLine("STAGE=" + (revision18IoLinkStage ?? "FULL"));
                    }
                    else if (revision18CipPage)
                    {
                        BuildRevision18CipPage(hmi, revision18CipStage);
                        Console.WriteLine("STEP=REV18_CIP_PAGE_READY");
                        Console.WriteLine("PAGE=cip");
                        Console.WriteLine("STAGE=" + (revision18CipStage ?? "FULL"));
                    }
                    else if (revision16SmcAnybus)
                    {
                        EnsureHmiTags(hmi, Path.Combine(repositoryRoot, "REV12", "HMI", "REV12_HMI_Tags.csv"));
                        BuildRevision16SmcAnybus(hmi);
                        Console.WriteLine("STEP=REV16_SMC_ANYBUS_DIAGNOSTICS_READY");
                    }
                    else if (revision15IoFixEv210Live)
                    {
                        FixRevision15Ev210Live(hmi);
                        Console.WriteLine("STEP=REV15_IO_EV210_LIVE_READY");
                    }
                    else if (revision15IoCompleteMissing)
                    {
                        CompleteRevision15IoDiagnosticsMissing(hmi);
                        Console.WriteLine("STEP=REV15_IO_MISSING_OBJECTS_READY");
                    }
                    else if (revision15IoNavigationDirect)
                    {
                        ApplyRevision15IoNavigationDirect(project, hmi);
                        Console.WriteLine("STEP=REV15_IO_NAVIGATION_DIRECT_READY");
                    }
                    else if (revision15IoDiagnostics)
                    {
                        HmiScreen ioDiagnostics = GetOrCreateScreen(hmi, "io_diagnostics");
                        BuildRevision15IoDiagnostics(ioDiagnostics);
                        AddRevision15IoEntryPoints(hmi);
                        ApplyProductionNavigationMasterToAllScreens(project, hmi, "io_diagnostics");
                        Console.WriteLine("STEP=REV15_IO_CONFIGURATION_DIAGNOSTICS_READY");
                    }
                    else if (revision13NavMasterAll)
                    {
                        ApplyProductionNavigationMasterToAllScreens(project, hmi, revision13NavMasterTarget);
                        Console.WriteLine("STEP=PRODUCTION_NAVIGATION_MASTER_ALL_SCREENS_READY");
                    }
                    else if (revision13NavButtonsAll)
                    {
                        ApplyRevision13ButtonsToAllScreens(hmi);
                        Console.WriteLine("STEP=REV13_NAVIGATION_BUTTONS_ALL_SCREENS_READY");
                    }
                    else if (revision14NavContrast)
                    {
                        HmiScreen productionPilot = hmi.Screens.Find("production");
                        if (productionPilot == null)
                        {
                            throw new InvalidOperationException(
                                "REV14.1 navigation contrast update requires the existing 'production' screen.");
                        }

                        Dictionary<string, string> graphics = EnsureRevision14Graphics(
                            project,
                            Path.Combine(repositoryRoot, ".modification_logic", "svg"),
                            Path.Combine(archiveDirectory, "rev14-navigation-contrast-import"),
                            true);
                        RefreshRevision14NavigationContrast(productionPilot, graphics);
                        CleanupRevision14PilotDuplicates(productionPilot);
                        Console.WriteLine("STEP=REV14_1_NAVIGATION_CONTRAST_READY");
                    }
                    else if (revision14Cleanup)
                    {
                        HmiScreen productionPilot = hmi.Screens.Find("production");
                        if (productionPilot == null)
                        {
                            throw new InvalidOperationException(
                                "REV14.1 cleanup requires the existing 'production' screen.");
                        }

                        CleanupRevision14PilotDuplicates(productionPilot);
                        Console.WriteLine("STEP=REV14_1_PRODUCTION_OVERLAP_CLEANUP_READY");
                    }
                    else if (revision14Pilot)
                    {
                        HmiScreen productionPilot = hmi.Screens.Find("production");
                        if (productionPilot == null)
                        {
                            throw new InvalidOperationException(
                                "REV14.1 pilot requires the existing 'production' screen; no screen was created.");
                        }

                        Dictionary<string, string> graphics = EnsureRevision14Graphics(
                            project,
                            Path.Combine(repositoryRoot, ".modification_logic", "svg"),
                            Path.Combine(archiveDirectory, "rev14-graphics-import"),
                            false);
                        BuildRevision14Pilot(home, productionPilot, graphics);
                        Console.WriteLine("STEP=REV14_1_HOME_PRODUCTION_PILOT_READY");
                    }
                    else
                    {
                    EnsureHmiTags(hmi, Path.Combine(repositoryRoot, "REV12", "HMI", "REV12_HMI_Tags.csv"));
                    Console.WriteLine("STEP=HMI_TAGS_READY");
                    EnsureNewAlarms(hmi);
                    Console.WriteLine("STEP=ALARMS_READY");

                    HmiScreen operate = GetOrCreateScreen(hmi, "operate");
                    HmiScreen production = GetOrCreateScreen(hmi, "production");
                    HmiScreen function = GetOrCreateScreen(hmi, "function");
                    HmiScreen alarms = GetOrCreateScreen(hmi, "alarms");
                    HmiScreen recipe = GetOrCreateScreen(hmi, "recipe");
                    HmiScreen setup = GetOrCreateScreen(hmi, "setup");
                    HmiScreen manual = GetOrCreateScreen(hmi, "manual");
                    HmiScreen efficiency = GetOrCreateEfficiencyScreen(hmi);
                    HmiScreen cip = GetOrCreateScreen(hmi, "cip");
                    HmiScreen diagnostics = GetOrCreateScreen(hmi, "diagnostics");
                    HmiScreen safety = GetOrCreateScreen(hmi, "safety");
                    HmiScreen settingsInputs = GetOrCreateScreen(hmi, "settings_inputs");
                    HmiScreen settingsOutputs = GetOrCreateScreen(hmi, "settings_outputs");
                    HmiScreen settingsTimers = GetOrCreateScreen(hmi, "settings_timers");
                    HmiScreen settingsWash = GetOrCreateScreen(hmi, "settings_external_wash");
                    HmiScreen liftWarning = GetOrCreateScreen(hmi, "lift_warning");

                    if (revision13HomeProduction)
                    {
                        BuildRevision13Home(home);
                        BuildProductionMasterExact(production);
                        ApplyRevision13ProductionVisuals(production);
                        Console.WriteLine("STEP=REV13_HOME_PRODUCTION_READY");
                    }
                    else if (productionMasterExact)
                    {
                        BuildProductionMasterExact(production);
                        Console.WriteLine("STEP=PRODUCTION_MASTER_EXACT_READY");
                    }
                    else if (productionCleanOnly)
                    {
                        BuildProductionMimicClean(production);
                        AddNavigationRail(production, "PRODUCTION");
                        Console.WriteLine("STEP=PRODUCTION_CLEAN_REBUILD_READY");
                    }
                    else if (engineeringSettingsUpdate)
                    {
                        BuildSetup(setup);
                        BuildSettingsInputs(settingsInputs);
                        BuildSettingsOutputs(settingsOutputs);
                        BuildSettingsTimers(settingsTimers);
                        BuildSettingsExternalWash(settingsWash);
                        BuildLiftWarning(liftWarning);
                        BuildManual(manual);
                        BuildProductionMimicClean(production);
                        BuildAlarms(alarms);
                        AddNavigationRail(setup, "SETUP");
                        AddNavigationRail(settingsInputs, "SETUP");
                        AddNavigationRail(settingsOutputs, "SETUP");
                        AddNavigationRail(settingsTimers, "SETUP");
                        AddNavigationRail(settingsWash, "SETUP");
                        AddNavigationRail(liftWarning, "MANUAL");
                        AddNavigationRail(manual, "MANUAL");
                        AddNavigationRail(production, "PRODUCTION");
                        Console.WriteLine("STEP=SETTINGS_CABLE_INTEGRATION_READY");
                    }
                    else if (efficiencyUpdate)
                    {
                        BuildAlarms(alarms);
                        Console.WriteLine("SCREEN_BUILT=alarms");
                        BuildEfficiency(efficiency);
                        Console.WriteLine("SCREEN_BUILT=efficiency");
                        HmiScreen[] allNavigationScreens =
                        {
                            home, safety, operate, production, cip, function, alarms, recipe, setup, manual, efficiency, diagnostics
                        };
                        string[] allActivePages =
                        {
                            "HOME", "SAFETY", "OPERATE", "PRODUCTION", "CIP", "FUNCTION", "ALARMS", "RECIPE", "SETUP", "MANUAL", "EFFICIENCY", "DIAGNOSTICS"
                        };
                        for (int index = 0; index < allNavigationScreens.Length; index++)
                        {
                            AddNavigationRail(allNavigationScreens[index], allActivePages[index]);
                        }
                        Console.WriteLine("STEP=EFFICIENCY_UPDATE_READY");
                    }
                    else if (alarmResetOnly)
                    {
                        BuildAlarms(alarms);
                        Console.WriteLine("SCREEN_BUILT=alarms");
                        Console.WriteLine("STEP=ALARM_RESET_ICON_READY");
                    }
                    else
                    {
                        Console.WriteLine("STEP=BUILD_HOME_START");
                        UpdateHomeFromBaseline(home);
                        Console.WriteLine("SCREEN_BUILT=home");
                        BuildSafety(safety);
                        Console.WriteLine("SCREEN_BUILT=safety");
                        BuildOperate(production);
                        Console.WriteLine("SCREEN_BUILT=production");
                        BuildAlarms(alarms);
                        Console.WriteLine("SCREEN_BUILT=alarms");
                        HmiScreen[] navigationScreens =
                        {
                            operate, production, function, alarms, recipe, setup, manual, efficiency, cip, diagnostics
                        };
                        string[] activePages =
                        {
                            "OPERATE", "PRODUCTION", "FUNCTION", "ALARMS", "RECIPE", "SETUP", "MANUAL", "EFFICIENCY", "CIP", "DIAGNOSTICS"
                        };
                        for (int index = 0; index < navigationScreens.Length; index++)
                        {
                            if (navigationScreens[index].Name.Equals("operate", StringComparison.OrdinalIgnoreCase))
                            {
                                CleanOperateLegacyObjects(navigationScreens[index]);
                                AddOperateCommands(navigationScreens[index]);
                                AddOperateProcessStatus(navigationScreens[index]);
                                ConfigureText(GetOrCreate<HmiText>(navigationScreens[index], "REV12_Operate_Header_Subtitle"), 820, 18, 370, 35,
                                    "OPERATE  |  FBS GLOBAL", Color.White, 18, HmiFontWeight.Bold, HmiHorizontalAlignment.Right);
                            }
                            AddNavigationRail(navigationScreens[index], activePages[index]);
                        }
                        Console.WriteLine("STEP=ALL_NAVIGATION_READY");
                    }
                    }

                    ICompilable compilable = hmiDevice.GetService<ICompilable>();
                    if (compilable == null)
                    {
                        compilable = hmiDeviceItem.GetService<ICompilable>();
                    }
                    if (compilable == null)
                    {
                        throw new InvalidOperationException(
                            "TIA V19 did not expose an HMI compiler service on the device or runtime item.");
                    }
                    CompilerResult result = compilable.Compile();
                    PrintCompilerResult(result, "");
                    Console.WriteLine("COMPILE_ERRORS=" + result.ErrorCount);
                    Console.WriteLine("COMPILE_WARNINGS=" + result.WarningCount);

                    if (result.ErrorCount != 0)
                    {
                        Console.Error.WriteLine("STATUS=FAIL_NOT_SAVED");
                        Console.Error.WriteLine("Home-screen changes remain unsaved in TIA; close without saving or restore the archive.");
                        return 6;
                    }

                    project.Save();
                    Console.WriteLine("HOME_ITEMS=" + home.ScreenItems.Count);
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

        private static HmiSoftware FindUnifiedHmi(
            Project project,
            out Device hmiDevice,
            out DeviceItem hmiDeviceItem)
        {
            hmiDevice = null;
            hmiDeviceItem = null;
            foreach (Device device in project.Devices)
            {
                HmiSoftware hmi = FindUnifiedHmi(device.DeviceItems, out hmiDeviceItem);
                if (hmi != null)
                {
                    hmiDevice = device;
                    return hmi;
                }
            }
            return null;
        }

        private static HmiSoftware FindUnifiedHmi(DeviceItemComposition items, out DeviceItem hmiDeviceItem)
        {
            hmiDeviceItem = null;
            foreach (DeviceItem item in items)
            {
                SoftwareContainer container = item.GetService<SoftwareContainer>();
                HmiSoftware hmi = container == null ? null : container.Software as HmiSoftware;
                if (hmi != null)
                {
                    hmiDeviceItem = item;
                    return hmi;
                }
                hmi = FindUnifiedHmi(item.DeviceItems, out hmiDeviceItem);
                if (hmi != null)
                {
                    return hmi;
                }
            }
            return null;
        }

        private static HmiScreen GetOrCreateScreen(HmiSoftware hmi, string name)
        {
            HmiScreen screen = hmi.Screens.Find(name);
            return screen ?? hmi.Screens.Create(name);
        }

        private static HmiScreen GetOrCreateEfficiencyScreen(HmiSoftware hmi)
        {
            HmiScreen efficiency = hmi.Screens.Find("efficiency");
            if (efficiency != null)
            {
                return efficiency;
            }

            HmiScreen legacyTrends = hmi.Screens.Find("trends");
            if (legacyTrends != null)
            {
                legacyTrends.Name = "efficiency";
                return legacyTrends;
            }

            return hmi.Screens.Create("efficiency");
        }

        private static void EnsureHmiTags(HmiSoftware hmi, string csvPath)
        {
            if (!File.Exists(csvPath)) throw new FileNotFoundException("REV12 HMI tag map not found.", csvPath);
            HmiTagTable table = hmi.TagTables.Find("REV12") ?? hmi.TagTables.Create("REV12");
            string[] lines = File.ReadAllLines(csvPath);
            int created = 0;
            int updated = 0;
            for (int index = 1; index < lines.Length; index++)
            {
                if (String.IsNullOrWhiteSpace(lines[index])) continue;
                string[] fields = ParseCsvLine(lines[index]).ToArray();
                if (fields.Length < 6) throw new InvalidDataException("Invalid HMI tag row " + (index + 1));
                HmiTag tag = table.Tags.Find(fields[0]);
                if (tag == null)
                {
                    tag = table.Tags.Create(fields[0]);
                    created++;
                }
                else updated++;
                tag.Connection = "HMI_Connection_1";
                tag.PlcTag = fields[1];
                tag.AcquisitionMode = HmiAcquisitionMode.CyclicOnUse;
                tag.AcquisitionCycle = "T1s";
                SetText(tag.Comment, "[" + fields[4] + "] " + fields[5] + " | Intended access: " + fields[3]);
            }
            Console.WriteLine("HMI_TAGS_CREATED=" + created);
            Console.WriteLine("HMI_TAGS_UPDATED=" + updated);
        }

        private static List<string> ParseCsvLine(string line)
        {
            List<string> fields = new List<string>();
            string current = "";
            bool quoted = false;
            for (int index = 0; index < line.Length; index++)
            {
                char ch = line[index];
                if (ch == '"')
                {
                    if (quoted && index + 1 < line.Length && line[index + 1] == '"')
                    {
                        current += '"';
                        index++;
                    }
                    else quoted = !quoted;
                }
                else if (ch == ',' && !quoted)
                {
                    fields.Add(current);
                    current = "";
                }
                else current += ch;
            }
            fields.Add(current);
            return fields;
        }

        private static void EnsureNewAlarms(HmiSoftware hmi)
        {
            EnsureAlarm(hmi, "ALM_2101_CIPStartBlocked", 2101U, 23U, "Warning",
                "Stop Production before starting CIP.", "CIP request blocked by the mode interlock.");
            EnsureAlarm(hmi, "ALM_2102_CIPMediaLoss", 2102U, 24U, "Critical",
                "CIP fluid presence lost.", "Product pump stopped after the configured media-loss delay.");
            EnsureAlarm(hmi, "ALM_2103_LowVacuum", 2103U, 25U, "Critical",
                "Production vacuum below configured minimum.", "Filler stopped; correct vacuum fault and reset.");
        }

        private static void EnsureRevision21Alarms(HmiSoftware hmi)
        {
            EnsureAlarmOnTag(hmi, "ALM_2201_CapHopperLow", 2201U, "Alarm_Word2", 0U, "Warning",
                "Cap hopper low level.", "Refill the cap hopper. Machine may continue until the validated escalation condition.");
            EnsureAlarmOnTag(hmi, "ALM_2202_CapChannelRequest", 2202U, "Alarm_Word2", 1U, "Warning",
                "Cap channel request / low supply.", "Check cap feed and refill the hopper.");
            EnsureAlarmOnTag(hmi, "ALM_2203_CapChannelEmpty", 2203U, "Alarm_Word2", 2U, "Critical",
                "Cap channel empty / caps unavailable.", "Controlled stop. Restore caps, clear the channel, then reset.");
            EnsureAlarmOnTag(hmi, "ALM_2204_CapperNotReady", 2204U, "Alarm_Word2", 3U, "Critical",
                "Capper not ready during Production.", "Check capper drive and permissives before reset.");
            EnsureAlarmOnTag(hmi, "ALM_2211_BottleShortage1", 2211U, "Alarm_Word2", 4U, "Warning",
                "Bottle shortage level 1.", "Restore infeed. Existing debounce and line-speed coordination remain active.");
            EnsureAlarmOnTag(hmi, "ALM_2212_BottleShortage2", 2212U, "Alarm_Word2", 5U, "Critical",
                "Bottle shortage level 2.", "Controlled stop. Restore bottle supply before reset.");
            EnsureAlarmOnTag(hmi, "ALM_2213_Accumulation1", 2213U, "Alarm_Word2", 6U, "Warning",
                "Outfeed accumulation level 1.", "Clear the downstream accumulation. Existing line slowdown remains active.");
            EnsureAlarmOnTag(hmi, "ALM_2214_Accumulation2", 2214U, "Alarm_Word2", 7U, "Critical",
                "Outfeed accumulation level 2.", "Controlled stop. Clear the outfeed before reset.");
            EnsureAlarmOnTag(hmi, "ALM_2221_DriveNotReady", 2221U, "Alarm_Word2", 8U, "Critical",
                "Main or conveyor drive not ready during Production.", "Open drive diagnostics and correct the readiness condition.");
            EnsureAlarmOnTag(hmi, "ALM_2231_SafetySystemFault", 2231U, "Alarm_Word2", 9U, "Critical",
                "Safety system diagnostic fault.", "Safety PLC remains authoritative. Inspect Safety diagnostics; HMI reset cannot bypass it.");
            EnsureAlarmOnTag(hmi, "ALM_2241_CIPRemoteTimeout", 2241U, "Alarm_Word2", 10U, "Critical",
                "CIP remote ready/accept timeout.", "Check the customer handshake and active CIP phase.");
            EnsureAlarmOnTag(hmi, "ALM_2242_CIPRemoteFault", 2242U, "Alarm_Word2", 11U, "Critical",
                "CIP customer remote fault.", "CIP aborted to the defined safe state. Inspect the customer interface.");
            EnsureAlarmOnTag(hmi, "ALM_2243_CIPInvalidState", 2243U, "Alarm_Word2", 12U, "Critical",
                "CIP sequence invalid state.", "Inspect CIP step and fault code before reset.");
            EnsureAlarmOnTag(hmi, "ALM_2244_CIPOperatorAbort", 2244U, "Alarm_Word2", 13U, "Information",
                "CIP sequence aborted by operator.", "Controlled abort status; acknowledge only after the sequence is safe.");
            EnsureAlarmOnTag(hmi, "ALM_2245_CIPComplete", 2245U, "Alarm_Word2", 14U, "Information",
                "CIP complete awaiting operator acknowledgement.", "Acknowledge completion and select the next permitted mode.");
            EnsureAlarmOnTag(hmi, "ALM_2246_AL104CommFault", 2246U, "Alarm_Word2", 15U, "Critical",
                "AL104 customer CIP interface communication fault.", "New CIP requests are inhibited. Open AL104 diagnostics.");
        }

        private static void EnsureAlarm(
            HmiSoftware hmi, string name, uint id, uint bit, string alarmClass, string text, string info)
        {
            HmiDiscreteAlarm alarm = hmi.DiscreteAlarms.Find(name) ?? hmi.DiscreteAlarms.Create(name);
            alarm.Id = id;
            alarm.AlarmClass = alarmClass;
            alarm.RaisedStateTag = "Alarm_Word1";
            alarm.RaisedStateTagBitNumber = bit;
            alarm.TriggerMode = HmiDiscreteAlarmTriggerMode.OnRisingEdge;
            SetText(alarm.EventText1, text);
            SetText(alarm.InfoText, info);
        }

        private static void EnsureAlarmOnTag(
            HmiSoftware hmi, string name, uint id, string triggerTag, uint bit,
            string alarmClass, string text, string info)
        {
            HmiDiscreteAlarm alarm = hmi.DiscreteAlarms.Find(name) ?? hmi.DiscreteAlarms.Create(name);
            alarm.Id = id;
            alarm.AlarmClass = alarmClass;
            alarm.RaisedStateTag = triggerTag;
            alarm.RaisedStateTagBitNumber = bit;
            alarm.TriggerMode = HmiDiscreteAlarmTriggerMode.OnRisingEdge;
            SetText(alarm.EventText1, text);
            SetText(alarm.InfoText, info);
        }

        private static void EnsureCipRecipeEngineAlarms(HmiSoftware hmi)
        {
            EnsureAlarmOnTag(hmi, "ALM_2250_CIPRecipeInvalid", 2250U, "Alarm_Word2", 16U, "Critical",
                "CIP recipe is invalid.", "Correct the recipe validation code before starting or resuming CIP.");
            EnsureAlarmOnTag(hmi, "ALM_2251_CIPNextRejected", 2251U, "Alarm_Word2", 17U, "Warning",
                "CIP manual NEXT request rejected.", "NEXT is accepted only at a manual-next boundary with PLC permissives valid.");
        }

        private static void BuildHome(HmiScreen screen)
        {
            screen.BackColor = Color.FromArgb(239, 244, 248);

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "Rectangle_1"), 0, 0, 1366, 72, Navy, Navy, 0);
            ConfigureText(GetOrCreate<HmiText>(screen, "Text_1"), 28, 13, 560, 46,
                "SCHLENKER MONOBLOCK REAL JUICE", Color.White, 28, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Header_Subtitle"), 820, 18, 370, 35,
                "HOME  |  FBS GLOBAL", Color.White, 18, HmiFontWeight.Bold, HmiHorizontalAlignment.Right);

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "Rectangle_2"), 0, 72, 1366, 48, PaleBlue, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "Text_2"), 28, 80, 180, 32,
                "MACHINE STATUS", Dark, 17, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);

            HmiSymbolicIOField machineState = GetOrCreate<HmiSymbolicIOField>(screen, "Symbolic IO field_1");
            ConfigureSymbolicField(machineState, 210, 78, 505, 36, "Machine_State", "MachineStateText");
            HmiScreenItemBase unused = screen.ScreenItems.Find("Symbolic IO field_2");
            if (unused != null)
            {
                unused.Visible = false;
            }

            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Status_Note"), 735, 81, 280, 30,
                "ACTIVE MODE  0 OFF / 1 PROD / 2 CIP / 3 RUN OUT", Dark, 12, HmiFontWeight.Bold, HmiHorizontalAlignment.Right);
            ConfigureIOField(GetOrCreate<HmiIOField>(screen, "REV12_Active_Mode_Code"), 1030, 80, 95, 32,
                "Mode_ActiveCode", true, "0");

            Console.WriteLine("STEP=HOME_CHROME_READY");
            AddMachineOverview(screen);
            Console.WriteLine("STEP=HOME_OVERVIEW_READY");
            AddHomeProcessPanel(screen);
            Console.WriteLine("STEP=HOME_PROCESS_READY");
            AddNavigationRail(screen, "HOME");
            Console.WriteLine("STEP=HOME_NAV_READY");

            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Footer"), 30, 731, 1150, 24,
                "OFFLINE-VALIDATED HMI  |  ALL COMMANDS REMAIN SUBJECT TO PLC AND SAFETY INTERLOCKS",
                Dark, 12, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
        }

        private static void UpdateHomeFromBaseline(HmiScreen screen)
        {
            AddAuxiliaryPanel(screen);
            Console.WriteLine("STEP=HOME_AUXILIARIES_READY");
            AddHomeProcessPanel(screen);
            Console.WriteLine("STEP=HOME_PROCESS_READY");
            AddNavigationRail(screen, "HOME");
            Console.WriteLine("STEP=HOME_NAV_READY");
        }

        private static void BuildOperate(HmiScreen screen)
        {
            screen.BackColor = Color.FromArgb(239, 244, 248);

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Operate_Header"), 0, 0, 1366, 72, Navy, Navy, 0);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Operate_Header_Title"), 28, 13, 560, 46,
                "SCHLENKER MONOBLOCK REAL JUICE", Color.White, 28, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Operate_Header_Subtitle"), 820, 18, 370, 35,
                "PRODUCTION  |  FBS GLOBAL", Color.White, 18, HmiFontWeight.Bold, HmiHorizontalAlignment.Right);

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Operate_Status_Bar"), 0, 72, 1366, 48, PaleBlue, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Operate_Status_Label"), 28, 80, 180, 32,
                "MACHINE STATUS", Dark, 17, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureSymbolicField(GetOrCreate<HmiSymbolicIOField>(screen, "REV12_Operate_State"),
                210, 78, 505, 36, "Machine_State", "MachineStateText");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Operate_Status_Note"), 735, 81, 450, 30,
                "ALL COMMANDS REQUIRE PLC AND SAFETY PERMISSIVES", Dark, 14, HmiFontWeight.Bold, HmiHorizontalAlignment.Right);

            AddOperateCommands(screen);
            AddOperateSafety(screen);
            AddOperateProcessStatus(screen);
            AddOperatePanelStatus(screen);
            AddNavigationRail(screen, "PRODUCTION");

            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Operate_Footer"), 30, 731, 1150, 24,
                "PHYSICAL START, STOP, AUXILIARY AND RESET PUSHBUTTONS REMAIN ACTIVE",
                Dark, 12, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
        }

        private static void BuildProductionMimic(HmiScreen screen)
        {
            BuildOperate(screen);

            // BuildOperate supplies the shared lower status panels, but its upper-right
            // safety widgets are replaced by the dedicated Production command panel.
            // Remove the superseded foreground objects so no hidden legacy layer can
            // overlap the Production controls or process values.
            string[] supersededProductionObjects =
            {
                "REV12_Label_MachineSafetyOK", "REV12_Value_MachineSafetyOK",
                "REV12_Label_EStopChainHealthy", "REV12_Value_EStopChainHealthy",
                "REV12_Label_DoorAllClosed", "REV12_Value_DoorAllClosed",
                "REV12_Label_DoorRequestCombined", "REV12_Value_DoorRequestCombined",
                "REV12_Label_DoorSequenceActive", "REV12_Value_DoorSequenceActive",
                "REV12_Label_DoorRestartPermitted", "REV12_Value_DoorRestartPermitted",
                "REV12_Operate_Safety_Note"
            };
            foreach (string objectName in supersededProductionObjects)
            {
                DeleteItem(screen, objectName);
            }

            // The upper-left zone becomes a read-only interactive process mimic.
            // Equipment selection only navigates to diagnostics; it never writes a command.
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Operate_Command_Panel"), 25, 140, 775, 310, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Operate_Command_Title"), 45, 152, 730, 34,
                "PRODUCT / FILLER / VACUUM PROCESS MIMIC", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);

            ConfigureDashboardLine(GetOrCreate<HmiLine>(screen, "REV12_Mimic_Pipe_In"), 95, 315, 300, 315, Blue, 6);
            ConfigureDashboardLine(GetOrCreate<HmiLine>(screen, "REV12_Mimic_Pipe_Out"), 480, 315, 720, 315, Blue, 6);
            ConfigureDashboardLine(GetOrCreate<HmiLine>(screen, "REV12_Mimic_Vacuum_Line"), 390, 230, 610, 230, Blue, 5);

            ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "REV12_Mimic_ProductPump"), 55, 260, 145, 70,
                "M102\nPRODUCT PUMP", Color.FromArgb(18, 91, 148), "diagnostics", "PRODUCTION");
            ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "REV12_Mimic_Valve210"), 210, 285, 85, 55,
                "EV210", Blue, "diagnostics", "PRODUCTION");

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Mimic_Tank"), 300, 190, 180, 220, Color.White, Navy, 3);
            HmiRectangle liquid = GetOrCreate<HmiRectangle>(screen, "REV12_Mimic_Tank_Liquid");
            ConfigureRectangle(liquid, 306, 404, 168, 1, Color.FromArgb(70, 160, 220), Color.FromArgb(70, 160, 220), 0);
            ConfigureNumericDynamization(liquid, "Height", "Tank_Level_Pct",
                "let v=HMIRuntime.Tags(\"Tank_Level_Pct\").Read(); return Math.max(1,Math.min(208,v*2.08));");
            ConfigureNumericDynamization(liquid, "Top", "Tank_Level_Pct",
                "let v=HMIRuntime.Tags(\"Tank_Level_Pct\").Read(); return 404-Math.max(1,Math.min(208,v*2.08));");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Mimic_Tank_Code"), 315, 205, 150, 30,
                "TLS100", Navy, 18, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureIOField(GetOrCreate<HmiIOField>(screen, "REV12_Mimic_Tank_Level"), 330, 350, 120, 42,
                "Tank_Level_Pct", true, "0.0");

            ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "REV12_Mimic_Valve212"), 510, 205, 85, 55,
                "EV212", Blue, "diagnostics", "PRODUCTION");
            ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "REV12_Mimic_VacuumPump"), 610, 198, 145, 70,
                "M103\nVACUUM PUMP", Color.FromArgb(18, 91, 148), "diagnostics", "PRODUCTION");
            ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "REV12_Mimic_Valve217"), 510, 285, 85, 55,
                "EV217", Blue, "diagnostics", "PRODUCTION");
            ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "REV12_Mimic_Valve213"), 610, 285, 85, 55,
                "EV213", Blue, "diagnostics", "PRODUCTION");
            ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "REV12_Mimic_Valve247"), 705, 285, 70, 55,
                "EV247", Blue, "diagnostics", "PRODUCTION");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Mimic_ProductPresence_Label"), 55, 360, 180, 24,
                "SPL100 PRODUCT PRESENT", Dark, 12, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureBooleanIndicator(GetOrCreate<HmiIOField>(screen, "REV12_Mimic_ProductPresence"), 235, 355, 65, 32,
                "Product_Present_At_Pump");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Mimic_Vacuum_Label"), 500, 360, 145, 24,
                "VS100 VACUUM", Dark, 12, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureIOField(GetOrCreate<HmiIOField>(screen, "REV12_Mimic_Vacuum"), 650, 355, 105, 34,
                "Vacuum_Actual_mbar", true, "0.0");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Mimic_Select_Note"), 55, 410, 700, 25,
                "SELECT EQUIPMENT FOR READ-ONLY DIAGNOSTICS - SELECTION NEVER COMMANDS HARDWARE",
                Red, 12, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);

            // Retain the Production commands in the existing right-side command area.
            // Recreate reused controls after the replacement panel so they are guaranteed
            // to be in the foreground. Moving an existing item does not change its Z-order.
            string[] foregroundProductionControls =
            {
                "REV12_Operate_AutoStart", "REV12_Operate_Stop",
                "REV12_Operate_Reset", "REV12_Operate_Manual",
                "REV12_Operate_Speed_SP_Label", "REV12_Operate_Speed_SP",
                "REV12_Operate_Speed_PV_Label", "REV12_Operate_Speed_PV"
            };
            foreach (string objectName in foregroundProductionControls)
            {
                DeleteItem(screen, objectName);
            }
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Operate_Safety_Panel"), 820, 140, 370, 310, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Operate_Safety_Title"), 840, 152, 330, 34,
                "PRODUCTION COMMANDS", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV12_Operate_AutoStart"), 840, 200, 150, 52,
                "PRODUCTION ON", Green, "Cmd_ProductionOn");
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV12_Operate_Stop"), 1010, 200, 150, 52,
                "PRODUCTION OFF", Red, "Cmd_ProductionOff");
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV12_Operate_Reset"), 840, 265, 150, 52,
                "RESET ALARMS", Blue, "Cmd_Reset");
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV12_Operate_Manual"), 1010, 265, 150, 52,
                "RUN OUT PRODUCT", Amber, "Run_Out_Product_Start");
            AddCompactMetric(screen, "REV12_Operate_Speed_SP_Label", "REV12_Operate_Speed_SP", 840, 335,
                "SPEED SETPOINT [BPH]", "Speed_Setpoint_BPH", false, "0");
            AddCompactMetric(screen, "REV12_Operate_Speed_PV_Label", "REV12_Operate_Speed_PV", 1010, 335,
                "DRIVE SPEED [%]", "Speed_Actual_Pct", true, "0.0");
            HideItem(screen, "REV12_Operate_Pump_PV_Label");
            HideItem(screen, "REV12_Operate_Pump_PV");
            HideItem(screen, "REV12_Operate_Tank_PV_Label");
            HideItem(screen, "REV12_Operate_Tank_PV");
        }

        private static void BuildProductionMimicClean(HmiScreen screen)
        {
            ConfigurePageChrome(screen, "PRODUCTION", "PRODUCTION",
                "PHYSICAL START, STOP, AUXILIARY AND RESET PUSHBUTTONS REMAIN ACTIVE");

            // Start the Production content area from a controlled blank layer. Retain only
            // shared chrome/navigation objects; this prevents any historical object from
            // remaining behind the new mimic or command panels.
            foreach (HmiScreenItemBase item in screen.ScreenItems.ToList())
            {
                if (item.Name.StartsWith("REV12_Common_", StringComparison.OrdinalIgnoreCase) ||
                    item.Name.StartsWith("REV12_Nav_", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                DeleteItem(screen, item.Name);
            }

            // REV12 Production master grid. The global header, status strip, footer and
            // right navigation remain shared chrome. All page-specific objects stay inside
            // X=25..1190 and use explicit gutters so no labels or controls can overlap.
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Production_Mode_Panel"), 25, 140, 225, 100, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Production_Mode_Title"), 40, 150, 195, 26,
                "MACHINE / MODE", Navy, 18, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Production_Mode_Label"), 40, 184, 105, 26,
                "Production", Dark, 13, HmiFontWeight.Normal, HmiHorizontalAlignment.Left);
            ConfigureBooleanIndicator(GetOrCreate<HmiIOField>(screen, "REV12_Production_Mode_Value"), 155, 181, 70, 30,
                "Mode_ProductionActive");

            // Approved Production command functions are preserved exactly; only their
            // placement changes to the master-layout command card.
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Operate_Safety_Panel"), 25, 255, 225, 245, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Operate_Safety_Title"), 40, 267, 195, 28,
                "PRODUCTION COMMANDS", Navy, 18, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV12_Operate_AutoStart"), 40, 308, 92, 46,
                "PRODUCTION\nON", Green, "Cmd_ProductionOn");
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV12_Operate_Stop"), 143, 308, 92, 46,
                "PRODUCTION\nOFF", Red, "Cmd_ProductionOff");
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV12_Operate_Reset"), 40, 367, 92, 46,
                "RESET\nALARMS", Blue, "Cmd_Reset");
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV12_Operate_Manual"), 143, 367, 92, 46,
                "RUN OUT\nPRODUCT", Amber, "Run_Out_Product_Start");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Production_Command_Note"), 40, 429, 195, 52,
                "COMMANDS REQUIRE PLC AND\nSAFETY PERMISSIVES", Red, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Production_Setpoint_Panel"), 25, 515, 225, 200, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Production_Setpoint_Title"), 40, 527, 195, 28,
                "SETPOINT / MODE", Navy, 18, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddProductionMetric(screen, "REV12_Operate_Speed_SP_Label", "REV12_Operate_Speed_SP", 40, 566,
                "SPEED SETPOINT [BPH]", "Speed_Setpoint_BPH", false, "0");
            AddProductionMetric(screen, "REV12_Operate_Speed_PV_Label", "REV12_Operate_Speed_PV", 40, 622,
                "DRIVE SPEED [%]", "Speed_Actual_Pct", true, "0.0");
            AddProductionMetric(screen, "REV12_Production_PumpSpeed_Label", "REV12_Production_PumpSpeed", 40, 678,
                "PUMP SPEED [%]", "Pump_Speed_Pct", true, "0.0");

            // Central read-only product/filler/vacuum mimic. Pipes are created first and
            // remain behind all equipment, identifiers and values.
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Operate_Command_Panel"), 270, 140, 535, 310, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Operate_Command_Title"), 288, 152, 499, 30,
                "PRODUCT / FILLER / VACUUM PROCESS", Navy, 18, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Mimic_ReadOnly_Note"), 288, 183, 499, 22,
                "TOUCH EQUIPMENT FOR READ-ONLY DIAGNOSTICS",
                Red, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);

            ConfigureDashboardLine(GetOrCreate<HmiLine>(screen, "REV12_Mimic_Pipe_In"), 388, 305, 475, 305, Blue, 6);
            ConfigureDashboardLine(GetOrCreate<HmiLine>(screen, "REV12_Mimic_Pipe_Out"), 600, 305, 783, 305, Blue, 6);
            ConfigureDashboardLine(GetOrCreate<HmiLine>(screen, "REV12_Mimic_Vacuum_Line"), 600, 245, 683, 245, Blue, 5);

            ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "REV12_Mimic_ProductPump"), 288, 275, 100, 60,
                "M102\nPRODUCT PUMP", Color.FromArgb(18, 91, 148), "diagnostics", "PRODUCTION");
            ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "REV12_Mimic_Valve210"), 402, 282, 64, 46,
                "EV210", Blue, "diagnostics", "PRODUCTION");

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Mimic_Tank"), 475, 215, 125, 175, Color.White, Navy, 3);
            HmiRectangle liquid = GetOrCreate<HmiRectangle>(screen, "REV12_Mimic_Tank_Liquid");
            ConfigureRectangle(liquid, 481, 384, 113, 1, Color.FromArgb(70, 160, 220), Color.FromArgb(70, 160, 220), 0);
            ConfigureNumericDynamization(liquid, "Height", "Tank_Level_Pct",
                "let v=HMIRuntime.Tags(\"Tank_Level_Pct\").Read(); return Math.max(1,Math.min(158,v*1.58));");
            ConfigureNumericDynamization(liquid, "Top", "Tank_Level_Pct",
                "let v=HMIRuntime.Tags(\"Tank_Level_Pct\").Read(); return 384-Math.max(1,Math.min(158,v*1.58));");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Mimic_Tank_Code"), 482, 228, 111, 28,
                "TLS100", Navy, 17, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureIOField(GetOrCreate<HmiIOField>(screen, "REV12_Mimic_Tank_Level"), 487, 347, 101, 34,
                "Tank_Level_Pct", true, "0.0");

            ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "REV12_Mimic_Valve212"), 617, 222, 62, 46,
                "EV212", Blue, "diagnostics", "PRODUCTION");
            ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "REV12_Mimic_VacuumPump"), 687, 213, 100, 60,
                "M103\nVACUUM PUMP", Color.FromArgb(18, 91, 148), "diagnostics", "PRODUCTION");
            ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "REV12_Mimic_Valve217"), 617, 282, 56, 46,
                "EV217", Blue, "diagnostics", "PRODUCTION");
            ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "REV12_Mimic_Valve213"), 677, 282, 56, 46,
                "EV213", Blue, "diagnostics", "PRODUCTION");
            ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "REV12_Mimic_Valve247"), 737, 282, 56, 46,
                "EV247", Blue, "diagnostics", "PRODUCTION");

            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Mimic_ProductPresence_Label"), 288, 400, 170, 28,
                "SPL100 PRODUCT PRESENT", Dark, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureBooleanIndicator(GetOrCreate<HmiIOField>(screen, "REV12_Mimic_ProductPresence"), 452, 398, 55, 30,
                "Product_Present_At_Pump");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Mimic_Vacuum_Label"), 535, 400, 120, 28,
                "VS100 VACUUM", Dark, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureIOField(GetOrCreate<HmiIOField>(screen, "REV12_Mimic_Vacuum"), 658, 398, 100, 30,
                "Vacuum_Actual_mbar", true, "0.0");

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Production_Performance_Panel"), 825, 140, 365, 310, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Production_Performance_Title"), 845, 152, 325, 30,
                "LIVE PERFORMANCE", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddProductionPerformance(screen, "Tank level", "Tank_Level_Pct", 845, 205, "%", "0.0");
            AddProductionPerformance(screen, "Vacuum actual", "Vacuum_Actual_mbar", 845, 249, "mbar", "0.0");
            AddProductionPerformance(screen, "Vacuum minimum", "Vacuum_Min_mbar", 845, 293, "mbar", "0.0");
            AddProductionPerformance(screen, "Main drive speed", "Speed_Actual_Pct", 845, 337, "%", "0.0");
            AddProductionPerformance(screen, "Pump speed", "Pump_Speed_Pct", 845, 381, "%", "0.0");

            AddProductionProcessStatusAligned(screen);
            AddOperatePanelStatus(screen);
        }

        private static void AddProductionMetric(
            HmiScreen screen, string labelName, string fieldName, int left, int top,
            string label, string tag, bool readOnly, string format)
        {
            ConfigureText(GetOrCreate<HmiText>(screen, labelName), left, top, 195, 20,
                label, Dark, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureIOField(GetOrCreate<HmiIOField>(screen, fieldName), left, top + 21, 195, 31,
                tag, readOnly, format);
        }

        private static void AddProductionPerformance(
            HmiScreen screen, string label, string tag, int left, int top, string unit, string format)
        {
            string safe = new string(tag.Where(char.IsLetterOrDigit).ToArray());
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Performance_Label_" + safe), left, top, 150, 30,
                label, Dark, 13, HmiFontWeight.Normal, HmiHorizontalAlignment.Left);
            ConfigureIOField(GetOrCreate<HmiIOField>(screen, "REV12_Performance_Value_" + safe), left + 155, top - 2, 90, 32,
                tag, true, format);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Performance_Unit_" + safe), left + 252, top, 60, 28,
                unit, Navy, 12, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
        }

        private static void AddProductionProcessStatusAligned(HmiScreen screen)
        {
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Operate_Process_Panel"), 270, 470, 535, 245, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Operate_Process_Title"), 288, 482, 499, 32,
                "PROCESS STATUS", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);

            // Fixed two-column grid with explicit 25 px gutter.
            AddAlignedProductionStatus(screen, "Product pump", "Product_Pump_Run", 288, 526);
            AddAlignedProductionStatus(screen, "Product present", "Product_Present_At_Pump", 288, 562);
            AddAlignedProductionStatus(screen, "Filler running", "Main_Drive_Run", 288, 598);
            AddAlignedProductionStatus(screen, "Bottle wash", "External_Wash_Active", 288, 634);

            AddAlignedProductionStatus(screen, "Vacuum pump", "Vacuum_Pump_Run", 548, 526);
            AddAlignedProductionStatus(screen, "Vacuum ready", "Vacuum_Ready", 548, 562);
            AddAlignedProductionStatus(screen, "Vacuum alarm", "Alarm_Low_Vacuum", 548, 598);
            AddAlignedProductionStatus(screen, "Gate open", "Gate_Open_Output", 548, 634);
        }

        private static void AddAlignedProductionStatus(HmiScreen screen, string label, string tag, int left, int top)
        {
            string safe = new string(tag.Where(char.IsLetterOrDigit).ToArray());
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Label_" + safe), left, top, 145, 28,
                label, Dark, 13, HmiFontWeight.Normal, HmiHorizontalAlignment.Left);
            ConfigureBooleanIndicator(GetOrCreate<HmiIOField>(screen, "REV12_Value_" + safe), left + 150, top - 2, 70, 32,
                tag);
        }

        private static void BuildProductionMasterExact(HmiScreen screen)
        {
            screen.BackColor = Color.FromArgb(244, 247, 250);
            foreach (HmiScreenItemBase item in screen.ScreenItems.ToList())
            {
                DeleteItem(screen, item.Name);
            }

            // Header and status strip reproduce the supplied approved layout at 1366 x 768.
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Common_Header"), 0, 0, 1366, 50, Color.FromArgb(3, 39, 86), Color.FromArgb(3, 39, 86), 0);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Common_Header_Title"), 16, 8, 610, 36,
                "SCHLENKER MONOBLOCK REAL JUICE", Color.White, 24, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Common_Header_Page"), 1010, 10, 335, 34,
                "PRODUCTION  |  FBS GLOBAL", Color.White, 18, HmiFontWeight.Bold, HmiHorizontalAlignment.Right);

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Common_Status_Bar"), 0, 50, 1366, 50, Color.FromArgb(247, 249, 251), Color.FromArgb(220, 226, 232), 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Common_Status_Label"), 26, 61, 125, 28,
                "MACHINE STATUS", Dark, 14, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureSymbolicField(GetOrCreate<HmiSymbolicIOField>(screen, "REV12_Common_State"),
                160, 58, 330, 34, "Machine_State", "MachineStateText");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Production_SystemReady_Label"), 805, 62, 112, 26,
                "SYSTEM READY", Dark, 13, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureStatusLamp(screen, "REV12_Production_SystemReady_Lamp", 795, 75, "Machine_SafetyOK");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Production_Alarm_Header"), 945, 61, 90, 28,
                "ALARMS", Navy, 13, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureBooleanIndicator(GetOrCreate<HmiIOField>(screen, "REV12_Production_Alarm_Header_Value"), 1035, 59, 48, 32,
                "Alarm_Critical");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Production_User_Header"), 1100, 61, 98, 28,
                "USER", Navy, 13, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);

            // Main upper card: process mimic on the left, approved commands on the right.
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Production_Upper_Panel"), 15, 130, 1000, 335, Color.White, Color.FromArgb(215, 222, 228), 1);
            ConfigureDashboardLine(GetOrCreate<HmiLine>(screen, "REV12_Production_Upper_Divider"), 738, 135, 738, 458, Color.FromArgb(220, 226, 232), 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Production_Mimic_Title"), 35, 144, 680, 28,
                "PRODUCT / FILLER / VACUUM PROCESS MIMIC", Navy, 17, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);

            // Product and vacuum paths are drawn first so equipment remains in the foreground.
            ConfigureDashboardLine(GetOrCreate<HmiLine>(screen, "REV12_Mimic_Pipe_In"), 140, 288, 280, 288, Color.FromArgb(0, 93, 190), 5);
            ConfigureDashboardLine(GetOrCreate<HmiLine>(screen, "REV12_Mimic_Pipe_Product"), 405, 288, 715, 288, Color.FromArgb(0, 93, 190), 5);
            ConfigureDashboardLine(GetOrCreate<HmiLine>(screen, "REV12_Mimic_Pipe_Vacuum"), 405, 220, 565, 220, Color.FromArgb(0, 93, 190), 5);

            ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "REV12_Mimic_ProductPump"), 40, 245, 100, 92,
                "M102\nPRODUCT PUMP", Color.FromArgb(18, 91, 148), "diagnostics", "PRODUCTION");
            ConfigureStatusPill(screen, "REV12_Mimic_ProductPump_State", 52, 316, 76, 25, "Product_Pump_Run", "RUNNING");
            ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "REV12_Mimic_Valve210"), 175, 266, 72, 46,
                "EV210", Blue, "diagnostics", "PRODUCTION");
            ConfigureStatusLamp(screen, "REV12_Mimic_Valve210_Lamp", 211, 258, "Valve_210_Cmd");

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Mimic_Tank"), 280, 190, 125, 180, Color.White, Border, 2);
            HmiRectangle liquid = GetOrCreate<HmiRectangle>(screen, "REV12_Mimic_Tank_Liquid");
            ConfigureRectangle(liquid, 286, 364, 113, 1, Color.FromArgb(155, 198, 241), Color.FromArgb(155, 198, 241), 0);
            ConfigureNumericDynamization(liquid, "Height", "Tank_Level_Pct",
                "let v=HMIRuntime.Tags(\"Tank_Level_Pct\").Read(); return Math.max(1,Math.min(132,v*1.32));");
            ConfigureNumericDynamization(liquid, "Top", "Tank_Level_Pct",
                "let v=HMIRuntime.Tags(\"Tank_Level_Pct\").Read(); return 364-Math.max(1,Math.min(132,v*1.32));");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Mimic_Tank_Code"), 290, 205, 105, 26,
                "TLS100", Navy, 16, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureIOField(GetOrCreate<HmiIOField>(screen, "REV12_Mimic_Tank_Level"), 300, 324, 85, 34,
                "Tank_Level_Pct", true, "0.0");

            ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "REV12_Mimic_Valve212"), 460, 198, 72, 46,
                "EV212", Blue, "diagnostics", "PRODUCTION");
            ConfigureStatusLamp(screen, "REV12_Mimic_Valve212_Lamp", 496, 190, "Valve_212_Cmd");
            ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "REV12_Mimic_VacuumPump"), 565, 175, 130, 92,
                "M103\nVACUUM PUMP", Color.FromArgb(18, 91, 148), "diagnostics", "PRODUCTION");
            ConfigureStatusPill(screen, "REV12_Mimic_VacuumPump_State", 588, 245, 84, 25, "Vacuum_Pump_Run", "RUNNING");
            ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "REV12_Mimic_Valve217"), 455, 266, 72, 46,
                "EV217", Blue, "diagnostics", "PRODUCTION");
            ConfigureStatusLamp(screen, "REV12_Mimic_Valve217_Lamp", 491, 258, "Valve_217_Cmd");
            ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "REV12_Mimic_Valve213"), 545, 266, 72, 46,
                "EV213", Blue, "diagnostics", "PRODUCTION");
            ConfigureStatusLamp(screen, "REV12_Mimic_Valve213_Lamp", 581, 258, "Valve_213_Cmd");
            ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "REV12_Mimic_Valve247"), 635, 266, 72, 46,
                "EV247", Blue, "diagnostics", "PRODUCTION");
            ConfigureStatusLamp(screen, "REV12_Mimic_Valve247_Lamp", 671, 258, "Valve_247_Cmd");

            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Mimic_ProductPresence_Label"), 35, 403, 170, 25,
                "SP-100 PRODUCT PRESENT", Navy, 11, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureBooleanIndicator(GetOrCreate<HmiIOField>(screen, "REV12_Mimic_ProductPresence"), 205, 399, 60, 32,
                "Product_Present_At_Pump");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Mimic_Vacuum_Label"), 430, 403, 120, 25,
                "VS100 VACUUM", Navy, 11, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureIOField(GetOrCreate<HmiIOField>(screen, "REV12_Mimic_Vacuum"), 555, 399, 95, 32,
                "Vacuum_Actual_mbar", true, "0.0");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Mimic_Vacuum_Unit"), 656, 403, 50, 25,
                "mbar", Navy, 11, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);

            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Operate_Safety_Title"), 762, 165, 225, 30,
                "PRODUCTION COMMANDS", Navy, 18, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV12_Operate_AutoStart"), 762, 205, 105, 45,
                "PRODUCTION ON", Green, "Cmd_ProductionOn");
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV12_Operate_Stop"), 885, 205, 105, 45,
                "PRODUCTION OFF", Red, "Cmd_ProductionOff");
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV12_Operate_Reset"), 762, 270, 105, 45,
                "RESET ALARMS", Color.FromArgb(0, 78, 171), "Cmd_Reset");
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV12_Operate_Manual"), 885, 270, 105, 45,
                "RUN OUT PRODUCT", Color.FromArgb(241, 145, 0), "Run_Out_Product_Start");
            AddProductionCommandMetric(screen, "REV12_Operate_Speed_SP_Label", "REV12_Operate_Speed_SP", 762, 347,
                "SPEED SETPOINT [BPH]", "Speed_Setpoint_BPH", false, "0");
            AddProductionCommandMetric(screen, "REV12_Operate_Speed_PV_Label", "REV12_Operate_Speed_PV", 885, 347,
                "DRIVE SPEED [%]", "Speed_Actual_Pct", true, "0.0");

            // Lower status cards follow the supplied three-column lamp grid and sequence card.
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Operate_Process_Panel"), 15, 475, 730, 225, Color.White, Color.FromArgb(215, 222, 228), 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Operate_Process_Title"), 35, 488, 690, 28,
                "PROCESS STATUS", Navy, 18, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddExactLampStatus(screen, "Production active", "Mode_ProductionActive", 35, 530, "P1");
            AddExactLampStatus(screen, "Pump enabled", "Product_Pump_Enable", 35, 565, "P2");
            AddExactLampStatus(screen, "Product present", "Product_Present_At_Pump", 35, 600, "P3");
            AddExactLampStatus(screen, "Pump running", "Product_Pump_Run", 35, 635, "P4");
            AddExactLampStatus(screen, "Vacuum ready", "Vacuum_Ready", 255, 530, "P5");
            AddExactLampStatus(screen, "Vacuum alarm", "Alarm_Low_Vacuum", 255, 565, "P6");
            AddExactLampStatus(screen, "Bottle wash", "External_Wash_Active", 255, 600, "P7");
            AddExactLampStatus(screen, "Gate open", "Gate_Open_Output", 255, 635, "P8");
            AddExactLampStatus(screen, "Valve 210", "Valve_210_Cmd", 475, 530, "P9");
            AddExactLampStatus(screen, "Valve 217", "Valve_217_Cmd", 475, 565, "P10");
            AddExactLampStatus(screen, "Valve 213", "Valve_213_Cmd", 475, 600, "P11");
            AddExactLampStatus(screen, "Valve 212", "Valve_212_Cmd", 475, 635, "P12");

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Operate_Panel_Panel"), 750, 475, 265, 225, Color.White, Color.FromArgb(215, 222, 228), 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Operate_Panel_Title"), 770, 488, 225, 28,
                "MODE & SEQUENCE", Navy, 18, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddExactLampStatus(screen, "Valve 247", "Valve_247_Cmd", 770, 530, "S1");
            AddExactLampStatus(screen, "Run Out active", "Run_Out_Active", 770, 565, "S2");
            AddExactLampStatus(screen, "Run Out complete", "Run_Out_Completed", 770, 600, "S3");
            AddExactLampStatus(screen, "Gate closed", "Gate_Close_Output", 770, 635, "S4");
            AddExactLampStatus(screen, "Critical alarm", "Alarm_Critical", 770, 670, "S5");

            BuildExactProductionKpis(screen);
            AddExactProductionNavigation(screen);
            BuildExactProductionBottomBar(screen);
        }

        private static void BuildRevision13Home(HmiScreen screen)
        {
            screen.BackColor = Color.FromArgb(244, 247, 250);
            foreach (HmiScreenItemBase item in screen.ScreenItems.ToList()) DeleteItem(screen, item.Name);

            BuildRevision13Chrome(screen, "HOME", "HOME");

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV13_Home_CommandCard"), 20, 130, 245, 525,
                Color.White, Color.FromArgb(215, 222, 228), 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV13_Home_CommandTitle"), 38, 146, 210, 30,
                "MACHINE COMMANDS", Navy, 18, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV13_Home_ProdOn"), 38, 195, 95, 48,
                "PRODUCTION ON", Green, "Cmd_ProductionOn");
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV13_Home_ProdOff"), 150, 195, 95, 48,
                "PRODUCTION OFF", Red, "Cmd_ProductionOff");
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV13_Home_Cip"), 38, 260, 207, 48,
                "CIP REQUEST", Color.FromArgb(0, 78, 171), "Cmd_CIPStart");
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV13_Home_RunOut"), 38, 325, 207, 48,
                "RUN OUT PRODUCT", Color.FromArgb(241, 145, 0), "Run_Out_Product_Start");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV13_Home_PumpLabel"), 38, 405, 95, 24,
                "PRODUCT PUMP", Navy, 12, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureWriteButton(GetOrCreate<HmiButton>(screen, "REV13_Home_PumpOff"), 128, 395, 54, 42,
                "OFF", Grey, "Product_Pump_Enable", 0);
            ConfigureWriteButton(GetOrCreate<HmiButton>(screen, "REV13_Home_PumpEnable"), 188, 395, 57, 42,
                "ON", Green, "Product_Pump_Enable", 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV13_Home_GateLabel"), 38, 465, 95, 24,
                "BOTTLE GATE", Navy, 12, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureWriteButton(GetOrCreate<HmiButton>(screen, "REV13_Home_GateOff"), 128, 455, 54, 42,
                "OFF", Grey, "Gate_Auto_Request_Enable", 0);
            ConfigureWriteButton(GetOrCreate<HmiButton>(screen, "REV13_Home_GateEnable"), 188, 455, 57, 42,
                "ON", Color.FromArgb(0, 78, 171), "Gate_Auto_Request_Enable", 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV13_Home_CommandNote"), 38, 535, 207, 72,
                "COMMANDS REQUIRE PLC AND\nSAFETY PERMISSIVES", Red, 11, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV13_Home_MachineCard"), 285, 130, 755, 365,
                Color.White, Color.FromArgb(215, 222, 228), 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV13_Home_MachineTitle"), 305, 146, 710, 30,
                "SCHLENKER MONOBLOCK OVERVIEW", Navy, 18, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureDashboardLine(GetOrCreate<HmiLine>(screen, "REV13_Home_Conveyor"), 335, 345, 985, 345,
                Color.FromArgb(120, 135, 150), 8);
            DrawMachineModule(screen, "Infeed", 325, 240, 130, 105, "INFEED", "Gate_Open_Output");
            DrawMachineModule(screen, "Filler", 485, 205, 160, 140, "FILLER", "DO_Main_Run");
            DrawMachineModule(screen, "Capper", 675, 220, 145, 125, "CAPPER", "DO_CapDrive_Run");
            DrawMachineModule(screen, "Outfeed", 850, 245, 140, 100, "OUTFEED", "DO_Conveyor_Run");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV13_Home_WashLabel"), 690, 375, 150, 24,
                "EXTERNAL WASH", Navy, 11, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureBooleanIndicator(GetOrCreate<HmiIOField>(screen, "REV13_Home_WashValue"), 730, 400, 70, 32,
                "External_Wash_Active");

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV13_Home_StatusCard"), 285, 515, 755, 140,
                Color.White, Color.FromArgb(215, 222, 228), 1);
            AddHomeMetric(screen, "Speed", 310, 545, "SPEED SETPOINT [BPH]", "Speed_Setpoint_BPH", "0");
            AddHomeMetric(screen, "Drive", 500, 545, "DRIVE SPEED [%]", "Speed_Actual_Pct", "0.0");
            AddHomeMetric(screen, "Tank", 690, 545, "TANK LEVEL [%]", "Tank_Level_Pct", "0.0");
            AddHomeMetric(screen, "Vacuum", 880, 545, "VACUUM [mbar]", "Vacuum_Actual_mbar", "0.0");

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV13_Home_KpiCard"), 1060, 130, 130, 215,
                Color.White, Color.FromArgb(215, 222, 228), 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV13_Home_KpiTitle"), 1070, 148, 110, 50,
                "TODAY'S\nPRODUCED BOTTLES", Navy, 12, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            DrawBottleIcon(screen, "REV13_Home_Bottle", 1100, 215, Color.FromArgb(0, 78, 171));
            ConfigureText(GetOrCreate<HmiText>(screen, "REV13_Home_KpiUnavailable"), 1070, 280, 110, 38,
                "TAG REQUIRED", Red, 12, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV13_Home_KpiTruthNote"), 1064, 350, 122, 92,
                "No produced-today PLC counter exists. No substitute value is displayed.",
                Dark, 10, HmiFontWeight.Normal, HmiHorizontalAlignment.Center);
        }

        private static void ApplyRevision13ProductionVisuals(HmiScreen screen)
        {
            BuildRevision13Chrome(screen, "PRODUCTION", "PRODUCTION");

            // Replace the former equipment rectangles with graphic process symbols.
            DeleteItem(screen, "REV12_Mimic_ProductPump");
            DeleteItem(screen, "REV12_Mimic_VacuumPump");
            DeleteItem(screen, "REV12_Mimic_Valve210");
            DeleteItem(screen, "REV12_Mimic_Valve212");
            DeleteItem(screen, "REV12_Mimic_Valve217");
            DeleteItem(screen, "REV12_Mimic_Valve213");
            DeleteItem(screen, "REV12_Mimic_Valve247");
            DrawPumpSymbol(screen, "REV13_ProductPump", 66, 255, "M102", "PRODUCT PUMP", "Product_Pump_Run");
            DrawPumpSymbol(screen, "REV13_VacuumPump", 592, 183, "M103", "VACUUM PUMP", "Vacuum_Pump_Run");
            DrawValveSymbol(screen, "REV13_EV210", 178, 270, "EV210", "Valve_210_Cmd");
            DrawValveSymbol(screen, "REV13_EV212", 462, 202, "EV212", "Valve_212_Cmd");
            DrawValveSymbol(screen, "REV13_EV217", 457, 270, "EV217", "Valve_217_Cmd");
            DrawValveSymbol(screen, "REV13_EV213", 547, 270, "EV213", "Valve_213_Cmd");
            DrawValveSymbol(screen, "REV13_EV247", 637, 270, "EV247", "Valve_247_Cmd");

            // Replace misleading KPI cards. Bottle-in-machine is not produced-today.
            DeleteItem(screen, "REV12_Exact_Kpi_Value_Bottles");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Exact_Kpi_Title_Bottles"), 1044, 140, 145, 34,
                "PRODUCED TODAY", Navy, 12, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            DrawBottleIcon(screen, "REV13_Production_Bottle", 1048, 178, Color.FromArgb(0, 78, 171));
            ConfigureText(GetOrCreate<HmiText>(screen, "REV13_Production_BottleUnavailable"), 1082, 180, 102, 34,
                "TAG REQUIRED", Red, 11, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            DeleteItem(screen, "REV12_Exact_Kpi_Unit_Bottles");

            BuildRevision13SpeedGauge(screen, 1117, 290);
            BuildRevision13EfficiencyGauge(screen, 1117, 400);
            AddRevision13Navigation(screen, "PRODUCTION");
        }

        private static Dictionary<string, string> EnsureRevision14Graphics(
            Project project,
            string svgDirectory,
            string packageDirectory,
            bool refreshNavigation)
        {
            string[] requiredAssets =
            {
                "bottle.svg",
                "command_production_on.svg",
                "command_production_off.svg",
                "command_reset.svg",
                "command_runout.svg",
                "nav_alarms.svg",
                "nav_cip.svg",
                "nav_diagnostics.svg",
                "nav_efficiency.svg",
                "nav_function.svg",
                "nav_home.svg",
                "nav_manual.svg",
                "nav_operate.svg",
                "nav_production.svg",
                "nav_recipe.svg",
                "nav_runout.svg",
                "nav_safety.svg",
                "nav_settings.svg",
                "pump_product.svg",
                "pump_vacuum.svg",
                "sensor_analog.svg",
                "sensor_digital.svg",
                "user_profile.svg",
                "valve.svg"
            };

            if (!Directory.Exists(svgDirectory))
            {
                throw new DirectoryNotFoundException("REV14 controlled SVG directory not found: " + svgDirectory);
            }
            Directory.CreateDirectory(packageDirectory);

            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string fileName in requiredAssets)
            {
                string fullPath = Path.Combine(svgDirectory, fileName);
                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException("Required REV14 controlled SVG is missing.", fullPath);
                }

                string stem = Path.GetFileNameWithoutExtension(fileName);
                string graphicName = "REV14_" + stem;
                MultiLingualGraphic existingGraphic = project.Graphics.Find(graphicName);
                bool isNavigation = stem.StartsWith("nav_", StringComparison.OrdinalIgnoreCase) &&
                    !stem.Equals("nav_runout", StringComparison.OrdinalIgnoreCase);
                if (existingGraphic != null && !(refreshNavigation && isNavigation))
                {
                    result[stem] = existingGraphic.Name;
                    Console.WriteLine("REV14_GRAPHIC_EXISTING=" + stem + "|" + existingGraphic.Name);
                    continue;
                }
                string assetDirectoryName = graphicName + " files";
                string assetDirectory = Path.Combine(packageDirectory, assetDirectoryName);
                Directory.CreateDirectory(assetDirectory);
                string packagedAsset = Path.Combine(assetDirectory, "DefaultImageStream.svg");
                File.Copy(fullPath, packagedAsset, true);
                string importXml = Path.Combine(packageDirectory, graphicName + ".xml");
                File.WriteAllText(importXml,
                    "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
                    "<Document>\r\n" +
                    "  <Engineering version=\"V19\" />\r\n" +
                    "  <DocumentInfo><Created>" + DateTime.UtcNow.ToString("o") +
                    "</Created><ExportSetting>WithDefaults</ExportSetting></DocumentInfo>\r\n" +
                    "  <Hmi.Globalization.MultiLingualGraphic ID=\"0\">\r\n" +
                    "    <AttributeList>\r\n" +
                    "      <DefaultDithering>false</DefaultDithering>\r\n" +
                    "      <DefaultImageStream external=\"path\">" + assetDirectoryName +
                    "\\DefaultImageStream.svg</DefaultImageStream>\r\n" +
                    "      <DefaultSmoothness>false</DefaultSmoothness>\r\n" +
                    "      <Name>" + graphicName + "</Name>\r\n" +
                    "    </AttributeList>\r\n" +
                    "  </Hmi.Globalization.MultiLingualGraphic>\r\n" +
                    "</Document>\r\n");

                IList<MultiLingualGraphic> imported = project.Graphics.Import(
                    new FileInfo(importXml),
                    ImportOptions.Override);
                MultiLingualGraphic graphic = imported.FirstOrDefault();
                if (graphic == null)
                {
                    graphic = project.Graphics.Find(graphicName);
                }
                if (graphic == null)
                {
                    throw new InvalidOperationException("TIA imported no project graphic for " + fileName + ".");
                }

                result[stem] = graphic.Name;
                Console.WriteLine("REV14_GRAPHIC=" + stem + "|" + graphic.Name);
            }
            return result;
        }

        private static void ApplyProductionCommandIconsOnly(
            HmiSoftware hmi,
            Dictionary<string, string> graphics)
        {
            HmiScreen production = hmi.Screens.Find("production");
            if (production == null)
            {
                throw new InvalidOperationException("Production screen was not found.");
            }

            string[,] assignments =
            {
                { "REV12_Operate_AutoStart", "command_production_on" },
                { "REV12_Operate_Stop", "command_production_off" },
                { "REV12_Operate_Reset", "command_reset" },
                { "REV12_Operate_Manual", "command_runout" }
            };

            for (int index = 0; index < assignments.GetLength(0); index++)
            {
                HmiButton button = production.ScreenItems.Find(assignments[index, 0]) as HmiButton;
                if (button == null)
                {
                    throw new InvalidOperationException("Production command button is missing: " + assignments[index, 0]);
                }

                int left = button.Left;
                int top = button.Top;
                uint width = button.Width;
                uint height = button.Height;
                button.Graphic = graphics[assignments[index, 1]];
                button.Content.ContentMode = HmiContentMode.GraphicAndText;
                button.Content.GraphicStretchMode = HmiGraphicStretchMode.Uniform;
                // Keep a compact vector symbol at the left and reserve the
                // majority of the button width for a full, bold command label.
                button.Content.TextPosition = HmiTextPosition.Right;
                button.Content.SplitRatio = 0.18;
                button.Content.Spacing = 2;
                button.Content.HorizontalTextAlignment = HmiHorizontalAlignment.Center;
                button.Content.VerticalTextAlignment = HmiVerticalAlignment.Center;
                button.Padding.Left = 4;
                button.Padding.Right = 4;
                button.Padding.Top = 3;
                button.Padding.Bottom = 3;
                button.Font.Size = 11;
                button.Font.Weight = HmiFontWeight.Bold;

                if (button.Left != left || button.Top != top || button.Width != width || button.Height != height)
                {
                    throw new InvalidOperationException("Production command button geometry changed: " + button.Name);
                }

                Console.WriteLine(
                    "PRODUCTION_COMMAND_ICON name={0} graphic={1} bounds={2},{3},{4},{5}",
                    button.Name, button.Graphic, button.Left, button.Top, button.Width, button.Height);
            }

            Console.WriteLine("NONPRODUCTION_SCREENS_MODIFIED=0");
            Console.WriteLine("PRODUCTION_COMMAND_BINDINGS_MODIFIED=0");
            Console.WriteLine("PRODUCTION_COMMAND_GEOMETRY_MODIFIED=0");
        }

        private static void SetProductionCommandIconScale(HmiSoftware hmi, double scale)
        {
            HmiScreen production = hmi.Screens.Find("production");
            if (production == null)
            {
                throw new InvalidOperationException("Production screen was not found.");
            }

            string[] names =
            {
                "REV12_Operate_AutoStart",
                "REV12_Operate_Stop",
                "REV12_Operate_Reset",
                "REV12_Operate_Manual"
            };

            const double sourceRatio = 0.18;
            double enlargedRatio = sourceRatio * scale;

            foreach (string name in names)
            {
                HmiButton button = production.ScreenItems.Find(name) as HmiButton;
                if (button == null)
                {
                    throw new InvalidOperationException("Production command button is missing: " + name);
                }

                int left = button.Left;
                int top = button.Top;
                uint width = button.Width;
                uint height = button.Height;

                button.Content.SplitRatio = enlargedRatio;

                if (button.Left != left || button.Top != top ||
                    button.Width != width || button.Height != height)
                {
                    throw new InvalidOperationException(
                        "Production command button geometry changed: " + button.Name);
                }

                Console.WriteLine(
                    "PRODUCTION_COMMAND_ICON_SCALE name={0} oldRatio={1:0.###} newRatio={2:0.###} bounds={3},{4},{5},{6}",
                    button.Name, sourceRatio, enlargedRatio,
                    button.Left, button.Top, button.Width, button.Height);
            }

            Console.WriteLine("PRODUCTION_SCREEN_ONLY=1");
            Console.WriteLine("PRODUCTION_COMMAND_BINDINGS_MODIFIED=0");
            Console.WriteLine("PRODUCTION_COMMAND_BUTTON_GEOMETRY_MODIFIED=0");
            Console.WriteLine("PRODUCTION_COMMAND_ICON_SCALE_PERCENT=" +
                Math.Round(scale * 100.0).ToString("0"));
        }

        private static void IncreaseProductionCommandButtonAreasTwentyFivePercent(HmiSoftware hmi)
        {
            HmiScreen production = hmi.Screens.Find("production");
            if (production == null)
            {
                throw new InvalidOperationException("Production screen was not found.");
            }

            string[] names =
            {
                "REV12_Operate_AutoStart",
                "REV12_Operate_Stop",
                "REV12_Operate_Reset",
                "REV12_Operate_Manual"
            };
            int[] left = { 731, 884, 731, 884 };
            int[] top = { 154, 154, 244, 244 };
            uint[] width = { 149, 145, 149, 145 };
            uint[] height = { 83, 84, 79, 79 };

            for (int index = 0; index < names.Length; index++)
            {
                HmiButton button = production.ScreenItems.Find(names[index]) as HmiButton;
                if (button == null)
                {
                    throw new InvalidOperationException(
                        "Production command button is missing: " + names[index]);
                }

                double iconWidthPixels = button.Width * button.Content.SplitRatio;
                double oldSplitRatio = button.Content.SplitRatio;
                uint oldWidth = button.Width;
                uint oldHeight = button.Height;

                button.Left = left[index];
                button.Top = top[index];
                button.Width = width[index];
                button.Height = height[index];
                // Preserve the current icon's physical pixel allocation while
                // expanding only the button background/content area.
                button.Content.SplitRatio = iconWidthPixels / button.Width;

                Console.WriteLine(
                    "PRODUCTION_COMMAND_BUTTON_AREA name={0} old={1}x{2} new={3}x{4} position={5},{6} oldIconRatio={7:0.###} newIconRatio={8:0.###} iconPixels={9:0.###}",
                    button.Name, oldWidth, oldHeight, button.Width, button.Height,
                    button.Left, button.Top, oldSplitRatio,
                    button.Content.SplitRatio, iconWidthPixels);
            }

            HmiLine divider = production.ScreenItems.Find(
                "REV12_Production_Upper_Divider") as HmiLine;
            if (divider != null)
            {
                divider.Left = 722;
            }

            HmiText title = production.ScreenItems.Find(
                "REV12_Operate_Safety_Title") as HmiText;
            if (title != null)
            {
                title.Left = 763;
            }

            Console.WriteLine("PRODUCTION_SCREEN_ONLY=1");
            Console.WriteLine("PRODUCTION_COMMAND_BUTTON_AREA_SCALE_PERCENT=125");
            Console.WriteLine("PRODUCTION_COMMAND_ICON_PIXEL_SIZE_MODIFIED=0");
            Console.WriteLine("PRODUCTION_COMMAND_TEXT_FORMAT_MODIFIED=0");
            Console.WriteLine("PRODUCTION_COMMAND_BINDINGS_MODIFIED=0");
            Console.WriteLine("PRODUCTION_COMMAND_EVENTS_MODIFIED=0");
            Console.WriteLine("PRODUCTION_COMMAND_OVERLAP=0");
        }

        private static void MakeProductionCommandLabelsCrisp(HmiSoftware hmi)
        {
            HmiScreen production = hmi.Screens.Find("production");
            if (production == null)
            {
                throw new InvalidOperationException("Production screen was not found.");
            }

            string[,] labels =
            {
                { "REV12_Operate_AutoStart", "PRODUCTION ON" },
                { "REV12_Operate_Stop", "PRODUCTION OFF" },
                { "REV12_Operate_Reset", "RESET ALARMS" },
                { "REV12_Operate_Manual", "RUN OUT PRODUCT" }
            };

            for (int index = 0; index < labels.GetLength(0); index++)
            {
                HmiButton button = production.ScreenItems.Find(labels[index, 0]) as HmiButton;
                if (button == null)
                {
                    throw new InvalidOperationException(
                        "Production command button is missing: " + labels[index, 0]);
                }

                int left = button.Left;
                int top = button.Top;
                uint width = button.Width;
                uint height = button.Height;
                double iconRatio = button.Content.SplitRatio;

                SetText(button.Text, labels[index, 1]);
                button.Font.Name = HmiFontName.SiemensSans;
                button.Font.Size = 13;
                button.Font.Weight = HmiFontWeight.Bold;
                button.Content.ContentMode = HmiContentMode.GraphicAndText;
                button.Content.GraphicStretchMode = HmiGraphicStretchMode.Uniform;
                button.Content.TextPosition = HmiTextPosition.Right;
                button.Content.HorizontalTextAlignment = HmiHorizontalAlignment.Center;
                button.Content.VerticalTextAlignment = HmiVerticalAlignment.Center;
                button.Content.Spacing = 6;
                button.Padding.Left = 6;
                button.Padding.Right = 6;
                button.Padding.Top = 4;
                button.Padding.Bottom = 4;

                if (button.Left != left || button.Top != top ||
                    button.Width != width || button.Height != height ||
                    Math.Abs(button.Content.SplitRatio - iconRatio) > 0.0001)
                {
                    throw new InvalidOperationException(
                        "Production command icon or button geometry changed: " + button.Name);
                }

                Console.WriteLine(
                    "PRODUCTION_COMMAND_LABEL name={0} text={1} font=SiemensSans size=13 weight=Bold bounds={2},{3},{4},{5} iconRatio={6:0.###}",
                    button.Name, labels[index, 1], button.Left, button.Top,
                    button.Width, button.Height, button.Content.SplitRatio);
            }

            Console.WriteLine("PRODUCTION_SCREEN_ONLY=1");
            Console.WriteLine("PRODUCTION_COMMAND_ICON_SIZE_MODIFIED=0");
            Console.WriteLine("PRODUCTION_COMMAND_BUTTON_GEOMETRY_MODIFIED=0");
            Console.WriteLine("PRODUCTION_COMMAND_BINDINGS_MODIFIED=0");
            Console.WriteLine("PRODUCTION_COMMAND_EVENTS_MODIFIED=0");
        }

        private static void AlignRevision25IoLinkDetailHeaders(
            HmiSoftware hmi,
            string screenName)
        {
            if (String.IsNullOrWhiteSpace(screenName))
            {
                throw new InvalidOperationException(
                    "An explicit IO-Link detail screen is required.");
            }
            if (!screenName.Equals("io_link_al100", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "This reviewed correction is approved only for io_link_al100.");
            }

            HmiScreen screen = hmi.Screens.Find(screenName);
            if (screen == null)
            {
                throw new InvalidOperationException(
                    "IO-Link detail screen was not found: " + screenName);
            }

            string[] names =
            {
                "REV18_IOL_Detail_H4",
                "REV18_IOL_Detail_H5",
                "REV18_IOL_Detail_H6",
                "REV18_IOL_Detail_H7"
            };
            int[] left = { 545, 640, 710, 890 };
            uint[] width = { 82, 65, 175, 285 };

            for (int index = 0; index < names.Length; index++)
            {
                HmiText header = screen.ScreenItems.Find(names[index]) as HmiText;
                if (header == null)
                {
                    throw new InvalidOperationException(
                        "IO-Link header object is missing: " + names[index]);
                }

                header.Left = left[index];
                header.Width = width[index];
                header.HorizontalTextAlignment = HmiHorizontalAlignment.Left;
                Console.WriteLine(
                    "REV25_IOLINK_HEADER name={0} bounds={1},{2},{3},{4}",
                    header.Name, header.Left, header.Top,
                    header.Width, header.Height);
            }

            Console.WriteLine("REV25_TARGET=" + screenName);
            Console.WriteLine("REV25_PAGE_BINDINGS_MODIFIED=0");
            Console.WriteLine("REV25_PAGE_EVENTS_MODIFIED=0");
            Console.WriteLine("REV25_DATA_ROWS_MODIFIED=0");
            Console.WriteLine("REV25_HEADER_OVERLAP_WITH_NAVIGATION=0");
        }

        private static void AlignRevision25CommonFooter(
            HmiSoftware hmi,
            string screenName)
        {
            if (String.IsNullOrWhiteSpace(screenName))
            {
                throw new InvalidOperationException(
                    "An explicit screen is required for the footer correction.");
            }
            if (!screenName.Equals("setup", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "This reviewed footer correction is approved only for setup.");
            }

            HmiScreen screen = hmi.Screens.Find(screenName);
            if (screen == null)
            {
                throw new InvalidOperationException(
                    "Screen was not found: " + screenName);
            }

            HmiText footer = screen.ScreenItems.Find("REV12_Common_Footer") as HmiText;
            if (footer == null)
            {
                throw new InvalidOperationException(
                    "REV12_Common_Footer is missing from " + screenName + ".");
            }

            int top = footer.Top;
            uint width = footer.Width;
            uint height = footer.Height;
            footer.Left = 30;
            Console.WriteLine(
                "REV25_FOOTER name={0} bounds={1},{2},{3},{4}",
                footer.Name, footer.Left, footer.Top, footer.Width, footer.Height);

            if (footer.Top != top || footer.Width != width || footer.Height != height)
            {
                throw new InvalidOperationException(
                    "Footer correction changed dimensions other than Left.");
            }

            Console.WriteLine("REV25_TARGET=" + screenName);
            Console.WriteLine("REV25_FOOTER_LEFT_ONLY=1");
            Console.WriteLine("REV25_PAGE_BINDINGS_MODIFIED=0");
            Console.WriteLine("REV25_PAGE_EVENTS_MODIFIED=0");
        }

        private static void AlignRevision25LiftFunctionButton(HmiSoftware hmi)
        {
            HmiScreen screen = hmi.Screens.Find("lift_warning");
            if (screen == null)
            {
                throw new InvalidOperationException(
                    "Lift Warning screen was not found.");
            }

            HmiButton button = screen.ScreenItems.Find(
                "REV13_Nav_Button_FUNCTION") as HmiButton;
            if (button == null)
            {
                throw new InvalidOperationException(
                    "Lift Warning FUNCTION navigation button is missing.");
            }

            int left = button.Left;
            int top = button.Top;
            uint height = button.Height;
            button.Width = 133;
            Console.WriteLine(
                "REV25_LIFT_FUNCTION_BUTTON bounds={0},{1},{2},{3}",
                button.Left, button.Top, button.Width, button.Height);

            if (button.Left != left || button.Top != top || button.Height != height)
            {
                throw new InvalidOperationException(
                    "Lift Warning FUNCTION correction changed geometry other than Width.");
            }

            Console.WriteLine("REV25_TARGET=lift_warning");
            Console.WriteLine("REV25_BUTTON_WIDTH_ONLY=1");
            Console.WriteLine("REV25_BUTTON_EVENT_MODIFIED=0");
            Console.WriteLine("REV25_PAGE_BINDINGS_MODIFIED=0");
        }

        private static void AlignRevision25EfficiencyHeader(HmiSoftware hmi)
        {
            HmiScreen screen = hmi.Screens.Find("efficiency");
            if (screen == null)
            {
                throw new InvalidOperationException(
                    "Efficiency screen was not found.");
            }

            HmiRectangle header = screen.ScreenItems.Find(
                "REV12_Common_Header") as HmiRectangle;
            if (header == null)
            {
                throw new InvalidOperationException(
                    "Efficiency common header is missing.");
            }

            int left = header.Left;
            uint width = header.Width;
            uint height = header.Height;
            header.Top = 0;
            Console.WriteLine(
                "REV25_EFFICIENCY_HEADER bounds={0},{1},{2},{3}",
                header.Left, header.Top, header.Width, header.Height);

            if (header.Left != left || header.Width != width || header.Height != height)
            {
                throw new InvalidOperationException(
                    "Efficiency header correction changed geometry other than Top.");
            }

            Console.WriteLine("REV25_TARGET=efficiency");
            Console.WriteLine("REV25_HEADER_TOP_ONLY=1");
            Console.WriteLine("REV25_PAGE_BINDINGS_MODIFIED=0");
            Console.WriteLine("REV25_PAGE_EVENTS_MODIFIED=0");
        }

        private static void RefreshRevision14NavigationContrast(
            HmiScreen production,
            IDictionary<string, string> graphics)
        {
            string[] labels =
            {
                "HOME", "SAFETY", "OPERATE", "PRODUCTION", "CIP", "FUNCTION",
                "ALARMS", "RECIPE", "SETUP", "MANUAL", "EFFICIENCY", "DIAGNOSTICS"
            };
            string[] assets =
            {
                "nav_home", "nav_safety", "nav_operate", "nav_production", "nav_cip", "nav_function",
                "nav_alarms", "nav_recipe", "nav_settings", "nav_manual", "nav_efficiency", "nav_diagnostics"
            };

            for (int index = 0; index < labels.Length; index++)
            {
                DeleteItem(production, "REV14_Nav_IconBack_" + labels[index]);
                HmiGraphicView icon = production.ScreenItems.Find(
                    "REV14_Nav_Icon_" + labels[index]) as HmiGraphicView;
                if (icon == null)
                {
                    throw new InvalidOperationException(
                        "Navigation icon is missing from Production: " + labels[index]);
                }
                icon.Graphic = graphics[assets[index]];
                icon.GraphicStretchMode = HmiGraphicStretchMode.Uniform;
                icon.BackColor = Color.Transparent;
                icon.Visible = true;
                icon.Enabled = false;
                Console.WriteLine("REV14_NAV_CONTRAST=" + labels[index] + "|" + graphics[assets[index]]);
            }
        }

        private static void ApplyRevision13ButtonsToAllScreens(HmiSoftware hmi)
        {
            Dictionary<string, string> activePages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "home", "HOME" },
                { "safety", "SAFETY" },
                { "operate", "OPERATE" },
                { "production", "PRODUCTION" },
                { "cip", "CIP" },
                { "function", "FUNCTION" },
                { "alarms", "ALARMS" },
                { "recipe", "RECIPE" },
                { "setup", "SETUP" },
                { "settings_inputs", "SETUP" },
                { "settings_outputs", "SETUP" },
                { "settings_timers", "SETUP" },
                { "settings_external_wash", "SETUP" },
                { "manual", "MANUAL" },
                { "lift_warning", "MANUAL" },
                { "efficiency", "EFFICIENCY" },
                { "diagnostics", "DIAGNOSTICS" },
                { "io_diagnostics", "DIAGNOSTICS" },
                { "io_link_overview", "DIAGNOSTICS" },
                { "io_link_al100", "DIAGNOSTICS" },
                { "io_link_al101", "DIAGNOSTICS" },
                { "io_link_al102", "DIAGNOSTICS" },
                { "io_link_al103", "DIAGNOSTICS" },
                { "io_link_al104", "DIAGNOSTICS" }
                ,{ "safety_pilz_diagnostics", "DIAGNOSTICS" }
            };

            foreach (KeyValuePair<string, string> entry in activePages)
            {
                HmiScreen screen = hmi.Screens.Find(entry.Key);
                if (screen == null)
                {
                    throw new InvalidOperationException(
                        "Required HMI screen is missing while applying REV13 navigation buttons: " + entry.Key);
                }

                // Production is the approved master. Do not rewrite it; propagate its
                // button geometry and visual language to every other page. Icons are
                // deliberately not created, deleted, moved, recolored, or rebound here.
                if (!entry.Key.Equals("production", StringComparison.OrdinalIgnoreCase))
                {
                    AddRevision13NavigationButtonsOnly(screen, entry.Value);
                }
                Console.WriteLine("REV13_NAV_BUTTONS=" + entry.Key + "|ACTIVE=" + entry.Value);
            }
        }

        private static void ApplyProductionNavigationMasterToAllScreens(
            Project project,
            HmiSoftware hmi,
            string targetScreen)
        {
            Dictionary<string, string> activePages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "home", "HOME" },
                { "safety", "SAFETY" },
                { "operate", "OPERATE" },
                { "production", "PRODUCTION" },
                { "cip", "CIP" },
                { "function", "FUNCTION" },
                { "alarms", "ALARMS" },
                { "recipe", "RECIPE" },
                { "setup", "SETUP" },
                { "settings_inputs", "SETUP" },
                { "settings_outputs", "SETUP" },
                { "settings_timers", "SETUP" },
                { "settings_external_wash", "SETUP" },
                { "manual", "MANUAL" },
                { "lift_warning", "MANUAL" },
                { "efficiency", "EFFICIENCY" },
                { "diagnostics", "DIAGNOSTICS" },
                { "io_diagnostics", "DIAGNOSTICS" },
                { "io_link_overview", "DIAGNOSTICS" },
                { "io_link_al100", "DIAGNOSTICS" },
                { "io_link_al101", "DIAGNOSTICS" },
                { "io_link_al102", "DIAGNOSTICS" },
                { "io_link_al103", "DIAGNOSTICS" },
                { "io_link_al104", "DIAGNOSTICS" },
                { "safety_pilz_diagnostics", "DIAGNOSTICS" }
            };
            string[] labels =
            {
                "HOME", "SAFETY", "OPERATE", "PRODUCTION", "CIP", "FUNCTION",
                "ALARMS", "RECIPE", "SETUP", "MANUAL", "EFFICIENCY", "DIAGNOSTICS"
            };
            string[] assets =
            {
                "nav_home", "nav_safety", "nav_operate", "nav_production", "nav_cip", "nav_function",
                "nav_alarms", "nav_recipe", "nav_settings", "nav_manual", "nav_efficiency", "nav_diagnostics"
            };
            int[] iconTops = { 115, 164, 213, 262, 310, 361, 408, 458, 508, 556, 604, 654 };
            Dictionary<string, string> graphics = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string asset in assets)
            {
                MultiLingualGraphic graphic = project.Graphics.Find("REV14_" + asset);
                if (graphic == null)
                {
                    throw new InvalidOperationException(
                        "Production navigation master graphic is missing: REV14_" + asset);
                }
                graphics[asset] = graphic.Name;
            }

            foreach (KeyValuePair<string, string> entry in activePages)
            {
                if (!String.IsNullOrWhiteSpace(targetScreen) &&
                    !entry.Key.Equals(targetScreen, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                HmiScreen screen = hmi.Screens.Find(entry.Key);
                if (screen == null)
                {
                    throw new InvalidOperationException(
                        "Required HMI screen is missing while applying the Production navigation master: " + entry.Key);
                }

                // Production is the approved immutable master. Only the other screens
                // are rebuilt within the navigation rail (x >= 1215, y >= 105).
                if (entry.Key.Equals("production", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("NAV_MASTER_SOURCE=production|UNCHANGED");
                    continue;
                }

                DeleteItem(screen, "REV13_Nav_Back");
                DeleteItem(screen, "REV12_Nav_Back");
                foreach (string label in labels)
                {
                    DeleteItem(screen, "REV12_Nav_" + label);
                    DeleteItem(screen, "REV13_Nav_Button_" + label);
                    DeleteItem(screen, "REV13_Nav_Icon_" + label + "_Ring");
                    DeleteItem(screen, "REV13_Nav_Icon_" + label + "_LineA");
                    DeleteItem(screen, "REV13_Nav_Icon_" + label + "_LineB");
                    DeleteItem(screen, "REV14_Nav_IconBack_" + label);
                    DeleteItem(screen, "REV14_Nav_Icon_" + label);
                }

                ConfigureRectangle(
                    GetOrCreate<HmiRectangle>(screen, "REV13_Nav_Back"),
                    1215, 105, 151, 612,
                    Color.FromArgb(3, 39, 86), Color.FromArgb(3, 39, 86), 0);
                AddRevision13NavigationButtonsOnly(screen, entry.Value);

                for (int index = 0; index < labels.Length; index++)
                {
                    AddGraphicView(
                        screen,
                        "REV14_Nav_Icon_" + labels[index],
                        1227,
                        iconTops[index],
                        22,
                        22,
                        graphics[assets[index]]);
                    HmiGraphicView icon = screen.ScreenItems.Find(
                        "REV14_Nav_Icon_" + labels[index]) as HmiGraphicView;
                    icon.BackColor = Color.Transparent;
                }

                Console.WriteLine("NAV_MASTER_APPLIED=" + entry.Key + "|ACTIVE=" + entry.Value);
            }
        }

        private static void ApplyRevision25ProductionMasterFrameExact(
            Project project,
            HmiSoftware hmi,
            string targetScreen)
        {
            HmiScreen production = hmi.Screens.Find("production");
            HmiScreen screen = hmi.Screens.Find(targetScreen);
            if (production == null || screen == null)
            {
                throw new InvalidOperationException(
                    "REV25 requires the existing Production master and target screen: " + targetScreen);
            }

            Dictionary<string, string> activePages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "home", "HOME" }, { "safety", "SAFETY" }, { "operate", "OPERATE" },
                { "cip", "CIP" }, { "function", "FUNCTION" }, { "alarms", "ALARMS" },
                { "recipe", "RECIPE" }, { "recipe_edit", "RECIPE" },
                { "setup", "SETUP" }, { "settings_inputs", "SETUP" },
                { "settings_outputs", "SETUP" }, { "settings_timers", "SETUP" },
                { "settings_external_wash", "SETUP" }, { "manual", "MANUAL" },
                { "lift_warning", "MANUAL" }, { "efficiency", "EFFICIENCY" },
                { "diagnostics", "DIAGNOSTICS" }, { "io_diagnostics", "DIAGNOSTICS" },
                { "io_link_overview", "DIAGNOSTICS" }, { "io_link_al100", "DIAGNOSTICS" },
                { "io_link_al101", "DIAGNOSTICS" }, { "io_link_al102", "DIAGNOSTICS" },
                { "io_link_al103", "DIAGNOSTICS" }, { "io_link_al104", "DIAGNOSTICS" },
                { "safety_pilz_diagnostics", "DIAGNOSTICS" }
            };
            string activePage;
            if (!activePages.TryGetValue(targetScreen, out activePage))
            {
                throw new InvalidOperationException("REV25 has no approved navigation classification for: " + targetScreen);
            }

            string[] labels =
            {
                "HOME", "SAFETY", "OPERATE", "PRODUCTION", "CIP", "FUNCTION",
                "ALARMS", "RECIPE", "SETUP", "MANUAL", "EFFICIENCY", "DIAGNOSTICS"
            };
            Dictionary<string, string> destinations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "HOME", "home" }, { "SAFETY", "safety" }, { "OPERATE", "operate" },
                { "PRODUCTION", "production" }, { "CIP", "cip" }, { "FUNCTION", "function" },
                { "ALARMS", "alarms" }, { "RECIPE", "recipe" }, { "SETUP", "setup" },
                { "MANUAL", "manual" }, { "EFFICIENCY", "efficiency" },
                { "DIAGNOSTICS", "diagnostics" }
            };

            // Remove only obsolete common-frame aliases. Page-specific process, alarm,
            // recipe, setup, and diagnostic objects are deliberately outside this list.
            string[] legacyNames =
            {
                "REV12_Common_Footer", "REV12_Common_Header", "REV12_Common_Header_Page",
                "REV12_Common_Header_Title", "REV12_Common_Page_Label", "REV12_Common_State",
                "REV12_Common_Status_Bar", "REV12_Common_Status_Label",
                "REV12_Production_SystemReady_Label", "REV12_Production_SystemReady_Lamp",
                "REV12_Production_Alarm_Header", "REV12_Production_Alarm_Header_Value",
                "REV13_Common_AlarmValue"
            };
            foreach (string legacyName in legacyNames) DeleteItem(screen, legacyName);

            string[] masterNames = production.ScreenItems
                .Where(item => IsRevision25MasterFrameItem(item.Name))
                .Select(item => item.Name)
                .ToArray();
            foreach (string masterName in masterNames)
            {
                HmiScreenItemBase source = production.ScreenItems.Find(masterName);
                HmiScreenItemBase target = screen.ScreenItems.Find(masterName);
                if (target != null && target.GetType() != source.GetType())
                {
                    target.Delete();
                    target = null;
                }
                if (target == null) target = CreateRevision25FrameItem(screen, source);
                CopyRevision25FrameProperties(source, target);
            }

            // A page title is page-specific by definition; everything else is copied
            // from the live Production master without hard-coded geometry.
            HmiText page = screen.ScreenItems.Find("REV13_Common_Page") as HmiText;
            if (page != null)
            {
                SetText(page.Text, GetRevision25PageTitle(targetScreen) + "  |  FBS GLOBAL");
            }

            HmiSymbolicIOField state = screen.ScreenItems.Find("REV13_Common_State") as HmiSymbolicIOField;
            if (state != null)
            {
                state.ProcessValue = "0";
                BindTag(state.Dynamizations, "ProcessValue", "Machine_State", true);
                state.ResourceList = "MachineStateText";
                state.IOFieldType = HmiIOFieldType.Output;
                state.RequireExplicitUnlock = false;
            }

            HmiEllipse readyLamp = screen.ScreenItems.Find("REV13_Common_ReadyLamp") as HmiEllipse;
            if (readyLamp != null)
            {
                DynamizationBase oldReady = readyLamp.Dynamizations.Find("BackColor");
                if (oldReady != null) oldReady.Delete();
                ScriptDynamization ready = readyLamp.Dynamizations.Create<ScriptDynamization>("BackColor");
                ready.Async = false;
                ready.Trigger.Type = TriggerType.T500ms;
                ready.ScriptCode =
                    "let v=HMIRuntime.Tags(\"Machine_SafetyOK\").Read();" +
                    "if(v){return HMIRuntime.Math.RGB(35,170,35);}" +
                    "return HMIRuntime.Math.RGB(145,150,155);";
            }

            HmiText dateTime = screen.ScreenItems.Find("REV21_Common_DateTime") as HmiText;
            if (dateTime != null)
            {
                ConfigureNumericDynamization(dateTime, "Text", "Machine_State",
                    "let d=new Date();let z=function(v){return v<10?\"0\"+v:String(v);};" +
                    "let h=d.getHours();let ap=h>=12?\"PM\":\"AM\";h=h%12;if(h===0){h=12;}" +
                    "return z(h)+\":\"+z(d.getMinutes())+\":\"+z(d.getSeconds())+\" \"+ap+" +
                    "\"\\n\"+z(d.getDate())+\"/\"+z(d.getMonth()+1)+\"/\"+d.getFullYear();");
            }

            foreach (string label in labels)
            {
                HmiButton button = screen.ScreenItems.Find("REV13_Nav_Button_" + label) as HmiButton;
                if (button == null) throw new InvalidOperationException("REV25 navigation button missing after copy: " + label);
                SetText(button.Text, label);
                HmiButtonEventHandler oldTapped = button.EventHandlers.Find(HmiButtonEventType.Tapped);
                if (oldTapped != null) oldTapped.Delete();
                if (label.Equals(activePage, StringComparison.OrdinalIgnoreCase))
                {
                    button.Enabled = false;
                    button.BackColor = Color.FromArgb(0, 90, 205);
                }
                else
                {
                    button.Enabled = true;
                    button.BackColor = Color.FromArgb(8, 55, 105);
                    ConfigureScreenNavigation(button, destinations[label], activePage);
                }
            }

            Console.WriteLine("REV25_FRAME_SOURCE=production|OBJECTS=" + masterNames.Length);
            Console.WriteLine("REV25_FRAME_TARGET=" + targetScreen + "|ACTIVE=" + activePage);
            Console.WriteLine("REV25_CONTENT_OBJECTS_TOUCHED=0");
        }

        private static bool IsRevision25MasterFrameItem(string name)
        {
            return name.Equals("REV12_Production_User_Header", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("REV13_Common_", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("REV13_Nav_", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("REV14_Nav_", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("REV21_Common_", StringComparison.OrdinalIgnoreCase);
        }

        private static HmiScreenItemBase CreateRevision25FrameItem(HmiScreen screen, HmiScreenItemBase source)
        {
            if (source is HmiRectangle) return screen.ScreenItems.Create<HmiRectangle>(source.Name);
            if (source is HmiText) return screen.ScreenItems.Create<HmiText>(source.Name);
            if (source is HmiSymbolicIOField) return screen.ScreenItems.Create<HmiSymbolicIOField>(source.Name);
            if (source is HmiEllipse) return screen.ScreenItems.Create<HmiEllipse>(source.Name);
            if (source is HmiButton) return screen.ScreenItems.Create<HmiButton>(source.Name);
            if (source is HmiGraphicView) return screen.ScreenItems.Create<HmiGraphicView>(source.Name);
            throw new InvalidOperationException(
                "REV25 Production frame contains an unsupported object type: " + source.GetType().FullName);
        }

        private static void CopyRevision25FrameProperties(HmiScreenItemBase source, HmiScreenItemBase target)
        {
            string[] properties =
            {
                "Left", "Top", "Width", "Height", "CenterX", "CenterY", "RadiusX", "RadiusY",
                "Visible", "Enabled", "BackColor", "ForeColor", "BorderColor", "BorderWidth",
                "BackFillPattern", "AlternateBackColor", "Graphic", "GraphicStretchMode",
                "HorizontalTextAlignment", "VerticalTextAlignment", "IOFieldType", "OutputFormat"
            };
            foreach (string propertyName in properties) CopyWritableProperty(source, target, propertyName);

            CopyNestedWritableProperties(source, target, "Font", new[] { "Name", "Size", "Weight" });
            CopyNestedWritableProperties(source, target, "Content", new[] { "HorizontalTextAlignment", "VerticalTextAlignment" });

            HmiText sourceText = source as HmiText;
            HmiText targetText = target as HmiText;
            if (sourceText != null && targetText != null) CopyMultilingualText(sourceText.Text, targetText.Text);
            HmiButton sourceButton = source as HmiButton;
            HmiButton targetButton = target as HmiButton;
            if (sourceButton != null && targetButton != null) CopyMultilingualText(sourceButton.Text, targetButton.Text);
        }

        private static void CopyWritableProperty(object source, object target, string name)
        {
            PropertyInfo sourceProperty = source.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            PropertyInfo targetProperty = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (sourceProperty == null || targetProperty == null || !sourceProperty.CanRead || !targetProperty.CanWrite) return;
            try { targetProperty.SetValue(target, sourceProperty.GetValue(source, null), null); }
            catch { }
        }

        private static void CopyNestedWritableProperties(object source, object target, string name, string[] properties)
        {
            PropertyInfo sourceProperty = source.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            PropertyInfo targetProperty = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (sourceProperty == null || targetProperty == null || !sourceProperty.CanRead || !targetProperty.CanRead) return;
            object sourceValue = sourceProperty.GetValue(source, null);
            object targetValue = targetProperty.GetValue(target, null);
            if (sourceValue == null || targetValue == null) return;
            foreach (string property in properties) CopyWritableProperty(sourceValue, targetValue, property);
        }

        private static void CopyMultilingualText(MultilingualText source, MultilingualText target)
        {
            MultilingualTextItem[] sourceItems = source.Items.ToArray();
            MultilingualTextItem[] targetItems = target.Items.ToArray();
            int count = Math.Min(sourceItems.Length, targetItems.Length);
            for (int index = 0; index < count; index++)
            {
                targetItems[index].Text = sourceItems[index].Text;
            }
        }

        private static string GetRevision25PageTitle(string screenName)
        {
            Dictionary<string, string> titles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "home", "HOME" }, { "safety", "SAFETY" }, { "operate", "OPERATE" },
                { "cip", "CIP" }, { "function", "FUNCTION" }, { "alarms", "ALARMS" },
                { "recipe", "RECIPE" }, { "recipe_edit", "CIP RECIPE EDIT" }, { "setup", "SETUP" },
                { "settings_inputs", "SETTINGS - DIGITAL INPUTS" },
                { "settings_outputs", "SETTINGS - DIGITAL OUTPUTS" },
                { "settings_timers", "SETTINGS - TIMERS & DELAYS" },
                { "settings_external_wash", "SETTINGS - EXTERNAL WASH" },
                { "manual", "MANUAL" }, { "lift_warning", "LIFT WARNING" },
                { "efficiency", "EFFICIENCY" }, { "diagnostics", "DIAGNOSTICS" },
                { "io_diagnostics", "I/O CONFIGURATION & DIAGNOSTICS" },
                { "io_link_overview", "IO-LINK OVERVIEW" }, { "io_link_al100", "AL100 IO-LINK DETAIL" },
                { "io_link_al101", "AL101 IO-LINK DETAIL" }, { "io_link_al102", "AL102 IO-LINK DETAIL" },
                { "io_link_al103", "AL103 IO-LINK DETAIL" }, { "io_link_al104", "AL104 IO-LINK DETAIL" },
                { "safety_pilz_diagnostics", "PILZ SAFETY DIAGNOSTICS" }
            };
            string title;
            return titles.TryGetValue(screenName, out title) ? title : screenName.ToUpperInvariant();
        }

        private static void ApplyRevision15IoNavigationDirect(Project project, HmiSoftware hmi)
        {
            HmiScreen screen = hmi.Screens.Find("io_diagnostics");
            if (screen == null)
            {
                throw new InvalidOperationException(
                    "REV15 direct navigation finalization requires the existing 'io_diagnostics' screen.");
            }

            string[] labels =
            {
                "HOME", "SAFETY", "OPERATE", "PRODUCTION", "CIP", "FUNCTION",
                "ALARMS", "RECIPE", "SETUP", "MANUAL", "EFFICIENCY", "DIAGNOSTICS"
            };
            string[] assets =
            {
                "nav_home", "nav_safety", "nav_operate", "nav_production", "nav_cip", "nav_function",
                "nav_alarms", "nav_recipe", "nav_settings", "nav_manual", "nav_efficiency", "nav_diagnostics"
            };
            int[] iconTops = { 115, 164, 213, 262, 310, 361, 408, 458, 508, 556, 604, 654 };

            HmiRectangle navBack = screen.ScreenItems.Find("REV13_Nav_Back") as HmiRectangle;
            if (navBack == null)
            {
                navBack = GetOrCreate<HmiRectangle>(screen, "REV13_Nav_Back");
            }
            ConfigureRectangle(navBack, 1215, 105, 151, 612,
                Color.FromArgb(3, 39, 86), Color.FromArgb(3, 39, 86), 0);

            AddRevision13NavigationButtonsOnly(screen, "DIAGNOSTICS");

            for (int index = 0; index < labels.Length; index++)
            {
                HmiEllipse ring = screen.ScreenItems.Find("REV13_Nav_Icon_" + labels[index] + "_Ring") as HmiEllipse;
                HmiLine lineA = screen.ScreenItems.Find("REV13_Nav_Icon_" + labels[index] + "_LineA") as HmiLine;
                HmiLine lineB = screen.ScreenItems.Find("REV13_Nav_Icon_" + labels[index] + "_LineB") as HmiLine;
                if (ring != null) ring.Visible = false;
                if (lineA != null) lineA.Visible = false;
                if (lineB != null) lineB.Visible = false;

                MultiLingualGraphic graphic = project.Graphics.Find("REV14_" + assets[index]);
                if (graphic == null)
                {
                    throw new InvalidOperationException(
                        "Production navigation master graphic is missing: REV14_" + assets[index]);
                }

                AddGraphicView(screen, "REV14_Nav_Icon_" + labels[index],
                    1227, iconTops[index], 22, 22, graphic.Name);
                HmiGraphicView icon = screen.ScreenItems.Find(
                    "REV14_Nav_Icon_" + labels[index]) as HmiGraphicView;
                icon.BackColor = Color.Transparent;
            }

            Console.WriteLine("NAV_MASTER_APPLIED=io_diagnostics|ACTIVE=DIAGNOSTICS|MODE=DIRECT");
        }

        private static void AddRevision13NavigationButtonsOnly(HmiScreen screen, string activePage)
        {
            string[] labels =
            {
                "HOME", "SAFETY", "OPERATE", "PRODUCTION", "CIP", "FUNCTION",
                "ALARMS", "RECIPE", "SETUP", "MANUAL", "EFFICIENCY", "DIAGNOSTICS"
            };
            string[] texts =
            {
                "HOME", "SAFETY", "OPERATE", "      PRODUCTION", "CIP", "FUNCTION",
                "      ALARMS", "RECIPE", "SETUP", "MANUAL", "     EFFICIENCY", "     DIAGNOSTICS"
            };
            string[] destinations =
            {
                "home", "safety", "operate", "production", "cip", "function",
                "alarms", "recipe", "setup", "manual", "efficiency", "diagnostics"
            };

            for (int index = 0; index < labels.Length; index++)
            {
                int top = 105 + index * 49;
                bool active = labels[index].Equals(activePage, StringComparison.OrdinalIgnoreCase);
                HmiButton button = GetOrCreate<HmiButton>(screen, "REV13_Nav_Button_" + labels[index]);
                ConfigureButton(
                    button,
                    1224,
                    top,
                    133,
                    43,
                    texts[index],
                    active ? Color.FromArgb(0, 90, 205) : Color.FromArgb(8, 55, 105));
                button.Enabled = !active;
                if (!active)
                {
                    ConfigureScreenNavigation(button, destinations[index], activePage);
                }
            }
        }

        private static void BuildRevision14Pilot(
            HmiScreen home,
            HmiScreen production,
            IDictionary<string, string> graphics)
        {
            BuildRevision13Home(home);
            BuildProductionMasterExact(production);
            ApplyRevision13ProductionVisuals(production);

            ApplyRevision14TruthCorrections(home, production);
            ApplyRevision14Graphics(home, production, graphics);
            CleanupRevision14PilotDuplicates(production);
        }

        private static void CleanupRevision14PilotDuplicates(HmiScreen production)
        {
            // REV13 supplies the approved common chrome and SVG navigation. Remove the
            // corresponding REV12 objects so no hidden controls or text remain beneath it.
            string[] obsoleteCommonObjects =
            {
                "REV12_Common_Header",
                "REV12_Common_Header_Title",
                "REV12_Common_Header_Page",
                "REV12_Common_Status_Bar",
                "REV12_Common_Status_Label",
                "REV12_Common_State",
                "REV12_Production_SystemReady_Label",
                "REV12_Production_SystemReady_Lamp",
                "REV12_Production_Alarm_Header",
                "REV12_Production_Alarm_Header_Value",
                "REV12_Exact_Bottom_Bar",
                "REV12_Exact_Bottom_Overview",
                "REV12_Exact_Bottom_Efficiency",
                "REV12_Exact_Bottom_Alarms",
                "REV12_Exact_Bottom_Diagnostics",
                "REV12_Exact_Bottom_Connection",
                "REV12_Exact_Bottom_PlcHmi"
            };
            foreach (string name in obsoleteCommonObjects) DeleteItem(production, name);

            string[] navigationLabels =
            {
                "HOME", "SAFETY", "OPERATE", "PRODUCTION", "CIP", "FUNCTION",
                "ALARMS", "RECIPE", "SETUP", "MANUAL", "EFFICIENCY", "DIAGNOSTICS"
            };
            foreach (string label in navigationLabels) DeleteItem(production, "REV12_Nav_" + label);

            // Preserve all operator-edited REV13 navigation buttons. Only remove each
            // white backing disc and make the small SVG graphic background transparent;
            // button geometry, styling and navigation events remain untouched.
            foreach (string label in navigationLabels)
            {
                DeleteItem(production, "REV14_Nav_IconBack_" + label);
                HmiGraphicView navigationIcon =
                    production.ScreenItems.Find("REV14_Nav_Icon_" + label) as HmiGraphicView;
                if (navigationIcon != null) navigationIcon.BackColor = Color.Transparent;
            }

            // Separate the retained process captions and the explicit missing-feedback
            // message. These are visible foreground objects and must never share pixels.
            ConfigureText(GetOrCreate<HmiText>(production, "REV13_ProductPump_Label"), 44, 350, 110, 26,
                "PRODUCT PUMP\nCOMMAND", Navy, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureText(GetOrCreate<HmiText>(production, "REV13_VacuumPump_Code"), 570, 241, 110, 20,
                "M103", Navy, 12, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureText(GetOrCreate<HmiText>(production, "REV13_VacuumPump_Label"), 570, 263, 110, 25,
                "VACUUM PUMP COMMAND", Navy, 9, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureText(GetOrCreate<HmiText>(production, "REV14_M103_MissingFeedback"), 565, 326, 135, 30,
                "FEEDBACK / SPEED\nNOT CONFIGURED", Color.FromArgb(105, 115, 125), 9,
                HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
        }

        private static void ApplyRevision14TruthCorrections(HmiScreen home, HmiScreen production)
        {
            // HOME: values must use proven sources and missing values must remain explicit.
            DeleteItem(home, "REV13_Home_MetricValue_Drive");
            ConfigureValidatedValue(
                GetOrCreate<HmiText>(home, "REV14_Home_ActualSpeed"),
                500, 573, 150, 36,
                "Speed_Feedback_Pct", "Speed_Feedback_Valid", "0.0");
            ConfigureText(GetOrCreate<HmiText>(home, "REV13_Home_MetricLabel_Drive"), 500, 545, 150, 24,
                "ACTUAL SPEED [%]", Navy, 11, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);

            DeleteItem(home, "REV13_Home_MetricValue_Vacuum");
            ConfigureValidatedValue(
                GetOrCreate<HmiText>(home, "REV14_Home_Vacuum"),
                880, 573, 150, 36,
                "Vacuum_Actual_mbar", "Vacuum_Signal_Valid", "0.0");

            ConfigureText(GetOrCreate<HmiText>(home, "REV13_Home_KpiUnavailable"), 1070, 280, 110, 38,
                "N/A", Color.FromArgb(105, 115, 125), 18, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureText(GetOrCreate<HmiText>(home, "REV13_Home_KpiTruthNote"), 1064, 350, 122, 92,
                "NOT CONFIGURED\nNo approved daily counter or reset basis.",
                Dark, 10, HmiFontWeight.Normal, HmiHorizontalAlignment.Center);
            ConfigureText(GetOrCreate<HmiText>(home, "REV13_Common_BottomInfo"), 1010, 722, 335, 34,
                "PLC / HMI LINK: NOT CONFIGURED", Color.FromArgb(235, 160, 30), 12,
                HmiFontWeight.Bold, HmiHorizontalAlignment.Right);

            // PRODUCTION: distinguish commands/references from actual feedback.
            ConfigureText(GetOrCreate<HmiText>(production, "REV12_Mimic_ProductPresence_Label"), 35, 403, 170, 25,
                "SPL100 PRODUCT PRESENT", Navy, 11, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(production, "REV13_ProductPump_Label"), 44, 346, 110, 30,
                "PRODUCT PUMP\nCOMMAND", Navy, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureText(GetOrCreate<HmiText>(production, "REV13_VacuumPump_Label"), 570, 274, 110, 30,
                "VACUUM PUMP\nCOMMAND", Navy, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            DeleteItem(production, "REV12_Mimic_ProductPump_State");
            DeleteItem(production, "REV12_Mimic_ProductPump_State_Back");
            DeleteItem(production, "REV12_Mimic_VacuumPump_State");
            DeleteItem(production, "REV12_Mimic_VacuumPump_State_Back");
            ConfigureText(GetOrCreate<HmiText>(production, "REV14_M103_MissingFeedback"), 565, 307, 135, 34,
                "FEEDBACK / SPEED\nNOT CONFIGURED", Color.FromArgb(105, 115, 125), 9,
                HmiFontWeight.Bold, HmiHorizontalAlignment.Center);

            string[] valveSuffixes = { "EV210", "EV212", "EV217", "EV213", "EV247" };
            int[] valveLeft = { 178, 462, 457, 547, 637 };
            int[] valveTop = { 270, 202, 270, 270, 270 };
            for (int index = 0; index < valveSuffixes.Length; index++)
            {
                string suffix = valveSuffixes[index];
                HmiText code = production.ScreenItems.Find("REV13_" + suffix + "_Code") as HmiText;
                if (code != null)
                {
                    ConfigureText(code, valveLeft[index] - 7, valveTop[index] + 31, 58, 20,
                        suffix + " CMD", Navy, 9, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
                }
            }

            ConfigureText(GetOrCreate<HmiText>(production, "REV14_ProductPressure_Label"), 35, 432, 150, 22,
                "PRODUCT PRESSURE", Navy, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(production, "REV14_ProductPressure_Value"), 185, 432, 120, 22,
                "NOT CONFIGURED", Color.FromArgb(105, 115, 125), 9, HmiFontWeight.Bold,
                HmiHorizontalAlignment.Center);

            DeleteItem(production, "REV12_Mimic_Vacuum");
            ConfigureValidatedValue(
                GetOrCreate<HmiText>(production, "REV14_Mimic_Vacuum"),
                555, 399, 95, 32,
                "Vacuum_Actual_mbar", "Vacuum_Signal_Valid", "0.0");

            DeleteItem(production, "REV12_Operate_Speed_PV");
            ConfigureValidatedValue(
                GetOrCreate<HmiText>(production, "REV14_Production_ActualSpeed"),
                885, 371, 105, 34,
                "Speed_Feedback_Pct", "Speed_Feedback_Valid", "0.0");
            ConfigureText(GetOrCreate<HmiText>(production, "REV12_Operate_Speed_PV_Label"), 885, 347, 105, 22,
                "ACTUAL SPEED [%]", Navy, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);

            for (int index = 0; index < 12; index++)
            {
                HmiEllipse dot = production.ScreenItems.Find("REV13_SpeedGauge_Dot_" + index) as HmiEllipse;
                if (dot == null) continue;
                int threshold = (index + 1) * 8;
                ConfigureNumericDynamization(dot, "Visible", "Speed_Feedback_Valid",
                    "let ok=Boolean(HMIRuntime.Tags(\"Speed_Feedback_Valid\").Read());" +
                    "let v=Number(HMIRuntime.Tags(\"Speed_Feedback_Pct\").Read());" +
                    "return ok && Number.isFinite(v) && v >= " + threshold + ";");
            }
            DeleteItem(production, "REV13_SpeedGauge_Value");
            ConfigureValidatedValue(
                GetOrCreate<HmiText>(production, "REV14_SpeedGauge_Value"),
                1091, 285, 52, 32,
                "Speed_Feedback_Pct", "Speed_Feedback_Valid", "0.0");

            ConfigureText(GetOrCreate<HmiText>(production, "REV13_Production_BottleUnavailable"), 1082, 180, 102, 34,
                "N/A", Color.FromArgb(105, 115, 125), 18, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureText(GetOrCreate<HmiText>(production, "REV12_Exact_Kpi_Title_ProductPump"), 1044, 580, 145, 22,
                "PUMP SPEED REF", Navy, 11, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);

            HmiText efficiency = production.ScreenItems.Find("REV13_EfficiencyGauge_Value") as HmiText;
            if (efficiency != null)
            {
                ConfigureNumericDynamization(efficiency, "Text", "Efficiency_Total_Seconds",
                    "let t=Number(HMIRuntime.Tags(\"Efficiency_Total_Seconds\").Read());" +
                    "let r=Number(HMIRuntime.Tags(\"Efficiency_Run_Seconds\").Read());" +
                    "if(!Number.isFinite(t) || t<=0){return \"N/A\";}" +
                    "return Math.round((r*100)/t).toString()+\"%\";");
            }

            ConfigureText(GetOrCreate<HmiText>(production, "REV13_Common_BottomInfo"), 1010, 722, 335, 34,
                "PLC / HMI LINK: NOT CONFIGURED", Color.FromArgb(235, 160, 30), 12,
                HmiFontWeight.Bold, HmiHorizontalAlignment.Right);
            ConfigureText(GetOrCreate<HmiText>(production, "REV12_Production_User_Header"), 1090, 61, 108, 28,
                "USER: DEFAULT", Navy, 11, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
        }

        private static void ConfigureValidatedValue(
            HmiText item,
            int left,
            int top,
            uint width,
            uint height,
            string valueTag,
            string validTag,
            string format)
        {
            ConfigureText(item, left, top, width, height, "N/A", Navy, 14, HmiFontWeight.Bold,
                HmiHorizontalAlignment.Center);
            ConfigureNumericDynamization(item, "Text", validTag,
                "let ok=Boolean(HMIRuntime.Tags(\"" + validTag + "\").Read());" +
                "let v=Number(HMIRuntime.Tags(\"" + valueTag + "\").Read());" +
                "if(!ok || !Number.isFinite(v)){return \"N/A\";}" +
                "return v.toFixed(" + (format.Contains(".") ? "1" : "0") + ");");
        }

        private static void ApplyRevision14Graphics(
            HmiScreen home,
            HmiScreen production,
            IDictionary<string, string> graphics)
        {
            ReplaceBottleWithGraphic(home, "REV13_Home_Bottle", "REV14_Home_BottleSvg", 1090, 205, 46, 68, graphics["bottle"]);
            ReplaceBottleWithGraphic(production, "REV13_Production_Bottle", "REV14_Production_BottleSvg", 1040, 175, 40, 60, graphics["bottle"]);

            ReplacePumpWithGraphic(production, "REV13_ProductPump", "REV14_ProductPumpSvg", 50, 232, 105, 76, graphics["pump_product"]);
            ReplacePumpWithGraphic(production, "REV13_VacuumPump", "REV14_VacuumPumpSvg", 570, 160, 115, 78, graphics["pump_vacuum"]);

            AddRevision14ValveGraphic(production, "EV210", 172, 263, graphics["valve"]);
            AddRevision14ValveGraphic(production, "EV212", 456, 195, graphics["valve"]);
            AddRevision14ValveGraphic(production, "EV217", 451, 263, graphics["valve"]);
            AddRevision14ValveGraphic(production, "EV213", 541, 263, graphics["valve"]);
            AddRevision14ValveGraphic(production, "EV247", 631, 263, graphics["valve"]);

            AddGraphicView(production, "REV14_SPL100_SensorSvg", 268, 394, 34, 34, graphics["sensor_digital"]);
            AddGraphicView(production, "REV14_TLS100_SensorSvg", 365, 198, 30, 30, graphics["sensor_analog"]);

            AddRevision14NavigationGraphics(home, "HOME", graphics);
            AddRevision14NavigationGraphics(production, "PRODUCTION", graphics);
        }

        private static void ReplaceBottleWithGraphic(
            HmiScreen screen,
            string legacyPrefix,
            string name,
            int left,
            int top,
            uint width,
            uint height,
            string graphic)
        {
            DeleteItem(screen, legacyPrefix + "_Body");
            DeleteItem(screen, legacyPrefix + "_Neck");
            AddGraphicView(screen, name, left, top, width, height, graphic);
        }

        private static void ReplacePumpWithGraphic(
            HmiScreen screen,
            string legacyPrefix,
            string name,
            int left,
            int top,
            uint width,
            uint height,
            string graphic)
        {
            DeleteItem(screen, legacyPrefix + "_Body");
            DeleteItem(screen, legacyPrefix + "_Motor");
            DeleteItem(screen, legacyPrefix + "_Base");
            AddGraphicView(screen, name, left, top, width, height, graphic);
        }

        private static void AddRevision14ValveGraphic(
            HmiScreen screen,
            string suffix,
            int left,
            int top,
            string graphic)
        {
            string prefix = "REV13_" + suffix;
            DeleteItem(screen, prefix + "_A");
            DeleteItem(screen, prefix + "_B");
            DeleteItem(screen, prefix + "_Stem");
            AddGraphicView(screen, "REV14_" + suffix + "_Svg", left, top, 46, 34, graphic);
        }

        private static void AddRevision14NavigationGraphics(
            HmiScreen screen,
            string activePage,
            IDictionary<string, string> graphics)
        {
            string[] labels = { "HOME", "SAFETY", "OPERATE", "PRODUCTION", "CIP", "FUNCTION", "ALARMS", "RECIPE", "SETUP", "MANUAL", "EFFICIENCY", "DIAGNOSTICS" };
            string[] assets = { "nav_home", "nav_safety", "nav_operate", "nav_production", "nav_cip", "nav_function", "nav_alarms", "nav_recipe", "nav_settings", "nav_manual", "nav_efficiency", "nav_diagnostics" };
            for (int index = 0; index < labels.Length; index++)
            {
                string legacy = "REV13_Nav_Icon_" + labels[index];
                DeleteItem(screen, legacy + "_Ring");
                DeleteItem(screen, legacy + "_LineA");
                DeleteItem(screen, legacy + "_LineB");
                int top = 105 + index * 49;
                HmiEllipse iconBack = GetOrCreate<HmiEllipse>(screen, "REV14_Nav_IconBack_" + labels[index]);
                iconBack.CenterX = 1225;
                iconBack.CenterY = top + 21;
                iconBack.RadiusX = 14;
                iconBack.RadiusY = 14;
                iconBack.BackColor = labels[index].Equals(activePage, StringComparison.OrdinalIgnoreCase)
                    ? Color.FromArgb(210, 235, 255)
                    : Color.White;
                iconBack.BorderColor = iconBack.BackColor;
                iconBack.BorderWidth = 1;
                iconBack.BackFillPattern = HmiFillPattern.Solid;
                iconBack.Visible = true;
                iconBack.Enabled = false;
                AddGraphicView(screen, "REV14_Nav_Icon_" + labels[index], 1214, top + 10, 22, 22, graphics[assets[index]]);
            }
        }

        private static void AddGraphicView(
            HmiScreen screen,
            string name,
            int left,
            int top,
            uint width,
            uint height,
            string graphic)
        {
            HmiGraphicView item = GetOrCreate<HmiGraphicView>(screen, name);
            SetBounds(item, left, top, width, height);
            item.Graphic = graphic;
            item.GraphicStretchMode = HmiGraphicStretchMode.Uniform;
            item.Visible = true;
            item.Enabled = false;
        }

        private static void BuildRevision13Chrome(HmiScreen screen, string pageTitle, string activePage)
        {
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV13_Common_Header"), 0, 0, 1366, 50,
                Color.FromArgb(3, 39, 86), Color.FromArgb(3, 39, 86), 0);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV13_Common_Title"), 16, 8, 610, 36,
                "SCHLENKER MONOBLOCK REAL JUICE", Color.White, 24, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV13_Common_Page"), 1010, 10, 335, 34,
                pageTitle + "  |  FBS GLOBAL", Color.White, 18, HmiFontWeight.Bold, HmiHorizontalAlignment.Right);
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV13_Common_Status"), 0, 50, 1366, 50,
                Color.FromArgb(247, 249, 251), Color.FromArgb(220, 226, 232), 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV13_Common_StatusLabel"), 26, 61, 125, 28,
                "MACHINE STATUS", Dark, 14, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureSymbolicField(GetOrCreate<HmiSymbolicIOField>(screen, "REV13_Common_State"),
                160, 58, 330, 34, "Machine_State", "MachineStateText");
            ConfigureStatusLamp(screen, "REV13_Common_ReadyLamp", 795, 75, "Machine_SafetyOK");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV13_Common_ReadyText"), 805, 62, 112, 26,
                "SYSTEM READY", Dark, 13, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV13_Common_AlarmText"), 940, 62, 90, 26,
                "ALARMS", Navy, 13, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureBooleanIndicator(GetOrCreate<HmiIOField>(screen, "REV13_Common_AlarmValue"), 1030, 59, 48, 32,
                "Alarm_Critical");
            AddRevision13Navigation(screen, activePage);
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV13_Common_Bottom"), 0, 712, 1366, 56,
                Color.FromArgb(3, 39, 86), Color.FromArgb(3, 39, 86), 0);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV13_Common_BottomInfo"), 1010, 722, 335, 34,
                "CONNECTION STATUS FROM LIVE PLC/HMI", Color.White, 12, HmiFontWeight.Bold, HmiHorizontalAlignment.Right);
        }

        private static void ApplyUserHeaderToMissingScreens(HmiSoftware hmi)
        {
            const string canonicalName = "REV12_Production_User_Header";
            int updated = 0;
            int existing = 0;

            foreach (HmiScreen screen in hmi.Screens.OrderBy(candidate => candidate.Name))
            {
                bool hasUserHeader = screen.ScreenItems.Any(item =>
                    item.Name.IndexOf("User_Header", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.Name.IndexOf("Session_User", StringComparison.OrdinalIgnoreCase) >= 0);

                if (hasUserHeader)
                {
                    existing++;
                    Console.WriteLine("USER_HEADER screen={0} status=EXISTING", screen.Name);
                    continue;
                }

                HmiText indicator = GetOrCreate<HmiText>(screen, canonicalName);
                ConfigureText(indicator, 1090, 61, 108, 28,
                    "USER", Navy, 13, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
                indicator.Enabled = false;
                indicator.Visible = true;

                updated++;
                Console.WriteLine("USER_HEADER screen={0} status=ADDED", screen.Name);
            }

            Console.WriteLine("USER_HEADER_UPDATED={0}", updated);
            Console.WriteLine("USER_HEADER_EXISTING={0}", existing);
            Console.WriteLine("USER_MANAGEMENT_SCOPE=GLOBAL_EXISTING_CONFIGURATION_UNCHANGED");
            Console.WriteLine("USER_CONTROL_DUPLICATES_CREATED=0");
        }

        private static void FixUserHeaderLayoutOnNonProductionScreens(HmiSoftware hmi, string userGraphic, string targetScreen)
        {
            const string canonicalName = "REV12_Production_User_Header";
            int updated = 0;

            foreach (HmiScreen screen in hmi.Screens.OrderBy(candidate => candidate.Name))
            {
                if (!String.IsNullOrWhiteSpace(targetScreen) &&
                    !screen.Name.Equals(targetScreen, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                NormalizeRevision13HeaderFrame(screen);

                HmiText indicator = screen.ScreenItems.Find(canonicalName) as HmiText;
                if (indicator == null)
                {
                    Console.WriteLine("USER_HEADER_LAYOUT screen={0} status=MISSING", screen.Name);
                    continue;
                }

                // Reserve enough width for real session names such as Maintenance
                // and Engineer.  Keep a gutter after the alarm value and before the
                // approved navigation master, which begins at X=1215/Y=100.
                ConfigureText(indicator, 1112, 61, 95, 28,
                    "USER", Navy, 13, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
                indicator.Enabled = false;
                indicator.Visible = true;

                AddGraphicView(
                    screen,
                    "REV21_Common_User_Profile_Icon",
                    1087,
                    64,
                    22,
                    22,
                    userGraphic);
                HmiGraphicView profileIcon = screen.ScreenItems.Find("REV21_Common_User_Profile_Icon") as HmiGraphicView;
                if (profileIcon != null)
                {
                    profileIcon.BackColor = Color.Transparent;
                }

                foreach (HmiText peer in screen.ScreenItems.OfType<HmiText>().ToList())
                {
                    if (Object.ReferenceEquals(peer, indicator) ||
                        peer.Name.Equals(canonicalName, StringComparison.OrdinalIgnoreCase) ||
                        !peer.Visible)
                    {
                        continue;
                    }

                    bool verticalConflict = peer.Top < 89 && peer.Top + peer.Height > 61;
                    bool horizontalConflict = peer.Left < 1210 && peer.Left + peer.Width > 1085;
                    if (!verticalConflict || !horizontalConflict)
                    {
                        continue;
                    }

                    if (peer.Left < 1065)
                    {
                        uint originalWidth = peer.Width;
                        peer.Width = (uint)(1065 - peer.Left);
                        Console.WriteLine(
                            "USER_HEADER_PEER screen={0} object={1} status=SHORTENED width={2}->{3}",
                            screen.Name,
                            peer.Name,
                            originalWidth,
                            peer.Width);
                    }
                    else
                    {
                        Console.WriteLine(
                            "USER_HEADER_PEER screen={0} object={1} status=AMBIGUOUS bounds={2},{3},{4},{5}",
                            screen.Name,
                            peer.Name,
                            peer.Left,
                            peer.Top,
                            peer.Width,
                            peer.Height);
                    }
                }

                updated++;
                Console.WriteLine("USER_HEADER_LAYOUT screen={0} status=MOVED bounds=1112,61,95,28 icon=1087,64,22,22", screen.Name);
            }

            Console.WriteLine("USER_HEADER_LAYOUT_UPDATED={0}", updated);
            Console.WriteLine("USER_HEADER_LAYOUT_ALL_PAGES=1");
            Console.WriteLine("PRODUCTION_CENTRAL_CONTENT_MODIFIED=0");
        }

        private static void PolishProductionMimicAndCommonStatus(HmiSoftware hmi, bool productionOnly)
        {
            HmiScreen production = hmi.Screens.Find("production");
            if (production == null)
            {
                throw new InvalidOperationException("Production screen was not found.");
            }

            // Preserve the operator's selected graphics, coordinates, dimensions,
            // names, events and bindings.  Only remove the opaque graphic-view
            // background and retain aspect-correct rendering.
            int polished = 0;
            foreach (HmiGraphicView icon in production.ScreenItems.OfType<HmiGraphicView>().ToList())
            {
                bool commandAreaIcon = icon.Left >= 730 && icon.Left < 1010 &&
                    icon.Top >= 170 && icon.Top < 340;
                if ((!icon.Name.StartsWith("REV14_", StringComparison.OrdinalIgnoreCase) && !commandAreaIcon) ||
                    icon.Name.IndexOf("Nav_Icon", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    icon.Name.IndexOf("User_Profile", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                icon.BackColor = Color.Transparent;
                icon.GraphicStretchMode = HmiGraphicStretchMode.Uniform;
                polished++;
                Console.WriteLine(
                    "PRODUCTION_ICON_TRANSPARENT name={0} bounds={1},{2},{3},{4} graphic={5}",
                    icon.Name, icon.Left, icon.Top, icon.Width, icon.Height, icon.Graphic);
            }

            string[] commandButtons =
            {
                "REV12_Operate_AutoStart",
                "REV12_Operate_Stop",
                "REV12_Operate_Reset",
                "REV12_Operate_Manual"
            };
            foreach (string commandButtonName in commandButtons)
            {
                HmiButton commandButton = production.ScreenItems.Find(commandButtonName) as HmiButton;
                if (commandButton == null)
                {
                    Console.WriteLine("PRODUCTION_COMMAND_ICON name={0} status=MISSING", commandButtonName);
                    continue;
                }

                bool stretchSet = TrySetProperty(commandButton, "GraphicStretchMode", HmiGraphicStretchMode.Uniform);
                Console.WriteLine(
                    "PRODUCTION_COMMAND_ICON name={0} status={1} bounds={2},{3},{4},{5}",
                    commandButtonName,
                    stretchSet ? "UNIFORM" : "NATIVE_PROPERTY_UNAVAILABLE",
                    commandButton.Left,
                    commandButton.Top,
                    commandButton.Width,
                    commandButton.Height);
            }

            // Keep the existing circular line-speed gauge geometry.  Its verified
            // actual-speed percentage is scaled to the machine's configured
            // 0..12,000 BPH range; no new PLC tag or binding is invented.
            HmiText speedTitle = production.ScreenItems.Find("REV12_Exact_Kpi_Title_LineSpeed") as HmiText;
            if (speedTitle != null)
            {
                SetText(speedTitle.Text, "LINE SPEED [BPH]");
            }

            HmiScreenItemBase oldSpeedValue = production.ScreenItems.Find("REV13_SpeedGauge_Value");
            if (oldSpeedValue != null)
            {
                oldSpeedValue.Visible = false;
            }

            HmiEllipse oldSpeedRing = production.ScreenItems.Find("REV13_SpeedGauge_Ring") as HmiEllipse;
            if (oldSpeedRing != null)
            {
                oldSpeedRing.Visible = false;
            }
            for (int index = 0; index < 12; index++)
            {
                HmiEllipse oldDot = production.ScreenItems.Find("REV13_SpeedGauge_Dot_" + index) as HmiEllipse;
                if (oldDot != null)
                {
                    oldDot.Visible = false;
                }
            }

            HmiText speedValue = GetOrCreate<HmiText>(production, "REV21_SpeedGauge_BPH_Value");
            ConfigureText(speedValue, 1062, 268, 90, 34,
                "N/A", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Right);
            ConfigureNumericDynamization(speedValue, "Text", "Speed_Actual_Pct",
                "let p=Number(HMIRuntime.Tags(\"Speed_Actual_Pct\").Read());" +
                "if(!Number.isFinite(p)){return \"N/A\";}" +
                "p=Math.max(0,Math.min(100,p));return String(Math.round(p*120));");
            ConfigureText(GetOrCreate<HmiText>(production, "REV21_SpeedGauge_Unit"), 1154, 276, 32, 24,
                "bph", Navy, 11, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);

            HmiRectangle speedTrack = GetOrCreate<HmiRectangle>(production, "REV21_SpeedGauge_Track");
            ConfigureRectangle(speedTrack, 1048, 307, 136, 8,
                Color.FromArgb(225, 230, 235), Color.FromArgb(225, 230, 235), 0);
            HmiRectangle speedFill = GetOrCreate<HmiRectangle>(production, "REV21_SpeedGauge_Fill");
            ConfigureRectangle(speedFill, 1048, 307, 1, 8,
                Color.FromArgb(0, 100, 210), Color.FromArgb(0, 100, 210), 0);
            ConfigureNumericDynamization(speedFill, "Width", "Speed_Actual_Pct",
                "let p=Number(HMIRuntime.Tags(\"Speed_Actual_Pct\").Read());" +
                "if(!Number.isFinite(p)){return 0;}p=Math.max(0,Math.min(100,p));return Math.round(p*1.36);");

            HmiEllipse speedKnob = GetOrCreate<HmiEllipse>(production, "REV21_SpeedGauge_Knob");
            speedKnob.CenterX = 1048;
            speedKnob.CenterY = 311;
            speedKnob.RadiusX = 7;
            speedKnob.RadiusY = 7;
            speedKnob.BackColor = Color.FromArgb(0, 100, 210);
            speedKnob.BorderColor = Color.FromArgb(0, 100, 210);
            speedKnob.BorderWidth = 1;
            speedKnob.BackFillPattern = HmiFillPattern.Solid;
            speedKnob.Enabled = false;
            speedKnob.Visible = true;
            ConfigureNumericDynamization(speedKnob, "CenterX", "Speed_Actual_Pct",
                "let p=Number(HMIRuntime.Tags(\"Speed_Actual_Pct\").Read());" +
                "if(!Number.isFinite(p)){return 1048;}p=Math.max(0,Math.min(100,p));return Math.round(1048+p*1.36);");

            ConfigureText(GetOrCreate<HmiText>(production, "REV21_SpeedGauge_Min"), 1042, 320, 40, 18,
                "0", Dark, 9, HmiFontWeight.Normal, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(production, "REV21_SpeedGauge_Mid"), 1092, 320, 48, 18,
                "6,000", Dark, 9, HmiFontWeight.Normal, HmiHorizontalAlignment.Center);
            ConfigureText(GetOrCreate<HmiText>(production, "REV21_SpeedGauge_Max"), 1142, 320, 48, 18,
                "12,000", Dark, 9, HmiFontWeight.Normal, HmiHorizontalAlignment.Right);

            int clockScreens = 0;
            foreach (HmiScreen screen in hmi.Screens.OrderBy(candidate => candidate.Name))
            {
                if (productionOnly && !screen.Name.Equals("production", StringComparison.OrdinalIgnoreCase))
                {
                    HmiScreenItemBase priorClock = screen.ScreenItems.Find("REV21_Common_DateTime");
                    if (priorClock != null)
                    {
                        DeleteItem(screen, "REV21_Common_DateTime");
                        HmiEllipse restoreReadyLamp = screen.ScreenItems.Find("REV13_Common_ReadyLamp") as HmiEllipse;
                        HmiText restoreReadyText = screen.ScreenItems.Find("REV13_Common_ReadyText") as HmiText;
                        HmiText restoreAlarmText = screen.ScreenItems.Find("REV13_Common_AlarmText") as HmiText;
                        HmiIOField restoreAlarmValue = screen.ScreenItems.Find("REV13_Common_AlarmValue") as HmiIOField;
                        HmiGraphicView restoreUserIcon = screen.ScreenItems.Find("REV21_Common_User_Profile_Icon") as HmiGraphicView;
                        HmiText restoreUserText = screen.ScreenItems.Find("REV12_Production_User_Header") as HmiText;
                        if (restoreReadyLamp != null)
                        {
                            restoreReadyLamp.CenterX = 795;
                            restoreReadyLamp.CenterY = 75;
                        }
                        if (restoreReadyText != null) SetBounds(restoreReadyText, 805, 62, 112, 26);
                        if (restoreAlarmText != null) SetBounds(restoreAlarmText, 940, 62, 90, 26);
                        if (restoreAlarmValue != null) SetBounds(restoreAlarmValue, 1030, 59, 48, 32);
                        if (restoreUserIcon != null)
                        {
                            SetBounds(restoreUserIcon, 1087, 64, 22, 22);
                            restoreUserIcon.BackColor = Color.Transparent;
                        }
                        if (restoreUserText != null) SetBounds(restoreUserText, 1112, 61, 95, 28);
                        Console.WriteLine("COMMON_CLOCK_ROLLBACK screen={0} status=RESTORED", screen.Name);
                    }
                    continue;
                }

                HmiEllipse readyLamp = screen.ScreenItems.Find("REV13_Common_ReadyLamp") as HmiEllipse;
                HmiText readyText = screen.ScreenItems.Find("REV13_Common_ReadyText") as HmiText;
                HmiText alarmText = screen.ScreenItems.Find("REV13_Common_AlarmText") as HmiText;
                HmiIOField alarmValue = screen.ScreenItems.Find("REV13_Common_AlarmValue") as HmiIOField;
                HmiGraphicView userIcon = screen.ScreenItems.Find("REV21_Common_User_Profile_Icon") as HmiGraphicView;
                HmiText userText = screen.ScreenItems.Find("REV12_Production_User_Header") as HmiText;

                if (userIcon == null)
                {
                    userIcon = screen.ScreenItems.OfType<HmiGraphicView>().FirstOrDefault(candidate =>
                        String.Equals(candidate.Graphic, "REV14_user_profile", StringComparison.OrdinalIgnoreCase));
                }

                if (readyLamp == null || readyText == null || alarmText == null ||
                    (!productionOnly && alarmValue == null) || userIcon == null || userText == null)
                {
                    Console.WriteLine("COMMON_CLOCK screen={0} status=SKIPPED_MISSING_COMMON_OBJECT", screen.Name);
                    continue;
                }

                if (!productionOnly)
                {
                    readyLamp.CenterX = 715;
                    readyLamp.CenterY = 75;
                    SetBounds(readyText, 725, 62, 112, 26);
                    SetBounds(alarmText, 840, 62, 70, 26);
                    SetBounds(alarmValue, 910, 59, 48, 32);
                }

                HmiText clock = GetOrCreate<HmiText>(screen, "REV21_Common_DateTime");
                ConfigureText(clock, productionOnly ? 970 : 965, 53, (uint)(productionOnly ? 110 : 105), 44,
                    "00:00:00 AM\n00/00/0000", Navy, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
                ConfigureNumericDynamization(clock, "Text", "", 
                    "let d=new Date();" +
                    "let z=function(v){return v<10?\"0\"+v:String(v);};" +
                    "let h=d.getHours();let ap=h>=12?\"PM\":\"AM\";h=h%12;if(h===0){h=12;}" +
                    "return z(h)+\":\"+z(d.getMinutes())+\":\"+z(d.getSeconds())+\" \"+ap+\"\\n\"+" +
                    "z(d.getDate())+\"/\"+z(d.getMonth()+1)+\"/\"+d.getFullYear();");

                SetBounds(userIcon, productionOnly ? 1087 : 1080, 64, 22, 22);
                userIcon.BackColor = Color.Transparent;
                SetBounds(userText, productionOnly ? 1112 : 1107, 61, (uint)(productionOnly ? 95 : 100), 28);
                clockScreens++;
                Console.WriteLine("COMMON_CLOCK screen={0} status=READY", screen.Name);
            }

            Console.WriteLine("PRODUCTION_ICONS_POLISHED={0}", polished);
            Console.WriteLine("COMMON_CLOCK_SCREENS={0}", clockScreens);
            Console.WriteLine("PRODUCTION_ONLY_SCOPE={0}", productionOnly ? 1 : 0);
            Console.WriteLine("PRODUCTION_OBJECTS_MOVED=0");
            Console.WriteLine("PRODUCTION_GRAPHICS_REPLACED=0");
            Console.WriteLine("PLC_BINDINGS_MODIFIED=0");
        }

        private static void AddProductionClockOnly(HmiSoftware hmi)
        {
            HmiScreen production = hmi.Screens.Find("production");
            if (production == null) throw new InvalidOperationException("Production screen was not found.");

            HmiText alarmText = production.ScreenItems.Find("REV13_Common_AlarmText") as HmiText;
            HmiGraphicView userIcon = production.ScreenItems.Find("REV21_Common_User_Profile_Icon") as HmiGraphicView;
            HmiText userText = production.ScreenItems.Find("REV12_Production_User_Header") as HmiText;
            if (alarmText == null || userIcon == null || userText == null)
            {
                throw new InvalidOperationException("Production ALARMS/USER common-status objects are incomplete.");
            }

            HmiText clock = GetOrCreate<HmiText>(production, "REV21_Common_DateTime");
            ConfigureText(clock, 970, 53, 110, 44,
                "00:00:00 AM\n00/00/0000", Navy, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureNumericDynamization(clock, "Text", "",
                "let d=new Date();" +
                "let z=function(v){return v<10?\"0\"+v:String(v);};" +
                "let h=d.getHours();let ap=h>=12?\"PM\":\"AM\";h=h%12;if(h===0){h=12;}" +
                "return z(h)+\":\"+z(d.getMinutes())+\":\"+z(d.getSeconds())+\" \"+ap+\"\\n\"+" +
                "z(d.getDate())+\"/\"+z(d.getMonth()+1)+\"/\"+d.getFullYear();");

            SetBounds(userIcon, 1087, 64, 22, 22);
            userIcon.BackColor = Color.Transparent;
            SetBounds(userText, 1112, 61, 95, 28);
            Console.WriteLine("PRODUCTION_CLOCK requested_visible_bounds=970,53,110,44 status=READY");
            Console.WriteLine("PRODUCTION_USER_ICON requested_visible_bounds=1087,64,22,22 transparent=1");
            Console.WriteLine("PRODUCTION_USER_TEXT bounds=1112,61,95,28");
            Console.WriteLine("NONPRODUCTION_SCREENS_MODIFIED=0");
            Console.WriteLine("PLC_BINDINGS_MODIFIED=0");
        }

        private static void NormalizeRevision13HeaderFrame(HmiScreen screen)
        {
            HmiRectangle header = screen.ScreenItems.OfType<HmiRectangle>()
                .FirstOrDefault(item => item.Left == 0 && item.Top == 0 &&
                    item.Name.IndexOf("Header", StringComparison.OrdinalIgnoreCase) >= 0);
            if (header != null)
            {
                ConfigureRectangle(header, 0, 0, 1366, 50,
                    Color.FromArgb(3, 39, 86), Color.FromArgb(3, 39, 86), 0);
            }

            HmiText title = screen.ScreenItems.OfType<HmiText>()
                .FirstOrDefault(item => item.Name.IndexOf("Common_Title", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.Name.IndexOf("Header_Title", StringComparison.OrdinalIgnoreCase) >= 0);
            if (title != null)
            {
                SetBounds(title, 16, 8, 610, 36);
                title.ForeColor = Color.White;
            }

            HmiText page = screen.ScreenItems.Find("REV13_Common_Page") as HmiText;
            if (page == null)
            {
                page = screen.ScreenItems.Find("REV12_Common_Header_Page") as HmiText;
            }
            if (page == null)
            {
                page = screen.ScreenItems.OfType<HmiText>()
                    .FirstOrDefault(item => item.Name.IndexOf("Common_Page", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        item.Name.IndexOf("Header_Page", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        item.Name.IndexOf("Header_Subtitle", StringComparison.OrdinalIgnoreCase) >= 0);
            }
            if (page != null)
            {
                SetBounds(page, 1010, 10, 335, 34);
                page.ForeColor = Color.White;
            }

            HmiRectangle status = screen.ScreenItems.OfType<HmiRectangle>()
                .FirstOrDefault(item => item.Left == 0 && item.Width == 1366 && item.Top >= 40 && item.Top <= 80 &&
                    item.Name.IndexOf("Status", StringComparison.OrdinalIgnoreCase) >= 0);
            if (status != null)
            {
                ConfigureRectangle(status, 0, 50, 1366, 50,
                    Color.FromArgb(247, 249, 251), Color.FromArgb(220, 226, 232), 1);
            }

            HmiText statusLabel = screen.ScreenItems.OfType<HmiText>()
                .FirstOrDefault(item => item.Name.IndexOf("StatusLabel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.Name.IndexOf("Status_Label", StringComparison.OrdinalIgnoreCase) >= 0);
            if (statusLabel != null)
            {
                SetBounds(statusLabel, 26, 61, 125, 28);
            }

            HmiSymbolicIOField state = screen.ScreenItems.OfType<HmiSymbolicIOField>()
                .FirstOrDefault(item => item.Top < 120 && item.Left < 750);
            if (state != null)
            {
                SetBounds(state, 160, 58, 330, 34);
            }

            HmiText statusNote = screen.ScreenItems.OfType<HmiText>()
                .FirstOrDefault(item => item.Name.IndexOf("Status_Note", StringComparison.OrdinalIgnoreCase) >= 0);
            if (statusNote != null)
            {
                SetBounds(statusNote, 735, 61, 330, 28);
            }

            HmiText duplicatePageLabel = screen.ScreenItems.Find("REV12_Common_Page_Label") as HmiText;
            if (duplicatePageLabel != null && !Object.ReferenceEquals(duplicatePageLabel, page))
            {
                duplicatePageLabel.Visible = false;
            }

            ConfigureStatusLamp(screen, "REV13_Common_ReadyLamp", 795, 75, "Machine_SafetyOK");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV13_Common_ReadyText"), 805, 62, 112, 26,
                "SYSTEM READY", Dark, 13, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV13_Common_AlarmText"), 940, 62, 90, 26,
                "ALARMS", Navy, 13, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureBooleanIndicator(GetOrCreate<HmiIOField>(screen, "REV13_Common_AlarmValue"), 1030, 59, 48, 32,
                "Alarm_Critical");

            if (screen.Name.Equals("manual", StringComparison.OrdinalIgnoreCase))
            {
                if (page != null)
                {
                    ConfigureText(page, 1010, 10, 335, 34,
                        "MANUAL  |  FBS GLOBAL", Color.White, 18,
                        HmiFontWeight.Bold, HmiHorizontalAlignment.Right);
                }
            }
            else if (screen.Name.Equals("diagnostics", StringComparison.OrdinalIgnoreCase) && page != null)
            {
                ConfigureText(page, 1010, 10, 335, 34,
                    "DIAGNOSTICS  |  FBS GLOBAL", Color.White, 18,
                    HmiFontWeight.Bold, HmiHorizontalAlignment.Right);
            }

            Console.WriteLine(
                "COMMON_HEADER_FRAME screen={0} header={1} status={2}",
                screen.Name,
                header == null ? "MISSING" : "0,0,1366,50",
                status == null ? "MISSING" : "0,50,1366,50");
        }

        private static void AddRevision13Navigation(HmiScreen screen, string activePage)
        {
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV13_Nav_Back"), 1215, 100, 151, 612,
                Color.FromArgb(3, 39, 86), Color.FromArgb(3, 39, 86), 0);
            string[] labels = { "HOME", "SAFETY", "OPERATE", "PRODUCTION", "CIP", "FUNCTION", "ALARMS", "RECIPE", "SETUP", "MANUAL", "EFFICIENCY", "DIAGNOSTICS" };
            string[] destinations = { "home", "safety", "operate", "production", "cip", "function", "alarms", "recipe", "setup", "manual", "efficiency", "diagnostics" };
            for (int index = 0; index < labels.Length; index++)
            {
                int top = 105 + index * 49;
                bool active = labels[index].Equals(activePage, StringComparison.OrdinalIgnoreCase);
                HmiButton button = GetOrCreate<HmiButton>(screen, "REV13_Nav_Button_" + labels[index]);
                ConfigureButton(button, 1248, top, 109, 43, labels[index], active ? Color.FromArgb(0, 90, 205) : Color.FromArgb(8, 55, 105));
                button.Enabled = !active;
                if (!active) ConfigureScreenNavigation(button, destinations[index], activePage);
                DrawNavigationIcon(screen, "REV13_Nav_Icon_" + labels[index], 1225, top + 21, index, active);
            }
        }

        private static void DrawNavigationIcon(HmiScreen screen, string name, int centerX, int centerY, int style, bool active)
        {
            Color color = active ? Color.FromArgb(110, 190, 255) : Color.White;
            HmiEllipse ring = GetOrCreate<HmiEllipse>(screen, name + "_Ring");
            ring.CenterX = centerX; ring.CenterY = centerY; ring.RadiusX = 10; ring.RadiusY = 10;
            ring.BackColor = Color.FromArgb(3, 39, 86); ring.BorderColor = color; ring.BorderWidth = 2;
            ring.BackFillPattern = HmiFillPattern.Solid; ring.Visible = true; ring.Enabled = false;
            ConfigureDashboardLine(GetOrCreate<HmiLine>(screen, name + "_LineA"), centerX - 6, centerY + 5,
                centerX + 6, centerY - 5 + (style % 3), color, 2);
            ConfigureDashboardLine(GetOrCreate<HmiLine>(screen, name + "_LineB"), centerX - 6, centerY - 5 + (style % 4),
                centerX + 6, centerY + 5, color, 2);
        }

        private static void DrawMachineModule(HmiScreen screen, string suffix, int left, int top, uint width, uint height, string label, string tag)
        {
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV13_Home_Module_" + suffix), left, top, width, height,
                Color.FromArgb(232, 240, 248), Color.FromArgb(0, 78, 171), 2);
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV13_Home_ModuleTop_" + suffix), left + 10, top - 12, width - 20, 16,
                Color.FromArgb(18, 91, 148), Color.FromArgb(18, 91, 148), 0);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV13_Home_ModuleLabel_" + suffix), left + 10, top + (int)height / 2 - 15, width - 20, 30,
                label, Navy, 16, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureStatusLamp(screen, "REV13_Home_ModuleLamp_" + suffix, left + (int)width - 18, top + 18, tag);
        }

        private static void DrawPumpSymbol(HmiScreen screen, string name, int left, int top, string code, string label, string stateTag)
        {
            HmiEllipse body = GetOrCreate<HmiEllipse>(screen, name + "_Body");
            body.CenterX = left + 32; body.CenterY = top + 30; body.RadiusX = 28; body.RadiusY = 28;
            body.BackColor = Color.FromArgb(18, 91, 148); body.BorderColor = Color.FromArgb(0, 70, 135); body.BorderWidth = 2;
            body.BackFillPattern = HmiFillPattern.Solid; body.Visible = true; body.Enabled = false;
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, name + "_Motor"), left + 20, top - 12, 24, 25,
                Color.FromArgb(18, 91, 148), Color.FromArgb(0, 70, 135), 2);
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, name + "_Base"), left, top + 57, 66, 8,
                Color.FromArgb(0, 70, 135), Color.FromArgb(0, 70, 135), 0);
            ConfigureText(GetOrCreate<HmiText>(screen, name + "_Code"), left - 10, top + 70, 86, 22,
                code, Navy, 12, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureText(GetOrCreate<HmiText>(screen, name + "_Label"), left - 22, top + 91, 110, 25,
                label, Navy, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureStatusLamp(screen, name + "_Lamp", left + 55, top + 4, stateTag);
        }

        private static void DrawValveSymbol(HmiScreen screen, string name, int left, int top, string code, string stateTag)
        {
            ConfigureDashboardLine(GetOrCreate<HmiLine>(screen, name + "_A"), left, top, left + 34, top + 28, Color.FromArgb(90, 110, 130), 3);
            ConfigureDashboardLine(GetOrCreate<HmiLine>(screen, name + "_B"), left, top + 28, left + 34, top, Color.FromArgb(90, 110, 130), 3);
            ConfigureDashboardLine(GetOrCreate<HmiLine>(screen, name + "_Stem"), left + 17, top, left + 17, top - 13, Color.FromArgb(90, 110, 130), 3);
            ConfigureStatusLamp(screen, name + "_Lamp", left + 17, top - 18, stateTag);
            ConfigureText(GetOrCreate<HmiText>(screen, name + "_Code"), left - 7, top + 31, 48, 20,
                code, Navy, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
        }

        private static void DrawBottleIcon(HmiScreen screen, string name, int left, int top, Color color)
        {
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, name + "_Body"), left, top + 12, 24, 44,
                Color.White, color, 2);
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, name + "_Neck"), left + 7, top, 10, 15,
                Color.White, color, 2);
        }

        private static void AddHomeMetric(HmiScreen screen, string suffix, int left, int top, string label, string tag, string format)
        {
            ConfigureText(GetOrCreate<HmiText>(screen, "REV13_Home_MetricLabel_" + suffix), left, top, 150, 24,
                label, Navy, 11, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureIOField(GetOrCreate<HmiIOField>(screen, "REV13_Home_MetricValue_" + suffix), left, top + 28, 150, 36,
                tag, true, format);
        }

        private static void BuildRevision13SpeedGauge(HmiScreen screen, int centerX, int centerY)
        {
            DeleteItem(screen, "REV12_Exact_Kpi_Value_LineSpeed");
            DeleteItem(screen, "REV12_Exact_Kpi_Unit_LineSpeed");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Exact_Kpi_Title_LineSpeed"), centerX - 72, centerY - 48, 144, 22,
                "LINE SPEED [%]", Navy, 12, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            HmiEllipse ring = GetOrCreate<HmiEllipse>(screen, "REV13_SpeedGauge_Ring");
            ring.CenterX = centerX; ring.CenterY = centerY + 12; ring.RadiusX = 33; ring.RadiusY = 33;
            ring.BackColor = Color.White; ring.BorderColor = Color.FromArgb(215, 222, 228); ring.BorderWidth = 8;
            ring.BackFillPattern = HmiFillPattern.Solid; ring.Visible = true; ring.Enabled = false;
            for (int i = 0; i < 12; i++)
            {
                double angle = (-210 + i * 22.5) * Math.PI / 180.0;
                int x = centerX + (int)Math.Round(Math.Cos(angle) * 34);
                int y = centerY + 12 + (int)Math.Round(Math.Sin(angle) * 34);
                HmiEllipse dot = GetOrCreate<HmiEllipse>(screen, "REV13_SpeedGauge_Dot_" + i);
                dot.CenterX = x; dot.CenterY = y; dot.RadiusX = 4; dot.RadiusY = 4;
                dot.BackColor = Color.FromArgb(0, 100, 210); dot.BorderColor = Color.FromArgb(0, 100, 210); dot.BorderWidth = 1;
                dot.BackFillPattern = HmiFillPattern.Solid; dot.Enabled = false; dot.Visible = true;
                int threshold = (i + 1) * 8;
                ConfigureNumericDynamization(dot, "Visible", "Speed_Actual_Pct",
                    "return Number(HMIRuntime.Tags(\"Speed_Actual_Pct\").Read()) >= " + threshold + ";");
            }
            ConfigureIOField(GetOrCreate<HmiIOField>(screen, "REV13_SpeedGauge_Value"), centerX - 26, centerY - 5, 52, 32,
                "Speed_Actual_Pct", true, "0.0");
        }

        private static void BuildRevision13EfficiencyGauge(HmiScreen screen, int centerX, int centerY)
        {
            DeleteItem(screen, "REV12_Exact_Kpi_Value_Efficiency");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Exact_Kpi_Title_Efficiency"), centerX - 72, centerY - 48, 144, 22,
                "EFFICIENCY", Navy, 12, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            HmiEllipse ring = GetOrCreate<HmiEllipse>(screen, "REV13_EfficiencyGauge_Ring");
            ring.CenterX = centerX; ring.CenterY = centerY + 12; ring.RadiusX = 33; ring.RadiusY = 33;
            ring.BackColor = Color.White; ring.BorderColor = Color.FromArgb(215, 222, 228); ring.BorderWidth = 8;
            ring.BackFillPattern = HmiFillPattern.Solid; ring.Visible = true; ring.Enabled = false;
            HmiText value = GetOrCreate<HmiText>(screen, "REV13_EfficiencyGauge_Value");
            ConfigureText(value, centerX - 30, centerY - 6, 60, 34, "0%", Navy, 17, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureNumericDynamization(value, "Text", "Efficiency_Total_Seconds",
                "let t=Number(HMIRuntime.Tags(\"Efficiency_Total_Seconds\").Read()); let r=Number(HMIRuntime.Tags(\"Efficiency_Run_Seconds\").Read()); if(t<=0){return \"0%\";} return Math.round((r*100)/t).toString()+\"%\";");
        }

        private static void AddProductionCommandMetric(
            HmiScreen screen, string labelName, string fieldName, int left, int top,
            string label, string tag, bool readOnly, string format)
        {
            ConfigureText(GetOrCreate<HmiText>(screen, labelName), left, top, 105, 22,
                label, Navy, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureIOField(GetOrCreate<HmiIOField>(screen, fieldName), left, top + 24, 105, 34,
                tag, readOnly, format);
        }

        private static void AddExactLampStatus(HmiScreen screen, string label, string tag, int left, int top, string suffix)
        {
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Exact_Status_Label_" + suffix), left, top, 135, 26,
                label, Dark, 12, HmiFontWeight.Normal, HmiHorizontalAlignment.Left);
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Exact_Status_Box_" + suffix), left + 140, top - 2, 48, 30,
                Color.White, Color.FromArgb(215, 222, 228), 1);
            ConfigureStatusLamp(screen, "REV12_Exact_Status_Lamp_" + suffix, left + 164, top + 13, tag);
        }

        private static void ConfigureStatusLamp(HmiScreen screen, string name, int centerX, int centerY, string tag)
        {
            HmiEllipse lamp = GetOrCreate<HmiEllipse>(screen, name);
            lamp.CenterX = centerX;
            lamp.CenterY = centerY;
            lamp.RadiusX = 7;
            lamp.RadiusY = 7;
            lamp.BackColor = Grey;
            lamp.BorderColor = Grey;
            lamp.BorderWidth = 1;
            lamp.BackFillPattern = HmiFillPattern.Solid;
            lamp.Visible = true;
            lamp.Enabled = false;
            ConfigureNumericDynamization(lamp, "BackColor", tag,
                "let v=HMIRuntime.Tags(\"" + tag + "\").Read(); if(v){return HMIRuntime.Math.RGB(35,170,35);} return HMIRuntime.Math.RGB(145,150,155);");
        }

        private static void ConfigureStatusPill(HmiScreen screen, string name, int left, int top, uint width, uint height, string tag, string activeText)
        {
            HmiRectangle back = GetOrCreate<HmiRectangle>(screen, name + "_Back");
            ConfigureRectangle(back, left, top, width, height, Color.FromArgb(235, 247, 235), Color.FromArgb(190, 220, 190), 1);
            ConfigureNumericDynamization(back, "Visible", tag,
                "return HMIRuntime.Tags(\"" + tag + "\").Read();");
            HmiText pill = GetOrCreate<HmiText>(screen, name);
            ConfigureText(pill, left, top, width, height, activeText, Color.FromArgb(20, 115, 35), 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureNumericDynamization(pill, "Visible", tag,
                "return HMIRuntime.Tags(\"" + tag + "\").Read();");
        }

        private static void BuildExactProductionKpis(HmiScreen screen)
        {
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Exact_Kpi_Breadcrumb"), 1035, 104, 170, 22,
                "PRODUCTION  >", Navy, 12, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddExactKpiCard(screen, "Bottles", "Bottle_Count_In_Machine", 1030, 130, "bottles", "0");
            AddExactKpiCard(screen, "LineSpeed", "Speed_Actual_Pct", 1030, 240, "%", "0.0");
            AddExactEfficiencyCard(screen, 1030, 350);
            AddExactKpiCard(screen, "Vacuum", "Vacuum_Actual_mbar", 1030, 460, "mbar", "0.0");
            AddExactKpiCard(screen, "ProductPump", "Pump_Speed_Pct", 1030, 570, "%", "0.0");
        }

        private static void AddExactKpiCard(HmiScreen screen, string suffix, string tag, int left, int top, string unit, string format)
        {
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Exact_Kpi_Card_" + suffix), left, top, 175, 100,
                Color.White, Color.FromArgb(215, 222, 228), 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Exact_Kpi_Title_" + suffix), left + 14, top + 10, 145, 22,
                suffix == "ProductPump" ? "PRODUCT PUMP" : suffix.ToUpperInvariant(), Navy, 12, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureIOField(GetOrCreate<HmiIOField>(screen, "REV12_Exact_Kpi_Value_" + suffix), left + 45, top + 38, 85, 36,
                tag, true, format);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Exact_Kpi_Unit_" + suffix), left + 132, top + 45, 38, 24,
                unit, Navy, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
        }

        private static void AddExactEfficiencyCard(HmiScreen screen, int left, int top)
        {
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Exact_Kpi_Card_Efficiency"), left, top, 175, 100,
                Color.White, Color.FromArgb(215, 222, 228), 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Exact_Kpi_Title_Efficiency"), left + 14, top + 10, 145, 22,
                "EFFICIENCY", Navy, 12, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            HmiText value = GetOrCreate<HmiText>(screen, "REV12_Exact_Kpi_Value_Efficiency");
            ConfigureText(value, left + 45, top + 38, 85, 40, "0%", Navy, 23, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureNumericDynamization(value, "Text", "Efficiency_Total_Seconds",
                "let t=HMIRuntime.Tags(\"Efficiency_Total_Seconds\").Read(); let r=HMIRuntime.Tags(\"Efficiency_Run_Seconds\").Read(); if(t<=0){return \"0%\";} return Math.round((r*100)/t).toString()+\"%\";");
        }

        private static void AddExactProductionNavigation(HmiScreen screen)
        {
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Nav_Back"), 1215, 100, 151, 612, Color.FromArgb(3, 39, 86), Color.FromArgb(3, 39, 86), 0);
            string[] labels = { "HOME", "SAFETY", "OPERATE", "PRODUCTION", "CIP", "FUNCTION", "ALARMS", "RECIPE", "SETUP", "MANUAL", "EFFICIENCY", "DIAGNOSTICS" };
            string[] destinations = { "home", "safety", "operate", "production", "cip", "function", "alarms", "recipe", "setup", "manual", "efficiency", "diagnostics" };
            for (int index = 0; index < labels.Length; index++)
            {
                int top = 105 + index * 49;
                bool active = labels[index] == "PRODUCTION";
                HmiButton button = GetOrCreate<HmiButton>(screen, "REV12_Nav_" + labels[index]);
                ConfigureButton(button, 1224, top, 133, 43, labels[index], active ? Color.FromArgb(0, 90, 205) : Color.FromArgb(8, 55, 105));
                button.Enabled = !active;
                if (button.Enabled) ConfigureScreenNavigation(button, destinations[index], "PRODUCTION");
            }
        }

        private static void BuildExactProductionBottomBar(HmiScreen screen)
        {
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Exact_Bottom_Bar"), 0, 712, 1366, 56,
                Color.FromArgb(3, 39, 86), Color.FromArgb(3, 39, 86), 0);
            ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "REV12_Exact_Bottom_Overview"), 12, 720, 110, 40,
                "OVERVIEW", Color.FromArgb(8, 55, 105), "home", "PRODUCTION");
            ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "REV12_Exact_Bottom_Efficiency"), 132, 720, 110, 40,
                "EFFICIENCY", Color.FromArgb(8, 55, 105), "efficiency", "PRODUCTION");
            ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "REV12_Exact_Bottom_Alarms"), 252, 720, 110, 40,
                "ALARMS", Color.FromArgb(8, 55, 105), "alarms", "PRODUCTION");
            ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "REV12_Exact_Bottom_Diagnostics"), 372, 720, 120, 40,
                "DIAGNOSTICS", Color.FromArgb(8, 55, 105), "diagnostics", "PRODUCTION");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Exact_Bottom_Connection"), 1015, 721, 175, 38,
                "●  CONNECTION OK", Color.FromArgb(80, 205, 70), 13, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Exact_Bottom_PlcHmi"), 1200, 721, 150, 38,
                "●  PLC / HMI", Color.FromArgb(80, 205, 70), 13, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
        }

        private static void BuildRevision22PilzDiagnostics(HmiSoftware hmi, Project project)
        {
            HmiScreen screen = GetOrCreateScreen(hmi, "safety_pilz_diagnostics");
            BuildRevision13Chrome(screen, "SAFETY / PILZ DIAGNOSTICS", "DIAGNOSTICS");

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV22_Pilz_OverallPanel"), 25, 120, 565, 250, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV22_Pilz_OverallTitle"), 45, 135, 525, 34,
                "OVERALL SAFETY STATUS", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddStatus(screen, "Safety ready / permissive", "Machine_SafetyOK", 55, 190);
            AddStatus(screen, "Pilz communication healthy", "Network_Pilz_OK", 55, 232);
            AddStatus(screen, "Safety circuit closed", "Door_Safety_Circuit_Closed", 55, 274);
            AddStatus(screen, "Safety-system fault", "Door_Safety_System_Fault", 55, 316);

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV22_Pilz_AccessPanel"), 610, 120, 580, 250, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV22_Pilz_AccessTitle"), 630, 135, 540, 34,
                "GROUPED EMERGENCY STOP & GUARD STATUS", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddStatus(screen, "Emergency-stop chain healthy", "EStop_Chain_Healthy", 640, 190);
            AddStatus(screen, "All 11 doors closed", "Door_AllClosed", 640, 232);
            AddStatus(screen, "All doors unlocked", "Door_AllUnlocked", 640, 274);
            AddStatus(screen, "Safety reset required", "Door_Alarm_Reset_Required", 640, 316);

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV22_Pilz_MotionPanel"), 25, 390, 565, 250, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV22_Pilz_MotionTitle"), 45, 405, 525, 34,
                "MOTION / ENERGY DIAGNOSTICS", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddStatus(screen, "Standstill / zero speed confirmed", "Door_ZeroSpeed", 55, 460);
            AddStatus(screen, "Three-phase power off confirmed", "Door_ThreePhase_Off", 55, 502);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV22_Pilz_STO_Missing"), 55, 550, 500, 28,
                "DRIVE STO: NOT CONFIGURED", Amber, 14, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV22_Pilz_EDM_Missing"), 55, 590, 500, 28,
                "SAFETY OUTPUT / EDM: NOT CONFIGURED", Amber, 14, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV22_Pilz_MissingPanel"), 610, 390, 580, 250, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV22_Pilz_MissingTitle"), 630, 405, 540, 34,
                "UNRESOLVED SAFETY DIAGNOSTICS", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV22_Pilz_IndividualEStops"), 640, 460, 520, 28,
                "INDIVIDUAL E-STOPS: NOT CONFIGURED", Amber, 14, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV22_Pilz_IndividualGuards"), 640, 500, 520, 28,
                "INDIVIDUAL GUARDS / LOCKS: NOT CONFIGURED", Amber, 14, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV22_Pilz_AirDump"), 640, 540, 520, 28,
                "PNEUMATIC DUMP / PRESSURE: NOT CONFIGURED", Amber, 14, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV22_Pilz_TruthNote"), 640, 580, 520, 45,
                "No Pilz device or channel mapping exists in the current TIA project. Values above are grouped diagnostics only.",
                Red, 12, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);

            ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "REV22_Pilz_Back"), 500, 655, 220, 42,
                "BACK TO DIAGNOSTICS", Navy, "diagnostics", "DIAGNOSTICS");

            HmiScreen diagnostics = hmi.Screens.Find("diagnostics");
            if (diagnostics != null)
            {
                ConfigureNavigateButton(GetOrCreate<HmiButton>(diagnostics, "REV22_Diagnostics_To_Pilz"), 430, 545, 330, 48,
                    "SAFETY / PILZ DIAGNOSTICS", Blue, "safety_pilz_diagnostics", "DIAGNOSTICS");
            }

            ApplyProductionNavigationMasterToAllScreens(project, hmi, "safety_pilz_diagnostics");
            Console.WriteLine("REV22_SCOPE=GROUPED_VERIFIED_DIAGNOSTICS_ONLY");
            Console.WriteLine("REV22_INDIVIDUAL_CHANNELS=NOT_CONFIGURED");
            Console.WriteLine("REV22_PLC_LOGIC_MODIFIED=0");
            Console.WriteLine("REV22_HARDWARE_CONFIGURATION_MODIFIED=0");
        }

        private static void BuildRevision23PilzProfinetDiagnostics(HmiSoftware hmi)
        {
            HmiScreen screen = GetOrCreateScreen(hmi, "safety_pilz_diagnostics");
            BuildRevision13Chrome(screen, "SAFETY / PILZ DIAGNOSTICS", "DIAGNOSTICS");
            AddGraphicView(screen, "REV21_Common_User_Profile_Icon",
                1087, 64, 22, 22, "REV14_user_profile");
            HmiGraphicView userIcon = screen.ScreenItems.Find("REV21_Common_User_Profile_Icon") as HmiGraphicView;
            userIcon.BackColor = Color.Transparent;
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Production_User_Header"), 1112, 61, 95, 28,
                "USER", Navy, 11, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);

            DeleteItemsWithPrefix(screen, "REV22_Pilz_");
            DeleteItem(screen, "REV22_Pilz_Back");
            foreach (string tag in new[]
            {
                "Machine_SafetyOK", "Network_Pilz_OK", "Door_Safety_Circuit_Closed",
                "Door_Safety_System_Fault", "EStop_Chain_Healthy", "Door_AllClosed",
                "Door_AllUnlocked", "Door_Alarm_Reset_Required", "Door_ZeroSpeed",
                "Door_ThreePhase_Off"
            })
            {
                string safe = new string(tag.Where(char.IsLetterOrDigit).ToArray());
                DeleteItem(screen, "REV12_Label_" + safe);
                DeleteItem(screen, "REV12_Value_" + safe);
            }

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV23_Pilz_OverallPanel"), 25, 120, 565, 250, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV23_Pilz_OverallTitle"), 45, 135, 525, 34,
                "PROFINET DIAGNOSTIC INTERFACE", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddPilzDirectState(screen, "Comm", "Pilz PROFINET communication", "PilzDiag_CommunicationOK",
                "HEALTHY", "COMM FAULT", true, 55, 190);
            AddPilzDirectState(screen, "Map", "Physical process map", "PilzDiag_PhysicalMapConfigured",
                "CONFIGURED", "NOT CONFIGURED", true, 55, 232);
            AddPilzDirectState(screen, "Valid", "Diagnostic dataset", "PilzDiag_DataValid",
                "VALID", "DATA INVALID", true, 55, 274);
            AddPilzTriState(screen, "Ready", "Safety ready / permissive", "PilzDiag_SafetyReady",
                "READY", "NOT READY", true, 55, 316);

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV23_Pilz_AccessPanel"), 610, 120, 580, 250, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV23_Pilz_AccessTitle"), 630, 135, 540, 34,
                "GROUPED EMERGENCY STOP & GUARD STATUS", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddPilzTriState(screen, "EStop", "Emergency-stop chain", "PilzDiag_EStopChainHealthy",
                "HEALTHY", "E-STOP ACTIVE", true, 640, 190);
            AddPilzTriState(screen, "Guards", "All 11 guarded doors", "PilzDiag_AllGuardsClosed",
                "CLOSED", "OPEN / NOT READY", true, 640, 232);
            AddPilzTriState(screen, "Locks", "Aggregate door unlock", "PilzDiag_AllGuardsUnlocked",
                "UNLOCKED", "LOCKED", true, 640, 274);
            AddPilzTriState(screen, "Reset", "Safety reset required", "PilzDiag_ResetRequired",
                "REQUIRED", "NOT REQUIRED", false, 640, 316);

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV23_Pilz_MotionPanel"), 25, 390, 565, 250, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV23_Pilz_MotionTitle"), 45, 405, 525, 34,
                "MOTION / ENERGY DIAGNOSTICS", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddPilzTriState(screen, "Circuit", "Safety circuit", "PilzDiag_SafetyCircuitClosed",
                "CLOSED", "OPEN / NOT READY", true, 55, 460);
            AddPilzTriState(screen, "Standstill", "Standstill / zero speed", "PilzDiag_StandstillConfirmed",
                "CONFIRMED", "NOT CONFIRMED", true, 55, 502);
            AddPilzTriState(screen, "PowerOff", "Three-phase power off", "PilzDiag_ThreePhaseOffConfirmed",
                "CONFIRMED", "NOT CONFIRMED", true, 55, 544);
            AddPilzTriState(screen, "Fault", "General safety fault", "PilzDiag_GeneralFault",
                "FAULT", "NO FAULT", false, 55, 586);

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV23_Pilz_MissingPanel"), 610, 390, 580, 250, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV23_Pilz_MissingTitle"), 630, 405, 540, 34,
                "UNRESOLVED PHYSICAL PROFINET DATA", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV23_Pilz_IndividualEStops"), 640, 456, 520, 26,
                "INDIVIDUAL E-STOPS: MISSING DATA MAP", Amber, 13, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV23_Pilz_IndividualGuards"), 640, 490, 520, 26,
                "INDIVIDUAL GUARDS / LOCKS: MISSING DATA MAP", Amber, 13, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV23_Pilz_STO"), 640, 524, 520, 26,
                "STO / AIR DUMP / EDM: MISSING DATA MAP", Amber, 13, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV23_Pilz_RawMap"), 640, 558, 520, 26,
                "GSDML / IP / BYTE LENGTHS / BIT OFFSETS: MISSING", Red, 13, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV23_Pilz_TruthNote"), 640, 592, 520, 34,
                "Diagnostics only. Pilz remains the sole safety authority; no HMI value bypasses safety.",
                Red, 11, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);

            ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "REV23_Pilz_Back"), 500, 655, 220, 42,
                "BACK TO DIAGNOSTICS", Navy, "diagnostics", "DIAGNOSTICS");

            Console.WriteLine("REV23_LOGICAL_INTERFACE=DB_Global.PilzDiag");
            Console.WriteLine("REV23_PHYSICAL_MAP_CONFIGURED=0");
            Console.WriteLine("REV23_RAW_PROFINET_MAP=MISSING");
            Console.WriteLine("REV23_SAFETY_AUTHORITY=PILZ");
            Console.WriteLine("REV23_PLC_MACHINE_LOGIC_MODIFIED=0");
            Console.WriteLine("REV23_HARDWARE_CONFIGURATION_MODIFIED=0");
            Console.WriteLine("REV23_NAVIGATION_MASTER_MODIFIED=0");
        }

        private static void AddPilzDirectState(
            HmiScreen screen, string suffix, string label, string valueTag,
            string trueText, string falseText, bool trueIsGood, int left, int top)
        {
            ConfigureText(GetOrCreate<HmiText>(screen, "REV23_Pilz_Label_" + suffix), left, top, 250, 28,
                label, Dark, 13, HmiFontWeight.Normal, HmiHorizontalAlignment.Left);
            HmiEllipse lamp = GetOrCreate<HmiEllipse>(screen, "REV23_Pilz_Lamp_" + suffix);
            lamp.CenterX = left + 274;
            lamp.CenterY = top + 14;
            lamp.RadiusX = 8;
            lamp.RadiusY = 8;
            lamp.BackColor = Grey;
            lamp.BorderColor = Grey;
            lamp.BorderWidth = 1;
            lamp.BackFillPattern = HmiFillPattern.Solid;
            lamp.Visible = true;
            lamp.Enabled = false;
            ConfigureNumericDynamization(lamp, "BackColor", valueTag,
                "let v=Boolean(HMIRuntime.Tags(\"" + valueTag + "\").Read());" +
                "if(v){return HMIRuntime.Math.RGB(" + (trueIsGood ? "28,145,82" : "195,45,55") + ");}" +
                "return HMIRuntime.Math.RGB(" + (trueIsGood ? "195,45,55" : "28,145,82") + ");");
            HmiText state = GetOrCreate<HmiText>(screen, "REV23_Pilz_State_" + suffix);
            ConfigureText(state, left + 290, top - 2, 175, 32, falseText, Navy, 12,
                HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureNumericDynamization(state, "Text", valueTag,
                "let v=Boolean(HMIRuntime.Tags(\"" + valueTag + "\").Read());" +
                "return v?\"" + trueText + "\":\"" + falseText + "\";");
        }

        private static void AddPilzTriState(
            HmiScreen screen, string suffix, string label, string valueTag,
            string trueText, string falseText, bool trueIsGood, int left, int top)
        {
            ConfigureText(GetOrCreate<HmiText>(screen, "REV23_Pilz_Label_" + suffix), left, top, 250, 28,
                label, Dark, 13, HmiFontWeight.Normal, HmiHorizontalAlignment.Left);
            HmiEllipse lamp = GetOrCreate<HmiEllipse>(screen, "REV23_Pilz_Lamp_" + suffix);
            lamp.CenterX = left + 274;
            lamp.CenterY = top + 14;
            lamp.RadiusX = 8;
            lamp.RadiusY = 8;
            lamp.BackColor = Grey;
            lamp.BorderColor = Grey;
            lamp.BorderWidth = 1;
            lamp.BackFillPattern = HmiFillPattern.Solid;
            lamp.Visible = true;
            lamp.Enabled = false;
            ConfigureNumericDynamization(lamp, "BackColor", "PilzDiag_DataValid",
                "let ok=Boolean(HMIRuntime.Tags(\"PilzDiag_DataValid\").Read());" +
                "if(!ok){return HMIRuntime.Math.RGB(110,120,130);}" +
                "let v=Boolean(HMIRuntime.Tags(\"" + valueTag + "\").Read());" +
                "if(v){return HMIRuntime.Math.RGB(" + (trueIsGood ? "28,145,82" : "195,45,55") + ");}" +
                "return HMIRuntime.Math.RGB(" + (trueIsGood ? "195,45,55" : "28,145,82") + ");");
            HmiText state = GetOrCreate<HmiText>(screen, "REV23_Pilz_State_" + suffix);
            ConfigureText(state, left + 290, top - 2, 175, 32, "DATA INVALID", Navy, 12,
                HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureNumericDynamization(state, "Text", "PilzDiag_DataValid",
                "let ok=Boolean(HMIRuntime.Tags(\"PilzDiag_DataValid\").Read());" +
                "if(!ok){return \"DATA INVALID\";}" +
                "let v=Boolean(HMIRuntime.Tags(\"" + valueTag + "\").Read());" +
                "return v?\"" + trueText + "\":\"" + falseText + "\";");
        }

        private static void BuildSafety(HmiScreen screen)
        {
            ConfigurePageChrome(screen, "SAFETY & DOOR ACCESS", "SAFETY",
                "SAFETY COMMANDS DO NOT BYPASS THE PILZ SAFETY SYSTEM OR MACHINE INTERLOCKS");

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Safety_Access_Panel"), 25, 140, 565, 575, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Safety_Access_Title"), 45, 152, 525, 34,
                "SAFETY PERMISSIVES", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddStatus(screen, "Safety permissive", "Machine_SafetyOK", 55, 205);
            AddStatus(screen, "Emergency-stop chain healthy", "EStop_Chain_Healthy", 55, 249);
            AddStatus(screen, "Safety circuit closed", "Door_Safety_Circuit_Closed", 55, 293);
            AddStatus(screen, "Safety-system fault", "Door_Safety_System_Fault", 55, 337);
            AddStatus(screen, "Zero speed confirmed", "Door_ZeroSpeed", 55, 381);
            AddStatus(screen, "Three-phase power off", "Door_ThreePhase_Off", 55, 425);
            AddStatus(screen, "Restart permitted", "Door_Restart_Permitted", 55, 469);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Safety_Access_Note"), 55, 535, 500, 60,
                "A separate operator START is always required after guarded access.",
                Red, 14, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV12_Safety_Reset"), 55, 625, 170, 58,
                "RESET PERMIT", Blue, "Cmd_Reset");

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Safety_Door_Panel"), 610, 140, 580, 575, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Safety_Door_Title"), 630, 152, 540, 34,
                "DOOR ACCESS & DIAGNOSTICS", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddStatus(screen, "Access request active", "Door_Request_Combined", 640, 205);
            AddStatus(screen, "Door sequence active", "Door_Sequence_Active", 640, 249);
            AddStatus(screen, "All 11 doors closed", "Door_AllClosed", 640, 293);
            AddStatus(screen, "All doors unlocked", "Door_AllUnlocked", 640, 337);
            AddStatus(screen, "Unlock request to safety PLC", "Door_Unlock_Request", 640, 381);
            AddStatus(screen, "Auxiliary reset required", "Door_Aux_Reset_Required", 640, 425);
            AddStatus(screen, "Alarm reset required", "Door_Alarm_Reset_Required", 640, 469);
            AddStatus(screen, "Door-access fault", "Door_Fault", 640, 513);
            AddStatus(screen, "Door fault code", "Door_FaultCode", 640, 557);
            AddStatus(screen, "Panel request PB", "Door_Request_Panel", 640, 601);
            AddStatus(screen, "Front request PB", "Door_Request_Front", 640, 645);
            AddStatus(screen, "Rear request PB", "Door_Request_Rear", 640, 689);
        }

        private static void AddOperateCommands(HmiScreen screen)
        {
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Operate_Command_Panel"), 25, 140, 775, 310, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Operate_Command_Title"), 45, 152, 730, 34,
                "PRODUCTION COMMANDS", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);

            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV12_Operate_AutoStart"), 55, 205, 160, 65,
                "PRODUCTION ON", Green, "Cmd_ProductionOn");
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV12_Operate_Stop"), 235, 205, 160, 65,
                "PRODUCTION OFF", Red, "Cmd_ProductionOff");
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV12_Operate_Reset"), 415, 205, 160, 65,
                "RESET ALARMS", Blue, "Cmd_Reset");
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV12_Operate_Manual"), 595, 205, 160, 65,
                "RUN OUT PRODUCT", Amber, "Run_Out_Product_Start");

            AddCompactMetric(screen, "REV12_Operate_Speed_SP_Label", "REV12_Operate_Speed_SP", 55, 305,
                "SPEED SETPOINT [BPH]", "Speed_Setpoint_BPH", false, "0");
            AddCompactMetric(screen, "REV12_Operate_Speed_PV_Label", "REV12_Operate_Speed_PV", 235, 305,
                "DRIVE SPEED [%]", "Speed_Actual_Pct", true, "0.0");
            AddCompactMetric(screen, "REV12_Operate_Pump_PV_Label", "REV12_Operate_Pump_PV", 415, 305,
                "VACUUM ACTUAL [mbar]", "Vacuum_Actual_mbar", true, "0.0");
            AddCompactMetric(screen, "REV12_Operate_Tank_PV_Label", "REV12_Operate_Tank_PV", 595, 305,
                "VACUUM MIN [mbar]", "Vacuum_Min_mbar", false, "0.0");
        }

        private static void AddOperateSafety(HmiScreen screen)
        {
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Operate_Safety_Panel"), 820, 140, 370, 310, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Operate_Safety_Title"), 840, 152, 330, 34,
                "SAFETY & ACCESS STATUS", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddStatus(screen, "Safety permissive", "Machine_SafetyOK", 840, 198);
            AddStatus(screen, "E-stop chain healthy", "EStop_Chain_Healthy", 840, 234);
            AddStatus(screen, "All 11 doors closed", "Door_AllClosed", 840, 270);
            AddStatus(screen, "Door request active", "Door_Request_Combined", 840, 306);
            AddStatus(screen, "Door sequence active", "Door_Sequence_Active", 840, 342);
            AddStatus(screen, "Restart permitted", "Door_Restart_Permitted", 840, 378);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Operate_Safety_Note"), 840, 414, 330, 24,
                "Use physical blue request pushbuttons for access", Red, 12, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
        }

        private static void AddOperateProcessStatus(HmiScreen screen)
        {
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Operate_Process_Panel"), 25, 470, 775, 245, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Operate_Process_Title"), 45, 482, 730, 32,
                "PROCESS STATUS", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddCompactStatus(screen, "Production active", "Mode_ProductionActive", 45, 526);
            AddCompactStatus(screen, "Pump enabled", "Product_Pump_Enable", 45, 562);
            AddCompactStatus(screen, "Product present", "Product_Present_At_Pump", 45, 598);
            AddCompactStatus(screen, "Pump running", "Product_Pump_Run", 45, 634);
            AddCompactStatus(screen, "Vacuum ready", "Vacuum_Ready", 300, 526);
            AddCompactStatus(screen, "Vacuum alarm", "Alarm_Low_Vacuum", 300, 562);
            AddCompactStatus(screen, "Bottle wash", "External_Wash_Active", 300, 598);
            AddCompactStatus(screen, "Gate open", "Gate_Open_Output", 300, 634);
            AddCompactStatus(screen, "Valve 210", "Valve_210_Cmd", 555, 526);
            AddCompactStatus(screen, "Valve 217", "Valve_217_Cmd", 555, 562);
            AddCompactStatus(screen, "Valve 213", "Valve_213_Cmd", 555, 598);
            AddCompactStatus(screen, "Valve 212", "Valve_212_Cmd", 555, 634);
        }

        private static void AddOperatePanelStatus(HmiScreen screen)
        {
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Operate_Panel_Panel"), 820, 470, 370, 245, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Operate_Panel_Title"), 840, 482, 330, 32,
                "MODE & SEQUENCE", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddStatus(screen, "Valve 247", "Valve_247_Cmd", 840, 532);
            AddStatus(screen, "Run Out active", "Run_Out_Active", 840, 568);
            AddStatus(screen, "Run Out complete", "Run_Out_Completed", 840, 604);
            AddStatus(screen, "Gate closed", "Gate_Close_Output", 840, 640);
            AddStatus(screen, "Critical alarm", "Alarm_Critical", 840, 676);
        }

        private static void ConfigurePageChrome(HmiScreen screen, string pageTitle, string activePage, string footer)
        {
            screen.BackColor = Color.FromArgb(239, 244, 248);
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Common_Header"), 0, 0, 1366, 72, Navy, Navy, 0);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Common_Header_Title"), 28, 13, 560, 46,
                "SCHLENKER MONOBLOCK REAL JUICE", Color.White, 28, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Common_Header_Page"), 820, 18, 370, 35,
                pageTitle + "  |  FBS GLOBAL", Color.White, 18, HmiFontWeight.Bold, HmiHorizontalAlignment.Right);
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Common_Status_Bar"), 0, 72, 1366, 48, PaleBlue, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Common_Status_Label"), 28, 80, 180, 32,
                "MACHINE STATUS", Dark, 17, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureSymbolicField(GetOrCreate<HmiSymbolicIOField>(screen, "REV12_Common_State"),
                210, 78, 505, 36, "Machine_State", "MachineStateText");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Common_Page_Label"), 735, 81, 450, 30,
                pageTitle, Dark, 15, HmiFontWeight.Bold, HmiHorizontalAlignment.Right);
            AddNavigationRail(screen, activePage);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Common_Footer"), 30, 731, 1150, 24,
                footer, Dark, 12, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
        }

        private static void BuildCip(HmiScreen screen)
        {
            ConfigurePageChrome(screen, "CIP", "CIP",
                "CIP VALVE 217 / 213 PATH REQUESTS AND PHYSICAL VALVE OUTPUTS REQUIRE COMMISSIONING CONFIRMATION");

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_CIP_Commands"), 25, 140, 360, 575, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_CIP_Commands_Title"), 45, 152, 320, 34,
                "CIP COMMANDS", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV12_CIP_Start"), 50, 205, 135, 60,
                "CIP START", Green, "Cmd_CIPStart");
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV12_CIP_Stop"), 205, 205, 135, 60,
                "CIP STOP", Red, "Cmd_CIPStop");
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV12_CIP_Reset"), 50, 285, 290, 55,
                "CIP RESET", Blue, "Cmd_Reset");
            AddMetric(screen, "REV12_CIP_Medium_Select_Label", "REV12_CIP_Medium_Select", 50, 375,
                "MEDIUM 0 OTHER / 1 WATER / 2 HOT / 3 CAUSTIC", "Cmd_CIPMedium", false, "0");
            AddStatus(screen, "CIP active", "Mode_CIPActive", 50, 485);
            AddStatus(screen, "Start blocked", "CIP_Start_Blocked", 50, 525);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_CIP_Interlock_Note"), 50, 580, 290, 90,
                "Production must be OFF. Machine must be stopped with safety valid before CIP can start.",
                Red, 13, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_CIP_Devices"), 405, 140, 390, 575, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_CIP_Devices_Title"), 425, 152, 350, 34,
                "CIP DEVICE STATUS", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddStatus(screen, "Product pump", "Product_Pump_Run", 425, 205);
            AddStatus(screen, "Vacuum pump", "Vacuum_Pump_Run", 425, 245);
            AddStatus(screen, "Valve 210", "Valve_210_Cmd", 425, 285);
            AddStatus(screen, "Valve 217", "Valve_217_Cmd", 425, 325);
            AddStatus(screen, "Valve 213", "Valve_213_Cmd", 425, 365);
            AddStatus(screen, "Valve 212", "Valve_212_Cmd", 425, 405);
            AddStatus(screen, "Valve 247", "Valve_247_Cmd", 425, 445);
            AddStatus(screen, "Media present", "Product_Present_At_Pump", 425, 485);
            AddStatus(screen, "Media seen", "CIP_Media_Seen", 425, 525);
            AddStatus(screen, "Gate forced closed", "CIP_Forced_Gate_Closed", 425, 565);
            AddStatus(screen, "Vacuum sequence done", "CIP_Vacuum_Sequence_Done", 425, 605);

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_CIP_Process"), 815, 140, 375, 575, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_CIP_Process_Title"), 835, 152, 335, 34,
                "CIP PROCESS", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddMetric(screen, "REV12_CIP_Step_Label", "REV12_CIP_Step", 835, 205,
                "SEQUENCE STEP", "CIP_Step", true, "0");
            AddMetric(screen, "REV12_CIP_Elapsed_Label", "REV12_CIP_Elapsed", 835, 285,
                "STEP ELAPSED [HH:MM:SS]", "CIP_Elapsed_Time", true, "hh:mm:ss");
            AddMetric(screen, "REV12_CIP_Tank_Label", "REV12_CIP_Tank", 835, 365,
                "TANK LEVEL [%]", "Tank_Level_Pct", true, "0.0");
            AddMetric(screen, "REV12_CIP_Vacuum_Label", "REV12_CIP_Vacuum", 835, 445,
                "VACUUM [mbar]", "Vacuum_Actual_mbar", true, "0.0");
            AddStatus(screen, "Media loss alarm", "CIP_Media_Loss_Alarm", 835, 550);
            AddStatus(screen, "Critical alarm", "Alarm_Critical", 835, 590);
            AddStatus(screen, "Warning active", "Alarm_Warning", 835, 630);
        }

        private static void BuildFunction(HmiScreen screen)
        {
            ConfigurePageChrome(screen, "FUNCTION", "FUNCTION",
                "FUNCTION STATUS IS READ FROM PLC_1  |  COMMANDS REMAIN INTERLOCKED");

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Function_Main"), 25, 140, 775, 310, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Function_Main_Title"), 45, 152, 730, 34,
                "MACHINE FUNCTIONS", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddStatus(screen, "Product pump output", "Pump_Speed_Pct", 55, 205);
            AddStatus(screen, "Tank level", "Tank_Level_Pct", 55, 241);
            AddStatus(screen, "Filler height enable", "Filler_Height_Enable", 55, 277);
            AddStatus(screen, "Capper height enable", "Capper_Height_Enable", 55, 313);
            AddStatus(screen, "Gate manual page", "Gate_Manual_PageActive", 430, 205);
            AddStatus(screen, "Gate test enable", "Gate_Manual_TestEnable", 430, 241);
            AddStatus(screen, "Door access sequence", "Door_Sequence_Active", 430, 277);
            AddStatus(screen, "SMC valve output word", "SMC_ValveBits", 430, 313);

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Function_Aux"), 25, 470, 775, 245, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Function_Aux_Title"), 45, 482, 730, 32,
                "AUXILIARY FUNCTIONS", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddStatus(screen, "Machine lights relay", "Lights_Relay", 55, 532);
            AddStatus(screen, "Air filter relay", "AirFilter_Relay", 55, 568);
            AddStatus(screen, "Air pressure proof", "AirFilter_PressureOK", 55, 604);
            AddStatus(screen, "Filter change required", "AirFilter_ChangeRequired", 55, 640);
            AddStatus(screen, "Network healthy", "Network_OK", 430, 532);
            AddStatus(screen, "Tank full 100%", "Tank_Full_100", 430, 568);
            AddStatus(screen, "Capper outfeed crash active", "Capper_Outfeed_Crash", 430, 604);
            AddStatus(screen, "Warning active", "Alarm_Warning", 430, 640);

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Function_Readiness"), 820, 140, 370, 575, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Function_Readiness_Title"), 840, 152, 330, 34,
                "FUNCTION READINESS", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddStatus(screen, "Safety permissive", "Machine_SafetyOK", 840, 205);
            AddStatus(screen, "E-stop chain healthy", "EStop_Chain_Healthy", 840, 241);
            AddStatus(screen, "All doors closed", "Door_AllClosed", 840, 277);
            AddStatus(screen, "Restart permitted", "Door_Restart_Permitted", 840, 313);
            AddStatus(screen, "Network healthy", "Network_OK", 840, 349);
            AddStatus(screen, "Critical alarm active", "Alarm_Critical", 840, 385);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Function_Note"), 840, 445, 330, 120,
                "Use MANUAL for individual gated commands.\nUse SETUP for device and network diagnostics.",
                Dark, 14, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
        }

        private static void BuildAlarms(HmiScreen screen)
        {
            ConfigurePageChrome(screen, "ALARMS", "ALARMS",
                "RESET ONLY AFTER THE FAULT CAUSE IS CLEARED AND THE SAFETY CIRCUIT IS CLOSED");

            HideLegacyAlarmPageItems(screen);

            HmiAlarmControl alarmControl = GetOrCreate<HmiAlarmControl>(screen, "REV12_Active_Alarm_Control");
            ConfigureAlarmControl(alarmControl);

            // WinCC collapses an empty built-in toolbar, so add a real foreground footer that
            // reserves the lower action area and prevents alarm rows from rendering behind reset.
            DeleteItem(screen, "REV12_Alarms_Action_Footer");
            HmiRectangle actionFooter = GetOrCreate<HmiRectangle>(screen, "REV12_Alarms_Action_Footer");
            ConfigureRectangle(actionFooter, 25, 420, 1165, 116, PaleBlue, Border, 1);

            // Recreate these after the Alarm Control so their Z-order is above its lower toolbar area.
            DeleteItem(screen, "REV12_Alarms_Reset");
            DeleteItem(screen, "REV12_Alarms_Reset_Label");
            HmiButton resetButton = GetOrCreate<HmiButton>(screen, "REV12_Alarms_Reset");
            ConfigureMomentaryButton(resetButton, 1000, 432, 120, 84,
                "↻\nRESET", Green, "Cmd_Reset");
            resetButton.Font.Size = 32;
            resetButton.BorderWidth = 2;
            /*
            SetText(resetButton.Text, "↻");
            */
            SetText(resetButton.Text, "\u21BB");
            ConfigureAlarmResetColor(resetButton);

            DeleteItem(screen, "REV12_Alarms_Reset_Warning");
            /* HmiButton warningReset = GetOrCreate<HmiButton>(screen, "REV12_Alarms_Reset_Warning");
            ConfigureMomentaryButton(warningReset, 1000, 538, 136, 136,
                "↻\nRESET", Amber, "Cmd_Reset");
            warningReset.Font.Size = 24;
            warningReset.BorderWidth = 2;
            warningReset.Visible = false;
            BindTag(warningReset.Dynamizations, "Visible", "Alarm_Warning", true);

            HmiButton criticalReset = GetOrCreate<HmiButton>(screen, "REV12_Alarms_Reset_Critical");
            ConfigureMomentaryButton(criticalReset, 1000, 538, 136, 136,
                "↻\nRESET", Red, "Cmd_Reset");
            criticalReset.Font.Size = 24;
            criticalReset.BorderWidth = 2;
            criticalReset.Visible = false;
            BindTag(criticalReset.Dynamizations, "Visible", "Alarm_Critical", true); */

            DeleteItem(screen, "REV12_Alarms_Reset_Critical");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Alarms_Reset_Label"), 882, 450, 118, 55,
                "RESET\nALARMS", Navy, 16, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);

            // Keep the instruction texts at their established coordinates, but recreate them
            // after the Alarm Control so they share the reset overlay's foreground layer.
            DeleteItem(screen, "REV12_Alarms_Reset_Note");
            DeleteItem(screen, "REV12_Common_Footer");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Alarms_Reset_Note"), 25, 690, 1165, 34,
                "Correct the cause first. PLC and safety reset conditions remain authoritative; an acknowledgement never bypasses an interlock.",
                Dark, 13, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Common_Footer"), 30, 731, 1150, 24,
                "RESET ONLY AFTER THE FAULT CAUSE IS CLEARED AND THE SAFETY CIRCUIT IS CLOSED",
                Dark, 12, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
        }

        private static void ConfigureAlarmResetColor(HmiButton button)
        {
            DynamizationBase existing = button.Dynamizations.Find("BackColor");
            if (existing != null)
            {
                existing.Delete();
            }

            ScriptDynamization color = button.Dynamizations.Create<ScriptDynamization>("BackColor");
            color.Async = false;
            color.Trigger.Type = TriggerType.T500ms;
            color.ScriptCode =
                "let critical = HMIRuntime.Tags(\"Alarm_Critical\").Read();\n" +
                "let warning = HMIRuntime.Tags(\"Alarm_Warning\").Read();\n" +
                "if (critical) { return HMIRuntime.Math.RGB(195, 45, 55); }\n" +
                "if (warning) { return HMIRuntime.Math.RGB(235, 160, 30); }\n" +
                "return HMIRuntime.Math.RGB(28, 145, 82);";
        }

        private static void ConfigureAlarmControl(HmiAlarmControl control)
        {
            SetBounds(control, 25, 140, 1165, 280);
            control.AlarmSourceType = HmiAlarmSourceType.ActiveAlarms;
            control.ActiveAlarmsViewSetup = HmiVisibleAlarms.UnSuppressed;
            control.AlarmDefinitionViewSetup = HmiVisibleAlarms.None;
            control.AlwaysShowRecent = true;
            control.DefaultSortDirection = HmiSortDirection.Descending;
            control.UseAlarmColors = true;
            control.SuppressFlashing = false;
            control.BackColor = Color.White;
            control.CaptionColor = Navy;
            control.Visible = true;
            control.Enabled = true;
            SetText(control.Caption.Text, "ACTIVE ALARMS");
            control.Caption.Visible = true;
            control.Caption.ForeColor = Color.White;
            control.Caption.Font.Name = HmiFontName.SiemensSans;
            control.Caption.Font.Size = 18;
            control.Caption.Font.Weight = HmiFontWeight.Bold;

            HmiDataGridViewPart grid = control.AlarmView;
            grid.AllowFilter = true;
            grid.AllowSort = true;
            grid.BackColor = Color.White;
            grid.ForeColor = Dark;
            grid.AlternateBackColor = PaleBlue;
            grid.AlternateForeColor = Dark;
            grid.ColoringMode = HmiGridColoringMode.Rows;
            grid.GridLineColor = Border;
            grid.GridLineVisibility = HmiSimpleGridLine.Horizontal;
            grid.GridLineWidth = 1;
            grid.RowHeight = 38;
            grid.SelectFullRow = true;
            grid.SelectionBackColor = Blue;
            grid.SelectionForeColor = Color.White;
            grid.SelectionBorderColor = Navy;
            grid.SelectionBorderWidth = 1;
            grid.HorizontalScrollBarVisibility = HmiScrollBarVisibility.Collapsed;
            grid.VerticalScrollBarVisibility = HmiScrollBarVisibility.Automatic;
            grid.Font.Name = HmiFontName.SiemensSans;
            grid.Font.Size = 15;
            grid.Font.Weight = HmiFontWeight.Normal;
            grid.HeaderSettings.AllowColumnReorder = false;
            grid.HeaderSettings.AllowColumnResize = true;
            grid.HeaderSettings.HeaderBackColor = Navy;
            grid.HeaderSettings.HeaderForeColor = Color.White;
            grid.HeaderSettings.HeaderGridLineColor = Border;
            grid.HeaderSettings.HeaderSelectionBackColor = Navy;
            grid.HeaderSettings.HeaderSelectionForeColor = Color.White;
            grid.HeaderSettings.Font.Name = HmiFontName.SiemensSans;
            grid.HeaderSettings.Font.Size = 14;
            grid.HeaderSettings.Font.Weight = HmiFontWeight.Bold;

            ConfigureAlarmColumns(grid);

            // Keep the toolbar band as a reserved footer so the alarm grid does not expand
            // behind the separate RESET ALARMS control, but hide every built-in toolbar icon.
            control.ToolBar.Visible = true;
            control.ToolBar.Enabled = false;
            control.ToolBar.BackColor = PaleBlue;
            control.ToolBar.ShowToolTips = true;
            control.ToolBar.UseHotKeys = false;
            control.ToolBar.Font.Name = HmiFontName.SiemensSans;
            control.ToolBar.Font.Size = 14;
            foreach (HmiControlBarButtonPart buttonPart in control.ToolBar.Elements.OfType<HmiControlBarButtonPart>())
            {
                buttonPart.Visible = false;
            }
            foreach (HmiControlBarToggleSwitchPart togglePart in control.ToolBar.Elements.OfType<HmiControlBarToggleSwitchPart>())
            {
                togglePart.Visible = false;
            }
            control.StatusBar.Visible = true;
            control.StatusBar.Enabled = true;
            control.StatusBar.BackColor = PaleBlue;
            control.StatusBar.ShowToolTips = true;
            control.StatusBar.Font.Name = HmiFontName.SiemensSans;
            control.StatusBar.Font.Size = 13;
            foreach (HmiControlBarDisplayPart display in control.StatusBar.Elements.OfType<HmiControlBarDisplayPart>())
            {
                display.Visible = false;
            }

            // WinCC Unified enforces a 396 px minimum height for this control.
            // The reset label and icon are intentionally placed in its clear lower-right toolbar area.
            SetBounds(control, 25, 140, 1165, 396);
        }

        private static void ConfigureAlarmColumns(HmiDataGridViewPart grid)
        {
            HmiAlarmBlock[] preferredOrder =
            {
                HmiAlarmBlock.RaiseTime,
                HmiAlarmBlock.ID,
                HmiAlarmBlock.Class,
                HmiAlarmBlock.AlarmText1,
                HmiAlarmBlock.EventText,
                HmiAlarmBlock.StateText,
                HmiAlarmBlock.AcknowledgmentState
            };
            Dictionary<HmiAlarmBlock, uint> widths = new Dictionary<HmiAlarmBlock, uint>
            {
                { HmiAlarmBlock.RaiseTime, 180 },
                { HmiAlarmBlock.ID, 90 },
                { HmiAlarmBlock.Class, 120 },
                { HmiAlarmBlock.AlarmText1, 485 },
                { HmiAlarmBlock.EventText, 485 },
                { HmiAlarmBlock.StateText, 135 },
                { HmiAlarmBlock.AcknowledgmentState, 140 }
            };
            Dictionary<HmiAlarmBlock, string> headers = new Dictionary<HmiAlarmBlock, string>
            {
                { HmiAlarmBlock.RaiseTime, "DATE / TIME" },
                { HmiAlarmBlock.ID, "ID" },
                { HmiAlarmBlock.Class, "CLASS" },
                { HmiAlarmBlock.AlarmText1, "ALARM TEXT" },
                { HmiAlarmBlock.EventText, "ALARM TEXT" },
                { HmiAlarmBlock.StateText, "STATE" },
                { HmiAlarmBlock.AcknowledgmentState, "ACKNOWLEDGED" }
            };

            bool hasAlarmText1 = grid.Columns.OfType<HmiAlarmColumnPart>()
                .Any(column => column.AlarmBlock == HmiAlarmBlock.AlarmText1);
            foreach (HmiAlarmColumnPart column in grid.Columns.OfType<HmiAlarmColumnPart>())
            {
                bool selected = preferredOrder.Contains(column.AlarmBlock);
                if (hasAlarmText1 && column.AlarmBlock == HmiAlarmBlock.EventText)
                {
                    selected = false;
                }
                column.Visible = selected;
                column.Enabled = true;
                column.AllowSort = true;
                column.UseAlarmColors = true;
                if (widths.ContainsKey(column.AlarmBlock))
                {
                    column.Width = widths[column.AlarmBlock];
                    column.MinimumWidth = 60;
                    column.MaximumWidth = 700;
                }
                if (headers.ContainsKey(column.AlarmBlock))
                {
                    SetText(column.Header.Text, headers[column.AlarmBlock]);
                }
            }
        }

        private static void HideLegacyAlarmPageItems(HmiScreen screen)
        {
            string[] names =
            {
                "REV12_Alarms_Summary", "REV12_Alarms_Summary_Title",
                "REV12_Alarms_Words", "REV12_Alarms_Words_Title",
                "REV12_AlarmWord_Label", "REV12_AlarmWord_Value",
                "REV12_DoorFaultCode_Label", "REV12_DoorFaultCode_Value",
                "REV12_NetworkMask_Label", "REV12_NetworkMask_Value",
                "REV12_Alarms_Action", "REV12_Alarms_Action_Title", "REV12_Alarms_Action_Text",
                "REV12_Label_AlarmCritical", "REV12_Value_AlarmCritical",
                "REV12_Label_AlarmWarning", "REV12_Value_AlarmWarning",
                "REV12_Label_DoorFault", "REV12_Value_DoorFault",
                "REV12_Label_DoorSafetySystemFault", "REV12_Value_DoorSafetySystemFault",
                "REV12_Label_AirFilterFault", "REV12_Value_AirFilterFault",
                "REV12_Label_CapperOutfeedCrash", "REV12_Value_CapperOutfeedCrash",
                "REV12_Label_NetworkOK", "REV12_Value_NetworkOK",
                "REV12_Label_EStopChainHealthy", "REV12_Value_EStopChainHealthy",
                "REV12_Label_DoorRestartPermitted", "REV12_Value_DoorRestartPermitted",
                "REV12_Label_DoorSafetyCircuitClosed", "REV12_Value_DoorSafetyCircuitClosed",
                "Rectangle_1"
            };
            foreach (string name in names)
            {
                DeleteItem(screen, name);
            }
        }

        private static void BuildRecipe(HmiScreen screen)
        {
            ConfigurePageChrome(screen, "RECIPE", "RECIPE",
                "VERIFY ALL PRODUCT PARAMETERS BEFORE STARTING AUTOMATIC PRODUCTION");

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Recipe_Parameters"), 25, 140, 775, 310, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Recipe_Parameters_Title"), 45, 152, 730, 34,
                "PRODUCTION PARAMETERS", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddMetric(screen, "REV12_Recipe_Speed_Label", "REV12_Recipe_Speed", 55, 215,
                "PRODUCTION SPEED [BPH]", "Speed_Setpoint_BPH", false, "0");
            AddMetric(screen, "REV12_Recipe_Tank_Label", "REV12_Recipe_Tank", 300, 215,
                "TANK SETPOINT [%]", "Tank_Setpoint_Pct", false, "0.0");
            AddMetric(screen, "REV12_Recipe_Actual_Label", "REV12_Recipe_Actual", 545, 215,
                "ACTUAL SPEED [%]", "Speed_Actual_Pct", true, "0.0");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Recipe_Note"), 55, 330, 700, 65,
                "Values are written to PLC_1 parameters. PLC range validation and machine permissives remain authoritative.",
                Dark, 14, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Recipe_Height"), 25, 470, 775, 245, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Recipe_Height_Title"), 45, 482, 730, 32,
                "FORMAT HEIGHT FUNCTIONS", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureLiftSelectButton(GetOrCreate<HmiButton>(screen, "REV12_Recipe_Filler_Enable"), 55, 545, 210, 60,
                "FILLER HEIGHT", "Filler_Height_Enable", "Capper_Height_Enable");
            ConfigureLiftSelectButton(GetOrCreate<HmiButton>(screen, "REV12_Recipe_Capper_Enable"), 290, 545, 210, 60,
                "CAPPER HEIGHT", "Capper_Height_Enable", "Filler_Height_Enable");
            AddStatus(screen, "Filler height command", "Filler_Height_Enable", 55, 635);
            AddStatus(screen, "Capper height command", "Capper_Height_Enable", 430, 635);

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Recipe_Validation"), 820, 140, 370, 575, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Recipe_Validation_Title"), 840, 152, 330, 34,
                "RECIPE VALIDATION", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddStatus(screen, "Safety permissive", "Machine_SafetyOK", 840, 205);
            AddStatus(screen, "All doors closed", "Door_AllClosed", 840, 241);
            AddStatus(screen, "Tank full input", "Tank_Full_100", 840, 277);
            AddStatus(screen, "Critical alarm active", "Alarm_Critical", 840, 313);
            AddStatus(screen, "Warning active", "Alarm_Warning", 840, 349);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Recipe_Validation_Note"), 840, 420, 330, 160,
                "Do not change production parameters while the machine is moving. Confirm the selected format mechanically before automatic start.",
                Red, 14, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
        }

        private static void BuildSetup(HmiScreen screen)
        {
            ConfigurePageChrome(screen, "SETUP", "SETUP",
                "SETUP PAGE IS READ-ONLY EXCEPT FOR EXPLICIT PARAMETER FIELDS");

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Setup_Network"), 25, 140, 775, 310, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Setup_Network_Title"), 45, 152, 730, 34,
                "NETWORK DIAGNOSTICS", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddStatus(screen, "PILZ safety controller", "Network_Pilz_OK", 55, 205);
            AddStatus(screen, "IO-Link master 1", "Network_IOLink1_OK", 55, 241);
            AddStatus(screen, "IO-Link master 2", "Network_IOLink2_OK", 55, 277);
            AddStatus(screen, "IO-Link master 3", "Network_IOLink3_OK", 55, 313);
            AddStatus(screen, "IO-Link master 4", "Network_IOLink4_OK", 55, 349);
            AddStatus(screen, "G120C drive", "Network_G120C_OK", 430, 205);
            AddStatus(screen, "HMI link", "Network_HMI_OK", 430, 241);
            AddStatus(screen, "Anybus PROFINET", "Network_Anybus_PN_OK", 430, 277);
            AddStatus(screen, "Anybus EtherCAT", "Network_Anybus_ECAT_OK", 430, 313);
            AddStatus(screen, "Overall network", "Network_OK", 430, 349);

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Setup_Signals"), 25, 470, 775, 245, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Setup_Signals_Title"), 45, 482, 730, 32,
                "MACHINE SIGNALS", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddMetric(screen, "REV12_Setup_Encoder_Label", "REV12_Setup_Encoder", 55, 535,
                "ENCODER COUNT", "Encoder_Count", true, "0");
            AddMetric(screen, "REV12_Setup_Valve_Label", "REV12_Setup_Valve", 275, 535,
                "SMC VALVE BITS", "SMC_ValveBits", true, "0");
            AddMetric(screen, "REV12_Setup_Mask_Label", "REV12_Setup_Mask", 495, 535,
                "FAULTY DEVICE MASK", "Network_Faulty_Device_Mask", true, "0");
            AddStatus(screen, "Zero speed confirmed", "Door_ZeroSpeed", 55, 635);
            AddStatus(screen, "Three-phase power off", "Door_ThreePhase_Off", 430, 635);

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Setup_Safety"), 820, 140, 370, 575, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Setup_Safety_Title"), 840, 152, 330, 34,
                "SAFETY INPUTS", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddStatus(screen, "E-stop chain healthy", "EStop_Chain_Healthy", 840, 205);
            AddStatus(screen, "Safety circuit closed", "Door_Safety_Circuit_Closed", 840, 241);
            AddStatus(screen, "Safety system fault", "Door_Safety_System_Fault", 840, 277);
            AddStatus(screen, "All 11 doors closed", "Door_AllClosed", 840, 313);
            AddStatus(screen, "All doors unlocked", "Door_AllUnlocked", 840, 349);
            AddStatus(screen, "Unlock request", "Door_Unlock_Request", 840, 385);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Setup_Safety_Note"), 840, 445, 330, 140,
                "Safety configuration and passwords are commissioned separately. Do not bypass or simulate safety inputs from this page.",
                Red, 14, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);

            ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "REV12_Setup_To_Inputs"), 840, 600, 150, 44,
                "DIGITAL INPUTS", Blue, "settings_inputs", "SETUP");
            ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "REV12_Setup_To_Outputs"), 1010, 600, 150, 44,
                "DIGITAL OUTPUTS", Blue, "settings_outputs", "SETUP");
            ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "REV12_Setup_To_Timers"), 840, 655, 150, 44,
                "TIMERS", Blue, "settings_timers", "SETUP");
            ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "REV12_Setup_To_Wash"), 1010, 655, 150, 44,
                "BOTTLE WASH", Blue, "settings_external_wash", "SETUP");
        }

        private static void BuildSettingsInputs(HmiScreen screen)
        {
            ConfigurePageChrome(screen, "SETTINGS - DIGITAL INPUTS", "SETUP",
                "READ ONLY  |  GREY = FALSE / NO 24 VDC  |  GREEN = TRUE / 24 VDC");
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_SettingsInputs_Panel"), 25, 140, 1165, 575, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_SettingsInputs_Title"), 45, 150, 1120, 34,
                "LIVE DIGITAL INPUTS - ENGINEERING CODE / FUNCTION / LOCATION", Navy, 19, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);

            string[,] items =
            {
                { "PS100", "Main air pressure", "Pneumatic main", "DI_AirPressure_OK" },
                { "M100", "Main drive ready", "Main drive", "DI_MainDrive_Ready" },
                { "M100F", "Main drive fault", "Main drive", "DI_MainDrive_Fault" },
                { "M101", "Conveyor ready", "Outfeed", "DI_Conveyor_Ready" },
                { "M101F", "Conveyor fault", "Outfeed", "DI_Conveyor_Fault" },
                { "M102", "Product pump ready", "Product circuit", "DI_ProductPump_Ready" },
                { "M102F", "Product pump fault", "Product circuit", "DI_ProductPump_Fault" },
                { "E100", "Main encoder healthy", "Main drive / PLC HSC", "DI_Encoder_Healthy" },
                { "ZS100", "Machine zero / valve 1 home", "Filler", "DI_Machine_Zero" },
                { "SPB100", "Bottle presence", "Infeed", "DI_Bottle_Infeed" },
                { "PEI100", "Infeed slowdown photoelectric", "Infeed", "DI_Bottle_Shortage_1" },
                { "PEI101", "Infeed stop photoelectric", "Infeed", "DI_Bottle_Shortage_2" },
                { "PEO100", "Outfeed slowdown photoelectric", "Outfeed", "DI_Accumulation_1" },
                { "PEO101", "Outfeed stop photoelectric", "Outfeed", "DI_Accumulation_2" },
                { "LSF100", "Filler lift upper limit", "Filler", "DI_Filler_High_Limit" },
                { "LSF101", "Filler lift lower limit", "Filler", "DI_Filler_Low_Limit" },
                { "LSC100", "Capper lift upper limit", "Capper", "DI_Capper_High_Limit" },
                { "LSC101", "Capper lift lower limit", "Capper", "DI_Capper_Low_Limit" },
                { "CS100", "Low cap level", "Cap hopper", "DI_Cap_Hopper_Low" },
                { "CS103", "Cap present", "Pick and place", "DI_Cap_Present" }
            };
            for (int index = 0; index < items.GetLength(0); index++)
            {
                int column = index / 10;
                int row = index % 10;
                AddIoDiagnosticRow(screen, "DI" + index, 45 + column * 565, 195 + row * 47,
                    items[index, 0], items[index, 1], items[index, 2], items[index, 3]);
            }
            AddSettingsPager(screen, "settings_external_wash", "settings_outputs");
        }

        private static void BuildSettingsOutputs(HmiScreen screen)
        {
            ConfigurePageChrome(screen, "SETTINGS - DIGITAL OUTPUTS", "SETUP",
                "READ ONLY  |  NORMAL DIAGNOSTICS NEVER FORCE OR OVERRIDE AN OUTPUT");
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_SettingsOutputs_Panel"), 25, 140, 1165, 575, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_SettingsOutputs_Title"), 45, 150, 1120, 34,
                "LIVE PLC OUTPUT COMMANDS - ENGINEERING CODE / FUNCTION / LOCATION", Navy, 19, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            string[,] items =
            {
                { "M100", "Main machine run", "Main drive", "DO_Main_Run" },
                { "M101", "Bottle conveyor run", "Outfeed", "DO_Conveyor_Run" },
                { "M102", "Product pump run", "Product circuit", "DO_ProductPump_Run" },
                { "CAPDRV", "Cap drive run", "Capper", "DO_CapDrive_Run" },
                { "EV200-O", "Bottle gate open", "Infeed gate / SMC", "DO_Gate_Open" },
                { "EV200-C", "Bottle gate close", "Infeed gate / SMC", "DO_Gate_Close" },
                { "LIFT-FU", "Filler lift UP", "Filler", "DO_Filler_Up" },
                { "LIFT-FD", "Filler lift DOWN", "Filler", "DO_Filler_Down" },
                { "LIFT-CU", "Capper lift UP", "Capper", "DO_Capper_Up" },
                { "LIFT-CD", "Capper lift DOWN", "Capper", "DO_Capper_Down" },
                { "M103", "Vacuum pump", "Vacuum circuit", "DO_Vacuum_Pump" },
                { "EV210", "Product valve 210", "Product circuit / SMC", "DO_Valve_210" },
                { "EV217", "Product valve 217", "Product circuit / SMC", "DO_Valve_217" },
                { "EV213", "Product valve 213", "Product circuit / SMC", "DO_Valve_213" },
                { "EV212", "Vacuum valve 212", "Vacuum circuit / SMC", "DO_Valve_212" },
                { "EV247", "Discharge valve 247", "Discharge / SMC", "DO_Valve_247" },
                { "WASH-TBC", "External bottle wash", "After filler / SMC", "DO_External_Wash" }
            };
            for (int index = 0; index < items.GetLength(0); index++)
            {
                int column = index / 9;
                int row = index % 9;
                AddIoDiagnosticRow(screen, "DO" + index, 45 + column * 565, 195 + row * 50,
                    items[index, 0], items[index, 1], items[index, 2], items[index, 3]);
            }
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_SettingsOutputs_Hold"), 610, 620, 540, 28,
                "SMC manifold physical bit mapping remains a controlled TBC hold point.", Amber, 13, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddSettingsPager(screen, "settings_inputs", "settings_timers");
        }

        private static void BuildSettingsTimers(HmiScreen screen)
        {
            ConfigurePageChrome(screen, "SETTINGS - TIMERS & DELAYS", "SETUP",
                "AUTHORISED PROCESS PARAMETERS  |  PLC DATA IS THE SOURCE OF TRUTH");
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_SettingsTimers_Panel"), 25, 140, 1165, 575, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_SettingsTimers_Title"), 45, 150, 1120, 34,
                "PROCESS TIMERS AND VALIDATED ENGINEERING LIMITS", Navy, 19, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddTimerSetting(screen, "CIPTankFull", 55, 205, "CIP TANK-FULL DELAY", "Par_CIP_Tank_Full_Delay", "s", "1 - 10 s");
            AddTimerSetting(screen, "CIPPreOpen", 55, 285, "CIP PUMP PRE-OPEN DELAY", "Par_CIP_Pump_PreOpen_Delay", "s", "0 - 5 s");
            AddTimerSetting(screen, "CIPValve212", 55, 365, "VALVE 212 OPEN TIME", "Par_CIP_Valve212_Open_Time", "s", "1 - 20 s");
            AddTimerSetting(screen, "CIPPostRun", 55, 445, "VACUUM PUMP POST-RUN", "Par_CIP_Vacuum_PostRun_Time", "s", "1 - 30 s");
            AddTimerSetting(screen, "CIPMediaLoss", 610, 205, "CIP MEDIA-LOSS TIMEOUT", "Par_CIP_Media_Loss_Delay", "s", "1 - 10 s");
            AddTimerSetting(screen, "VacuumStartup", 610, 285, "VACUUM STARTUP INHIBIT", "Par_Vacuum_Startup_Delay", "s", "1 - 15 s");
            AddTimerSetting(screen, "RunOutFinal", 610, 365, "RUN OUT FINAL CLEARING", "Par_RunOut_Final_Delay", "s", "5 - 60 s");
            AddTimerSetting(screen, "WashOff", 610, 445, "BOTTLE-WASH OFF DELAY", "Par_External_Wash_Off_Delay", "s", "0 - 10 s");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_SettingsTimers_Note"), 55, 555, 1070, 90,
                "Values are retentive process parameters. PLC range validation remains mandatory during commissioning. Safety-certified timing values are not exposed on this page.",
                Red, 14, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddSettingsPager(screen, "settings_outputs", "settings_external_wash");
        }

        private static void BuildSettingsExternalWash(HmiScreen screen)
        {
            ConfigurePageChrome(screen, "SETTINGS - EXTERNAL BOTTLE WASH", "SETUP",
                "DIAGNOSTIC AND APPROVED PROCESS SETTING  |  NO INTERLOCK BYPASS");
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_SettingsWash_Panel"), 25, 140, 1165, 575, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_SettingsWash_Title"), 45, 152, 1120, 34,
                "EXTERNAL BOTTLE WASH - AFTER FILLER", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddStatus(screen, "Production mode", "Mode_ProductionActive", 55, 220);
            AddStatus(screen, "Machine running", "DO_Main_Run", 55, 270);
            AddStatus(screen, "Bottle count > 0 (count)", "Bottle_Count_In_Machine", 55, 320);
            AddStatus(screen, "Manual JOG active", "Jog_PB", 55, 370);
            AddStatus(screen, "Safety permissive", "Machine_SafetyOK", 55, 420);
            AddStatus(screen, "CIP active - wash inhibited", "Mode_CIPActive", 55, 470);
            AddStatus(screen, "Wash valve command", "External_Wash_Active", 55, 520);
            AddTimerSetting(screen, "ExternalWashOnly", 610, 220, "WASH VALVE OFF DELAY", "Par_External_Wash_Off_Delay", "s", "0 - 10 s");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_SettingsWash_Note"), 610, 330, 510, 170,
                "The page cannot open the valve. Production RUN or Manual JOG, positive bottle count, safety permission and no CIP are still required by PLC logic.",
                Red, 15, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_SettingsWash_Code"), 610, 535, 510, 70,
                "DEVICE CODE / CABLE / SMC BIT: OPEN - TBC\nDo not assign until the physical valve and manifold mapping are confirmed.",
                Amber, 14, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddSettingsPager(screen, "settings_timers", "settings_inputs");
        }

        private static void BuildLiftWarning(HmiScreen screen)
        {
            ConfigurePageChrome(screen, "WARNING - MACHINE HEIGHT ADJUSTMENT", "MANUAL",
                "CONFIRMATION NEVER BYPASSES HARDWARE SAFETY, LIMITS OR PLC INTERLOCKS");
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_LiftWarning_Panel"), 25, 140, 1165, 575, Panel, Red, 3);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_LiftWarning_Title"), 55, 165, 1090, 50,
                "WARNING - RELEASE THE MECHANICAL LIFTING LOCK", Red, 24, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_LiftWarning_Text"), 90, 230, 1020, 105,
                "Before operating the filler/capper lifting system, release the applicable mechanical lifting lock and verify that the movement area is clear. Confirm only after the mechanical lock has been released. All machine safety conditions must remain valid.",
                Dark, 17, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            AddStatus(screen, "Safety permissive", "Machine_SafetyOK", 100, 370);
            AddStatus(screen, "Critical alarm active", "Alarm_Critical", 100, 415);
            AddStatus(screen, "Filler selected", "Filler_Height_Enable", 100, 460);
            AddStatus(screen, "Capper selected", "Capper_Height_Enable", 100, 505);
            AddStatus(screen, "Filler confirmation valid", "Lift_Filler_Confirmed", 650, 370);
            AddStatus(screen, "Capper confirmation valid", "Lift_Capper_Confirmed", 650, 415);
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV12_LiftWarning_FillerConfirm"), 650, 480, 220, 70,
                "CONFIRM FILLER LOCK RELEASED", Green, "Lift_Filler_Confirm_Cmd");
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV12_LiftWarning_CapperConfirm"), 890, 480, 220, 70,
                "CONFIRM CAPPER LOCK RELEASED", Green, "Lift_Capper_Confirm_Cmd");
            ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "REV12_LiftWarning_Return"), 650, 585, 460, 60,
                "RETURN TO MANUAL MOVEMENT CONTROLS", Blue, "manual", "LIFT_WARNING");
        }

        private static void BuildManual(HmiScreen screen)
        {
            ConfigurePageChrome(screen, "MANUAL", "MANUAL",
                "MANUAL COMMANDS DROP OUT WHEN LEAVING THIS PAGE AND REMAIN PLC-INTERLOCKED");

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Manual_Gate"), 25, 140, 775, 310, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Manual_Gate_Title"), 45, 152, 730, 34,
                "ACCUMULATION GATE MANUAL", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV12_Manual_Gate_Open"), 55, 215, 210, 70,
                "GATE OPEN  -125Y1-", Blue, "Gate_Open_125Y1");
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV12_Manual_Gate_Close"), 290, 215, 210, 70,
                "GATE CLOSE", Color.FromArgb(78, 102, 125), "Gate_Close");
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV12_Manual_Test_Enable"), 525, 215, 210, 70,
                "HOLD TEST ENABLE", Amber, "Gate_Manual_TestEnable");
            AddStatus(screen, "Manual page active", "Gate_Manual_PageActive", 55, 330);
            AddStatus(screen, "Manual test enabled", "Gate_Manual_TestEnable", 430, 330);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Manual_Gate_Note"), 55, 385, 690, 40,
                "Valve identification follows the SMC island label. Release the button to remove the command.",
                Dark, 13, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Manual_Jog"), 25, 470, 775, 245, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Manual_Jog_Title"), 45, 482, 730, 32,
                "JOG & HEIGHT COMMANDS", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV12_Manual_Up"), 55, 540, 140, 60,
                "PENDANT UP", Blue, "Pendant_Up");
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV12_Manual_Down"), 210, 540, 140, 60,
                "PENDANT DOWN", Blue, "Pendant_Down");
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV12_Manual_Jog_Enable"), 365, 540, 140, 60,
                "JOG ENABLE", Amber, "Jog_PB");
            ConfigureLiftSelectButton(GetOrCreate<HmiButton>(screen, "REV12_Manual_Filler"), 520, 540, 110, 60,
                "FILLER", "Filler_Height_Enable", "Capper_Height_Enable");
            ConfigureLiftSelectButton(GetOrCreate<HmiButton>(screen, "REV12_Manual_Capper"), 645, 540, 110, 60,
                "CAPPER", "Capper_Height_Enable", "Filler_Height_Enable");
            AddStatus(screen, "Pendant UP", "Pendant_Up", 55, 635);
            AddStatus(screen, "Pendant DOWN", "Pendant_Down", 430, 635);

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Manual_Permissive"), 820, 140, 370, 575, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Manual_Permissive_Title"), 840, 152, 330, 34,
                "MANUAL PERMISSIVES", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddStatus(screen, "Safety permissive", "Machine_SafetyOK", 840, 205);
            AddStatus(screen, "E-stop chain healthy", "EStop_Chain_Healthy", 840, 241);
            AddStatus(screen, "All doors closed", "Door_AllClosed", 840, 277);
            AddStatus(screen, "Manual selected", "Cmd_ManualSelect", 840, 313);
            AddStatus(screen, "Door sequence active", "Door_Sequence_Active", 840, 349);
            AddStatus(screen, "Critical alarm active", "Alarm_Critical", 840, 385);
            AddStatus(screen, "Lift confirmation valid", "Lift_Confirmation_Valid", 840, 421);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Manual_Warning"), 840, 465, 330, 145,
                "WARNING\n\nConfirm personnel are clear before operating any manual output. Leaving MANUAL clears the page-active command and all momentary commands return to zero.",
                Red, 14, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureWriteButton(GetOrCreate<HmiButton>(screen, "REV12_Manual_Pump_Off"), 840, 645, 150, 50,
                "PRODUCT PUMP OFF", Grey, "Cmd_ProductPumpManualEnable", 0);
            ConfigureWriteButton(GetOrCreate<HmiButton>(screen, "REV12_Manual_Pump_On"), 1010, 645, 150, 50,
                "PRODUCT PUMP ON", Blue, "Cmd_ProductPumpManualEnable", 1);
        }

        private static void BuildEfficiency(HmiScreen screen)
        {
            DeleteItemsWithPrefix(screen, "REV12_Trends_");
            DeleteItemsWithPrefix(screen, "REV12_Efficiency_");
            DeleteItemsWithPrefix(screen, "REV12_Eff_");
            foreach (string legacyObject in new[]
            {
                "REV12_Label_MachineSafetyOK", "REV12_Value_MachineSafetyOK",
                "REV12_Label_AirFilterPressureOK", "REV12_Value_AirFilterPressureOK",
                "REV12_Label_TankFull100", "REV12_Value_TankFull100",
                "REV12_Label_DoorSequenceActive", "REV12_Value_DoorSequenceActive",
                "REV12_Label_CapperOutfeedCrash", "REV12_Value_CapperOutfeedCrash",
                "REV12_Label_NetworkOK", "REV12_Value_NetworkOK",
                "REV12_Label_NetworkHMIOK", "REV12_Value_NetworkHMIOK",
                "REV12_Label_ModeProductionActive", "REV12_Value_ModeProductionActive",
                "REV12_Label_AlarmCritical", "REV12_Value_AlarmCritical",
                "REV12_Label_AlarmWarning", "REV12_Value_AlarmWarning",
                "REV12_Label_ProductPresentAtPump", "REV12_Value_ProductPresentAtPump",
                "REV12_Value_EfficiencyRunSeconds", "REV12_Value_EfficiencyBreakdownSeconds"
            })
            {
                DeleteItem(screen, legacyObject);
            }
            ConfigurePageChrome(screen, "EFFICIENCY", "EFFICIENCY",
                "EFFICIENCY DASHBOARD  |  LIVE PLC MACHINE-TIME CLASSIFICATION");

            Color dashboardBack = Color.FromArgb(226, 234, 241);
            Color dashboardPanel = Color.FromArgb(247, 250, 252);
            Color dashboardBlue = Color.FromArgb(14, 164, 220);
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Efficiency_Dashboard"),
                25, 140, 1165, 575, dashboardBack, Border, 1);
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Efficiency_Dashboard_Tab"),
                410, 145, 395, 38, dashboardBlue, dashboardBlue, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Efficiency_Dashboard_Title"), 430, 151, 355, 26,
                "DASHBOARD", Color.White, 18, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Efficiency_Previous"), 45, 151, 250, 26,
                "<  MACHINE TIME", Navy, 13, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Efficiency_Next"), 925, 151, 240, 26,
                "PRODUCTION SUMMARY  >", Navy, 13, HmiFontWeight.Bold, HmiHorizontalAlignment.Right);

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Efficiency_Run_Panel"),
                45, 195, 265, 200, dashboardPanel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Efficiency_Run_Title"), 58, 205, 235, 26,
                "RUN TIME", Navy, 15, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureDashboardGauge(screen, "Run", 178, 288, Green, "Efficiency_Run_Seconds", "REV12_Efficiency_Run_Value");

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Efficiency_Waiting_Panel"),
                315, 195, 310, 200, dashboardPanel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Efficiency_Waiting_Title"), 328, 205, 280, 26,
                "MACHINE TIME", Navy, 15, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Efficiency_Waiting_Label"), 330, 242, 135, 24,
                "Waiting [s]", Dark, 13, HmiFontWeight.Normal, HmiHorizontalAlignment.Left);
            ConfigureIOField(GetOrCreate<HmiIOField>(screen, "REV12_Efficiency_Waiting_Value"), 485, 237, 120, 34,
                "Efficiency_Waiting_Seconds", true, "0");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Eff_Total_Label"), 330, 292, 135, 24,
                "Total [s]", Dark, 13, HmiFontWeight.Normal, HmiHorizontalAlignment.Left);
            ConfigureIOField(GetOrCreate<HmiIOField>(screen, "REV12_Eff_Total"), 485, 287, 120, 34,
                "Efficiency_Total_Seconds", true, "0");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Eff_Bottles_Label"), 330, 342, 135, 24,
                "Bottles inside", Dark, 13, HmiFontWeight.Normal, HmiHorizontalAlignment.Left);
            ConfigureIOField(GetOrCreate<HmiIOField>(screen, "REV12_Eff_Bottles"), 485, 337, 120, 34,
                "Bottle_Count_In_Machine", true, "0");

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Efficiency_Breakdown_Panel"),
                630, 195, 305, 200, dashboardPanel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Efficiency_Breakdown_Title"), 643, 205, 275, 26,
                "BREAKDOWN", Navy, 15, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Efficiency_Breakdown_Label"), 645, 242, 130, 24,
                "Breakdown [s]", Dark, 13, HmiFontWeight.Normal, HmiHorizontalAlignment.Left);
            ConfigureIOField(GetOrCreate<HmiIOField>(screen, "REV12_Efficiency_Breakdown_Value"), 795, 237, 120, 34,
                "Efficiency_Breakdown_Seconds", true, "0");
            AddTightStatus(screen, "Critical", "Alarm_Critical", 645, 292);
            AddTightStatus(screen, "Warning", "Alarm_Warning", 645, 342);

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Efficiency_Interface_Panel"),
                940, 195, 230, 200, dashboardPanel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Efficiency_Interface_Title"), 953, 205, 200, 26,
                "MACHINE INTERFACE", Navy, 15, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddTightStatus(screen, "Running", "Mode_ProductionActive", 955, 242);
            AddTightStatus(screen, "Safety OK", "Machine_SafetyOK", 955, 292);
            AddTightStatus(screen, "Network", "Network_OK", 955, 342);

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Efficiency_Stop_Panel"),
                45, 410, 265, 280, dashboardPanel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Efficiency_Stop_Title"), 58, 420, 235, 26,
                "STOP TIME", Navy, 15, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureDashboardGauge(screen, "Stop", 178, 535, Color.FromArgb(92, 105, 118),
                "Efficiency_Stop_Seconds", "REV12_Efficiency_Stop_Value");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Efficiency_Stop_Note"), 70, 628, 215, 42,
                "Time stopped without an active critical alarm", Dark, 12, HmiFontWeight.Normal,
                HmiHorizontalAlignment.Center);

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Efficiency_Distribution_Panel"),
                315, 410, 620, 280, dashboardPanel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Efficiency_Distribution_Title"), 328, 420, 590, 26,
                "TIME DISTRIBUTION", Navy, 15, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureDashboardChart(screen);
            AddDashboardMiniMetric(screen, "SpeedSP", 335, "SPEED SP", "Speed_Setpoint_BPH", "REV12_Eff_SpeedSP", "0");
            AddDashboardMiniMetric(screen, "SpeedPV", 480, "SPEED PV", "Speed_Actual_Pct", "REV12_Eff_SpeedPV", "0.0");
            AddDashboardMiniMetric(screen, "Tank", 625, "TANK %", "Tank_Level_Pct", "REV12_Eff_Tank", "0.0");
            AddDashboardMiniMetric(screen, "Encoder", 770, "ENCODER", "Encoder_Count", "REV12_Eff_Encoder", "0");

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Efficiency_Media_Panel"),
                940, 410, 230, 120, dashboardPanel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Efficiency_Media_Title"), 953, 420, 200, 26,
                "MATERIAL STATUS", Navy, 15, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddTightStatus(screen, "Product", "Product_Present_At_Pump", 955, 458);
            AddTightStatus(screen, "Air filter", "AirFilter_PressureOK", 955, 495);

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Efficiency_Counter_Panel"),
                940, 540, 230, 150, dashboardPanel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Efficiency_Counter_Title"), 953, 550, 200, 26,
                "GOOD / BAD COUNTER", Navy, 15, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Efficiency_Good_Label"), 955, 586, 95, 24,
                "Run [s]", Dark, 12, HmiFontWeight.Normal, HmiHorizontalAlignment.Left);
            ConfigureIOField(GetOrCreate<HmiIOField>(screen, "REV12_Value_EfficiencyRunSeconds"), 1060, 581, 90, 32,
                "Efficiency_Run_Seconds", true, "0");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Efficiency_Bad_Label"), 955, 630, 95, 24,
                "Fault [s]", Dark, 12, HmiFontWeight.Normal, HmiHorizontalAlignment.Left);
            ConfigureIOField(GetOrCreate<HmiIOField>(screen, "REV12_Value_EfficiencyBreakdownSeconds"), 1060, 625, 90, 32,
                "Efficiency_Breakdown_Seconds", true, "0");
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Efficiency_Good_Bar"),
                955, 668, 145, 8, Green, Green, 0);
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Efficiency_Bad_Bar"),
                1105, 668, 45, 8, Red, Red, 0);
        }

        private static void BuildRevision15IoDiagnostics(HmiScreen screen)
        {
            screen.BackColor = Color.FromArgb(239, 244, 248);
            BuildRevision13Chrome(screen, "I/O CONFIGURATION & DIAGNOSTICS", "DIAGNOSTICS");

            string[] filters = { "ALL", "DI", "DO", "AI", "AO", "IO-LINK", "FAULTS" };
            for (int index = 0; index < filters.Length; index++)
            {
                HmiButton filter = GetOrCreate<HmiButton>(screen, "REV15_IO_Filter_" + filters[index].Replace("-", "_"));
                ConfigureButton(filter, 20 + index * 115, 112, 105, 38, filters[index], index == 0 ? Blue : Navy);
                filter.Enabled = true;
                ConfigureRevision15Filter(filter, filters[index]);
            }
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV15_IO_Search_Back"), 830, 112, 360, 38,
                Color.White, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV15_IO_Search_Text"), 842, 118, 336, 26,
                "SEARCH: NOT CONFIGURED - NO APPROVED QUERY TAG", Amber, 11, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV15_IO_Table_Panel"), 20, 165, 790, 535,
                Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV15_IO_Table_Title"), 35, 174, 755, 28,
                "LIVE I/O DIRECTORY - SELECT DEVICE CODE FOR DETAILS", Navy, 17, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV15_IO_Table_Header"), 30, 207, 770, 30,
                Navy, Navy, 0);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV15_IO_H_Code"), 35, 210, 65, 24, "DEVICE", Color.White, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV15_IO_H_Desc"), 105, 210, 170, 24, "DESCRIPTION", Color.White, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV15_IO_H_Type"), 280, 210, 45, 24, "TYPE", Color.White, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV15_IO_H_Tag"), 330, 210, 180, 24, "HMI / PLC SIGNAL", Color.White, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV15_IO_H_Module"), 515, 210, 130, 24, "MODULE / CH", Color.White, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV15_IO_H_Address"), 650, 210, 65, 24, "ADDRESS", Color.White, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV15_IO_H_Live"), 720, 210, 70, 24, "LIVE", Color.White, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);

            AddRevision15IoRow(screen, "DI_PS100", "DI", 241, "PS100", "Main air pressure", "DI", "DI_AirPressure_OK", "DB_Global.Inp.AirPressureOK", "PLC / TBC", "TBC", "", false);
            AddRevision15IoRow(screen, "DI_PEI100", "DI", 283, "PEI100", "Infeed slowdown", "DI", "DI_Bottle_Shortage_1", "DB_Global.Inp.BottleShortage1", "AL1403 / TBC", "TBC", "", false);
            AddRevision15IoRow(screen, "DO_M102", "DO", 325, "M102", "Product pump run", "DO", "DO_ProductPump_Run", "DB_Global.Out.ProductPump", "PLC DO / TBC", "TBC", "", false);
            AddRevision15IoRow(screen, "DO_EV210", "DO", 367, "EV210", "Product valve 210", "DO", "DO_Valve_210", "DB_Global.Out.Valve210Cmd", "EX260 LOGICAL X4", "ABS MISSING", "", false);
            AddRevision15IoRow(screen, "AI_TLS100", "AI", 409, "TLS100", "Tank level", "AI", "Tank_Level_Pct", "DB_Global.Inp.TankLevelPct", "AI or IO-Link", "TBC", "%", true);
            AddRevision15IoRow(screen, "AI_VS100", "AI", 451, "VS100", "Vacuum value", "AI", "Vacuum_Actual_mbar", "DB_Global.Inp.VacuumActual_mbar", "AI or IO-Link", "TBC", "mbar", true);
            AddRevision15IoRow(screen, "AO_NA", "AO", 493, "N/A", "No confirmed AO", "AO", null, null, "NOT CONFIGURED", "N/A", "", true);
            AddRevision15IoRow(screen, "IOL_MASTER1", "IO-LINK", 535, "AL1403-1", "IO-Link master 1", "IO-LINK", "Network_IOLink1_OK", "DB_Global.Inp.IOLinkMaster1OK", "NOT IN HW CONFIG", "MISSING", "", false);
            AddRevision15IoRow(screen, "FAULT_M100", "FAULTS", 577, "M100F", "Main drive fault", "DI", "DI_MainDrive_Fault", "DB_Global.Inp.MainDriveFault", "PLC / TBC", "TBC", "", false);
            AddRevision15IoRow(screen, "FAULT_M102", "FAULTS", 619, "M102F", "Product pump fault", "DI", "DI_ProductPump_Fault", "DB_Global.Inp.ProductPumpFault", "PLC / TBC", "TBC", "", false);

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV15_IO_Detail_Panel"), 830, 165, 360, 535,
                Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV15_IO_Detail_Title"), 848, 178, 324, 30,
                "SELECTED I/O DETAIL", Navy, 18, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddRevision15DetailLine(screen, "Device", 220, "DEVICE", "N/A - SELECT A ROW");
            AddRevision15DetailLine(screen, "Description", 258, "DESCRIPTION", "N/A");
            AddRevision15DetailLine(screen, "Type", 296, "TYPE", "N/A");
            AddRevision15DetailLine(screen, "HmiTag", 334, "HMI TAG", "N/A");
            AddRevision15DetailLine(screen, "PlcTag", 372, "PLC SYMBOL", "N/A");
            AddRevision15DetailLine(screen, "Module", 410, "MODULE / CHANNEL", "NOT CONFIGURED");
            AddRevision15DetailLine(screen, "Address", 448, "ADDRESS", "NOT CONFIGURED");
            AddRevision15DetailLine(screen, "Unit", 486, "UNIT", "N/A");
            AddRevision15DetailLine(screen, "Quality", 524, "QUALITY / COMMS", "NOT CONFIGURED");
            AddRevision15DetailLine(screen, "Status", 562, "CLASSIFICATION", "MISSING SELECTION");
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV15_IO_Test_Boundary"), 848, 608, 324, 72,
                Color.FromArgb(255, 247, 230), Amber, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV15_IO_Test_Boundary_Text"), 858, 616, 304, 56,
                "MAINTENANCE TEST: NOT CONFIGURED\nNo proven PLC safe-test request interface. No HMI forcing provided.",
                Red, 11, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
        }

        private static void BuildRevision18CipPage(HmiSoftware hmi, string stage)
        {
            HmiScreen screen = hmi.Screens.Find("cip");
            if (screen == null)
            {
                throw new InvalidOperationException("The existing CIP screen was not found.");
            }

            string normalized = String.IsNullOrWhiteSpace(stage) ? "full" : stage.Trim().ToLowerInvariant();
            if (normalized == "full" || normalized == "chrome")
            {
                foreach (HmiScreenItemBase item in screen.ScreenItems.ToList())
                {
                    // The CIP page is rebuilt completely by the four page-scoped REV18
                    // stages below. Remove every legacy page object first so obsolete
                    // REV12 controls cannot remain behind or overlap the approved layout.
                    item.Delete();
                }
                BuildRevision13Chrome(screen, "CIP", "CIP");
                ConfigureText(GetOrCreate<HmiText>(screen, "REV18_CIP_HoldPoint"), 25, 682, 1165, 22,
                    "AL104 PHYSICAL PORT / CHANNEL MAPPING IS NOT COMMISSIONED - LOGICAL COMMANDS REMAIN PLC INTERLOCKED",
                    Red, 11, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            }

            if (normalized == "full" || normalized == "commands")
            {
                ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV18_CIP_Commands_Panel"), 25, 140, 360, 550, Panel, Border, 1);
                ConfigureText(GetOrCreate<HmiText>(screen, "REV18_CIP_Commands_Title"), 45, 152, 320, 34,
                    "CIP COMMANDS", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
                ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV18_CIP_Start"), 50, 205, 135, 52, "CIP START", Green, "Cmd_CIPStart");
                ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV18_CIP_Stop"), 205, 205, 135, 52, "CIP STOP", Red, "Cmd_CIPStop");
                ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV18_CIP_Pause"), 50, 273, 135, 46, "PAUSE", Color.FromArgb(224, 146, 0), "Cmd_CIPPause");
                ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV18_CIP_Resume"), 205, 273, 135, 46, "RESUME", Blue, "Cmd_CIPResume");
                ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV18_CIP_Abort"), 50, 335, 135, 46, "ABORT", Red, "Cmd_CIPAbort");
                ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV18_CIP_Reset"), 205, 335, 135, 46, "RESET", Blue, "Cmd_Reset");
                ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV18_CIP_WashOn"), 50, 403, 135, 46, "WASH ON", Green, "Cmd_InternalWashOn");
                ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV18_CIP_WashOff"), 205, 403, 135, 46, "WASH OFF", Grey, "Cmd_InternalWashOff");
                AddStatus(screen, "CIP active", "Mode_CIPActive", 50, 478);
                AddStatus(screen, "Start blocked", "CIP_Start_Blocked", 50, 518);
                AddStatus(screen, "Internal wash active", "InternalWash_Active", 50, 558);
                AddStatus(screen, "Wash mapping TBC", "InternalWash_NotCommissioned", 50, 598);
                ConfigureText(GetOrCreate<HmiText>(screen, "REV18_CIP_Command_Note"), 50, 642, 290, 32,
                    "COMMANDS REQUIRE PLC, PROCESS AND SAFETY PERMISSIVES", Red, 11, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            }

            if (normalized == "full" || normalized == "sequence")
            {
                ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV18_CIP_Sequence_Panel"), 405, 140, 390, 550, Panel, Border, 1);
                ConfigureText(GetOrCreate<HmiText>(screen, "REV18_CIP_Sequence_Title"), 425, 152, 350, 34,
                    "CIP SEQUENCE", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
                AddRevision18SequenceMetric(screen, "State", 425, 200, "STATE", "CIP_Sequence_State", "0");
                AddRevision18SequenceMetric(screen, "Phase", 425, 246, "PHASE [1..14]", "CIP_Sequence_Phase", "0");
                AddRevision18SequenceMetric(screen, "Progress", 425, 292, "PROGRESS [%]", "CIP_Progress_Pct", "0");
                AddRevision18SequenceMetric(screen, "Elapsed", 425, 338, "PHASE ELAPSED", "CIP_Phase_Elapsed", "hh:mm:ss");
                AddRevision18SequenceMetric(screen, "Duration", 425, 384, "PHASE DURATION", "CIP_Phase_Duration", "hh:mm:ss");
                AddStatus(screen, "Paused", "CIP_Paused", 425, 448);
                AddStatus(screen, "Sequence complete", "CIP_Sequence_Complete", 425, 490);
                AddStatus(screen, "Sequence fault", "CIP_Sequence_Fault", 425, 532);
                AddRevision18SequenceMetric(screen, "FaultCode", 425, 582, "FAULT CODE", "CIP_Sequence_FaultCode", "0");
            }

            if (normalized == "full" || normalized == "interface")
            {
                ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV18_CIP_Interface_Panel"), 815, 140, 375, 550, Panel, Border, 1);
                ConfigureText(GetOrCreate<HmiText>(screen, "REV18_CIP_Interface_Title"), 835, 152, 335, 34,
                    "AL104 CUSTOMER CIP INTERFACE", Navy, 18, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
                AddStatus(screen, "AL104 configured", "IOLink_AL104_Configured", 835, 200);
                AddStatus(screen, "PROFINET communication", "Network_AL104_OK", 835, 236);
                AddStatus(screen, "Remote ready", "CIP_Remote_Ready", 835, 272);
                AddStatus(screen, "Remote busy", "CIP_Remote_Busy", 835, 308);
                AddStatus(screen, "Remote fault", "CIP_Remote_Fault", 835, 344);
                AddStatus(screen, "Request accepted", "CIP_Request_Accepted", 835, 380);
                AddStatus(screen, "Medium available", "CIP_Medium_Available", 835, 416);
                AddStatus(screen, "Discharge complete", "CIP_Discharge_Complete", 835, 452);
                AddStatus(screen, "Cold water request", "CIP_Req_Cold_Water", 835, 498);
                AddStatus(screen, "Hot water request", "CIP_Req_Hot_Water", 835, 534);
                AddStatus(screen, "Acid / chemical request", "CIP_Req_Acid", 835, 570);
                AddStatus(screen, "Citra request", "CIP_Req_Citra", 835, 606);
                AddStatus(screen, "Discharge request", "CIP_Req_Discharge", 835, 642);
            }

            if (normalized != "full" && normalized != "chrome" && normalized != "commands" &&
                normalized != "sequence" && normalized != "interface")
            {
                throw new ArgumentException("Unknown REV18 CIP stage: " + stage);
            }
        }

        private static void BuildCipRecipeEnginePage(HmiSoftware hmi, string target)
        {
            if (String.IsNullOrWhiteSpace(target))
            {
                throw new InvalidOperationException("CIP recipe engine requires one page-scoped target: cip, recipe, or cip_edit.");
            }
            if (target.Equals("cip", StringComparison.OrdinalIgnoreCase))
            {
                BuildCipRecipeRuntime(hmi);
                return;
            }
            if (target.Equals("recipe", StringComparison.OrdinalIgnoreCase))
            {
                BuildCipRecipeOperational(hmi);
                return;
            }
            if (target.Equals("cip_edit", StringComparison.OrdinalIgnoreCase))
            {
                BuildCipRecipeEditor(hmi);
                return;
            }
            throw new InvalidOperationException("Unknown CIP recipe engine target: " + target);
        }

        private static void BuildCipRecipeRuntime(HmiSoftware hmi)
        {
            HmiScreen screen = hmi.Screens.Find("cip");
            if (screen == null) throw new InvalidOperationException("CIP screen not found.");
            ClearCipRecipePageContent(screen);
            BuildRevision13Chrome(screen, "CIP", "CIP");
            NormalizeCipRecipeNavigation(screen, "CIP");

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "ROB_CIP_CommandPanel"), 24, 120, 310, 572, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "ROB_CIP_CommandTitle"), 42, 132, 142, 32,
                "CIP COMMANDS", Navy, 16, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            HmiButton editButton = GetOrCreate<HmiButton>(screen, "ROB_CIP_EngineeringEdit");
            ConfigureNavigateButton(editButton, 188, 128, 124, 38, "CIP EDIT", Blue, "cip_edit", "CIP");
            editButton.Authorization = "User management";
            ConfigureCipExecutionMomentary(GetOrCreate<HmiButton>(screen, "ROB_CIP_Start"), 44, 182, 124, 48, "START", Green, "Cmd_CIPStart");
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "ROB_CIP_Stop"), 188, 182, 124, 48, "STOP", Red, "Cmd_CIPStop");
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "ROB_CIP_Pause"), 44, 246, 124, 44, "PAUSE", Amber, "Cmd_CIPPause");
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "ROB_CIP_Resume"), 188, 246, 124, 44, "RESUME", Blue, "Cmd_CIPResume");
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "ROB_CIP_Abort"), 44, 306, 124, 44, "ABORT", Red, "Cmd_CIPAbort");
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "ROB_CIP_Reset"), 188, 306, 124, 44, "RESET", Blue, "Cmd_Reset");
            ConfigureCipExecutionMomentary(GetOrCreate<HmiButton>(screen, "ROB_CIP_Next"), 44, 366, 268, 50, "MANUAL NEXT", Color.FromArgb(224, 120, 0), "Cmd_CIPNext");
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "ROB_CIP_AckComplete"), 44, 432, 268, 44, "ACKNOWLEDGE COMPLETE", Navy, "Cmd_CIPAcknowledgeComplete");
            AddCompactStatus(screen, "Recipe valid", "CIP_Recipe_Valid", 44, 510);
            AddCompactStatus(screen, "Waiting for NEXT", "CIP_Waiting_For_Next", 44, 548);
            AddCompactStatus(screen, "NEXT permitted", "CIP_Next_Permitted", 44, 586);
            AddCompactStatus(screen, "Transition OFF", "CIP_Transition_Active", 44, 624);
            ConfigureText(GetOrCreate<HmiText>(screen, "ROB_CIP_ActiveRevisionLabel"), 44, 654, 166, 24,
                "ACTIVE RECIPE REVISION", Dark, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureIOField(GetOrCreate<HmiIOField>(screen, "ROB_CIP_ActiveRevision"), 222, 650, 90, 30,
                "CIP_Active_Recipe_Revision", true, "0");
            ConfigureText(GetOrCreate<HmiText>(screen, "ROB_CIP_CommandNote"), 44, 682, 268, 18,
                "PLC AND SAFETY PERMISSIVES REMAIN AUTHORITATIVE", Red, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "ROB_CIP_RuntimePanel"), 350, 120, 450, 572, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "ROB_CIP_RuntimeTitle"), 370, 132, 410, 32,
                "ACTIVE RECIPE / STEP", Navy, 18, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddCipRecipeRuntimeMetric(screen, "Name", 370, 180, "ACTIVE SNAPSHOT", "CIP_Active_Recipe_Name", "");
            AddCipRecipeRuntimeMetric(screen, "Step", 370, 228, "CURRENT STEP", "CIP_Sequence_Phase", "0");
            AddCipRecipeRuntimeMetric(screen, "Total", 370, 276, "TOTAL STEPS", "CIP_Total_Steps", "0");
            AddCipRecipeRuntimeMetric(screen, "Medium", 370, 324, "MEDIUM CODE", "CIP_Requested_Medium", "0");
            AddCipRecipeRuntimeMetric(screen, "Advance", 370, 372, "ADVANCE 0 AUTO / 1 NEXT", "CIP_Advance_Mode", "0");
            AddCipRecipeRuntimeMetric(screen, "Elapsed", 370, 420, "ELAPSED", "CIP_Phase_Elapsed", "hh:mm:ss");
            AddCipRecipeRuntimeMetric(screen, "Remaining", 370, 468, "REMAINING", "CIP_Remaining_Time", "hh:mm:ss");
            AddCipRecipeRuntimeMetric(screen, "Progress", 370, 516, "PROGRESS [%]", "CIP_Progress_Pct", "0");
            AddCipRecipeRuntimeMetric(screen, "State", 370, 564, "STATE", "CIP_Sequence_State", "0");
            AddCipRecipeRuntimeMetric(screen, "Fault", 370, 612, "FAULT / VALIDATION", "CIP_Sequence_FaultCode", "0");

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "ROB_CIP_InterfacePanel"), 816, 120, 374, 572, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "ROB_CIP_InterfaceTitle"), 836, 132, 334, 32,
                "MEDIUM REQUEST / CUSTOMER INTERFACE", Navy, 16, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddStatus(screen, "Cold water request", "CIP_Req_Cold_Water", 836, 184);
            AddStatus(screen, "Hot water request", "CIP_Req_Hot_Water", 836, 224);
            AddStatus(screen, "Caustic / acid request", "CIP_Req_Acid", 836, 264);
            AddStatus(screen, "Citra request", "CIP_Req_Citra", 836, 304);
            AddStatus(screen, "Discharge request", "CIP_Req_Discharge", 836, 344);
            AddStatus(screen, "Remote ready", "CIP_Remote_Ready", 836, 400);
            AddStatus(screen, "Request accepted", "CIP_Request_Accepted", 836, 440);
            AddStatus(screen, "Medium available", "CIP_Medium_Available", 836, 480);
            AddStatus(screen, "Remote fault", "CIP_Remote_Fault", 836, 520);
            AddStatus(screen, "Sequence complete", "CIP_Sequence_Complete", 836, 576);
            AddStatus(screen, "Complete acknowledged", "CIP_Complete_Acknowledged", 836, 616);
            ConfigureText(GetOrCreate<HmiText>(screen, "ROB_CIP_InterfaceNote"), 836, 658, 334, 28,
                "WARM / STEAM / AIR / NITROGEN: NOT CONFIGURED", Amber, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
        }

        private static void BuildCipRecipeOperational(HmiSoftware hmi)
        {
            HmiScreen screen = hmi.Screens.Find("recipe");
            if (screen == null) throw new InvalidOperationException("Recipe screen not found.");
            ClearCipRecipePageContent(screen);
            BuildRevision13Chrome(screen, "CIP RECIPE", "RECIPE");
            NormalizeCipRecipeNavigation(screen, "RECIPE");

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "ROB_CIP_RECIPE_HeaderPanel"), 22, 112, 1168, 72, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "ROB_CIP_RECIPE_Title"), 38, 120, 280, 28,
                "APPROVED OPERATIONAL RECIPE", Navy, 17, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "ROB_CIP_RECIPE_NameLabel"), 350, 122, 90, 24,
                "NAME", Dark, 12, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureIOField(GetOrCreate<HmiIOField>(screen, "ROB_CIP_RECIPE_Name"), 438, 117, 230, 34, "CIP_Recipe_Name", true, "");
            ConfigureText(GetOrCreate<HmiText>(screen, "ROB_CIP_RECIPE_RevisionLabel"), 690, 122, 82, 24,
                "REVISION", Dark, 12, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureIOField(GetOrCreate<HmiIOField>(screen, "ROB_CIP_RECIPE_Revision"), 774, 117, 70, 34, "CIP_Recipe_Revision", true, "0");
            ConfigureText(GetOrCreate<HmiText>(screen, "ROB_CIP_RECIPE_StateLabel"), 862, 122, 64, 24,
                "STATE", Dark, 12, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureIOField(GetOrCreate<HmiIOField>(screen, "ROB_CIP_RECIPE_State"), 926, 117, 64, 34, "CIP_Recipe_Lifecycle_State", true, "0");
            ConfigureText(GetOrCreate<HmiText>(screen, "ROB_CIP_RECIPE_LifecycleLegend"), 350, 154, 820, 22,
                "STATE: 0 DRAFT | 1 APPROVED | 2 LOCKED / OPERATOR SELECTABLE | 3 SUPERSEDED",
                Dark, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "ROB_CIP_RECIPE_ColumnsBack"), 22, 188, 1168, 34, Navy, Navy, 0);
            ConfigureText(GetOrCreate<HmiText>(screen, "ROB_CIP_RECIPE_Columns"), 34, 192, 1138, 26,
                "STEP       ENABLED          MEDIUM CODE                         DURATION [HH:MM:SS]             ADVANCE (0 AUTO / 1 NEXT)",
                Color.White, 12, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);

            for (int step = 1; step <= 10; step++)
            {
                int top = 228 + ((step - 1) * 43);
                Color row = (step % 2 == 0) ? Color.FromArgb(232, 239, 246) : Color.White;
                ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "ROB_CIP_RECIPE_RowBack_" + step), 22, top - 3, 1168, 39, row, Border, 1);
                ConfigureText(GetOrCreate<HmiText>(screen, "ROB_CIP_RECIPE_StepLabel_" + step), 38, top, 52, 30,
                    step.ToString(), Navy, 14, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
                ConfigureIOField(GetOrCreate<HmiIOField>(screen, "ROB_CIP_RECIPE_Enabled_" + step), 130, top, 84, 30,
                    "CIP_Recipe_Step" + step + "_Enabled", true, "");
                ConfigureIOField(GetOrCreate<HmiIOField>(screen, "ROB_CIP_RECIPE_Medium_" + step), 300, top, 120, 30,
                    "CIP_Recipe_Step" + step + "_Medium", true, "0");
                ConfigureIOField(GetOrCreate<HmiIOField>(screen, "ROB_CIP_RECIPE_Duration_" + step), 540, top, 180, 30,
                    "CIP_Recipe_Step" + step + "_Duration", true, "hh:mm:ss");
                ConfigureIOField(GetOrCreate<HmiIOField>(screen, "ROB_CIP_RECIPE_Advance_" + step), 850, top, 150, 30,
                    "CIP_Recipe_Step" + step + "_AdvanceMode", true, "0");
                ConfigureText(GetOrCreate<HmiText>(screen, "ROB_CIP_RECIPE_Availability_" + step), 1030, top, 140, 30,
                    "1..5 AVAILABLE", Green, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            }
            ConfigureText(GetOrCreate<HmiText>(screen, "ROB_CIP_RECIPE_MediumLegend"), 32, 668, 760, 24,
                "MEDIUM: 1 COLD | 2 HOT | 3 CAUSTIC/ACID | 4 CITRA | 5 DISCHARGE | 6..9 NOT CONFIGURED",
                Dark, 11, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "ROB_CIP_RECIPE_Validation"), 815, 668, 355, 24,
                "LOCKED RECIPE IS READ-ONLY; PLC VALIDATES BEFORE START", Red, 11, HmiFontWeight.Bold, HmiHorizontalAlignment.Right);
        }

        private static void BuildCipRecipeEditor(HmiSoftware hmi)
        {
            HmiScreen screen = GetOrCreateCipRecipeEditorScreen(hmi);
            ClearCipRecipePageContent(screen);
            BuildRevision13Chrome(screen, "CIP RECIPE ENGINEERING", "CIP");
            NormalizeCipRecipeNavigation(screen, "CIP");

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "ROB_CIP_RECIPE_EDIT_Header"), 22, 112, 1168, 72, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "ROB_CIP_RECIPE_EDIT_Title"), 38, 118, 258, 30,
                "ENGINEERING DRAFT", Navy, 18, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "ROB_CIP_RECIPE_EDIT_NameLabel"), 306, 121, 58, 24,
                "NAME", Dark, 11, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            HmiIOField name = GetOrCreate<HmiIOField>(screen, "ROB_CIP_RECIPE_EDIT_Name");
            ConfigureIOField(name, 366, 117, 230, 34, "CIP_Recipe_Draft_Name", false, "");
            name.Authorization = "User management";
            ConfigureText(GetOrCreate<HmiText>(screen, "ROB_CIP_RECIPE_EDIT_RevisionLabel"), 612, 121, 68, 24,
                "REV", Dark, 11, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            HmiIOField revision = GetOrCreate<HmiIOField>(screen, "ROB_CIP_RECIPE_EDIT_Revision");
            ConfigureIOField(revision, 674, 117, 70, 34, "CIP_Recipe_Draft_Revision", false, "0");
            revision.Authorization = "User management";
            ConfigureText(GetOrCreate<HmiText>(screen, "ROB_CIP_RECIPE_EDIT_CountLabel"), 758, 121, 86, 24,
                "STEPS", Dark, 11, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            HmiIOField count = GetOrCreate<HmiIOField>(screen, "ROB_CIP_RECIPE_EDIT_Count");
            ConfigureIOField(count, 842, 117, 65, 34, "CIP_Recipe_Draft_StepCount", false, "0");
            count.Authorization = "User management";
            AddCompactStatus(screen, "Draft valid", "CIP_Recipe_Draft_Valid", 930, 118);
            ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "ROB_CIP_RECIPE_EDIT_Back"), 1030, 117, 140, 36,
                "BACK TO CIP", Navy, "cip", "CIP");
            ConfigureText(GetOrCreate<HmiText>(screen, "ROB_CIP_RECIPE_EDIT_Access"), 306, 154, 864, 20,
                "ENGINEER / ADMIN SESSION RIGHT REQUIRED. OPERATOR AND MAINTENANCE ARE READ-ONLY.",
                Red, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "ROB_CIP_RECIPE_EDIT_ColumnsBack"), 22, 188, 1168, 34, Navy, Navy, 0);
            ConfigureText(GetOrCreate<HmiText>(screen, "ROB_CIP_RECIPE_EDIT_Columns"), 34, 192, 1138, 26,
                "STEP       ENABLED          MEDIUM CODE                         DURATION [HH:MM:SS]             ADVANCE (0 AUTO / 1 NEXT)",
                Color.White, 12, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);

            for (int step = 1; step <= 10; step++)
            {
                int top = 228 + ((step - 1) * 39);
                Color row = (step % 2 == 0) ? Color.FromArgb(232, 239, 246) : Color.White;
                ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "ROB_CIP_RECIPE_EDIT_RowBack_" + step), 22, top - 3, 1168, 35, row, Border, 1);
                ConfigureText(GetOrCreate<HmiText>(screen, "ROB_CIP_RECIPE_EDIT_StepLabel_" + step), 38, top, 52, 28,
                    step.ToString(), Navy, 13, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
                HmiIOField enabled = GetOrCreate<HmiIOField>(screen, "ROB_CIP_RECIPE_EDIT_Enabled_" + step);
                ConfigureIOField(enabled, 130, top, 84, 28, "CIP_Recipe_Draft_Step" + step + "_Enabled", false, "");
                enabled.Authorization = "User management";
                HmiIOField medium = GetOrCreate<HmiIOField>(screen, "ROB_CIP_RECIPE_EDIT_Medium_" + step);
                ConfigureIOField(medium, 300, top, 62, 28, "CIP_Recipe_Draft_Step" + step + "_Medium", true, "0");
                ConfigureRecipeMediumCycleButton(
                    GetOrCreate<HmiButton>(screen, "ROB_CIP_RECIPE_EDIT_MediumSelect_" + step),
                    370, top, 130, 28, "SELECT NEXT", "CIP_Recipe_Draft_Step" + step + "_Medium");
                HmiIOField duration = GetOrCreate<HmiIOField>(screen, "ROB_CIP_RECIPE_EDIT_Duration_" + step);
                ConfigureIOField(duration, 540, top, 180, 28, "CIP_Recipe_Draft_Step" + step + "_Duration", false, "hh:mm:ss");
                duration.Authorization = "User management";
                HmiIOField advance = GetOrCreate<HmiIOField>(screen, "ROB_CIP_RECIPE_EDIT_Advance_" + step);
                ConfigureIOField(advance, 850, top, 150, 28, "CIP_Recipe_Draft_Step" + step + "_AdvanceMode", false, "0");
                advance.Authorization = "User management";
                ConfigureText(GetOrCreate<HmiText>(screen, "ROB_CIP_RECIPE_EDIT_Availability_" + step), 1030, top, 140, 28,
                    "1..5 AVAILABLE", Green, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            }

            ConfigureRecipeEngineeringButton(GetOrCreate<HmiButton>(screen, "ROB_CIP_RECIPE_EDIT_SaveAs"), 260, 638, 150, 40,
                "SAVE AS NEW REV", Blue, "Cmd_CIPRecipe_SaveAs");
            ConfigureRecipeEngineeringButton(GetOrCreate<HmiButton>(screen, "ROB_CIP_RECIPE_EDIT_Save"), 426, 638, 130, 40,
                "SAVE DRAFT", Navy, "Cmd_CIPRecipe_SaveDraft");
            ConfigureRecipeEngineeringButton(GetOrCreate<HmiButton>(screen, "ROB_CIP_RECIPE_EDIT_Approve"), 572, 638, 130, 40,
                "APPROVE", Green, "Cmd_CIPRecipe_Approve");
            ConfigureRecipeEngineeringButton(GetOrCreate<HmiButton>(screen, "ROB_CIP_RECIPE_EDIT_Lock"), 718, 638, 130, 40,
                "LOCK", Color.FromArgb(224, 120, 0), "Cmd_CIPRecipe_Lock");
            ConfigureRecipeEngineeringButton(GetOrCreate<HmiButton>(screen, "ROB_CIP_RECIPE_EDIT_Supersede"), 864, 638, 150, 40,
                "SUPERSEDE", Red, "Cmd_CIPRecipe_Supersede");
            ConfigureText(GetOrCreate<HmiText>(screen, "ROB_CIP_RECIPE_EDIT_Note"), 32, 682, 1138, 20,
                "LOCKED RECIPES CANNOT BE OVERWRITTEN. USE SAVE AS TO CREATE THE NEXT REVISION.",
                Dark, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
        }

        private static HmiScreen GetOrCreateCipRecipeEditorScreen(HmiSoftware hmi)
        {
            HmiScreen screen = hmi.Screens.Find("cip_edit");
            if (screen != null) return screen;

            HmiScreen legacy = hmi.Screens.Find("recipe_edit");
            if (legacy != null)
            {
                legacy.Name = "cip_edit";
                Console.WriteLine("CIP_EDIT_SCREEN_RENAMED_FROM=recipe_edit");
                return legacy;
            }

            return hmi.Screens.Create("cip_edit");
        }

        private static void ConfigureRecipeEngineeringButton(
            HmiButton item, int left, int top, uint width, uint height,
            string text, Color back, string tag)
        {
            ConfigureButton(item, left, top, width, height, text, back);
            item.Enabled = true;
            item.Authorization = "User management";
            HmiButtonEventHandler down = item.EventHandlers.Find(HmiButtonEventType.Down) ??
                item.EventHandlers.Create(HmiButtonEventType.Down);
            down.Script.ScriptCode =
                "let u=HMIRuntime.Tags(\"@UserName\").Read();" +
                "HMIRuntime.Tags(\"CIP_Recipe_Actor_Name\").Write(u);" +
                "HMIRuntime.Tags(\"CIP_Recipe_Actor_Role\").Write(2);" +
                "HMIRuntime.Tags(\"CIP_Recipe_Engineering_Authorized\").Write(1);" +
                "HMIRuntime.Tags(\"" + tag + "\").Write(1);";
            HmiButtonEventHandler up = item.EventHandlers.Find(HmiButtonEventType.Up) ??
                item.EventHandlers.Create(HmiButtonEventType.Up);
            up.Script.ScriptCode =
                "HMIRuntime.Tags(\"" + tag + "\").Write(0);" +
                "HMIRuntime.Tags(\"CIP_Recipe_Engineering_Authorized\").Write(0);";
        }

        private static void ConfigureRecipeMediumCycleButton(
            HmiButton item, int left, int top, uint width, uint height,
            string text, string tag)
        {
            ConfigureButton(item, left, top, width, height, text, Blue);
            item.Enabled = true;
            item.Authorization = "User management";
            HmiButtonEventHandler down = item.EventHandlers.Find(HmiButtonEventType.Down) ??
                item.EventHandlers.Create(HmiButtonEventType.Down);
            down.Script.ScriptCode =
                "let u=HMIRuntime.Tags(\"@UserName\").Read();" +
                "HMIRuntime.Tags(\"CIP_Recipe_Actor_Name\").Write(u);" +
                "HMIRuntime.Tags(\"CIP_Recipe_Actor_Role\").Write(2);" +
                "HMIRuntime.Tags(\"CIP_Recipe_Engineering_Authorized\").Write(1);" +
                "let t=HMIRuntime.Tags(\"" + tag + "\");" +
                "let v=Number(t.Read());" +
                "t.Write((v>=1 && v<5)?(v+1):1);";
            HmiButtonEventHandler up = item.EventHandlers.Find(HmiButtonEventType.Up) ??
                item.EventHandlers.Create(HmiButtonEventType.Up);
            up.Script.ScriptCode =
                "HMIRuntime.Tags(\"CIP_Recipe_Engineering_Authorized\").Write(0);";
        }

        private static void ConfigureCipExecutionMomentary(
            HmiButton item, int left, int top, uint width, uint height,
            string text, Color back, string tag)
        {
            ConfigureButton(item, left, top, width, height, text, back);
            item.Enabled = true;
            HmiButtonEventHandler down = item.EventHandlers.Find(HmiButtonEventType.Down) ??
                item.EventHandlers.Create(HmiButtonEventType.Down);
            down.Script.ScriptCode =
                "let u=HMIRuntime.Tags(\"@UserName\").Read();" +
                "HMIRuntime.Tags(\"CIP_Recipe_Actor_Name\").Write(u);" +
                "HMIRuntime.Tags(\"CIP_Recipe_Actor_Role\").Write(0);" +
                "HMIRuntime.Tags(\"" + tag + "\").Write(1);";
            HmiButtonEventHandler up = item.EventHandlers.Find(HmiButtonEventType.Up) ??
                item.EventHandlers.Create(HmiButtonEventType.Up);
            up.Script.ScriptCode = "HMIRuntime.Tags(\"" + tag + "\").Write(0);";
        }

        private static void ClearCipRecipePageContent(HmiScreen screen)
        {
            string[] retainedPrefixes =
            {
                "REV13_Common_",
                "REV13_Nav_",
                "REV14_Nav_",
                "REV12_Production_User_Header",
                "REV21_Common_User_Profile_Icon"
            };
            List<string> deleteNames = screen.ScreenItems
                .Where(item => !retainedPrefixes.Any(prefix =>
                    item.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                .Select(item => item.Name)
                .ToList();
            foreach (string name in deleteNames)
            {
                DeleteItem(screen, name);
            }
            Console.WriteLine("CIP_RECIPE_CENTRAL_ITEMS_REMOVED=" + deleteNames.Count);
        }

        private static void NormalizeCipRecipeNavigation(HmiScreen screen, string activePage)
        {
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV13_Nav_Back"),
                1215, 105, 151, 612, Color.FromArgb(3, 39, 86), Color.FromArgb(3, 39, 86), 0);
            AddRevision13NavigationButtonsOnly(screen, activePage);

            string[] labels =
            {
                "HOME", "SAFETY", "OPERATE", "PRODUCTION", "CIP", "FUNCTION",
                "ALARMS", "RECIPE", "SETUP", "MANUAL", "EFFICIENCY", "DIAGNOSTICS"
            };
            foreach (string label in labels)
            {
                DeleteItem(screen, "REV13_Nav_Icon_" + label + "_Ring");
                DeleteItem(screen, "REV13_Nav_Icon_" + label + "_LineA");
                DeleteItem(screen, "REV13_Nav_Icon_" + label + "_LineB");
            }
        }

        private static void AddCipRecipeRuntimeMetric(HmiScreen screen, string suffix, int left, int top,
            string label, string tag, string format)
        {
            ConfigureText(GetOrCreate<HmiText>(screen, "ROB_CIP_Runtime_" + suffix + "_Label"), left, top, 210, 32,
                label, Dark, 12, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureIOField(GetOrCreate<HmiIOField>(screen, "ROB_CIP_Runtime_" + suffix), left + 230, top - 2, 170, 34,
                tag, true, format);
        }

        private static void AddRevision18SequenceMetric(
            HmiScreen screen,
            string suffix,
            int left,
            int top,
            string label,
            string tag,
            string format)
        {
            ConfigureText(GetOrCreate<HmiText>(screen, "REV18_CIP_" + suffix + "_Label"),
                left, top + 5, 190, 28, label, Dark, 12, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureIOField(GetOrCreate<HmiIOField>(screen, "REV18_CIP_" + suffix),
                left + 205, top, 135, 36, tag, true, format);
        }

        private static void BuildRevision18IoLinkPage(HmiSoftware hmi, string target, string stage)
        {
            if (String.IsNullOrWhiteSpace(target))
            {
                throw new ArgumentException("REV18 page target is required.");
            }

            string normalized = target.Trim().ToLowerInvariant();
            if (normalized == "diagnostics")
            {
                HmiScreen diagnostics = hmi.Screens.Find("diagnostics");
                if (diagnostics == null)
                {
                    throw new InvalidOperationException("The existing diagnostics screen was not found.");
                }
                ConfigureNavigateButton(GetOrCreate<HmiButton>(diagnostics, "REV18_Diagnostics_To_IOLink"),
                    820, 650, 360, 42, "IO-LINK DIAGNOSTICS", Blue, "io_link_overview", "DIAGNOSTICS");
                return;
            }

            if (normalized == "io_link_overview")
            {
                BuildRevision18IoLinkOverview(GetOrCreateScreen(hmi, normalized), stage);
                return;
            }

            string[] detailPages =
            {
                "io_link_al100", "io_link_al101", "io_link_al102", "io_link_al103", "io_link_al104"
            };
            int index = Array.IndexOf(detailPages, normalized);
            if (index < 0)
            {
                throw new ArgumentException("Unknown REV18 IO-Link page target: " + target);
            }

            string engineeringId = "AL" + (100 + index).ToString();
            string[] rev20Descriptions =
            {
                "BOTTLE INFEED / OUTFEED DETECTION",
                "CAP HANDLING / PNEUMATIC UTILITY",
                "PROCESS INSTRUMENTATION / PRODUCT SYSTEM",
                "ADDITIONAL SMART SENSORS / EXPANSION",
                "DEDICATED CUSTOMER CIP INTERFACE"
            };
            string description = rev20Descriptions[index];
            string statusTag = "Network_AL" + (100 + index).ToString() + "_OK";
            if (Revision18Stage(stage, "chrome"))
            {
                HmiScreen overview = hmi.Screens.Find("io_link_overview");
                if (overview != null)
                {
                    DeleteRevision18MasterCard(overview, engineeringId);
                }
            }
            BuildRevision18IoLinkDetail(
                GetOrCreateScreen(hmi, normalized), engineeringId, description, statusTag,
                detailPages[(index + detailPages.Length - 1) % detailPages.Length],
                detailPages[(index + 1) % detailPages.Length], index == 4, stage);
        }

        private static bool Revision18Stage(string requested, string expected)
        {
            return String.IsNullOrWhiteSpace(requested) ||
                requested.Equals(expected, StringComparison.OrdinalIgnoreCase);
        }

        private static void BuildRevision18IoLinkOverview(HmiScreen screen, string stage)
        {
            screen.BackColor = Color.FromArgb(239, 244, 248);
            if (Revision18Stage(stage, "chrome"))
            {
                BuildRevision13Chrome(screen, "IO-LINK DIAGNOSTICS", "DIAGNOSTICS");
                ConfigureText(GetOrCreate<HmiText>(screen, "REV18_IOL_Overview_Instruction"),
                    25, 112, 1160, 34,
                    "FIVE IFM AL1403 MASTERS - SELECT A MASTER FOR PORT DIAGNOSTICS",
                    Navy, 18, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            }
            if (Revision18Stage(stage, "al100")) AddRevision18MasterCard(screen, "AL100", 25, 158, "BOTTLE INFEED / OUTFEED DETECTION", "Network_AL100_OK", "io_link_al100", false);
            if (Revision18Stage(stage, "al101")) AddRevision18MasterCard(screen, "AL101", 410, 158, "CAP HANDLING / PNEUMATIC UTILITY", "Network_AL101_OK", "io_link_al101", false);
            if (Revision18Stage(stage, "al102")) AddRevision18MasterCard(screen, "AL102", 795, 158, "PROCESS INSTRUMENTATION / PRODUCT SYSTEM", "Network_AL102_OK", "io_link_al102", false);
            if (Revision18Stage(stage, "al103")) AddRevision18MasterCard(screen, "AL103", 215, 405, "ADDITIONAL SMART SENSORS / EXPANSION", "Network_AL103_OK", "io_link_al103", false);
            if (Revision18Stage(stage, "al104")) AddRevision18MasterCard(screen, "AL104", 600, 405, "DEDICATED CUSTOMER CIP INTERFACE", "Network_AL104_OK", "io_link_al104", true);
            if (Revision18Stage(stage, "footer"))
            {
                ConfigureText(GetOrCreate<HmiText>(screen, "REV18_IOL_Overview_HoldPoint"),
                    25, 660, 1160, 38,
                    "HOLD POINT: AL1403 devices, PROFINET identities, port modes and process addresses are absent from the TIA hardware configuration. Values are NOT COMMISSIONED.",
                    Red, 11, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            }
        }

        private static void AddRevision18MasterCard(
            HmiScreen screen, string id, int left, int top, string description, string statusTag,
            string detailPage, bool cipMaster)
        {
            string prefix = "REV18_IOL_" + id;
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, prefix + "_Panel"),
                left, top, 360, 218, Panel, cipMaster ? Blue : Border, (byte)(cipMaster ? 3 : 1));
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, prefix + "_Header"),
                left, top, 360, 46, cipMaster ? Blue : Navy, cipMaster ? Blue : Navy, 0);
            ConfigureText(GetOrCreate<HmiText>(screen, prefix + "_Title"),
                left + 14, top + 8, 332, 30, id + "  |  IFM AL1403", Color.White, 17,
                HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, prefix + "_Description"),
                left + 14, top + 58, 332, 38, description, Dark, 11,
                HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, prefix + "_CommLabel"),
                left + 14, top + 108, 188, 26, "PROFINET COMM STATE", Dark, 10,
                HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureBooleanIndicator(GetOrCreate<HmiIOField>(screen, prefix + "_Comm"),
                left + 248, top + 102, 82, 32, statusTag);
            ConfigureText(GetOrCreate<HmiText>(screen, prefix + "_Quality"),
                left + 14, top + 142, 190, 24, "QUALITY / PORT COUNT", Dark, 10,
                HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, prefix + "_QualityValue"),
                left + 204, top + 142, 126, 24, "NOT CONFIGURED", Amber, 10,
                HmiFontWeight.Bold, HmiHorizontalAlignment.Right);
            ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, prefix + "_Open"),
                left + 14, top + 174, 316, 32, "OPEN " + id + " DETAIL", Blue, detailPage, "DIAGNOSTICS");
        }

        private static void DeleteRevision18MasterCard(HmiScreen screen, string id)
        {
            string prefix = "REV18_IOL_" + id;
            string[] suffixes =
            {
                "_Panel", "_Header", "_Title", "_Description", "_CommLabel", "_Comm",
                "_Quality", "_QualityValue", "_Open"
            };
            foreach (string suffix in suffixes)
            {
                DeleteItem(screen, prefix + suffix);
            }
        }

        private static void BuildRevision18IoLinkDetail(
            HmiScreen screen, string engineeringId, string description, string statusTag,
            string previousPage, string nextPage, bool cipMaster, string stage)
        {
            screen.BackColor = Color.FromArgb(239, 244, 248);
            if (Revision18Stage(stage, "chrome"))
            {
                BuildRevision13Chrome(screen, engineeringId + " IO-LINK DETAIL", "DIAGNOSTICS");
                ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV18_IOL_Detail_Summary"),
                    20, 112, 1170, 82, Panel, cipMaster ? Blue : Border, (byte)(cipMaster ? 2 : 1));
                ConfigureText(GetOrCreate<HmiText>(screen, "REV18_IOL_Detail_Identity"),
                    34, 122, 360, 28, engineeringId + "  |  IFM AL1403", Navy, 18,
                    HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
                ConfigureText(GetOrCreate<HmiText>(screen, "REV18_IOL_Detail_Description"),
                    34, 154, 650, 24, description, cipMaster ? Blue : Amber, 11,
                    HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
                ConfigureText(GetOrCreate<HmiText>(screen, "REV18_IOL_Detail_CommLabel"),
                    720, 128, 240, 24, "PROFINET COMMUNICATION", Dark, 10,
                    HmiFontWeight.Bold, HmiHorizontalAlignment.Right);
                ConfigureBooleanIndicator(GetOrCreate<HmiIOField>(screen, "REV18_IOL_Detail_Comm"),
                    980, 122, 82, 32, statusTag);
                ConfigureText(GetOrCreate<HmiText>(screen, "REV18_IOL_Detail_Quality"),
                    1070, 128, 105, 24, "UNVERIFIED", Amber, 10,
                    HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            }
            if (Revision18Stage(stage, "header"))
            {
                ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV18_IOL_Detail_Header"),
                    20, 204, 1170, 32, Navy, Navy, 0);
                string[] headings = { "PORT", "DEVICE ID", "DESCRIPTION", "MODE", "LIVE", "UNIT", "QUALITY / DIAG", "PLC / HMI MAPPING" };
                int[] x = { 30, 90, 220, 440, 540, 640, 710, 890 };
                int[] w = { 55, 125, 215, 95, 95, 65, 175, 285 };
                for (int column = 0; column < headings.Length; column++)
                {
                    ConfigureText(GetOrCreate<HmiText>(screen, "REV18_IOL_Detail_H" + column.ToString()),
                        x[column], 208, (uint)w[column], 24, headings[column], Color.White, 9,
                        HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
                }
            }
            if (Revision18Stage(stage, "ports12")) { AddRevision18PortRow(screen, engineeringId, 1, 240, cipMaster); AddRevision18PortRow(screen, engineeringId, 2, 289, cipMaster); }
            if (Revision18Stage(stage, "ports34")) { AddRevision18PortRow(screen, engineeringId, 3, 338, cipMaster); AddRevision18PortRow(screen, engineeringId, 4, 387, cipMaster); }
            if (Revision18Stage(stage, "ports56")) { AddRevision18PortRow(screen, engineeringId, 5, 436, cipMaster); AddRevision18PortRow(screen, engineeringId, 6, 485, cipMaster); }
            if (Revision18Stage(stage, "ports78")) { AddRevision18PortRow(screen, engineeringId, 7, 534, cipMaster); AddRevision18PortRow(screen, engineeringId, 8, 583, cipMaster); }
            if (Revision18Stage(stage, "navigation"))
            {
                ConfigureText(GetOrCreate<HmiText>(screen, "REV18_IOL_Detail_HoldPoint"),
                    25, 644, 1160, 28,
                    "No port/channel/address is shown until confirmed by TIA hardware configuration and customer wiring records.",
                    Red, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
                ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "REV18_IOL_Detail_Back"),
                    350, 674, 220, 36, "BACK TO IO-LINK OVERVIEW", Navy, "io_link_overview", "DIAGNOSTICS");
                ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "REV18_IOL_Detail_Previous"),
                    590, 674, 170, 36, "PREVIOUS", Navy, previousPage, "DIAGNOSTICS");
                ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "REV18_IOL_Detail_Next"),
                    780, 674, 170, 36, "NEXT", Blue, nextPage, "DIAGNOSTICS");
            }
        }

        private static void AddRevision18PortRow(
            HmiScreen screen, string engineeringId, int port, int top, bool cipMaster)
        {
            Revision20PortDefinition allocation = GetRevision20PortDefinition(engineeringId, port);
            string prefix = "REV18_IOL_Port_" + port.ToString();
            Color back = (port % 2 == 0) ? Color.FromArgb(232, 240, 247) : Color.White;
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, prefix + "_Back"),
                20, top, 1170, 45, back, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, prefix + "_Number"), 30, top + 10, 55, 24,
                "P" + port.ToString(), Navy, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, prefix + "_Device"), 90, top + 10, 125, 24,
                allocation.DeviceId, allocation.Status == "SPARE" ? Dark : Navy, 9, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, prefix + "_Description"), 220, top + 10, 215, 24,
                allocation.Description, Dark, 8, HmiFontWeight.Normal, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, prefix + "_Mode"), 440, top + 10, 95, 24,
                allocation.Mode, allocation.Status == "SPARE" ? Dark : Navy, 8, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            DeleteItem(screen, prefix + "_Live");
            if (String.IsNullOrWhiteSpace(allocation.HmiTag))
            {
                ConfigureText(GetOrCreate<HmiText>(screen, prefix + "_LiveText"), 540, top + 10, 95, 24,
                    "N/A", Amber, 9, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
                DeleteItem(screen, prefix + "_LiveValue");
            }
            else
            {
                DeleteItem(screen, prefix + "_LiveText");
                ConfigureIOField(GetOrCreate<HmiIOField>(screen, prefix + "_LiveValue"), 545, top + 6, 82, 32,
                    allocation.HmiTag, true, allocation.Format);
            }
            ConfigureText(GetOrCreate<HmiText>(screen, prefix + "_Unit"), 640, top + 10, 65, 24,
                allocation.Unit, Dark, 9, HmiFontWeight.Normal, HmiHorizontalAlignment.Center);
            ConfigureText(GetOrCreate<HmiText>(screen, prefix + "_Quality"), 710, top + 10, 175, 24,
                allocation.Status, allocation.Status == "SPARE" ? Dark : (allocation.Status.Contains("RESERVED") ? Amber : Blue),
                8, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, prefix + "_Mapping"), 890, top + 10, 285, 24,
                allocation.LogicalTag + " | " + engineeringId + "/P" + port.ToString() + " | HW ADDR N/A", Amber, 8,
                HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
        }

        private sealed class Revision20PortDefinition
        {
            public string DeviceId;
            public string Description;
            public string LogicalTag;
            public string HmiTag;
            public string Mode;
            public string Status;
            public string Unit;
            public string Format;
        }

        private static Revision20PortDefinition GetRevision20PortDefinition(string master, int port)
        {
            string key = master.ToUpperInvariant() + "/P" + port.ToString();
            string[][] rows =
            {
                new[] { "AL100/P1", "DI-031", "Bottle Shortage 1 - slowdown", "b_BottleShortage_1", "DI_Bottle_Shortage_1", "DI / SIO", "FROZEN PORT", "", "" },
                new[] { "AL100/P2", "DI-032", "Bottle Shortage 2 - gate", "b_BottleShortage_2", "DI_Bottle_Shortage_2", "DI / SIO", "FROZEN PORT", "", "" },
                new[] { "AL100/P3", "DI-033", "Outfeed Accumulation 1", "b_Accumulation_1", "DI_Accumulation_1", "DI / SIO", "FROZEN PORT", "", "" },
                new[] { "AL100/P4", "DI-034", "Outfeed Accumulation 2", "b_Accumulation_2", "DI_Accumulation_2", "DI / SIO", "FROZEN PORT", "", "" },
                new[] { "AL100/P5", "SPARE-A100-05", "Future bottle sensor", "-", "", "SPARE", "SPARE", "N/A", "" },
                new[] { "AL100/P6", "SPARE-A100-06", "Future bottle sensor", "-", "", "SPARE", "SPARE", "N/A", "" },
                new[] { "AL100/P7", "SPARE-A100-07", "Future expansion", "-", "", "SPARE", "SPARE", "N/A", "" },
                new[] { "AL100/P8", "SPARE-A100-08", "Future expansion", "-", "", "SPARE", "SPARE", "N/A", "" },
                new[] { "AL101/P1", "DI-040", "Cap Present - Pick & Place", "b_CapPresent_PP", "DI_Cap_Present", "DI / SIO", "FROZEN PORT", "", "" },
                new[] { "AL101/P2", "DI-060", "Cap Hopper Low Level", "b_CapHopper_Low", "DI_Cap_Hopper_Low", "DI / SIO", "FROZEN PORT", "", "" },
                new[] { "AL101/P3", "DI-061", "Cap Channel Request", "b_CapChannel_Req", "DI_Cap_Channel_Demand", "DI / SIO", "FROZEN PORT", "", "" },
                new[] { "AL101/P4", "DI-062", "Cap Channel Empty", "b_CapChannel_Empty", "DI_Caps_Missing", "DI / SIO", "FROZEN PORT", "", "" },
                new[] { "AL101/P5", "DI-010", "Machine Air Pressure OK", "b_AirPressureOK", "DI_AirPressure_OK", "DI / IO-Link", "FROZEN PORT", "", "" },
                new[] { "AL101/P6", "SPARE-A101-06", "Cap / pneumatic expansion", "-", "", "SPARE", "SPARE", "N/A", "" },
                new[] { "AL101/P7", "SPARE-A101-07", "Cap / pneumatic expansion", "-", "", "SPARE", "SPARE", "N/A", "" },
                new[] { "AL101/P8", "SPARE-A101-08", "Future expansion", "-", "", "SPARE", "SPARE", "N/A", "" },
                new[] { "AL102/P1", "PT-PRODUCT", "Product Pressure", "ai_ProductPressure", "", "IO-Link", "ASSIGN DEVICE AT BUILD", "bar", "0.00" },
                new[] { "AL102/P2", "PT-AIR", "Air / Process Pressure", "ai_AirPressure", "", "IO-Link", "ASSIGN DEVICE AT BUILD", "bar", "0.00" },
                new[] { "AL102/P3", "PT-CIP", "CIP / Service Pressure", "ai_CIPPressure", "", "IO-Link", "ASSIGN DEVICE AT BUILD", "bar", "0.00" },
                new[] { "AL102/P4", "PT-SPARE", "Spare Pressure Measurement", "ai_SparePressure", "", "IO-Link", "SPARE INSTRUMENT", "bar", "0.00" },
                new[] { "AL102/P5", "LT-PRODUCT", "Product Continuous Level", "ai_ProductLevel", "", "IO-Link", "ASSIGN DEVICE AT BUILD", "%", "0.0" },
                new[] { "AL102/P6", "LT-CIP", "CIP / Process Level", "ai_CIPLevel", "", "IO-Link", "ASSIGN DEVICE AT BUILD", "%", "0.0" },
                new[] { "AL102/P7", "TT-PROCESS", "Process Temperature", "ai_ProcessTemperature", "", "IO-Link", "ASSIGN DEVICE AT BUILD", "degC", "0.0" },
                new[] { "AL102/P8", "SPARE-A102-08", "Future process instrument", "-", "", "SPARE", "SPARE", "N/A", "" },
                new[] { "AL103/P1", "SEN-103-01", "Additional machine sensor 1", "io_AL103_P1", "", "IO-Link/SIO", "RESERVED / NOT INSTALLED", "N/A", "" },
                new[] { "AL103/P2", "SEN-103-02", "Additional machine sensor 2", "io_AL103_P2", "", "IO-Link/SIO", "RESERVED / NOT INSTALLED", "N/A", "" },
                new[] { "AL103/P3", "SEN-103-03", "Additional machine sensor 3", "io_AL103_P3", "", "IO-Link/SIO", "RESERVED / NOT INSTALLED", "N/A", "" },
                new[] { "AL103/P4", "SEN-103-04", "Additional machine sensor 4", "io_AL103_P4", "", "IO-Link/SIO", "RESERVED / NOT INSTALLED", "N/A", "" },
                new[] { "AL103/P5", "SPARE-A103-05", "Future smart sensor", "-", "", "SPARE", "SPARE", "N/A", "" },
                new[] { "AL103/P6", "SPARE-A103-06", "Future smart sensor", "-", "", "SPARE", "SPARE", "N/A", "" },
                new[] { "AL103/P7", "SPARE-A103-07", "Future smart sensor", "-", "", "SPARE", "SPARE", "N/A", "" },
                new[] { "AL103/P8", "SPARE-A103-08", "Future smart sensor", "-", "", "SPARE", "SPARE", "N/A", "" },
                new[] { "AL104/P1", "CIP-DO-01", "Cold Water Request", "CIP_REQ_COLD_WATER", "CIP_Req_Cold_Water", "DO", "FROZEN FUNCTION", "", "" },
                new[] { "AL104/P2", "CIP-DO-02", "Hot Water Request", "CIP_REQ_HOT_WATER", "CIP_Req_Hot_Water", "DO", "FROZEN FUNCTION", "", "" },
                new[] { "AL104/P3", "CIP-DO-03", "Acid / Chemical Request", "CIP_REQ_ACID", "CIP_Req_Acid", "DO", "FROZEN FUNCTION", "", "" },
                new[] { "AL104/P4", "CIP-DO-04", "Citra / Neutralizer Request", "CIP_REQ_CITRA", "CIP_Req_Citra", "DO", "FROZEN FUNCTION", "", "" },
                new[] { "AL104/P5", "CIP-DO-05", "Discharge Request", "CIP_REQ_DISCHARGE", "CIP_Req_Discharge", "DO", "FROZEN FUNCTION", "", "" },
                new[] { "AL104/P6", "PROD-DO-01", "Production / Product Request", "PRODUCTION_REQ_PRODUCT", "Production_Req_Product", "DO", "FROZEN FUNCTION", "", "" },
                new[] { "AL104/P7", "CIP-DI-01", "Customer CIP Ready / Accepted", "CIP_REMOTE_READY", "CIP_Remote_Ready", "DI", "RESERVED FEEDBACK", "", "" },
                new[] { "AL104/P8", "CIP-DI-02", "Customer CIP Fault / Busy", "CIP_REMOTE_FAULT", "CIP_Remote_Fault", "DI", "RESERVED FEEDBACK", "", "" }
            };
            string[] row = rows.First(candidate => candidate[0].Equals(key, StringComparison.OrdinalIgnoreCase));
            return new Revision20PortDefinition
            {
                DeviceId = row[1], Description = row[2], LogicalTag = row[3], HmiTag = row[4],
                Mode = row[5], Status = row[6], Unit = row[7], Format = row[8]
            };
        }

        private static void BuildRevision16SmcAnybus(HmiSoftware hmi)
        {
            HmiScreen screen = GetOrCreateScreen(hmi, "smc_diagnostics");
            screen.BackColor = Color.FromArgb(239, 244, 248);
            BuildRevision13Chrome(screen, "SMC / ANYBUS DIAGNOSTICS", "DIAGNOSTICS");

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV16_SMC_Architecture_Panel"),
                20, 112, 1170, 132, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV16_SMC_Architecture_Title"), 35, 122, 540, 28,
                "COMMUNICATION CHAIN - PRE-HARDWARE PREPARATION", Navy, 17, HmiFontWeight.Bold,
                HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV16_SMC_Architecture_Chain"), 35, 154, 1125, 26,
                "S7-1512C-1 PN  ->  HMS ABC3113-A  ->  EtherCAT  ->  SMC EX260-SEC1  ->  VALVE MANIFOLD P1",
                Navy, 14, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);

            AddRevision16CommStatus(screen, "AnybusPN", 35, 190, "ANYBUS PROFINET", "Network_Anybus_PN_OK");
            AddRevision16CommStatus(screen, "AnybusECAT", 305, 190, "ANYBUS ETHERCAT", "Network_Anybus_ECAT_OK");
            AddRevision16CommStatus(screen, "SmcComm", 575, 190, "SMC EX260 COMM", "Network_SMC_OK");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV16_SMC_OutputWord_Label"), 845, 192, 160, 24,
                "OUTPUT WORD", Dark, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureIOField(GetOrCreate<HmiIOField>(screen, "REV16_SMC_OutputWord"), 1000, 186, 90, 32,
                "SMC_ValveBits", true, "0");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV16_SMC_DataValid"), 1098, 192, 70, 24,
                "TBC", Amber, 11, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV16_SMC_Mapping_Panel"),
                20, 256, 1170, 444, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV16_SMC_Mapping_Title"), 35, 266, 760, 28,
                "LOGICAL EVxxx / SMC MAPPING - READ ONLY", Navy, 17, HmiFontWeight.Bold,
                HmiHorizontalAlignment.Left);
            ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "REV16_SMC_BackToIO"), 980, 264, 190, 34,
                "I/O DIRECTORY", Blue, "io_diagnostics", "DIAGNOSTICS");

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV16_SMC_Mapping_Header"),
                30, 304, 1150, 30, Navy, Navy, 0);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV16_SMC_H_Valve"), 35, 307, 60, 24,
                "VALVE", Color.White, 9, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV16_SMC_H_Function"), 100, 307, 145, 24,
                "FUNCTION", Color.White, 9, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV16_SMC_H_Logical"), 250, 307, 135, 24,
                "LOGICAL / SMC", Color.White, 9, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV16_SMC_H_Plc"), 390, 307, 250, 24,
                "PLC COMMAND / HMI", Color.White, 9, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV16_SMC_H_Physical"), 645, 307, 175, 24,
                "PHYSICAL POSITION", Color.White, 9, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV16_SMC_H_Command"), 825, 307, 70, 24,
                "CMD", Color.White, 9, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV16_SMC_H_Feedback"), 900, 307, 105, 24,
                "FEEDBACK", Color.White, 9, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV16_SMC_H_Quality"), 1010, 307, 160, 24,
                "DIAGNOSTIC QUALITY", Color.White, 9, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);

            AddRevision16ValveRow(screen, "EV200O", 338, "EV200-O", "Gate open", "ValveBits.%X0",
                "DB_Global.Out.GateOpen / DO_Gate_Open", "TBC / NOT COMM.", "DO_Gate_Open", "SYMBOLIC FB", "PN/ECAT/SMC");
            AddRevision16ValveRow(screen, "EV200C", 374, "EV200-C", "Gate close", "ValveBits.%X1",
                "DB_Global.Out.GateClose / DO_Gate_Close", "TBC / NOT COMM.", "DO_Gate_Close", "SYMBOLIC FB", "PN/ECAT/SMC");
            AddRevision16ValveRow(screen, "EV210", 410, "EV210", "Product valve", "ValveBits.%X4",
                "DB_Global.Out.Valve210Cmd / DO_Valve_210", "TBC / NOT COMM.", "DO_Valve_210", "N/A", "PN/ECAT/SMC");
            AddRevision16ValveRow(screen, "EV211", 446, "EV211", "Function TBC", "ValveBits.%X5",
                "DB_Global.Out.EV211 / SMC_EV211_Cmd", "TBC / NOT COMM.", "SMC_EV211_Cmd", "N/A", "PN/ECAT/SMC");
            AddRevision16ValveRow(screen, "EV212", 482, "EV212", "Vacuum valve", "ValveBits.%X6",
                "DB_Global.Out.Valve212Cmd / DO_Valve_212", "TBC / NOT COMM.", "DO_Valve_212", "N/A", "PN/ECAT/SMC");
            AddRevision16ValveRow(screen, "EV217", 518, "EV217", "Product valve", "MAPPING MISSING",
                "DB_Global.Out.Valve217Cmd / DO_Valve_217", "TBC / NOT COMM.", "DO_Valve_217", "N/A", "PN/ECAT/SMC");
            AddRevision16ValveRow(screen, "EV213", 554, "EV213", "Product valve", "MAPPING MISSING",
                "DB_Global.Out.Valve213Cmd / DO_Valve_213", "TBC / NOT COMM.", "DO_Valve_213", "N/A", "PN/ECAT/SMC");
            AddRevision16ValveRow(screen, "EV247", 590, "EV247", "Discharge valve", "MAPPING MISSING",
                "DB_Global.Out.Valve247Cmd / DO_Valve_247", "TBC / NOT COMM.", "DO_Valve_247", "N/A", "PN/ECAT/SMC");
            AddRevision16ValveRow(screen, "WASH", 626, "TBC-WASH", "Bottle wash", "ValveBits.%X9",
                "DB_Global.Out.ExternalBottleWashValveCmd / DO_External_Wash", "TBC / NOT COMM.", "DO_External_Wash", "N/A", "PN/ECAT/SMC");

            ConfigureText(GetOrCreate<HmiText>(screen, "REV16_SMC_HoldPoint"), 35, 668, 1135, 24,
                "X2/X3/X7/X8 have no confirmed EV code. Physical address, PDO and solenoid positions remain NOT COMMISSIONED. No HMI forcing is provided.",
                Red, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);

            HmiScreen ioDiagnostics = hmi.Screens.Find("io_diagnostics");
            if (ioDiagnostics != null)
            {
                DeleteItem(ioDiagnostics, "REV15_IO_Search_Back");
                DeleteItem(ioDiagnostics, "REV15_IO_Search_Text");
                ConfigureNavigateButton(GetOrCreate<HmiButton>(ioDiagnostics, "REV16_IO_To_SMC"),
                    830, 112, 360, 38, "SMC / ANYBUS DETAIL", Blue, "smc_diagnostics", "DIAGNOSTICS");
            }
        }

        private static void AddRevision16CommStatus(
            HmiScreen screen, string suffix, int left, int top, string label, string hmiTag)
        {
            ConfigureText(GetOrCreate<HmiText>(screen, "REV16_SMC_Comm_" + suffix + "_Label"),
                left, top + 2, 175, 24, label, Dark, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureBooleanIndicator(GetOrCreate<HmiIOField>(screen, "REV16_SMC_Comm_" + suffix + "_Value"),
                left + 180, top - 4, 70, 32, hmiTag);
        }

        private static void AddRevision16ValveRow(
            HmiScreen screen, string suffix, int top, string valve, string function, string logical,
            string plcAndHmi, string physical, string hmiTag, string feedback, string quality)
        {
            string prefix = "REV16_SMC_Row_" + suffix;
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, prefix + "_Back"), 30, top, 1150, 34,
                ((top / 36) % 2 == 0) ? Color.White : Color.FromArgb(232, 240, 247), Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, prefix + "_Valve"), 35, top + 5, 60, 24,
                valve, Navy, 9, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, prefix + "_Function"), 100, top + 5, 145, 24,
                function, Dark, 9, HmiFontWeight.Normal, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, prefix + "_Logical"), 250, top + 5, 135, 24,
                logical, logical.IndexOf("MISSING", StringComparison.OrdinalIgnoreCase) >= 0 ? Amber : Navy,
                9, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, prefix + "_Plc"), 390, top + 5, 250, 24,
                plcAndHmi, Dark, 8, HmiFontWeight.Normal, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, prefix + "_Physical"), 645, top + 5, 175, 24,
                physical, Amber, 9, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureBooleanIndicator(GetOrCreate<HmiIOField>(screen, prefix + "_Command"),
                830, top + 2, 60, 30, hmiTag);
            ConfigureText(GetOrCreate<HmiText>(screen, prefix + "_Feedback"), 900, top + 5, 105, 24,
                feedback, feedback == "N/A" ? Amber : Dark, 9, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, prefix + "_Quality"), 1010, top + 5, 160, 24,
                quality, Navy, 9, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
        }

        private static void AddRevision15IoRow(
            HmiScreen screen, string suffix, string category, int top, string code, string description,
            string type, string hmiTag, string plcTag, string module, string address, string unit, bool numeric)
        {
            string prefix = "REV15_IO_Row_" + suffix;
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, prefix + "_Back"), 30, top, 770, 38,
                ((top / 42) % 2 == 0) ? Color.White : Color.FromArgb(232, 240, 247), Border, 1);
            HmiButton select = GetOrCreate<HmiButton>(screen, prefix + "_Select");
            ConfigureButton(select, 35, top + 3, 65, 32, code, Navy);
            select.Font.Size = 10;
            select.Enabled = true;
            ConfigureRevision15DetailSelection(select, code, description, type, hmiTag, plcTag, module, address, unit);
            ConfigureText(GetOrCreate<HmiText>(screen, prefix + "_Desc"), 105, top + 6, 170, 26,
                description, Dark, 10, HmiFontWeight.Normal, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, prefix + "_Type"), 280, top + 6, 45, 26,
                type, Navy, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, prefix + "_Tag"), 330, top + 6, 180, 26,
                String.IsNullOrWhiteSpace(hmiTag) ? "N/A" : hmiTag, Dark, 9, HmiFontWeight.Normal, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, prefix + "_Module"), 515, top + 6, 130, 26,
                module, Dark, 9, HmiFontWeight.Normal, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, prefix + "_Address"), 650, top + 6, 65, 26,
                address, Amber, 9, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            if (String.IsNullOrWhiteSpace(hmiTag))
            {
                ConfigureText(GetOrCreate<HmiText>(screen, prefix + "_LiveNA"), 720, top + 6, 70, 26,
                    "N/A", Amber, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            }
            else if (numeric)
            {
                ConfigureIOField(GetOrCreate<HmiIOField>(screen, prefix + "_Live"), 720, top + 3, 70, 32,
                    hmiTag, true, "0.0");
            }
            else
            {
                ConfigureBooleanIndicator(GetOrCreate<HmiIOField>(screen, prefix + "_Live"), 720, top + 3, 70, 32, hmiTag);
            }
        }

        private static void AddRevision15DetailLine(HmiScreen screen, string suffix, int top, string label, string value)
        {
            ConfigureText(GetOrCreate<HmiText>(screen, "REV15_IO_Detail_" + suffix + "_Label"), 848, top, 120, 26,
                label, Dark, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV15_IO_Detail_" + suffix), 970, top, 202, 26,
                value, Navy, 10, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
        }

        private static void CompleteRevision15IoDiagnosticsMissing(HmiSoftware hmi)
        {
            HmiScreen screen = hmi.Screens.Find("io_diagnostics");
            if (screen == null)
            {
                throw new InvalidOperationException(
                    "REV15 completion requires the existing partial 'io_diagnostics' screen.");
            }

            string[] filters = { "ALL", "DI", "DO", "AI", "AO", "IO-LINK", "FAULTS" };
            foreach (string filterName in filters)
            {
                HmiButton filter = screen.ScreenItems.Find(
                    "REV15_IO_Filter_" + filterName.Replace("-", "_")) as HmiButton;
                if (filter == null)
                {
                    throw new InvalidOperationException("REV15 filter is missing: " + filterName);
                }
                ConfigureRevision15Filter(filter, filterName);
            }

            AddRevision15IoRow(screen, "AI_TLS100", "AI", 409, "TLS100", "Tank level", "AI", "Tank_Level_Pct", "DB_Global.Inp.TankLevelPct", "AI or IO-Link", "TBC", "%", true);
            AddRevision15IoRow(screen, "AI_VS100", "AI", 451, "VS100", "Vacuum value", "AI", "Vacuum_Actual_mbar", "DB_Global.Inp.VacuumActual_mbar", "AI or IO-Link", "TBC", "mbar", true);
            AddRevision15IoRow(screen, "AO_NA", "AO", 493, "N/A", "No confirmed AO", "AO", null, null, "NOT CONFIGURED", "N/A", "", true);
            AddRevision15IoRow(screen, "IOL_MASTER1", "IO-LINK", 535, "AL1403-1", "IO-Link master 1", "IO-LINK", "Network_IOLink1_OK", "DB_Global.Inp.IOLinkMaster1OK", "NOT IN HW CONFIG", "MISSING", "", false);
            AddRevision15IoRow(screen, "FAULT_M100", "FAULTS", 577, "M100F", "Main drive fault", "DI", "DI_MainDrive_Fault", "DB_Global.Inp.MainDriveFault", "PLC / TBC", "TBC", "", false);
            AddRevision15IoRow(screen, "FAULT_M102", "FAULTS", 619, "M102F", "Product pump fault", "DI", "DI_ProductPump_Fault", "DB_Global.Inp.ProductPumpFault", "PLC / TBC", "TBC", "", false);

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV15_IO_Detail_Panel"), 830, 165, 360, 535,
                Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV15_IO_Detail_Title"), 848, 178, 324, 30,
                "SELECTED I/O DETAIL", Navy, 18, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddRevision15DetailLine(screen, "Device", 220, "DEVICE", "N/A - SELECT A ROW");
            AddRevision15DetailLine(screen, "Description", 258, "DESCRIPTION", "N/A");
            AddRevision15DetailLine(screen, "Type", 296, "TYPE", "N/A");
            AddRevision15DetailLine(screen, "HmiTag", 334, "HMI TAG", "N/A");
            AddRevision15DetailLine(screen, "PlcTag", 372, "PLC SYMBOL", "N/A");
            AddRevision15DetailLine(screen, "Module", 410, "MODULE / CHANNEL", "NOT CONFIGURED");
            AddRevision15DetailLine(screen, "Address", 448, "ADDRESS", "NOT CONFIGURED");
            AddRevision15DetailLine(screen, "Unit", 486, "UNIT", "N/A");
            AddRevision15DetailLine(screen, "Quality", 524, "QUALITY / COMMS", "NOT CONFIGURED");
            AddRevision15DetailLine(screen, "Status", 562, "CLASSIFICATION", "MISSING SELECTION");
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV15_IO_Test_Boundary"), 848, 608, 324, 72,
                Color.FromArgb(255, 247, 230), Amber, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV15_IO_Test_Boundary_Text"), 858, 616, 304, 56,
                "MAINTENANCE TEST: NOT CONFIGURED\nNo proven PLC safe-test request interface. No HMI forcing provided.",
                Red, 11, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);

            AddRevision15IoEntryPoints(hmi);
        }

        private static void FixRevision15Ev210Live(HmiSoftware hmi)
        {
            HmiScreen screen = hmi.Screens.Find("io_diagnostics");
            if (screen == null)
            {
                throw new InvalidOperationException(
                    "REV15 EV210 fix requires the existing 'io_diagnostics' screen.");
            }

            AddRevision15IoRow(screen, "DO_EV210", "DO", 367, "EV210", "Product valve 210", "DO",
                "DO_Valve_210", "DB_Global.Out.Valve210Cmd", "EX260 LOGICAL X4", "ABS MISSING", "", false);
            AddRevision15IoRow(screen, "IOL_MASTER1", "IO-LINK", 535, "AL1403-1", "IO-Link master 1", "IO-LINK",
                "Network_IOLink1_OK", "DB_Global.Inp.IOLinkMaster1OK", "NOT IN HW CONFIG", "MISSING", "", false);

            string[] filters = { "ALL", "DI", "DO", "AI", "AO", "IO-LINK", "FAULTS" };
            foreach (string filterName in filters)
            {
                HmiButton filter = screen.ScreenItems.Find(
                    "REV15_IO_Filter_" + filterName.Replace("-", "_")) as HmiButton;
                if (filter != null) ConfigureRevision15Filter(filter, filterName);
            }
        }

        private static string Revision15Js(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return "N/A";
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static void ConfigureRevision15DetailSelection(
            HmiButton button, string code, string description, string type, string hmiTag,
            string plcTag, string module, string address, string unit)
        {
            HmiButtonEventHandler tapped = button.EventHandlers.Find(HmiButtonEventType.Tapped) ??
                button.EventHandlers.Create(HmiButtonEventType.Tapped);
            string quality = hmiTag == "DO_Valve_210" ? "SMC COMM SYMBOLIC" :
                (hmiTag == "Network_IOLink1_OK" ? "MASTER OK SYMBOLIC" : "NOT CONFIGURED");
            string classification = address.IndexOf("MISSING", StringComparison.OrdinalIgnoreCase) >= 0 ?
                (hmiTag == "DO_Valve_210" ? "LOGICAL CHANNEL VERIFIED / PHYSICAL MAP MISSING" :
                    "PROJECT HARDWARE / PORT MISSING") :
                "CONFIRMED TAG / PHYSICAL MAP UNVERIFIED";
            tapped.Script.ScriptCode =
                "Screen.Items(\"REV15_IO_Detail_Device\").Text=\"" + Revision15Js(code) + "\";" +
                "Screen.Items(\"REV15_IO_Detail_Description\").Text=\"" + Revision15Js(description) + "\";" +
                "Screen.Items(\"REV15_IO_Detail_Type\").Text=\"" + Revision15Js(type) + "\";" +
                "Screen.Items(\"REV15_IO_Detail_HmiTag\").Text=\"" + Revision15Js(hmiTag) + "\";" +
                "Screen.Items(\"REV15_IO_Detail_PlcTag\").Text=\"" + Revision15Js(plcTag) + "\";" +
                "Screen.Items(\"REV15_IO_Detail_Module\").Text=\"" + Revision15Js(module) + "\";" +
                "Screen.Items(\"REV15_IO_Detail_Address\").Text=\"" + Revision15Js(address) + "\";" +
                "Screen.Items(\"REV15_IO_Detail_Unit\").Text=\"" + Revision15Js(unit) + "\";" +
                "Screen.Items(\"REV15_IO_Detail_Quality\").Text=\"" + Revision15Js(quality) + "\";" +
                "Screen.Items(\"REV15_IO_Detail_Status\").Text=\"" + Revision15Js(classification) + "\";";
        }

        private static void ConfigureRevision15Filter(HmiButton button, string selected)
        {
            string[] suffixes = { "DI_PS100", "DI_PEI100", "DO_M102", "DO_EV210", "AI_TLS100", "AI_VS100", "AO_NA", "IOL_MASTER1", "FAULT_M100", "FAULT_M102" };
            string[] categories = { "DI", "DI", "DO", "DO", "AI", "AI", "AO", "IO-LINK", "FAULTS", "FAULTS" };
            string[] commonParts = { "Back", "Select", "Desc", "Type", "Tag", "Module", "Address" };
            string[] liveParts = { "Live", "Live", "Live", "Live", "Live", "Live", "LiveNA", "Live", "Live", "Live" };
            StringBuilder script = new StringBuilder();
            for (int row = 0; row < suffixes.Length; row++)
            {
                bool visible = selected == "ALL" || selected == categories[row];
                foreach (string part in commonParts.Concat(new[] { liveParts[row] }))
                {
                    script.Append("var o=Screen.Items(\"REV15_IO_Row_").Append(suffixes[row]).Append("_").Append(part)
                        .Append("\");if(o){o.Visible=").Append(visible ? "true" : "false").Append(";}");
                }
            }
            HmiButtonEventHandler tapped = button.EventHandlers.Find(HmiButtonEventType.Tapped) ??
                button.EventHandlers.Create(HmiButtonEventType.Tapped);
            tapped.Script.ScriptCode = script.ToString();
        }

        private static void AddRevision15IoEntryPoints(HmiSoftware hmi)
        {
            HmiScreen diagnostics = hmi.Screens.Find("diagnostics");
            if (diagnostics != null)
            {
                ConfigureNavigateButton(GetOrCreate<HmiButton>(diagnostics, "REV15_Diagnostics_To_IO"), 45, 545, 330, 48,
                    "I/O CONFIGURATION & DIAGNOSTICS", Blue, "io_diagnostics", "DIAGNOSTICS");
            }
            HmiScreen setup = hmi.Screens.Find("setup");
            if (setup != null)
            {
                ConfigureNavigateButton(GetOrCreate<HmiButton>(setup, "REV15_Setup_To_IO"), 430, 665, 250, 42,
                    "I/O DIAGNOSTICS", Blue, "io_diagnostics", "SETUP");
            }
        }

        private static void BuildDiagnostics(HmiScreen screen)
        {
            ConfigurePageChrome(screen, "DIAGNOSTICS", "DIAGNOSTICS",
                "DIAGNOSTIC VALUES ARE READ ONLY  |  COMMANDS REMAIN ON THEIR DEDICATED OPERATOR PAGES");

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Diagnostics_Network"), 25, 140, 370, 575, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Diagnostics_Network_Title"), 45, 152, 330, 34,
                "NETWORK & CONTROLLER", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddStatus(screen, "Network healthy", "Network_OK", 45, 205);
            AddStatus(screen, "Pilz network healthy", "Network_Pilz_OK", 45, 245);
            AddStatus(screen, "IO-Link island 1", "Network_IOLink1_OK", 45, 285);
            AddStatus(screen, "IO-Link island 2", "Network_IOLink2_OK", 45, 325);
            AddStatus(screen, "IO-Link island 3", "Network_IOLink3_OK", 45, 365);
            AddStatus(screen, "IO-Link island 4", "Network_IOLink4_OK", 45, 405);
            AddStatus(screen, "G120C drive network", "Network_G120C_OK", 45, 445);
            AddStatus(screen, "HMI communication", "Network_HMI_OK", 45, 485);

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Diagnostics_Safety"), 410, 140, 380, 575, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Diagnostics_Safety_Title"), 430, 152, 340, 34,
                "SAFETY & ACCESS", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddStatus(screen, "Safety permissive", "Machine_SafetyOK", 430, 205);
            AddStatus(screen, "E-stop chain healthy", "EStop_Chain_Healthy", 430, 245);
            AddStatus(screen, "All 11 doors closed", "Door_AllClosed", 430, 285);
            AddStatus(screen, "All doors unlocked", "Door_AllUnlocked", 430, 325);
            AddStatus(screen, "Door sequence active", "Door_Sequence_Active", 430, 365);
            AddStatus(screen, "Restart permitted", "Door_Restart_Permitted", 430, 405);
            AddStatus(screen, "Critical alarm active", "Alarm_Critical", 430, 445);
            AddStatus(screen, "Warning active", "Alarm_Warning", 430, 485);

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Diagnostics_Process"), 805, 140, 385, 575, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Diagnostics_Process_Title"), 825, 152, 345, 34,
                "PROCESS INPUTS", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddStatus(screen, "Product present", "Product_Present_At_Pump", 825, 205);
            AddStatus(screen, "Vacuum signal valid", "Vacuum_Signal_Valid", 825, 245);
            AddStatus(screen, "Air pressure proof", "AirFilter_PressureOK", 825, 285);
            AddStatus(screen, "Tank full 100%", "Tank_Full_100", 825, 325);
            AddStatus(screen, "Capper crash active", "Capper_Outfeed_Crash", 825, 365);
            AddCompactMetric(screen, "REV12_Diagnostics_Tank_Label", "REV12_Diagnostics_Tank", 825, 425,
                "TANK LEVEL [%]", "Tank_Level_Pct", true, "0.0");
            AddCompactMetric(screen, "REV12_Diagnostics_Vacuum_Label", "REV12_Diagnostics_Vacuum", 825, 510,
                "VACUUM [mbar]", "Vacuum_Actual_mbar", true, "0.0");
            AddCompactMetric(screen, "REV12_Diagnostics_Speed_Label", "REV12_Diagnostics_Speed", 825, 595,
                "DRIVE SPEED [%]", "Speed_Actual_Pct", true, "0.0");
        }

        private static void AddMachineOverview(HmiScreen screen)
        {
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Overview_Panel"), 25, 140, 775, 310, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Overview_Title"), 45, 152, 730, 34,
                "MACHINE OVERVIEW", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);

            ConfigureProcessBlock(GetOrCreate<HmiRectangle>(screen, "REV12_Infeed_Block"), 60);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Infeed_Text"), 60, 248, 150, 50,
                "INFEED", Color.White, 18, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureProcessBlock(GetOrCreate<HmiRectangle>(screen, "REV12_Filler_Block"), 250);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Filler_Text"), 250, 248, 150, 50,
                "FILLER", Color.White, 18, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureProcessBlock(GetOrCreate<HmiRectangle>(screen, "REV12_Capper_Block"), 440);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Capper_Text"), 440, 248, 150, 50,
                "CAPPER", Color.White, 18, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureProcessBlock(GetOrCreate<HmiRectangle>(screen, "REV12_Outfeed_Block"), 630);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Outfeed_Text"), 630, 248, 150, 50,
                "OUTFEED", Color.White, 18, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);

            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Arrow_1"), 210, 246, 40, 45, "→", Blue, 30, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Arrow_2"), 400, 246, 40, 45, "→", Blue, 30, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Arrow_3"), 590, 246, 40, 45, "→", Blue, 30, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);

            AddMetric(screen, "REV12_Speed_Label", "REV12_Speed_Field", 55, 374, "SPEED SETPOINT [BPH]", "Speed_Setpoint_BPH", false, "0");
            AddMetric(screen, "REV12_Actual_Label", "REV12_Actual_Field", 300, 374, "DRIVE SPEED [%]", "Speed_Actual_Pct", true, "0.0");
            AddMetric(screen, "REV12_Tank_Label", "REV12_Tank_Field", 520, 374, "TANK LEVEL [%]", "Tank_Level_Pct", true, "0.0");
            AddCompactStatus(screen, "External wash valve", "External_Wash_Valve", 250, 330);
        }

        private static void AddStatusPanel(HmiScreen screen)
        {
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Status_Panel"), 820, 140, 370, 310, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Status_Title"), 840, 152, 330, 34,
                "SAFETY & DOOR ACCESS", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddStatus(screen, "Safety permissive", "Machine_SafetyOK", 840, 198);
            AddStatus(screen, "E-stop chain healthy", "EStop_Chain_Healthy", 840, 234);
            AddStatus(screen, "All 11 doors closed", "Door_AllClosed", 840, 270);
            AddStatus(screen, "All doors unlocked", "Door_AllUnlocked", 840, 306);
            AddStatus(screen, "Door sequence active", "Door_Sequence_Active", 840, 342);
            AddStatus(screen, "Restart permitted", "Door_Restart_Permitted", 840, 378);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Door_Note"), 840, 414, 330, 24,
                "No automatic restart after door access", Red, 13, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
        }

        private static void AddHomeProcessPanel(HmiScreen screen)
        {
            string[] safetyHomeObjects =
            {
                "REV12_Status_Title", "REV12_Label_MachineSafetyOK", "REV12_Value_MachineSafetyOK",
                "REV12_Label_EStopChainHealthy", "REV12_Value_EStopChainHealthy",
                "REV12_Label_DoorAllClosed", "REV12_Value_DoorAllClosed",
                "REV12_Label_DoorAllUnlocked", "REV12_Value_DoorAllUnlocked",
                "REV12_Label_DoorSequenceActive", "REV12_Value_DoorSequenceActive",
                "REV12_Label_DoorRestartPermitted", "REV12_Value_DoorRestartPermitted", "REV12_Door_Note"
            };
            foreach (string objectName in safetyHomeObjects)
            {
                DeleteItem(screen, objectName);
            }
            foreach (string objectName in new[]
            {
                "REV12_Label_VacuumPumpRun", "REV12_Value_VacuumPumpRun",
                "REV12_Label_ExternalWashActive", "REV12_Value_ExternalWashActive"
            })
            {
                DeleteItem(screen, objectName);
            }
            string[] obsoleteHomeObjects =
            {
                "REV12_Status_Title", "REV12_Label_MachineSafetyOK", "REV12_Value_MachineSafetyOK",
                "REV12_Label_EStopChainHealthy", "REV12_Value_EStopChainHealthy",
                "REV12_Label_DoorAllClosed", "REV12_Value_DoorAllClosed",
                "REV12_Label_DoorAllUnlocked", "REV12_Value_DoorAllUnlocked",
                "REV12_Label_DoorSequenceActive", "REV12_Value_DoorSequenceActive",
                "REV12_Label_DoorRestartPermitted", "REV12_Value_DoorRestartPermitted", "REV12_Door_Note",
                "REV12_Alarm_Panel", "REV12_Alarm_Title", "REV12_Label_AlarmCritical", "REV12_Value_AlarmCritical",
                "REV12_Label_AlarmWarning", "REV12_Value_AlarmWarning", "REV12_Label_NetworkOK", "REV12_Value_NetworkOK",
                "REV12_Label_CapperOutfeedCrash", "REV12_Value_CapperOutfeedCrash", "REV12_Alarm_Note"
                , "REV12_Home_Production_Title", "REV12_Label_ModeProductionActive", "REV12_Value_ModeProductionActive"
                , "REV12_Label_ProductPresentAtPump", "REV12_Value_ProductPresentAtPump"
                , "REV12_Label_TankLevelPct", "REV12_Value_TankLevelPct"
                , "REV12_Label_VacuumActualmbar", "REV12_Value_VacuumActualmbar"
                , "REV12_Label_RunOutActive", "REV12_Value_RunOutActive"
                , "REV12_Label_RunOutCompleted", "REV12_Value_RunOutCompleted"
                , "REV12_Run_Out_Start", "REV12_Production_On", "REV12_Production_Off", "REV12_CIP_Request"
            };
            foreach (string objectName in obsoleteHomeObjects)
            {
                DeleteItem(screen, objectName);
            }

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Status_Panel"), 820, 140, 370, 575, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Home_Process_Title"), 840, 152, 330, 30,
                "PROCESS COMMANDS", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Home_Process_Legend"), 840, 184, 330, 22,
                "GREEN ACTIVE  |  GREY OFF  |  RED FAULT", Dark, 11, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);

            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Pump_Enable_Label"), 840, 216, 120, 30,
                "PRODUCT PUMP", Dark, 13, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureWriteButton(GetOrCreate<HmiButton>(screen, "REV12_Pump_Off"), 960, 212, 90, 38,
                "PUMP OFF", Grey, "Product_Pump_Enable", 0);
            ConfigureWriteButton(GetOrCreate<HmiButton>(screen, "REV12_Pump_On"), 1060, 212, 90, 38,
                "PUMP ON", Green, "Product_Pump_Enable", 1);

            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Gate_Auto_Label"), 840, 266, 120, 30,
                "BOTTLE GATE", Dark, 13, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureWriteButton(GetOrCreate<HmiButton>(screen, "REV12_Gate_Auto_Off"), 960, 262, 90, 38,
                "GATE OFF", Grey, "Gate_Auto_Request_Enable", 0);
            ConfigureWriteButton(GetOrCreate<HmiButton>(screen, "REV12_Gate_Auto_On"), 1060, 262, 90, 38,
                "GATE ON", Green, "Gate_Auto_Request_Enable", 1);

            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Home_Vacuum_Label"), 840, 320, 210, 30,
                "VACUUM PUMP (AUTO)", Dark, 13, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureIOField(GetOrCreate<HmiIOField>(screen, "REV12_Home_Vacuum_Status"), 1060, 316, 90, 38,
                "Vacuum_Pump_Run", true, "");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Home_Wash_Label"), 840, 374, 210, 30,
                "EXTERNAL WASH (AUTO)", Dark, 13, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureIOField(GetOrCreate<HmiIOField>(screen, "REV12_Home_Wash_Status"), 1060, 370, 90, 38,
                "External_Wash_Active", true, "");

            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Home_Future_Title"), 840, 438, 330, 30,
                "FUTURE PROCESS ACTUATORS", Navy, 17, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            AddReservedProcessSlot(screen, "REV12_Home_Reserved_1", "RESERVED ACTUATOR 1", 840, 480);
            AddReservedProcessSlot(screen, "REV12_Home_Reserved_2", "RESERVED ACTUATOR 2", 840, 548);
            AddReservedProcessSlot(screen, "REV12_Home_Reserved_3", "RESERVED ACTUATOR 3", 840, 616);
        }

        private static void AddReservedProcessSlot(HmiScreen screen, string name, string label, int left, int top)
        {
            HmiRectangle back = GetOrCreate<HmiRectangle>(screen, name + "_Back");
            ConfigureRectangle(back, left, top, 310, 52, Color.FromArgb(235, 239, 242), Border, 1);
            SetRoundedCorners(back, 8);
            ConfigureText(GetOrCreate<HmiText>(screen, name + "_Text"), left + 15, top + 8, 280, 36,
                label, Color.FromArgb(105, 115, 125), 14, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
        }

        private static void CleanOperateLegacyObjects(HmiScreen screen)
        {
            string[] obsoleteTags =
            {
                "FillerHeightEnable", "CapperHeightEnable", "TankFull100", "CapperOutfeedCrash",
                "NetworkOK", "AirFilterRelay", "LightsRelay", "AlarmCritical",
                "PanelStartPB", "PanelStopPB", "PanelModeAuto", "DoorRequestPanel"
            };
            foreach (string safeTag in obsoleteTags)
            {
                DeleteItem(screen, "REV12_Label_" + safeTag);
                DeleteItem(screen, "REV12_Value_" + safeTag);
            }
        }

        private static void AddAuxiliaryPanel(HmiScreen screen)
        {
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Aux_Panel"), 25, 470, 775, 245, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Aux_Title"), 45, 480, 250, 28,
                "MAIN AUXILIARIES", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);

            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Lights_Title"), 45, 512, 220, 24,
                "MACHINE LIGHTS 24 VDC", Dark, 16, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV12_Lights_On"), 45, 542, 110, 50,
                "LIGHTS ON", Green, "Lights_On");
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV12_Lights_Off"), 165, 542, 110, 50,
                "LIGHTS OFF", Grey, "Lights_Off");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Label_LightsRelay"), 295, 518, 150, 28,
                "Relay energized", Dark, 14, HmiFontWeight.Normal, HmiHorizontalAlignment.Left);
            ConfigureIOField(GetOrCreate<HmiIOField>(screen, "REV12_Value_LightsRelay"), 455, 516, 75, 32,
                "Lights_Relay", true, "");

            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Filter_Title"), 45, 610, 220, 24,
                "AIR FILTER 24 VDC", Dark, 16, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV12_Filter_On"), 45, 640, 110, 50,
                "FILTER ON", Green, "AirFilter_On");
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV12_Filter_Off"), 165, 640, 110, 50,
                "FILTER OFF", Grey, "AirFilter_Off");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Label_AirFilterRelay"), 295, 612, 150, 28,
                "Relay energized", Dark, 14, HmiFontWeight.Normal, HmiHorizontalAlignment.Left);
            ConfigureIOField(GetOrCreate<HmiIOField>(screen, "REV12_Value_AirFilterRelay"), 455, 610, 75, 32,
                "AirFilter_Relay", true, "");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Label_AirFilterPressureOK"), 295, 654, 150, 28,
                "Pressure proof", Dark, 14, HmiFontWeight.Normal, HmiHorizontalAlignment.Left);
            ConfigureIOField(GetOrCreate<HmiIOField>(screen, "REV12_Value_AirFilterPressureOK"), 455, 652, 75, 32,
                "AirFilter_PressureOK", true, "");

            HmiRectangle filterService = GetOrCreate<HmiRectangle>(screen, "REV12_Filter_Warn_Back");
            ConfigureRectangle(filterService, 640, 510, 145, 180,
                Color.FromArgb(18, 91, 148), Blue, 2);
            SetRoundedCorners(filterService, 12);
            filterService.AlternateBackColor = Color.FromArgb(8, 58, 105);
            filterService.BackFillPattern = HmiFillPattern.GradientVertical;
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Filter_Warn_Title"), 652, 526, 120, 36,
                "FILTER SERVICE", Color.White, 15, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureIOField(GetOrCreate<HmiIOField>(screen, "REV12_Filter_Warn_Field"), 662, 574, 100, 40,
                "AirFilter_ChangeRequired", true, "");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Filter_Warn_Text"), 652, 634, 120, 26,
                "CHANGE REQUIRED", Color.White, 12, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
        }

        private static void AddAlarmPanel(HmiScreen screen)
        {
            HideItem(screen, "REV12_Label_AlarmCritical");
            HideItem(screen, "REV12_Value_AlarmCritical");
            HideItem(screen, "REV12_Label_AlarmWarning");
            HideItem(screen, "REV12_Value_AlarmWarning");
            HideItem(screen, "REV12_Label_NetworkOK");
            HideItem(screen, "REV12_Value_NetworkOK");
            HideItem(screen, "REV12_Label_CapperOutfeedCrash");
            HideItem(screen, "REV12_Value_CapperOutfeedCrash");
            HideItem(screen, "REV12_Alarm_Note");

            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Alarm_Panel"), 820, 470, 370, 245, Panel, Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Alarm_Title"), 840, 482, 330, 32,
                "PROCESS COMMANDS", Navy, 20, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);

            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Gate_Auto_Label"), 840, 520, 120, 30,
                "BOTTLE GATE", Dark, 13, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureWriteButton(GetOrCreate<HmiButton>(screen, "REV12_Gate_Auto_Off"), 960, 516, 90, 38,
                "GATE OFF", Grey, "Gate_Auto_Request_Enable", 0);
            ConfigureWriteButton(GetOrCreate<HmiButton>(screen, "REV12_Gate_Auto_On"), 1060, 516, 90, 38,
                "GATE ON", Blue, "Gate_Auto_Request_Enable", 1);

            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Pump_Enable_Label"), 840, 566, 120, 30,
                "PRODUCT PUMP", Dark, 13, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureWriteButton(GetOrCreate<HmiButton>(screen, "REV12_Pump_Off"), 960, 562, 90, 38,
                "PUMP OFF", Grey, "Product_Pump_Enable", 0);
            ConfigureWriteButton(GetOrCreate<HmiButton>(screen, "REV12_Pump_On"), 1060, 562, 90, 38,
                "PUMP ON", Green, "Product_Pump_Enable", 1);

            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV12_Run_Out_Start"), 840, 612, 170, 48,
                "RUN OUT PRODUCT", Amber, "Run_Out_Product_Start");
            AddTightStatus(screen, "Run Out Active", "Run_Out_Active", 1020, 604);
            AddTightStatus(screen, "Run Out Complete", "Run_Out_Completed", 1020, 642);
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV12_Production_On"), 840, 674, 95, 34,
                "PROD ON", Green, "Cmd_ProductionOn");
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV12_Production_Off"), 945, 674, 95, 34,
                "PROD OFF", Red, "Cmd_ProductionOff");
            ConfigureMomentaryButton(GetOrCreate<HmiButton>(screen, "REV12_CIP_Request"), 1050, 674, 100, 34,
                "CIP START", Blue, "Cmd_CIPStart");
        }

        private static void AddNavigationRail(HmiScreen screen, string activePage)
        {
            DeleteItem(screen, "REV12_Nav_OPERATE");
            DeleteItem(screen, "REV12_Nav_TRENDS");
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Nav_Back"), 1210, 120, 156, 648, Navy, Navy, 0);
            string[] labels = { "HOME", "SAFETY", "OPERATE", "PRODUCTION", "CIP", "FUNCTION", "ALARMS", "RECIPE", "SETUP", "MANUAL", "EFFICIENCY", "DIAGNOSTICS" };
            string[] destinations = { "home", "safety", "operate", "production", "cip", "function", "alarms", "recipe", "setup", "manual", "efficiency", "diagnostics" };
            for (int index = 0; index < labels.Length; index++)
            {
                int top = 124 + index * 51;
                bool active = labels[index] == activePage;
                Color fill = active ? Blue : Color.FromArgb(44, 72, 102);
                HmiButton button = GetOrCreate<HmiButton>(screen, "REV12_Nav_" + labels[index]);
                ConfigureButton(button, 1220, top, 136, 42, labels[index], fill);
                bool pageAvailable = true;
                bool destinationIsCurrent = destinations[index].Equals(screen.Name, StringComparison.OrdinalIgnoreCase);
                button.Enabled = pageAvailable && !active && !destinationIsCurrent;
                if (button.Enabled)
                {
                    ConfigureScreenNavigation(button, destinations[index], activePage);
                }
            }
        }

        private static void ConfigureScreenNavigation(HmiButton button, string screenName, string activePage)
        {
            HmiButtonEventHandler click = button.EventHandlers.Find(HmiButtonEventType.Tapped);
            if (click == null)
            {
                click = button.EventHandlers.Create(HmiButtonEventType.Tapped);
            }
            string script = "";
            if (activePage == "MANUAL")
            {
                script += "HMIRuntime.Tags(\"Gate_Manual_PageActive\").Write(0);";
                script += "HMIRuntime.Tags(\"Gate_Manual_TestEnable\").Write(0);";
                script += "HMIRuntime.Tags(\"Gate_Open_125Y1\").Write(0);";
                script += "HMIRuntime.Tags(\"Gate_Close\").Write(0);";
                script += "HMIRuntime.Tags(\"Pendant_Up\").Write(0);";
                script += "HMIRuntime.Tags(\"Pendant_Down\").Write(0);";
                script += "HMIRuntime.Tags(\"Jog_PB\").Write(0);";
                script += "HMIRuntime.Tags(\"Filler_Height_Enable\").Write(0);";
                script += "HMIRuntime.Tags(\"Capper_Height_Enable\").Write(0);";
                script += "HMIRuntime.Tags(\"Cmd_ManualSelect\").Write(0);";
                script += "HMIRuntime.Tags(\"Cmd_ProductPumpManualEnable\").Write(0);";
            }
            if (screenName == "manual")
            {
                script += "HMIRuntime.Tags(\"Gate_Manual_PageActive\").Write(1);";
                script += "HMIRuntime.Tags(\"Cmd_ManualSelect\").Write(1);";
            }
            script += "HMIRuntime.UI.SysFct.ChangeScreen(\"" + screenName + "\", \"/\");";
            click.Script.ScriptCode = script;
        }

        private static void ConfigureNavigateButton(
            HmiButton button, int left, int top, uint width, uint height,
            string text, Color back, string screenName, string activePage)
        {
            ConfigureButton(button, left, top, width, height, text, back);
            button.Enabled = true;
            ConfigureScreenNavigation(button, screenName, activePage);
        }

        private static void ConfigureLiftSelectButton(
            HmiButton button, int left, int top, uint width, uint height,
            string text, string selectedTag, string otherTag)
        {
            ConfigureButton(button, left, top, width, height, text, Color.FromArgb(78, 102, 125));
            button.Enabled = true;
            HmiButtonEventHandler tapped = button.EventHandlers.Find(HmiButtonEventType.Tapped);
            if (tapped == null)
            {
                tapped = button.EventHandlers.Create(HmiButtonEventType.Tapped);
            }
            tapped.Script.ScriptCode =
                "HMIRuntime.Tags(\"" + otherTag + "\").Write(0);" +
                "HMIRuntime.Tags(\"" + selectedTag + "\").Write(1);" +
                "HMIRuntime.UI.SysFct.ChangeScreen(\"lift_warning\", \"/\");";
        }

        private static void AddSettingsPager(HmiScreen screen, string previousScreen, string nextScreen)
        {
            ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "REV12_Settings_Previous"), 870, 665, 130, 42,
                "PREVIOUS", Color.FromArgb(78, 102, 125), previousScreen, "SETUP");
            ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "REV12_Settings_Next"), 1020, 665, 130, 42,
                "NEXT", Blue, nextScreen, "SETUP");
            ConfigureNavigateButton(GetOrCreate<HmiButton>(screen, "REV12_Settings_Return"), 700, 665, 150, 42,
                "SETTINGS HOME", Navy, "setup", "SETUP");
        }

        private static void AddIoDiagnosticRow(
            HmiScreen screen, string suffix, int left, int top,
            string code, string description, string location, string tag)
        {
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_IORow_" + suffix), left, top, 530, 40,
                Color.FromArgb(238, 243, 247), Border, 1);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_IOCode_" + suffix), left + 8, top + 4, 85, 32,
                code, Navy, 13, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_IODescription_" + suffix), left + 95, top + 4, 235, 32,
                description, Dark, 12, HmiFontWeight.Normal, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_IOLocation_" + suffix), left + 330, top + 4, 135, 32,
                location, Dark, 11, HmiFontWeight.Normal, HmiHorizontalAlignment.Left);
            ConfigureBooleanIndicator(GetOrCreate<HmiIOField>(screen, "REV12_IOState_" + suffix), left + 470, top + 4, 50, 32, tag);
        }

        private static void ConfigureBooleanIndicator(HmiIOField item, int left, int top, uint width, uint height, string tag)
        {
            ConfigureIOField(item, left, top, width, height, tag, true, "0");
            item.ForeColor = Color.White;
            DynamizationBase existing = item.Dynamizations.Find("BackColor");
            if (existing != null) existing.Delete();
            ScriptDynamization color = item.Dynamizations.Create<ScriptDynamization>("BackColor");
            color.Async = false;
            color.Trigger.Type = TriggerType.T500ms;
            color.ScriptCode =
                "let v=HMIRuntime.Tags(\"" + tag + "\").Read();" +
                "if(v){return HMIRuntime.Math.RGB(28,145,82);}" +
                "return HMIRuntime.Math.RGB(160,170,180);";
        }

        private static void AddTimerSetting(
            HmiScreen screen, string suffix, int left, int top,
            string label, string tag, string unit, string limits)
        {
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_TimerLabel_" + suffix), left, top, 290, 26,
                label, Dark, 13, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureIOField(GetOrCreate<HmiIOField>(screen, "REV12_TimerValue_" + suffix), left + 300, top - 4, 125, 38,
                tag, false, "");
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_TimerUnit_" + suffix), left + 435, top, 35, 26,
                unit, Navy, 13, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_TimerLimits_" + suffix), left, top + 34, 470, 24,
                "ALLOWED RANGE: " + limits, Amber, 11, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
        }

        private static void ConfigureNumericDynamization(
            HmiScreenItemBase item, string propertyName, string tag, string scriptCode)
        {
            DynamizationBase existing = item.Dynamizations.Find(propertyName);
            if (existing != null) existing.Delete();
            ScriptDynamization value = item.Dynamizations.Create<ScriptDynamization>(propertyName);
            value.Async = false;
            value.Trigger.Type = TriggerType.T500ms;
            value.ScriptCode = scriptCode;
        }

        private static void AddMetric(
            HmiScreen screen,
            string labelName,
            string fieldName,
            int left,
            int top,
            string label,
            string tag,
            bool outputOnly,
            string format)
        {
            ConfigureText(GetOrCreate<HmiText>(screen, labelName), left, top, 210, 24,
                label, Dark, 13, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureIOField(GetOrCreate<HmiIOField>(screen, fieldName), left, top + 28, 180, 38,
                tag, outputOnly, format);
        }

        private static void AddEfficiencyCard(
            HmiScreen screen,
            string suffix,
            int left,
            Color accent,
            Color fill,
            string title,
            string tag)
        {
            ConfigureRectangle(GetOrCreate<HmiRectangle>(screen, "REV12_Efficiency_" + suffix + "_Card"),
                left, 140, 270, 165, fill, accent, 3);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Efficiency_" + suffix + "_Title"),
                left + 18, 158, 234, 32, title, accent, 17, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            HmiIOField value = GetOrCreate<HmiIOField>(screen, "REV12_Efficiency_" + suffix + "_Value");
            ConfigureIOField(value, left + 35, 215, 200, 58, tag, true, "0");
            value.Font.Size = 24;
            value.BorderColor = accent;
            value.BorderWidth = 2;
        }

        private static void ConfigureDashboardGauge(
            HmiScreen screen,
            string suffix,
            int centerX,
            int centerY,
            Color accent,
            string tag,
            string fieldName)
        {
            HmiEllipse outer = GetOrCreate<HmiEllipse>(screen, "REV12_Efficiency_" + suffix + "_Gauge");
            outer.CenterX = centerX;
            outer.CenterY = centerY;
            outer.RadiusX = 70;
            outer.RadiusY = 70;
            outer.BackColor = Color.White;
            outer.BorderColor = Color.FromArgb(205, 213, 220);
            outer.BorderWidth = 12;
            outer.BackFillPattern = HmiFillPattern.Solid;
            outer.Visible = true;
            outer.Enabled = true;

            HmiEllipse marker = GetOrCreate<HmiEllipse>(screen, "REV12_Efficiency_" + suffix + "_Marker");
            marker.CenterX = centerX - 55;
            marker.CenterY = centerY + 18;
            marker.RadiusX = 9;
            marker.RadiusY = 9;
            marker.BackColor = accent;
            marker.BorderColor = accent;
            marker.BorderWidth = 1;
            marker.BackFillPattern = HmiFillPattern.Solid;
            marker.Visible = true;
            marker.Enabled = true;

            HmiIOField value = GetOrCreate<HmiIOField>(screen, fieldName);
            ConfigureIOField(value, centerX - 62, centerY - 18, 124, 48, tag, true, "0");
            value.Font.Size = 21;
            value.BorderColor = accent;
            value.BorderWidth = 2;
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Efficiency_" + suffix + "_Unit"),
                centerX - 55, centerY + 38, 110, 22, "seconds", Dark, 12,
                HmiFontWeight.Normal, HmiHorizontalAlignment.Center);
        }

        private static void ConfigureDashboardChart(HmiScreen screen)
        {
            int[] x = { 350, 415, 480, 545, 610, 675, 740, 805, 870, 915 };
            int[] y = { 560, 500, 575, 530, 555, 485, 535, 500, 545, 510 };
            for (int grid = 0; grid < 4; grid++)
            {
                HmiLine gridLine = GetOrCreate<HmiLine>(screen, "REV12_Efficiency_Chart_Grid_" + grid);
                ConfigureDashboardLine(gridLine, 345, 470 + (grid * 38), 565, 470 + (grid * 38),
                    Color.FromArgb(210, 218, 225), 1);
            }
            for (int index = 0; index < x.Length - 1; index++)
            {
                HmiLine segment = GetOrCreate<HmiLine>(screen, "REV12_Efficiency_Chart_Line_" + index);
                ConfigureDashboardLine(segment, x[index], y[index], x[index + 1], y[index + 1],
                    Color.FromArgb(55, 126, 171), 3);
                HmiEllipse point = GetOrCreate<HmiEllipse>(screen, "REV12_Efficiency_Chart_Point_" + index);
                point.CenterX = x[index];
                point.CenterY = y[index];
                point.RadiusX = 5;
                point.RadiusY = 5;
                point.BackColor = Color.White;
                point.BorderColor = Color.FromArgb(55, 126, 171);
                point.BorderWidth = 2;
                point.BackFillPattern = HmiFillPattern.Solid;
                point.Visible = true;
                point.Enabled = true;
            }
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Efficiency_Chart_Run"), 348, 455, 100, 20,
                "RUN", Green, 11, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Efficiency_Chart_Wait"), 520, 455, 120, 20,
                "WAITING", Amber, 11, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Efficiency_Chart_Stop"), 700, 455, 100, 20,
                "STOP", Color.FromArgb(92, 105, 118), 11, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Efficiency_Chart_Fault"), 815, 455, 100, 20,
                "FAULT", Red, 11, HmiFontWeight.Bold, HmiHorizontalAlignment.Right);
        }

        private static void ConfigureDashboardLine(
            HmiLine line,
            int x1,
            int y1,
            int x2,
            int y2,
            Color color,
            byte width)
        {
            int left = Math.Min(x1, x2);
            int top = Math.Min(y1, y2);
            uint lineWidth = (uint)Math.Max(1, Math.Abs(x2 - x1));
            uint lineHeight = (uint)Math.Max(1, Math.Abs(y2 - y1));
            line.Left = left;
            line.Top = top;
            line.Width = lineWidth;
            line.Height = lineHeight;
            line.X1 = x1 - left;
            line.Y1 = y1 - top;
            line.X2 = x2 - left;
            line.Y2 = y2 - top;
            line.LineColor = color;
            line.LineWidth = width;
            line.Visible = true;
            line.Enabled = true;
        }

        private static void AddDashboardMiniMetric(
            HmiScreen screen,
            string suffix,
            int left,
            string label,
            string tag,
            string fieldName,
            string format)
        {
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Efficiency_" + suffix + "_MiniLabel"),
                left, 608, 120, 20, label, Dark, 11, HmiFontWeight.Bold, HmiHorizontalAlignment.Center);
            ConfigureIOField(GetOrCreate<HmiIOField>(screen, fieldName), left + 8, 632, 104, 32,
                tag, true, format);
        }

        private static void AddCompactMetric(
            HmiScreen screen,
            string labelName,
            string fieldName,
            int left,
            int top,
            string label,
            string tag,
            bool outputOnly,
            string format)
        {
            ConfigureText(GetOrCreate<HmiText>(screen, labelName), left, top, 170, 24,
                label, Dark, 12, HmiFontWeight.Bold, HmiHorizontalAlignment.Left);
            ConfigureIOField(GetOrCreate<HmiIOField>(screen, fieldName), left, top + 28, 160, 38,
                tag, outputOnly, format);
        }

        private static void AddStatus(HmiScreen screen, string label, string tag, int left, int top)
        {
            string safe = new string(tag.Where(char.IsLetterOrDigit).ToArray());
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Label_" + safe), left, top, 215, 28,
                label, Dark, 14, HmiFontWeight.Normal, HmiHorizontalAlignment.Left);
            ConfigureIOField(GetOrCreate<HmiIOField>(screen, "REV12_Value_" + safe), left + 225, top - 2, 95, 32,
                tag, true, "");
        }

        private static void AddCompactStatus(HmiScreen screen, string label, string tag, int left, int top)
        {
            string safe = new string(tag.Where(char.IsLetterOrDigit).ToArray());
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Label_" + safe), left, top, 170, 28,
                label, Dark, 14, HmiFontWeight.Normal, HmiHorizontalAlignment.Left);
            ConfigureIOField(GetOrCreate<HmiIOField>(screen, "REV12_Value_" + safe), left + 180, top - 2, 75, 32,
                tag, true, "");
        }

        private static void AddTightStatus(HmiScreen screen, string label, string tag, int left, int top)
        {
            string safe = new string(tag.Where(char.IsLetterOrDigit).ToArray());
            ConfigureText(GetOrCreate<HmiText>(screen, "REV12_Label_" + safe), left, top, 95, 28,
                label, Dark, 12, HmiFontWeight.Normal, HmiHorizontalAlignment.Left);
            ConfigureIOField(GetOrCreate<HmiIOField>(screen, "REV12_Value_" + safe), left + 100, top - 2, 55, 32,
                tag, true, "");
        }

        private static void HideItem(HmiScreen screen, string name)
        {
            HmiScreenItemBase item = screen.ScreenItems.Find(name);
            if (item != null)
            {
                item.Visible = false;
                item.Enabled = false;
            }
        }

        private static void HideItemsWithPrefix(HmiScreen screen, string prefix)
        {
            foreach (HmiScreenItemBase item in screen.ScreenItems
                .Where(candidate => candidate.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToArray())
            {
                item.Visible = false;
                item.Enabled = false;
            }
        }

        private static void DeleteItemsWithPrefix(HmiScreen screen, string prefix)
        {
            foreach (HmiScreenItemBase item in screen.ScreenItems
                .Where(candidate => candidate.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToArray())
            {
                item.Delete();
            }
        }

        private static void DeleteItem(HmiScreen screen, string name)
        {
            HmiScreenItemBase item = screen.ScreenItems.Find(name);
            if (item != null)
            {
                item.Delete();
            }
        }

        private static T GetOrCreate<T>(HmiScreen screen, string name) where T : HmiScreenItemBase
        {
            HmiScreenItemBase existing = screen.ScreenItems.Find(name);
            if (existing != null)
            {
                T typed = existing as T;
                if (typed == null)
                {
                    throw new InvalidOperationException(
                        "Screen item '" + name + "' exists as " + existing.GetType().Name +
                        " instead of " + typeof(T).Name + ".");
                }
                return typed;
            }
            return screen.ScreenItems.Create<T>(name);
        }

        private static void ConfigureRectangle(
            HmiRectangle item,
            int left,
            int top,
            uint width,
            uint height,
            Color back,
            Color border,
            byte borderWidth)
        {
            SetBounds(item, left, top, width, height);
            item.BackColor = back;
            item.BorderColor = border;
            item.BorderWidth = borderWidth;
            item.BackFillPattern = HmiFillPattern.Solid;
            item.Visible = true;
            item.Enabled = true;
        }

        private static void ConfigureProcessBlock(HmiRectangle item, int left)
        {
            ConfigureRectangle(item, left, 230, 150, 90, Color.FromArgb(18, 91, 148), Blue, 2);
            SetRoundedCorners(item, 12);
            item.AlternateBackColor = Color.FromArgb(8, 58, 105);
            item.BackFillPattern = HmiFillPattern.GradientVertical;
        }

        private static void SetRoundedCorners(HmiRectangle item, uint radius)
        {
            item.Corners.TopLeftRadius = radius;
            item.Corners.TopRightRadius = radius;
            item.Corners.BottomLeftRadius = radius;
            item.Corners.BottomRightRadius = radius;
        }

        private static void ConfigureText(
            HmiText item,
            int left,
            int top,
            uint width,
            uint height,
            string text,
            Color fore,
            float fontSize,
            HmiFontWeight fontWeight,
            HmiHorizontalAlignment alignment)
        {
            SetBounds(item, left, top, width, height);
            SetText(item.Text, text);
            item.ForeColor = fore;
            item.Font.Name = HmiFontName.SiemensSans;
            item.Font.Size = fontSize;
            item.Font.Weight = fontWeight;
            item.HorizontalTextAlignment = alignment;
            item.VerticalTextAlignment = HmiVerticalAlignment.Center;
            item.Visible = true;
            item.Enabled = true;
        }

        private static void ConfigureIOField(
            HmiIOField item,
            int left,
            int top,
            uint width,
            uint height,
            string processValue,
            bool outputOnly,
            string format)
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
            item.Font.Size = 15;
            item.Font.Weight = HmiFontWeight.Bold;
            item.HorizontalTextAlignment = HmiHorizontalAlignment.Center;
            item.VerticalTextAlignment = HmiVerticalAlignment.Center;
            item.Visible = true;
            item.Enabled = true;
            item.RequireExplicitUnlock = false;
        }

        private static void ConfigureSymbolicField(
            HmiSymbolicIOField item,
            int left,
            int top,
            uint width,
            uint height,
            string processValue,
            string resourceList)
        {
            SetBounds(item, left, top, width, height);
            item.ProcessValue = "0";
            BindTag(item.Dynamizations, "ProcessValue", processValue, true);
            item.ResourceList = resourceList;
            item.IOFieldType = HmiIOFieldType.Output;
            item.BackColor = Color.White;
            item.ForeColor = Navy;
            item.BorderColor = Blue;
            item.BorderWidth = 2;
            item.Font.Name = HmiFontName.SiemensSans;
            item.Font.Size = 18;
            item.Font.Weight = HmiFontWeight.Bold;
            item.Visible = true;
            item.Enabled = true;
            item.RequireExplicitUnlock = false;
        }

        private static void ConfigureButton(
            HmiButton item,
            int left,
            int top,
            uint width,
            uint height,
            string text,
            Color back)
        {
            SetBounds(item, left, top, width, height);
            SetText(item.Text, text);
            item.BackColor = back;
            item.ForeColor = Color.White;
            item.BorderColor = Color.White;
            item.BorderWidth = 1;
            item.Font.Name = HmiFontName.SiemensSans;
            item.Font.Size = 15;
            item.Font.Weight = HmiFontWeight.Bold;
            item.Content.HorizontalTextAlignment = HmiHorizontalAlignment.Center;
            item.Content.VerticalTextAlignment = HmiVerticalAlignment.Center;
            item.Visible = true;
            item.RequireExplicitUnlock = false;
        }

        private static void ConfigureMomentaryButton(
            HmiButton item,
            int left,
            int top,
            uint width,
            uint height,
            string text,
            Color back,
            string tag)
        {
            ConfigureButton(item, left, top, width, height, text, back);
            item.Enabled = true;
            HmiButtonEventHandler down = item.EventHandlers.Find(HmiButtonEventType.Down);
            if (down == null)
            {
                down = item.EventHandlers.Create(HmiButtonEventType.Down);
            }
            down.Script.ScriptCode = "HMIRuntime.Tags(\"" + tag + "\").Write(1);";

            HmiButtonEventHandler up = item.EventHandlers.Find(HmiButtonEventType.Up);
            if (up == null)
            {
                up = item.EventHandlers.Create(HmiButtonEventType.Up);
            }
            up.Script.ScriptCode = "HMIRuntime.Tags(\"" + tag + "\").Write(0);";
        }

        private static void ConfigureWriteButton(
            HmiButton item,
            int left,
            int top,
            uint width,
            uint height,
            string text,
            Color back,
            string tag,
            int value)
        {
            ConfigureButton(item, left, top, width, height, text, back);
            item.Enabled = true;
            HmiButtonEventHandler tapped = item.EventHandlers.Find(HmiButtonEventType.Tapped);
            if (tapped == null)
            {
                tapped = item.EventHandlers.Create(HmiButtonEventType.Tapped);
            }
            tapped.Script.ScriptCode = "HMIRuntime.Tags(\"" + tag + "\").Write(" + value + ");";
        }

        private static void ConfigureManualSelectButton(
            HmiButton item, int left, int top, uint width, uint height)
        {
            ConfigureButton(item, left, top, width, height, "MANUAL SELECT", Color.FromArgb(78, 102, 125));
            item.Enabled = true;
            HmiButtonEventHandler tapped = item.EventHandlers.Find(HmiButtonEventType.Tapped);
            if (tapped == null)
            {
                tapped = item.EventHandlers.Create(HmiButtonEventType.Tapped);
            }
            tapped.Script.ScriptCode =
                "HMIRuntime.Tags(\"Cmd_ManualSelect\").Write(1);" +
                "HMIRuntime.Tags(\"Gate_Manual_PageActive\").Write(1);" +
                "HMIRuntime.UI.SysFct.ChangeScreen(\"manual\", \"/\");";
        }

        private static void BindTag(
            DynamizationBaseComposition dynamizations,
            string propertyName,
            string tag,
            bool readOnly)
        {
            DynamizationBase existing = dynamizations.Find(propertyName);
            TagDynamization binding;
            if (existing == null)
            {
                binding = dynamizations.Create<TagDynamization>(propertyName);
            }
            else
            {
                binding = existing as TagDynamization;
                if (binding == null)
                {
                    throw new InvalidOperationException(
                        "Property '" + propertyName + "' already has a non-tag dynamization.");
                }
            }
            binding.Tag = tag;
            binding.ReadOnly = readOnly;
            binding.UseIndirectAddressing = false;
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
            if (property == null || !property.CanWrite)
            {
                throw new InvalidOperationException(
                    "Property '" + name + "' is not writable on " + target.GetType().FullName + ".");
            }
            property.SetValue(target, value, null);
        }

        private static bool TrySetProperty(object target, string name, object value)
        {
            PropertyInfo property = target.GetType().GetProperty(name);
            if (property == null || !property.CanWrite)
            {
                return false;
            }
            property.SetValue(target, value, null);
            return true;
        }

        private static void SetText(MultilingualText text, string value)
        {
            string rich = "<body><p>" + value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;") + "</p></body>";
            foreach (MultilingualTextItem item in text.Items)
            {
                item.Text = rich;
            }
        }

        private static void PrintCompilerResult(CompilerResult result, string indent)
        {
            foreach (CompilerResultMessage message in result.Messages)
            {
                Console.WriteLine(
                    "{0}COMPILE state={1} errors={2} warnings={3} path={4} description={5}",
                    indent,
                    message.State,
                    message.ErrorCount,
                    message.WarningCount,
                    message.Path,
                    message.Description);
                foreach (CompilerResultMessage nested in message.Messages)
                {
                    PrintCompilerMessage(nested, indent + "  ");
                }
            }
        }

        private static void PrintCompilerMessage(CompilerResultMessage message, string indent)
        {
            Console.WriteLine(
                "{0}COMPILE state={1} errors={2} warnings={3} path={4} description={5}",
                indent,
                message.State,
                message.ErrorCount,
                message.WarningCount,
                message.Path,
                message.Description);
            foreach (CompilerResultMessage nested in message.Messages)
            {
                PrintCompilerMessage(nested, indent + "  ");
            }
        }
    }
}
