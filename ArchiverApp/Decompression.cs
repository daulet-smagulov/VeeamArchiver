using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ArchiverApp
{
    public class Decompression : ArchivationProcess
    {
        #region Fields

        public override event TerminationEventHandler Terminate;

        public override event ProgressEventHandler ShowProgress;

        //For FAT32 file system
        private const long MAX_FILE_SIZE = 4294967295;

        private long _originLength;

        #endregion

        public Decompression(GZip gzip)
        {
            ApplyGZip = gzip.Decompress;
        }

        public override void ReadFile(string source, ref TaskPool readPool, int bufferSize)
        {
            try
            {
                using var br = new BinaryReader(new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.None));
                _originLength = br.ReadInt64();
                _blockCount = br.ReadInt64();

                for (int count = 0; count < _blockCount; count++)
                {
                    int blockNumber = br.ReadInt32();
                    int blockLength = br.ReadInt32();
                    byte[] blockValue = br.ReadBytes(blockLength);

                    if (blockValue == null)
                    {
                        throw new ArgumentNullException("blockValue", "Incorrect block value");
                    }

                    if (!readPool.TrySet(blockNumber, blockValue))
                    {
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                _delete = true;
                Terminate();
                Console.WriteLine(e.Message);
                return;
            }
        }

        public override void Handle(ref TaskPool readPool, ref TaskPool writePool)
        {
            int blockNumber = -1;
            byte[] blockValue = null;

            while (true)
            {
                try
                {
                    if (!readPool.TryGet(out blockNumber, out blockValue))
                    {
                        return;
                    }

                    if (blockValue == null)
                    {
                        break;
                    }

                    byte[] decompressedBlock = ApplyGZip(blockValue);

                    if (!writePool.TrySet(blockNumber, decompressedBlock))
                    {
                        return;
                    }
                }
                catch (Exception e)
                {
                    _delete = true;
                    Terminate();
                    Console.WriteLine(e.Message);
                    return;
                }
            }
        }

        public override void WriteFile(string destination, ref TaskPool writePool)
        {
            try
            {
                FileInfo fi = new FileInfo(destination);
                DriveInfo drive = new DriveInfo(fi.Directory.Root.FullName);
                if (drive.DriveFormat == "FAT32" && _originLength > MAX_FILE_SIZE)
                {
                    throw new IOException("ERROR: недостаточно места на диске записи распакованного файла (ограничение FAT32)");
                }
            }
            catch (Exception e)
            {
                Terminate();
                Console.WriteLine(e.Message);
                return;
            }

            int counter = 0;

            int blockNumber = -1;
            byte[] blockValue = null;

            Dictionary<int, byte[]> buffer = new Dictionary<int, byte[]>();

            try
            {
                using var bw = new BinaryWriter(new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None));
                while (true)
                {
                    if (!writePool.TryGet(out blockNumber, out blockValue))
                    {
                        return;
                    }

                    if (blockValue == null)
                    {
                        break;
                    }

                    buffer[blockNumber] = blockValue;

                    while (buffer.ContainsKey(counter))
                    {
                        bw.Write(buffer[counter]);
                        buffer.Remove(counter);

                        counter++;
                        ShowProgress((double)counter / _blockCount);

                        if (counter == _blockCount)
                        {
                            Terminate();
                            return;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _delete = true;
                Terminate();
                Console.WriteLine(e.Message);
            }

            if (_delete)
            {
                try
                {
                    File.Delete(destination);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return;
                }
            }
        }
    }
}
