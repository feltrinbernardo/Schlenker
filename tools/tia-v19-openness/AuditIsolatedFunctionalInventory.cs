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
internal static class AuditIsolatedFunctionalInventory
{
    const string AllowedProject = @"C:\www\Schlenker\runs\2026-11-11-rob-isolated-workspace\work-copy\Schlenker_Pendant_Offline.ap19";
    const string AllowedOutput = @"C:\www\Schlenker\runs\2026-11-12-offline-functional-audit";
    static StreamWriter objects, details, tags, issues;
    static int issueCount, objectCount, screenCount, tagCount;
    static HashSet<object> visited = new HashSet<object>();
    static readonly string[] Properties = { "Name", "Text", "AlternateText", "Visible", "Enabled", "Left", "Top", "Width", "Height", "ProcessValue", "IOFieldType", "ReadOnly", "PropertyName", "Tag", "UseIndirectAddressing", "EventType", "ScriptCode", "PlcTag", "Connection", "DataType", "AcquisitionMode", "AcquisitionCycle" };

    static int Main(string[] args)
    {
        try
        {
            if (args.Length != 1) throw new ArgumentException("Usage: AuditIsolatedFunctionalInventory <new-output-subdirectory-under-approved-run>");
            string output = Path.GetFullPath(args[0]);
            string prefix = Path.GetFullPath(AllowedOutput).TrimEnd('\\') + "\\";
            if (!output.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Output outside approved run");
            if (Directory.Exists(output) || File.Exists(output)) throw new InvalidOperationException("Output must be new; existing evidence is immutable");
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
                    List<HmiSoftware> hmis = new List<HmiSoftware>();
                    foreach (Device device in project.Devices.Concat(project.UngroupedDevicesGroup.Devices)) FindHmi(device.DeviceItems, hmis);
                    if (hmis.Count != 1) throw new InvalidOperationException("Expected one Unified HMI, found " + hmis.Count);
                    HmiSoftware hmi = hmis[0];
                    WalkScreens(hmi, "", exclusive);
                    visited.Clear();
                    WalkTags(hmi, "HMI", exclusive);
                    if (screenCount == 0 || objectCount == 0 || tagCount == 0) Issue("ROOT", "Empty required inventory");
                }
                File.WriteAllLines(Path.Combine(output, "manifest.txt"), new[] {
                    "Project=" + project.Path.FullName, "Timestamp=" + DateTimeOffset.Now.ToString("o"),
                    "Mode=READ_ONLY_ATTACHED_EXCLUSIVE", "Save=NO; Compile=NO; Download=NO; Online=NO",
                    "Screens=" + screenCount, "Objects=" + objectCount, "Tags=" + tagCount, "CoverageIssues=" + issueCount,
                    "Status=" + (issueCount == 0 ? "EXTRACTED_NOT_FUNCTIONALLY_TESTED" : "PARTIAL_EXTRACTION"),
                    "Scope=Screen objects, nested screen-item compositions, event and binding scripts, tag tables. Runtime behavior and external/global script implementation are not tested.",
                    "PLCExport=NOT_PERFORMED; Connections/security configuration not exported; no secret-valued attributes requested."
                }, new UTF8Encoding(false));
                Console.WriteLine("OUTPUT=" + output + "; SCREENS=" + screenCount + "; OBJECTS=" + objectCount + "; TAGS=" + tagCount + "; ISSUES=" + issueCount);
            }
            return issueCount == 0 ? 0 : 3;
        }
        catch (Exception e) { Console.Error.WriteLine("STATUS=FAILED_OR_INCOMPLETE; " + e.GetType().Name + ": " + e.Message); return 1; }
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
            screenCount++;
            DescribeEntries(screen, "EventHandlers", name, "[screen]");
            foreach (object item in Collection(screen, "ScreenItems", name)) WalkItem(item, name, "", access);
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
        objects.WriteLine(Clean(screen) + "\t" + Clean(path) + "\t" + item.GetType().Name + "\t" + Clean(Describe(item, path, 0)));
        DescribeEntries(item, "EventHandlers", screen, path);
        DescribeEntries(item, "Dynamizations", screen, path);
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
                if (!visited.Add(tag)) continue;
                tagCount++;
                tags.WriteLine(Clean(tablePath + "/" + Scalar(Get(tag, "Name", path))) + "\t" + tag.GetType().Name + "\t" + Clean(Describe(tag, tablePath, 0)));
            }
        }
        foreach (string groupProperty in new[] { "TagTableGroups", "Groups" })
            foreach (object group in Collection(owner, groupProperty, path)) WalkTags(group, path + "/" + Scalar(Get(group, "Name", path)), access);
    }
    static void DescribeEntries(object owner, string property, string screen, string path)
    {
        foreach (object entry in Collection(owner, property, path))
            details.WriteLine(Clean(screen) + "\t" + Clean(path) + "\t" + property + "\t" + Clean(Describe(entry, path, 0)));
    }
    static string Describe(object value, string path, int depth)
    {
        if (value == null) return "";
        if (depth > 4) { Issue(path, "Description depth exceeded"); return "[DEPTH_LIMIT]"; }
        var parts = new List<string>();
        parts.Add("Type=" + value.GetType().Name);
        foreach (string prop in Properties)
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
            else parts.Add(prop + "=" + Scalar(v));
        }
        object script = Get(value, "Script", path);
        if (script != null) parts.Add("Script={" + Describe(script, path, depth + 1) + "}");
        // Enumerate binding ranges and dynamic triggers, where exposed, without invoking methods.
        foreach (string composition in new[] { "Ranges", "Triggers", "Parameters" })
            foreach (object child in Collection(value, composition, path)) parts.Add(composition + "={" + Describe(child, path, depth + 1) + "}");
        return String.Join("; ", parts);
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
        PropertyInfo p = value.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.Instance);
        if (p == null || !p.CanRead || p.GetIndexParameters().Length != 0) return null;
        try { return p.GetValue(value, null); }
        catch (Exception e) { Issue(path, property + " unreadable: " + e.GetType().Name); return null; }
    }
    static void Issue(string path, string message) { issueCount++; if (issues != null) issues.WriteLine(Clean(path) + "\t" + Clean(message)); }
    static string Scalar(object o) { return o == null ? "" : o.ToString(); }
    static string Clean(object o) { return Scalar(o).Replace("\\", "\\\\").Replace("\t", "\\t").Replace("\r", "\\r").Replace("\n", "\\n"); }
    static StreamWriter Writer(string root, string name) { return new StreamWriter(Path.Combine(root, name), false, new UTF8Encoding(false)); }
}
