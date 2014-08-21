#define serial
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using RtPsHost;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using RazorTransform.Model;

namespace RazorTransform
{
    /// <summary>
    /// class for running Razor transform on a set of files
    /// given an ExpandoObject
    /// </summary>
    class RazorFileTransformer
    {
        dynamic _model;

        private int transformFiles(string inputMask, string outputFolder, bool saveFiles, bool recursive, CancellationToken cancel, IProgress<ProgressInfo> progress)
        {
            string currentFile = String.Empty;
            int count = 0;
            try
            {
                string folder = inputMask;
                string mask = "*.*";

                if (!Directory.Exists(inputMask))
                {
                    folder = Path.GetDirectoryName(inputMask);
                    mask = Path.GetFileName(inputMask);
                }

                if (progress != null) progress.Report(new ProgressInfo(Resource.StartingTransforms));

                var fileList = Directory.EnumerateFiles(folder, mask).OrderBy(o => o).ToList();

                var sw = new Stopwatch();
                sw.Start();

                int i = 0;
                var processing = Resource.ProcessingTransforms;
#if USE_PARALLEL				
                Parallel.ForEach(fileList, f =>
#else
                foreach (var f in fileList)
#endif
                {
                    Interlocked.Increment(ref i);
                    currentFile = f;

                    cancel.ThrowIfCancellationRequested();

                    if (progress != null) progress.Report(new ProgressInfo(processing, currentOperation: f, percentComplete: (100 * i / fileList.Count) - 1));

                    string template = File.ReadAllText(f);
                    var content = RazorTemplateUtil.Transform(_model, template);
                    if (content == null)
                        throw new Exception(String.Format(Resource.TransformNoContent, currentFile));

                    if (content.Contains("@Model.") || content.Contains("@(")) // allow one level of nesting of @Model. or @(
                        content = RazorTemplateUtil.Transform(_model, HttpUtility.HtmlDecode(content));

                    if (saveFiles)
                        File.WriteAllText(Path.Combine(outputFolder, Path.GetFileName(f)), content);

                    Interlocked.Increment(ref count);
                }
#if USE_PARALLEL				
				);
#endif
                sw.Stop();
                if (progress != null) progress.Report(new ProgressInfo(String.Format(Resource.Success, count, outputFolder, sw.Elapsed.TotalSeconds), percentComplete: 100));

            }
            catch (RazorEngine.Templating.TemplateCompilationException e)
            {
                var sb = new StringBuilder();
                sb.AppendFormat("Error processing file {0} {1}", Path.GetFileName(currentFile), e.Message);
                foreach (var ee in e.Errors)
                {
                    sb.AppendLine(String.Format("   {0} at line {1}({2})", ee.ErrorText, ee.Line, ee.Column));
                }
                var fname = Path.Combine(System.IO.Path.GetTempPath(), "RazorTransform.xml");
                try
                {
                    System.IO.File.WriteAllText(fname, e.SourceCode);
                    sb.AppendLine("    Output written to " + fname);
                }
                catch (Exception) { }

                throw new Exception(sb.ToString());
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ee)
            {
                var sb = new StringBuilder();
                sb.AppendFormat("Error processing file {0}", Path.GetFileName(currentFile));
                throw new Exception(sb.ToString(), ee);
            }
            finally
            {
                if (progress != null)
                    progress.Report(new ProgressInfo(String.Empty, percentComplete: 100));
            }
            return count;
        }

        // constructor 
        // make nested substitutions first to avoid problems with transforming them
        public RazorFileTransformer(dynamic model)
        {
            _model = model;
        }

        /// <summary>
        /// @Model mappings that were substituted during the transform.
        /// </summary>
        private IDictionary<string, string> _substituteMapping = new Dictionary<string, string>();

        private int substituteValues(IModel model, CancellationToken cancel, IProgress<ProgressInfo> progress = null, int depth = 0, string nestedName = null)
        {
            Regex r = new Regex(@"@\({0,1}Model\.[\w\.]+\){0,1}");

            int max = Math.Max(1, model.Items.Count);
            int changeCount = 0;
            int count = 0;

            var scanning = Resource.ScanningDependents;
            if (nestedName != null)
                scanning += " " + nestedName;
            if (progress != null)
                progress.Report(new ProgressInfo(scanning));

            try
            {
#if serial
                foreach (var g in model.Items)
#else
                Parallel.ForEach( model.Groups, g =>
#endif
                {
                    if (progress != null)
                    {
                        progress.Report(new ProgressInfo(scanning, currentOperation: g.DisplayName, percentComplete: (count * 100 / max)));
                    }

                    if (g is IItemList)
                    {
                        foreach (var a in (g as IItemList))
                        {
                            changeCount += substituteValues(a, cancel, null, ++depth, g.DisplayName);
                        }
                    }
                    else if ( g is IItem )
                    {
                        var i = g as IItem;
                        {
                            var subst = i.ValueStr != null ? i.ValueStr : String.Empty;

                            var matches = r.Matches(subst);
                            if (matches.Count > 0)
                            {
                                foreach (Match match in r.Matches(subst))
                                {
                                    string errorMessage = null;

                                    string result = null;
                                    lock (_substituteMapping)
                                    {
                                        if (_substituteMapping.ContainsKey(match.Value))
                                            result = _substituteMapping[match.Value];
                                    }
                                    if (result == null)
                                    {
                                        result = RazorTemplateUtil.TryTransform(model, HttpUtility.HtmlDecode(match.Value), out errorMessage, match.Value);
                                        if (!String.IsNullOrEmpty(errorMessage))
                                        {
                                            throw new Exception(errorMessage);
                                        }
                                        lock (_substituteMapping)
                                        {
                                            if (match.Value.Contains(".Root.") || depth == 0)
                                                _substituteMapping[match.Value] = result;
                                        }
                                    }
                                    if (result != null)
                                        subst = subst.Replace(match.Value, result);

                                }
                                i.ExpandedValueStr = subst;
                            }

                        }
                    }
                    Interlocked.Increment(ref count);
                }
#if !serial
                );
#endif
            }
            finally
            {
                if (progress != null)
                    progress.Report(new ProgressInfo(scanning, percentComplete: 100));
            }
            return changeCount;
        }

        /// <summary>
        /// Synchronous transform
        /// </summary>
        /// <param name="outputFolder"></param>
        /// <param name="recursive"></param>
        /// <param name="progress"></param>
        public int TransformFiles(string inputMask, string outputFolder, bool saveFiles = true, bool recursive = false, IProgress<ProgressInfo> progress = null)
        {
            return transformFiles(inputMask, outputFolder, saveFiles, recursive, CancellationToken.None, progress);
        }

        public Task<int> TransformFilesAsync(string inputMask, string outputFolder, bool saveFiles, CancellationToken cancel, bool recursive = false, IProgress<ProgressInfo> progress = null)
        {
            return Task.Run(() =>
            {
                return transformFiles(inputMask, outputFolder, saveFiles, recursive, cancel, progress);
            });
        }

        public Task SubstituteValuesAsync(CancellationToken cancel, IProgress<ProgressInfo> progress = null)
        {
            return Task.Run(() =>
            {
                _substituteMapping.Clear();

                // first do any values that have @Model in them
                for (int i = 0; i < 5; i++) // allow 5 levels of nesting
                {
                    var model = new RazorTransform.Model.Model();
                    foreach( var j in _model.Items)
                        model.Items.Add(j);
                    if (substituteValues(model, cancel, progress) == 0)
                        break;
                }
            });
        }

    }
}
