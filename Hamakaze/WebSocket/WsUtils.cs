using System;

namespace Hamakaze.WebSocket {
    internal static class WsUtils {
        public static byte[] FromU16(ushort num) {
            byte[] buff = BitConverter.GetBytes(num);
            if(BitConverter.IsLittleEndian)
                Array.Reverse(buff);
            return buff;
        }

        public static ushort ToU16(ReadOnlySpan<byte> buffer) {
            if(BitConverter.IsLittleEndian)
                buffer = new byte[2] {
                    buffer[1], buffer[0],
                };

            return BitConverter.ToUInt16(buffer);
        }

        public static byte[] FromI64(long num) {
            byte[] buff = BitConverter.GetBytes(num);
            if(BitConverter.IsLittleEndian)
                Array.Reverse(buff);
            return buff;
        }

        public static long ToI64(ReadOnlySpan<byte> buffer) {
            if(BitConverter.IsLittleEndian)
                buffer = new byte[8] {
                    buffer[7], buffer[6], buffer[5], buffer[4],
                    buffer[3], buffer[2], buffer[1], buffer[0],
                };

            return BitConverter.ToInt64(buffer);
        }
    }
}
