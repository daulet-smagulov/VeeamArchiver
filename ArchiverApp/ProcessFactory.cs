using System;
using System.Collections.Generic;
using System.Text;

namespace ArchiverApp
{
    public static class ProcessFactory
    {
        public static ArchivationProcess GetProcess(Mode mode, GZip gzip)
        {
            return mode switch
            {
                Mode.Compress => new Compression(gzip),
                Mode.Decompress => new Decompression(gzip),
                _ => throw new ArgumentException("Cannot create operator: unknown mode " + mode),
            };
        }
    }
}
