using System;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.Connection;
using Siemens.Engineering.HW;
using Siemens.Engineering.Online;

namespace Schlenker.TiaV19
{
    internal static class AuditOnlineProvidersReadOnly
    {
        private static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("Usage: AuditOnlineProvidersReadOnly <exact-ap19-path> <report-path>");
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
                    report.WriteLine("MODE=READ_ONLY_SERVICE_AUDIT; SAVE=NO; DOWNLOAD=NO; ONLINE_WRITE=NO");
                    report.WriteLine("PROJECT=" + project.Path.FullName);
                    foreach (Device device in project.Devices)
                    {
                        report.WriteLine("DEVICE=" + device.Name);
                        Inspect(device.DeviceItems, report, "  ");
                    }
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

        private static void Inspect(DeviceItemComposition items, StreamWriter report, string indent)
        {
            foreach (DeviceItem item in items)
            {
                OnlineProvider provider = item.GetService<OnlineProvider>();
                report.WriteLine(indent + "ITEM=" + item.Name + "; ONLINE_PROVIDER=" + (provider != null));
                if (provider != null)
                {
                    report.WriteLine(indent + "  STATE=" + provider.State);
                    ConnectionConfiguration configuration = provider.Configuration;
                    report.WriteLine(indent + "  CONFIGURED=" + configuration.IsConfigured);
                    foreach (ConfigurationMode mode in configuration.Modes)
                    {
                        report.WriteLine(indent + "  MODE=" + mode.Name);
                        foreach (ConfigurationPcInterface pc in mode.PcInterfaces)
                        {
                            if (pc.Name.IndexOf("I219-LM", StringComparison.OrdinalIgnoreCase) < 0) continue;
                            report.WriteLine(indent + "    PC=" + pc.Name);
                            foreach (ConfigurationTargetInterface target in pc.TargetInterfaces)
                            {
                                report.WriteLine(indent + "      TARGET=" + target.Name);
                                foreach (ConfigurationAddress address in target.Addresses)
                                    report.WriteLine(indent + "        ADDRESS=" + address.Name + "; VALUE=" + address.Address);
                            }
                        }
                    }
                }
                Inspect(item.DeviceItems, report, indent + "  ");
            }
        }
    }
}
