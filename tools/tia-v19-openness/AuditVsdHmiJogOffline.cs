using System;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.HmiUnified.HmiTags;
using Siemens.Engineering.HmiUnified.UI.Base;
using Siemens.Engineering.HmiUnified.UI.Controls;
using Siemens.Engineering.HmiUnified.UI.Enum;
using Siemens.Engineering.HmiUnified.UI.Events;
using Siemens.Engineering.HmiUnified.UI.Screens;
using Siemens.Engineering.HmiUnified.UI.Widgets;

namespace Schlenker.TiaV19
{
    internal static class AuditVsdHmiJogOffline
    {
        private static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("Usage: AuditVsdHmiJogOffline <exact-ap19-path> <report-path>");
                return 2;
            }

            string projectPath = Path.GetFullPath(args[0]);
            string reportPath = Path.GetFullPath(args[1]);
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));

            try
            {
                using (StreamWriter report = new StreamWriter(reportPath, false))
                {
                    TiaPortalProcess process = TiaPortal.GetProcesses().FirstOrDefault(candidate =>
                        candidate.ProjectPath != null && candidate.ProjectPath.FullName.Equals(
                            projectPath, StringComparison.OrdinalIgnoreCase));
                    bool attached = process != null;
                    using (TiaPortal portal = attached
                        ? process.Attach()
                        : new TiaPortal(TiaPortalMode.WithoutUserInterface))
                    {
                    Project project = attached
                        ? portal.Projects.First()
                        : portal.Projects.Open(new FileInfo(projectPath));
                    HmiSoftware hmi = FindUnifiedHmi(project);
                    if (hmi == null)
                        throw new InvalidOperationException("Unified HMI software was not found.");

                    report.WriteLine("SCHLENKER VSD HMI JOG OFFLINE AUDIT");
                    report.WriteLine("Project=" + project.Path.FullName);
                    report.WriteLine("Timestamp=" + DateTimeOffset.Now.ToString("o"));
                    report.WriteLine("Mode=READ_ONLY_" + (attached ? "ATTACHED" : "OFFLINE") +
                        "; SAVE=NO; DOWNLOAD=NO; ONLINE_WRITE=NO");

                    InspectTag(hmi, report, "Speed_Setpoint_BPH");
                    InspectTag(hmi, report, "Cmd_ManualSelect");
                    InspectTag(hmi, report, "Jog_PB");
                    InspectTag(hmi, report, "Machine_Manual_Active");
                    InspectTag(hmi, report, "VSD_Main_Command_Hz");
                    InspectTag(hmi, report, "VSD_Main_Actual_Hz");
                    InspectTag(hmi, report, "VSD_Main_Ready");
                    InspectTag(hmi, report, "VSD_Main_Running");
                    InspectTag(hmi, report, "VSD_Main_Fault");

                    HmiScreen manual = hmi.Screens.Find("manual");
                    if (manual == null)
                        throw new InvalidOperationException("Manual screen was not found.");
                    InspectButton(manual, report, "REV12_Manual_Jog_Enable");

                    report.WriteLine("STATUS=PASS");
                    report.Flush();
                    if (!attached) project.Close();
                    }
                }
                Console.WriteLine("STATUS=PASS");
                Console.WriteLine("REPORT=" + reportPath);
                return 0;
            }
            catch (Exception exception)
            {
                File.AppendAllText(reportPath, Environment.NewLine + "STATUS=FAIL" + Environment.NewLine + exception + Environment.NewLine);
                Console.Error.WriteLine("STATUS=FAIL");
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static void InspectTag(HmiSoftware hmi, StreamWriter report, string name)
        {
            HmiTag found = null;
            string tableName = "";
            foreach (HmiTagTable table in hmi.TagTables)
            {
                HmiTag candidate = table.Tags.Find(name);
                if (candidate != null)
                {
                    found = candidate;
                    tableName = table.Name;
                    break;
                }
            }
            if (found == null)
            {
                report.WriteLine("TAG=" + name + "; FOUND=NO");
                return;
            }
            report.WriteLine("TAG=" + name + "; FOUND=YES; TABLE=" + tableName +
                "; CONNECTION=" + found.Connection + "; PLC_TAG=" + found.PlcTag +
                "; ACQUISITION=" + found.AcquisitionMode + "; CYCLE=" + found.AcquisitionCycle);
        }

        private static void InspectButton(HmiScreen screen, StreamWriter report, string name)
        {
            HmiButton button = screen.ScreenItems.Find(name) as HmiButton;
            if (button == null)
            {
                report.WriteLine("BUTTON=" + name + "; FOUND=NO");
                return;
            }
            report.WriteLine("BUTTON=" + name + "; FOUND=YES; ENABLED=" + button.Enabled +
                "; VISIBLE=" + button.Visible);
            foreach (HmiButtonEventType eventType in new[] { HmiButtonEventType.Down, HmiButtonEventType.Up, HmiButtonEventType.Tapped })
            {
                HmiButtonEventHandler handler = button.EventHandlers.Find(eventType);
                report.WriteLine("EVENT=" + eventType + "; SCRIPT=" +
                    (handler == null ? "<missing>" : handler.Script.ScriptCode));
            }
        }

        private static HmiSoftware FindUnifiedHmi(Project project)
        {
            foreach (Device device in project.Devices)
            {
                HmiSoftware hmi = FindUnifiedHmi(device.DeviceItems);
                if (hmi != null) return hmi;
            }
            return null;
        }

        private static HmiSoftware FindUnifiedHmi(DeviceItemComposition items)
        {
            foreach (DeviceItem item in items)
            {
                SoftwareContainer container = item.GetService<SoftwareContainer>();
                HmiSoftware hmi = container == null ? null : container.Software as HmiSoftware;
                if (hmi != null) return hmi;
                hmi = FindUnifiedHmi(item.DeviceItems);
                if (hmi != null) return hmi;
            }
            return null;
        }
    }
}
