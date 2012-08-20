using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Dynamic;
using System.IO;
using System.Xml.Linq;
using System.Threading;

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

        static string DEFAULT_FILE_NAME = "DeployCfgFields.xml";
        ConfigSettings _cs;
        string _destination = "..";
        string _sourceFile = DEFAULT_FILE_NAME;
        List<Control> _controls = new List<Control>();
        XDocument _doc;

        public MainWindow()
        {
            InitializeComponent();
        }

        public bool Initialize(IList<string> args)
        {
            bool ret = false;
            bool loadLastPath = true;
            try
            {
                int skipCount = 0;
                if (args != null && args.Count() > 0)
                {
                    if (File.Exists(args[0]))
                    {
                        _sourceFile = args[0];
                        skipCount++;
                    }

                    if (args.Count() > 1 && Directory.Exists(args[1]))
                    {
                        _destination = args[1];
                        loadLastPath = false;
                        skipCount++;
                    }
                }

                loadTemplateFile(loadLastPath, args.Count > skipCount ? args.Skip(skipCount) : null );
                ret = true;
            }
            catch (Exception e)
            {
                MessageBox.Show(String.Format(Resource.ExceptionFormat, e.Message), Resource.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                Close();
            }
            return ret;
        }

        private void loadTemplateFile(bool loadLastPath = true, IEnumerable<string> overrideParms = null)
        {
            // load the config file
            _cs = new ConfigSettings();

            // if source doesn't exist look for *.template.xml
            var source = _sourceFile;
            if (!File.Exists(source))
                source = _sourceFile.Replace(".xml", ".template.xml");

            _doc = XDocument.Load(source);

            var root = _doc.Root;
            if (root.Attribute("title") != null)
                this.Title = root.Attribute("title").Value;
            if (loadLastPath && root.Attribute("lastPath") != null && Directory.Exists(System.IO.Path.GetFullPath(root.Attribute("lastPath").Value)))
                _destination = System.IO.Path.GetFullPath(root.Attribute("lastPath").Value);
            if (_destination == "..")
                _destination = System.IO.Path.GetFullPath("..");
            if (root.Attribute("saveValues") != null)
                cbSaveAsDefaults.IsChecked = (bool)(root.Attribute("saveValues"));

            _cs.LoadFromXElement(root, _destination, overrideParms);

            editControl.Load(_cs.Info);

            txtSourceFile.Text = _sourceFile;
            txtDestinationFolder.Text = _destination;
        }

        private CancellationTokenSource _cts = null;
        public bool RanTransformOk = false;

        private 
#if ASYNC 
            async 
#endif
        void btnOkAndClose_Click(object sender, RoutedEventArgs e)
        {
            btnOk_Click(sender, e);
            if ( RanTransformOk )
                btnCancel_Click(sender, e);
        }

#if ASYNC
        async
#endif
        void btnOk_Click(object sender, RoutedEventArgs e)
        {
            RanTransformOk = false;

            _destination = txtDestinationFolder.Text;
            if (!Directory.Exists(_destination))
            {
                MessageBox.Show(Resource.DestMustExist,Resource.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            else
            {
                bool saveValues = cbSaveAsDefaults.IsChecked.HasValue && cbSaveAsDefaults.IsChecked.Value;
                try
                {
                    var d = _cs.GetProperties(saveValues, true);

                    // save right away in case it errors out
                    if (saveValues)
                    {
                        _doc.Root.SetAttributeValue("lastPath", _destination);
                        _doc.Root.SetAttributeValue("saveValues", saveValues);
                        _doc.Save(_sourceFile); // always save Xml to write out last settings
                    }

                    RazorFileTransformer rf = new RazorFileTransformer(d);
                    btnOk.IsEnabled = btnOkAndClose.IsEnabled = btnGetMaskFolder.IsEnabled = btnGetSourceFile.IsEnabled = editControl.IsEnabled = false;
                    btnCancel.Content = Resource.Cancel;
                    _cts = new CancellationTokenSource();

#if ASYNC
                    await rf.TransformFilesAsync(_destination, _cts.Token, false, new Progress(this));
#else
                    Mouse.SetCursor(Cursors.Wait);
                    rf.TransformFiles(_destination);
#endif
                    if ( sender == btnOk )
                        MessageBox.Show(Resource.Success, Resource.Title, MessageBoxButton.OK, MessageBoxImage.Information);
                    RanTransformOk = true;

                }
                catch (OperationCanceledException)
                {
                    MessageBox.Show(Resource.Canceled, Resource.Title, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ee)
                {
                    MessageBox.Show(String.Format(Resource.ExceptionFormat, ee.Message), Resource.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                finally
                {
                    Mouse.SetCursor(null);
                    _cts = null;
                    lblProgress.Content = String.Empty;
                    btnCancel.Content = Resource.Close;
                    btnOk.IsEnabled = btnOkAndClose.IsEnabled = btnGetMaskFolder.IsEnabled = btnGetSourceFile.IsEnabled = editControl.IsEnabled = true;
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

        private void btnGetMaskFolder_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fb = new System.Windows.Forms.FolderBrowserDialog();
            fb.Description = Resource.BrowsePrompt;
            fb.ShowNewFolderButton = false;
            if (!String.IsNullOrWhiteSpace(txtDestinationFolder.Text))
                fb.SelectedPath = txtDestinationFolder.Text;

            if (fb.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtDestinationFolder.Text = _destination = fb.SelectedPath;
                loadTemplateFile(false);
            }
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

        private void btnGetSourceFile_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new System.Windows.Forms.OpenFileDialog();
            ofd.CheckFileExists = true;
            ofd.DefaultExt = "XML";
            ofd.AddExtension = true;
            ofd.InitialDirectory = System.IO.Path.GetDirectoryName(_sourceFile);
            ofd.FileName = _sourceFile;
            ofd.RestoreDirectory = true;

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtSourceFile.Text = _sourceFile = ofd.FileName;
                loadTemplateFile();
            }
        }

    }
}
