using System;
using System.IO;
using System.Net.Sockets;

namespace Schlenker.Profinet
{
    internal static class ReadS7BenchIoReadOnly
    {
        private const byte Inputs = 0x81;
        private const byte Outputs = 0x82;

        private static int Main(string[] args)
        {
            if (args.Length != 1 || !args[0].Equals("192.168.10.1", StringComparison.Ordinal))
            {
                Console.Error.WriteLine("Usage: ReadS7BenchIoReadOnly 192.168.10.1");
                return 2;
            }

            try
            {
                using (TcpClient client = new TcpClient())
                {
                    client.ReceiveTimeout = 3000;
                    client.SendTimeout = 3000;
                    client.Connect(args[0], 102);
                    NetworkStream stream = client.GetStream();

                    Exchange(stream, new byte[] {
                        0x03,0x00,0x00,0x16,0x11,0xE0,0x00,0x00,0x00,0x01,0x00,
                        0xC1,0x02,0x01,0x00,0xC2,0x02,0x01,0x02,0xC0,0x01,0x0A
                    });
                    Exchange(stream, new byte[] {
                        0x03,0x00,0x00,0x19,0x02,0xF0,0x80,0x32,0x01,0x00,0x00,
                        0x00,0x01,0x00,0x08,0x00,0x00,0xF0,0x00,0x00,0x01,0x00,
                        0x01,0x03,0xC0
                    });

                    byte[] inputs = ReadArea(stream, Inputs, 308, 19, 2);
                    byte[] outputs = ReadArea(stream, Outputs, 140, 16, 3);

                    Console.WriteLine("MODE=PLC_IO_READ_ONLY");
                    Console.WriteLine("TARGET=192.168.10.1:102");
                    Console.WriteLine("WRITE_REQUESTS=NO");
                    PrintWord("IW308_MAIN_STATUS", inputs, 0);
                    PrintWord("IW310_MAIN_ACTUAL", inputs, 2);
                    PrintWord("IW312_CONVEYOR_STATUS", inputs, 4);
                    PrintWord("IW314_CONVEYOR_ACTUAL", inputs, 6);
                    PrintWord("IW316_PRODUCT_STATUS", inputs, 8);
                    PrintWord("IW318_PRODUCT_ACTUAL", inputs, 10);
                    PrintWord("IW320_CAP_STATUS", inputs, 12);
                    PrintWord("IW322_CAP_ACTUAL", inputs, 14);
                    PrintWord("IW324_LMT100", inputs, 16);
                    Console.WriteLine("IB326_LMT100_PQI=0x" + inputs[18].ToString("X2"));
                    PrintWord("QW140_MAIN_CONTROL", outputs, 0);
                    PrintWord("QW142_MAIN_SETPOINT", outputs, 2);
                    PrintWord("QW144_CONVEYOR_CONTROL", outputs, 4);
                    PrintWord("QW146_CONVEYOR_SETPOINT", outputs, 6);
                    PrintWord("QW148_PRODUCT_CONTROL", outputs, 8);
                    PrintWord("QW150_PRODUCT_SETPOINT", outputs, 10);
                    PrintWord("QW152_CAP_CONTROL", outputs, 12);
                    PrintWord("QW154_CAP_SETPOINT", outputs, 14);
                    Console.WriteLine("STATUS=PASS");
                }
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("STATUS=FAIL");
                Console.Error.WriteLine(exception.GetType().FullName + ": " + exception.Message);
                return 1;
            }
        }

        private static byte[] ReadArea(NetworkStream stream, byte area, int startByte,
            int byteCount, ushort reference)
        {
            int bitAddress = checked(startByte * 8);
            byte[] request = new byte[] {
                0x03,0x00,0x00,0x1F,0x02,0xF0,0x80,
                0x32,0x01,0x00,0x00,
                (byte)(reference >> 8),(byte)reference,
                0x00,0x0E,0x00,0x00,
                0x04,0x01,0x12,0x0A,0x10,0x02,
                (byte)(byteCount >> 8),(byte)byteCount,
                0x00,0x00,area,
                (byte)(bitAddress >> 16),(byte)(bitAddress >> 8),(byte)bitAddress
            };
            byte[] response = Exchange(stream, request);
            if (response.Length < 25 || response[7] != 0x32 || response[8] != 0x03)
                throw new InvalidDataException("Unexpected S7 read response.");
            int parameterLength = (response[13] << 8) | response[14];
            int dataOffset = 19 + parameterLength;
            if (dataOffset + 4 > response.Length)
                throw new InvalidDataException("Truncated S7 read response.");
            if (response[dataOffset] != 0xFF)
                throw new InvalidDataException("S7 read rejected with return code 0x" +
                    response[dataOffset].ToString("X2") + ".");
            int bitLength = (response[dataOffset + 2] << 8) | response[dataOffset + 3];
            int receivedBytes = (bitLength + 7) / 8;
            if (receivedBytes < byteCount || dataOffset + 4 + byteCount > response.Length)
                throw new InvalidDataException("S7 read returned fewer bytes than requested.");
            byte[] result = new byte[byteCount];
            Buffer.BlockCopy(response, dataOffset + 4, result, 0, byteCount);
            return result;
        }

        private static byte[] Exchange(NetworkStream stream, byte[] request)
        {
            stream.Write(request, 0, request.Length);
            byte[] header = ReadExact(stream, 4);
            if (header[0] != 0x03)
                throw new InvalidDataException("Invalid TPKT response.");
            int length = (header[2] << 8) | header[3];
            if (length < 4 || length > 65535)
                throw new InvalidDataException("Invalid TPKT length.");
            byte[] response = new byte[length];
            Buffer.BlockCopy(header, 0, response, 0, 4);
            byte[] remainder = ReadExact(stream, length - 4);
            Buffer.BlockCopy(remainder, 0, response, 4, remainder.Length);
            return response;
        }

        private static byte[] ReadExact(Stream stream, int count)
        {
            byte[] buffer = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = stream.Read(buffer, offset, count - offset);
                if (read <= 0) throw new EndOfStreamException();
                offset += read;
            }
            return buffer;
        }

        private static void PrintWord(string label, byte[] buffer, int offset)
        {
            ushort value = (ushort)((buffer[offset] << 8) | buffer[offset + 1]);
            Console.WriteLine(label + "=0x" + value.ToString("X4") + "; DEC=" + value);
        }
    }
}
