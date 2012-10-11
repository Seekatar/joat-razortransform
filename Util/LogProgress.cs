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
    class LogProgress :  ITranformOutput
    {
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern bool AttachConsole(int processId);

        FileStream _fs;
        TextWriter _tw;
        bool _consoleAttached;

        public LogProgress(string fname)
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
        public void Report(string t)
        {
            if (_tw != null)
                _tw.WriteLine(t);
            if (_consoleAttached)
                Console.WriteLine(t);
        }

        public void Dispose()
        {
            _tw.Dispose();
            _fs.Dispose();
        }

        public MessageBoxResult ShowMessage(string msg, MessageBoxButton messageBoxButton = MessageBoxButton.OK, MessageBoxImage messageBoxImage = MessageBoxImage.None, string secondaryMsg = null)
        {
            if (!String.IsNullOrWhiteSpace(secondaryMsg))
                msg += Environment.NewLine + secondaryMsg;

            Report(msg);

            return MessageBoxResult.None;
        }
    }
}
