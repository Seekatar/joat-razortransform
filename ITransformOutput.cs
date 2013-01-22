using System;
using System.Windows;

namespace RazorTransform
{
    /// <summary>
    /// for progress loggers to implement
    /// </summary>
    internal interface ITransformOutput : IProgress<string>, IDisposable
    {
        /// <summary>
        /// show a message, interactively if possible
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="messageBoxButton"></param>
        /// <param name="messageBoxImage"></param>
        /// <param name="secondaryMsg"></param>
        /// <returns></returns>
        MessageBoxResult ShowMessage(string msg, MessageBoxButton messageBoxButton = MessageBoxButton.OK, MessageBoxImage messageBoxImage = MessageBoxImage.None, string secondaryMsg = null);
    }
}
