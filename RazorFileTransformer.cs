﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace RazorTransform
{
    /// <summary>
    /// class for running Razor transform on a set of files
    /// given an ExpandoObject
    /// </summary>
    class RazorFileTransformer
    {
        dynamic _model;

        private int transformFiles(string inputMask, string outputFolder, bool saveFiles, bool recursive, CancellationToken cancel, IProgress<string> progress)
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

                if (progress != null) progress.Report("Starting transforms");

                var sw = new Stopwatch();
                sw.Start();

                foreach (var f in Directory.EnumerateFiles(folder, mask))
                {
                    currentFile = f;

                    cancel.ThrowIfCancellationRequested();

                    if (progress != null) progress.Report(f);

                    string template = File.ReadAllText(f);
                    string content = RazorEngine.Razor.Parse(template, _model);

                    if (content == null)
                        throw new Exception("Transform returned no content");

                    if (content.Contains("@Model.") || content.Contains("@(")) // allow one level of nesting of @Model. or @(
                        content = RazorEngine.Razor.Parse(content, _model);

                    if (saveFiles)
                        File.WriteAllText(Path.Combine(outputFolder, Path.GetFileName(f)), content);

                    count++;
                }
                sw.Stop();
                if (progress != null) progress.Report(String.Format(Resource.Success, count, outputFolder, sw.Elapsed.TotalSeconds ));

            }
            catch (RazorEngine.Templating.TemplateCompilationException e)
            {
                var sb = new StringBuilder();
                sb.AppendFormat("Error processing file {0} {1}", Path.GetFileName(currentFile), e.Message);
                foreach (var ee in e.Errors)
                {
                    sb.AppendLine(String.Format("   {0} at line {1}({2})", ee.ErrorText, ee.Line, ee.Column));
                }
                if (progress != null) progress.Report(sb.ToString());

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
                if (progress != null) progress.Report(sb.ToString()+ee.BuildMessage());

                throw new Exception(sb.ToString(),ee);
            }
            return count;
        }

        // constructor 
        public RazorFileTransformer(dynamic model)
        {
            _model = model;
        }

        /// <summary>
        /// Synchronous transform
        /// </summary>
        /// <param name="outputFolder"></param>
        /// <param name="recursive"></param>
        /// <param name="progress"></param>
        public int TransformFiles(string inputMask, string outputFolder, bool saveFiles = true, bool recursive = false, IProgress<string> progress = null)
        {
            return transformFiles(inputMask, outputFolder, saveFiles, recursive, CancellationToken.None, progress);
        }

        public Task<int> TransformFilesAsync( string inputMask, string outputFolder, bool saveFiles, CancellationToken cancel, bool recursive = false, IProgress<string> progress = null )
        {
            return Task.Run(() => { return transformFiles(inputMask, outputFolder, saveFiles, recursive, cancel, progress);  });
        }
    }
}
