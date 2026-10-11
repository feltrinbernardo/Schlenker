using System;
using System.IO;
using Siemens.Engineering;
using Siemens.Engineering.Compiler;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.SW.ExternalSources;

namespace Schlenker.TiaV19
{
    internal static class RemoveTemporaryVsdTestOffline
    {
        private static int Main(string[] args)
        {
            if (args.Length != 1) return 2;
            try
            {
                using (TiaPortal portal = new TiaPortal(TiaPortalMode.WithoutUserInterface))
                {
                    Project project = portal.Projects.Open(new FileInfo(Path.GetFullPath(args[0])));
                    try
                    {
                        PlcSoftware plc = FindPlc(project);
                        if (plc == null) throw new InvalidOperationException("PLC software not found.");

                        DeleteBlock(plc, "DB_BenchVsdMainTest");
                        DeleteBlock(plc, "FB_BenchVsdMainTest");
                        DeleteSource(plc, "16_DB_BenchVsdMainTest.scl");
                        DeleteSource(plc, "15_FB_BenchVsdMainTest.scl");

                        CompilerResult result = plc.GetService<ICompilable>().Compile();
                        Console.WriteLine("ERRORS=" + result.ErrorCount);
                        Console.WriteLine("WARNINGS=" + result.WarningCount);
                        if (result.ErrorCount != 0)
                            throw new InvalidOperationException("PLC compile failed after temporary-test cleanup.");
                        project.Save();
                        Console.WriteLine("STATUS=PASS_CLEANED");
                    }
                    finally { project.Close(); }
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

        private static void DeleteBlock(PlcSoftware plc, string name)
        {
            PlcBlock block = plc.BlockGroup.Blocks.Find(name);
            if (block != null)
            {
                block.Delete();
                Console.WriteLine("DELETED_BLOCK=" + name);
            }
        }

        private static void DeleteSource(PlcSoftware plc, string name)
        {
            PlcExternalSource source = plc.ExternalSourceGroup.ExternalSources.Find(name);
            if (source != null)
            {
                source.Delete();
                Console.WriteLine("DELETED_SOURCE=" + name);
            }
        }

        private static PlcSoftware FindPlc(Project project)
        {
            foreach (Device device in project.Devices)
            {
                PlcSoftware plc = FindPlc(device.DeviceItems);
                if (plc != null) return plc;
            }
            return null;
        }

        private static PlcSoftware FindPlc(DeviceItemComposition items)
        {
            foreach (DeviceItem item in items)
            {
                SoftwareContainer container = item.GetService<SoftwareContainer>();
                PlcSoftware plc = container == null ? null : container.Software as PlcSoftware;
                if (plc != null) return plc;
                plc = FindPlc(item.DeviceItems);
                if (plc != null) return plc;
            }
            return null;
        }
    }
}
