using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using RtPsHost;
using System.Linq;

namespace RazorTransform
{
    internal class RazorTransformer
    {
        private CancellationTokenSource _cts = null;

        public event EventHandler<string> OnValuesSave;



        TransformModel _model = new TransformModel(true);
        Settings _settings = new Settings();
        ITransformOutput _output = null;

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
                bool ret = _model.Load(_settings);
                if ( ret )
                {
                    var task = await RefreshModelAsync(false);
                    ret = _model.Load(_settings, task.Item1.Root);
                }
                return ret;
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
                _output.ShowMessage(String.Format(Resource.SettingsException, settingsException.BuildMessage()), MessageBoxButton.OK, MessageBoxImage.Exclamation);
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
                Output.ShowMessage(String.Format(Resource.DestMustExist, Settings.OutputFolder), MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            else
            {
                try
                {
                    // save right away in case it errors out
                    var modelObject = await SaveAsync();

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
                        ret.TranformResult = ProcessingResult.ok;
                        ret.Elapsed = sw.Elapsed;
                    }

                }
                catch (OperationCanceledException)
                {
                    _output.ShowMessage(Resource.Canceled, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (ValidationException ve)
                {
                    _output.ShowMessage(ve.ToString(), MessageBoxButton.OK, MessageBoxImage.Exclamation);

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
        internal async Task<System.Dynamic.ExpandoObject> SaveAsync(bool validateModel = true)
        {
            if (!_settings.Test && !_settings.NoSave)
            {
                // add this to the model since we sneak it in for transforms.  That way if someone needs it
                // after the transform, it's there.
                var dest = _model.Groups[0].Children.Where(o => o.PropertyName == "DestinationFolder").SingleOrDefault();
                if (dest != null)
                    dest.Value = _settings.OutputFolder;

                var docModel = await RefreshModelAsync(validateModel);
                if ( docModel.Item1 != null )
                {
                    docModel.Item1.Save(Settings.ValuesFile);

                    if (OnValuesSave != null)
                    {
                        OnValuesSave(this, docModel.ToString());
                    }
                    return docModel.Item2;
                }
            }
            return null;
        }

        private async Task<Tuple<System.Xml.Linq.XDocument,System.Dynamic.ExpandoObject>> RefreshModelAsync(bool validateModel)
        {
            System.Dynamic.ExpandoObject modelObject = null;

            var body = _model.GenerateXml();

            if (!String.IsNullOrWhiteSpace(body)) // failed extension validation?
            {
                try
                {
                    modelObject = _model.GetProperties(!_settings.Test, false, _settings.OutputFolder, validateModel);
                }
                catch (ValidationException e)
                {
                    Output.ShowMessage(Resource.ValidationError, MessageBoxButton.OK, MessageBoxImage.Asterisk, String.Format(Resource.ValidationMessage, e.ToString()));
                    return null;
                }

                RazorFileTransformer rf = new RazorFileTransformer(modelObject);
                _cts = new CancellationTokenSource();

                await rf.SubstituteValuesAsync(_cts.Token, Settings.Run ? null : _output );  // don't show substitute progress if running w/o UI

                lock (this)
                {
                    _cts = null;
                }

                // do any substitutions in  XML
                if (!String.IsNullOrWhiteSpace(body)) // if not saving, this will be empty
                {
                    var r = new System.Text.RegularExpressions.Regex(@"@\({0,1}Model\.[\w\.]+\){0,1}");
                    var doc = System.Xml.Linq.XDocument.Parse(body);
                    foreach (var x in doc.Root.Descendants())
                    {
                        if (!x.HasElements && x.Value.Contains("Model."))
                        {
                            // save off @Model.. in orig for next time
                            x.Add(new System.Xml.Linq.XAttribute(Constants.Original, x.Value));
                            var matches = r.Matches(x.Value);
                            foreach (System.Text.RegularExpressions.Match m in matches)
                            {
                                if (rf.SubstituteMapping.ContainsKey(m.Value))
                                {
                                    x.Value = x.Value.Replace(m.Value, rf.SubstituteMapping[m.Value]);
                                }
                            }
                        }
                    }
                    return new Tuple<System.Xml.Linq.XDocument,System.Dynamic.ExpandoObject>(doc,modelObject);
                }
            }
            return new Tuple<System.Xml.Linq.XDocument,System.Dynamic.ExpandoObject>(null,null);
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
