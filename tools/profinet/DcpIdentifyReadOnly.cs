using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Schlenker.Profinet
{
    internal static class DcpIdentifyReadOnly
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct PcapHeader
        {
            public int Seconds;
            public int Microseconds;
            public uint CapturedLength;
            public uint OriginalLength;
        }

        [DllImport("wpcap.dll", CharSet = CharSet.Ansi)]
        private static extern IntPtr pcap_open_live(string device, int snaplen, int promiscuous,
            int timeoutMs, StringBuilder errorBuffer);

        [DllImport("wpcap.dll")]
        private static extern int pcap_sendpacket(IntPtr handle, byte[] packet, int length);

        [DllImport("wpcap.dll")]
        private static extern int pcap_next_ex(IntPtr handle, out IntPtr header, out IntPtr packet);

        [DllImport("wpcap.dll")]
        private static extern void pcap_close(IntPtr handle);

        private static int Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine("Usage: DcpIdentifyReadOnly <adapter-guid> <local-mac> <seconds>");
                return 2;
            }

            string device = @"\Device\NPF_" + args[0].Trim();
            byte[] localMac = ParseMac(args[1]);
            int duration;
            if (!Int32.TryParse(args[2], out duration) || duration < 1 || duration > 30)
                throw new ArgumentOutOfRangeException("seconds");

            StringBuilder error = new StringBuilder(512);
            IntPtr handle = pcap_open_live(device, 65536, 1, 250, error);
            if (handle == IntPtr.Zero)
            {
                Console.Error.WriteLine("STATUS=FAIL");
                Console.Error.WriteLine(error.ToString());
                return 1;
            }

            try
            {
                uint xid = unchecked((uint)Environment.TickCount);
                byte[] request = BuildIdentifyRequest(localMac, xid);
                if (pcap_sendpacket(handle, request, request.Length) != 0)
                    throw new InvalidOperationException("Npcap could not send the read-only DCP Identify request.");

                Console.WriteLine("MODE=PROFINET_DCP_IDENTIFY_READ_ONLY");
                Console.WriteLine("ADAPTER=" + device);
                Console.WriteLine("LOCAL_MAC=" + FormatMac(localMac));
                Console.WriteLine("REQUEST_XID=0x" + xid.ToString("X8"));
                Console.WriteLine("DEVICE_WRITES=NO");

                HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                HashSet<string> lldpSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                Stopwatch timer = Stopwatch.StartNew();
                int count = 0;
                while (timer.Elapsed.TotalSeconds < duration)
                {
                    IntPtr headerPtr;
                    IntPtr packetPtr;
                    int result = pcap_next_ex(handle, out headerPtr, out packetPtr);
                    if (result <= 0) continue;
                    PcapHeader header = (PcapHeader)Marshal.PtrToStructure(headerPtr, typeof(PcapHeader));
                    int length = checked((int)header.CapturedLength);
                    if (length < 30) continue;
                    byte[] packet = new byte[length];
                    Marshal.Copy(packetPtr, packet, 0, length);
                    LldpResponse lldp = ParseLldp(packet);
                    if (lldp != null && lldpSeen.Add(lldp.Mac))
                    {
                        Console.WriteLine("LLDP_BEGIN");
                        Console.WriteLine("MAC=" + lldp.Mac);
                        Console.WriteLine("SYSTEM_NAME=" + lldp.SystemName);
                        Console.WriteLine("PORT_ID=" + lldp.PortId);
                        Console.WriteLine("SYSTEM_DESCRIPTION=" + lldp.SystemDescription);
                        Console.WriteLine("LLDP_END");
                    }
                    DeviceResponse response = ParseIdentifyResponse(packet, xid);
                    if (response == null || !seen.Add(response.Mac)) continue;
                    count++;
                    Console.WriteLine("DEVICE_BEGIN");
                    Console.WriteLine("MAC=" + response.Mac);
                    Console.WriteLine("NAME=" + response.Name);
                    Console.WriteLine("IP=" + response.Ip);
                    Console.WriteLine("MASK=" + response.Mask);
                    Console.WriteLine("GATEWAY=" + response.Gateway);
                    Console.WriteLine("VENDOR_ID=" + response.VendorId);
                    Console.WriteLine("DEVICE_ID=" + response.DeviceId);
                    Console.WriteLine("MANUFACTURER=" + response.Manufacturer);
                    Console.WriteLine("DEVICE_END");
                }

                Console.WriteLine("DEVICES_FOUND=" + count);
                Console.WriteLine("LLDP_SOURCES_FOUND=" + lldpSeen.Count);
                Console.WriteLine("STATUS=PASS");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("STATUS=FAIL");
                Console.Error.WriteLine(exception);
                return 1;
            }
            finally
            {
                pcap_close(handle);
            }
        }

        private static byte[] BuildIdentifyRequest(byte[] localMac, uint xid)
        {
            byte[] frame = new byte[30];
            byte[] destination = { 0x01, 0x0E, 0xCF, 0x00, 0x00, 0x00 };
            Buffer.BlockCopy(destination, 0, frame, 0, 6);
            Buffer.BlockCopy(localMac, 0, frame, 6, 6);
            frame[12] = 0x88; frame[13] = 0x92;
            frame[14] = 0xFE; frame[15] = 0xFE;
            frame[16] = 0x05; frame[17] = 0x00;
            WriteUInt32BigEndian(frame, 18, xid);
            frame[22] = 0x00; frame[23] = 0x00;
            frame[24] = 0x00; frame[25] = 0x04;
            frame[26] = 0xFF; frame[27] = 0xFF;
            frame[28] = 0x00; frame[29] = 0x00;
            return frame;
        }

        private static DeviceResponse ParseIdentifyResponse(byte[] packet, uint expectedXid)
        {
            int offset = 14;
            ushort etherType = ReadUInt16BigEndian(packet, 12);
            if (etherType == 0x8100 && packet.Length >= 34)
            {
                etherType = ReadUInt16BigEndian(packet, 16);
                offset = 18;
            }
            if (etherType != 0x8892 || packet.Length < offset + 12) return null;
            if (ReadUInt16BigEndian(packet, offset) != 0xFEFF) return null;
            if (packet[offset + 2] != 0x05 || packet[offset + 3] != 0x01) return null;
            if (ReadUInt32BigEndian(packet, offset + 4) != expectedXid) return null;

            int dataLength = ReadUInt16BigEndian(packet, offset + 10);
            int cursor = offset + 12;
            int end = Math.Min(packet.Length, cursor + dataLength);
            DeviceResponse response = new DeviceResponse { Mac = FormatMac(packet, 6) };
            while (cursor + 4 <= end)
            {
                byte option = packet[cursor];
                byte suboption = packet[cursor + 1];
                int blockLength = ReadUInt16BigEndian(packet, cursor + 2);
                int data = cursor + 4;
                if (data + blockLength > end) break;
                int payload = data + 2;
                int payloadLength = Math.Max(0, blockLength - 2);

                if (option == 0x01 && suboption == 0x02 && payloadLength >= 12)
                {
                    response.Ip = FormatIp(packet, payload);
                    response.Mask = FormatIp(packet, payload + 4);
                    response.Gateway = FormatIp(packet, payload + 8);
                }
                else if (option == 0x02 && suboption == 0x02 && payloadLength > 0)
                {
                    response.Name = Encoding.ASCII.GetString(packet, payload, payloadLength).TrimEnd('\0');
                }
                else if (option == 0x02 && suboption == 0x03 && payloadLength >= 4)
                {
                    response.VendorId = "0x" + ReadUInt16BigEndian(packet, payload).ToString("X4");
                    response.DeviceId = "0x" + ReadUInt16BigEndian(packet, payload + 2).ToString("X4");
                }
                else if (option == 0x02 && suboption == 0x01 && payloadLength > 0)
                {
                    response.Manufacturer = Encoding.ASCII.GetString(packet, payload, payloadLength).TrimEnd('\0');
                }

                cursor = data + blockLength;
                if ((blockLength & 1) != 0) cursor++;
            }
            return response;
        }

        private static LldpResponse ParseLldp(byte[] packet)
        {
            int offset = 14;
            ushort etherType = ReadUInt16BigEndian(packet, 12);
            if (etherType == 0x8100 && packet.Length >= 22)
            {
                etherType = ReadUInt16BigEndian(packet, 16);
                offset = 18;
            }
            if (etherType != 0x88CC) return null;

            LldpResponse response = new LldpResponse { Mac = FormatMac(packet, 6) };
            int cursor = offset;
            while (cursor + 2 <= packet.Length)
            {
                ushort header = ReadUInt16BigEndian(packet, cursor);
                int type = header >> 9;
                int length = header & 0x01FF;
                cursor += 2;
                if (cursor + length > packet.Length) break;
                if (type == 0) break;
                if (type == 2 && length > 1)
                    response.PortId = Encoding.ASCII.GetString(packet, cursor + 1, length - 1).TrimEnd('\0');
                else if (type == 5 && length > 0)
                    response.SystemName = Encoding.ASCII.GetString(packet, cursor, length).TrimEnd('\0');
                else if (type == 6 && length > 0)
                    response.SystemDescription = Encoding.ASCII.GetString(packet, cursor, length).TrimEnd('\0');
                cursor += length;
            }
            return response;
        }

        private sealed class DeviceResponse
        {
            public string Mac = "";
            public string Name = "";
            public string Ip = "";
            public string Mask = "";
            public string Gateway = "";
            public string VendorId = "";
            public string DeviceId = "";
            public string Manufacturer = "";
        }

        private sealed class LldpResponse
        {
            public string Mac = "";
            public string PortId = "";
            public string SystemName = "";
            public string SystemDescription = "";
        }

        private static byte[] ParseMac(string text)
        {
            string[] parts = text.Split('-', ':');
            if (parts.Length != 6) throw new FormatException("Invalid local MAC address.");
            byte[] value = new byte[6];
            for (int i = 0; i < 6; i++) value[i] = Convert.ToByte(parts[i], 16);
            return value;
        }

        private static string FormatMac(byte[] value) { return FormatMac(value, 0); }
        private static string FormatMac(byte[] value, int offset)
        {
            return String.Format("{0:X2}-{1:X2}-{2:X2}-{3:X2}-{4:X2}-{5:X2}",
                value[offset], value[offset + 1], value[offset + 2], value[offset + 3], value[offset + 4], value[offset + 5]);
        }

        private static string FormatIp(byte[] value, int offset)
        {
            return String.Format("{0}.{1}.{2}.{3}", value[offset], value[offset + 1], value[offset + 2], value[offset + 3]);
        }

        private static ushort ReadUInt16BigEndian(byte[] value, int offset)
        {
            return (ushort)((value[offset] << 8) | value[offset + 1]);
        }

        private static uint ReadUInt32BigEndian(byte[] value, int offset)
        {
            return ((uint)value[offset] << 24) | ((uint)value[offset + 1] << 16) |
                   ((uint)value[offset + 2] << 8) | value[offset + 3];
        }

        private static void WriteUInt32BigEndian(byte[] value, int offset, uint number)
        {
            value[offset] = (byte)(number >> 24);
            value[offset + 1] = (byte)(number >> 16);
            value[offset + 2] = (byte)(number >> 8);
            value[offset + 3] = (byte)number;
        }
    }
}
