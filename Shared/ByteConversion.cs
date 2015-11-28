using System;

namespace AudioStreaming
{
    public static class ByteConversion
    {
        public static int ByteArrayToInt(byte[] data, int startindex)
        {
            if (!BitConverter.IsLittleEndian)
                return GetLittleEndianIntegerFromByteArray(data, startindex);
            else
                return GetBigEndianIntegerFromByteArray(data, startindex);
        }
        public static uint ByteArrayToUInt(byte[] data, int startindex)
        {
            if (!BitConverter.IsLittleEndian)
                return GetLittleEndianUnsignedIntegerFromByteArray(data, startindex);
            else
                return GetBigEndianUnsignedIntegerFromByteArray(data, startindex);
        }

        /// <summary>
        /// converts an int to a byte and then returns what is in result[_byte];
        /// </summary>
        /// <param name="value">the integer to convert</param>
        /// <param name="_byte">index, 0 to 3</param>
        /// <returns>byte</returns>
        public static byte ByteFromInt(int value, byte _byte)
        {
            if (_byte < 0 || _byte > 4 || value == 0 || _byte == 0)
                return 0;

            byte[] intBytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(intBytes);
            byte[] result = intBytes;
            return (result[_byte]);
        }


        private static int GetBigEndianIntegerFromByteArray(byte[] data, int startIndex)
        {
            return (data[startIndex] << 24)
                 | (data[startIndex + 1] << 16)
                 | (data[startIndex + 2] << 8)
                 | data[startIndex + 3];
        }

        private static int GetLittleEndianIntegerFromByteArray(byte[] data, int startIndex)
        {
            return (data[startIndex + 3] << 24)
                 | (data[startIndex + 2] << 16)
                 | (data[startIndex + 1] << 8)
                 | data[startIndex];
        }
        private static uint GetBigEndianUnsignedIntegerFromByteArray(byte[] data, int startIndex)
        {
            return (uint)((data[startIndex] << 24)
                 | (data[startIndex + 1] << 16)
                 | (data[startIndex + 2] << 8)
                 | data[startIndex + 3]);
        }

        private static uint GetLittleEndianUnsignedIntegerFromByteArray(byte[] data, int startIndex)
        {
            return (uint)((data[startIndex + 3] << 24)
                 | (data[startIndex + 2] << 16)
                 | (data[startIndex + 1] << 8)
                 | data[startIndex]);
        }
    }
}
