using System;

namespace ArchiverApp
{
    public class ProgressReport
    {
        private readonly string _operation;

        public ProgressReport(Mode mode)
        {
            _operation = mode.ToString();
        }

        public void ShowProgress(double progress)
        {
            Console.Write("{0}: {1:P}\r", _operation, progress);
        }

        public void Done(TimeSpan delta)
        {
            Console.Write("{0}: Done! ({1:D2}:{2:D2}:{3:D2})", _operation, delta.Hours, delta.Minutes, delta.Seconds);
        }
    }
}
