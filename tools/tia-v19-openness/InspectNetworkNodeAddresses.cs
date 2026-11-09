using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
namespace Schlenker.TiaV19
{
    internal static class InspectNetworkNodeAddresses
    {
        private static int Main(string[] args)
        {
            if (args.Length != 1) return 2;
            string expected = Path.GetFullPath(args[0]);
            try
            {
                TiaPortalProcess p = TiaPortal.GetProcesses().First(x => x.ProjectPath != null && x.ProjectPath.FullName.Equals(expected, StringComparison.OrdinalIgnoreCase));
                using (TiaPortal portal = p.Attach())
                {
                    Project project = portal.Projects.First();
                    foreach (Device d in AllDevices(project)) foreach (Node n in Nodes(d.DeviceItems))
                    {
                        IEngineeringObject e = n;
                        Console.WriteLine("DEVICE="+d.Name+"; NODE="+n.Name+"; IP="+Get(e,"Address")+"; MASK="+Get(e,"SubnetMask")+"; PN="+Get(e,"PnDeviceName")+"; SUBNET="+(n.ConnectedSubnet==null?"<none>":n.ConnectedSubnet.Name));
                    }
                }
                return 0;
            }
            catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
        }
        private static object Get(IEngineeringObject e,string n){try{return e.GetAttribute(n)??"<null>";}catch{return "<unavailable>";}}
        private static IEnumerable<Node> Nodes(DeviceItemComposition items){foreach(DeviceItem i in items){NetworkInterface n=i.GetService<NetworkInterface>();if(n!=null)foreach(Node x in n.Nodes)yield return x;foreach(Node x in Nodes(i.DeviceItems))yield return x;}}
        private static IEnumerable<Device> AllDevices(Project p){HashSet<string>s=new HashSet<string>(StringComparer.OrdinalIgnoreCase);foreach(Device d in p.Devices)if(s.Add(d.Name))yield return d;foreach(Device d in p.UngroupedDevicesGroup.Devices)if(s.Add(d.Name))yield return d;foreach(DeviceUserGroup g in p.DeviceGroups)foreach(Device d in Group(g))if(s.Add(d.Name))yield return d;}
        private static IEnumerable<Device> Group(DeviceUserGroup g){foreach(Device d in g.Devices)yield return d;foreach(DeviceUserGroup c in g.Groups)foreach(Device d in Group(c))yield return d;}
    }
}
