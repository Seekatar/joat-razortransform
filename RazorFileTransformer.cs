using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace RazorTransform
{
#if !ASYNC
    interface IProgress<T>
    {
        void Report(T t);
    }
#endif
    /// <summary>
    /// class for running Razor transform on a set of files
    /// given an ExpandoObject
    /// </summary>
    class RazorFileTransformer
    {
        dynamic _model;

        private void transformFiles(string mask, bool recursive, CancellationToken cancel, IProgress<string> progress)
        {
            string currentFile = String.Empty;
            try
            {
                if (progress != null) progress.Report("Starting transforms");
                foreach (var f in Directory.EnumerateFiles("Templates", "*.*"))
                {
                    currentFile = f;

                    cancel.ThrowIfCancellationRequested();

                    if (progress != null)
                        progress.Report(f);

                    string template = File.ReadAllText(f);
                    string content = RazorEngine.Razor.Parse(template, _model);

                    if (content == null)
                        throw new Exception("Transform returned no content");

                    if (content.Contains("@Model.") || content.Contains("@(") ) // allow one level of nesting of @Model. or @(
                        content = RazorEngine.Razor.Parse(content, _model);

                    File.WriteAllText(Path.Combine(mask, Path.GetFileName(f)), content);
                }
                if (progress != null) progress.Report("Success");

            }
            catch ( RazorEngine.Templating.TemplateCompilationException e )
            {
                var sb = new StringBuilder();
                sb.AppendFormat( "Error processing file {0} {1}", Path.GetFileName(currentFile), e.Message );
                foreach (var ee in e.Errors)
                {
                    sb.AppendLine( String.Format( "   {0} at line {1}({2})", ee.ErrorText, ee.Line, ee.Column ) );
                }
                if (progress != null) progress.Report(sb.ToString());

                throw new Exception(sb.ToString());
            }
            catch (Exception ee)
            {
                var sb = new StringBuilder();
                sb.AppendFormat("Error processing file {0} {1}", Path.GetFileName(currentFile), ee.Message);
                if (progress != null) progress.Report(sb.ToString());

                throw new Exception(sb.ToString());
            }
        }

        // constructor 
        public RazorFileTransformer(dynamic model)
        {
            _model = model;
        }

        /// <summary>
        /// Synchronous transform
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="recursive"></param>
        /// <param name="progress"></param>
        public void TransformFiles(string mask, bool recursive = false, IProgress<string> progress = null)
        {
            transformFiles(mask, recursive, CancellationToken.None, progress );
        }

#if ASYNC
        private Task<bool> transformFilesAsync(string mask, bool recursive, CancellationToken cancel , IProgress<string> progress )
        {
            return TaskEx.Run(() => { transformFiles(mask, recursive, cancel, progress); return true; });
        }

        public async Task<bool> TransformFilesAsync( string mask, CancellationToken cancel, bool recursive = false, IProgress<string> progress = null )
        {
            return await transformFilesAsync(mask, recursive, cancel, progress);
        }
#endif
    }
}
