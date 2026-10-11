using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.Library;
using Siemens.Engineering.Library.MasterCopies;

namespace Schlenker.TiaV19
{
    internal static class RecreateAnybusForTreeOrder
    {
        private const string DeviceName = "ANYBUS_SMC";

        private static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("Usage: RecreateAnybusForTreeOrder <exact-ap19> <library-directory>");
                return 2;
            }
            string projectPath = Path.GetFullPath(args[0]);
            DirectoryInfo libraryDirectory = new DirectoryInfo(Path.GetFullPath(args[1]));
            libraryDirectory.Create();
            try
            {
                TiaPortalProcess process = TiaPortal.GetProcesses().SingleOrDefault(candidate =>
                    candidate.ProjectPath != null &&
                    Path.GetFullPath(candidate.ProjectPath.FullName).Equals(projectPath,
                        StringComparison.OrdinalIgnoreCase));
                if (process == null)
                    throw new InvalidOperationException("Exact open TIA project not found.");

                using (TiaPortal portal = process.Attach())
                {
                    Project project = portal.Projects.Single();
                    Device original = project.Devices.Single(device =>
                        device.Name.Equals(DeviceName, StringComparison.OrdinalIgnoreCase));
                    Snapshot before = Snapshot.Capture(original);
                    before.Print("BEFORE");

                    UserGlobalLibrary library = portal.GlobalLibraries.Create<UserGlobalLibrary>(
                        libraryDirectory, "Anybus_Tree_Reorder_Transfer");
                    try
                    {
                        MasterCopy copy = library.MasterCopyFolder.MasterCopies.Create(original);
                        library.Save();
                        original.Delete();
                        Device recreated = project.Devices.CreateFrom(copy);
                        ((IEngineeringObject)recreated).SetAttribute("Name", DeviceName);

                        RestoreConnections(recreated, project, before);
                        Snapshot after = Snapshot.Capture(recreated);
                        after.Print("AFTER");
                        before.AssertEquivalent(after);
                        project.Save();
                        Console.WriteLine("SAVE=YES; ONLINE=NO; DOWNLOAD=NO");
                    }
                    finally { library.Close(); }
                }
                Console.WriteLine("STATUS=PASS_RECREATED_AND_VERIFIED");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("STATUS=FAIL_NOT_SAVED");
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static void RestoreConnections(Device device, Project project, Snapshot before)
        {
            List<Device> devices = project.Devices.Concat(project.UngroupedDevicesGroup.Devices).ToList();
            Subnet subnet = devices.SelectMany(d => Nodes(d.DeviceItems))
                .Select(n => n.ConnectedSubnet).FirstOrDefault(s => s != null &&
                    s.Name.Equals(before.Subnet, StringComparison.OrdinalIgnoreCase));
            if (subnet == null) throw new InvalidOperationException("Original Anybus subnet not found.");
            IoSystem ioSystem = devices.SelectMany(d => Networks(d.DeviceItems))
                .SelectMany(n => n.IoControllers).Select(c => c.IoSystem)
                .FirstOrDefault(s => s != null &&
                    s.Name.Equals(before.IoSystem, StringComparison.OrdinalIgnoreCase));
            if (ioSystem == null) throw new InvalidOperationException("Original Anybus IO system not found.");

            Node node = Nodes(device.DeviceItems).Single(n =>
                SafeString(n, "Address").Equals(before.IpAddress, StringComparison.OrdinalIgnoreCase));
            if (node.ConnectedSubnet == null) node.ConnectToSubnet(subnet);
            NetworkInterface network = Networks(device.DeviceItems).Single(n => n.IoConnectors.Any());
            IoConnector connector = network.IoConnectors.First();
            if (connector.ConnectedToIoSystem == null) connector.ConnectToIoSystem(ioSystem);
        }

        private sealed class Snapshot
        {
            public string DeviceType;
            public string IpAddress;
            public string PnName;
            public string Subnet;
            public string IoSystem;
            public List<string> Items;
            public List<string> Addresses;

            public static Snapshot Capture(Device device)
            {
                List<DeviceItem> items = ItemsOf(device.DeviceItems).ToList();
                Node node = Nodes(device.DeviceItems).Single(n => !String.IsNullOrEmpty(SafeString(n, "Address")));
                IoConnector connector = Networks(device.DeviceItems).SelectMany(n => n.IoConnectors).First();
                return new Snapshot
                {
                    DeviceType = device.TypeIdentifier ?? String.Empty,
                    IpAddress = SafeString(node, "Address"),
                    PnName = SafeString(node, "PnDeviceName"),
                    Subnet = node.ConnectedSubnet == null ? String.Empty : node.ConnectedSubnet.Name,
                    IoSystem = connector.ConnectedToIoSystem == null ? String.Empty : connector.ConnectedToIoSystem.Name,
                    Items = items.Select(i => (i.Name ?? String.Empty) + "|" + (i.TypeIdentifier ?? String.Empty)).OrderBy(x => x).ToList(),
                    Addresses = items.SelectMany(i => i.Addresses).Where(a => a.StartAddress >= 0)
                        .Select(a => a.IoType + "|" + a.StartAddress + "|" + a.Length)
                        .OrderBy(x => x).ToList()
                };
            }

            public void Print(string prefix)
            {
                Console.WriteLine(prefix + "_DEVICE_TYPE=" + DeviceType);
                Console.WriteLine(prefix + "_IP=" + IpAddress);
                Console.WriteLine(prefix + "_PN_NAME=" + PnName);
                Console.WriteLine(prefix + "_SUBNET=" + Subnet);
                Console.WriteLine(prefix + "_IO_SYSTEM=" + IoSystem);
                Console.WriteLine(prefix + "_ITEM_COUNT=" + Items.Count);
                Console.WriteLine(prefix + "_ADDRESSES=" + String.Join(",", Addresses));
            }

            public void AssertEquivalent(Snapshot other)
            {
                if (DeviceType != other.DeviceType || IpAddress != other.IpAddress ||
                    PnName != other.PnName || Subnet != other.Subnet || IoSystem != other.IoSystem ||
                    !Items.SequenceEqual(other.Items) || !Addresses.SequenceEqual(other.Addresses))
                    throw new InvalidOperationException("Recreated Anybus differs from the original snapshot.");
            }
        }

        private static string SafeString(IEngineeringObject item, string name)
        {
            try { return item.GetAttribute(name) as string ?? String.Empty; }
            catch { return String.Empty; }
        }

        private static IEnumerable<DeviceItem> ItemsOf(DeviceItemComposition composition)
        {
            foreach (DeviceItem item in composition)
            {
                yield return item;
                foreach (DeviceItem child in ItemsOf(item.DeviceItems)) yield return child;
            }
        }

        private static IEnumerable<NetworkInterface> Networks(DeviceItemComposition composition)
        {
            foreach (DeviceItem item in ItemsOf(composition))
            {
                NetworkInterface network = item.GetService<NetworkInterface>();
                if (network != null) yield return network;
            }
        }

        private static IEnumerable<Node> Nodes(DeviceItemComposition composition)
        {
            return Networks(composition).SelectMany(network => network.Nodes);
        }
    }
}
