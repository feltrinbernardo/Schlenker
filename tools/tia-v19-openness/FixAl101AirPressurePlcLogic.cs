using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Siemens.Engineering;
using Siemens.Engineering.Compiler;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;

namespace Schlenker.TiaV19
{
    internal static class FixAl101AirPressurePlcLogic
    {
        private const string OldSymbol = "IOL_AL101_X5_OUT1";
        private const string NewSymbol = "IOL_AL101_X5_OUT2";

        private static int Main(string[] args)
        {
            if (args.Length != 2) return 2;
            string expected = Path.GetFullPath(args[0]);
            string evidence = Path.GetFullPath(args[1]);
            Directory.CreateDirectory(evidence);
            try
            {
                TiaPortalProcess process = TiaPortal.GetProcesses().Single(candidate =>
                    candidate.ProjectPath != null && Path.GetFullPath(candidate.ProjectPath.FullName)
                        .Equals(expected, StringComparison.OrdinalIgnoreCase));
                using (TiaPortal portal = process.Attach())
                {
                    Project project = portal.Projects.Single();
                    PlcSoftware plc = FindPlc(project);
                    if (plc == null) throw new InvalidOperationException("PLC software was not found.");
                    PlcBlockComposition owner = null;
                    PlcBlock block = FindBlock(plc.BlockGroup, "FB_Application", ref owner);
                    if (block == null || owner == null)
                        throw new InvalidOperationException("FB_Application was not found.");

                    string originalPath = Path.Combine(evidence, "FB_Application.before.xml");
                    string changedPath = Path.Combine(evidence, "FB_Application.air-pressure.xml");
                    block.Export(new FileInfo(originalPath), ExportOptions.WithDefaults);
                    string xml = File.ReadAllText(originalPath, Encoding.UTF8);
                    int occurrences = CountOccurrences(xml, "Component Name=\"" + OldSymbol + "\"");
                    if (occurrences != 1)
                        throw new InvalidOperationException("Expected exactly one legacy air-pressure source, found " + occurrences + ".");
                    string changed = xml.Replace("Component Name=\"" + OldSymbol + "\"",
                        "Component Name=\"" + NewSymbol + "\"");
                    File.WriteAllText(changedPath, changed, new UTF8Encoding(false));
                    owner.Import(new FileInfo(changedPath), ImportOptions.Override);

                    ICompilable compiler = FindPlcItem(project).GetService<ICompilable>();
                    if (compiler == null) throw new InvalidOperationException("PLC compiler unavailable.");
                    CompilerResult result = compiler.Compile();
                    Console.WriteLine("COMPILE_STATE=" + result.State + ";ERRORS=" + result.ErrorCount +
                        ";WARNINGS=" + result.WarningCount);
                    if (result.ErrorCount != 0)
                        throw new InvalidOperationException("PLC compilation failed.");
                    project.Save();
                    Console.WriteLine("PLC_ASSIGNMENT=DB_Global.Inp.AirPressureOK := " + NewSymbol);
                    Console.WriteLine("PROJECT_SAVED=YES");
                    Console.WriteLine("DOWNLOAD_PERFORMED=NO");
                    Console.WriteLine("STATUS=PASS");
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

        private static int CountOccurrences(string source, string value)
        {
            int count = 0, index = 0;
            while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }
            return count;
        }

        private static PlcSoftware FindPlc(Project project)
        {
            DeviceItem item = FindPlcItem(project);
            SoftwareContainer container = item == null ? null : item.GetService<SoftwareContainer>();
            return container == null ? null : container.Software as PlcSoftware;
        }

        private static DeviceItem FindPlcItem(Project project)
        {
            foreach (Device device in project.Devices.Concat(project.UngroupedDevicesGroup.Devices))
            {
                DeviceItem found = FindPlcItem(device.DeviceItems);
                if (found != null) return found;
            }
            return null;
        }

        private static DeviceItem FindPlcItem(DeviceItemComposition items)
        {
            foreach (DeviceItem item in items)
            {
                SoftwareContainer container = item.GetService<SoftwareContainer>();
                if (container != null && container.Software is PlcSoftware) return item;
                DeviceItem found = FindPlcItem(item.DeviceItems);
                if (found != null) return found;
            }
            return null;
        }

        private static PlcBlock FindBlock(PlcBlockGroup group, string name, ref PlcBlockComposition owner)
        {
            PlcBlock found = group.Blocks.Find(name);
            if (found != null) { owner = group.Blocks; return found; }
            foreach (PlcBlockUserGroup child in group.Groups)
            {
                found = FindBlock(child, name, ref owner);
                if (found != null) return found;
            }
            return null;
        }
    }
}
