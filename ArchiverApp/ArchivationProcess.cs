using NLog;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArchiverApp
{
    public abstract class ArchivationProcess
    {
        #region Fields

        public abstract event TerminationEventHandler Terminate;

        public abstract event ProgressEventHandler ShowProgress;

        protected ArgumentNullException incorrectBlockValueException;

        protected static Logger logger;

        protected long _blockCount;

        protected bool _delete;

        protected Func<byte[], byte[]> ApplyGZip;

        #endregion

        #region .ctor

        public ArchivationProcess()
        {
            incorrectBlockValueException = new ArgumentNullException("blockValue", "Incorrect block value");
            logger = LogManager.GetCurrentClassLogger();
        }

        #endregion

        public abstract void ReadFile(string source, ref TaskPool readerTaskPool, int bufferSize);

        public abstract void Handle(ref TaskPool readerTaskPool, ref TaskPool writerTaskPool);

        public abstract void WriteFile(string destination, ref TaskPool writerTaskPool);
    }
}
