using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace RazorTransform
{
    /// <summary>
    /// Interaction logic for MainEdit.xaml
    /// </summary>
    public partial class MainEdit : UserControl,  IDisposable
    {
        private RazorTransformer _transformer = new RazorTransformer();
        private IDictionary<string, object> _parms;
        private IDictionary<string, string> _overrides;
        private ITransformParentWindow _parent;
        private bool _embedded = false; // are we embedded in another app (hide certain buttons)
        private ProcessingState _currentState = ProcessingState.idle;
        enum ProcessingState
        {
            idle,
            transforming,
            transformed,
            shellExecuting,
            shellExecuted
        }

        public MainEdit()
        {
            InitializeComponent();
            _transformer.OnValuesSave += _transformer_OnValuesSave;
        }

        /// <summary>
        /// have we run a tranform successfully
        /// </summary>
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

        /// <summary>
        /// load all the data from the model
        /// </summary>
        /// <param name="list"></param>
        internal void Load(List<TransformModelGroup> list)
        {
            editControl.Load(list);
        }

        public void Initalize(ITransformParentWindow parent, IDictionary<string, object> parms, IDictionary<string, string> overrides, bool embedded = false)
        {
            _embedded = embedded;
            _parent = parent;
            _parms = parms;
            _overrides = overrides;
        }
        /// <summary>
        /// handler for whem the transformer saves the RTValues.xml file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _transformer_OnValuesSave(object sender, string e)
        {
            Dispatcher.Invoke(new Action(() =>
                {
                    _parent.SendData(new Dictionary<string, string>
                    {
                         {"ValuesContent", e}
                    });
                }));
        }
		
        private async Task<ProcessingResult> doTransforms(bool sentFromOk)
        {
            setButtonStates(ProcessingState.transforming);

            RanTransformOk = false;
            TransformResult transformResult = new TransformResult();
            transformResult.TranformResult = ProcessingResult.ok;

            if (!_overrides.ContainsKey("PsSkipTransform"))
            {
                transformResult = await _transformer.DoTransformAsync();
               
            }

            if (transformResult.TranformResult == ProcessingResult.ok )
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

            setButtonStates(ProcessingState.transformed);

            return transformResult.TranformResult;
        }

        /// <summary>
        /// monkey with the visibility and enabled states of the buttons
        /// depending on what's going on
        /// </summary>
        /// <param name="currentState"></param>
        private void setButtonStates(ProcessingState currentState)
        {
            if (_transformer.Settings.NoSave || _transformer.Settings.Test)
                btnSave.Visibility = System.Windows.Visibility.Collapsed;

            _currentState = currentState;
            switch (currentState)
            {
                case ProcessingState.transforming:
                    btnSave.IsEnabled = btnOk.IsEnabled = btnOkAndClose.IsEnabled = editControl.IsEnabled = btnSettings.IsEnabled = false;
                    btnCancel.IsEnabled = true;

                    btnCancel.Content = Resource.Cancel;
                    break;
                case ProcessingState.shellExecuting:
                    // set buttons, control visibility
                    editControl.Visibility = System.Windows.Visibility.Collapsed;

                    btnSave.Visibility = btnSettings.Visibility = btnOk.Visibility = btnOkAndClose.Visibility = System.Windows.Visibility.Collapsed;
                    btnCancel.Content = Resource.Cancel;
                    break;
                case ProcessingState.shellExecuted:
                    btnCancel.IsEnabled = true;
                    btnCancel.Content = Resource.Close;
                    break;
                case ProcessingState.transformed:
                case ProcessingState.idle:
                    btnSave.IsEnabled = btnOk.IsEnabled = btnOkAndClose.IsEnabled = editControl.IsEnabled = btnSettings.IsEnabled = true;

                    if (_embedded)
                    {
                        btnSave.Visibility = btnOkAndClose.Visibility = btnSettings.Visibility = System.Windows.Visibility.Collapsed;
                        btnCancel.Content = Resource.Cancel;
                        btnCancel.IsEnabled = false;
                    }
                    else
                    {
                        if (!_transformer.Settings.NoSave && !_transformer.Settings.Test)
                            btnSave.Visibility = System.Windows.Visibility.Visible;
                        btnOkAndClose.Visibility = btnSettings.Visibility = System.Windows.Visibility.Visible;
                        btnCancel.Content = Resource.Close;
                        btnCancel.IsEnabled = true;
                    }

                    btnOk.Visibility = System.Windows.Visibility.Visible;

                    editControl.Visibility = System.Windows.Visibility.Visible;

                    break;
            }
        }

        /// <summary>
        /// cancel a running transform
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (!_transformer.Cancel())
            {
                _parent.ProcessingComplete(RanTransformOk ? ProcessingResult.close : ProcessingResult.canceled);
            }
            else
            {
                setButtonStates(ProcessingState.idle);
            }
        }


        /// <summary>
        /// run the transform
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnOk_Click(object sender, RoutedEventArgs e)
        {
            await doTransforms(true);
        }

        /// <summary>
        /// run the transform and close (if ok)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnOkAndClose_Click(object sender, RoutedEventArgs e)
        {
            var ret = await doTransforms(false);
            if ( ret == ProcessingResult.ok )
                _parent.ProcessingComplete(ret);
        }

        /// <summary>
        /// show the settings edit dialog
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            var aie = new ArrayItemEdit();
            aie.Owner = Window.GetWindow(this);
            aie.ShowDialog(_transformer.Settings.ConfigInfo);
        }

        /// <summary>
        /// initialize on loaded to make sure the gui log has correct context
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                _transformer.Initialize(_parms, _overrides, this);
                editControl.Load(_transformer.Model.Groups);
                setButtonStates(ProcessingState.idle);
                _parent.SetTitle(TitleSuffix);
            }
        }

        /// <summary>
        /// show a status message
        /// </summary>
        /// <param name="value"></param>
        internal void Report(ProgressInfo value)
        {
            if (value.PercentComplete == 100)
            {
                progress.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                if (progress.Visibility != System.Windows.Visibility.Visible)
                    progress.Visibility = System.Windows.Visibility.Visible;
                progress.Report(value);
            }
        }

        /// <summary>
        /// just save the current model out to the values file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            _transformer.Save();
        }
		
        public void Dispose()
        {
            _transformer.OnValuesSave -= _transformer_OnValuesSave;
            _transformer = null;
            _parent = null;
          
           this.Dispatcher.InvokeShutdown();
        }
    }
}
