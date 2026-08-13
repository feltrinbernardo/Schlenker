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
using Siemens.Engineering.HmiUnified.HmiTags;
using Siemens.Engineering.HmiUnified.UI.Base;
using Siemens.Engineering.HmiUnified.UI.Screens;
using Siemens.Engineering.SW;

namespace Schlenker.TiaV19
{
    internal static class Rev25ProductionBindingAudit
    {
        private static readonly string[] StaticProperties =
        {
            "Left", "Top", "Width", "Height", "Text", "Graphic", "Visible", "Enabled",
            "Authorization", "ProcessValue", "IOFieldType", "BackColor", "ForeColor"
        };

        private static int Main(string[] args)
        {
            string projectHint = args.Length > 0 ? args[0] : ".ap19";
            string screenName = args.Length > 1 ? args[1] : "production";
            string outputDirectory = args.Length > 2 ? args[2] : Environment.CurrentDirectory;

            try
            {
                Directory.CreateDirectory(outputDirectory);
                TiaPortalProcess process = TiaPortal.GetProcesses().FirstOrDefault(candidate =>
                    candidate.ProjectPath != null &&
                    candidate.ProjectPath.FullName.EndsWith(projectHint, StringComparison.OrdinalIgnoreCase));
                if (process == null) throw new InvalidOperationException("No open TIA project matched " + projectHint);

                using (TiaPortal portal = process.Attach())
                {
                    Project project = portal.Projects.FirstOrDefault();
                    if (project == null) throw new InvalidOperationException("Attached process has no project.");
                    HmiSoftware hmi = FindHmi(project);
                    if (hmi == null) throw new InvalidOperationException("No Unified HMI software found.");
                    HmiScreen screen = hmi.Screens.Find(screenName);
                    if (screen == null) throw new InvalidOperationException("Screen not found: " + screenName);

                    Dictionary<string, HmiTag> tags = hmi.TagTables
                        .SelectMany(table => table.Tags)
                        .GroupBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

                    string objectsPath = Path.Combine(outputDirectory, "production-object-binding-audit.tsv");
                    string tagsPath = Path.Combine(outputDirectory, "production-referenced-tags.tsv");
                    HashSet<string> referencedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    using (StreamWriter writer = new StreamWriter(objectsPath, false, System.Text.Encoding.UTF8))
                    {
                        writer.WriteLine("Screen\tObject\tType\tStaticProperties\tDynamizations\tEvents\tClassification");
                        foreach (HmiScreenItemBase item in screen.ScreenItems.OrderBy(item => item.Name))
                        {
                            string dynamizations = DescribeComposition(item, "Dynamizations", referencedTags);
                            string events = DescribeComposition(item, "EventHandlers", referencedTags);
                            string classification = Classify(item, dynamizations, events);
                            writer.WriteLine(string.Join("\t", new[]
                            {
                                Clean(screen.Name), Clean(item.Name), Clean(item.GetType().Name),
                                Clean(DescribeStatic(item)), Clean(dynamizations), Clean(events), classification
                            }));
                        }
                    }

                    using (StreamWriter writer = new StreamWriter(tagsPath, false, System.Text.Encoding.UTF8))
                    {
                        writer.WriteLine("HmiTag\tExists\tPlcTag\tDataType\tConnection\tClassification");
                        foreach (string name in referencedTags.OrderBy(value => value))
                        {
                            HmiTag tag;
                            bool exists = tags.TryGetValue(name, out tag);
                            writer.WriteLine(string.Join("\t", new[]
                            {
                                Clean(name), exists ? "YES" : "NO", exists ? Clean(tag.PlcTag) : "",
                                exists ? Clean(tag.DataType.ToString()) : "", exists ? Clean(tag.Connection) : "",
                                !exists ? "MISSING" : String.IsNullOrWhiteSpace(tag.PlcTag) ? "NOT CONFIGURED" : "CONFIRMED"
                            }));
                        }
                    }

                    Console.WriteLine("PROJECT=" + project.Path.FullName);
                    Console.WriteLine("SCREEN=" + screen.Name + "; ITEMS=" + screen.ScreenItems.Count);
                    Console.WriteLine("OBJECT_AUDIT=" + objectsPath);
                    Console.WriteLine("TAG_AUDIT=" + tagsPath);
                    Console.WriteLine("REFERENCED_TAGS=" + referencedTags.Count);
                }

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

        private static HmiSoftware FindHmi(Project project)
        {
            foreach (Device device in project.Devices)
            {
                HmiSoftware found = FindHmi(device.DeviceItems);
                if (found != null) return found;
            }
            return null;
        }

        private static HmiSoftware FindHmi(DeviceItemComposition items)
        {
            foreach (DeviceItem item in items)
            {
                SoftwareContainer container = item.GetService<SoftwareContainer>();
                HmiSoftware hmi = container == null ? null : container.Software as HmiSoftware;
                if (hmi != null) return hmi;
                HmiSoftware child = FindHmi(item.DeviceItems);
                if (child != null) return child;
            }
            return null;
        }

        private static string DescribeStatic(object value)
        {
            List<string> parts = new List<string>();
            foreach (string propertyName in StaticProperties)
            {
                object propertyValue = ReadProperty(value, propertyName);
                if (propertyValue != null) parts.Add(propertyName + "=" + propertyValue);
            }
            return string.Join("; ", parts);
        }

        private static string DescribeComposition(object item, string propertyName, HashSet<string> referencedTags)
        {
            object composition = ReadProperty(item, propertyName);
            IEnumerable values = composition as IEnumerable;
            if (values == null) return "";
            List<string> parts = new List<string>();
            foreach (object value in values)
            {
                if (value == null) continue;
                List<string> detail = new List<string>();
                detail.Add("Type=" + value.GetType().Name);
                foreach (string name in new[] { "PropertyName", "Tag", "ReadOnly", "UseIndirectAddressing", "EventType" })
                {
                    object propertyValue = ReadProperty(value, name);
                    if (propertyValue == null) continue;
                    detail.Add(name + "=" + propertyValue);
                    if (name == "Tag" && !String.IsNullOrWhiteSpace(propertyValue.ToString())) referencedTags.Add(propertyValue.ToString());
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
                    detail.Add("Script=" + code);
                    foreach (string tag in ExtractScriptTags(code)) referencedTags.Add(tag);
                }
                parts.Add(string.Join(",", detail));
            }
            return string.Join(" | ", parts);
        }

        private static IEnumerable<string> ExtractScriptTags(string script)
        {
            const string marker = "HMIRuntime.Tags(\"";
            int index = 0;
            while (!String.IsNullOrEmpty(script) && (index = script.IndexOf(marker, index, StringComparison.Ordinal)) >= 0)
            {
                int start = index + marker.Length;
                int end = script.IndexOf("\"", start, StringComparison.Ordinal);
                if (end < 0) yield break;
                yield return script.Substring(start, end - start);
                index = end + 1;
            }
        }

        private static string Classify(HmiScreenItemBase item, string dynamizations, string events)
        {
            string combined = (item.Name + " " + DescribeStatic(item) + " " + dynamizations + " " + events).ToUpperInvariant();
            if (combined.Contains("NOT CONFIGURED") || combined.Contains("TBC") || combined.Contains("N/A")) return "NOT CONFIGURED";
            if (combined.Contains("MISSING") || combined.Contains("UNRESOLVED")) return "MISSING";
            return "CONFIRMED";
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

        private static string Clean(object value)
        {
            return value == null ? "" : value.ToString().Replace("\t", " ").Replace("\r", " ").Replace("\n", " ");
        }
    }
}
