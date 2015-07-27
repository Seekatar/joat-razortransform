using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using RazorTransform;
using RtPsHost;
using System.Security;

namespace PSHostGui
{
    /// <summary>
    /// Interaction logic for PsConsole.xaml
    /// </summary>
    public partial class PsConsole : UserControl, IPsConsole, IDisposable
    {
        private Paragraph _lastParagraph = null;
        private LoggingConsole _loggingConsole;

        private SynchronizationContext syncContext;
        private ConsoleColor _defaultForeground;
        private ConsoleColor _defaultBackground;

        IPsHost _psHost = PsHostFactory.CreateHost();

        public static ConsoleColor EchoColor = ConsoleColor.Gray;
        public static ConsoleColor SystemColor = ConsoleColor.Cyan;

        public PsConsole()
        {
            InitializeComponent();

            flowDoc.LineHeight = 1;

            _defaultForeground = ConsoleColor.White;
            _defaultBackground = ConsoleColor.DarkBlue;

        }

        public SynchronizationContext SyncContext { get { return syncContext; } set { syncContext = value; } }
        public void Initialize()
        {
            syncContext = SynchronizationContext.Current ?? new SynchronizationContext();

            _psHost.Initialize(this);
        }

        /// <summary>
        /// cancel the currently running steps
        /// </summary>
        public void Cancel()
        {
            WriteSystemMessage("Cancelling...");
            _psHost.Cancel();
        }

        /// <summary>
        /// start the script
        /// </summary>
        /// <param name="scriptFname"></param>
        /// <param name="logFname"></param>
        /// <param name="step"></param>
        /// <returns></returns>
        public async Task<ProcessingResult> InvokeAsync(string scriptFname, string logFname, bool step, IDictionary<string, object> variables, ScriptInfo.ScriptType scriptType = ScriptInfo.ScriptType.normal)
        {
            if (scriptType == ScriptInfo.ScriptType.postRun)
                throw new ArgumentException("scriptType cannot be postRun");

            ProcessingResult ret = ProcessingResult.ok;

            if (!String.IsNullOrWhiteSpace(Path.GetDirectoryName(logFname)) &&
                 !Directory.Exists(Path.GetDirectoryName(logFname)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(logFname));
            }

            var timings = new List<StepTiming>();

            using (_loggingConsole = new LoggingConsole(logFname))
            {
                try
                {

                    WriteSystemMessage(String.Format("Logging to \"{0}\"", logFname));

                    ret = await _psHost.InvokeAsync(scriptFname, step, variables, scriptType, timings: timings);

                    if (scriptType == ScriptInfo.ScriptType.normal)
                    {
                        if (ret == ProcessingResult.ok)
                        {
                            await _psHost.InvokeAsync(scriptFname, step, variables, ScriptInfo.ScriptType.postRun, timings: timings);
                            writeTimings(timings);
                            _loggingConsole.Dispose();
                            await _psHost.InvokeAsync(scriptFname, step, variables, ScriptInfo.ScriptType.success, timings: timings);
                        }
                        else
                        {
                            writeTimings(timings);
                            _loggingConsole.Dispose();
                            await _psHost.InvokeAsync(scriptFname, step, variables, ScriptInfo.ScriptType.fail, timings: timings);
                        }
                    }
                }
                finally
                {
                }
            }

            WriteSystemMessage(String.Format("Logged to \"{0}\"", logFname));

            return ret;
        }

        private void writeTimings(List<StepTiming> timings)
        {
            string s;
            var total = timings.Sum(o => o.Duration.TotalSeconds);
            WriteSystemMessage(String.Empty);
            WriteSystemMessage("Steps successfully run:");

            _loggingConsole.WriteLine(String.Empty, WriteType.System);
            _loggingConsole.WriteLine("Steps successfully run:", WriteType.System);

            var maxNameLen = timings.Max(o => o.Name.Length);
            var timingFormat = String.Format("{{0,{0}}} {{1}} {{2,5:f0}}% {{3}}", maxNameLen);
            var summaryFormat = String.Format("{{0,{0}}} {{1,7:f2}} secs", maxNameLen);

            foreach (var t in timings)
            {
                s = string.Format( timingFormat,
                                            t.Name,
                                            t.Skipped ? "skipped     " : string.Format("{0,7:f2} secs", t.Duration.TotalSeconds),
                                            (total > 0) ? 100 * t.Duration.TotalSeconds / total : 0,
                                            (total > 0) ? (new String('*', (int)(50 * t.Duration.TotalSeconds / total))) : string.Empty);
                WriteSystemMessage(s);
                _loggingConsole.WriteLine(s,WriteType.System);

            }
            s = string.Format(summaryFormat,
                                        "TOTAL",
                                        total);
            WriteSystemMessage(s);
            _loggingConsole.WriteLine(s,WriteType.System);
        }

        /// <summary>
        /// map from consoleColor to Color
        /// </summary>
        static Dictionary<ConsoleColor, SolidColorBrush> _colorMap = new Dictionary<ConsoleColor, SolidColorBrush>() 
        {
            {ConsoleColor.Black, new SolidColorBrush( Colors.Black)},
            {ConsoleColor.DarkBlue, new SolidColorBrush( Colors.DarkBlue)},
            {ConsoleColor.DarkGreen, new SolidColorBrush( Colors.DarkGreen)},
            {ConsoleColor.DarkCyan, new SolidColorBrush( Colors.DarkCyan)},
            {ConsoleColor.DarkRed, new SolidColorBrush( Colors.DarkRed)},
            {ConsoleColor.DarkMagenta, new SolidColorBrush( Colors.DarkMagenta)},
            {ConsoleColor.DarkYellow, new SolidColorBrush( Colors.DarkGoldenrod)},
            {ConsoleColor.Gray, new SolidColorBrush( Colors.Gray)},
            {ConsoleColor.DarkGray, new SolidColorBrush( Colors.DarkGray)},
            {ConsoleColor.Blue, new SolidColorBrush( Colors.Blue)},
            {ConsoleColor.Green, new SolidColorBrush( Colors.Green)},
            {ConsoleColor.Cyan, new SolidColorBrush( Colors.Cyan)},
            {ConsoleColor.Red, new SolidColorBrush( Colors.Red)},
            {ConsoleColor.Magenta, new SolidColorBrush( Colors.Magenta)},
            {ConsoleColor.Yellow, new SolidColorBrush( Colors.Yellow)},
            {ConsoleColor.White, new SolidColorBrush( Colors.White)}
        };

        public static IReadOnlyDictionary<ConsoleColor, SolidColorBrush> ColorMap { get { return _colorMap; } }

        /// <summary>
        /// base function for writing out text to the window
        /// </summary>
        /// <param name="foreground"></param>
        /// <param name="background"></param>
        /// <param name="msg"></param>
        /// <param name="isLine"></param>
        void writeText(Brush foreground, Brush background, string msg, bool isLine, WriteType type)
        {
            if (_loggingConsole != null)
            {
                _loggingConsole.WriteText(msg, isLine, type);
            }

            var run = new Run(msg.TrimEnd("\r\n".ToCharArray())) { Foreground = foreground, Background = background };
            if (_lastParagraph == null)
                _lastParagraph = new Paragraph(run);
            else
                _lastParagraph.Inlines.Add(run);

            flowDoc.Blocks.Add(_lastParagraph);
            (flowDoc.Parent as RichTextBox).ScrollToEnd();

            if (isLine)
                _lastParagraph = null; // don't add to this one

        }

        #region IPSListener implementation

        public bool ShouldExit
        {
            get;
            set;
        }

        public int ExitCode
        {
            get;
            set;
        }

        public void Write(string msg, WriteType type)
        {
            syncContext.Post(_ =>
            {
                writeText(_colorMap[_defaultForeground], _colorMap[_defaultBackground], msg, false, type);
            }, null);
        }

        public void WriteLine(string msg, WriteType type)
        {
            syncContext.Post(_ =>
            {
                writeText(_colorMap[_defaultForeground], _colorMap[_defaultBackground], msg, true, type);
            }, null);
        }

        public void Write(ConsoleColor foreground, ConsoleColor background, string msg, WriteType type)
        {
            syncContext.Post(_ =>
            {
                writeText(_colorMap[foreground], _colorMap[background], msg, false, type);
            }, null);
        }

        public void WriteLine(ConsoleColor foreground, ConsoleColor background, string msg, WriteType type)
        {
            syncContext.Post(_ =>
            {
                writeText(_colorMap[foreground], _colorMap[background], msg, true, type);
            }, null);
        }

        public ConsoleColor ForegroundColor
        {
            get
            {
                ConsoleColor ret = ConsoleColor.White;
                syncContext.Send(_ =>
                {
                    ret = _colorMap.Where(o => o.Value == _colorMap[_defaultForeground]).Select(o => o.Key).FirstOrDefault();
                }, null);
                return ret;
            }
            set
            {
                _defaultForeground = value;
            }
        }

        public ConsoleColor BackgroundColor
        {
            get
            {
                return _colorMap.Where(o => o.Value == _colorMap[_defaultBackground]).Select(o => o.Key).FirstOrDefault();
            }
            set
            {
                _defaultBackground = value;
            }
        }

        // helper stolen from StackOverflow
        private System.Windows.Window GetTopLevelControl(System.Windows.DependencyObject control)
        {
            var tmp = control;
            System.Windows.DependencyObject parent = null;
            while ((tmp = VisualTreeHelper.GetParent(tmp)) != null)
            {
                parent = tmp;
            }
            return parent as System.Windows.Window;
        }

        public int PromptForChoice(string caption, string message, IEnumerable<PromptChoice> choices, int defaultChoice)
        {
            var p = prompt(caption, message, choices, defaultChoice);
            return p.Choice;
        }

        public string PromptForString(string caption, string message, string description)
        {
            var choices = new List<PromptChoice>() { new PromptChoice("OK"), new PromptChoice("Cancel") };
            int defaultChoice = 0;

            caption = description; // can we get > 1?
            var p = prompt(caption, message, choices, defaultChoice, true);
            return p.Text;
        }


        public System.Security.SecureString PromptForSecureString(string caption, string message, string description)
        {
            var choices = new List<PromptChoice>() { new PromptChoice("OK"), new PromptChoice("Cancel") };
            int defaultChoice = 0;

            caption = description; // can we get > 1?

            var p = prompt(caption, message, choices, defaultChoice, true, true);

            return p.SecureString;
        }

        Dictionary<ProgressInfo, Progress> _progress = new Dictionary<ProgressInfo, Progress>();

        public void WriteProgress(ProgressInfo progressInfo)
        {
            KeyValuePair<ProgressInfo,Progress> tuple;
            lock (_progress)
            {
                tuple = _progress.Where(o => o.Key.Id == progressInfo.Id || o.Key.Activity == progressInfo.Activity).FirstOrDefault();
            }

            syncContext.Send(_ =>
            {
                Progress progress = null;
                if (tuple.Key == null)
                {
                    if (progressInfo.PercentComplete >= 100)
                        return;


                    progress = new Progress();
                    if (progressStack.Visibility != System.Windows.Visibility.Visible)
                        progressStack.Visibility = System.Windows.Visibility.Visible;

                    progressStack.Children.Add(progress);
                    lock (_progress)
                    {
                        _progress[progressInfo] = progress;
                    }
                }
                else
                {
                    progress = tuple.Value;
                }

                if (progressInfo.PercentComplete >= 100)
                {
                    bool removed = false;
                    lock (_progress)
                    {
                        if (_progress.ContainsKey(tuple.Key))
                        {
                            _progress.Remove(tuple.Key);
                            removed = true;
                        }
                    }
                    if (removed)
                    {
                        progressStack.Children.Remove(progress);
                        if (progressStack.Children.Count == 0)
                            progressStack.Visibility = System.Windows.Visibility.Collapsed;
                    }
                }
                else
                {
                    progress.Report(progressInfo);
                }
            }, null);
        }

        #endregion

        public void WriteSystemMessage(string msg)
        {
            WriteLine(SystemColor, BackgroundColor, "> " + msg, WriteType.System);
        }

        private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                Initialize();
            }
        }

        /// <summary>
        /// width of window in chars
        /// </summary>
        public int WindowWidth
        {
            get { return 500; }
        }

        /// <summary>
        /// clear the content of the console
        /// </summary>
        internal void Clear()
        {
            flowDoc.Blocks.Clear();
        }

        public void Dispose()
        {
            _psHost.Dispose();
            syncContext = null;
        }

        internal void SetColors(Color foreground, Color background)
        {
            // find the color
            var c = ColorMap.Where(o => o.Value.Color == foreground);
            if (c.Count() > 0)
                _defaultForeground = c.First().Key;
            c = ColorMap.Where(o => o.Value.Color == background);
            if (c.Count() > 0)
            {
                textBox.Background = c.First().Value;
                _defaultBackground = c.First().Key;
            }
        }

        public System.Windows.Window Owner { get; set; }

        private Prompt prompt(string caption, string message, IEnumerable<PromptChoice> choices, int defaultChoice, bool getText = false, bool isPassword = false)
        {
            Prompt p = null;
            if (syncContext != null)
            {
                syncContext.Send(_ =>
                {
                    p = new Prompt(caption, message, choices, defaultChoice, getText, isPassword);
                    p.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
                    p.Owner = GetTopLevelControl(this);
                    if (p.Owner == null)
                        p.Owner = Owner;
                    if (p.Owner == null)
                    {
                        p.Loaded += (sender, e) => { (sender as Prompt).Topmost = true; };
                        p.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
                    }
                    p.ShowDialog();
                    p.Topmost = true;
                    if (_loggingConsole != null)
                    {
                        _loggingConsole.PromptForChoice(caption, message, choices, defaultChoice);
                    }
                }, null);
            }
            return p;
        }
    }
}