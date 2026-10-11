using System;
using System.Linq;
using System.Reflection;
using Siemens.Engineering.HmiUnified.UI.Controls;
using Siemens.Engineering.HmiUnified.UI.Parts;
using Siemens.Engineering.HmiUnified.UI.Enum;

namespace Schlenker.TiaV19
{
    internal static class InspectHmiAlarmApi
    {
        private static void Main()
        {
            PrintType(typeof(HmiAlarmControl));
            foreach (PropertyInfo property in typeof(HmiAlarmControl).GetProperties())
            {
                if (property.Name.IndexOf("Alarm", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    property.Name.IndexOf("Filter", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    property.Name.IndexOf("View", StringComparison.OrdinalIgnoreCase) >= 0)
                    PrintType(property.PropertyType);
            }
            PrintType(typeof(HmiAlarmColumnPart));
            Console.WriteLine("ENUM HmiAlarmBlock=" +
                String.Join(",", Enum.GetNames(typeof(HmiAlarmBlock))));
            Console.WriteLine("ENUM HmiAlarmSourceType=" +
                String.Join(",", Enum.GetNames(typeof(HmiAlarmSourceType))));
            Console.WriteLine("ENUM HmiVisibleAlarms=" +
                String.Join(",", Enum.GetNames(typeof(HmiVisibleAlarms))));
        }

        private static void PrintType(Type type)
        {
            Console.WriteLine("TYPE=" + type.FullName);
            foreach (PropertyInfo property in type.GetProperties(
                BindingFlags.Public | BindingFlags.Instance))
                Console.WriteLine("  PROPERTY={0}; TYPE={1}; READ={2}; WRITE={3}",
                    property.Name, property.PropertyType.FullName,
                    property.CanRead, property.CanWrite);
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(item => item.Name.IndexOf("Filter", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               item.Name.IndexOf("Alarm", StringComparison.OrdinalIgnoreCase) >= 0))
                Console.WriteLine("  METHOD=" + method);
        }
    }
}
