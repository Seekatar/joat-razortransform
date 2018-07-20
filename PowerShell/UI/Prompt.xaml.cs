using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using RtPsHost;
using System.Security;

namespace PSHostGui
{
	/// <summary>
	/// Interaction logic for Prompt.xaml
	/// </summary>
	public partial class Prompt : Window
    {
        public Prompt(string caption, string message, IEnumerable<PromptChoice> choices, int defaultChoice, bool getText = false, bool textAsPassword = false )
        {
            InitializeComponent();

            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            txtPrompt.Text = caption;
            Title = "PowerShell Prompt";

            if (!String.IsNullOrWhiteSpace(message))
            {
                txtDescription.Text = message;
                txtDescription.Visibility = System.Windows.Visibility.Visible;
            }

            if (getText)
            {
                if (textAsPassword)
                {
                    pbEdit.Visibility = System.Windows.Visibility.Visible;
                    pbEdit.Focus();
                }
                else
                {
                    tbEdit.Visibility = System.Windows.Visibility.Visible;
                    tbEdit.Focus();
                }
            }

            int i = 0;
            foreach (var c in choices)
            {
                var b = new Button()
                {
                    Content = c.Name.Replace("&","_"), // label
                    IsDefault = defaultChoice == i,
                    Tag = i,
                    Margin = new Thickness(0,0,5,0),
                    Style = this.FindResource("txtButton") as Style,
                    IsCancel = c.Name.EndsWith("Cancel")
                };
                if (!String.IsNullOrWhiteSpace(c.HelpString)) // helpMsg
                    b.ToolTip = c.HelpString;
                b.Click += b_Click;
                stackPanel.Children.Add(b);
                i++;
            }
            Choice = -1;
        }

        public int Choice { get; private set; }

        public string Text { get; private set; }
        public SecureString SecureString { get; private set; }

        void b_Click(object sender, RoutedEventArgs e)
        {
            Choice = (int)((sender as Button).Tag);
            if (Choice == 0)
                if (tbEdit.Visibility == System.Windows.Visibility.Visible)
                    Text = tbEdit.Text;
                else if (pbEdit.Visibility == System.Windows.Visibility.Visible)
                    SecureString = pbEdit.SecurePassword;
                  
            Close();
        }

        private void Window_Closing_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Choice == -1)
            {
                MessageBox.Show("Must pick a choice.  Click cancel in main window to stop script");
                e.Cancel = true;
            }
        }
    }
}
