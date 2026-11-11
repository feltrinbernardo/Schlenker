using System;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.HmiUnified.HmiAlarm;
using Siemens.Engineering.SW;

namespace Schlenker.TiaV19
{
    internal static class AuditAirPressureAlarm
    {
        private static int Main(string[] args)
        {
            string expected = Path.GetFullPath(args[0]);
            TiaPortalProcess process = TiaPortal.GetProcesses().Single(p => p.ProjectPath != null &&
                Path.GetFullPath(p.ProjectPath.FullName).Equals(expected, StringComparison.OrdinalIgnoreCase));
            using (TiaPortal portal = process.Attach())
            {
                Project project = portal.Projects.Single();
                HmiSoftware hmi = null;
                foreach (Device device in project.Devices.Concat(project.UngroupedDevicesGroup.Devices))
                    FindHmi(device.DeviceItems, ref hmi);
                foreach (HmiDiscreteAlarm alarm in hmi.DiscreteAlarms)
                {
                    string text = alarm.EventText1.ToString();
                    if (alarm.Name.IndexOf("Air", StringComparison.OrdinalIgnoreCase) < 0 &&
                        alarm.Name.IndexOf("Pressure", StringComparison.OrdinalIgnoreCase) < 0 &&
                        text.IndexOf("air", StringComparison.OrdinalIgnoreCase) < 0 &&
                        text.IndexOf("pressure", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    Console.WriteLine("ALARM=" + alarm.Name + ";ID=" + alarm.Id + ";TAG=" +
                        alarm.RaisedStateTag + ";BIT=" + alarm.RaisedStateTagBitNumber +
                        ";MODE=" + alarm.TriggerMode + ";TEXT=" + text);
                }
            }
            Console.WriteLine("STATUS=PASS");
            return 0;
        }

        private static void FindHmi(DeviceItemComposition items, ref HmiSoftware hmi)
        {
            foreach (DeviceItem item in items)
            {
                SoftwareContainer container = item.GetService<SoftwareContainer>();
                if (hmi == null && container != null) hmi = container.Software as HmiSoftware;
                FindHmi(item.DeviceItems, ref hmi);
            }
        }
    }
}
