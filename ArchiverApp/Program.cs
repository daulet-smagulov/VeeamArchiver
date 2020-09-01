using NLog;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace ArchiverApp
{
    class Program
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private const int BUFFER_SIZE = 1048576;

        static int Main(string[] args)
        {
            if (!ArgumentsValidator.Validate(args, out ArchiverSettings settings))
            {
                return 1;
            }

            try
            {
                var gzip = new GZip(BUFFER_SIZE);

                ArchivationProcess archivation = ProcessFactory.GetProcess(settings.Mode, gzip);
                var progressReport = new ProgressReport(settings.Mode);

                int coreCount = Environment.ProcessorCount * 2;

                var readPool = new TaskPool(coreCount);
                var writePool = new TaskPool(coreCount);

                Stopwatch sw = new Stopwatch();
                sw.Start();

                archivation.ShowProgress += progressReport.ShowProgress;
                archivation.Terminate += readPool.Terminate;
                archivation.Terminate += writePool.Terminate;

                Thread readerThread = new Thread(delegate () { archivation.ReadFile(settings.SourceFileName, ref readPool, BUFFER_SIZE); });
                Thread writerThread = new Thread(delegate () { archivation.WriteFile(settings.DestinationFileName, ref writePool); });

                var handlers = new Thread[coreCount];
                for (int i = 0; i < coreCount; i++)
                {
                    handlers[i] = new Thread(delegate () { archivation.Handle(ref readPool, ref writePool); });
                }

                readerThread.Start();
                foreach (Thread handler in handlers)
                {
                    handler.Start();
                }
                writerThread.Start();

                writerThread.Join();
                foreach (Thread handler in handlers)
                {
                    handler.Join();
                }
                readerThread.Join();

                sw.Stop();
                progressReport.Done(sw.Elapsed);

                archivation.Terminate -= writePool.Terminate;
                archivation.Terminate -= readPool.Terminate;
                archivation.ShowProgress -= progressReport.ShowProgress;
            }
            catch (Exception ex)
            {
                logger.Fatal(ex.Message);
                return 1;
            }
            finally
            {
                logger.Info("Done");
            }

            return 0;
        }
    }
}
