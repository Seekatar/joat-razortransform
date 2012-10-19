using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace RazorTransform
{
    /// <summary>
    /// implmentation of System.Progress to show progress of transform
    /// </summary>
    class GuiProgress : ITranformOutput
    {
        private readonly SynchronizationContext syncContext;
        private MainEdit _window;

        public GuiProgress(MainEdit w)
        {
            this.syncContext = SynchronizationContext.Current ?? new SynchronizationContext();
            _window = w;
        }

        public void Report(string value)
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

            return MessageBox.Show(msg + Environment.NewLine + secondaryMsg, Resource.Title, messageBoxButton, messageBoxImage);
        }

        public void Dispose()
        {
        }
    }

}
