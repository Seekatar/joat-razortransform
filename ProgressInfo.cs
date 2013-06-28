using System;

namespace RtPsHost
{
    public class ProgressInfo
    {
        public string Activity { get; set; }
        public string Status { get; set; }
        public string CurrentOperation { get; set; }
        public int PercentComplete { get; set; }
        public int Id { get; set; }
        public string TimeRemaining { get; set; }

        public ProgressInfo(string activity, string status = null, string currentOperation = null, int percentComplete = 0, int id = 0, int secondsRemaining = 0)
        {
            Activity = activity;
            Status = status;
            CurrentOperation = currentOperation;
            PercentComplete = percentComplete;
            Id = id;
            if (secondsRemaining > 0)
            {
                var ts = TimeSpan.FromSeconds(secondsRemaining);
                TimeRemaining = String.Format("{0:d2}:{1:d2}:{2:d2}", ts.Hours, ts.Minutes, ts.Seconds);
            }
        }

        public static implicit operator string(ProgressInfo pi) { return pi.Activity; }

        public override string ToString()
        {
            return String.Format("{0} {3,3}% {1} {2} ", Activity, CurrentOperation, Status, PercentComplete);
        }

    }

}
