using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using RtPsHost;

namespace RazorTransform
{
    /// <summary>
    /// progress class that writes to console, if attached or a file
    /// </summary>
    class LogProgress :  ITransformOutput
    {
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool AttachConsole(int processId);

        static public bool IsConsoleAttached()
        {
            if( !_consoleAttached )
            {
                _consoleAttached = AttachConsole(-1); // current process
            }
            return _consoleAttached;
        }

        FileStream _fs;
        TextWriter _tw;
        static bool _consoleAttached;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="fname">name of output file</param>
        public LogProgress(ProgressInfo fname)
        {
            IsConsoleAttached();

            try
            {
                _fs = new FileStream(fname, FileMode.Append);
                _tw = new StreamWriter(_fs);
            }
            catch
            { }
        }

        /// <summary>
        /// interaface implmentation of reporting a value
        /// </summary>
        /// <param name="t"></param>
        public void Report(ProgressInfo t)
        {
            if (_tw != null)
            {
                _tw.WriteLine("{0} {1}", DateTime.Now.ToString("G"), t.ToString());
                _tw.Flush();
            }
            if (_consoleAttached)
                Console.WriteLine(t.ToString());
        }

        /// <summary>
        /// interface impl of dispose
        /// </summary>
        public void Dispose()
        {
            _tw.Dispose();
            _fs.Dispose();
        }

        /// <summary>
        /// interface impl just calls Report()
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="messageBoxButton"></param>
        /// <param name="messageBoxImage"></param>
        /// <param name="secondaryMsg"></param>
        /// <returns></returns>
        public MessageBoxResult ShowMessage(string msg, MessageBoxButton messageBoxButton = MessageBoxButton.OK, MessageBoxImage messageBoxImage = MessageBoxImage.None, string secondaryMsg = null)
        {
            if (!String.IsNullOrWhiteSpace(secondaryMsg))
                msg += Environment.NewLine + secondaryMsg;

            Report(new ProgressInfo(msg));

            return MessageBoxResult.None;
        }

        /// <summary>
        /// Writes the PowerShell exports to the console so an attached script can read them
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        internal static void WriteExports(System.Collections.Generic.Dictionary<string, object> dictionary)
        {
            if (LogProgress.IsConsoleAttached())
            {
                int count = 0;
                foreach (var v in dictionary)
                {
                    count++;
                    Console.WriteLine("=>\t{0}\t{1}", v.Key, v.Value.ToString());
                }
            }
        }
    }
}
