using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.HmiUnified.UI.Base;
using Siemens.Engineering.HmiUnified.UI.Parts;
using Siemens.Engineering.HmiUnified.UI.Shapes;
using Siemens.Engineering.HmiUnified.UI.Screens;
using Siemens.Engineering.SW;

namespace Schlenker.TiaV19
{
    internal static class AuditCommonRenderTarget
    {
        private static int Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.Error.WriteLine("Usage: AuditCommonRenderTarget <project.ap19> <screen> <report.tsv>");
                return 2;
            }

            string projectPath = Path.GetFullPath(args[0]);
            string screenName = args[1];
            string reportPath = Path.GetFullPath(args[2]);

            try
            {
                List<TiaPortalProcess> matches = TiaPortal.GetProcesses()
                    .Where(candidate => candidate.ProjectPath != null &&
                        Path.GetFullPath(candidate.ProjectPath.FullName)
                            .Equals(projectPath, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (matches.Count != 1)
                {
                    throw new InvalidOperationException(String.Format(
                        "Expected one open TIA project at '{0}', found {1}.", projectPath, matches.Count));
                }

                List<string> issues = new List<string>();
                using (TiaPortal portal = matches[0].Attach())
                {
                    Project project = portal.Projects.FirstOrDefault();
                    if (project == null) throw new InvalidOperationException("No open project.");
                    HmiSoftware hmi = FindHmi(project);
                    if (hmi == null) throw new InvalidOperationException("No Unified HMI found.");
                    HmiScreen screen = hmi.Screens.Find(screenName);
                    if (screen == null) throw new InvalidOperationException("Screen not found: " + screenName);

                    HmiText page = screen.ScreenItems.Find("REV13_Common_Page") as HmiText;
                    if (page == null)
                    {
                        issues.Add("REV13_Common_Page missing");
                    }
                    else
                    {
                        Check(issues, "REV13_Common_Page.Left", page.Left, 1010);
                        Check(issues, "REV13_Common_Page.Top", page.Top, 10);
                        Check(issues, "REV13_Common_Page.Width", page.Width, 335);
                        Check(issues, "REV13_Common_Page.Height", page.Height, 34);
                    }

                    AuditSeparator(screen, "Line_1", 1029, 60, issues);
                    AuditSeparator(screen, "Line_2", 1215, 61, issues);
                }

                Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
                using (StreamWriter writer = new StreamWriter(reportPath, false, System.Text.Encoding.UTF8))
                {
                    writer.WriteLine("Screen\tStatus\tIssueCount\tIssues");
                    writer.WriteLine(screenName + "\t" + (issues.Count == 0 ? "VALIDATED" : "FAILED") +
                        "\t" + issues.Count + "\t" + String.Join(" | ", issues));
                }

                Console.WriteLine("TARGET=" + screenName);
                Console.WriteLine("AUDIT_SCOPE=REV13_Common_Page,Line_1,Line_2");
                Console.WriteLine("ISSUES=" + issues.Count);
                Console.WriteLine("REPORT=" + reportPath);
                Console.WriteLine("STATUS=" + (issues.Count == 0 ? "PASS" : "FAIL"));
                return issues.Count == 0 ? 0 : 1;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("STATUS=FAIL");
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static void AuditSeparator(
            HmiScreen screen,
            string name,
            int left,
            int top,
            ICollection<string> issues)
        {
            HmiLine line = screen.ScreenItems.Find(name) as HmiLine;
            if (line == null)
            {
                issues.Add(name + " missing");
                return;
            }
            Check(issues, name + ".Left", line.Left, left);
            Check(issues, name + ".Top", line.Top, top);
            Check(issues, name + ".Width", line.Width, 1);
            Check(issues, name + ".Height", line.Height, 31);
            Check(issues, name + ".X1", line.X1, 0);
            Check(issues, name + ".Y1", line.Y1, 0);
            Check(issues, name + ".X2", line.X2, 0);
            Check(issues, name + ".Y2", line.Y2, 31);
            if (!line.Visible) issues.Add(name + ".Visible=False expected True");
        }

        private static void Check(ICollection<string> issues, string property, int actual, int expected)
        {
            if (actual != expected) issues.Add(property + "=" + actual + " expected " + expected);
        }

        private static void Check(ICollection<string> issues, string property, uint actual, uint expected)
        {
            if (actual != expected) issues.Add(property + "=" + actual + " expected " + expected);
        }

        private static HmiSoftware FindHmi(Project project)
        {
            foreach (Device device in project.Devices)
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
