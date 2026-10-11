using System;
using System.Collections.Generic;
using System.IO;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;

namespace Schlenker.TiaV19
{
    internal static class ProbeIoSystemOffline
    {
        private static int Main(string[] args)
        {
            if (args.Length != 1) return 2;
            try
            {
                using (TiaPortal portal = new TiaPortal(TiaPortalMode.WithoutUserInterface))
                {
                    Project project = portal.Projects.Open(new FileInfo(Path.GetFullPath(args[0])));
                    foreach (Device device in project.Devices)
                    {
                        Console.WriteLine("DEVICE=" + device.Name);
                        Dump(device.DeviceItems, "  ");
                    }
                    project.Close();
                }
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static void Dump(DeviceItemComposition items, string indent)
        {
            foreach (DeviceItem item in items)
            {
                Console.WriteLine(indent + "ITEM=" + item.Name + "; TYPE=" + item.TypeIdentifier +
                    "; POSITION=" + item.PositionNumber);
                foreach (Address address in item.Addresses)
                    Console.WriteLine(indent + "  ADDRESS=" + address.IoType +
                        "; START=" + address.StartAddress + "; LENGTH_BITS=" + address.Length);
                NetworkInterface network = item.GetService<NetworkInterface>();
                if (network != null)
                {
                    Console.WriteLine(indent + "  NETWORK_MODE=" + network.InterfaceOperatingMode);
                    foreach (Node node in network.Nodes)
                    {
                        IEngineeringObject engineeringNode = node;
                        Console.WriteLine(indent + "  NODE=" + node.Name +
                            "; IP=" + SafeGet(engineeringNode, "Address") +
                            "; MASK=" + SafeGet(engineeringNode, "SubnetMask") +
                            "; PN=" + SafeGet(engineeringNode, "PnDeviceName") +
                            "; SUBNET=" + (node.ConnectedSubnet == null ? "<none>" : node.ConnectedSubnet.Name));
                    }
                    foreach (IoController controller in network.IoControllers)
                        Console.WriteLine(indent + "  CONTROLLER; IOSYSTEM=" +
                            (controller.IoSystem == null ? "<none>" : controller.IoSystem.Name));
                    foreach (IoConnector connector in network.IoConnectors)
                        Console.WriteLine(indent + "  CONNECTOR; IOSYSTEM=" +
                            (connector.ConnectedToIoSystem == null ? "<none>" : connector.ConnectedToIoSystem.Name));
                }
                Dump(item.DeviceItems, indent + "  ");
            }
        }

        private static object SafeGet(IEngineeringObject engineeringObject, string attribute)
        {
            try { return engineeringObject.GetAttribute(attribute) ?? "<null>"; }
            catch { return "<unavailable>"; }
        }
    }
}
