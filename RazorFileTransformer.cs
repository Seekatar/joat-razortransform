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

                if (progress != null) progress.Report(new ProgressInfo("Starting transforms"));

                var fileList = Directory.EnumerateFiles(folder, mask).OrderBy(o => o).ToList();

                var sw = new Stopwatch();
                sw.Start();

                int i = 0;
                foreach (var f in fileList)
                {
                    i++;
                    currentFile = f;

                    cancel.ThrowIfCancellationRequested();

                    if (progress != null) progress.Report(new ProgressInfo("Processing transforms...", currentOperation: f, percentComplete: 100 * i / fileList.Count));

                    string template = File.ReadAllText(f);
                    string content = RazorEngine.Razor.Parse(template, _model);

                    if (content == null)
                        throw new Exception("Transform returned no content");

                    if (content.Contains("@Model.") || content.Contains("@(")) // allow one level of nesting of @Model. or @(
                        content = RazorEngine.Razor.Parse(HttpUtility.HtmlDecode(content), _model);

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

        private int substituteValues(dynamic model)
        {
            var ex = model as System.Dynamic.ExpandoObject as System.Collections.Generic.IDictionary<string, object>;

            int changeCount = 0;
            var changes = new System.Collections.Generic.Dictionary<string, object>();
            foreach (var kv in ex)
            {
                if (kv.Value is System.Collections.Generic.IList<System.Dynamic.ExpandoObject>)
                {
                    foreach (var e in kv.Value as System.Collections.Generic.IList<System.Dynamic.ExpandoObject>)
                    {
                        changeCount += substituteValues(e);
                    }
                }
                else if (kv.Value.ToString().Contains("@Model.") || kv.Value.ToString().Contains("@(")) // allow one level of nesting of @Model. or @(
                {
                    changes.Add(kv.Key, RazorEngine.Razor.Parse(HttpUtility.HtmlDecode(kv.Value.ToString()), model));
                }
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
            // first do any values that have @Model in them
            for (int i = 0; i < 5; i++) // allow 5 levels of nesting
            {
                if (substituteValues(_model) == 0)
                    break;
            }
            return Task.Run(() => { return transformFiles(inputMask, outputFolder, saveFiles, recursive, cancel, progress); });
        }
    }
}
