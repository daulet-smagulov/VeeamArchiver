using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace ArchiverApp
{
    public class GZip
    {
        private readonly int _bufferSize;

        public GZip(int bufferSize)
        {
            _bufferSize = bufferSize;
        }

        public byte[] Compress(byte[] block)
        {
            using var compressedBlock = new MemoryStream();
            using var zip = new GZipStream(compressedBlock, CompressionMode.Compress);
            zip.Write(block, 0, block.Length);

            return compressedBlock.ToArray();
        }

        public byte[] Decompress(byte[] block)
        {
            byte[] decompressedBlock = new byte[_bufferSize];
            int size;

            using var compressedBlock = new MemoryStream(block);
            using var zip = new GZipStream(compressedBlock, CompressionMode.Decompress);
            size = zip.Read(decompressedBlock, 0, _bufferSize);

            return decompressedBlock.Take(size).ToArray();
        }
    }
}
