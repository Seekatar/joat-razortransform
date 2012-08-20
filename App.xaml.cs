using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.IO;

namespace RazorTransform
{
    class LogProgress : IProgress<string>, IDisposable
    {
        FileStream _fs;
        TextWriter _tw;
        public LogProgress(string fname)
        {
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
        }

        public void Dispose()
        {
            _tw.Dispose();
            _fs.Dispose();
        }
    }

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static int EXIT_NO_ERROR = 0;
        static int EXIT_ERROR = 1;

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern bool AttachConsole(int processId); 

        [STAThread]
        [SuppressUnmanagedCodeSecurity]
        public static int Main(string[] args)
        {
#if thisJustNeverPrintsAnythingOutForSomeReasonArrrrrrg
            if (AttachConsole((int)-1))
            {
                Console.WriteLine("asdfasdfasdfasdfasdf");
                Console.Out.WriteLine("asdfasdfasdf");
                Console.Out.Flush();
                Console.Out.Close();
                var s = Console.OpenStandardOutput();
                var ss = "asdfaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
                byte[] buffer = Encoding.ASCII.GetBytes(ss);
                s.Write(buffer, 0, buffer.Length);
                return;
            }
#endif
            int ret = EXIT_ERROR; // failed

            bool run = false;
            bool showHelp = false;
            bool maximize = false;
            string logFile = String.Empty;

            var options = new Mono.Options.OptionSet()
                .Add("run", v => run = true)
                .Add("h|?|help", v => showHelp = true)
                .Add("max", v => maximize = true)
                .Add("logFile=", v => logFile = v);

            List<string> parms = options.Parse(args);

            if (showHelp)
                ShowHelp();
            else
            {
                if (run)
                {
                    if (parms.Count < 2 || !File.Exists(parms[0]) || !Directory.Exists(parms[1]))
                    {
                        ShowHelp();
                    }
                    else
                    {
                        LogProgress progress;
                        if (logFile != String.Empty)
                            progress = new LogProgress(logFile);
                        else
                            progress = new LogProgress(Path.Combine(parms[1], Resource.Title.Replace(" ", "_") + ".log"));

                        using (progress)
                        {
                            try
                            {
                                var _cs = new ConfigSettings();
                                _cs.LoadFromFile(parms[0], parms[1],parms.Skip(2));

                                var d = _cs.GetProperties(true, true);

                                RazorFileTransformer rf = new RazorFileTransformer(d);
                                rf.TransformFiles(parms[1], false, progress);
                                ret = EXIT_NO_ERROR; // ok
                            }
                            catch (Exception e)
                            {
                                progress.Report(e.Message);
                            }
                        }
                    }
                }
                else // show gui
                {
                    var app = new App();
                    var mw = new MainWindow();
                    if (mw.Initialize(parms))
                    {
                        if ( maximize )
                            mw.WindowState = WindowState.Maximized;
                        mw.Show();
                        app.Run();
                    }
                    ret = mw.RanTransformOk ? EXIT_NO_ERROR : EXIT_ERROR;
                }
            }

            return ret;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
        }

        public static void ShowHelp()
        {
            // sure would like to show console output
            MessageBox.Show(Resource.HelpString, Resource.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }
    }
}
