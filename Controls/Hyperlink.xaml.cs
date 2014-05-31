using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace RazorTransform
{
    [System.Windows.Markup.ContentProperty("HyperlinkStr")]
    public partial class Hyperlink : UserControl
    {
        public Hyperlink()
        {
            InitializeComponent();
        }


        public Uri NavigateUri { get { return _hyperlink.NavigateUri; } set { _hyperlink.NavigateUri = value;} }
        public string Text { get { return (_hyperlink.Inlines.FirstInline as Run).Text; } set { (_hyperlink.Inlines.FirstInline as Run).Text = value; } }

        public static readonly DependencyProperty HyperlinkProperty =
           DependencyProperty.Register("Hyperlink", typeof(string), typeof(Hyperlink));

        private void _hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
