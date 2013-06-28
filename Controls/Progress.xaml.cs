using System;
using System.Windows.Controls;
using RtPsHost;

namespace RazorTransform
{
    /// <summary>
    /// Interaction logic for Progress.xaml
    /// </summary>
    public partial class Progress : UserControl, IProgress<ProgressInfo>
    {
        public Progress()
        {
            InitializeComponent();
        }

        private void setTextAndVisibility(TextBlock c, String text)
        {
            if (!String.IsNullOrWhiteSpace(text))
            {
                c.Text = text;
                c.Visibility = System.Windows.Visibility.Visible;
            }
            else
                c.Visibility = System.Windows.Visibility.Collapsed;
        }

        public void Report(ProgressInfo value)
        {
            txtActivity.Text = value.Activity;
            setTextAndVisibility( txtTimeRemaining, value.TimeRemaining);
            setTextAndVisibility(txtStatus,value.Status);
            setTextAndVisibility(txtCurrentOperation,value.CurrentOperation);

            pbProgress.Value = Math.Min(100, Math.Max(0, value.PercentComplete));
        }
    }
}
