#define _LZ4_
//#define _GZIP_
using System;
using System.IO;
using System.IO.Compression;
using Lz4Net;
using System.Collections.Generic;

namespace AudioStreaming
{
    static public class Compressor
    {
#if _LZ4_
        //LZ4 compression
        static public byte[] Compress(byte[] data)
        {
            if(data == null)
                throw new ArgumentNullException("input data to compress should not be null");

            byte[] compress = null;
            compress = Lz4.CompressBytes(data,Lz4Mode.Fast);
            return compress;
        }
        static public byte[] Decompress(byte[] data)
        {
            if(data == null)
                throw new ArgumentNullException("input data to decompress should not be null");

            byte[] decompressed = null;
            decompressed = Lz4.DecompressBytes(data);
            return decompressed;
        }
#elif _GZIP_
        //GZIP compression by http://toreaurstad.blogspot.be/2014/01/compressing-byte-array-in-c-with.html
        private static int BUFFER_SIZE = 8 * 1024 * 1024; //8MB //64kB

        public static byte[] Compress(byte[] inputData)
        {
            if (inputData == null)
                throw new ArgumentNullException("inputData must be non-null");

            using (var compressIntoMs = new MemoryStream())
            {
                using (var gzs = new BufferedStream(new GZipStream(compressIntoMs,
                 CompressionMode.Compress), BUFFER_SIZE))
                {
                    gzs.Write(inputData, 0, inputData.Length);
                }
                return compressIntoMs.ToArray();
            }
        }

        public static byte[] Decompress(byte[] inputData)
        {
            if (inputData == null)
                throw new ArgumentNullException("inputData must be non-null");

            using (var compressedMs = new MemoryStream(inputData))
            {
                using (var decompressedMs = new MemoryStream())
                {
                    using (var gzs = new BufferedStream(new GZipStream(compressedMs,
                     CompressionMode.Decompress), BUFFER_SIZE))
                    {
                        gzs.CopyTo(decompressedMs);
                    }
                    return decompressedMs.ToArray();
                }
            }
        }
#else
        const int BUFFERSIZE = 1024;

        public static byte[] Compress(byte[] data)
        {
            var memoryStream = new MemoryStream();
            var deflateStream = new DeflateStream(memoryStream, CompressionMode.Compress);
            deflateStream.Write(data, 0, data.Length);
            deflateStream.Flush();
            deflateStream.Close();

            return memoryStream.ToArray();
        }



        public static byte[] Decompress(byte[] data)
        {
            var buffer = new byte[BUFFERSIZE];
            var returnVal = new List<byte>();

            var deflateStream = new DeflateStream(new MemoryStream(data), CompressionMode.Decompress);
            int count;
            while ((count = deflateStream.Read(buffer, 0, BUFFERSIZE)) > 0)
            {

                if (count != BUFFERSIZE)
                {
                    var tmpBuffer = new byte[count];
                    Array.Copy(buffer, tmpBuffer, count);
                    returnVal.AddRange(tmpBuffer);
                }
                else
                    returnVal.AddRange(buffer);
            }

            return returnVal.ToArray();
        }
#endif
    }
}