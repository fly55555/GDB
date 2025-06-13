using System;
using System.Text;

namespace GDB.Core.Protocol
{
    public class GdbPacket
    {
        public string Data { get; }
        public byte Checksum { get; }

        public GdbPacket(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                throw new ArgumentException("Packet data cannot be null or empty.", nameof(data));
            }

            Data = data;
            Checksum = CalculateChecksum(data);
        }

        private GdbPacket(string data, byte checksum)
        {
            Data = data;
            Checksum = checksum;
        }

        public byte[] Serialize()
        {
            string packet = $"${Data}#{Checksum:x2}";
            return Encoding.ASCII.GetBytes(packet);
        }

        public static GdbPacket Deserialize(string rawPacket)
        {
            if (string.IsNullOrEmpty(rawPacket) || rawPacket.Length < 4)
            {
                throw new ArgumentException("Invalid GDB packet.", nameof(rawPacket));
            }

            if (rawPacket[0] != '$')
            {
                throw new ArgumentException("Packet must start with '$'.", nameof(rawPacket));
            }

            int hashIndex = rawPacket.LastIndexOf('#');
            if (hashIndex == -1 || hashIndex > rawPacket.Length - 3)
            {
                throw new ArgumentException("Packet must contain a valid checksum.", nameof(rawPacket));
            }

            string data = rawPacket.Substring(1, hashIndex - 1);
            string checksumString = rawPacket.Substring(hashIndex + 1);

            if (!byte.TryParse(checksumString, System.Globalization.NumberStyles.HexNumber, null, out byte receivedChecksum))
            {
                throw new ArgumentException("Invalid checksum format.", nameof(rawPacket));
            }

            byte calculatedChecksum = CalculateChecksum(data);
            if (receivedChecksum != calculatedChecksum)
            {
                throw new InvalidOperationException("Checksum mismatch.");
            }

            return new GdbPacket(data, receivedChecksum);
        }

        private static byte CalculateChecksum(string data)
        {
            byte checksum = 0;
            foreach (char c in data)
            {
                checksum += (byte)c;
            }
            return checksum;
        }
    }
} 