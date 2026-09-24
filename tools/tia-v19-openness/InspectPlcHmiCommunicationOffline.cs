using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.SW;

namespace Schlenker.TiaV19
{
    internal static class InspectPlcHmiCommunicationOffline
    {
        private static readonly string[] Relevant =
        {
            "Address", "Subnet", "Router", "Gateway", "Certificate", "Secure",
            "Protection", "Communication", "Access", "Interface", "Name", "Firmware"
        };

        private static int Main(string[] args)
        {
            if (args.Length != 1) return 2;
            string expected = Path.GetFullPath(args[0]);
            TiaPortalProcess process = TiaPortal.GetProcesses().SingleOrDefault(candidate =>
                candidate.ProjectPath != null && candidate.ProjectPath.FullName.Equals(
                    expected, StringComparison.OrdinalIgnoreCase));
            if (process == null) throw new InvalidOperationException("Exact project is not open.");
            using (TiaPortal portal = process.Attach())
            {
                Project project = portal.Projects.Single();
                Console.WriteLine("MODE=OFFLINE_READ_ONLY; SAVE=NO; DOWNLOAD=NO");
                foreach (Device device in project.Devices.Where(item =>
                    item.Name.Equals("S7-1500/ET200MP station_1", StringComparison.OrdinalIgnoreCase) ||
                    item.Name.Equals("HMI_1", StringComparison.OrdinalIgnoreCase) ||
                    item.Name.Equals("ANYBUS_SMC", StringComparison.OrdinalIgnoreCase)))
                {
                    Console.WriteLine("DEVICE=" + device.Name);
                    Inspect(device.DeviceItems, "  ");
                }
            }
            Console.WriteLine("STATUS=PASS");
            return 0;
        }

        private static void Inspect(DeviceItemComposition items, string indent)
        {
            foreach (DeviceItem item in items)
            {
                Console.WriteLine(indent + "ITEM=" + item.Name + "; TYPE=" + item.TypeIdentifier);
                IEngineeringObject engineering = item as IEngineeringObject;
                foreach (EngineeringAttributeInfo attribute in engineering.GetAttributeInfos())
                {
                    if (!Relevant.Any(term => attribute.Name.IndexOf(term,
                        StringComparison.OrdinalIgnoreCase) >= 0)) continue;
                    if (attribute.Name.IndexOf("Password", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Console.WriteLine(indent + "  ATTR=" + attribute.Name + "; VALUE=<redacted>");
                        continue;
                    }
                    try
                    {
                        object value = engineering.GetAttribute(attribute.Name);
                        Console.WriteLine(indent + "  ATTR=" + attribute.Name + "; VALUE=" +
                            (value == null ? "<null>" : value.ToString()));
                        if (attribute.Name.Equals("PlcCommunicationCertificate",
                            StringComparison.OrdinalIgnoreCase) && value != null)
                            InspectObject(value, indent + "    CERT_");
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(indent + "  ATTR=" + attribute.Name + "; ERROR=" +
                            exception.GetType().Name);
                    }
                }
                NetworkInterface network = item.GetService<NetworkInterface>();
                if (network != null)
                {
                    foreach (Node node in network.Nodes)
                    {
                        IEngineeringObject nodeObject = node as IEngineeringObject;
                        Console.WriteLine(indent + "  NODE=" + node.Name + "; SUBNET=" +
                            (node.ConnectedSubnet == null ? "<none>" : node.ConnectedSubnet.Name));
                        foreach (string name in new[] { "Address", "SubnetMask", "RouterAddress", "PnDeviceName" })
                        {
                            try
                            {
                                object value = nodeObject.GetAttribute(name);
                                Console.WriteLine(indent + "    ATTR=" + name + "; VALUE=" +
                                    (value == null ? "<null>" : value.ToString()));
                            }
                            catch (Exception exception)
                            {
                                Console.WriteLine(indent + "    ATTR=" + name + "; ERROR=" +
                                    exception.GetType().Name);
                            }
                        }
                    }
                }
                SoftwareContainer softwareContainer = item.GetService<SoftwareContainer>();
                HmiSoftware hmi = softwareContainer == null ? null :
                    softwareContainer.Software as HmiSoftware;
                if (hmi != null)
                {
                    foreach (object connection in hmi.Connections)
                    {
                        Console.WriteLine(indent + "  HMI_CONNECTION=" + connection);
                        InspectObject(connection, indent + "    CONNECTION_");
                        PropertyInfo driverProperties = connection.GetType().GetProperty("DriverProperties");
                        IEnumerable values = driverProperties == null ? null :
                            driverProperties.GetValue(connection, null) as IEnumerable;
                        if (values == null) continue;
                        foreach (object value in values)
                        {
                            PropertyInfo propertyName = value.GetType().GetProperty("PropertyName");
                            PropertyInfo propertyValue = value.GetType().GetProperty("Value");
                            try
                            {
                                Console.WriteLine(indent + "    DRIVER_PROPERTY=" +
                                    (propertyName == null ? "<unknown>" : propertyName.GetValue(value, null)) +
                                    "; VALUE=" +
                                    (propertyValue == null ? "<unknown>" : propertyValue.GetValue(value, null)));
                            }
                            catch (Exception exception)
                            {
                                Console.WriteLine(indent + "    DRIVER_PROPERTY_ERROR=" +
                                    exception.GetType().Name);
                            }
                        }
                    }
                }
                Inspect(item.DeviceItems, indent + "  ");
            }
        }

        private static void InspectObject(object value, string prefix)
        {
            Console.WriteLine(prefix + "TYPE=" + value.GetType().FullName);
            IEngineeringObject engineering = value as IEngineeringObject;
            if (engineering != null)
            {
                foreach (EngineeringAttributeInfo attribute in engineering.GetAttributeInfos())
                {
                    if (attribute.Name.IndexOf("Password", StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;
                    try
                    {
                        object attributeValue = engineering.GetAttribute(attribute.Name);
                        Console.WriteLine(prefix + attribute.Name + "=" +
                            (attributeValue == null ? "<null>" : attributeValue.ToString()));
                    }
                    catch { }
                }
            }
            foreach (PropertyInfo property in value.GetType().GetProperties())
            {
                if (property.GetIndexParameters().Length != 0 ||
                    property.Name.IndexOf("Password", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                try
                {
                    object propertyValue = property.GetValue(value, null);
                    if (propertyValue == null || propertyValue is string ||
                        propertyValue.GetType().IsPrimitive || propertyValue is DateTime ||
                        propertyValue.GetType().IsEnum)
                        Console.WriteLine(prefix + property.Name + "=" +
                            (propertyValue == null ? "<null>" : propertyValue.ToString()));
                }
                catch { }
            }
        }
    }
}
