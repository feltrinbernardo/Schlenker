using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.HmiUnified.HmiTags;
using Siemens.Engineering.HmiUnified.UI.Base;
using Siemens.Engineering.HmiUnified.UI.Screens;
using Siemens.Engineering.SW;

namespace Schlenker.TiaV19
{
    internal static class HmiRefinementReadOnlyBaseline
    {
        private static readonly string[] ObjectProperties =
        {
            "Left", "Top", "Width", "Height", "CenterX", "CenterY", "RadiusX", "RadiusY",
            "Text", "Graphic", "Visible", "Enabled", "Authorization", "ProcessValue", "IOFieldType",
            "BackColor", "ForeColor", "BorderColor", "BorderWidth", "FontSize", "FontWeight",
            "HorizontalAlignment", "VerticalAlignment"
        };

        private static readonly string[] FramePrefixes =
        {
            "REV13_Common_", "REV13_Nav_", "REV14_Nav_", "REV21_Common_"
        };

        private static int Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine(
                    "Usage: HmiRefinementReadOnlyBaseline <exact-ap19-path> <screen-name> <output-directory>");
                return 2;
            }

            string projectPath = Path.GetFullPath(args[0]);
            string screenName = args[1];
            string outputDirectory = Path.GetFullPath(args[2]);

            try
            {
                if (!File.Exists(projectPath) || !projectPath.EndsWith(".ap19", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Exact TIA Portal V19 .ap19 path was not found: " + projectPath);
                if (!screenName.Equals("production", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("This baseline exporter is restricted to the production screen.");

                Directory.CreateDirectory(outputDirectory);
                string projectHashBefore = Sha256(projectPath);
                DateTime projectWriteTimeBefore = File.GetLastWriteTimeUtc(projectPath);

                using (TiaPortal portal = new TiaPortal(TiaPortalMode.WithoutUserInterface))
                {
                    Project project = portal.Projects.Open(new FileInfo(projectPath));
                    if (project == null) throw new InvalidOperationException("TIA did not open the exact project.");
                    if (!Path.GetFullPath(project.Path.FullName).Equals(projectPath, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("TIA opened a different project: " + project.Path.FullName);

                    string deviceName;
                    string hmiItemName;
                    HmiSoftware hmi = FindHmi(project, out deviceName, out hmiItemName);
                    if (hmi == null) throw new InvalidOperationException("No WinCC Unified HMI software was found.");
                    HmiScreen production = hmi.Screens.Find(screenName);
                    if (production == null) throw new InvalidOperationException("Screen not found: " + screenName);

                    Dictionary<string, HmiTag> tags = hmi.TagTables
                        .SelectMany(table => table.Tags)
                        .GroupBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
                    HashSet<string> referencedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    WriteScreenSummary(hmi, Path.Combine(outputDirectory, "screen-summary.tsv"));
                    WriteProductionObjects(production, referencedTags,
                        Path.Combine(outputDirectory, "production-object-baseline.tsv"));
                    WriteReferencedTags(tags, referencedTags,
                        Path.Combine(outputDirectory, "production-referenced-tags.tsv"));
                    WriteFrameMatrix(hmi, Path.Combine(outputDirectory, "common-frame-matrix.tsv"));

                    string manifestPath = Path.Combine(outputDirectory, "baseline-manifest.tsv");
                    using (StreamWriter writer = Utf8Writer(manifestPath))
                    {
                        writer.WriteLine("Field\tValue");
                        writer.WriteLine("Mode\tREAD_ONLY");
                        writer.WriteLine("Platform\tTIA Portal V19 Openness");
                        writer.WriteLine("ProjectPath\t" + Clean(project.Path.FullName));
                        writer.WriteLine("ProjectSha256Before\t" + projectHashBefore);
                        writer.WriteLine("ProjectLastWriteUtcBefore\t" + projectWriteTimeBefore.ToString("o"));
                        writer.WriteLine("Device\t" + Clean(deviceName));
                        writer.WriteLine("HmiItem\t" + Clean(hmiItemName));
                        writer.WriteLine("Screen\t" + Clean(production.Name));
                        writer.WriteLine("Resolution\t" + production.Width + "x" + production.Height);
                        writer.WriteLine("ScreenItems\t" + production.ScreenItems.Count);
                        writer.WriteLine("Screens\t" + hmi.Screens.Count);
                        writer.WriteLine("ReferencedTags\t" + referencedTags.Count);
                        writer.WriteLine("SaveInvoked\tNO");
                        writer.WriteLine("CompileInvoked\tNO");
                        writer.WriteLine("ArchiveInvoked\tNO");
                        writer.WriteLine("HardwareCommunicationInvoked\tNO");
                    }

                    project.Close();
                }

                string projectHashAfter = Sha256(projectPath);
                DateTime projectWriteTimeAfter = File.GetLastWriteTimeUtc(projectPath);
                if (!projectHashAfter.Equals(projectHashBefore, StringComparison.OrdinalIgnoreCase) ||
                    projectWriteTimeAfter != projectWriteTimeBefore)
                    throw new InvalidOperationException("The project changed during the read-only baseline export.");

                File.AppendAllLines(Path.Combine(outputDirectory, "baseline-manifest.tsv"), new[]
                {
                    "ProjectSha256After\t" + projectHashAfter,
                    "ProjectLastWriteUtcAfter\t" + projectWriteTimeAfter.ToString("o"),
                    "ProjectUnchanged\tYES",
                    "Status\tPASS"
                }, new UTF8Encoding(false));

                Console.WriteLine("PROJECT=" + projectPath);
                Console.WriteLine("SCREEN=" + screenName);
                Console.WriteLine("OUTPUT=" + outputDirectory);
                Console.WriteLine("PROJECT_UNCHANGED=YES");
                Console.WriteLine("STATUS=PASS");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("STATUS=FAIL");
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static void WriteScreenSummary(HmiSoftware hmi, string path)
        {
            using (StreamWriter writer = Utf8Writer(path))
            {
                writer.WriteLine("Screen\tNumber\tWidth\tHeight\tItems");
                foreach (HmiScreen screen in hmi.Screens.OrderBy(value => value.Name))
                    writer.WriteLine(String.Join("\t", Clean(screen.Name), screen.ScreenNumber,
                        screen.Width, screen.Height, screen.ScreenItems.Count));
            }
        }

        private static void WriteProductionObjects(
            HmiScreen screen, HashSet<string> referencedTags, string path)
        {
            using (StreamWriter writer = Utf8Writer(path))
            {
                writer.WriteLine("Screen\tObject\tType\tProperties\tDynamizations\tEvents\tClassification");
                foreach (HmiScreenItemBase item in screen.ScreenItems.OrderBy(value => value.Name))
                {
                    string dynamizations = DescribeComposition(item, "Dynamizations", referencedTags);
                    string events = DescribeComposition(item, "EventHandlers", referencedTags);
                    string combined = (item.Name + " " + DescribeProperties(item) + " " +
                        dynamizations + " " + events).ToUpperInvariant();
                    string classification = combined.Contains("MISSING") || combined.Contains("UNRESOLVED")
                        ? "MISSING"
                        : combined.Contains("NOT CONFIGURED") || combined.Contains("N/A") || combined.Contains("TBC")
                            ? "NOT CONFIGURED" : "CONFIRMED";
                    writer.WriteLine(String.Join("\t", Clean(screen.Name), Clean(item.Name),
                        item.GetType().Name, Clean(DescribeProperties(item)), Clean(dynamizations),
                        Clean(events), classification));
                }
            }
        }

        private static void WriteReferencedTags(
            IDictionary<string, HmiTag> tags, IEnumerable<string> referencedTags, string path)
        {
            using (StreamWriter writer = Utf8Writer(path))
            {
                writer.WriteLine("HmiTag\tExists\tPlcTag\tDataType\tConnection\tClassification");
                foreach (string name in referencedTags.OrderBy(value => value))
                {
                    HmiTag tag;
                    bool exists = tags.TryGetValue(name, out tag);
                    writer.WriteLine(String.Join("\t", Clean(name), exists ? "YES" : "NO",
                        exists ? Clean(tag.PlcTag) : "", exists ? Clean(tag.DataType.ToString()) : "",
                        exists ? Clean(tag.Connection) : "",
                        !exists ? "MISSING" : String.IsNullOrWhiteSpace(tag.PlcTag) ? "NOT CONFIGURED" : "CONFIRMED"));
                }
            }
        }

        private static void WriteFrameMatrix(HmiSoftware hmi, string path)
        {
            using (StreamWriter writer = Utf8Writer(path))
            {
                writer.WriteLine("Screen\tObject\tType\tProperties");
                foreach (HmiScreen screen in hmi.Screens.OrderBy(value => value.Name))
                foreach (HmiScreenItemBase item in screen.ScreenItems.OrderBy(value => value.Name))
                {
                    if (!IsFrameObject(item.Name)) continue;
                    writer.WriteLine(String.Join("\t", Clean(screen.Name), Clean(item.Name),
                        item.GetType().Name, Clean(DescribeProperties(item))));
                }
            }
        }

        private static bool IsFrameObject(string name)
        {
            return name.Equals("REV12_Production_User_Header", StringComparison.OrdinalIgnoreCase) ||
                FramePrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        private static string DescribeProperties(object value)
        {
            List<string> parts = new List<string>();
            foreach (string propertyName in ObjectProperties)
            {
                object propertyValue = ReadProperty(value, propertyName);
                if (propertyValue != null) parts.Add(propertyName + "=" + propertyValue);
            }
            return String.Join("; ", parts);
        }

        private static string DescribeComposition(
            object item, string propertyName, HashSet<string> referencedTags)
        {
            IEnumerable values = ReadProperty(item, propertyName) as IEnumerable;
            if (values == null) return "";
            List<string> parts = new List<string>();
            foreach (object value in values)
            {
                if (value == null) continue;
                List<string> details = new List<string> { "Type=" + value.GetType().Name };
                foreach (string name in new[] { "PropertyName", "Tag", "ReadOnly", "UseIndirectAddressing", "EventType" })
                {
                    object propertyValue = ReadProperty(value, name);
                    if (propertyValue == null) continue;
                    details.Add(name + "=" + propertyValue);
                    if (name == "Tag" && !String.IsNullOrWhiteSpace(propertyValue.ToString()))
                        referencedTags.Add(propertyValue.ToString());
                }
                object scriptCode = ReadProperty(value, "ScriptCode");
                if (scriptCode == null)
                {
                    object script = ReadProperty(value, "Script");
                    scriptCode = script == null ? null : ReadProperty(script, "ScriptCode");
                }
                if (scriptCode != null)
                {
                    string code = scriptCode.ToString();
                    details.Add("Script=" + code);
                    foreach (string tag in ExtractScriptTags(code)) referencedTags.Add(tag);
                }
                parts.Add(String.Join(",", details));
            }
            return String.Join(" | ", parts);
        }

        private static IEnumerable<string> ExtractScriptTags(string script)
        {
            const string marker = "HMIRuntime.Tags(\"";
            int index = 0;
            while (!String.IsNullOrEmpty(script) &&
                (index = script.IndexOf(marker, index, StringComparison.Ordinal)) >= 0)
            {
                int start = index + marker.Length;
                int end = script.IndexOf("\"", start, StringComparison.Ordinal);
                if (end < 0) yield break;
                yield return script.Substring(start, end - start);
                index = end + 1;
            }
        }

        private static HmiSoftware FindHmi(Project project, out string deviceName, out string hmiItemName)
        {
            foreach (Device device in project.Devices)
            {
                HmiSoftware found = FindHmi(device.DeviceItems, out hmiItemName);
                if (found != null)
                {
                    deviceName = device.Name;
                    return found;
                }
            }
            deviceName = null;
            hmiItemName = null;
            return null;
        }

        private static HmiSoftware FindHmi(DeviceItemComposition items, out string itemName)
        {
            foreach (DeviceItem item in items)
            {
                SoftwareContainer container = item.GetService<SoftwareContainer>();
                HmiSoftware hmi = container == null ? null : container.Software as HmiSoftware;
                if (hmi != null)
                {
                    itemName = item.Name;
                    return hmi;
                }
                hmi = FindHmi(item.DeviceItems, out itemName);
                if (hmi != null) return hmi;
            }
            itemName = null;
            return null;
        }

        private static object ReadProperty(object value, string name)
        {
            if (value == null) return null;
            try
            {
                PropertyInfo property = value.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
                return property == null || !property.CanRead ? null : property.GetValue(value, null);
            }
            catch { return null; }
        }

        private static StreamWriter Utf8Writer(string path)
        {
            return new StreamWriter(path, false, new UTF8Encoding(false));
        }

        private static string Sha256(string path)
        {
            using (SHA256 algorithm = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
                return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", "");
        }

        private static string Clean(object value)
        {
            return value == null ? "" : value.ToString()
                .Replace("\t", " ").Replace("\r", " ").Replace("\n", " ");
        }
    }
}
