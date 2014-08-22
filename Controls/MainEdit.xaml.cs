using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using RtPsHost;
using RazorTransform.Model;
using System.Linq;

namespace RazorTransform
{
    /// <summary>
    /// Interaction logic for MainEdit.xaml
    /// </summary>
    public partial class MainEdit : UserControl, IDisposable
    {
        private RazorTransformer _transformer = new RazorTransformer();
        private IDictionary<string, object> _parms;
        private IDictionary<string, string> _overrides;
        private ITransformParentWindow _parent;
        private bool _embedded = false; // are we embedded in another app (hide certain buttons)
        private bool _runPowerShell = false;
        private string _logFileName = "Deploy";
        private string _workingDir = "..";
        private string _scriptFname = "PsScripts.xml";
        private bool _step = false;
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
        /// Only invoke this constructor if using this control as an AddIn.
        /// </summary>
        /// <param name="parent">class for calling back</param>
        /// <param name="overrides">additional parameter</param>
        public MainEdit(ITransformParentWindow parent, IDictionary<string, object> parms, IDictionary<string, string> overrides)
            : this()
        {
            this.Initalize(parent, parms, overrides, true, true);
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
                return Settings.Instance.TitleSuffix;
            }
        }

        /// <summary>
        /// load all the data from the model
        /// </summary>
        /// <param name="list"></param>
        internal void Load(IModel model)
        {
            editControl.Load(model, _transformer.Settings.ShowHidden);
        }

        public void Initalize(ITransformParentWindow parent, IDictionary<string, object> parms, IDictionary<string, string> overrides, bool embedded = false, bool runPowerShell = false)
        {
            _embedded = embedded;
            _parent = parent;
            _parms = parms;
            _overrides = overrides;
            _runPowerShell = runPowerShell || _parms.ContainsKey("PowerShell");

            // any overrides?
            if (_overrides.ContainsKey("PsScriptFileName"))
                _scriptFname = _overrides["PsScriptFileName"];
            if (_overrides.ContainsKey("PsLogFileName"))
                _logFileName = _overrides["PsLogFileName"];
            if (_overrides.ContainsKey("PsStep") && !Boolean.TryParse(_overrides["PsStep"], out _step))
                _step = false;
            if (_overrides.ContainsKey("PsWorkingDir"))
                _workingDir = _overrides["PsWorkingDir"];
            _workingDir = System.IO.Path.GetFullPath(_workingDir); // convert .. to path
            _logFileName = String.Format("{0}_{1}.log", _logFileName, DateTime.Now.ToString("yyMMdd-HHmmss"));

            _workingDir = Path.GetFullPath(_workingDir);
            _scriptFname = Path.GetFullPath(_scriptFname);
            _logFileName = Path.GetFullPath(_logFileName);
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
            transformResult.Result = ProcessingResult.ok;

            if (!_overrides.ContainsKey("PsSkipTransform"))
            {
                transformResult = await _transformer.DoTransformAsync(editControl.Dirty);
                editControl.Dirty = false;
            }

            if (transformResult.Result == ProcessingResult.ok)
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

                    if (_embedded || _runPowerShell)
                        psConsole.WriteSystemMessage(msg);
                    else
                        _transformer.Output.ShowMessage(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }

            if (RanTransformOk && _runPowerShell)
            {
                RanTransformOk = false;
                psConsole.WriteLine("", WriteType.Host);
                psConsole.WriteSystemMessage(Resource.RunningPowerShell);
                _parent.SendData(new Dictionary<string, string>
                {
                    {"PSStart", DateTime.Now.Ticks.ToString() }
                });
                transformResult.Result = await runPowerShell();
                _parent.SendData(new Dictionary<string, string>
                {
                    {"PSEnd", DateTime.Now.Ticks.ToString() }
                });
                RanTransformOk = transformResult.Result == ProcessingResult.ok;
            }
            else
                setButtonStates(ProcessingState.transformed);

            return transformResult.Result;
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

            var endHeight = Math.Max(editControl.ActualHeight, psConsole.ActualHeight);
            var endWidth = Math.Max(editControl.ActualWidth, psConsole.ActualWidth);

            _currentState = currentState;
            switch (currentState)
            {
                case ProcessingState.transforming:
                    if (_runPowerShell)
                    {
                        editControl.Visibility = System.Windows.Visibility.Collapsed;

                        zoomConsole(0, endHeight, 0, endWidth);
                    }
                    btnSave.IsEnabled = btnOk.IsEnabled = btnOkAndClose.IsEnabled = editControl.IsEnabled = btnSettings.IsEnabled = btnRefresh.IsEnabled = false;
                    btnCancel.IsEnabled = true;

                    btnCancel.ToolTip = Resource.Cancel;
                    break;
                case ProcessingState.shellExecuting:
                    // set buttons, control visibility
                    editControl.Visibility = System.Windows.Visibility.Collapsed;
                    zoomConsole(0, endHeight, 0, endWidth);

                    btnSave.Visibility = btnSettings.Visibility = btnSettings.Visibility = btnOk.Visibility = btnOkAndClose.Visibility = System.Windows.Visibility.Collapsed;
                    btnCancel.ToolTip = Resource.Cancel;
                    break;
                case ProcessingState.shellExecuted:
                    btnCancel.IsEnabled = true;
                    btnCancel.ToolTip = Resource.Close;
                    break;
                case ProcessingState.transformed:
                case ProcessingState.idle:
                    btnSave.IsEnabled = btnOk.IsEnabled = btnOkAndClose.IsEnabled = editControl.IsEnabled = btnSettings.IsEnabled = btnRefresh.IsEnabled = true;

                    if (_embedded)
                    {
                        btnSave.Visibility = btnOkAndClose.Visibility = btnSettings.Visibility = System.Windows.Visibility.Collapsed;
                        btnCancel.ToolTip = Resource.Cancel;
                        btnCancel.IsEnabled = false;
                    }
                    else
                    {
                        if (!_transformer.Settings.NoSave && !_transformer.Settings.Test)
                            btnSave.Visibility = System.Windows.Visibility.Visible;
                        btnOkAndClose.Visibility = btnSettings.Visibility = System.Windows.Visibility.Visible;
                        btnCancel.ToolTip = Resource.Close;
                        btnCancel.IsEnabled = true;
                    }

                    btnOk.Visibility = System.Windows.Visibility.Visible;

                    zoomConsole(psConsole.ActualHeight, 0, psConsole.ActualWidth, 0);
                    editControl.Visibility = System.Windows.Visibility.Visible;

                    break;
            }
        }

        /// <summary>
        /// show/hide the PS console with an animation
        /// </summary>
        /// <param name="startHeight"></param>
        /// <param name="endHeight"></param>
        /// <param name="startWidth"></param>
        /// <param name="endWidth"></param>
        /// <param name="durationMs"></param>
        private void zoomConsole(double startHeight, double endHeight, double startWidth, double endWidth, double durationMs = 300)
        {
            if (psConsole.ActualWidth == endWidth && psConsole.ActualHeight == endHeight)
                return;

            var sb = new Storyboard();
            var heightAnim = new DoubleAnimation(startHeight, endHeight, new Duration(TimeSpan.FromMilliseconds(durationMs)));
            Storyboard.SetTarget(heightAnim, psConsole);
            Storyboard.SetTargetProperty(heightAnim, new PropertyPath(Window.HeightProperty));

            var widthAnim = new DoubleAnimation(startWidth, endWidth, new Duration(TimeSpan.FromMilliseconds(durationMs)));
            Storyboard.SetTarget(widthAnim, psConsole);
            Storyboard.SetTargetProperty(widthAnim, new PropertyPath(Window.WidthProperty));

            heightAnim.EasingFunction = new ExponentialEase();
            widthAnim.EasingFunction = new ExponentialEase();
            sb.Children.Add(heightAnim);
            sb.Children.Add(widthAnim);

            psConsole.Height = startHeight;
            psConsole.Width = startWidth;

            if (endHeight == 0 || endWidth == 0)
            {
                var timer = new System.Timers.Timer(durationMs);

                var current = SynchronizationContext.Current;
                timer.Elapsed += (s, e) =>
                {
                    timer.Stop();
                    current.Post(_ =>
                    {
                        psConsole.Visibility = System.Windows.Visibility.Collapsed;
                    }, null);
                }; // timer_Elapsed;
                timer.Start();
            }
            else
            {
                psConsole.Clear();
                psConsole.Visibility = System.Windows.Visibility.Visible;
            }
            sb.Begin(psConsole);
        }

        /// <summary>
        /// grab all the password to set as variables in the PsHost to avoid writing them to disk
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, object> variableItems()
        {
            var dict = new Dictionary<string, object>();
            dict["PsLogFileName"] = _logFileName;
            dict["PsWorkingDir"] = _workingDir;
            dict["PsStep"] = _step ? "$True" : "$False";
            if (_overrides.ContainsKey("InstanceDbName"))
            {
                dict["instanceDbName"] = _overrides["InstanceDbName"];
            }
            variableItems(_transformer.Model.Items, dict);

            return dict;
        }

        private void variableItems(IEnumerable<IItemBase> items, Dictionary<string, object> dict)
        {
            foreach (var i in items.OfType<IItem>())
            {
                if (i.IsPassword)
                    dict[i.Name] = i.Value;
                else if (i.Name == "SQLServer" || i.Name == "InstanceName")
                    dict[i.Name] = i.Value;
            }
            foreach (var i in items.OfType<IItemList>())
            {
                foreach (var m in i)
                {
                    variableItems(m.Items, dict);
                }
            }
        }

        private async Task<ProcessingResult> runPowerShell()
        {
            ProcessingResult ret = ProcessingResult.failed;

            setButtonStates(ProcessingState.shellExecuting);

            var curDir = Directory.GetCurrentDirectory();
            if (Directory.Exists(_workingDir))
            {
                try
                {
                    // this doesn't appear to set it in the PSHost!
                    Directory.SetCurrentDirectory(_workingDir);
                    psConsole.WriteSystemMessage("Working dir set to " + _workingDir);
                    if (File.Exists(_scriptFname))
                    {
                        // invoke all the scripts
                        ret = (ProcessingResult)await psConsole.InvokeAsync(_scriptFname, _logFileName, _step, variableItems());
                    }
                    else
                    {
                        _transformer.Output.ShowMessage(String.Format(Resource.FileNotFound, _scriptFname));
                    }
                }
                finally
                {
                    Directory.SetCurrentDirectory(curDir);
                }
            }
            else
            {
                _transformer.Output.ShowMessage(String.Format(Resource.FolderNotFound, _workingDir));
            }

            setButtonStates(ProcessingState.shellExecuted);

            return ret;
        }
        /// <summary>
        /// cancel a running transform
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            btnCancel.IsEnabled = false;
            if (_currentState == ProcessingState.shellExecuting)
            {
                psConsole.Cancel(); // this is async
            }
            else if (_currentState == ProcessingState.shellExecuted && !RanTransformOk)
            {
                setButtonStates(ProcessingState.idle); // show Artie again if PS failed
            }
            else if (!_transformer.Cancel())
            {
                bool okToClose = true;
                if (editControl.Dirty)
                {
                    var resp = _transformer.Output.ShowMessage(String.Format(Resource.IsDirty, _transformer.Settings.ValuesFile), MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                    if (resp == MessageBoxResult.Yes)
                    {
                        if ((await _transformer.SaveAsync(true, editControl.Dirty)) == null)  // failed validation don't exit
                        {
                            okToClose = false;
                        }
                    }
                    else if (resp == MessageBoxResult.Cancel)
                        okToClose = false;

                    // else No, so close window without saving
                }

                if (okToClose)
                    _parent.ProcessingComplete(RanTransformOk ? ProcessingResult.close : ProcessingResult.canceled);
                else
                    btnCancel.IsEnabled = true;
            }
            else
            {
                setButtonStates(ProcessingState.idle);
            }
        }


        /// <summary>
        /// run the transform, Go button
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
            if (ret == ProcessingResult.ok && !_runPowerShell)
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
            aie.TrySetOwner(Window.GetWindow(this));
            aie.ShowDialog(_transformer.Settings.Model, _transformer.Settings.ShowHidden);
            if (aie.Dirty)
                editControl.Dirty = true;
        }

        /// <summary>
        /// initialize on loaded to make sure the gui log has correct context
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                psConsole.SyncContext = SynchronizationContext.Current;
                psConsole.Owner = Window.GetWindow(this);
                if (_runPowerShell)
                {
                    // init for powershell, then do init again below since script may have touched input
                    var oldValue = _transformer.Settings.Run;
                    _transformer.Settings.Run = true; // set run to true to avoid popups
                    await _transformer.InitializeAsync(_parms, _overrides, this);
                    _transformer.Settings.Run = oldValue;

                    psConsole.SyncContext = null;
                    psConsole.Initialize();
                    var result = await psConsole.InvokeAsync(_scriptFname, _logFileName, _step, variableItems(), ScriptInfo.ScriptType.preRun);
                }
                if (!String.IsNullOrWhiteSpace(_transformer.Settings["RTSettings_PSForeground"]) && !String.IsNullOrWhiteSpace(_transformer.Settings["RTSettings_PSBackground"]))
                {
                    psConsole.SetColors((Color)ColorConverter.ConvertFromString(_transformer.Settings["RTSettings_PSForeground"]), (Color)ColorConverter.ConvertFromString(_transformer.Settings["RTSettings_PSBackground"]));
                }
                await _transformer.InitializeAsync(_parms, _overrides, this); // reinit after running pre script
                editControl.Load(_transformer.Model, _transformer.Settings.ShowHidden);
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
        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            setButtonStates(ProcessingState.transforming);
            if (await _transformer.SaveAsync(true, editControl.Dirty) != null)
                editControl.Dirty = false;
            setButtonStates(ProcessingState.idle);
        }

        /// <summary>
        /// refresh the model
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await _transformer.RefreshModelAsync(false, true);
            // reload 
            editControl.Load(_transformer.Model, _transformer.Settings.ShowHidden);
        }

        public void Dispose()
        {
            if (_transformer != null)
            {
                _transformer.OnValuesSave -= _transformer_OnValuesSave;
                _transformer = null;
            }
            _parent = null;
            if (this.psConsole != null)
                this.psConsole.Dispose();

            if (this.Dispatcher != null)
                this.Dispatcher.InvokeShutdown();
        }

    }
}
