using System;
using System.Threading;
using System.Windows;
using RtPsHost;

namespace RazorTransform
{
    /// <summary>
    /// implmentation of System.Progress to show progress of transform
    /// </summary>
    class GuiProgress : ITransformOutput
    {
        private readonly SynchronizationContext syncContext;
        private MainEdit _window;

        public GuiProgress(MainEdit w)
        {
            this.syncContext = SynchronizationContext.Current ?? new SynchronizationContext();
            _window = w;
        }

        public void Report(ProgressInfo value)
        {
            syncContext.Post(_ =>
            {
                if ( _window != null )
                     _window.Report(value);
            }, null);
        }

        public MessageBoxResult ShowMessage(string msg, MessageBoxButton messageBoxButton = MessageBoxButton.OK, MessageBoxImage messageBoxImage = MessageBoxImage.None, string secondaryMsg = null)
        {
            try
            {
                if (App.Current != null && App.Current.MainWindow != null)
                {
                    return Joat.TaskDialog.ShowMsg(App.Current.MainWindow, msg, secondaryMsg, messageBoxButton, messageBoxImage);
                }
                else
                {
                    return Joat.TaskDialog.ShowMsg(null, msg, secondaryMsg, messageBoxButton, messageBoxImage);
                }
            }
            catch (EntryPointNotFoundException)
            {
                // fall back to message box since in debugger TaskDialog doesn't usually work
                if (App.Current != null && App.Current.MainWindow != null)
                    return MessageBox.Show(App.Current.MainWindow, msg + Environment.NewLine + secondaryMsg, Resource.Title, messageBoxButton, messageBoxImage);
                else
                    return MessageBox.Show(msg+ Environment.NewLine + secondaryMsg, Resource.Title, messageBoxButton, messageBoxImage);
            }
        }

        public void Dispose()
        {
        }
    }

}
