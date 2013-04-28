using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RazorTransform
{
    /// <summary>
    /// progress class that writes to console, if attached or a file
    /// </summary>
    class LogProgress :  ITransformOutput
    {
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern bool AttachConsole(int processId);

        FileStream _fs;
        TextWriter _tw;
        bool _consoleAttached;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="fname">name of output file</param>
        public LogProgress(ProgressInfo fname)
        {
            _consoleAttached = AttachConsole((int)-1);

            try
            {
                _fs = new FileStream(fname, FileMode.Create);
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
                _tw.WriteLine("{0} {1}", DateTime.Now.ToString("G"), t);
                _tw.Flush();
            }
            if (_consoleAttached)
                Console.WriteLine(t);
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
    }
}
