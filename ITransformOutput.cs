using System;
using System.Windows;

namespace RazorTransform
{
    public class ProgressInfo
    {
        public string Activity { get; set; }
        public string Status { get; set; }
        public string CurrentOperation { get; set; }
        public int PercentComplete { get; set; }
        public int Id { get; set; }

        public ProgressInfo( string activity, string status = null, string currentOperation = null, int percentComplete = 0, int id = 0 )
        {
            Activity = activity;
            Status = status;
            CurrentOperation = currentOperation;
            PercentComplete = percentComplete;
            Id = id;
        }

        public static implicit operator string( ProgressInfo pi ) { return pi.Activity; }
    }

    /// <summary>
    /// for progress loggers to implement
    /// </summary>
    internal interface ITransformOutput : IProgress<ProgressInfo>, IDisposable
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
