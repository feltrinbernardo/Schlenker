using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.HmiUnified.UI.Base;
using Siemens.Engineering.HmiUnified.UI.Enum;
using Siemens.Engineering.HmiUnified.UI.Events;
using Siemens.Engineering.HmiUnified.UI.Screens;
using Siemens.Engineering.HmiUnified.UI.Widgets;
using Siemens.Engineering.SW;

namespace Schlenker.TiaV19
{
    internal static class FixUnifiedMainWindowNavigation
    {
        private const int ExpectedNavigationCalls = 335;
        private const string MainScreenWindowPath = "/Main screen window_1";

        private static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("Usage: FixUnifiedMainWindowNavigation <exact-ap19-path>");
                return 2;
            }

            string projectPath = Path.GetFullPath(args[0]);
            string protectedProject = Path.GetFullPath(
                @"C:\TIA Projects\Schlenkers 36-10 190036-7-8v2.12\Backup Schlenkers 36-10 190036-7-8v2.12.ap19");

            try
            {
                if (projectPath.Equals(protectedProject, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Refusing the protected original project.");
                if (projectPath.IndexOf("-DeployWorking-", StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException("Refusing a project outside a DeployWorking path.");

                TiaPortalProcess[] matches = TiaPortal.GetProcesses()
                    .Where(candidate => candidate.ProjectPath != null &&
                        Path.GetFullPath(candidate.ProjectPath.FullName)
                            .Equals(projectPath, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                if (matches.Length != 1)
                    throw new InvalidOperationException(String.Format(
                        "Expected exactly one open working project, found {0}.", matches.Length));

                using (TiaPortal portal = matches[0].Attach())
                {
                    Project project = portal.Projects.Single();
                    HmiSoftware hmi = FindUnifiedHmi(project);
                    if (hmi == null)
                        throw new InvalidOperationException("WinCC Unified HMI software was not found.");

                    int oldPathCalls = 0;
                    int changedHandlers = 0;
                    int changedScreens = 0;
                    Dictionary<string, int> changesByScreen =
                        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                    foreach (HmiScreen screen in hmi.Screens)
                    {
                        int screenChanges = 0;
                        foreach (HmiButton button in screen.ScreenItems.OfType<HmiButton>())
                        {
                            foreach (HmiButtonEventType eventType in
                                Enum.GetValues(typeof(HmiButtonEventType)))
                            {
                                HmiButtonEventHandler handler = button.EventHandlers.Find(eventType);
                                if (handler == null || handler.Script == null)
                                    continue;

                                string script = handler.Script.ScriptCode ?? String.Empty;
                                if (script.IndexOf("HMIRuntime.UI.SysFct.ChangeScreen", StringComparison.Ordinal) < 0)
                                    continue;

                                int handlerOldCalls = CountOccurrences(script, ", \".\")") +
                                    CountOccurrences(script, ",\".\")") +
                                    CountOccurrences(script, ", \"/\")") +
                                    CountOccurrences(script, ",\"/\")");
                                if (handlerOldCalls == 0)
                                    continue;

                                string updated = script
                                    .Replace(", \".\")", ", \"" + MainScreenWindowPath + "\")")
                                    .Replace(",\".\")", ",\"" + MainScreenWindowPath + "\")")
                                    .Replace(", \"/\")", ", \"" + MainScreenWindowPath + "\")")
                                    .Replace(",\"/\")", ",\"" + MainScreenWindowPath + "\")");
                                handler.Script.ScriptCode = updated;
                                oldPathCalls += handlerOldCalls;
                                changedHandlers++;
                                screenChanges += handlerOldCalls;
                            }
                        }

                        if (screenChanges > 0)
                        {
                            changedScreens++;
                            changesByScreen.Add(screen.Name, screenChanges);
                        }
                    }

                    if (oldPathCalls != ExpectedNavigationCalls)
                        throw new InvalidOperationException(String.Format(
                            "Expected {0} old navigation calls, found {1}; the project was not saved.",
                            ExpectedNavigationCalls, oldPathCalls));

                    int remainingOldCalls = 0;
                    int correctedCalls = 0;
                    foreach (HmiScreen screen in hmi.Screens)
                    {
                        foreach (HmiButton button in screen.ScreenItems.OfType<HmiButton>())
                        {
                            foreach (HmiButtonEventType eventType in
                                Enum.GetValues(typeof(HmiButtonEventType)))
                            {
                                HmiButtonEventHandler handler = button.EventHandlers.Find(eventType);
                                if (handler == null || handler.Script == null)
                                    continue;
                                string script = handler.Script.ScriptCode ?? String.Empty;
                                if (script.IndexOf("HMIRuntime.UI.SysFct.ChangeScreen", StringComparison.Ordinal) < 0)
                                    continue;
                                remainingOldCalls += CountOccurrences(script, ", \".\")") +
                                    CountOccurrences(script, ",\".\")") +
                                    CountOccurrences(script, ", \"/\")") +
                                    CountOccurrences(script, ",\"/\")");
                                correctedCalls += CountOccurrences(
                                    script, ", \"" + MainScreenWindowPath + "\")") +
                                    CountOccurrences(
                                        script, ",\"" + MainScreenWindowPath + "\")");
                            }
                        }
                    }

                    if (remainingOldCalls != 0 || correctedCalls != ExpectedNavigationCalls)
                        throw new InvalidOperationException(String.Format(
                            "Verification failed: old={0}, corrected={1}; the project was not saved.",
                            remainingOldCalls, correctedCalls));

                    project.Save();

                    Console.WriteLine("PROJECT=" + project.Path.FullName);
                    Console.WriteLine("HMI=" + hmi.Name);
                    Console.WriteLine("CHANGED_SCREENS=" + changedScreens);
                    Console.WriteLine("CHANGED_HANDLERS=" + changedHandlers);
                    Console.WriteLine("CORRECTED_CALLS=" + correctedCalls);
                    Console.WriteLine("REMAINING_OLD_CALLS=" + remainingOldCalls);
                    foreach (KeyValuePair<string, int> entry in changesByScreen)
                        Console.WriteLine("SCREEN=" + entry.Key + " CALLS=" + entry.Value);
                    Console.WriteLine("STATUS=PASS");
                    return 0;
                }
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("STATUS=FAIL");
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static int CountOccurrences(string value, string fragment)
        {
            int count = 0;
            int position = 0;
            while ((position = value.IndexOf(fragment, position, StringComparison.Ordinal)) >= 0)
            {
                count++;
                position += fragment.Length;
            }
            return count;
        }

        private static HmiSoftware FindUnifiedHmi(Project project)
        {
            foreach (Device device in project.Devices)
            {
                HmiSoftware hmi = FindInItems(device.DeviceItems);
                if (hmi != null)
                    return hmi;
            }
            return null;
        }

        private static HmiSoftware FindInItems(DeviceItemComposition items)
        {
            foreach (DeviceItem item in items)
            {
                SoftwareContainer container = item.GetService<SoftwareContainer>();
                HmiSoftware hmi = container == null ? null : container.Software as HmiSoftware;
                if (hmi != null)
                    return hmi;
                HmiSoftware nested = FindInItems(item.DeviceItems);
                if (nested != null)
                    return nested;
            }
            return null;
        }
    }
}
