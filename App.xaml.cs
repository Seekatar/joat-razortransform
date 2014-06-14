using System;
using System.Collections.Generic;
using System.Security;
using System.Windows;
using RtPsHost;

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

            try
            {
                bool run = false;
                bool upgrade = false;
                bool showHelp = false;
                string outputFolder = "..";
                string logFile = "RazorTransform.log";
                Dictionary<string, object> parms = new Dictionary<string, object>();

                var options = new Mono.Options.OptionSet()
                    .Add("run", v => { parms["Run"] = true; run = true; })
                    .Add("h|?|help", v => showHelp = true)
                    .Add("object=", v => parms.Add("ObjectFile", v))
                    .Add("values=", v => parms.Add("ValuesFile", v))
                    .Add("output=", v => { outputFolder = v; parms.Add("OverrideOutputFolder", v); })
                    .Add("log=", v => { logFile = v; parms.Add("LogFile", v); })
                    .Add("test", v => parms.Add("Test", true))
                    .Add("nosave", v => parms.Add("NoSave", true))
                    .Add("debug", v => { parms.Add("Debug", true); ExceptionExtension.ShowStack = true; })
                    .Add("powerShell", v => { parms.Add("PowerShell", true); })
                    .Add("template=", v => parms.Add("OverrideTemplateFolder", v))
                    .Add("showHidden", v => parms.Add("ShowHidden", true))
                    .Add("upgrade", v => { upgrade = true; parms["Run"] = true; run = true; });

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
                            var ok = transformer.InitializeAsync(parms, Settings.SplitCommandLineOverrides(overrides));
                            ok.Wait();

                            transformer.Output.Report(new ProgressInfo(transformer.Settings.ToString(transformer.Model)));
                            if (transformer.Settings.Test)
                            {
                                transformer.Output.Report(new ProgressInfo(Resource.TestModeLogStart));
                            }

                            if (upgrade) // only upgrading
                            {
                                transformer.SaveAsync(false, false).Wait();
                                transformer.Output.Report(new ProgressInfo(Resource.ConversionComplete, percentComplete: 100));
                                ret = EXIT_NO_ERROR;
                            }
                            else
                            {
                                var result = transformer.DoTransformAsync(false);
                                result.Wait();

                                if (result.Result.Result == ProcessingResult.ok)
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
                        }
                        catch (Exception e)
                        {
                            transformer.Output.Report(new ProgressInfo(String.Format(Resource.ExceptionFormat, e.BuildMessage())));
                        }
                    }
                    else // show gui
                    {
                        var app = new App();

                        app.InitializeComponent();

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
            }
            catch (Exception e)
            {
                MessageBox.Show(String.Format(Resource.ExceptionFormat, e.BuildMessage()), Resource.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
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
