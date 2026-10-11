using System;
using System.IO;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;

namespace Schlenker.TiaV19
{
    internal static class ProbeG120CGsdOffline
    {
        private const string G120CType =
            "GSD:GSDML-V2.31-SIEMENS-SINAMICS_G120C-20221115.XML/DAP/IDD_CU240C-V4.7";
        private const string NoSafetyType =
            "GSD:GSDML-V2.31-SIEMENS-SINAMICS_G120C-20221115.XML/SM/IDS_NOSAFE";
        private const string Telegram1Type =
            "GSD:GSDML-V2.31-SIEMENS-SINAMICS_G120C-20221115.XML/SM/IDS_TEL1";

        private static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("Usage: ProbeG120CGsdOffline <exact-ap19-path>");
                return 2;
            }

            string projectPath = Path.GetFullPath(args[0]);
            Device probe = null;
            try
            {
                using (TiaPortal portal = new TiaPortal(TiaPortalMode.WithoutUserInterface))
                {
                    Project project = portal.Projects.Open(new FileInfo(projectPath));
                    try
                    {
                        Console.WriteLine("MODE=OFFLINE_PROBE; SAVE=NO; ONLINE=NO");
                        Console.WriteLine("PROJECT=" + project.Path.FullName);
                        probe = project.Devices.CreateWithItem(G120CType, "g120c_probe", "G120C_PROBE");
                        Console.WriteLine("TYPE_ACCEPTED=" + G120CType);
                        ProbePlugSupport(probe.DeviceItems, "  ");
                        DumpItems(probe.DeviceItems, "  ");
                        probe.Delete();
                        probe = null;
                        Console.WriteLine("PROBE_DELETED=YES");
                    }
                    finally
                    {
                        if (probe != null)
                        {
                            try { probe.Delete(); }
                            catch { }
                        }
                        project.Close();
                    }
                }

                Console.WriteLine("STATUS=PASS");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("STATUS=FAIL");
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static DeviceItem FindItem(DeviceItemComposition items, string typeFragment)
        {
            foreach (DeviceItem item in items)
            {
                if ((item.TypeIdentifier ?? String.Empty).IndexOf(
                        typeFragment, StringComparison.OrdinalIgnoreCase) >= 0)
                    return item;
                DeviceItem nested = FindItem(item.DeviceItems, typeFragment);
                if (nested != null) return nested;
            }
            return null;
        }

        private static void ProbePlugSupport(DeviceItemComposition items, string indent)
        {
            foreach (DeviceItem item in items)
            {
                Console.WriteLine("{0}PLUG_OWNER name={1}; type={2}", indent,
                    item.Name, item.TypeIdentifier);
                foreach (Siemens.Engineering.HW.Extensions.PlugLocation location in item.GetPlugLocations())
                    Console.WriteLine("{0}  LOCATION position={1}; label={2}", indent,
                        location.PositionNumber, location.Label);
                for (int position = 0; position <= 4; position++)
                {
                    bool noSafety = item.CanPlugNew(NoSafetyType, "No safety telegram", position);
                    bool telegram1 = item.CanPlugNew(Telegram1Type, "Standard telegram 1", position);
                    if (noSafety || telegram1)
                        Console.WriteLine("{0}  CAN position={1}; noSafety={2}; telegram1={3}",
                            indent, position, noSafety, telegram1);
                }
                ProbePlugSupport(item.DeviceItems, indent + "  ");
            }
        }

        private static void DumpItems(DeviceItemComposition items, string indent)
        {
            foreach (DeviceItem item in items)
            {
                Console.WriteLine("{0}ITEM name={1}; type={2}; position={3}; class={4}",
                    indent, item.Name, item.TypeIdentifier, item.PositionNumber, item.Classification);
                foreach (Address address in item.Addresses)
                    Console.WriteLine("{0}  ADDRESS io={1}; start={2}; length={3}", indent,
                        address.IoType, address.StartAddress, address.Length);

                NetworkInterface network = item.GetService<NetworkInterface>();
                if (network != null)
                    foreach (Node node in network.Nodes)
                        Console.WriteLine("{0}  NODE name={1}; type={2}; subnet={3}", indent,
                            node.Name, node.NodeType,
                            node.ConnectedSubnet == null ? "<none>" : node.ConnectedSubnet.Name);

                DumpItems(item.DeviceItems, indent + "  ");
            }
        }
    }
}
