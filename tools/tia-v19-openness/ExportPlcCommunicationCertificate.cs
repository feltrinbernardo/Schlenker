using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.Security;

namespace Schlenker.TiaV19
{
    internal static class ExportPlcCommunicationCertificate
    {
        private static int Main(string[] args)
        {
            if (args.Length != 2) return 2;
            string projectPath = Path.GetFullPath(args[0]);
            string outputPath = Path.GetFullPath(args[1]);
            TiaPortalProcess process = TiaPortal.GetProcesses().SingleOrDefault(candidate =>
                candidate.ProjectPath != null && candidate.ProjectPath.FullName.Equals(
                    projectPath, StringComparison.OrdinalIgnoreCase));
            if (process == null) throw new InvalidOperationException("Exact project is not open.");
            using (TiaPortal portal = process.Attach())
            {
                Project project = portal.Projects.Single();
                Device station = project.Devices.Single(device => device.Name.Equals(
                    "S7-1500/ET200MP station_1", StringComparison.OrdinalIgnoreCase));
                DeviceItem cpu = FindItems(station.DeviceItems).Single(item =>
                    item.Name.Equals("PLC_1", StringComparison.OrdinalIgnoreCase));
                Certificate certificate = ((IEngineeringObject)cpu).GetAttribute(
                    "PlcCommunicationCertificate") as Certificate;
                if (certificate == null)
                    throw new InvalidOperationException("PLC communication certificate is missing.");
                if (!certificate.SubjectCommonName.Equals("PLC-1/Communication-1",
                    StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Unexpected PLC certificate: " +
                        certificate.SubjectCommonName);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                if (File.Exists(outputPath))
                    throw new InvalidOperationException("Refusing to overwrite existing certificate export.");
                certificate.Export(new FileInfo(outputPath));
                Console.WriteLine("MODE=OFFLINE_EXPORT_PUBLIC_CERTIFICATE_ONLY");
                Console.WriteLine("SUBJECT=" + certificate.SubjectCommonName);
                Console.WriteLine("VALID_UNTIL=" + certificate.ValidUntil.ToString("o"));
                Console.WriteLine("PRIVATE_KEY_EXPORTED=NO");
                Console.WriteLine("OUTPUT=" + outputPath);
                Console.WriteLine("STATUS=PASS");
            }
            return 0;
        }

        private static IEnumerable<DeviceItem> FindItems(DeviceItemComposition items)
        {
            foreach (DeviceItem item in items)
            {
                yield return item;
                foreach (DeviceItem child in FindItems(item.DeviceItems)) yield return child;
            }
        }
    }
}
