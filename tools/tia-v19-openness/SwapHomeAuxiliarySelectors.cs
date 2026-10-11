using System;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.Compiler;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.HmiUnified.UI.Screens;
using Siemens.Engineering.HmiUnified.UI.Widgets;
using Siemens.Engineering.SW;

namespace Schlenker.TiaV19
{
    internal static class SwapHomeAuxiliarySelectors
    {
        private static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("Usage: SwapHomeAuxiliarySelectors <exact-ap19> <report>");
                return 2;
            }

            string projectPath = Path.GetFullPath(args[0]);
            string reportPath = Path.GetFullPath(args[1]);
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));

            try
            {
                TiaPortalProcess process = TiaPortal.GetProcesses().SingleOrDefault(candidate =>
                    candidate.ProjectPath != null &&
                    Path.GetFullPath(candidate.ProjectPath.FullName).Equals(
                        projectPath, StringComparison.OrdinalIgnoreCase));
                if (process == null) throw new InvalidOperationException("The exact TIA project is not open.");

                using (StreamWriter report = new StreamWriter(reportPath, false))
                using (TiaPortal portal = process.Attach())
                {
                    Project project = portal.Projects.Single();
                    HmiSoftware hmi;
                    Device hmiDevice;
                    DeviceItem hmiItem;
                    FindHmi(project, out hmi, out hmiDevice, out hmiItem);
                    if (hmi == null) throw new InvalidOperationException("Unified HMI not found.");
                    HmiScreen screen = hmi.Screens.Find("home");
                    if (screen == null) throw new InvalidOperationException("Home screen not found.");

                    HmiButton lightsOff = Require(screen, "REV12_Lights_Off");
                    HmiButton lightsOn = Require(screen, "REV12_Lights_On");
                    HmiButton filterOff = Require(screen, "REV12_Filter_Off");
                    HmiButton filterOn = Require(screen, "REV12_Filter_On");

                    lightsOff.Left = 45;
                    lightsOn.Left = 165;
                    filterOff.Left = 45;
                    filterOn.Left = 165;

                    if (lightsOff.Left != 45 || lightsOn.Left != 165 ||
                        filterOff.Left != 45 || filterOn.Left != 165)
                        throw new InvalidOperationException("Selector positions were not applied.");

                    ICompilable compilable = hmiItem.GetService<ICompilable>() ??
                        hmiDevice.GetService<ICompilable>();
                    CompilerResult result = compilable.Compile();
                    int errors = result.ErrorCount;
                    int warnings = result.WarningCount;
                    report.WriteLine("Screen=home");
                    report.WriteLine("Lights=OFF_LEFT_45;ON_RIGHT_165");
                    report.WriteLine("Filter=OFF_LEFT_45;ON_RIGHT_165");
                    report.WriteLine("RootErrors=" + errors);
                    report.WriteLine("RootWarnings=" + warnings);
                    if (errors != 0)
                    {
                        report.WriteLine("SaveInvoked=NO");
                        report.WriteLine("STATUS=FAIL_NOT_SAVED");
                        return 1;
                    }

                    project.Save();
                    report.WriteLine("SaveInvoked=YES");
                    report.WriteLine("STATUS=PASS");
                    Console.WriteLine("ERRORS=" + errors);
                    Console.WriteLine("WARNINGS=" + warnings);
                    Console.WriteLine("STATUS=PASS");
                    return 0;
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

        private static HmiButton Require(HmiScreen screen, string name)
        {
            HmiButton item = screen.ScreenItems.Find(name) as HmiButton;
            if (item == null) throw new InvalidOperationException("Missing HMI button: " + name);
            return item;
        }

        private static void FindHmi(Project project, out HmiSoftware hmi,
            out Device hmiDevice, out DeviceItem hmiItem)
        {
            hmi = null;
            hmiDevice = null;
            hmiItem = null;
            foreach (Device device in project.Devices)
            foreach (DeviceItem item in Flatten(device.DeviceItems))
            {
                SoftwareContainer container = item.GetService<SoftwareContainer>();
                HmiSoftware candidate = container == null ? null : container.Software as HmiSoftware;
                if (candidate == null) continue;
                hmi = candidate;
                hmiDevice = device;
                hmiItem = item;
                return;
            }
        }

        private static System.Collections.Generic.IEnumerable<DeviceItem> Flatten(
            DeviceItemComposition items)
        {
            foreach (DeviceItem item in items)
            {
                yield return item;
                foreach (DeviceItem child in Flatten(item.DeviceItems)) yield return child;
            }
        }
    }
}
