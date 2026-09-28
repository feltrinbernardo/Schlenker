using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using Siemens.Engineering;
using Siemens.Engineering.Connection;
using Siemens.Engineering.Download;
using Siemens.Engineering.Download.Configurations;
using Siemens.Engineering.HW;

namespace Schlenker.TiaV19
{
    internal static class DownloadAuthorizedSoftware
    {
        private const string ExpectedProject =
            @"C:\TIA Projects\Schlenkers 36-10 190036-7-8v2.12-MTP1200-DeployWorking-20260918\Backup Schlenkers 36-10 190036-7-8v2.12.ap19";
        private const string ExpectedPcInterface = "Intel(R) Ethernet Connection (2) I219-LM";

        private sealed class AuthorizedTarget
        {
            internal string DeviceName;
            internal string ProviderItemName;
            internal string TargetInterface;
            internal string AccessibleName;
            internal string Address;
            internal string Mac;
        }

        private static readonly AuthorizedTarget[] Targets =
        {
            new AuthorizedTarget
            {
                DeviceName = "S7-1500/ET200MP station_1",
                ProviderItemName = "PLC_1",
                TargetInterface = "1 X1",
                AccessibleName = "plc_1.profinet interface_1",
                Address = "192.168.10.1",
                Mac = "10-D6-57-85-C9-FA"
            },
            new AuthorizedTarget
            {
                DeviceName = "HMI_1",
                ProviderItemName = "HMI_RT_1",
                TargetInterface = "5 X1",
                AccessibleName = "hmi_1.profinet interface_1",
                Address = "192.168.10.2",
                Mac = "EC-1C-5D-31-CB-12"
            }
        };

        private static int Main(string[] args)
        {
            try
            {
                bool fullAuthorized = args.Length == 1 &&
                    args[0].Equals("--authorized-software-download", StringComparison.Ordinal);
                bool plcOnlyAuthorized = args.Length == 1 &&
                    args[0].Equals("--authorized-plc-software-download", StringComparison.Ordinal);
                bool plcHardwareSoftwareAuthorized = args.Length == 1 &&
                    args[0].Equals("--authorized-plc-hardware-software-download", StringComparison.Ordinal);
                bool hmiChangesAuthorized = args.Length == 1 &&
                    args[0].Equals("--authorized-hmi-changes-download", StringComparison.Ordinal);
                if (!fullAuthorized && !plcOnlyAuthorized && !plcHardwareSoftwareAuthorized && !hmiChangesAuthorized)
                    throw new InvalidOperationException("Explicit authorization switch is required.");

                TiaPortalProcess selected = TiaPortal.GetProcesses().SingleOrDefault(process =>
                    process.ProjectPath != null && process.ProjectPath.FullName.Equals(
                        ExpectedProject, StringComparison.OrdinalIgnoreCase));
                if (selected == null)
                    throw new InvalidOperationException("Exact authorized working project is not open.");

                using (TiaPortal portal = selected.Attach())
                {
                    Project project = portal.Projects.Single();
                    Console.WriteLine("PROJECT=" + project.Path.FullName);
                    foreach (AuthorizedTarget target in Targets.Where(item =>
                        fullAuthorized ||
                        (plcOnlyAuthorized && item.ProviderItemName.Equals("PLC_1", StringComparison.OrdinalIgnoreCase)) ||
                        (plcHardwareSoftwareAuthorized && item.ProviderItemName.Equals("PLC_1", StringComparison.OrdinalIgnoreCase)) ||
                        (hmiChangesAuthorized && item.ProviderItemName.Equals("HMI_RT_1", StringComparison.OrdinalIgnoreCase))))
                        DownloadTarget(project, target,
                            hmiChangesAuthorized ? DownloadOptions.SoftwareOnlyChanges :
                            plcHardwareSoftwareAuthorized ? DownloadOptions.Hardware | DownloadOptions.Software :
                            DownloadOptions.Software);
                }

                Console.WriteLine("OVERALL_STATUS=SUCCESS");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("OVERALL_STATUS=FAILED_OR_BLOCKED");
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static void DownloadTarget(Project project, AuthorizedTarget target, DownloadOptions options)
        {
            Device device = project.Devices.Single(item =>
                item.Name.Equals(target.DeviceName, StringComparison.OrdinalIgnoreCase));
            DeviceItem providerItem = FindItems(device.DeviceItems).Single(item =>
                item.Name.Equals(target.ProviderItemName, StringComparison.OrdinalIgnoreCase));
            DownloadProvider provider = providerItem.GetService<DownloadProvider>();
            if (provider == null)
                throw new InvalidOperationException("Download provider missing for " + target.ProviderItemName);

            ConnectionConfiguration connection = provider.Configuration;
            ConfigurationMode mode = connection.Modes.Single(item => item.Name.Equals("PN/IE", StringComparison.OrdinalIgnoreCase));
            ConfigurationPcInterface pc = mode.PcInterfaces.Single(item =>
                item.Name.Equals(ExpectedPcInterface, StringComparison.OrdinalIgnoreCase));
            ConfigurationTargetInterface targetInterface = pc.TargetInterfaces.Single(item =>
                item.Name.Equals(target.TargetInterface, StringComparison.OrdinalIgnoreCase));
            connection.ApplyConfiguration(targetInterface);

            bool identityMatch = false;
            string actualName = null;
            string actualAddress = null;
            string actualMac = null;
            foreach (ConfigurationAccessibleDevice item in pc.GetAccessibleDevices())
            {
                string name = item == null ? null : item.Name;
                string address = item == null ? null : item.Address;
                string mac = item == null ? null : item.MACAddress;
                if (string.Equals(name, target.AccessibleName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(address, target.Address, StringComparison.OrdinalIgnoreCase) &&
                    NormalizeMac(mac).Equals(NormalizeMac(target.Mac), StringComparison.OrdinalIgnoreCase))
                {
                    identityMatch = true;
                    actualName = name;
                    actualAddress = address;
                    actualMac = mac;
                }
            }
            if (!identityMatch)
            {
                if (!VerifyIpAndArpIdentity(target.Address, target.Mac))
                    throw new InvalidOperationException("Authorized target identity not present: " + target.ProviderItemName);
                identityMatch = true;
                actualName = target.AccessibleName + " (IP/ARP verified; DCP enumeration unavailable)";
                actualAddress = target.Address;
                actualMac = target.Mac;
            }

            Console.WriteLine("IDENTITY_PASS=" + target.ProviderItemName + "; NAME=" + actualName +
                "; ADDRESS=" + actualAddress + "; MAC=" + actualMac + "; INTERFACE=X1");
            DownloadResult result = provider.Download(
                targetInterface,
                ConfigurePreDownload,
                ConfigurePostDownload,
                options);
            PrintResult(target.ProviderItemName, result, "");
            if (result.State != DownloadResultState.Success || result.ErrorCount != 0)
                throw new InvalidOperationException("Software download did not succeed for " + target.ProviderItemName);
        }

        private static IEnumerable<DeviceItem> FindItems(DeviceItemComposition items)
        {
            foreach (DeviceItem item in items)
            {
                yield return item;
                foreach (DeviceItem child in FindItems(item.DeviceItems))
                    yield return child;
            }
        }

        private static string NormalizeMac(string value)
        {
            return new string((value ?? string.Empty).Where(Uri.IsHexDigit).ToArray()).ToUpperInvariant();
        }

        private static bool VerifyIpAndArpIdentity(string address, string expectedMac)
        {
            using (Ping ping = new Ping())
            {
                PingReply reply = ping.Send(address, 1500);
                if (reply == null || reply.Status != IPStatus.Success) return false;
            }
            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = "arp.exe",
                Arguments = "-a " + address,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            using (Process process = Process.Start(start))
            {
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return process.ExitCode == 0 &&
                    output.IndexOf(address, StringComparison.OrdinalIgnoreCase) >= 0 &&
                    NormalizeMac(output).IndexOf(NormalizeMac(expectedMac),
                        StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }

        private static void ConfigurePreDownload(DownloadConfiguration configuration)
        {
            Configure(configuration, "PRE");
        }

        private static void ConfigurePostDownload(DownloadConfiguration configuration)
        {
            Configure(configuration, "POST");
        }

        private static void Configure(DownloadConfiguration configuration, string phase)
        {
            string name = configuration.GetType().Name;
            Console.WriteLine(phase + "_CONFIG=" + name);

            if (name.IndexOf("Password", StringComparison.OrdinalIgnoreCase) >= 0)
                throw new InvalidOperationException("Password/security input required; automatic transfer blocked: " + name);

            switch (name)
            {
                case "AllBlocksDownload": SetSelection(configuration, "DownloadAllBlocks"); return;
                case "ConsistentBlocksDownload": SetSelection(configuration, "ConsistentDownload"); return;
                case "AlarmTextLibrariesDownload": SetSelection(configuration, "ConsistentDownload"); return;
                case "OverwriteSystemData": SetSelection(configuration, "Overwrite"); return;
                case "StopModules": SetSelection(configuration, "StopAll"); return;
                case "StartModules": SetSelection(configuration, "StartModule"); return;
                case "ActiveTestCanBeAborted": SetSelection(configuration, "AcceptAll"); return;
                case "ActiveTestCanPreventDownload": SetSelection(configuration, "AcceptAll"); return;
                case "ProtectionLevelChanged": SetSelection(configuration, "ContinueDownloading"); return;
                case "OverwriteHmiData": SetChecked(configuration, true); return;
                case "CheckBeforeDownload": SetChecked(configuration, true); return;
                case "FitHmiComponents": return;
                case "TurnOffSequence": return;
                case "WaitOnReboot": SetSelection(configuration, "WaitForReboot"); return;
                case "TargetForSoftware": SetSelection(configuration, "AcceptAll"); return;
                case "UserManagementDownload": SetSelection(configuration, "ConsistentDownload"); return;

                // These conditions would broaden the authorized operation or indicate a target mismatch.
                case "DifferentTargetConfiguration":
                case "DowngradeTargetDevice":
                case "UpgradeTargetDevice":
                case "ResetModule":
                case "InitializeMemory":
                case "DataBlockReinitialization":
                case "SelectiveDeleteDownload":
                case "OverwriteOnMemoryCard":
                case "LoadIdentificationData":
                case "ExpandDownload":
                case "DownloadCertificate":
                case "OverwriteTargetLanguages":
                case "PlcMasterSecretPassword":
                    throw new InvalidOperationException("Unexpected or destructive download configuration blocked: " + name);
                default:
                    throw new InvalidOperationException("Unknown download configuration blocked: " + name);
            }
        }

        private static void SetChecked(DownloadConfiguration configuration, bool value)
        {
            PropertyInfo property = configuration.GetType().GetProperty("Checked", BindingFlags.Public | BindingFlags.Instance);
            if (property == null || !property.CanWrite)
                throw new InvalidOperationException("Checked selection unavailable for " + configuration.GetType().Name);
            property.SetValue(configuration, value, null);
            Console.WriteLine("  SELECTED=Checked:" + value);
        }

        private static void SetSelection(DownloadConfiguration configuration, string desired)
        {
            PropertyInfo property = configuration.GetType().GetProperty("CurrentSelection", BindingFlags.Public | BindingFlags.Instance);
            if (property == null || !property.CanWrite || !property.PropertyType.IsEnum)
                throw new InvalidOperationException("CurrentSelection unavailable for " + configuration.GetType().Name);
            if (!Enum.GetNames(property.PropertyType).Contains(desired))
                throw new InvalidOperationException("Selection " + desired + " unavailable for " + configuration.GetType().Name);
            object value = Enum.Parse(property.PropertyType, desired);
            property.SetValue(configuration, value, null);
            Console.WriteLine("  SELECTED=" + desired);
        }

        private static void PrintResult(string target, DownloadResult result, string indent)
        {
            Console.WriteLine(indent + "RESULT=" + target + "; STATE=" + result.State +
                "; ERRORS=" + result.ErrorCount + "; WARNINGS=" + result.WarningCount);
            foreach (DownloadResultMessage message in result.Messages)
            {
                Console.WriteLine(indent + "MESSAGE=" + message.State + "; ERRORS=" + message.ErrorCount +
                    "; WARNINGS=" + message.WarningCount + "; TEXT=" + message.Message);
                PrintMessages(message.Messages, indent + "  ");
            }
        }

        private static void PrintMessages(DownloadResultMessageComposition messages, string indent)
        {
            foreach (DownloadResultMessage message in messages)
            {
                Console.WriteLine(indent + "MESSAGE=" + message.State + "; ERRORS=" + message.ErrorCount +
                    "; WARNINGS=" + message.WarningCount + "; TEXT=" + message.Message);
                PrintMessages(message.Messages, indent + "  ");
            }
        }
    }
}
