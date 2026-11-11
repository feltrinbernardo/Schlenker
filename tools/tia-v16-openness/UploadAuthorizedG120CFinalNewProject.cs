using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using Siemens.Engineering;
using Siemens.Engineering.Connection;
using Siemens.Engineering.HW;
using Siemens.Engineering.Upload;
using Siemens.Engineering.Upload.Configurations;

namespace Schlenker.TiaV16
{
    internal static class UploadAuthorizedG120CFinalNewProject
    {
        private const string AuthorizationSwitch =
            "--authorized-read-g120c-192.168.10.43";
        private const string ExpectedAddress = "192.168.10.43";
        private const string ExpectedMac = "00-1F-F8-E4-7B-8A";
        private const string PcInterfaceName =
            "Intel(R) Ethernet Connection (2) I219-LM";
        private const string FinalDeviceName =
            "G120C_CAP_DISTRIBUTOR_FINAL_20261008";

        private static int Main(string[] args)
        {
            if (args.Length != 3 ||
                !args[0].Equals(AuthorizationSwitch, StringComparison.Ordinal))
            {
                Console.Error.WriteLine(
                    "Usage: UploadAuthorizedG120CFinalNewProject " +
                    AuthorizationSwitch + " <new-project-directory> <project-name>");
                return 2;
            }

            try
            {
                string projectDirectory = Path.GetFullPath(args[1]);
                string projectName = args[2];
                if (Directory.Exists(projectDirectory))
                    throw new InvalidOperationException(
                        "The final project directory already exists: " + projectDirectory);

                VerifyIpAndArpIdentity(ExpectedAddress, ExpectedMac);

                using (TiaPortal portal = new TiaPortal(TiaPortalMode.WithoutUserInterface))
                {
                    Project project = portal.Projects.Create(
                        new DirectoryInfo(projectDirectory), projectName);
                    Console.WriteLine("TIA_SESSION=HEADLESS_V16_NEW_PROJECT");
                    Console.WriteLine("PROJECT=" + project.Path.FullName);

                    StationUploadProvider provider =
                        project.GetService<StationUploadProvider>();
                    if (provider == null)
                        throw new InvalidOperationException(
                            "StationUploadProvider is unavailable.");

                    ConfigurationMode mode = provider.Configuration.Modes.Single(candidate =>
                        candidate.Name.Equals("PN/IE", StringComparison.OrdinalIgnoreCase));
                    ConfigurationPcInterface pc = mode.PcInterfaces.Single(candidate =>
                        candidate.Name.Equals(PcInterfaceName, StringComparison.OrdinalIgnoreCase));
                    ConfigurationAddress address = pc.Addresses.Find(ExpectedAddress) ??
                        pc.Addresses.Create(ExpectedAddress);

                    Console.WriteLine("MODE=AUTHORIZED_DEVICE_READ; DEVICE_WRITE=NO");
                    Console.WriteLine("NETWORK_IDENTITY=PASS; ADDRESS=" + ExpectedAddress +
                        "; MAC=" + ExpectedMac);
                    Console.WriteLine("PC_INTERFACE=" + pc.Name);

                    UploadResult result = provider.StationUpload(address, ConfigureUpload);
                    PrintResult(result, string.Empty);
                    if (result.State != UploadResultState.Success ||
                        result.ErrorCount != 0 || result.UploadedStation == null)
                        throw new InvalidOperationException(
                            "The final station upload did not complete successfully.");

                    result.UploadedStation.Name = FinalDeviceName;
                    project.Save();
                    Console.WriteLine("UPLOADED_DEVICE=" + result.UploadedStation.Name);
                    Console.WriteLine("PROJECT_SAVE=YES");
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

        private static void ConfigureUpload(UploadConfiguration configuration)
        {
            Console.WriteLine("UPLOAD_CONFIG=" + configuration.GetType().FullName +
                "; MESSAGE=" + configuration.Message);
            if (configuration is UploadPasswordConfiguration)
                throw new InvalidOperationException(
                    "Password input requested; automatic upload blocked.");

            UploadMissingProducts missing = configuration as UploadMissingProducts;
            if (missing != null)
            {
                missing.CurrentSelection = UploadMissingProductsSelections.TryUpload;
                Console.WriteLine("  SELECTED=TryUpload");
                return;
            }

            throw new InvalidOperationException(
                "Unknown upload confirmation blocked: " +
                configuration.GetType().FullName);
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

        private static void PrintResult(UploadResult result, string indent)
        {
            Console.WriteLine(indent + "RESULT_STATE=" + result.State +
                "; ERRORS=" + result.ErrorCount +
                "; WARNINGS=" + result.WarningCount);
            foreach (UploadResultMessage message in result.Messages)
                PrintMessage(message, indent + "  ");
        }

        private static void PrintMessage(UploadResultMessage message, string indent)
        {
            Console.WriteLine(indent + "MESSAGE_STATE=" + message.State +
                "; ERRORS=" + message.ErrorCount +
                "; WARNINGS=" + message.WarningCount +
                "; TEXT=" + message.Message);
            foreach (UploadResultMessage nested in message.Messages)
                PrintMessage(nested, indent + "  ");
        }
    }
}
