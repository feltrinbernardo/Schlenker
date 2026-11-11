using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.Library;
using Siemens.Engineering.Library.MasterCopies;
using Siemens.Engineering.MC.Drives;

namespace Schlenker.TiaV19
{
    internal static class IntegrateCapDistributorDriveOffline
    {
        private const string SourceDeviceName = "SINAMICS G_1";
        private const string TargetDeviceName = "G120C_CAP_DISTRIBUTOR";
        private const string PreviousDisplayDeviceName = "G120C_CapDistributor";
        private const string LegacyTargetDeviceName = "VSD_CAP_DISTRIBUTOR";
        private const string TargetItemName = "VSD_CAP_DISTRIBUTOR";
        private const string PnName = "vsd-cap-distributor";
        private const string IpAddress = "192.168.10.43";
        private const int InputStart = 320;
        private const int OutputStart = 152;

        private static int Main(string[] args)
        {
            if (args.Length == 2 &&
                args[0].Equals("rename-open", StringComparison.OrdinalIgnoreCase))
                return RenameOpenTarget(args[1]);
            if (args.Length == 2 &&
                args[0].Equals("inspect-open", StringComparison.OrdinalIgnoreCase))
                return InspectOpenTarget(args[1]);
            if (args.Length == 3 &&
                args[0].Equals("apply-library", StringComparison.OrdinalIgnoreCase))
                return ApplyLibraryToOpenProject(args[1], args[2]);
            if (args.Length == 2 &&
                args[0].Equals("inspect", StringComparison.OrdinalIgnoreCase))
                return InspectTarget(args[1]);
            if (args.Length != 3)
            {
                Console.Error.WriteLine(
                    "Usage: IntegrateCapDistributorDriveOffline <source-ap19> <target-ap19> <library-directory>");
                return 2;
            }

            string sourcePath = Path.GetFullPath(args[0]);
            string targetPath = Path.GetFullPath(args[1]);
            DirectoryInfo libraryDirectory = new DirectoryInfo(Path.GetFullPath(args[2]));
            if (sourcePath.Equals(targetPath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Source and target projects must differ.");
            libraryDirectory.Create();

            try
            {
                using (TiaPortal portal = new TiaPortal(TiaPortalMode.WithoutUserInterface))
                {
                    UserGlobalLibrary library = portal.GlobalLibraries.Create<UserGlobalLibrary>(
                        libraryDirectory, "G120C_CapDistributor_Transfer");
                    MasterCopy masterCopy;

                    Project source = portal.Projects.Open(new FileInfo(sourcePath));
                    try
                    {
                        Device sourceDevice = AllDevices(source).Single(device =>
                            device.Name.Equals(SourceDeviceName, StringComparison.OrdinalIgnoreCase));
                        if (!FindItems(sourceDevice.DeviceItems).Any(item =>
                                item.Name.Equals(TargetItemName, StringComparison.OrdinalIgnoreCase)))
                            throw new InvalidOperationException(
                                "The commissioned source drive item was not found.");
                        masterCopy = library.MasterCopyFolder.MasterCopies.Create(sourceDevice);
                        library.Save();
                        Console.WriteLine("SOURCE_DEVICE=" + sourceDevice.Name);
                        Console.WriteLine("MASTER_COPY_CREATED=YES");
                    }
                    finally
                    {
                        source.Close();
                    }

                    Project target = portal.Projects.Open(new FileInfo(targetPath));
                    try
                    {
                        List<Device> existing = AllDevices(target).ToList();
                        Subnet subnet = FindSubnet(existing, "PN/IE_1");
                        if (subnet == null)
                            throw new InvalidOperationException("PROFINET subnet PN/IE_1 was not found.");
                        IoSystem ioSystem = FindIoSystem(existing, "PROFINET IO-System_1");
                        if (ioSystem == null)
                            throw new InvalidOperationException(
                                "PROFINET IO-System_1 was not found.");

                        Device replaceable = existing.SingleOrDefault(device =>
                            device.Name.Equals(TargetDeviceName,
                                StringComparison.OrdinalIgnoreCase));
                        if (replaceable != null)
                        {
                            ValidateReplaceableGsd(replaceable);
                            replaceable.Delete();
                            existing.Remove(replaceable);
                            Console.WriteLine("REPLACED_EXISTING_GSD=YES");
                        }

                        EnsureIdentityIsFree(existing);
                        EnsureAddressRangesAreFree(existing);

                        Device created = target.UngroupedDevicesGroup.Devices.CreateFrom(masterCopy);
                        ((IEngineeringObject)created).SetAttribute("Name", TargetDeviceName);

                        Node node = FindNodes(created.DeviceItems).Single();
                        IEngineeringObject attributes = node;
                        attributes.SetAttribute("Address", IpAddress);
                        attributes.SetAttribute("SubnetMask", "255.255.255.0");
                        TrySetAttribute(attributes, "PnDeviceNameAutoGeneration", false);
                        attributes.SetAttribute("PnDeviceName", PnName);
                        if (node.ConnectedSubnet == null)
                            node.ConnectToSubnet(subnet);
                        else if (!node.ConnectedSubnet.Name.Equals(subnet.Name,
                                     StringComparison.OrdinalIgnoreCase))
                            throw new InvalidOperationException(
                                "Transferred drive is connected to an unexpected subnet.");

                        NetworkInterface network = FindNetworks(created.DeviceItems).Single();
                        if (network.InterfaceOperatingMode != InterfaceOperatingModes.IoDevice)
                            network.InterfaceOperatingMode = InterfaceOperatingModes.IoDevice;
                        IoConnector connector = network.IoConnectors.First();
                        if (connector.ConnectedToIoSystem == null)
                            connector.ConnectToIoSystem(ioSystem);
                        else if (!connector.ConnectedToIoSystem.Name.Equals(ioSystem.Name,
                                     StringComparison.OrdinalIgnoreCase))
                            throw new InvalidOperationException(
                                "Transferred drive is connected to an unexpected IO system.");

                        DriveObjectContainer driveContainer = FindItems(created.DeviceItems)
                            .Select(item => item.GetService<DriveObjectContainer>())
                            .Single(item => item != null);
                        DriveObject driveObject = driveContainer.DriveObjects.Single();
                        Telegram telegram = driveObject.Telegrams.FirstOrDefault(item =>
                            item.TelegramNumber == 1);
                        if (telegram == null)
                        {
                            if (!driveObject.Telegrams.CanInsertMainTelegram(1))
                                throw new InvalidOperationException(
                                    "Startdrive does not accept Standard telegram 1.");
                            driveObject.Telegrams.InsertMainTelegram(1);
                            telegram = driveObject.Telegrams.Single(item =>
                                item.TelegramNumber == 1);
                        }
                        Address input = telegram.Addresses.Single(address =>
                            address.IoType.ToString().Equals("Input",
                                StringComparison.OrdinalIgnoreCase));
                        Address output = telegram.Addresses.Single(address =>
                            address.IoType.ToString().Equals("Output",
                                StringComparison.OrdinalIgnoreCase));
                        if (input.Length != 32 || output.Length != 32)
                            throw new InvalidOperationException(
                                "Startdrive Telegram 1 is not 2 PZD words in each direction.");
                        input.StartAddress = InputStart;
                        output.StartAddress = OutputStart;

                        Validate(created, node, connector, input, output);
                        target.Save();
                        Console.WriteLine("TARGET_PROJECT=" + target.Path.FullName);
                        Console.WriteLine("CREATED_DEVICE=" + created.Name);
                        Console.WriteLine("PN_NAME=" + attributes.GetAttribute("PnDeviceName"));
                        Console.WriteLine("IP_ADDRESS=" + attributes.GetAttribute("Address"));
                        Console.WriteLine("SUBNET=" + node.ConnectedSubnet.Name);
                        Console.WriteLine("IO_SYSTEM=" + connector.ConnectedToIoSystem.Name);
                        Console.WriteLine("INPUT=I{0}..I{1}", input.StartAddress,
                            input.StartAddress + 3);
                        Console.WriteLine("OUTPUT=Q{0}..Q{1}", output.StartAddress,
                            output.StartAddress + 3);
                        Console.WriteLine("SAVE=YES; ONLINE=NO; DOWNLOAD=NO");
                    }
                    finally
                    {
                        target.Close();
                        library.Close();
                    }
                }

                Console.WriteLine("STATUS=PASS_APPLIED");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("STATUS=FAIL");
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static int RenameOpenTarget(string projectPath)
        {
            try
            {
                string exactProjectPath = Path.GetFullPath(projectPath);
                TiaPortalProcess process = TiaPortal.GetProcesses().SingleOrDefault(candidate =>
                    candidate.ProjectPath != null &&
                    Path.GetFullPath(candidate.ProjectPath.FullName).Equals(exactProjectPath,
                        StringComparison.OrdinalIgnoreCase));
                if (process == null) throw new InvalidOperationException("Exact open project not found.");
                using (TiaPortal portal = process.Attach())
                {
                    Project project = portal.Projects.Single();
                    Device device = AllDevices(project).Single(item =>
                        item.Name.Equals(LegacyTargetDeviceName, StringComparison.OrdinalIgnoreCase) ||
                        item.Name.Equals(PreviousDisplayDeviceName, StringComparison.Ordinal) ||
                        item.Name.Equals(TargetDeviceName, StringComparison.OrdinalIgnoreCase));
                    ValidateReplaceableGsd(device);
                    ((IEngineeringObject)device).SetAttribute("Name", TargetDeviceName);
                    DeviceItem visibleItem = FindItems(device.DeviceItems).SingleOrDefault(item =>
                        (item.Name.Equals(TargetItemName, StringComparison.OrdinalIgnoreCase) ||
                         item.Name.Equals(PreviousDisplayDeviceName, StringComparison.Ordinal) ||
                         item.Name.Equals(TargetDeviceName, StringComparison.OrdinalIgnoreCase)) &&
                        (item.TypeIdentifier ?? String.Empty).StartsWith("OrderNumber:6SL3210-1KE11-8AF1",
                            StringComparison.OrdinalIgnoreCase));
                    if (visibleItem == null)
                        throw new InvalidOperationException("Visible Startdrive device item was not found.");
                    ((IEngineeringObject)visibleItem).SetAttribute("Name", TargetDeviceName);
                    project.Save();
                    Console.WriteLine("RENAMED_DEVICE=" + device.Name);
                    Console.WriteLine("RENAMED_VISIBLE_ITEM=" + visibleItem.Name);
                    Console.WriteLine("PN_NAME_UNCHANGED=" + PnName);
                    Console.WriteLine("IP_ADDRESS_UNCHANGED=" + IpAddress);
                }
                Console.WriteLine("STATUS=PASS_RENAMED_OPEN_PROJECT");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("STATUS=FAIL");
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static int InspectOpenTarget(string projectPath)
        {
            try
            {
                string exactProjectPath = Path.GetFullPath(projectPath);
                TiaPortalProcess process = TiaPortal.GetProcesses().SingleOrDefault(candidate =>
                    candidate.ProjectPath != null &&
                    Path.GetFullPath(candidate.ProjectPath.FullName).Equals(exactProjectPath,
                        StringComparison.OrdinalIgnoreCase));
                if (process == null) throw new InvalidOperationException("Exact open project not found.");
                using (TiaPortal portal = process.Attach())
                {
                    Project project = portal.Projects.Single();
                    int index = 0;
                    foreach (Device device in project.Devices)
                    {
                        DeviceItem firstItem = device.DeviceItems.FirstOrDefault();
                        Console.WriteLine("ROOT_DEVICE[{0}]={1}; FIRST_ITEM={2}", index++,
                            device.Name, firstItem == null ? "<none>" : firstItem.Name);
                        IEngineeringObject deviceObject = device;
                        foreach (EngineeringAttributeInfo info in deviceObject.GetAttributeInfos())
                            if (info.Name.IndexOf("position", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                info.Name.IndexOf("order", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                info.Name.IndexOf("number", StringComparison.OrdinalIgnoreCase) >= 0)
                                try { Console.WriteLine("  DEVICE_ATTR=" + info.Name + "; VALUE=" +
                                    deviceObject.GetAttribute(info.Name)); } catch { }
                    }
                    index = 0;
                    foreach (Device device in project.UngroupedDevicesGroup.Devices)
                        Console.WriteLine("UNGROUPED_DEVICE[{0}]={1}", index++, device.Name);
                    Device target = AllDevices(project).Single(device =>
                        device.Name.Equals(TargetDeviceName, StringComparison.OrdinalIgnoreCase));
                    ValidateReplaceableGsd(target);
                    foreach (DeviceItem item in FindItems(target.DeviceItems))
                        Console.WriteLine("TARGET_ITEM=" + item.Name + "; TYPE=" + item.TypeIdentifier);
                    Console.WriteLine("TARGET_VALIDATED=YES; IS_GSD=" + target.IsGsd);
                }
                Console.WriteLine("STATUS=PASS_INSPECT_OPEN");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("STATUS=FAIL");
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static int ApplyLibraryToOpenProject(string projectPath, string libraryPath)
        {
            try
            {
                string exactProjectPath = Path.GetFullPath(projectPath);
                TiaPortalProcess process = TiaPortal.GetProcesses().SingleOrDefault(candidate =>
                    candidate.ProjectPath != null &&
                    Path.GetFullPath(candidate.ProjectPath.FullName).Equals(exactProjectPath,
                        StringComparison.OrdinalIgnoreCase));
                if (process == null)
                    throw new InvalidOperationException(
                        "The exact target project is not open in one unique TIA Portal process.");

                using (TiaPortal portal = process.Attach())
                {
                    Project target = portal.Projects.Single();
                    if (!Path.GetFullPath(target.Path.FullName).Equals(exactProjectPath,
                            StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Attached project identity changed.");

                    UserGlobalLibrary library = portal.GlobalLibraries.Open(
                        new FileInfo(Path.GetFullPath(libraryPath)), OpenMode.ReadOnly);
                    try
                    {
                        MasterCopy masterCopy = library.MasterCopyFolder.MasterCopies.Single();
                        ApplyToTarget(target, masterCopy);
                    }
                    finally { library.Close(); }
                }
                Console.WriteLine("STATUS=PASS_APPLIED_TO_OPEN_PROJECT");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("STATUS=FAIL");
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static void ApplyToTarget(Project target, MasterCopy masterCopy)
        {
            List<Device> existing = AllDevices(target).ToList();
            Subnet subnet = FindSubnet(existing, "PN/IE_1");
            if (subnet == null)
                throw new InvalidOperationException("PROFINET subnet PN/IE_1 was not found.");
            IoSystem ioSystem = FindIoSystem(existing, "PROFINET IO-System_1");
            if (ioSystem == null)
                throw new InvalidOperationException("PROFINET IO-System_1 was not found.");

            Device replaceable = existing.SingleOrDefault(device =>
                device.Name.Equals(TargetDeviceName, StringComparison.OrdinalIgnoreCase) ||
                device.Name.Equals(PreviousDisplayDeviceName, StringComparison.Ordinal) ||
                device.Name.Equals(LegacyTargetDeviceName, StringComparison.OrdinalIgnoreCase));
            if (replaceable != null)
            {
                ValidateReplaceableGsd(replaceable);
                replaceable.Delete();
                existing.Remove(replaceable);
                Console.WriteLine("REPLACED_EXISTING_DEVICE=YES");
            }

            EnsureIdentityIsFree(existing);
            EnsureAddressRangesAreFree(existing);

            Device created = target.Devices.CreateFrom(masterCopy);
            ((IEngineeringObject)created).SetAttribute("Name", TargetDeviceName);

            Node node = FindNodes(created.DeviceItems).Single();
            IEngineeringObject attributes = node;
            attributes.SetAttribute("Address", IpAddress);
            attributes.SetAttribute("SubnetMask", "255.255.255.0");
            TrySetAttribute(attributes, "PnDeviceNameAutoGeneration", false);
            attributes.SetAttribute("PnDeviceName", PnName);
            if (node.ConnectedSubnet == null) node.ConnectToSubnet(subnet);
            else if (!node.ConnectedSubnet.Name.Equals(subnet.Name,
                         StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "Transferred drive is connected to an unexpected subnet.");

            NetworkInterface network = FindNetworks(created.DeviceItems).Single();
            if (network.InterfaceOperatingMode != InterfaceOperatingModes.IoDevice)
                network.InterfaceOperatingMode = InterfaceOperatingModes.IoDevice;
            IoConnector connector = network.IoConnectors.First();
            if (connector.ConnectedToIoSystem == null) connector.ConnectToIoSystem(ioSystem);
            else if (!connector.ConnectedToIoSystem.Name.Equals(ioSystem.Name,
                         StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "Transferred drive is connected to an unexpected IO system.");

            DriveObjectContainer driveContainer = FindItems(created.DeviceItems)
                .Select(item => item.GetService<DriveObjectContainer>()).Single(item => item != null);
            DriveObject driveObject = driveContainer.DriveObjects.Single();
            Telegram telegram = driveObject.Telegrams.FirstOrDefault(item => item.TelegramNumber == 1);
            if (telegram == null)
            {
                if (!driveObject.Telegrams.CanInsertMainTelegram(1))
                    throw new InvalidOperationException("Startdrive does not accept Standard telegram 1.");
                driveObject.Telegrams.InsertMainTelegram(1);
                telegram = driveObject.Telegrams.Single(item => item.TelegramNumber == 1);
            }
            Address input = telegram.Addresses.Single(address =>
                address.IoType.ToString().Equals("Input", StringComparison.OrdinalIgnoreCase));
            Address output = telegram.Addresses.Single(address =>
                address.IoType.ToString().Equals("Output", StringComparison.OrdinalIgnoreCase));
            if (input.Length != 32 || output.Length != 32)
                throw new InvalidOperationException("Startdrive Telegram 1 is not 2 PZD words in each direction.");
            input.StartAddress = InputStart;
            output.StartAddress = OutputStart;

            Validate(created, node, connector, input, output);
            target.Save();
            Console.WriteLine("TARGET_PROJECT=" + target.Path.FullName);
            Console.WriteLine("CREATED_DEVICE=" + created.Name);
            Console.WriteLine("DEVICE_GROUP=PROJECT_ROOT_STARTDRIVE");
            Console.WriteLine("PN_NAME=" + attributes.GetAttribute("PnDeviceName"));
            Console.WriteLine("IP_ADDRESS=" + attributes.GetAttribute("Address"));
            Console.WriteLine("SUBNET=" + node.ConnectedSubnet.Name);
            Console.WriteLine("IO_SYSTEM=" + connector.ConnectedToIoSystem.Name);
            Console.WriteLine("INPUT=I{0}..I{1}", input.StartAddress, input.StartAddress + 3);
            Console.WriteLine("OUTPUT=Q{0}..Q{1}", output.StartAddress, output.StartAddress + 3);
            Console.WriteLine("SAVE=YES; ONLINE=NO; DOWNLOAD=NO");
        }

        private static int InspectTarget(string path)
        {
            try
            {
                using (TiaPortal portal = new TiaPortal(TiaPortalMode.WithoutUserInterface))
                {
                    Project project = portal.Projects.Open(new FileInfo(Path.GetFullPath(path)));
                    try
                    {
                        Device device = AllDevices(project).Single(item =>
                            item.Name.Equals(TargetDeviceName, StringComparison.OrdinalIgnoreCase));
                        Console.WriteLine("EXISTING_DEVICE=" + device.Name +
                            "; IS_GSD=" + device.IsGsd);
                        foreach (DeviceItem item in FindItems(device.DeviceItems))
                            Console.WriteLine("EXISTING_ITEM=" + item.Name +
                                "; TYPE=" + item.TypeIdentifier);
                        foreach (Node node in FindNodes(device.DeviceItems))
                            Console.WriteLine("EXISTING_NODE_ADDRESS=" + SafeString(node, "Address") +
                                "; PN_NAME=" + SafeString(node, "PnDeviceName") +
                                "; SUBNET=" + (node.ConnectedSubnet == null ? "<none>" :
                                    node.ConnectedSubnet.Name));
                        foreach (Address address in FindItems(device.DeviceItems)
                                     .SelectMany(item => item.Addresses))
                            Console.WriteLine("EXISTING_ADDRESS=" + address.IoType +
                                "; START=" + address.StartAddress +
                                "; LENGTH_BITS=" + address.Length);
                        foreach (DriveObjectContainer container in FindItems(device.DeviceItems)
                                     .Select(item => item.GetService<DriveObjectContainer>())
                                     .Where(item => item != null))
                            foreach (DriveObject driveObject in container.DriveObjects)
                                foreach (Telegram telegram in driveObject.Telegrams)
                                {
                                    Console.WriteLine("EXISTING_TELEGRAM=" +
                                        telegram.TelegramNumber);
                                    foreach (Address address in telegram.Addresses)
                                        Console.WriteLine("EXISTING_TELEGRAM_ADDRESS=" +
                                            address.IoType + "; START=" +
                                            address.StartAddress + "; LENGTH_BITS=" +
                                            address.Length);
                                }
                    }
                    finally { project.Close(); }
                }
                Console.WriteLine("STATUS=PASS_INSPECT");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("STATUS=FAIL");
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static void Validate(Device device, Node node, IoConnector connector,
            Address input, Address output)
        {
            if (!device.Name.Equals(TargetDeviceName, StringComparison.OrdinalIgnoreCase) ||
                !IpAddress.Equals(((IEngineeringObject)node).GetAttribute("Address") as string,
                    StringComparison.OrdinalIgnoreCase) ||
                !PnName.Equals(((IEngineeringObject)node).GetAttribute("PnDeviceName") as string,
                    StringComparison.OrdinalIgnoreCase) ||
                node.ConnectedSubnet == null ||
                connector.ConnectedToIoSystem == null ||
                input.StartAddress != InputStart || output.StartAddress != OutputStart)
                throw new InvalidOperationException(
                    "Transferred drive failed final offline validation.");
        }

        private static void ValidateReplaceableGsd(Device device)
        {
            Node node = FindNodes(device.DeviceItems).Single(item =>
                item.NodeType == NetType.Ethernet || !String.IsNullOrEmpty(
                    SafeString(item, "Address")));
            if (!IpAddress.Equals(SafeString(node, "Address"),
                    StringComparison.OrdinalIgnoreCase) ||
                !PnName.Equals(SafeString(node, "PnDeviceName"),
                    StringComparison.OrdinalIgnoreCase) ||
                node.ConnectedSubnet == null ||
                !node.ConnectedSubnet.Name.Equals("PN/IE_1",
                    StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "Existing GSD identity does not match the approved cap-distributor identity.");

            Address input;
            Address output;
            if (device.IsGsd)
            {
                input = FindAddresses(device.DeviceItems, "Input").Single(address =>
                    address.Length == 32);
                output = FindAddresses(device.DeviceItems, "Output").Single(address =>
                    address.Length == 32);
            }
            else
            {
                DriveObjectContainer container = FindItems(device.DeviceItems)
                    .Select(item => item.GetService<DriveObjectContainer>())
                    .Single(item => item != null);
                Telegram telegram = container.DriveObjects.Single().Telegrams.Single(item =>
                    item.TelegramNumber == 1);
                input = telegram.Addresses.Single(address =>
                    address.IoType.ToString().Equals("Input",
                        StringComparison.OrdinalIgnoreCase));
                output = telegram.Addresses.Single(address =>
                    address.IoType.ToString().Equals("Output",
                        StringComparison.OrdinalIgnoreCase));
            }
            if (input.StartAddress != InputStart || output.StartAddress != OutputStart)
                throw new InvalidOperationException(
                    "Existing GSD telegram addresses do not match the approved mapping.");
        }

        private static void EnsureIdentityIsFree(IEnumerable<Device> devices)
        {
            foreach (Device device in devices)
            {
                if (device.Name.Equals(TargetDeviceName, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("EXISTING_DEVICE=" + device.Name +
                        "; IS_GSD=" + device.IsGsd);
                    foreach (DeviceItem item in FindItems(device.DeviceItems))
                        Console.WriteLine("EXISTING_ITEM=" + item.Name +
                            "; TYPE=" + item.TypeIdentifier);
                    throw new InvalidOperationException(
                        "Target device already exists: " + TargetDeviceName);
                }
                foreach (Node node in FindNodes(device.DeviceItems))
                {
                    string address = SafeString(node, "Address");
                    string pnName = SafeString(node, "PnDeviceName");
                    if (address.Equals(IpAddress, StringComparison.OrdinalIgnoreCase) ||
                        pnName.Equals(PnName, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException(
                            "The approved cap-distributor identity is already in use by " +
                            device.Name + ".");
                }
            }
        }

        private static void EnsureAddressRangesAreFree(IEnumerable<Device> devices)
        {
            foreach (Device device in devices)
                foreach (DeviceItem item in FindItems(device.DeviceItems))
                    foreach (Address address in item.Addresses)
                    {
                        string ioType = address.IoType.ToString();
                        if (address.StartAddress < 0 || address.Length <= 0) continue;
                        int byteLength = (address.Length + 7) / 8;
                        int end = address.StartAddress + byteLength - 1;
                        if (ioType.Equals("Input", StringComparison.OrdinalIgnoreCase) &&
                            InputStart <= end && InputStart + 3 >= address.StartAddress)
                            throw new InvalidOperationException(
                                "Input I320..I323 overlaps " + device.Name + "/" + item.Name);
                        if (ioType.Equals("Output", StringComparison.OrdinalIgnoreCase) &&
                            OutputStart <= end && OutputStart + 3 >= address.StartAddress)
                            throw new InvalidOperationException(
                                "Output Q152..Q155 overlaps " + device.Name + "/" + item.Name);
                    }
        }

        private static string SafeString(IEngineeringObject obj, string name)
        {
            try { return obj.GetAttribute(name) as string ?? String.Empty; }
            catch { return String.Empty; }
        }

        private static void TrySetAttribute(IEngineeringObject obj, string name,
            object value)
        {
            try { obj.SetAttribute(name, value); }
            catch (EngineeringNotSupportedException)
            {
                Console.WriteLine("OPTIONAL_ATTRIBUTE_NOT_SUPPORTED=" + name);
            }
        }

        private static Subnet FindSubnet(IEnumerable<Device> devices, string name)
        {
            return devices.SelectMany(device => FindNodes(device.DeviceItems))
                .Select(node => node.ConnectedSubnet).FirstOrDefault(subnet =>
                    subnet != null && subnet.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        private static IoSystem FindIoSystem(IEnumerable<Device> devices, string name)
        {
            return devices.SelectMany(device => FindNetworks(device.DeviceItems))
                .SelectMany(network => network.IoControllers)
                .Select(controller => controller.IoSystem).FirstOrDefault(system =>
                    system != null && system.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        private static IEnumerable<Address> FindAddresses(DeviceItemComposition items,
            string ioType)
        {
            return FindItems(items).SelectMany(item => item.Addresses).Where(address =>
                address.IoType.ToString().Equals(ioType, StringComparison.OrdinalIgnoreCase));
        }

        private static IEnumerable<NetworkInterface> FindNetworks(DeviceItemComposition items)
        {
            foreach (DeviceItem item in items)
            {
                NetworkInterface network = item.GetService<NetworkInterface>();
                if (network != null) yield return network;
                foreach (NetworkInterface child in FindNetworks(item.DeviceItems)) yield return child;
            }
        }

        private static IEnumerable<Node> FindNodes(DeviceItemComposition items)
        {
            return FindNetworks(items).SelectMany(network => network.Nodes);
        }

        private static IEnumerable<DeviceItem> FindItems(DeviceItemComposition items)
        {
            foreach (DeviceItem item in items)
            {
                yield return item;
                foreach (DeviceItem child in FindItems(item.DeviceItems)) yield return child;
            }
        }

        private static IEnumerable<Device> AllDevices(Project project)
        {
            HashSet<Device> seen = new HashSet<Device>();
            foreach (Device device in project.Devices)
                if (seen.Add(device)) yield return device;
            foreach (Device device in project.UngroupedDevicesGroup.Devices)
                if (seen.Add(device)) yield return device;
            foreach (DeviceUserGroup group in project.DeviceGroups)
                foreach (Device device in DevicesInGroup(group))
                    if (seen.Add(device)) yield return device;
        }

        private static IEnumerable<Device> DevicesInGroup(DeviceUserGroup group)
        {
            foreach (Device device in group.Devices) yield return device;
            foreach (DeviceUserGroup child in group.Groups)
                foreach (Device device in DevicesInGroup(child)) yield return device;
        }
    }
}
