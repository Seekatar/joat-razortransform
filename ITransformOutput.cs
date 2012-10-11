using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RazorTransform
{
    /// <summary>
    /// for progress loggers to implment
    /// </summary>
    internal interface ITranformOutput : IProgress<string>, IDisposable
    {
        MessageBoxResult ShowMessage(string msg, MessageBoxButton messageBoxButton = MessageBoxButton.OK, MessageBoxImage messageBoxImage = MessageBoxImage.None, string secondaryMsg = null);
    }
}
