using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ArchiverApp
{
    public class Compression : ArchivationProcess
    {
        #region Fields

        public override event TerminationEventHandler Terminate;

        public override event ProgressEventHandler ShowProgress;

        private long _sourceFileSize;

        #endregion

        public Compression(GZip gzip)
        {
            ApplyGZip = gzip.Compress;
        }

        public override void ReadFile(string sourceFile, ref TaskPool readPool, int bufferSize)
        {
            try
            {
                FileInfo file = new FileInfo(sourceFile);
                _sourceFileSize = file.Length;
                _blockCount = file.Length / bufferSize;
                if (file.Length % bufferSize > 0)
                {
                    _blockCount++;
                }
            }
            catch (Exception e)
            {
                _delete = true;
                Terminate();
                logger.Warn(e.Message);
                return;
            }

            try
            {
                using var binReader = new BinaryReader(new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.None));
                for (int blockNumber = 0; blockNumber < _blockCount; blockNumber++)
                {
                    byte[] blockValue = binReader.ReadBytes(bufferSize);

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
                logger.Warn(e.Message);
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

                    byte[] compressedBlock = ApplyGZip(blockValue);

                    if (!writePool.TrySet(blockNumber, compressedBlock))
                    {
                        return;
                    }
                }
                catch (Exception e)
                {
                    _delete = true;
                    Terminate();
                    logger.Warn(e.Message);
                    return;
                }
            }
        }

        public override void WriteFile(string destination, ref TaskPool writePool)
        {
            int counter = 0;

            int blockNumber = -1;
            byte[] blockValue = null;

            try
            {
                using var binWriter = new BinaryWriter(new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None));
                binWriter.Write(BitConverter.GetBytes(_sourceFileSize));
                binWriter.Write(BitConverter.GetBytes(_blockCount));

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

                    try
                    {
                        binWriter.Write(BitConverter.GetBytes(blockNumber));
                        binWriter.Write(blockValue.Length);
                        binWriter.Write(blockValue);
                    }
                    catch (IOException e)
                    {
                        Terminate();
                        logger.Error("Writing is terminated ({0})", e.Message);
                        binWriter.Close();
                        File.Delete(destination);
                        return;
                    }

                    counter++;
                    ShowProgress((double)counter / _blockCount);

                    if (counter == _blockCount)
                    {
                        Terminate();
                    }
                }
            }
            catch (Exception e)
            {
                _delete = true;
                Terminate();
                logger.Warn(e.Message);
            }

            if (_delete)
            {
                try
                {
                    File.Delete(destination);
                }
                catch (Exception e)
                {
                    logger.Warn(e.Message);
                    return;
                }
            }
        }
    }
}
