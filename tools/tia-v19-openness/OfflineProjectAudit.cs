using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.SW.ExternalSources;
using Siemens.Engineering.SW.Types;

namespace Schlenker.TiaV19
{
    internal static class OfflineProjectAudit
    {
        private static int Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine("Usage: OfflineProjectAudit <exact-ap19-path> <report-path> <source-path>");
                return 2;
            }

            string projectPath = Path.GetFullPath(args[0]);
            string reportPath = Path.GetFullPath(args[1]);
            string sourcePath = Path.GetFullPath(args[2]);
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            Directory.CreateDirectory(Path.GetDirectoryName(sourcePath));

            try
            {
                using (StreamWriter report = new StreamWriter(reportPath, false))
                using (TiaPortal portal = new TiaPortal(TiaPortalMode.WithoutUserInterface))
                {
                    Project project = portal.Projects.Open(new FileInfo(projectPath));
                    if (project == null) throw new InvalidOperationException("Project open failed.");
                    PlcSoftware plc = null;
                    HmiSoftware hmi = null;
                    foreach (Device device in project.Devices)
                        FindSoftware(device.DeviceItems, ref plc, ref hmi);
                    if (plc == null || hmi == null)
                        throw new InvalidOperationException("Expected one PLC and one HMI software container.");

                    report.WriteLine("SCHLENKER OFFLINE PROJECT AUDIT");
                    report.WriteLine("Project=" + project.Path.FullName);
                    report.WriteLine("Timestamp=" + DateTimeOffset.Now.ToString("o"));
                    report.WriteLine("SaveInvoked=NO");
                    report.WriteLine("CompileInvoked=NO");
                    report.WriteLine("OnlineApisUsed=NO");

                    List<IGenerateSource> sourceObjects = new List<IGenerateSource>();
                    CollectTypes(plc.TypeGroup.Types, plc.TypeGroup.Groups, sourceObjects, report, "");
                    CollectBlocks(plc.BlockGroup.Blocks, plc.BlockGroup.Groups, sourceObjects, report, "");
                    report.WriteLine("ExternalSources=" + plc.ExternalSourceGroup.ExternalSources.Count);
                    foreach (PlcExternalSource source in plc.ExternalSourceGroup.ExternalSources)
                        report.WriteLine("EXTERNAL_SOURCE\t" + source.Name);

                    plc.ExternalSourceGroup.GenerateSource(sourceObjects, new FileInfo(sourcePath));
                    report.WriteLine("GeneratedSourceObjects=" + sourceObjects.Count);
                    report.WriteLine("GeneratedSource=" + sourcePath);

                    AuditHmiCompositions(hmi, report);
                    report.WriteLine("STATUS=PASS");
                    report.Flush();
                    project.Close();
                    Console.WriteLine("SOURCE_OBJECTS=" + sourceObjects.Count);
                    Console.WriteLine("REPORT=" + reportPath);
                    Console.WriteLine("SOURCE=" + sourcePath);
                    Console.WriteLine("STATUS=PASS");
                    return 0;
                }
            }
            catch (Exception exception)
            {
                File.AppendAllText(reportPath, Environment.NewLine + "STATUS=FAIL" + Environment.NewLine + exception);
                Console.Error.WriteLine("STATUS=FAIL");
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static void FindSoftware(DeviceItemComposition items, ref PlcSoftware plc, ref HmiSoftware hmi)
        {
            foreach (DeviceItem item in items)
            {
                SoftwareContainer container = item.GetService<SoftwareContainer>();
                if (container != null)
                {
                    PlcSoftware candidatePlc = container.Software as PlcSoftware;
                    HmiSoftware candidateHmi = container.Software as HmiSoftware;
                    if (candidatePlc != null)
                    {
                        if (plc != null) throw new InvalidOperationException("Multiple PLC software containers found.");
                        plc = candidatePlc;
                    }
                    if (candidateHmi != null)
                    {
                        if (hmi != null) throw new InvalidOperationException("Multiple HMI software containers found.");
                        hmi = candidateHmi;
                    }
                }
                FindSoftware(item.DeviceItems, ref plc, ref hmi);
            }
        }

        private static void CollectTypes(PlcTypeComposition types, PlcTypeUserGroupComposition groups,
            IList<IGenerateSource> targets, StreamWriter report, string indent)
        {
            foreach (PlcType type in types)
            {
                report.WriteLine(indent + "PLC_TYPE\t" + type.Name);
                IGenerateSource target = type as IGenerateSource;
                if (target != null) targets.Add(target);
            }
            foreach (PlcTypeUserGroup group in groups)
            {
                report.WriteLine(indent + "PLC_TYPE_GROUP\t" + group.Name);
                CollectTypes(group.Types, group.Groups, targets, report, indent + "  ");
            }
        }

        private static void CollectBlocks(PlcBlockComposition blocks, PlcBlockUserGroupComposition groups,
            IList<IGenerateSource> targets, StreamWriter report, string indent)
        {
            foreach (PlcBlock block in blocks)
            {
                report.WriteLine(indent + "PLC_BLOCK\t" + block.Name + "\tNumber=" + block.Number +
                    "\tLanguage=" + block.ProgrammingLanguage + "\tConsistent=" + block.IsConsistent);
                IGenerateSource target = block as IGenerateSource;
                if (target != null) targets.Add(target);
            }
            foreach (PlcBlockUserGroup group in groups)
            {
                report.WriteLine(indent + "PLC_BLOCK_GROUP\t" + group.Name);
                CollectBlocks(group.Blocks, group.Groups, targets, report, indent + "  ");
            }
        }

        private static void AuditHmiCompositions(HmiSoftware hmi, StreamWriter report)
        {
            IEngineeringObject owner = hmi as IEngineeringObject;
            report.WriteLine("HMI_OBJECT_TYPE=" + hmi.GetType().FullName);
            if (owner == null)
            {
                report.WriteLine("HMI_COMPOSITIONS=<unavailable>");
                return;
            }
            foreach (EngineeringCompositionInfo info in owner.GetCompositionInfos())
            {
                report.WriteLine("HMI_COMPOSITION\t" + info.Name);
                if (info.Name.IndexOf("Parameter", StringComparison.OrdinalIgnoreCase) < 0 &&
                    info.Name.IndexOf("Recipe", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                try
                {
                    object composition = owner.GetComposition(info.Name);
                    IEnumerable enumerable = composition as IEnumerable;
                    if (enumerable == null) continue;
                    int index = 0;
                    foreach (object item in enumerable)
                    {
                        index++;
                        report.WriteLine("  HMI_COMPOSITION_ITEM\t" + index + "\t" + item.GetType().FullName +
                            "\t" + item);
                        IEngineeringObject engineeringItem = item as IEngineeringObject;
                        if (engineeringItem == null) continue;
                        foreach (EngineeringAttributeInfo attribute in engineeringItem.GetAttributeInfos())
                        {
                            if (attribute.Name.IndexOf("Password", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                report.WriteLine("    ATTRIBUTE\t" + attribute.Name + "\t<redacted>");
                                continue;
                            }
                            try
                            {
                                object value = engineeringItem.GetAttribute(attribute.Name);
                                report.WriteLine("    ATTRIBUTE\t" + attribute.Name + "\t" +
                                    (value == null ? "<null>" : value.ToString().Replace('\r', ' ').Replace('\n', ' ')));
                            }
                            catch (Exception exception)
                            {
                                report.WriteLine("    ATTRIBUTE\t" + attribute.Name + "\t<error:" +
                                    exception.GetType().Name + ">");
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    report.WriteLine("  HMI_COMPOSITION_ERROR\t" + exception.GetType().Name + "\t" + exception.Message);
                }
            }
        }
    }
}
