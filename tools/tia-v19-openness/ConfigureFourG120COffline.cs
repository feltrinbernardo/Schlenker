using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;

namespace Schlenker.TiaV19
{
    internal static class ConfigureFourG120COffline
    {
        private const string G120CType =
            "GSD:GSDML-V2.31-SIEMENS-SINAMICS_G120C-20221115.XML/DAP/IDD_CU240C-V4.7";
        private const string Telegram1Type =
            "GSD:GSDML-V2.31-SIEMENS-SINAMICS_G120C-20221115.XML/SM/IDS_TEL1";

        private sealed class DriveDefinition
        {
            public string DeviceName;
            public string ItemName;
            public string PnName;
            public string IpAddress;
            public int InputStart;
            public int OutputStart;
        }

        private sealed class AddressRange
        {
            public string Owner;
            public string IoType;
            public int Start;
            public int End;
        }

        private static readonly DriveDefinition[] Drives =
        {
            new DriveDefinition
            {
                DeviceName = "VSD_MAIN_MACHINE",
                ItemName = "G120C_MainMachine",
                PnName = "vsd-main-machine",
                IpAddress = "192.168.10.40",
                InputStart = 308,
                OutputStart = 140
            },
            new DriveDefinition
            {
                DeviceName = "VSD_BOTTLE_CONVEYOR",
                ItemName = "G120C_BottleConveyor",
                PnName = "vsd-bottle-conveyor",
                IpAddress = "192.168.10.41",
                InputStart = 312,
                OutputStart = 144
            },
            new DriveDefinition
            {
                DeviceName = "VSD_PRODUCT_PUMP",
                ItemName = "G120C_ProductPump",
                PnName = "vsd-product-pump",
                IpAddress = "192.168.10.42",
                InputStart = 316,
                OutputStart = 148
            },
            new DriveDefinition
            {
                DeviceName = "VSD_CAP_DISTRIBUTOR",
                ItemName = "G120C_CapDistributor",
                PnName = "vsd-cap-distributor",
                IpAddress = "192.168.10.43",
                InputStart = 320,
                OutputStart = 152
            }
        };

        private static int Main(string[] args)
        {
            if (args.Length != 2 ||
                !(args[0].Equals("audit", StringComparison.OrdinalIgnoreCase) ||
                  args[0].Equals("apply", StringComparison.OrdinalIgnoreCase)))
            {
                Console.Error.WriteLine("Usage: ConfigureFourG120COffline audit|apply <exact-ap19-path>");
                return 2;
            }

            bool apply = args[0].Equals("apply", StringComparison.OrdinalIgnoreCase);
            string projectPath = Path.GetFullPath(args[1]);
            List<Device> created = new List<Device>();

            try
            {
                using (TiaPortal portal = new TiaPortal(TiaPortalMode.WithoutUserInterface))
                {
                    Project project = portal.Projects.Open(new FileInfo(projectPath));
                    try
                    {
                        string actualPath = Path.GetFullPath(project.Path.FullName);
                        if (!actualPath.Equals(projectPath, StringComparison.OrdinalIgnoreCase))
                            throw new InvalidOperationException("TIA opened a different project: " + actualPath);

                        Console.WriteLine("PROJECT=" + actualPath);
                        Console.WriteLine("MODE=" + (apply ? "OFFLINE_APPLY" : "OFFLINE_AUDIT") +
                                          "; ONLINE=NO; DOWNLOAD=NO");

                        List<Device> existingDevices = AllDevices(project).ToList();
                        EnsureNoExistingDriveDefinitions(existingDevices);
                        List<AddressRange> occupied = CollectAddressRanges(existingDevices);
                        ValidatePlannedRanges(occupied);

                        Subnet subnet = FindSubnet(existingDevices, "PN/IE_1");
                        if (subnet == null)
                            throw new InvalidOperationException("PROFINET subnet PN/IE_1 was not found.");

                        foreach (DriveDefinition drive in Drives)
                        {
                            Console.WriteLine("PLAN device={0}; pn={1}; ip={2}; input=I{3}..I{4}; output=Q{5}..Q{6}; telegram=1",
                                drive.DeviceName, drive.PnName, drive.IpAddress,
                                drive.InputStart, drive.InputStart + 3,
                                drive.OutputStart, drive.OutputStart + 3);
                        }

                        if (!apply)
                        {
                            Console.WriteLine("SAVE=NO");
                            Console.WriteLine("STATUS=PASS_READY");
                            return 0;
                        }

                        foreach (DriveDefinition definition in Drives)
                            created.Add(CreateDrive(project, subnet, definition));

                        project.Save();
                        Console.WriteLine("SAVE=YES");
                        Console.WriteLine("CREATED_DRIVES=" + created.Count);
                        Console.WriteLine("STATUS=PASS_APPLIED");
                    }
                    catch
                    {
                        foreach (Device device in created.AsEnumerable().Reverse())
                        {
                            try { device.Delete(); }
                            catch { }
                        }
                        throw;
                    }
                    finally
                    {
                        project.Close();
                    }
                }
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("STATUS=FAIL");
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static Device CreateDrive(Project project, Subnet subnet, DriveDefinition definition)
        {
            Device device = project.Devices.CreateWithItem(G120CType, definition.ItemName, definition.DeviceName);
            List<Node> nodes = FindNodes(device.DeviceItems).ToList();
            if (nodes.Count != 1)
                throw new InvalidOperationException(definition.DeviceName +
                    " expected one Ethernet node, found " + nodes.Count + ".");

            Node node = nodes[0];
            IEngineeringObject attributes = node;
            attributes.SetAttribute("IpProtocolSelection", IpProtocolSelection.Project);
            attributes.SetAttribute("Address", definition.IpAddress);
            attributes.SetAttribute("SubnetMask", "255.255.255.0");
            attributes.SetAttribute("PnDeviceNameAutoGeneration", false);
            attributes.SetAttribute("PnDeviceName", definition.PnName);
            node.ConnectToSubnet(subnet);

            DeviceItem driveModule = FindItems(device.DeviceItems).Single(item =>
                (item.TypeIdentifier ?? String.Empty).IndexOf("/M/IDM_DRIVE", StringComparison.OrdinalIgnoreCase) >= 0);
            if (!driveModule.CanPlugNew(Telegram1Type, "Standard telegram 1", 2))
                throw new InvalidOperationException(definition.DeviceName +
                    " does not accept Standard telegram 1 at API position 2.");

            DeviceItem telegram = driveModule.PlugNew(Telegram1Type, "Standard telegram 1", 2);
            Address input = telegram.Addresses.Single(address =>
                address.IoType.ToString().Equals("Input", StringComparison.OrdinalIgnoreCase));
            Address output = telegram.Addresses.Single(address =>
                address.IoType.ToString().Equals("Output", StringComparison.OrdinalIgnoreCase));
            if (input.Length != 32 || output.Length != 32)
                throw new InvalidOperationException(definition.DeviceName +
                    " Telegram 1 is not 2 PZD words in each direction.");

            input.StartAddress = definition.InputStart;
            output.StartAddress = definition.OutputStart;

            if (!definition.IpAddress.Equals(attributes.GetAttribute("Address") as string,
                    StringComparison.OrdinalIgnoreCase) ||
                !definition.PnName.Equals(attributes.GetAttribute("PnDeviceName") as string,
                    StringComparison.OrdinalIgnoreCase) ||
                node.ConnectedSubnet == null ||
                !"PN/IE_1".Equals(node.ConnectedSubnet.Name, StringComparison.OrdinalIgnoreCase) ||
                input.StartAddress != definition.InputStart ||
                output.StartAddress != definition.OutputStart)
                throw new InvalidOperationException(definition.DeviceName + " failed validation before save.");

            Console.WriteLine("CREATED device={0}; pn={1}; ip={2}; subnet={3}; telegram=1; input=I{4}..I{5}; output=Q{6}..Q{7}",
                definition.DeviceName,
                attributes.GetAttribute("PnDeviceName"),
                attributes.GetAttribute("Address"),
                node.ConnectedSubnet.Name,
                input.StartAddress, input.StartAddress + 3,
                output.StartAddress, output.StartAddress + 3);
            return device;
        }

        private static void EnsureNoExistingDriveDefinitions(IEnumerable<Device> devices)
        {
            foreach (Device device in devices)
            {
                if (Drives.Any(drive => device.Name.Equals(drive.DeviceName, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("Drive device already exists: " + device.Name + ".");

                foreach (Node node in FindNodes(device.DeviceItems))
                {
                    string pnName = SafeString(node, "PnDeviceName");
                    string address = SafeString(node, "Address");
                    if (Drives.Any(drive =>
                        drive.PnName.Equals(pnName, StringComparison.OrdinalIgnoreCase) ||
                        drive.IpAddress.Equals(address, StringComparison.OrdinalIgnoreCase)))
                        throw new InvalidOperationException("A configured node already uses planned identity: " +
                            device.Name + "; PN=" + pnName + "; IP=" + address + ".");
                }
            }
        }

        private static List<AddressRange> CollectAddressRanges(IEnumerable<Device> devices)
        {
            List<AddressRange> ranges = new List<AddressRange>();
            foreach (Device device in devices)
            {
                foreach (DeviceItem item in FindItems(device.DeviceItems))
                {
                    foreach (Address address in item.Addresses)
                    {
                        string ioType = address.IoType.ToString();
                        if ((!ioType.Equals("Input", StringComparison.OrdinalIgnoreCase) &&
                             !ioType.Equals("Output", StringComparison.OrdinalIgnoreCase)) ||
                            address.StartAddress < 0 || address.Length <= 0)
                            continue;
                        int byteLength = (address.Length + 7) / 8;
                        ranges.Add(new AddressRange
                        {
                            Owner = device.Name + "/" + item.Name,
                            IoType = ioType,
                            Start = address.StartAddress,
                            End = address.StartAddress + byteLength - 1
                        });
                    }
                }
            }
            return ranges;
        }

        private static void ValidatePlannedRanges(IEnumerable<AddressRange> occupied)
        {
            List<AddressRange> all = occupied.ToList();
            foreach (DriveDefinition drive in Drives)
            {
                AddressRange[] planned =
                {
                    new AddressRange { Owner = drive.DeviceName, IoType = "Input",
                        Start = drive.InputStart, End = drive.InputStart + 3 },
                    new AddressRange { Owner = drive.DeviceName, IoType = "Output",
                        Start = drive.OutputStart, End = drive.OutputStart + 3 }
                };
                foreach (AddressRange candidate in planned)
                {
                    AddressRange collision = all.FirstOrDefault(existing =>
                        existing.IoType.Equals(candidate.IoType, StringComparison.OrdinalIgnoreCase) &&
                        candidate.Start <= existing.End && candidate.End >= existing.Start);
                    if (collision != null)
                        throw new InvalidOperationException(String.Format(
                            "Planned {0} range {1}..{2} for {3} overlaps {4} range {5}..{6}.",
                            candidate.IoType, candidate.Start, candidate.End, candidate.Owner,
                            collision.Owner, collision.Start, collision.End));
                    all.Add(candidate);
                }
            }
        }

        private static Subnet FindSubnet(IEnumerable<Device> devices, string name)
        {
            foreach (Device device in devices)
                foreach (Node node in FindNodes(device.DeviceItems))
                    if (node.ConnectedSubnet != null &&
                        node.ConnectedSubnet.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                        return node.ConnectedSubnet;
            return null;
        }

        private static string SafeString(IEngineeringObject obj, string attribute)
        {
            try { return obj.GetAttribute(attribute) as string ?? String.Empty; }
            catch { return String.Empty; }
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

        private static IEnumerable<Node> FindNodes(DeviceItemComposition items)
        {
            foreach (DeviceItem item in items)
            {
                NetworkInterface network = item.GetService<NetworkInterface>();
                if (network != null)
                    foreach (Node node in network.Nodes)
                        if (node.NodeType == NetType.Ethernet)
                            yield return node;
                foreach (Node child in FindNodes(item.DeviceItems)) yield return child;
            }
        }
    }
}
