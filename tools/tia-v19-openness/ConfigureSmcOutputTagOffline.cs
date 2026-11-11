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
    internal static class ConfigureSmcOutputTagOffline
    {
        private const string TagName = "Y_SMC_EX260_OutputImage";
        private const string Address = "%QD128";

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
                PlcTag existingByName = FindTag(plc.TagTableGroup, tag =>
                    tag.Name.Equals(TagName, StringComparison.OrdinalIgnoreCase));
                PlcTag existingByAddress = FindTag(plc.TagTableGroup, tag =>
                    tag.LogicalAddress.Equals(Address, StringComparison.OrdinalIgnoreCase));

                if (existingByName != null)
                {
                    if (!existingByName.LogicalAddress.Equals(Address, StringComparison.OrdinalIgnoreCase) ||
                        !existingByName.DataTypeName.Equals("DWord", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Existing SMC output tag does not match DWord %QD128.");
                    Console.WriteLine("TAG_OK name={0}; type={1}; address={2}",
                        existingByName.Name, existingByName.DataTypeName, existingByName.LogicalAddress);
                }
                else
                {
                    if (existingByAddress != null)
                        throw new InvalidOperationException("%QD128 is already assigned to " + existingByAddress.Name + ".");
                    PlcTagTable table = plc.TagTableGroup.TagTables
                        .FirstOrDefault(item => item.Name.Equals("SMC EX260 Outputs", StringComparison.OrdinalIgnoreCase));
                    if (table == null) table = plc.TagTableGroup.TagTables.Create("SMC EX260 Outputs");
                    PlcTag created = table.Tags.Create(TagName, "DWord", Address);
                    Console.WriteLine("TAG_CREATED name={0}; type={1}; address={2}",
                        created.Name, created.DataTypeName, created.LogicalAddress);
                    project.Save();
                }
            }
            return 0;
        }

        private static PlcTag FindTag(PlcTagTableGroup group, Func<PlcTag, bool> predicate)
        {
            foreach (PlcTagTable table in group.TagTables)
                foreach (PlcTag tag in table.Tags)
                    if (predicate(tag)) return tag;
            foreach (PlcTagTableUserGroup child in group.Groups)
            {
                PlcTag found = FindTag(child, predicate);
                if (found != null) return found;
            }
            return null;
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
