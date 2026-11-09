using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
namespace Schlenker.TiaV19
{
    internal static class ConfigureBenchNetworkAddressesOffline
    {
        private static readonly Dictionary<string,string> AddressByDevice = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase)
        {
            {"S7-1500/ET200MP station_1","192.168.10.1"}, {"HMI_1","192.168.10.2"},
            {"AL100","192.168.10.30"}, {"AL101","192.168.10.31"},
            {"AL102","192.168.10.32"}, {"AL103","192.168.10.33"}
        };
        private static int Main(string[] args)
        {
            if(args.Length!=1)return 2; string expected=Path.GetFullPath(args[0]);
            try
            {
                TiaPortalProcess p=TiaPortal.GetProcesses().First(x=>x.ProjectPath!=null&&x.ProjectPath.FullName.Equals(expected,StringComparison.OrdinalIgnoreCase));
                using(TiaPortal portal=p.Attach())
                {
                    Project project=portal.Projects.First();
                    foreach(Device d in AllDevices(project))
                    {
                        string address; if(!AddressByDevice.TryGetValue(d.Name,out address))continue;
                        Node node=Nodes(d.DeviceItems).First(n=>n.ConnectedSubnet!=null&&n.ConnectedSubnet.Name.Equals("PN/IE_1",StringComparison.OrdinalIgnoreCase));
                        IEngineeringObject e=node;
                        e.SetAttribute("Address",address);
                        e.SetAttribute("SubnetMask","255.255.255.0");
                        string actual=e.GetAttribute("Address") as string;
                        if(!address.Equals(actual,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException(d.Name+" address validation failed: "+actual);
                        Console.WriteLine(d.Name+"="+actual);
                    }
                    project.Save(); Console.WriteLine("SAVE=YES; ONLINE=NO; DOWNLOAD=NO; STATUS=PASS");
                }
                return 0;
            }
            catch(Exception ex){Console.Error.WriteLine("STATUS=FAIL");Console.Error.WriteLine(ex);return 1;}
        }
        private static IEnumerable<Node> Nodes(DeviceItemComposition items){foreach(DeviceItem i in items){NetworkInterface n=i.GetService<NetworkInterface>();if(n!=null)foreach(Node x in n.Nodes)yield return x;foreach(Node x in Nodes(i.DeviceItems))yield return x;}}
        private static IEnumerable<Device> AllDevices(Project p){HashSet<string>s=new HashSet<string>(StringComparer.OrdinalIgnoreCase);foreach(Device d in p.Devices)if(s.Add(d.Name))yield return d;foreach(Device d in p.UngroupedDevicesGroup.Devices)if(s.Add(d.Name))yield return d;foreach(DeviceUserGroup g in p.DeviceGroups)foreach(Device d in Group(g))if(s.Add(d.Name))yield return d;}
        private static IEnumerable<Device> Group(DeviceUserGroup g){foreach(Device d in g.Devices)yield return d;foreach(DeviceUserGroup c in g.Groups)foreach(Device d in Group(c))yield return d;}
    }
}
