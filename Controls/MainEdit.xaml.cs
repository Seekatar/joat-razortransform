using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace RazorTransform
{
    /// <summary>
    /// Interaction logic for MainEdit.xaml
    /// </summary>
    public partial class MainEdit : UserControl
    {
        private RazorTransformer _transformer = new RazorTransformer();
        private Dictionary<string, object> _parms;
        private List<string> _overrides;
        private ITransformParentWindow _parent;
        private bool _embedded = false; // are we embedded in another app (hide certain buttons)

        public MainEdit()
        {
            InitializeComponent();
        }

        internal void Load(List<TransformModelItem> list)
        {
            editControl.Load(list);
        }

        public bool RanTransformOk { get; set; }

        /// <summary>
        /// get the suffix for the title, usually configured Title and destination folder
        /// </summary>
        public string TitleSuffix 
        { 
            get 
            {
                return _transformer.Model.TitleSuffix;
            }  
        }

        public void Initalize(ITransformParentWindow parent, Dictionary<string, object> parms, List<string> overrides, bool embedded = false)
        {
            _embedded = embedded;
            _parent = parent;
            _parms = parms;
            _overrides = overrides;
        }

        private async Task<bool> DoTransforms(bool sentFromOk)
        {
            btnOk.IsEnabled = btnOkAndClose.IsEnabled = editControl.IsEnabled = settingBtn.IsEnabled = false;
            btnCancel.IsEnabled = true;
            btnCancel.Content = Resource.Cancel;

            RanTransformOk = false;
            var transformResult = await _transformer.DoTransformAsync();

            if (transformResult.TranformOk)
            {
                RanTransformOk = true;
                if (sentFromOk)
                {
                    string msg = String.Format(Resource.Success, transformResult.Count, _transformer.Settings.OutputFolder, transformResult.Elapsed.TotalSeconds);
                    if (_transformer.Settings.Test)
                    {
                        msg += Environment.NewLine + Resource.TestModeComplete;
                    }
                    if (_transformer.Settings.NoSave)
                    {
                        msg += Environment.NewLine + Resource.NoSave;
                    }

                    _transformer.Output.ShowMessage(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }

            btnOk.IsEnabled = btnOkAndClose.IsEnabled = editControl.IsEnabled = settingBtn.IsEnabled = true;

            if (_embedded)
                btnCancel.IsEnabled = false;
            else
                btnCancel.Content = Resource.Close;

            lblProgress.Content = String.Empty;

            return transformResult.TranformOk;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (!_transformer.Cancel())
            {
                _parent.ProcessingComplete(RanTransformOk ? ProcessingResult.close : ProcessingResult.canceled);
            }
        }


        private async void btnOk_Click(object sender, RoutedEventArgs e)
        {
            await DoTransforms(true);
        }

        private async void btnOkAndClose_Click(object sender, RoutedEventArgs e)
        {
            if ( await DoTransforms(false) )
                _parent.ProcessingComplete(ProcessingResult.ok);
        }

        private void settingBtn_Click(object sender, RoutedEventArgs e)
        {
            var aie = new ArrayItemEdit();
            aie.ShowDialog(_transformer.Settings.ConfigInfo);
        }

        /// <summary>
        /// initialize on loaded to make sure the gui log has correct context
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void create_Loaded(object sender, RoutedEventArgs e)
        {
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                _transformer.Initialize(_parms, _overrides, this);
                editControl.Load(_transformer.Model.Groups);
                if (_embedded)
                {
                    btnOkAndClose.Visibility = settingBtn.Visibility = System.Windows.Visibility.Hidden;
                    btnCancel.Content = Resource.Cancel;
                    btnCancel.IsEnabled = false;
                }
                _parent.SetTitle(TitleSuffix);
            }
        }
    }
}
