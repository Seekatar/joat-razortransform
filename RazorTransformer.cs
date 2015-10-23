using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using RtPsHost;
using System.Linq;
using RazorTransform.Model;
using System.Xml.Linq;
using System.Text;

namespace RazorTransform
{
    internal class RazorTransformer
    {
        private CancellationTokenSource _cts = null;

        public event EventHandler<string> OnValuesSave;

        public static RazorTransformer _instance = null;

        public static RazorTransformer Instance { get { return _instance; } }

        public RazorTransformer() { _instance = this; }

        Settings _settings = new Settings();
        ITransformOutput _output = null;

        public IModel Model { get { return _model; } }
        public ITransformOutput Output { get { return _output; } }
        public Settings Settings { get { return _settings; } }

        IModel _model = new RazorTransform.Model.Model();

        /// <summary>
        /// load the settings, model and values from disk
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="overrides"></param>
        /// <param name="window"></param>
        /// <returns></returns>
        public async Task<bool> InitializeAsync(IDictionary<string, object> parms, IDictionary<string, string> overrides, MainEdit window = null)
        {
            try
            {
                _settings.SetParameters(parms);
                if (_settings.Run)
                    _output = new LogProgress(new ProgressInfo(_settings.LogFile));
                else
                    _output = new GuiProgress(window);

                _settings.Load(overrides);
                _settings.PowerShellConfig.Run = parms.ContainsKey("powerShell");

                List<Exception> warnings = new List<Exception>();
                bool ret = ModelConfig.Instance.Load(_settings, warnings:warnings);
                if (ret)
                {
                    _model.LoadFromXml(ModelConfig.Instance.Root, ModelConfig.Instance.ValuesRoot, overrides);

                    ModelConfig.Instance.OnModelLoaded(new ModelLoadedArgs(_model));
                }

                if (ret)
                {
                    // refresh the model to update any @Model values in values file if they changed manually since last save
                    // TODO could use a checksum embedded in to see if changed since last time.
                    var task = await RefreshModelAsync(false, true); // don't validate, assume dirty 
                }
                if (warnings.Count > 0)
                    throw new AggregateException(warnings);
                return ret;
            }
            catch (Exception settingsException)
            {
                // continue if output got initialize
                if (_output == null)
                {
                    if (_settings.Run)
                        _output = new LogProgress(new ProgressInfo(_settings.LogFile));
                    else
                        _output = new GuiProgress(window);

                    _output.ShowMessage(String.Format(Resource.SettingsException, settingsException.BuildMessage()), MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return false;
                }
                _output.ShowMessage(String.Format(Resource.SettingsException, settingsException.BuildMessage()), MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }

            return false;
        }

        internal async Task<TransformResult> DoTransformAsync(bool dirty)
        {
            var ret = new TransformResult();
            if (!Directory.Exists(Settings.OutputFolder))
            {
                // try to create it
                try
                {
                    Directory.CreateDirectory(Settings.OutputFolder);
                }
                catch (Exception) { }
                if (!Directory.Exists(Settings.OutputFolder))
                {
                    Output.ShowMessage(String.Format(Resource.DestMustExist, Settings.OutputFolder), MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    ret.Result = ProcessingResult.failed;
                    return ret;
                }
            }

            try
            {
                // save right away in case it errors out
                var modelObject = await SaveAsync(true, dirty); // validate, only save if dirty

                if (modelObject != null)
                {
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
                    ret.Result = ProcessingResult.ok;
                    ret.Elapsed = sw.Elapsed;
                }

            }
            catch (OperationCanceledException)
            {
                _output.ShowMessage(Resource.Canceled, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            //catch (ValidationError ve)
            //{
            //    _output.ShowMessage(ve.ToString(), MessageBoxButton.OK, MessageBoxImage.Exclamation);

            //}
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

            return ret;
        }

        /// <summary>
        /// build the model from the values and save it to the file
        /// </summary>
        /// <returns></returns>
        internal async Task<object> SaveAsync(bool validateModel = true, bool dirty = true)
        {
            if (!_settings.Test && !_settings.NoSave && _model.Items.Any())
            {
                // add destinationFolder to the model since we sneak it in for transforms.  That way if someone needs it
                // after the transform, it's there.
                var dest = _model.Items.SingleOrDefault(o => o is IItem && o.Name == Constants.DestinationFolder) as IItem;
                if (dest != null)
                {
                    if (!dirty && !String.Equals(_settings.OutputFolder, dest.Value, StringComparison.CurrentCultureIgnoreCase))
                        dirty = true;
                    dest.Value = _settings.OutputFolder;

                }

                var docModel = await RefreshModelAsync(validateModel, dirty);
                if (docModel.Item1 != null)
                {

                    docModel.Item1.Save(Settings.ValuesFile);

                    if (OnValuesSave != null)
                    {
                        OnValuesSave(this, docModel.Item1.ToString());
                    }
                    return docModel.Item2;
                }
            }
            return null;
        }

        // generate the RtValues Xml for the model, with Settings on the front.
        string generateXml()
        {
            var doc = XDocument.Parse(String.Format("<RtValues {0}=\"{1}\"/>", Constants.Version, Constants.CurrentRtValuesVersion));
            var root = doc.Root;

            _settings.Model.GenerateXml(root);

            _model.GenerateXml(root);

            ModelConfig.Instance.OnModelSaved(new ModelChangedArgs(_model));

            return doc.ToString();
        }

        internal async Task<Tuple<System.Xml.Linq.XDocument, object>> RefreshModelAsync(bool validateModel, bool dirty)
        {
            object modelObject = null;

                modelObject = _model;

                RazorFileTransformer rf = new RazorFileTransformer(modelObject);
                if (dirty)
                {
                    _cts = new CancellationTokenSource();

                    try
                    {
                        await rf.SubstituteValuesAsync(_cts.Token, Settings.Run ? null : _output);  // don't show substitute progress if running w/o UI
                    }
                    catch (Exception e)
                    {
                        Output.ShowMessage("Error updating values", MessageBoxButton.OK, MessageBoxImage.Error, e.Message);
                    }
                    finally
                    {
                        lock (this)
                        {
                            _cts = null;
                        }
                    }
                }

                // validate after doing substitutions
                if (validateModel)
                {
                    var errors = new List<ValidationError>();
                    _model.Validate(errors);
                    if (errors.Any())
                    {
                        var sb = new StringBuilder();
                        foreach (var e in errors)
                        {
                            sb.AppendLine(e.ToString());
                        }
                        Output.ShowMessage(Resource.ValidationError, secondaryMsg: sb.ToString(), messageBoxImage: MessageBoxImage.Asterisk);
                        return new Tuple<System.Xml.Linq.XDocument, object>(null, null);
                    }
                }


                var body = generateXml();

                // do any substitutions in  XML
                if (!String.IsNullOrWhiteSpace(body)) // if not saving, this will be empty
                {
                    var doc = System.Xml.Linq.XDocument.Parse(body);
                    return new Tuple<System.Xml.Linq.XDocument, object>(doc, modelObject);
                }
                else
                    return new Tuple<System.Xml.Linq.XDocument, object>(null, null);
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
                    _cts = null;
                    return true;
                }
            }
            return false;
        }
    }

}
