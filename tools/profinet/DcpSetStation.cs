using System;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace Schlenker.Profinet
{
    internal static class DcpSetStation
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
            bool nameOnly = args.Length == 6 && args[5] == "--name-only-execute";
            bool fullSet = args.Length == 8 && args[7] == "--execute";
            if (!nameOnly && !fullSet)
            {
                Console.Error.WriteLine("Usage: DcpSetStation <adapter-guid> <local-mac> <target-mac> <name> <ip> <mask> <gateway> --execute");
                Console.Error.WriteLine("   or: DcpSetStation <adapter-guid> <local-mac> <target-mac> <name> <reserved> --name-only-execute");
                return 2;
            }

            string adapter = @"\Device\NPF_" + args[0].Trim();
            byte[] localMac = ParseMac(args[1]);
            byte[] targetMac = ParseMac(args[2]);
            string stationName = args[3].Trim();
            ValidateName(stationName);

            StringBuilder error = new StringBuilder(512);
            IntPtr handle = pcap_open_live(adapter, 65536, 1, 250, error);
            if (handle == IntPtr.Zero) throw new InvalidOperationException(error.ToString());

            try
            {
                uint nameXid = unchecked((uint)Environment.TickCount);
                byte[] nameFrame = BuildSetFrame(localMac, targetMac, nameXid, 0x02, 0x02,
                    Encoding.ASCII.GetBytes(stationName));

                Console.WriteLine("MODE=" + (nameOnly ? "PROFINET_DCP_SET_NAME_ONLY_EXACT_TARGET" : "PROFINET_DCP_SET_EXACT_TARGET"));
                Console.WriteLine("TARGET_MAC=" + FormatMac(targetMac));
                Console.WriteLine("NAME=" + stationName);

                Send(handle, nameFrame, "NAME");
                bool nameAck = WaitForResponse(handle, targetMac, nameXid, 4);
                Console.WriteLine("NAME_ACK=" + nameAck.ToString().ToUpperInvariant());
                if (nameOnly)
                {
                    Console.WriteLine("IP_WRITE=NO");
                    Console.WriteLine("STATUS=" + (nameAck ? "PASS" : "UNVERIFIED"));
                    return nameAck ? 0 : 3;
                }

                byte[] ip = ParseIp(args[4]);
                byte[] mask = ParseIp(args[5]);
                byte[] gateway = ParseIp(args[6]);
                uint ipXid = unchecked(nameXid + 1);
                byte[] ipData = new byte[12];
                Buffer.BlockCopy(ip, 0, ipData, 0, 4);
                Buffer.BlockCopy(mask, 0, ipData, 4, 4);
                Buffer.BlockCopy(gateway, 0, ipData, 8, 4);
                byte[] ipFrame = BuildSetFrame(localMac, targetMac, ipXid, 0x01, 0x02, ipData);
                Console.WriteLine("IP=" + args[4]);
                Console.WriteLine("MASK=" + args[5]);
                Console.WriteLine("GATEWAY=" + args[6]);
                Send(handle, ipFrame, "IP");
                bool ipAck = WaitForResponse(handle, targetMac, ipXid, 4);
                Console.WriteLine("IP_ACK=" + ipAck.ToString().ToUpperInvariant());
                Console.WriteLine("STATUS=" + (nameAck && ipAck ? "PASS" : "UNVERIFIED"));
                return nameAck && ipAck ? 0 : 3;
            }
            finally { pcap_close(handle); }
        }

        private static void Send(IntPtr handle, byte[] frame, string label)
        {
            if (pcap_sendpacket(handle, frame, frame.Length) != 0)
                throw new InvalidOperationException("Npcap failed to send the " + label + " DCP Set request.");
            Console.WriteLine(label + "_REQUEST_SENT=TRUE");
        }

        private static byte[] BuildSetFrame(byte[] source, byte[] target, uint xid,
            byte option, byte suboption, byte[] value)
        {
            int blockLength = 2 + value.Length;
            int paddedBlockLength = blockLength + (blockLength & 1);
            byte[] frame = new byte[14 + 12 + 4 + paddedBlockLength];
            Buffer.BlockCopy(target, 0, frame, 0, 6);
            Buffer.BlockCopy(source, 0, frame, 6, 6);
            frame[12] = 0x88; frame[13] = 0x92;
            frame[14] = 0xFE; frame[15] = 0xFD;
            frame[16] = 0x04; frame[17] = 0x00;
            WriteUInt32BigEndian(frame, 18, xid);
            frame[22] = 0x00; frame[23] = 0x00;
            WriteUInt16BigEndian(frame, 24, (ushort)(4 + paddedBlockLength));
            frame[26] = option; frame[27] = suboption;
            WriteUInt16BigEndian(frame, 28, (ushort)blockLength);
            frame[30] = 0x00; frame[31] = 0x01;
            Buffer.BlockCopy(value, 0, frame, 32, value.Length);
            return frame;
        }

        private static bool WaitForResponse(IntPtr handle, byte[] target, uint xid, int seconds)
        {
            Stopwatch timer = Stopwatch.StartNew();
            while (timer.Elapsed.TotalSeconds < seconds)
            {
                IntPtr headerPtr, packetPtr;
                int result = pcap_next_ex(handle, out headerPtr, out packetPtr);
                if (result <= 0) continue;
                PcapHeader header = (PcapHeader)Marshal.PtrToStructure(headerPtr, typeof(PcapHeader));
                int length = checked((int)header.CapturedLength);
                if (length < 26) continue;
                byte[] packet = new byte[length];
                Marshal.Copy(packetPtr, packet, 0, length);
                if (!MacEquals(packet, 6, target)) continue;
                int offset = 14;
                ushort etherType = ReadUInt16BigEndian(packet, 12);
                if ((etherType == 0x8100 || etherType == 0x88A8) && length >= 30)
                {
                    etherType = ReadUInt16BigEndian(packet, 16);
                    offset = 18;
                }
                if (etherType != 0x8892 || length < offset + 12) continue;
                if (ReadUInt16BigEndian(packet, offset) != 0xFEFD) continue;
                if (packet[offset + 2] != 0x04 || packet[offset + 3] != 0x01) continue;
                if (ReadUInt32BigEndian(packet, offset + 4) != xid) continue;
                return true;
            }
            return false;
        }

        private static void ValidateName(string value)
        {
            if (value.Length < 1 || value.Length > 240)
                throw new ArgumentOutOfRangeException("name");
            foreach (char c in value)
                if (!((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '-' || c == '.'))
                    throw new FormatException("PROFINET name contains an unsupported character.");
        }

        private static byte[] ParseIp(string value)
        {
            IPAddress address;
            if (!IPAddress.TryParse(value, out address) || address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                throw new FormatException("Invalid IPv4 address: " + value);
            return address.GetAddressBytes();
        }

        private static byte[] ParseMac(string value)
        {
            string[] parts = value.Split('-', ':');
            if (parts.Length != 6) throw new FormatException("Invalid MAC address.");
            byte[] result = new byte[6];
            for (int i = 0; i < 6; i++) result[i] = Convert.ToByte(parts[i], 16);
            return result;
        }

        private static bool MacEquals(byte[] packet, int offset, byte[] mac)
        {
            for (int i = 0; i < 6; i++) if (packet[offset + i] != mac[i]) return false;
            return true;
        }

        private static string FormatMac(byte[] value)
        {
            return String.Format("{0:X2}-{1:X2}-{2:X2}-{3:X2}-{4:X2}-{5:X2}",
                value[0], value[1], value[2], value[3], value[4], value[5]);
        }

        private static ushort ReadUInt16BigEndian(byte[] value, int offset)
        { return (ushort)((value[offset] << 8) | value[offset + 1]); }
        private static uint ReadUInt32BigEndian(byte[] value, int offset)
        {
            return ((uint)value[offset] << 24) | ((uint)value[offset + 1] << 16) |
                   ((uint)value[offset + 2] << 8) | value[offset + 3];
        }
        private static void WriteUInt16BigEndian(byte[] value, int offset, ushort data)
        { value[offset] = (byte)(data >> 8); value[offset + 1] = (byte)data; }
        private static void WriteUInt32BigEndian(byte[] value, int offset, uint data)
        {
            value[offset] = (byte)(data >> 24); value[offset + 1] = (byte)(data >> 16);
            value[offset + 2] = (byte)(data >> 8); value[offset + 3] = (byte)data;
        }
    }
}
