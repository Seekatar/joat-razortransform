using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using RtPsHost;

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
                foreach (var f in fileList)
                {
                    i++;
                    currentFile = f;

                    cancel.ThrowIfCancellationRequested();

                    if (progress != null) progress.Report(new ProgressInfo(processing, currentOperation: f, percentComplete: (100 * i / fileList.Count)-1));

                    string template = File.ReadAllText(f);
                    var content = RazorTemplateUtil.Transform(_model,template);
                    if (content == null)
                        throw new Exception(String.Format(Resource.TransformNoContent, currentFile));

                    if (content.Contains("@Model.") || content.Contains("@(")) // allow one level of nesting of @Model. or @(
                        content= RazorTemplateUtil.Transform(_model, HttpUtility.HtmlDecode(content));

                    if (saveFiles)
                        File.WriteAllText(Path.Combine(outputFolder, Path.GetFileName(f)), content);

                    count++;
                }
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
                var fname = Path.Combine( System.IO.Path.GetTempPath(), "RazorTransform.xml");
                try 
                {
                    System.IO.File.WriteAllText(fname, e.SourceCode);
                    sb.AppendLine("    Output written to " + fname);
                }
                catch(Exception) {}
                
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

        private int substituteValues(dynamic model, CancellationToken cancel, IProgress<ProgressInfo> progress = null )
        {
            var ex = model as System.Dynamic.ExpandoObject as System.Collections.Generic.IDictionary<string, object>;

            int changeCount = 0;
            var changes = new System.Collections.Generic.Dictionary<string, object>();
            int max = Math.Max(1,ex.Count);
            int count = 0;
            var scanning = Resource.ScanningDependents;
            if (progress != null)
                progress.Report(new ProgressInfo(scanning));
            try
            {
                foreach (var kv in ex)
                {
                    if (progress != null)
                        progress.Report(new ProgressInfo(scanning, currentOperation: kv.Key, percentComplete: (++count * 100 / max)));

                    if (kv.Value is System.Collections.Generic.IList<System.Dynamic.ExpandoObject>)
                    {
                        foreach (var e in kv.Value as System.Collections.Generic.IList<System.Dynamic.ExpandoObject>)
                        {
                            changeCount += substituteValues(e, cancel);
                        }
                    }
                    else if (kv.Value.ToString().Contains("@Model.") || kv.Value.ToString().Contains("@(")) // allow one level of nesting of @Model. or @(
                    {
                        string errorMessage = null;
                        var result = RazorTemplateUtil.TryTransform(model, HttpUtility.HtmlDecode(kv.Value.ToString()), out errorMessage);
                        if (!String.IsNullOrEmpty(errorMessage))
                        {
                            throw new Exception(errorMessage);
                        }
                        changes.Add(kv.Key, result);
                    }
                }
            }
            finally
            {
                if (progress != null)
                    progress.Report(new ProgressInfo(scanning, percentComplete: 100));
            }

            if (changes.Count > 0)
            {
                foreach (var c in changes)
                {
                    ex[c.Key] = c.Value;
                }
                changeCount += changes.Count;
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

        public Task<int> TransformFilesAsync( string inputMask, string outputFolder, bool saveFiles, CancellationToken cancel, bool recursive = false, IProgress<ProgressInfo> progress = null )
        {
            return Task.Run(() =>
            {             
                // first do any values that have @Model in them
                for (int i = 0; i < 5; i++) // allow 5 levels of nesting
                {
                    if (substituteValues(_model, cancel, progress) == 0)
                        break;
                }
                return transformFiles(inputMask, outputFolder, saveFiles, recursive, cancel, progress);
            });
        }
    }
}
