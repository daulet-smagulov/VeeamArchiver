using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ArchiverApp
{
    public class ArgumentsValidator
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public static bool Validate(string[] args, out ArchiverSettings settings)
        {
            if (ParseArguments(args, out settings))
            {
                return true;
            }

            ShowHelpMessage();
            return false;
        }

        private static bool ParseArguments(string[] args, out ArchiverSettings settings)
        {
            settings = null;

            if (args.Length != 3)
            {
                logger.Error("Three arguments is expected", args);
                return false;
            }

            if (!Enum.TryParse(typeof(Mode), args[0], ignoreCase: true, out object mode))
            {
                logger.Error("Unknown operation mode. Please specify compress/decompress", args[0]);
                return false;
            }

            if (!File.Exists(args[1]))
            {
                logger.Error("Source file doesn't exist", args[1]);
                return false;
            }

            if (!Directory.Exists(Path.GetDirectoryName(args[2])))
            {
                logger.Error("Destination folder doesn't exist", Path.GetDirectoryName(args[2]));
                return false;
            }

            settings = new ArchiverSettings((Mode)mode, args[1], args[2]);
            return true;
        }

        private static void ShowHelpMessage()
        {
            string helpMessage = "Use arguments in the following format:\n" +
                "app.exe [mode] [source file name] [destination file name]\n" +
                " - mode: compress/decompress;\n" +
                " - source file name: full path of the file to be archived;\n" +
                " - destination file name: full path of the result compressed file.";
            Console.WriteLine(helpMessage);
        }
    }
}
