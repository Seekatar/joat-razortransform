using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

namespace RazorTransform
{
    internal class RazorTransformer
    {
        private CancellationTokenSource _cts = null;

        public event EventHandler<string> OnValuesSave;

       

        TransformModel _model = new TransformModel();
        Settings _settings = new Settings();
        ITransformOutput _output = null;

        public bool Initialize(IDictionary<string, object> parms, IDictionary<string, string> overrides, MainEdit window = null)
        {
            try
            {
                _settings.SetParameters(parms);
                if (_settings.Run)
                    _output = new LogProgress(_settings.LogFile);
                else
                    _output = new GuiProgress(window);

                _settings.Load(overrides);
                return _model.Load(_settings);
            }
            catch (Exception settingsException)
            {
                // continue if output got initialize
                if (_output == null)
                {
                    _output = new GuiProgress(null);
                    _output.ShowMessage(String.Format(Resource.SettingsException, settingsException.BuildMessage()), MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return false;
                }
                _output.ShowMessage(String.Format(Resource.SettingsException, settingsException.BuildMessage()), MessageBoxButton.OK, MessageBoxImage.Exclamation );
            }

            return false;
        }

        public TransformModel Model { get { return _model; } }
        public ITransformOutput Output { get { return _output; } }
        public Settings Settings { get { return _settings; } }

        internal async Task<TransformResult> DoTransformAsync()
        {
            var ret = new TransformResult();
            if (!Directory.Exists(Settings.OutputFolder))
            {
                Output.ShowMessage( String.Format(Resource.DestMustExist, Settings.OutputFolder) , MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            else
            {
                try
                {
                    // save right away in case it errors out
                    Save();

                    var modelObject = _model.GetProperties(!_settings.Test, true, _settings.OutputFolder);

                    RazorFileTransformer rf = new RazorFileTransformer(modelObject);
                    _cts = new CancellationTokenSource();

                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    ret.Count = await rf.TransformFilesAsync(_settings.TemplateFolder, _settings.OutputFolder, !_settings.Test, _cts.Token, false, _output);
                    sw.Stop();
                    lock (this)
                    {
                        _cts = null;
                    }
                    ret.TranformResult = ProcessingResult.ok;
                    ret.Elapsed = sw.Elapsed;
                }
                catch (OperationCanceledException)
                {
                    _output.ShowMessage(Resource.Canceled, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ee)
                {
                    _output.ShowMessage(String.Format(Resource.ExceptionFormat, ee.BuildMessage()), MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                finally
                {
                    lock (this)
                    {
                        _cts = null;
                    }
                }

            }
            return ret;
        }

        /// <summary>
        /// build the model from the values and save it to the file
        /// </summary>
        /// <returns></returns>
        internal void Save()
        {
            if (!_settings.Test && !_settings.NoSave)
            {
                var body = _model.Save(_settings.ValuesFile);
                if (OnValuesSave != null)
                {
                    OnValuesSave(this, body);
                }

            }
        }

        /// <summary>
        /// cancel a running transform
        /// </summary>
        /// <returns></returns>
        internal bool Cancel()
        {
            lock (this)
            {
                if (_cts != null)
                {
                    _cts.Cancel();
                    return true;
                }
            }
            return false;
        }
    }

}
