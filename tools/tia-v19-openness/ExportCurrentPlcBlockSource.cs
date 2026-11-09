using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.SW.ExternalSources;

namespace Schlenker.TiaV19
{
    internal static class ExportCurrentPlcBlockSource
    {
        private static int Main(string[] args)
        {
            if (args.Length != 3) return 2;
            string expected=Path.GetFullPath(args[0]), name=args[1], output=Path.GetFullPath(args[2]);
            try
            {
                TiaPortalProcess process=TiaPortal.GetProcesses().Single(p=>p.ProjectPath!=null&&Path.GetFullPath(p.ProjectPath.FullName).Equals(expected,StringComparison.OrdinalIgnoreCase));
                using(TiaPortal portal=process.Attach())
                {
                    Project project=portal.Projects.Single(); PlcSoftware plc=FindPlc(project);
                    PlcBlock block=FindBlock(plc.BlockGroup,name); if(block==null)throw new InvalidOperationException("Block not found: "+name);
                    Directory.CreateDirectory(Path.GetDirectoryName(output));
                    plc.ExternalSourceGroup.GenerateSource(new List<IGenerateSource>{block},new FileInfo(output));
                    Console.WriteLine("BLOCK="+name); Console.WriteLine("SOURCE="+output); Console.WriteLine("STATUS=PASS");
                }
                return 0;
            }
            catch(Exception ex){Console.Error.WriteLine("STATUS=FAIL");Console.Error.WriteLine(ex);return 1;}
        }
        private static PlcBlock FindBlock(PlcBlockGroup g,string n){PlcBlock b=g.Blocks.Find(n);if(b!=null)return b;foreach(PlcBlockUserGroup c in g.Groups){b=FindBlock(c,n);if(b!=null)return b;}return null;}
        private static PlcSoftware FindPlc(Project p){foreach(Device d in p.Devices.Concat(p.UngroupedDevicesGroup.Devices)){PlcSoftware x=FindPlc(d.DeviceItems);if(x!=null)return x;}return null;}
        private static PlcSoftware FindPlc(DeviceItemComposition items){foreach(DeviceItem i in items){SoftwareContainer c=i.GetService<SoftwareContainer>();if(c!=null&&c.Software is PlcSoftware)return(PlcSoftware)c.Software;PlcSoftware x=FindPlc(i.DeviceItems);if(x!=null)return x;}return null;}
    }
}
