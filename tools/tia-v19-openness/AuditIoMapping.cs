using System;
using System.Collections.Generic;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.CrossReference;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.HmiUnified.HmiTags;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.SW.Tags;

namespace Schlenker.TiaV19
{
    internal static class AuditIoMapping
    {
        private static readonly string[] Terms =
        {
            "SMC", "EX260", "ABC3113", "AL1403", "IO-Link", "IO Link",
            "EV210", "EV211", "EV212", "EV213", "EV217", "EV247",
            "ValveBits", "Valve_210", "Valve_211", "Valve_212", "Valve_213",
            "Valve_217", "Valve_247", "GateOpen", "GateClose", "ProductInlet",
            "ProductClose", "IOlink", "IO_Link"
        };

        private static string ExportDirectory;

        private static int Main(string[] args)
        {
            string hint = args.Length > 0 ? args[0] : "schlenkers 36-10 190036-7-8v2.12.ap19";
            ExportDirectory = args.Length > 1 ? args[1] : null;
            try
            {
                IList<TiaPortalProcess> processes = TiaPortal.GetProcesses();
                TiaPortalProcess selected = processes.FirstOrDefault(p => p.ProjectPath != null &&
                    p.ProjectPath.FullName.EndsWith(hint, StringComparison.OrdinalIgnoreCase));
                if (selected == null)
                {
                    Console.Error.WriteLine("STATUS=FAIL");
                    Console.Error.WriteLine("No open TIA project matched: " + hint);
                    return 2;
                }

                using (TiaPortal portal = selected.Attach())
                {
                    Project project = portal.Projects.FirstOrDefault();
                    if (project == null) return 3;
                    Console.WriteLine("PROJECT=" + project.Path.FullName);
                    Console.WriteLine("AUDIT_MODE=READ_ONLY; SAVE=NO; COMPILE=NO");
                    TryCrossReferences(project, "PROJECT", "");
                    foreach (Device device in project.Devices)
                    {
                        Console.WriteLine("DEVICE name={0}", device.Name);
                        InspectItems(device.Name, device.DeviceItems, "  ");
                    }
                }
                Console.WriteLine("STATUS=PASS");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("STATUS=FAIL");
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        private static void InspectItems(string device, DeviceItemComposition items, string indent)
        {
            foreach (DeviceItem item in items)
            {
                Console.WriteLine("{0}ITEM name={1}; type={2}; position={3}; class={4}; plugged={5}",
                    indent, item.Name, item.TypeIdentifier, item.PositionNumber,
                    item.Classification, item.IsPlugged);
                foreach (HwIdentifier hw in item.HwIdentifiers)
                    Console.WriteLine("{0}  HW_ID={1}", indent, hw.Identifier);
                foreach (Address address in item.Addresses)
                    Console.WriteLine("{0}  ADDRESS io={1}; start={2}; length={3}", indent,
                        address.IoType, address.StartAddress, address.Length);
                foreach (Channel channel in item.Channels)
                    Console.WriteLine("{0}  CHANNEL number={1}; io={2}; type={3}", indent,
                        channel.Number, channel.IoType, channel.Type);

                NetworkInterface network = item.GetService<NetworkInterface>();
                if (network != null)
                {
                    Console.WriteLine("{0}  NETWORK type={1}; mode={2}", indent,
                        network.InterfaceType, network.InterfaceOperatingMode);
                    foreach (Node node in network.Nodes)
                        Console.WriteLine("{0}    NODE name={1}; id={2}; type={3}; subnet={4}", indent,
                            node.Name, node.NodeId, node.NodeType,
                            node.ConnectedSubnet == null ? "<none>" : node.ConnectedSubnet.Name);
                }

                SoftwareContainer container = item.GetService<SoftwareContainer>();
                PlcSoftware plc = container == null ? null : container.Software as PlcSoftware;
                if (plc != null) InspectPlc(plc, indent + "  ");
                HmiSoftware hmi = container == null ? null : container.Software as HmiSoftware;
                if (hmi != null) InspectHmi(hmi, indent + "  ");
                InspectItems(device, item.DeviceItems, indent + "  ");
            }
        }

        private static void InspectPlc(PlcSoftware plc, string indent)
        {
            Console.WriteLine("{0}PLC_SOFTWARE name={1}", indent, plc.Name);
            InspectTagTables(plc.TagTableGroup.TagTables, indent + "  ");
            InspectTagGroups(plc.TagTableGroup.Groups, indent + "  ");
            InspectBlocks(plc.BlockGroup.Blocks, indent + "  ");
            InspectBlockGroups(plc.BlockGroup.Groups, indent + "  ");
            TryCrossReferences(plc, "PLC_SOFTWARE", indent);
        }

        private static void InspectBlockGroups(PlcBlockUserGroupComposition groups, string indent)
        {
            foreach (PlcBlockUserGroup group in groups)
            {
                Console.WriteLine("{0}PLC_BLOCK_GROUP name={1}", indent, group.Name);
                InspectBlocks(group.Blocks, indent + "  ");
                InspectBlockGroups(group.Groups, indent + "  ");
            }
        }

        private static void InspectBlocks(PlcBlockComposition blocks, string indent)
        {
            foreach (PlcBlock block in blocks)
            {
                if (!Interesting(block.Name)) continue;
                Console.WriteLine("{0}PLC_BLOCK name={1}; number={2}; language={3}; consistent={4}",
                    indent, block.Name, block.Number, block.ProgrammingLanguage, block.IsConsistent);
                if (!String.IsNullOrWhiteSpace(ExportDirectory))
                {
                    System.IO.Directory.CreateDirectory(ExportDirectory);
                    string safe = String.Concat(block.Name.Select(c =>
                        System.IO.Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
                    System.IO.FileInfo target = new System.IO.FileInfo(
                        System.IO.Path.Combine(ExportDirectory, safe + ".xml"));
                    block.Export(target, ExportOptions.WithDefaults);
                    Console.WriteLine("{0}  BLOCK_EXPORTED={1}", indent, target.FullName);
                }
                TryCrossReferences(block, "PLC_BLOCK:" + block.Name, indent + "  ");
            }
        }

        private static void TryCrossReferences(IEngineeringObject owner, string label, string indent)
        {
            try
            {
                System.Reflection.MethodInfo getter = owner.GetType().GetMethods()
                    .FirstOrDefault(m => m.Name == "GetService" && m.IsGenericMethodDefinition &&
                        m.GetParameters().Length == 0);
                CrossReferenceService service = getter == null ? null :
                    getter.MakeGenericMethod(typeof(CrossReferenceService)).Invoke(owner, null) as CrossReferenceService;
                if (service == null)
                {
                    Console.WriteLine("{0}CROSS_REFERENCES owner={1}; service=<unavailable>", indent, label);
                    return;
                }
                CrossReferenceResult result = service.GetCrossReferences(CrossReferenceFilter.ObjectsWithReferences);
                int count = 0;
                foreach (SourceObject source in result.Sources)
                    count += PrintSource(source, indent + "  ");
                Console.WriteLine("{0}CROSS_REFERENCE_MATCHES owner={1}; count={2}", indent, label, count);
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0}CROSS_REFERENCES owner={1}; error={2}: {3}", indent, label,
                    ex.GetType().Name, ex.Message.Replace('\r', ' ').Replace('\n', ' '));
            }
        }

        private static void InspectTagGroups(PlcTagTableUserGroupComposition groups, string indent)
        {
            foreach (PlcTagTableUserGroup group in groups)
            {
                Console.WriteLine("{0}PLC_TAG_GROUP name={1}", indent, group.Name);
                InspectTagTables(group.TagTables, indent + "  ");
                InspectTagGroups(group.Groups, indent + "  ");
            }
        }

        private static void InspectTagTables(PlcTagTableComposition tables, string indent)
        {
            foreach (PlcTagTable table in tables)
            {
                Console.WriteLine("{0}PLC_TAG_TABLE name={1}; tags={2}", indent, table.Name, table.Tags.Count);
                foreach (PlcTag tag in table.Tags)
                    if (Interesting(tag.Name) || Interesting(tag.LogicalAddress) || Interesting(tag.DataTypeName))
                        Console.WriteLine("{0}  PLC_TAG name={1}; type={2}; address={3}", indent,
                            tag.Name, tag.DataTypeName, tag.LogicalAddress);
            }
        }

        private static int PrintSource(SourceObject source, string indent)
        {
            int found = 0;
            bool match = Interesting(source.Name) || Interesting(source.Address) || Interesting(source.Path) ||
                source.References.Any(r => Interesting(r.Name) || Interesting(r.Address) || Interesting(r.Path));
            if (match)
            {
                found++;
                Console.WriteLine("{0}XREF_SOURCE name={1}; type={2}; address={3}; path={4}", indent,
                    source.Name, source.TypeName, source.Address, source.Path);
                foreach (ReferenceObject reference in source.References)
                    Console.WriteLine("{0}  XREF_TARGET name={1}; type={2}; address={3}; path={4}; locations={5}", indent,
                        reference.Name, reference.TypeName, reference.Address, reference.Path, reference.Locations.Count);
            }
            foreach (SourceObject child in source.Children) found += PrintSource(child, indent);
            return found;
        }

        private static void InspectHmi(HmiSoftware hmi, string indent)
        {
            Console.WriteLine("{0}HMI_SOFTWARE name={1}; tagTables={2}", indent, hmi.Name, hmi.TagTables.Count);
            foreach (HmiTagTable table in hmi.TagTables)
            {
                Console.WriteLine("{0}  HMI_TAG_TABLE name={1}; tags={2}", indent, table.Name, table.Tags.Count);
                foreach (HmiTag tag in table.Tags)
                    if (Interesting(tag.Name) || Interesting(tag.PlcTag))
                        Console.WriteLine("{0}    HMI_TAG name={1}; plcTag={2}; type={3}; connection={4}", indent,
                            tag.Name, tag.PlcTag, tag.DataType, tag.Connection);
            }
        }

        private static bool Interesting(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return false;
            return Terms.Any(term => value.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
