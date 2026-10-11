using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Tags;

namespace Schlenker.TiaV19
{
    internal static class ConfigureLmt100ProductPresenceOffline
    {
        private const string PortType =
            "GSD:GSDML-V2.35-IFM-AL1X0X-20241112-144500.XML/SM/IDS IO-Link 2I";
        private const string PortItemName = "AL103_P1_SPL100_LMT100_2I";
        private const string TagName = "SPL100_LMT100_ProcessData";
        private const string TagTableName = "IO-Link Sensors";

        private static int Main(string[] args)
        {
            if (args.Length < 2 || args.Length > 3)
            {
                Console.Error.WriteLine(
                    "Usage: ConfigureLmt100ProductPresenceOffline <audit|apply> <exact-ap19-path> [report-path]");
                return 2;
            }

            string mode = args[0].Trim().ToLowerInvariant();
            if (mode != "audit" && mode != "apply") return 2;
            string expected = Path.GetFullPath(args[1]);
            string reportPath = args.Length == 3 ? Path.GetFullPath(args[2]) : null;

            try
            {
                TiaPortalProcess process = TiaPortal.GetProcesses().SingleOrDefault(candidate =>
                    candidate.ProjectPath != null && candidate.ProjectPath.FullName.Equals(
                        expected, StringComparison.OrdinalIgnoreCase));
                if (process == null)
                    throw new InvalidOperationException("The exact authorized TIA project is not open.");

                List<string> lines = new List<string>();
                using (TiaPortal portal = process.Attach())
                {
                    Project project = portal.Projects.Single();
                    if (!project.Path.FullName.Equals(expected, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Attached project identity changed unexpectedly.");

                    Device al103 = AllDevices(project).Single(device =>
                        device.Name.Equals("al103", StringComparison.OrdinalIgnoreCase));
                    DeviceItem module = FindItems(al103.DeviceItems).Single(item =>
                        (item.TypeIdentifier ?? String.Empty).IndexOf(
                            "IOLM_MODULE_8P_ENMO", StringComparison.OrdinalIgnoreCase) >= 0);
                    DeviceItem port = FindItems(al103.DeviceItems).FirstOrDefault(item =>
                        item.PositionNumber == 1 &&
                        !Object.ReferenceEquals(item, module) &&
                        (item.TypeIdentifier ?? String.Empty).IndexOf(
                            "/SM/IDS IO-Link", StringComparison.OrdinalIgnoreCase) >= 0);

                    lines.Add("PROJECT=" + project.Path.FullName);
                    lines.Add("MODE=" + mode.ToUpperInvariant() +
                        "; ONLINE_WRITE=NO; DOWNLOAD=NO; CPU_CONTROL=NO");
                    lines.Add("TARGET=AL103/P1; DEVICE=SPL100; MODEL=IFM LMT100");
                    lines.Add("IODD_VENDOR_ID=310; IODD_DEVICE_ID=304; PROCESS_INPUT_BITS=16");

                    if (port != null && !port.TypeIdentifier.Equals(
                        PortType, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException(
                            "AL103/P1 is occupied by an unexpected IO-Link submodule: " +
                            port.Name + " / " + port.TypeIdentifier);

                    bool canPlug = port != null || module.CanPlugNew(PortType, PortItemName, 1);
                    lines.Add("PORT_EXISTING=" + (port == null ? "NO" : "YES"));
                    lines.Add("CAN_PLUG=" + canPlug);
                    if (!canPlug)
                        throw new InvalidOperationException(
                            "TIA does not allow the 2-byte IO-Link input submodule at AL103/P1.");

                    if (mode == "audit")
                    {
                        if (port != null) AppendPort(lines, port);
                        lines.Add("SAVE=NO");
                        lines.Add("STATUS=PASS_READY");
                    }
                    else
                    {
                        DeviceItem createdPort = null;
                        PlcTag createdTag = null;
                        try
                        {
                            if (port == null)
                            {
                                createdPort = module.PlugNew(PortType, PortItemName, 1);
                                port = createdPort;
                            }
                            if (port == null || port.PositionNumber != 1 ||
                                !port.TypeIdentifier.Equals(PortType, StringComparison.OrdinalIgnoreCase))
                                throw new InvalidOperationException(
                                    "The created AL103/P1 submodule failed validation.");

                            Address input = port.Addresses.Single(address =>
                                address.IoType.ToString().Equals("Input", StringComparison.OrdinalIgnoreCase));
                            // The IFM GSD submodule contributes two process-data bytes followed
                            // by one PQI diagnostic byte, therefore TIA exposes 24 input bits.
                            if (input.StartAddress < 0 || input.Length != 24)
                                throw new InvalidOperationException(
                                    "AL103/P1 must expose 16 process-data bits plus 8 PQI bits; actual start=" +
                                    input.StartAddress + ", length=" + input.Length + ".");

                            PlcSoftware plc = FindPlc(project);
                            if (plc == null)
                                throw new InvalidOperationException("PLC software was not found.");
                            string logicalAddress = "%IW" + input.StartAddress;
                            PlcTag byName = FindTag(plc.TagTableGroup, tag =>
                                tag.Name.Equals(TagName, StringComparison.OrdinalIgnoreCase));
                            PlcTag byAddress = FindTag(plc.TagTableGroup, tag =>
                                tag.LogicalAddress.Equals(logicalAddress, StringComparison.OrdinalIgnoreCase));

                            if (byName != null)
                            {
                                if (!byName.DataTypeName.Equals("Word", StringComparison.OrdinalIgnoreCase) ||
                                    !byName.LogicalAddress.Equals(logicalAddress, StringComparison.OrdinalIgnoreCase))
                                    throw new InvalidOperationException(
                                        "Existing " + TagName + " does not match Word " + logicalAddress + ".");
                            }
                            else
                            {
                                if (byAddress != null)
                                    throw new InvalidOperationException(
                                        logicalAddress + " is already assigned to " + byAddress.Name + ".");
                                PlcTagTable table = plc.TagTableGroup.TagTables.FirstOrDefault(item =>
                                    item.Name.Equals(TagTableName, StringComparison.OrdinalIgnoreCase));
                                if (table == null)
                                    table = plc.TagTableGroup.TagTables.Create(TagTableName);
                                createdTag = table.Tags.Create(TagName, "Word", logicalAddress);
                            }

                            project.Save();
                            AppendPort(lines, port);
                            lines.Add("PLC_TAG=" + TagName + "; TYPE=Word; ADDRESS=" + logicalAddress);
                            lines.Add("PROCESS_MAPPING=OUT1->WORD.%X0; OUT2->WORD.%X1; LEVEL->WORD.%X2..%X15; PQI->THIRD_BYTE");
                            lines.Add("SAVE=YES");
                            lines.Add("STATUS=PASS_APPLIED");
                        }
                        catch
                        {
                            if (createdTag != null)
                            {
                                try { createdTag.Delete(); }
                                catch { }
                            }
                            if (createdPort != null)
                            {
                                try { createdPort.Delete(); }
                                catch { }
                            }
                            throw;
                        }
                    }
                }

                foreach (string line in lines) Console.WriteLine(line);
                if (!String.IsNullOrWhiteSpace(reportPath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
                    File.WriteAllLines(reportPath, lines);
                }
                return 0;
            }
            catch (Exception exception)
            {
                string failure = "STATUS=FAIL" + Environment.NewLine + exception;
                Console.Error.WriteLine(failure);
                if (!String.IsNullOrWhiteSpace(reportPath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
                    File.WriteAllText(reportPath, failure);
                }
                return 1;
            }
        }

        private static void AppendPort(List<string> lines, DeviceItem port)
        {
            Address input = port.Addresses.Single(address =>
                address.IoType.ToString().Equals("Input", StringComparison.OrdinalIgnoreCase));
            lines.Add("PORT_NAME=" + port.Name + "; TYPE=" + port.TypeIdentifier +
                "; POSITION=" + port.PositionNumber);
            lines.Add("INPUT_START=" + input.StartAddress + "; INPUT_LENGTH_BITS=" + input.Length);
        }

        private static PlcSoftware FindPlc(Project project)
        {
            foreach (Device device in AllDevices(project))
            {
                PlcSoftware plc = FindPlc(device.DeviceItems);
                if (plc != null) return plc;
            }
            return null;
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

        private static IEnumerable<Device> AllDevices(Project project)
        {
            HashSet<Device> seen = new HashSet<Device>();
            foreach (Device device in project.Devices)
                if (seen.Add(device)) yield return device;
            foreach (Device device in project.UngroupedDevicesGroup.Devices)
                if (seen.Add(device)) yield return device;
            foreach (DeviceUserGroup group in project.DeviceGroups)
                foreach (Device device in DevicesInGroup(group))
                    if (seen.Add(device)) yield return device;
        }

        private static IEnumerable<Device> DevicesInGroup(DeviceUserGroup group)
        {
            foreach (Device device in group.Devices) yield return device;
            foreach (DeviceUserGroup child in group.Groups)
                foreach (Device device in DevicesInGroup(child)) yield return device;
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
