using System;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.Compiler;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;

namespace Schlenker.TiaV19
{
    internal static class RestoreAl103ProductPresenceMapping
    {
        private static int Main(string[] args)
        {
            if (args.Length != 2) return 2;
            string expected = Path.GetFullPath(args[0]);
            string backup = Path.GetFullPath(args[1]);
            try
            {
                if (!File.Exists(backup)) throw new FileNotFoundException("Verified pre-change block export missing.", backup);
                TiaPortalProcess process = TiaPortal.GetProcesses().Single(p => p.ProjectPath != null &&
                    Path.GetFullPath(p.ProjectPath.FullName).Equals(expected, StringComparison.OrdinalIgnoreCase));
                using (TiaPortal portal = process.Attach())
                {
                    Project project = portal.Projects.Single();
                    PlcSoftware plc = FindPlc(project);
                    DeviceItem plcItem = FindPlcItem(project);
                    if (plc == null || plcItem == null) throw new InvalidOperationException("PLC software was not found.");
                    PlcBlockComposition owner = null;
                    PlcBlock block = FindBlock(plc.BlockGroup, "FB_Application", ref owner);
                    if (block == null || owner == null) throw new InvalidOperationException("FB_Application was not found.");
                    owner.Import(new FileInfo(backup), ImportOptions.Override);
                    ICompilable compiler = plcItem.GetService<ICompilable>();
                    CompilerResult result = compiler.Compile();
                    Console.WriteLine("COMPILE=PLC;ERRORS=" + result.ErrorCount + ";WARNINGS=" + result.WarningCount);
                    if (result.ErrorCount != 0) throw new InvalidOperationException("PLC compilation failed.");
                    project.Save();
                    Console.WriteLine("PLC_ASSIGNMENT=DB_Global.Inp.ProductPresentAtPump := SPL100_LMT100_ProcessData.%X0");
                    Console.WriteLine("SOURCE=AL103_X1");
                    Console.WriteLine("STATUS=PASS");
                }
                return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine("STATUS=FAIL"); Console.Error.WriteLine(ex); return 1; }
        }

        private static PlcBlock FindBlock(PlcBlockGroup g,string n,ref PlcBlockComposition o){PlcBlock b=g.Blocks.Find(n);if(b!=null){o=g.Blocks;return b;}foreach(PlcBlockUserGroup c in g.Groups){b=FindBlock(c,n,ref o);if(b!=null)return b;}return null;}
        private static PlcSoftware FindPlc(Project p){DeviceItem i=FindPlcItem(p);SoftwareContainer c=i==null?null:i.GetService<SoftwareContainer>();return c==null?null:c.Software as PlcSoftware;}
        private static DeviceItem FindPlcItem(Project p){foreach(Device d in p.Devices.Concat(p.UngroupedDevicesGroup.Devices)){DeviceItem x=FindPlcItem(d.DeviceItems);if(x!=null)return x;}return null;}
        private static DeviceItem FindPlcItem(DeviceItemComposition items){foreach(DeviceItem i in items){SoftwareContainer c=i.GetService<SoftwareContainer>();if(c!=null&&c.Software is PlcSoftware)return i;DeviceItem x=FindPlcItem(i.DeviceItems);if(x!=null)return x;}return null;}
    }
}
