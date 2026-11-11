using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.Compiler;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.HmiUnified.HmiTags;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Tags;

namespace Schlenker.TiaV19
{
    internal static class FixAl101AirPressureHmi
    {
        private const string PlcTagName = "IOL_AL101_X5_OUT2";
        private const string PlcAddress = "%I338.1";
        private const string HmiTagName = "DI_AirPressure_OK";

        private static int Main(string[] args)
        {
            if (args.Length != 1) return 2;
            string expected = Path.GetFullPath(args[0]);
            try
            {
                TiaPortalProcess process = TiaPortal.GetProcesses().Single(candidate =>
                    candidate.ProjectPath != null && Path.GetFullPath(candidate.ProjectPath.FullName)
                        .Equals(expected, StringComparison.OrdinalIgnoreCase));
                using (TiaPortal portal = process.Attach())
                {
                    Project project = portal.Projects.Single();
                    PlcSoftware plc = null;
                    HmiSoftware hmi = null;
                    DeviceItem plcItem = null;
                    DeviceItem hmiItem = null;
                    foreach (Device device in project.Devices.Concat(project.UngroupedDevicesGroup.Devices))
                        FindSoftware(device.DeviceItems, ref plc, ref hmi, ref plcItem, ref hmiItem);
                    if (plc == null || hmi == null)
                        throw new InvalidOperationException("PLC or HMI software was not found.");

                    PlcTag existingByName = FindTag(plc.TagTableGroup, tag =>
                        tag.Name.Equals(PlcTagName, StringComparison.OrdinalIgnoreCase));
                    PlcTag existingByAddress = FindTag(plc.TagTableGroup, tag =>
                        tag.LogicalAddress.Equals(PlcAddress, StringComparison.OrdinalIgnoreCase));
                    if (existingByAddress != null && existingByName == null)
                        throw new InvalidOperationException("Address " + PlcAddress +
                            " is already assigned to PLC tag " + existingByAddress.Name + ".");

                    PlcTag plcTag = existingByName;
                    if (plcTag == null)
                    {
                        PlcTagTable table = plc.TagTableGroup.TagTables.Find("IO-Link Sensors") ??
                            plc.TagTableGroup.TagTables.Create("IO-Link Sensors");
                        plcTag = table.Tags.Create(PlcTagName, "Bool", PlcAddress);
                    }
                    if (!plcTag.DataTypeName.Equals("Bool", StringComparison.OrdinalIgnoreCase) ||
                        !plcTag.LogicalAddress.Equals(PlcAddress, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Existing PLC tag does not match the verified mapping.");

                    HmiTag hmiTag = hmi.TagTables.SelectMany(table => table.Tags).Single(tag =>
                        tag.Name.Equals(HmiTagName, StringComparison.OrdinalIgnoreCase));
                    string oldPlcTag = hmiTag.PlcTag;
                    hmiTag.PlcTag = PlcTagName;
                    project.Save();

                    Console.WriteLine("PROJECT=" + project.Path.FullName);
                    Console.WriteLine("AL101_P5_MODE=IO-Link 2I");
                    Console.WriteLine("AL101_P5_PROCESS_INPUT=%IW337");
                    Console.WriteLine("AL101_P5_OUT2=" + PlcAddress);
                    Console.WriteLine("PLC_TAG=" + plcTag.Name + ";TYPE=" + plcTag.DataTypeName +
                        ";ADDRESS=" + plcTag.LogicalAddress);
                    Console.WriteLine("HMI_TAG=" + hmiTag.Name + ";OLD_PLC_TAG=" + oldPlcTag +
                        ";NEW_PLC_TAG=" + hmiTag.PlcTag + ";CONNECTION=" + hmiTag.Connection);

                    Compile("PLC", plcItem.GetService<ICompilable>());
                    Compile("HMI", hmiItem.GetService<ICompilable>());
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

        private static void Compile(string scope, ICompilable compilable)
        {
            if (compilable == null) throw new InvalidOperationException(scope + " compiler is unavailable.");
            CompilerResult result = compilable.Compile();
            int errors = result.ErrorCount;
            int warnings = result.WarningCount;
            Console.WriteLine("COMPILE_SCOPE=" + scope + ";STATE=" + result.State +
                ";ERRORS=" + errors + ";WARNINGS=" + warnings);
            if (errors != 0)
                throw new InvalidOperationException(scope + " compile failed.");
        }

        private static void FindSoftware(DeviceItemComposition items, ref PlcSoftware plc, ref HmiSoftware hmi,
            ref DeviceItem plcItem, ref DeviceItem hmiItem)
        {
            foreach (DeviceItem item in items)
            {
                SoftwareContainer container = item.GetService<SoftwareContainer>();
                if (container != null)
                {
                    PlcSoftware foundPlc = container.Software as PlcSoftware;
                    HmiSoftware foundHmi = container.Software as HmiSoftware;
                    if (plc == null && foundPlc != null) { plc = foundPlc; plcItem = item; }
                    if (hmi == null && foundHmi != null) { hmi = foundHmi; hmiItem = item; }
                }
                FindSoftware(item.DeviceItems, ref plc, ref hmi, ref plcItem, ref hmiItem);
            }
        }

        private static PlcTag FindTag(PlcTagTableGroup group, Func<PlcTag, bool> predicate)
        {
            foreach (PlcTagTable table in group.TagTables)
                foreach (PlcTag tag in table.Tags)
                    if (predicate(tag)) return tag;
            foreach (PlcTagTableUserGroup child in group.Groups)
            {
                PlcTag found = FindTag(child, predicate);
                if (found != null) return found;
            }
            return null;
        }
    }
}
