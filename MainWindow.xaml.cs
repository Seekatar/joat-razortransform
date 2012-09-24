using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;

namespace RazorTransform
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        class Progress : IProgress<string>
        {
            private readonly SynchronizationContext syncContext;

            MainWindow _window;
            public Progress(MainWindow w)
            {
                this.syncContext = SynchronizationContext.Current ?? new SynchronizationContext();
                _window = w;
            }

            public void Report(string value)
            {
                syncContext.Post(_ =>
                {
                    _window.lblProgress.Content = "Processing " + value;
                }, null);
            }
        }

        static string DEFAULT_OBJECT_FILE_NAME = "RtObject.xml";
        static string DEFAULT_VALUE_FILE_NAME = "RtValues.xml";

        ConfigSettings _cs = new ConfigSettings();

        string _destination = "..";
        string _valueFileName = DEFAULT_VALUE_FILE_NAME;
        string _definitionFileName = DEFAULT_OBJECT_FILE_NAME;

        List<Control> _controls = new List<Control>();
        XDocument _valuesDoc = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        public bool Initialize(Settings settings)
        {
            _settings = settings;
                 
            bool ret = false;
            try
            {
                _valueFileName = settings.ValuesFile;
                _destination = settings.OutputFolder;
                _definitionFileName = settings.ObjectFile;

                loadTemplateFile();
                ret = true;
            }
            catch (Exception e)
            {
                MessageBox.Show(String.Format(Resource.ExceptionFormat, e.BuildMessage()), Resource.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                Close();
            }
            return ret;
        }

        Settings _settings = new Settings();

        private void loadTemplateFile()
        {
            // load the config file

            XElement objectValues = null;
            if (File.Exists(_valueFileName))
            {
                _valuesDoc = XDocument.Load(_valueFileName);
                objectValues = _valuesDoc.Root;

            }
            _cs.Info.Add(_settings.ArrayConfigInfo); // add so we can save it when we save


            _destination = _settings.OutputFolder;

            if (_destination == "..")
                _destination = System.IO.Path.GetFullPath("..");

            if (!String.IsNullOrWhiteSpace(_settings.Title))
                this.Title += " " + _settings.Title;

            this.Title += " -> " + _destination;

            // load the main file
            if (!File.Exists(_definitionFileName))
                throw new FileNotFoundException(String.Format(Resource.FileNotFound, _definitionFileName));

            var definitionDoc = XDocument.Load(_definitionFileName);
            _cs.LoadFromXElement(definitionDoc.Root, objectValues, _settings.Overrides);

            editControl.Load(_cs.Groups);

        }

        private CancellationTokenSource _cts = null;
        public bool RanTransformOk = false;

        private 
        void btnOkAndClose_Click(object sender, RoutedEventArgs e)
        {
            btnOk_Click(sender, e);
        }

#if ASYNC 
        async 
#endif
        void btnOk_Click(object sender, RoutedEventArgs e)
        {
          
            RanTransformOk = false;

            if (!Directory.Exists(_destination))
            {
                MessageBox.Show(Resource.DestMustExist,Resource.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            else
            {
                try
                {
                    var d = _cs.GetProperties(!_settings.Test, true, _destination);

                    // save right away in case it errors out
                    if (!_settings.Test && !_settings.NoSave)
                    {
                        _cs.Save(_valueFileName);
                    }

                    RazorFileTransformer rf = new RazorFileTransformer(d);
                    btnOk.IsEnabled = btnOkAndClose.IsEnabled = editControl.IsEnabled = settingBtn.IsEnabled = false;
                    btnCancel.Content = Resource.Cancel;
                    _cts = new CancellationTokenSource();

#if ASYNC
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    int count = await rf.TransformFilesAsync(_settings.TemplateFolder, _destination, !_settings.Test, _cts.Token, false, new Progress(this));
                    sw.Stop();
                    _cts = null;
                    RanTransformOk = true;
                    if (sender == btnOk)
                    {
                        string msg = String.Format(Resource.Success, count, _destination, sw.Elapsed.TotalSeconds);
                        if (_settings.Test)
                        {
                            msg += Environment.NewLine + Resource.TestModeComplete;
                        }
                        if (_settings.NoSave)
                        {
                            msg += Environment.NewLine + Resource.NoSave;
                        }
                        
                        MessageBox.Show(msg, Resource.Title, MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        btnCancel_Click(sender, e);
                    }
#else
                    Mouse.SetCursor(Cursors.Wait);
                    rf.TransformFiles(_destination);
#endif
                    if ( sender == btnOk )
                    	MessageBox.Show(String.Format(Resource.Success,count,_destination), Resource.Title, MessageBoxButton.OK, MessageBoxImage.Information);
                    RanTransformOk = true;

                }
                catch (OperationCanceledException)
                {
                    MessageBox.Show(Resource.Canceled, Resource.Title, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ee)
                {
                    MessageBox.Show(String.Format(Resource.ExceptionFormat, ee.BuildMessage()), Resource.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                finally
                {
                    Mouse.SetCursor(null);
                    _cts = null;
                    btnOk.IsEnabled = btnOkAndClose.IsEnabled = editControl.IsEnabled = settingBtn.IsEnabled = true;
                    btnCancel.Content = Resource.Close;
                    lblProgress.Content = String.Empty;
                }
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_cts != null)
                _cts.Cancel();
            else
                Close();
        }


        private void StackPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var lblWidth = e.NewSize.Width / 3 - SystemParameters.VerticalScrollBarWidth;
            var txtWidth = e.NewSize.Width - lblWidth - 10;
            foreach (var c in _controls)
            {
                if (c is Label )
                    c.Width = lblWidth;
                else
                    c.Width = txtWidth;
            }
        }

        private void settingBtn_Click(object sender, RoutedEventArgs e)
        {
            var aie = new ArrayItemEdit();
            aie.ShowDialog(_settings.ConfigInfo);
        }
    }
}
