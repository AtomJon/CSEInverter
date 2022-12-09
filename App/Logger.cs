using System.Diagnostics;
using System.IO;

namespace CSEInverter
{
    public class Logger
    {
        private static Logger singleton = new();

        StreamWriter file = new("./log.txt");

        private Logger() { }

        ~Logger()
        {
            file.Dispose();
        }

        public static void WriteLine(object o, bool writeToDebug) => singleton._WriteLine(o.ToString(), writeToDebug);

        public static void WriteLine(string text, bool writeToDebug) => singleton._WriteLine(text, writeToDebug);

        private void _WriteLine(string message, bool writeToDebug)
        {
            string caller = new StackFrame(2).GetMethod().DeclaringType.Name;

            message = $"{caller}: {message}";

            if (writeToDebug)
            {
                Debug.WriteLine(message);
            }

            file.WriteLine(message);
            file.Flush();
        }
    }
}
