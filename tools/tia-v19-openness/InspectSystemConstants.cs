using System;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Tags;

namespace Schlenker.TiaV19
{
    internal static class InspectSystemConstants
    {
        private static int Main(string[] args)
        {
            if (args.Length != 1) return 2;
            string expected = Path.GetFullPath(args[0]);
            TiaPortalProcess process = TiaPortal.GetProcesses().SingleOrDefault(candidate =>
                candidate.ProjectPath != null &&
                candidate.ProjectPath.FullName.Equals(expected, StringComparison.OrdinalIgnoreCase));
            if (process == null) throw new InvalidOperationException("Exact project is not open.");
            using (TiaPortal portal = process.Attach())
            {
                Project project = portal.Projects.Single();
                PlcSoftware plc = FindPlc(project);
                foreach (PlcTagTable table in plc.TagTableGroup.TagTables)
                {
                    foreach (PlcSystemConstant item in table.SystemConstants)
                    {
                        if (item.Name.IndexOf("ANYBUS", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            item.Name.IndexOf("SMC", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            item.DataTypeName.Equals("Hw_IoSystem", StringComparison.OrdinalIgnoreCase))
                            Console.WriteLine("SYSTEM_CONSTANT name={0}; type={1}; value={2}",
                                item.Name, item.DataTypeName, item.Value);
                    }
                }
            }
            return 0;
        }

        private static PlcSoftware FindPlc(Project project)
        {
            foreach (Device device in project.Devices)
            {
                PlcSoftware plc = FindPlc(device.DeviceItems);
                if (plc != null) return plc;
            }
            throw new InvalidOperationException("PLC software not found.");
        }

        private static PlcSoftware FindPlc(DeviceItemComposition items)
        {
            foreach (DeviceItem item in items)
            {
                SoftwareContainer container = item.GetService<SoftwareContainer>();
                PlcSoftware plc = container == null ? null : container.Software as PlcSoftware;
                if (plc != null) return plc;
                plc = FindPlc(item.DeviceItems);
                if (plc != null) return plc;
            }
            return null;
        }
    }
}
