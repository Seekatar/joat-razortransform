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

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static int EXIT_NO_ERROR = 0;
        static int EXIT_ERROR = 1;

        [STAThread]
        [SuppressUnmanagedCodeSecurity]
        public static int Main(string[] args)
        {
            int ret = EXIT_ERROR; // failed

            bool run = false;
            bool showHelp = false;
            string outputFolder = "..";
            string logFile = "RazorTransform.log";
            Dictionary<string, object> parms = new Dictionary<string, object>();

            var options = new Mono.Options.OptionSet()
                .Add("run", v => { parms.Add("Run", true); run = true; })
                .Add("h|?|help", v => showHelp = true)
                .Add("object=", v => parms.Add("ObjectFile", v))
                .Add("values=", v => parms.Add("ValuesFile", v))
                .Add("output=", v => { outputFolder = v; parms.Add("OverrideOutputFolder", v); })
                .Add("log=", v => { logFile = v; parms.Add("LogFile", v); })
                .Add("test", v => parms.Add("Test", true))
                .Add("nosave", v => parms.Add("NoSave", true))
                .Add("debug", v => { parms.Add("Debug", true); ExceptionExtension.ShowStack = true; })
                .Add("template=", v => parms.Add("OverrideTemplateFolder", v));

            List<string> overrides = options.Parse(args);


            if (showHelp)
                ShowHelp();
            else
            {
                if (run) // run without the UI
                {
                    var transformer = new RazorTransformer();
                    try
                    {
                        transformer.Initialize(parms, Settings.SplitCommandLineOverrides(overrides));

                        transformer.Output.Report(new ProgressInfo( transformer.Settings.ToString(transformer.Model)));
                        if (transformer.Settings.Test)
                        {
                            transformer.Output.Report(new ProgressInfo(Resource.TestModeLogStart));
                        }

                        var result = transformer.DoTransformAsync();
                        result.Wait();

                        if (result.Result.TranformResult == ProcessingResult.ok)
                        {
                            ret = EXIT_NO_ERROR; // ok
                            if (transformer.Settings.Test)
                            {
                                transformer.Output.Report(new ProgressInfo(Resource.TestModeComplete));
                            }
                            else if (transformer.Settings.NoSave)
                            {
                                transformer.Output.Report(new ProgressInfo(Resource.NoSave));
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        transformer.Output.Report(new ProgressInfo(String.Format(Resource.ExceptionFormat, e.BuildMessage())));
                    }
                }
                else // show gui
                {
                    var app = new App();
#if !TELERIK
                    app.InitializeComponent();
#endif
                    var mw = new MainWindow();
                    try
                    {
                        mw.Initialize(parms, Settings.SplitCommandLineOverrides(overrides));
                        mw.Show();

                        app.Run();

                        ret = mw.RanTransformOk ? EXIT_NO_ERROR : EXIT_ERROR;
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(String.Format(Resource.ExceptionFormat, e.BuildMessage()), Resource.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        mw.Close();
                    }
                }
            }

            return ret;
        }

        public static void ShowHelp()
        {
            // sure would like to show console output
            MessageBox.Show(Resource.HelpString, Resource.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }
    }
}
