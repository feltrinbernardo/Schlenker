using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.HmiUnified.UI.Screens;

// READ ONLY: attaches only to the designated agent copy. No save, compile,
// project open/close, online service, download, runtime tag write or PLC action.
internal static class AuditIsolatedFunctionalCoverage
{
    const string AllowedProject = @"C:\www\Schlenker\runs\2026-11-11-rob-isolated-workspace\work-copy\Schlenker_Pendant_Offline.ap19";
    const string AllowedOutput = @"C:\www\Schlenker\runs\2026-11-12-offline-functional-audit";
    static StreamWriter objects, details, tags, issues;
    static int issueCount, objectCount, screenCount, tagCount;
    static HashSet<object> visited = new HashSet<object>();
    static HashSet<string> eventTargets = new HashSet<string>(StringComparer.Ordinal);
    static HashSet<string> dynamicTargets = new HashSet<string>(StringComparer.Ordinal);
    const string PriorInventory = @"C:\www\Schlenker\runs\2026-11-12-offline-functional-audit\native-inventory-02\events-bindings.tsv";
    static Dictionary<string, PropertyInfo> propertyCache = new Dictionary<string, PropertyInfo>();
    static readonly string[] Properties = { "Address", "AccessMode", "TagType", "Scope", "LinearScaling", "HmiStartValue", "HmiEndValue", "PlcStartValue", "PlcEndValue", "PlcName", "Authorization", "RequiredFunctionRights", "ScreenName", "ScreenNumber", "GlobalDefinitionAreaScriptCode", "Async", "DynamizationType", "CustomDuration", "Type", "ConditionType", "Condition", "Relevant", "BitDynamizationType", "From", "To", "RangeType", "Value", "AlternateValue", "Flashing", "FlashingRate", "ResourceList", "Name", "Text", "AlternateText", "Visible", "Enabled", "Left", "Top", "Width", "Height", "ProcessValue", "IOFieldType", "ReadOnly", "PropertyName", "Tag", "UseIndirectAddressing", "EventType", "ScriptCode", "PlcTag", "Connection", "DataType", "AcquisitionMode", "AcquisitionCycle" };

    static int Main(string[] args)
    {
        try
        {
            if (args.Length != 1) throw new ArgumentException("Usage: AuditIsolatedFunctionalCoverage <new-output-subdirectory-under-approved-run>");
            string output = Path.GetFullPath(args[0]);
            string prefix = Path.GetFullPath(AllowedOutput).TrimEnd('\\') + "\\";
            if (!output.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Output outside approved run");
            if (Directory.Exists(output) || File.Exists(output)) throw new InvalidOperationException("Output must be new; existing evidence is immutable");
            LoadPriorTargets();
            string ancestor = Path.GetDirectoryName(output);
            while (!String.IsNullOrEmpty(ancestor))
            {
                if (Directory.Exists(ancestor) && (File.GetAttributes(ancestor) & FileAttributes.ReparsePoint) != 0) throw new InvalidOperationException("Reparse output ancestor");
                ancestor = Path.GetDirectoryName(ancestor);
            }
            TiaPortalProcess process = TiaPortal.GetProcesses().Single(p => p.ProjectPath != null && Path.GetFullPath(p.ProjectPath.FullName).Equals(AllowedProject, StringComparison.OrdinalIgnoreCase));
            using (TiaPortal portal = process.Attach())
            using (ExclusiveAccess exclusive = portal.ExclusiveAccess("Read-only isolated Schlenker command inventory; do not edit concurrently"))
            {
                Console.WriteLine("STAGE=ATTACHED_EXCLUSIVE; AT=" + DateTimeOffset.Now.ToString("o"));
                Project project = portal.Projects.Single();
                if (!Path.GetFullPath(project.Path.FullName).Equals(AllowedProject, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Project changed after attachment");
                Directory.CreateDirectory(output);
                using (objects = Writer(output, "objects.tsv"))
                using (details = Writer(output, "events-bindings.tsv"))
                using (tags = Writer(output, "tags.tsv"))
                using (issues = Writer(output, "coverage-issues.tsv"))
                {
                    objects.WriteLine("Screen\tObjectPath\tType\tProperties");
                    details.WriteLine("Screen\tObjectPath\tKind\tDetails");
                    tags.WriteLine("Path\tType\tProperties");
                    issues.WriteLine("Path\tIssue");
                    Console.WriteLine("STAGE=FIND_NAMED_HMI_1");
                    List<HmiSoftware> hmis = FindNamedHmi(project);
                    if (hmis.Count != 1) throw new InvalidOperationException("Expected one Unified HMI, found " + hmis.Count);
                    HmiSoftware hmi = hmis[0];
                    Console.WriteLine("STAGE=HMI_FOUND; AT=" + DateTimeOffset.Now.ToString("o"));
                    WalkScreens(hmi, "", exclusive);
                    visited.Clear();
                    WalkTags(hmi, "HMI", exclusive);
                    ExportScriptModules(hmi, output, exclusive);
                    if (screenCount == 0 || objectCount == 0 || tagCount == 0) Issue("ROOT", "Empty required inventory");
                }
                File.WriteAllLines(Path.Combine(output, "manifest.txt"), new[] {
                    "Project=" + project.Path.FullName, "Timestamp=" + DateTimeOffset.Now.ToString("o"),
                    "Mode=READ_ONLY_ATTACHED_EXCLUSIVE", "Save=NO; Compile=NO; Download=NO; Online=NO",
                    "Screens=" + screenCount, "Objects=" + objectCount, "Tags=" + tagCount, "CoverageIssues=" + issueCount,
                    "Status=" + (issueCount == 0 ? "EXTRACTED_NOT_FUNCTIONALLY_TESTED" : "PARTIAL_EXTRACTION"),
                    "Scope=Screen objects, property events, event/binding/global-definition scripts, singular triggers, value converters, mapping entries, tag members and global script-module export. Runtime behavior is NOT TESTED.",
                    "Coverage=Reflection extraction does not prove semantic completeness. Faceplates, custom controls, runtime/system behavior and indirect references require separate verification.",
                    "TargetInventory=" + PriorInventory,
                    "TargetPolicy=Supplemental metadata only; existing event/dynamization targets from first native inventory. PropertyEventHandlers examined on every item. No edit may occur between inventories.",
                    "PLCExport=NOT_PERFORMED; Connections/security configuration not exported; no secret-valued attributes requested."
                }, new UTF8Encoding(false));
                Console.WriteLine("OUTPUT=" + output + "; SCREENS=" + screenCount + "; OBJECTS=" + objectCount + "; TAGS=" + tagCount + "; ISSUES=" + issueCount);
            }
            return issueCount == 0 ? 0 : 3;
        }
        catch (Exception e) { Console.Error.WriteLine("STATUS=FAILED_OR_INCOMPLETE; " + e.GetType().Name + ": " + e.Message); return 1; }
    }

    static void LoadPriorTargets()
    {
        if (!File.Exists(PriorInventory)) throw new InvalidOperationException("Required preceding native inventory missing");
        foreach (string line in File.ReadLines(PriorInventory).Skip(1))
        {
            string[] p = line.Split('\t');
            if (p.Length < 4) throw new InvalidOperationException("Malformed prior inventory");
            string key = p[0] + "/" + p[1];
            if (p[2] == "EventHandlers") eventTargets.Add(key);
            if (p[2] == "Dynamizations") dynamicTargets.Add(key);
        }
        Console.WriteLine("TARGETS_EVENTS=" + eventTargets.Count + "; TARGETS_DYNAMIZATIONS=" + dynamicTargets.Count);
    }

    static List<HmiSoftware> FindNamedHmi(Project project)
    {
        // HMI_1 is the operator-visible target. Inspect names at the first level
        // rather than recursively querying SoftwareContainer on every drive/module.
        List<Device> devices = project.Devices.Concat(project.UngroupedDevicesGroup.Devices).ToList();
        List<Device> namedDevices = devices.Where(d => d.Name.Equals("HMI_1", StringComparison.OrdinalIgnoreCase)).ToList();
        List<HmiSoftware> result = new List<HmiSoftware>();
        if (namedDevices.Count > 1) throw new InvalidOperationException("Ambiguous HMI_1 device");
        if (namedDevices.Count == 1) { FindHmi(namedDevices[0].DeviceItems, result); return result; }
        List<DeviceItem> namedItems = new List<DeviceItem>();
        foreach (Device device in devices)
        {
            Console.WriteLine("DEVICE_NAME=" + device.Name);
            foreach (DeviceItem item in device.DeviceItems)
                if (item.Name.Equals("HMI_1", StringComparison.OrdinalIgnoreCase)) namedItems.Add(item);
        }
        if (namedItems.Count != 1) throw new InvalidOperationException("Expected unique top-level HMI_1 item; found " + namedItems.Count + ". Inspect device tree; do not fall back to broad recursive scan.");
        DeviceItem target = namedItems[0];
        SoftwareContainer c = target.GetService<SoftwareContainer>();
        if (c != null && c.Software is HmiSoftware) result.Add((HmiSoftware)c.Software);
        FindHmi(target.DeviceItems, result);
        return result;
    }

    static void FindHmi(DeviceItemComposition items, List<HmiSoftware> found)
    {
        foreach (DeviceItem item in items)
        {
            SoftwareContainer c = item.GetService<SoftwareContainer>();
            if (c != null && c.Software is HmiSoftware) found.Add((HmiSoftware)c.Software);
            FindHmi(item.DeviceItems, found);
        }
    }
    static void WalkScreens(object owner, string parent, ExclusiveAccess access)
    {
        if (!visited.Add(owner)) return;
        foreach (object screen in Collection(owner, "Screens", parent))
        {
            if (access.IsCancellationRequested) throw new OperationCanceledException();
            string name = parent + Scalar(Get(screen, "Name", parent));
            Console.WriteLine("SCREEN_BEGIN=" + name + "; AT=" + DateTimeOffset.Now.ToString("o"));
            screenCount++;
            objects.WriteLine(Clean(name) + "\t[screen]\t" + screen.GetType().Name + "\t" + Clean(Describe(screen, name, 0)));
            DescribeEntries(screen, "EventHandlers", name, "[screen]");
            DescribeEntries(screen, "PropertyEventHandlers", name, "[screen]");
            DescribeEntries(screen, "Dynamizations", name, "[screen]");
            foreach (object item in Collection(screen, "ScreenItems", name)) WalkItem(item, name, "", access);
            Console.WriteLine("SCREEN_END=" + name + "; OBJECTS_TOTAL=" + objectCount + "; ISSUES=" + issueCount);
        }
        foreach (string groupProperty in new[] { "ScreenGroups", "Groups" })
            foreach (object group in Collection(owner, groupProperty, parent)) WalkScreens(group, parent + Scalar(Get(group, "Name", parent)) + "/", access);
    }
    static void WalkItem(object item, string screen, string parent, ExclusiveAccess access)
    {
        if (!visited.Add(item)) return;
        if (access.IsCancellationRequested) throw new OperationCanceledException();
        string path = parent + Scalar(Get(item, "Name", screen));
        objectCount++;
        string key = Clean(screen) + "/" + Clean(path);
        // The first inventory already contains geometry, text, static enable/visibility.
        // Only supplemental operator-rights and navigation fields are needed here.
        if (eventTargets.Contains(key) || item.GetType().Name.Contains("IOField") || item.GetType().Name.Contains("ScreenWindow"))
            objects.WriteLine(Clean(screen) + "\t" + Clean(path) + "\t" + item.GetType().Name + "\t" + Clean(Describe(item, path, 0)));
        if (eventTargets.Contains(key)) DescribeEntries(item, "EventHandlers", screen, path);
        DescribeEntries(item, "PropertyEventHandlers", screen, path);
        if (dynamicTargets.Contains(key)) DescribeEntries(item, "Dynamizations", screen, path);
        foreach (object child in Collection(item, "ScreenItems", path)) WalkItem(child, screen, path + "/", access);
        if (item.GetType().Name.IndexOf("Faceplate", StringComparison.OrdinalIgnoreCase) >= 0) Issue(screen + "/" + path, "Faceplate internals not expanded; separate type/interface audit required");
        if (item.GetType().Name.IndexOf("Group", StringComparison.OrdinalIgnoreCase) >= 0 && item.GetType().GetProperty("ScreenItems") == null) Issue(screen + "/" + path, "Group without supported ScreenItems composition; child coverage unverified");
    }
    static void WalkTags(object owner, string path, ExclusiveAccess access)
    {
        if (!visited.Add(owner)) return;
        foreach (object table in Collection(owner, "TagTables", path))
        {
            string tablePath = path + "/" + Scalar(Get(table, "Name", path));
            foreach (object tag in Collection(table, "Tags", tablePath))
            {
                if (access.IsCancellationRequested) throw new OperationCanceledException();
                WalkTag(tag, tablePath, access);
            }
        }
        foreach (string groupProperty in new[] { "TagTableGroups", "Groups" })
            foreach (object group in Collection(owner, groupProperty, path)) WalkTags(group, path + "/" + Scalar(Get(group, "Name", path)), access);
    }
    static void WalkTag(object tag, string parent, ExclusiveAccess access)
    {
        if (!visited.Add(tag)) return;
        if (access.IsCancellationRequested) throw new OperationCanceledException();
        string path = parent + "/" + Scalar(Get(tag, "Name", parent));
        tagCount++;
        if (tagCount % 100 == 0) Console.WriteLine("TAG_PROGRESS=" + tagCount);
        tags.WriteLine(Clean(path) + "\t" + tag.GetType().Name + "\t" + Clean(Describe(tag, path, 0)));
        foreach (object member in Collection(tag, "Members", path)) WalkTag(member, path, access);
    }
    static void ExportScriptModules(HmiSoftware hmi, string output, ExclusiveAccess access)
    {
        string root = Path.Combine(output, "global-scripts");
        Directory.CreateDirectory(root);
        using (StreamWriter index = Writer(output, "global-script-index.tsv"))
        {
            index.WriteLine("Module\tExportDirectory\tStatus");
            int i = 0;
            foreach (var module in hmi.Scripts)
            {
                if (access.IsCancellationRequested) throw new OperationCanceledException();
                string target = Path.Combine(root, (++i).ToString("D4"));
                Directory.CreateDirectory(target);
                try
                {
                    module.Export(new DirectoryInfo(target));
                    index.WriteLine(Clean(module.Name) + "\t" + Clean(target) + "\tEXPORTED_NOT_TESTED");
                }
                catch (Exception e)
                {
                    Issue(module.Name, "Global script export failed: " + e.GetType().Name);
                    index.WriteLine(Clean(module.Name) + "\t" + Clean(target) + "\tFAILED");
                }
            }
        }
    }
    static void DescribeEntries(object owner, string property, string screen, string path)
    {
        foreach (object entry in Collection(owner, property, path))
            details.WriteLine(Clean(screen) + "\t" + Clean(path) + "\t" + property + "\t" + Clean((property == "Dynamizations" ? "PropertyName=" + Scalar(Get(entry, "PropertyName", path)) + "; " : "") + Describe(entry, path, 0)));
    }
    static string Describe(object value, string path, int depth, bool eventScript = false)
    {
        if (value == null) return "";
        if (value is string || value.GetType().IsValueType) return Scalar(value);
        if (depth > 4) { Issue(path, "Description depth exceeded"); return "[DEPTH_LIMIT]"; }
        var parts = new List<string>();
        parts.Add("Type=" + value.GetType().Name);
        foreach (string prop in FieldsFor(value))
        {
            object v = Get(value, prop, path);
            if (v == null) continue;
            if (v is string || v.GetType().IsValueType) parts.Add(prop + "=" + Scalar(v));
            else if (prop == "Text" || prop == "AlternateText")
            {
                bool any = false;
                foreach (object text in Collection(v, "Items", path)) { any = true; parts.Add(prop + "[" + Scalar(Get(text, "Language", path)) + "]=" + Scalar(Get(text, "Text", path))); }
                if (!any) { parts.Add(prop + "=" + Scalar(v)); Issue(path, "Unexpanded non-scalar " + prop); }
            }
            else if (prop == "RequiredFunctionRights" && v is IEnumerable)
                parts.Add(prop + "=[" + String.Join(",", ((IEnumerable)v).Cast<object>().Select(Scalar)) + "]");
            else parts.Add(prop + "=" + Scalar(v));
        }
        foreach (string single in new[] { "Script", "Trigger", "ValueConverter", "MappingTable" })
        {
            if (eventScript && single == "Trigger") continue;
            object child = Get(value, single, path);
            if (child != null) parts.Add(single + "={" + Describe(child, path, depth + 1, single == "Script") + "}");
        }
        // Explicit known metadata compositions only: no reflection invocation of arbitrary methods.
        foreach (string composition in new[] { "Entries", "Ranges", "Triggers", "Parameters", "Tags" })
            foreach (object child in Collection(value, composition, path)) parts.Add(composition + "={" + Describe(child, path, depth + 1) + "}");
        return String.Join("; ", parts);
    }
    static IEnumerable<string> FieldsFor(object value)
    {
        string type = value.GetType().Name;
        // This supplements the first geometry inventory. Read only properties
        // pertinent to each metadata kind to reduce remote getter traffic.
        if (type == "HmiTag" || type.Contains("HmiTagMember"))
            return new[] { "Name", "PlcTag", "PlcName", "Address", "Connection", "DataType", "TagType", "AccessMode", "Scope", "AcquisitionMode", "AcquisitionCycle", "LinearScaling", "HmiStartValue", "HmiEndValue", "PlcStartValue", "PlcEndValue", "RequiredFunctionRights" };
        if (type == "HmiScreen") return new[] { "Name", "Enabled", "ScreenNumber" };
        if (type == "PropertyEventHandler") return new[] { "EventType", "PropertyName" };
        if (type.Contains("EventHandler")) return new[] { "EventType" };
        if (type.Contains("Script")) return new[] { "ScriptCode", "GlobalDefinitionAreaScriptCode", "Async" };
        if (type == "Trigger") return new[] { "Type", "CustomDuration" };
        if (type.Contains("MappingTableEntry")) return new[] { "Condition", "Relevant", "BitDynamizationType", "From", "To", "RangeType", "Value", "AlternateValue", "Flashing", "FlashingRate" };
        if (type == "MappingTable") return new[] { "ConditionType" };
        if (type == "ValueConverter") return new string[0];
        if (type.Contains("Dynamization")) return new[] { "PropertyName", "DynamizationType", "Tag", "PlcTag", "Address", "DataType", "ReadOnly", "UseIndirectAddressing", "ResourceList" };
        if (value is Siemens.Engineering.HmiUnified.UI.Base.HmiScreenItemBase)
            return new[] { "Authorization", "ScreenName", "ScreenNumber" };
        return Properties;
    }
    static IEnumerable<object> Collection(object value, string property, string path)
    {
        object raw = Get(value, property, path);
        if (raw == null) return new object[0];
        IEnumerable list = raw as IEnumerable;
        if (list == null) { Issue(path, property + " is not enumerable"); return new object[0]; }
        try { return list.Cast<object>().ToArray(); }
        catch (Exception e) { Issue(path, property + " enumeration failed: " + e.GetType().Name); return new object[0]; }
    }
    static object Get(object value, string property, string path)
    {
        if (value == null) return null;
        string key = value.GetType().FullName + ":" + property;
        PropertyInfo p;
        if (!propertyCache.TryGetValue(key, out p))
        {
            p = value.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.Instance);
            propertyCache[key] = p;
        }
        if (p == null || !p.CanRead || p.GetIndexParameters().Length != 0) return null;
        try { return p.GetValue(value, null); }
        catch (Exception e) { Issue(path, property + " unreadable: " + e.GetType().Name); return null; }
    }
    static void Issue(string path, string message) { issueCount++; if (issues != null) issues.WriteLine(Clean(path) + "\t" + Clean(message)); }
    static string Scalar(object o) { return o == null ? "" : o.ToString(); }
    static string Clean(object o) { return Scalar(o).Replace("\\", "\\\\").Replace("\t", "\\t").Replace("\r", "\\r").Replace("\n", "\\n"); }
    static StreamWriter Writer(string root, string name) { StreamWriter w = new StreamWriter(Path.Combine(root, name), false, new UTF8Encoding(false)); w.AutoFlush = true; return w; }
}
