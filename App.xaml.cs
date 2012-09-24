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
        bool _consoleAttached;

        public LogProgress(string fname, bool consoleAttached )
        {
            _consoleAttached = consoleAttached;
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
            if ( _consoleAttached )
                Console.WriteLine(t);
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
            int ret = EXIT_ERROR; // failed

            bool run = false;
            bool showHelp = false;
            bool maximize = false;

            Settings settings = new Settings();

            var options = new Mono.Options.OptionSet()
                .Add("run", v => run = true)
                .Add("h|?|help", v => showHelp = true)
                .Add("object=", v => settings.ObjectFile = v)
                .Add("values=", v => settings.ValuesFile = v)
                .Add("output=", v => settings.OverrideOutputFolder = v)
                .Add("log=", v => settings.LogFile = v)
                .Add("test", v => settings.Test = true)
                .Add("nosave", v => settings.NoSave = true )
                .Add("debug", v => { settings.Debug = true; ExceptionExtension.ShowStack = true; })
                .Add("template=", v => settings.OverrideTemplateFolder = v);

            List<string> parms = options.Parse(args);
            settings.SetOverrides(parms);

            if (showHelp)
                ShowHelp();
            else
            {
                Exception settingsException = null;
                try
                {
                    settings.Load();
                }
                catch (Exception e)
                {
                    settingsException = e;
                }


                if (run) // run without the UI
                {
                    LogProgress progress;
                    progress = new LogProgress(Path.Combine(settings.OutputFolder,settings.LogFile),AttachConsole((int)-1));

                    using (progress)
                    {
                        if (settingsException != null)
                            progress.Report(String.Format(Resource.SettingsException, settingsException.BuildMessage()));

                        progress.Report(settings.ToString());
                        if (settings.Test)
                        {
                            progress.Report(Resource.TestModeLogStart);
                        }

                        try
                        {
                            var _cs = new ConfigSettings();
                            _cs.LoadFromFile(settings); 

                            var d = _cs.GetProperties(true, true, settings.OutputFolder); 

                            RazorFileTransformer rf = new RazorFileTransformer(d);
                            rf.TransformFiles(settings.TemplateFolder, settings.OutputFolder, !settings.Test, false, progress);
                            ret = EXIT_NO_ERROR; // ok
                            if (settings.Test)
                            {
                                progress.Report(Resource.TestModeComplete);
                            }
                            else if (settings.NoSave)
                            {
                                progress.Report(Resource.NoSave);
                            }
                        }
                        catch (Exception e)
                        {
                            progress.Report(String.Format(Resource.ExceptionFormat,e.BuildMessage()));
                        }
                    }
                }
                else // show gui
                {
                    if (settingsException != null)
                        MessageBox.Show(String.Format(Resource.SettingsException, settingsException.BuildMessage()), Resource.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);

                    var app = new App();
                    app.InitializeComponent();
                    var mw = new MainWindow();
                    if (mw.Initialize(settings))
                    {
                        if (maximize)
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
