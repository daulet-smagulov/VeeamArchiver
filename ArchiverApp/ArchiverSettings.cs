using System;
using System.Collections.Generic;
using System.Text;

namespace ArchiverApp
{
    public class ArchiverSettings
    {
        public ArchiverSettings(Mode mode, string sourceFileName, string targetFileName)
        {
            Mode = mode;
            SourceFileName = sourceFileName;
            DestinationFileName = targetFileName;
        }

        public Mode Mode { get; }
        public string SourceFileName { get; }
        public string DestinationFileName { get; }
    }

    public enum Mode
    {
        Compress,
        Decompress
    }
}
