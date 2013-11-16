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
                _window.Report(value);
            }, null);
        }

        public MessageBoxResult ShowMessage(string msg, MessageBoxButton messageBoxButton = MessageBoxButton.OK, MessageBoxImage messageBoxImage = MessageBoxImage.None, string secondaryMsg = null)
        {
            if (!String.IsNullOrWhiteSpace(secondaryMsg))
                msg += Environment.NewLine + secondaryMsg;

            if ( App.Current != null && App.Current.MainWindow != null )
                return MessageBox.Show(App.Current.MainWindow, msg + Environment.NewLine + secondaryMsg, Resource.Title, messageBoxButton, messageBoxImage);
            else
                return MessageBox.Show(msg + Environment.NewLine + secondaryMsg, Resource.Title, messageBoxButton, messageBoxImage);
        }

        public void Dispose()
        {
        }
    }

}
