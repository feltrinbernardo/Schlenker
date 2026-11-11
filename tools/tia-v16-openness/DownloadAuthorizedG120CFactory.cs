using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using Siemens.Engineering;
using Siemens.Engineering.Connection;
using Siemens.Engineering.Download;
using Siemens.Engineering.Download.Configurations;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.MC.Drives;

namespace Schlenker.TiaV16
{
    internal static class DownloadAuthorizedG120CFactory
    {
        private const string AuthorizationSwitch =
            "--authorized-factory-reset-g120c-192.168.10.43";
        private const string InspectSwitch = "--inspect-drive-types";
        private const string ReplaceSwitch = "--replace-clean-drive-with-v47";
        private const string InspectBackupParametersSwitch =
            "--inspect-backup-motor-parameters";
        private const string CorrectDriveType =
            "OrderNumber:6SL3210-1KE11-8AF1/4.7";
        private const string WrongDriveType =
            "OrderNumber:6SL3210-1KE11-8AF1/4.7.10";
        private const string DeviceName = "SINAMICS G_1";
        private const string ProviderItemName = "VSD_CAP_DISTRIBUTOR";
        private const string ModeName = "PN/IE";
        private const string PcInterfaceName =
            "Intel(R) Ethernet Connection (2) I219-LM";
        private const string TargetInterfaceName = "0 X150";
        private const string ExpectedAddress = "192.168.10.43";
        private const string ExpectedMac = "00-1F-F8-E4-7B-8A";

        private static int Main(string[] args)
        {
            bool inspectOnly = args.Length == 2 &&
                args[0].Equals(InspectSwitch, StringComparison.Ordinal);
            bool replaceOnly = args.Length == 2 &&
                args[0].Equals(ReplaceSwitch, StringComparison.Ordinal);
            bool inspectBackupParametersOnly = args.Length == 2 &&
                args[0].Equals(InspectBackupParametersSwitch, StringComparison.Ordinal);
            bool authorizedDownload = args.Length == 2 &&
                args[0].Equals(AuthorizationSwitch, StringComparison.Ordinal);
            if (!inspectOnly && !replaceOnly && !inspectBackupParametersOnly &&
                !authorizedDownload)
            {
                Console.Error.WriteLine(
                    "Usage: DownloadAuthorizedG120CFactory " +
                    AuthorizationSwitch + "|" + InspectSwitch + "|" + ReplaceSwitch +
                    "|" + InspectBackupParametersSwitch +
                    " <exact-ap16-path>");
                return 2;
            }

            try
            {
                string projectPath = Path.GetFullPath(args[1]);
                TiaPortalProcess process = TiaPortal.GetProcesses().SingleOrDefault(candidate =>
                    candidate.ProjectPath != null &&
                    candidate.ProjectPath.FullName.Equals(
                        projectPath, StringComparison.OrdinalIgnoreCase));
                if (process == null)
                    throw new InvalidOperationException(
                        "The exact authorized TIA V16 project is not open.");

                if (authorizedDownload)
                {
                    VerifyIpAndArpIdentity(ExpectedAddress, ExpectedMac);
                    Console.WriteLine("AUTHORIZATION=EXACT_FACTORY_RESET_CONFIRMED");
                    Console.WriteLine("NETWORK_IDENTITY=PASS; ADDRESS=" + ExpectedAddress +
                        "; MAC=" + ExpectedMac);
                }

                using (TiaPortal portal = process.Attach())
                {
                    Project project = portal.Projects.Single();
                    if (!Path.GetFullPath(project.Path.FullName).Equals(
                            projectPath, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException(
                            "Authorized project identity changed.");

                    if (inspectOnly)
                    {
                        Console.WriteLine("MODE=READ_ONLY_TYPE_INSPECTION; DOWNLOAD=NO");
                        foreach (Device candidate in project.Devices)
                        {
                            Console.WriteLine("DEVICE=" + candidate.Name +
                                "; TYPE=" + candidate.TypeIdentifier);
                            PrintItems(candidate.DeviceItems, "  ");
                        }
                        Console.WriteLine("OVERALL_STATUS=SUCCESS");
                        return 0;
                    }

                    if (replaceOnly)
                    {
                        ReplaceCleanDriveWithV47(project);
                        Console.WriteLine("OVERALL_STATUS=SUCCESS");
                        return 0;
                    }

                    if (inspectBackupParametersOnly)
                    {
                        InspectBackupMotorParameters(project);
                        Console.WriteLine("OVERALL_STATUS=SUCCESS");
                        return 0;
                    }

                    Device device = project.Devices.Single(candidate =>
                        candidate.Name.Equals(DeviceName, StringComparison.OrdinalIgnoreCase));
                    DeviceItem providerItem = Find(device.DeviceItems, ProviderItemName);
                    if (providerItem == null)
                        throw new InvalidOperationException(
                            "The exact authorized drive provider item was not found.");

                    DownloadProvider provider = providerItem.GetService<DownloadProvider>();
                    if (provider == null)
                        throw new InvalidOperationException(
                            "The selected drive item has no DownloadProvider.");

                    ConnectionConfiguration connection = provider.Configuration;
                    ConfigurationMode mode = connection.Modes.Single(candidate =>
                        candidate.Name.Equals(ModeName, StringComparison.OrdinalIgnoreCase));
                    ConfigurationPcInterface pc = mode.PcInterfaces.Single(candidate =>
                        candidate.Name.Equals(PcInterfaceName, StringComparison.OrdinalIgnoreCase));
                    ConfigurationTargetInterface target = pc.TargetInterfaces.Single(candidate =>
                        candidate.Name.Equals(TargetInterfaceName, StringComparison.OrdinalIgnoreCase));
                    connection.ApplyConfiguration(target);

                    Console.WriteLine("PROJECT=" + project.Path.FullName);
                    Console.WriteLine("DEVICE=" + device.Name);
                    Console.WriteLine("PROVIDER_ITEM=" + providerItem.Name);
                    Console.WriteLine("MODE=" + mode.Name);
                    Console.WriteLine("PC_INTERFACE=" + pc.Name);
                    Console.WriteLine("TARGET_INTERFACE=" + target.Name);
                    Console.WriteLine("DOWNLOAD_OPTIONS=SOFTWARE");

                    DownloadResult result = provider.Download(
                        target,
                        ConfigurePreDownload,
                        ConfigurePostDownload,
                        DownloadOptions.Software);

                    PrintResult(result, string.Empty);
                    if (result.State != DownloadResultState.Success || result.ErrorCount != 0)
                        throw new InvalidOperationException(
                            "The authorized drive download did not complete successfully.");
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
            Type type = configuration.GetType();
            string name = type.Name;
            Console.WriteLine(phase + "_CONFIG=" + type.FullName);

            if (name.IndexOf("Password", StringComparison.OrdinalIgnoreCase) >= 0)
                throw new InvalidOperationException(
                    "Password input requested; automatic transfer blocked.");

            if (name.Equals("StartDriveDownloadCheckConfiguration",
                    StringComparison.OrdinalIgnoreCase) ||
                name.Equals("CheckBeforeDownload", StringComparison.OrdinalIgnoreCase))
            {
                SetBoolean(configuration, "Checked", true);
                return;
            }

            if (name.Equals("DownloadSelectionConfiguration",
                    StringComparison.OrdinalIgnoreCase))
            {
                SetInteger(configuration, "SelectedIndex", 0);
                return;
            }

            if (name.Equals("TurnOffSequence", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("FitHmiComponents", StringComparison.OrdinalIgnoreCase))
                return;

            throw new InvalidOperationException(
                "Unknown download confirmation blocked before acceptance: " + type.FullName);
        }

        private static void SetBoolean(
            DownloadConfiguration configuration, string propertyName, bool value)
        {
            PropertyInfo property = configuration.GetType().GetProperty(
                propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property == null || !property.CanWrite || property.PropertyType != typeof(bool))
                throw new InvalidOperationException(
                    propertyName + " is unavailable for " + configuration.GetType().FullName);
            property.SetValue(configuration, value, null);
            Console.WriteLine("  SELECTED=" + propertyName + ":" + value);
        }

        private static void SetInteger(
            DownloadConfiguration configuration, string propertyName, int value)
        {
            PropertyInfo property = configuration.GetType().GetProperty(
                propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property == null || !property.CanWrite || property.PropertyType != typeof(int))
                throw new InvalidOperationException(
                    propertyName + " is unavailable for " + configuration.GetType().FullName);
            property.SetValue(configuration, value, null);
            Console.WriteLine("  SELECTED=" + propertyName + ":" + value);
        }

        private static void VerifyIpAndArpIdentity(string address, string expectedMac)
        {
            using (Ping ping = new Ping())
            {
                PingReply reply = ping.Send(address, 1500);
                if (reply == null || reply.Status != IPStatus.Success)
                    throw new InvalidOperationException(
                        "Authorized target did not answer at " + address + ".");
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
                if (process.ExitCode != 0 ||
                    output.IndexOf(address, StringComparison.OrdinalIgnoreCase) < 0 ||
                    NormalizeMac(output).IndexOf(
                        NormalizeMac(expectedMac), StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException(
                        "ARP identity does not match the authorized G120C target.");
            }
        }

        private static string NormalizeMac(string value)
        {
            return new string((value ?? string.Empty)
                .Where(Uri.IsHexDigit).ToArray()).ToUpperInvariant();
        }

        private static DeviceItem Find(DeviceItemComposition items, string name)
        {
            foreach (DeviceItem item in items)
            {
                if (item.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return item;
                DeviceItem nested = Find(item.DeviceItems, name);
                if (nested != null)
                    return nested;
            }

            return null;
        }

        private static void PrintItems(DeviceItemComposition items, string indent)
        {
            foreach (DeviceItem item in items)
            {
                Console.WriteLine(indent + "ITEM=" + item.Name +
                    "; TYPE=" + item.TypeIdentifier +
                    "; POSITION=" + item.PositionNumber);
                PrintItems(item.DeviceItems, indent + "  ");
            }
        }

        private static void ReplaceCleanDriveWithV47(Project project)
        {
            const string temporaryDeviceName = "SINAMICS_G120C_V47_TEMP";
            const string temporaryItemName = "VSD_CAP_DISTRIBUTOR_V47_TEMP";

            Device oldDevice = project.Devices.Single(candidate =>
                candidate.Name.Equals(DeviceName, StringComparison.OrdinalIgnoreCase));
            DeviceItem oldProviderItem = Find(oldDevice.DeviceItems, ProviderItemName);
            if (oldProviderItem == null ||
                !WrongDriveType.Equals(oldProviderItem.TypeIdentifier,
                    StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "The clean source drive is not the expected V4.7.10 object.");

            if (project.Devices.Any(candidate =>
                    candidate.Name.Equals(temporaryDeviceName,
                        StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Temporary drive name already exists.");

            Device replacement = null;
            bool oldDeleted = false;
            try
            {
                replacement = project.Devices.CreateWithItem(
                    CorrectDriveType, temporaryItemName, temporaryDeviceName);
                DeviceItem replacementItem = FindByType(
                    replacement.DeviceItems, CorrectDriveType);
                if (replacementItem == null)
                    throw new InvalidOperationException(
                        "TIA did not create the requested V4.7 drive type.");

                Node node = FindEthernetNodes(replacement.DeviceItems).Single();
                IEngineeringObject attributes = node;
                attributes.SetAttribute("Address", ExpectedAddress);
                attributes.SetAttribute("SubnetMask", "255.255.255.0");
                attributes.SetAttribute("PnDeviceNameAutoGeneration", false);
                attributes.SetAttribute("PnDeviceName", "vsd-cap-distributor");

                string actualAddress = attributes.GetAttribute("Address") as string;
                string actualPnName = attributes.GetAttribute("PnDeviceName") as string;
                if (!ExpectedAddress.Equals(actualAddress, StringComparison.OrdinalIgnoreCase) ||
                    !"vsd-cap-distributor".Equals(actualPnName,
                        StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        "The replacement drive network identity failed validation.");

                oldDevice.Delete();
                oldDeleted = true;
                replacement.Name = DeviceName;
                replacementItem.Name = ProviderItemName;
                project.Save();

                Device savedDevice = project.Devices.Single(candidate =>
                    candidate.Name.Equals(DeviceName, StringComparison.OrdinalIgnoreCase));
                DeviceItem savedItem = Find(savedDevice.DeviceItems, ProviderItemName);
                if (savedItem == null ||
                    !CorrectDriveType.Equals(savedItem.TypeIdentifier,
                        StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        "Saved replacement drive validation failed.");

                Console.WriteLine("MODE=OFFLINE_REPLACE_CLEAN_DRIVE; DOWNLOAD=NO");
                Console.WriteLine("REPLACED_TYPE=" + WrongDriveType);
                Console.WriteLine("NEW_TYPE=" + savedItem.TypeIdentifier);
                Console.WriteLine("DEVICE=" + savedDevice.Name);
                Console.WriteLine("PROVIDER_ITEM=" + savedItem.Name);
                Console.WriteLine("ADDRESS=" + actualAddress);
                Console.WriteLine("PN_NAME=" + actualPnName);
                Console.WriteLine("PROJECT_SAVE=YES");
            }
            catch
            {
                if (!oldDeleted && replacement != null)
                {
                    try { replacement.Delete(); }
                    catch { }
                }
                throw;
            }
        }

        private static DeviceItem FindByType(
            DeviceItemComposition items, string typeIdentifier)
        {
            foreach (DeviceItem item in items)
            {
                if (typeIdentifier.Equals(item.TypeIdentifier,
                        StringComparison.OrdinalIgnoreCase))
                    return item;
                DeviceItem nested = FindByType(item.DeviceItems, typeIdentifier);
                if (nested != null)
                    return nested;
            }

            return null;
        }

        private static System.Collections.Generic.IEnumerable<Node> FindEthernetNodes(
            DeviceItemComposition items)
        {
            foreach (DeviceItem item in items)
            {
                Siemens.Engineering.HW.Features.NetworkInterface network =
                    item.GetService<Siemens.Engineering.HW.Features.NetworkInterface>();
                if (network != null)
                    foreach (Node node in network.Nodes)
                        if (node.NodeType == NetType.Ethernet)
                            yield return node;
                foreach (Node nested in FindEthernetNodes(item.DeviceItems))
                    yield return nested;
            }
        }

        private static void InspectBackupMotorParameters(Project project)
        {
            Device backupDevice = project.Devices.Single(candidate =>
                candidate.Name.Equals("SINAMICS G_2", StringComparison.OrdinalIgnoreCase));
            DeviceItem backupCu = Find(
                backupDevice.DeviceItems, "SL1SD-M01 Magasin 1");
            if (backupCu == null)
                throw new InvalidOperationException("Backup drive CU was not found.");

            DriveObjectContainer container = backupCu.GetService<DriveObjectContainer>();
            if (container == null || container.DriveObjects.Count == 0)
                throw new InvalidOperationException(
                    "Backup drive object parameters are unavailable.");

            int[] numbers =
            {
                10, 96, 100, 300, 304, 305, 307, 308, 310, 311,
                700, 1000, 1080, 1082, 1120, 1121, 1300, 1900, 3900,
                9601, 9400
            };

            Console.WriteLine("MODE=READ_ONLY_BACKUP_PARAMETER_INSPECTION; DOWNLOAD=NO");
            Console.WriteLine("DEVICE=" + backupDevice.Name);
            Console.WriteLine("PROVIDER_ITEM=" + backupCu.Name);
            Console.WriteLine("DRIVE_OBJECT_COUNT=" + container.DriveObjects.Count);
            for (int objectIndex = 0;
                objectIndex < container.DriveObjects.Count;
                objectIndex++)
            {
                DriveObject driveObject = container.DriveObjects[objectIndex];
                Console.WriteLine("DRIVE_OBJECT_INDEX=" + objectIndex +
                    "; OBJECT=" + driveObject);
                DriveParameter commissioningFilter =
                    driveObject.Parameters.Find(10, -1);
                object originalFilter = commissioningFilter == null
                    ? null
                    : commissioningFilter.Value;
                bool filterChanged = false;
                try
                {
                    if (commissioningFilter != null &&
                        Convert.ToInt32(originalFilter,
                            System.Globalization.CultureInfo.InvariantCulture) != 1)
                    {
                        commissioningFilter.Value = 1;
                        filterChanged = true;
                        Console.WriteLine("TEMPORARY_FILTER=p10:1; SAVE=NO; DOWNLOAD=NO");
                    }

                    foreach (int number in numbers)
                    {
                        bool found = false;
                        foreach (int arrayIndex in new[] { -1, 0, 1 })
                        {
                            DriveParameter parameter =
                                driveObject.Parameters.Find(number, arrayIndex);
                            if (parameter == null)
                                continue;
                            found = true;
                            Console.WriteLine("PARAMETER=" + parameter.Name +
                                "; VALUE=" + Convert.ToString(
                                    parameter.Value,
                                    System.Globalization.CultureInfo.InvariantCulture) +
                                "; UNIT=" + parameter.Unit +
                                "; TEXT=" + parameter.ParameterText +
                                "; OBJECT_INDEX=" + objectIndex);
                        }
                        if (!found)
                            Console.WriteLine("PARAMETER=p" + number +
                                "; STATUS=NOT_FOUND; OBJECT_INDEX=" + objectIndex);
                    }
                }
                finally
                {
                    if (filterChanged)
                    {
                        commissioningFilter.Value = originalFilter;
                        Console.WriteLine("TEMPORARY_FILTER_RESTORED=p10:" +
                            Convert.ToString(originalFilter,
                                System.Globalization.CultureInfo.InvariantCulture));
                    }
                }
            }
        }

        private static void PrintResult(DownloadResult result, string indent)
        {
            Console.WriteLine(indent + "RESULT_STATE=" + result.State +
                "; ERRORS=" + result.ErrorCount +
                "; WARNINGS=" + result.WarningCount);
            foreach (DownloadResultMessage message in result.Messages)
                PrintMessage(message, indent + "  ");
        }

        private static void PrintMessage(DownloadResultMessage message, string indent)
        {
            Console.WriteLine(indent + "MESSAGE_STATE=" + message.State +
                "; ERRORS=" + message.ErrorCount +
                "; WARNINGS=" + message.WarningCount +
                "; TEXT=" + message.Message);
            foreach (DownloadResultMessage nested in message.Messages)
                PrintMessage(nested, indent + "  ");
        }
    }
}
