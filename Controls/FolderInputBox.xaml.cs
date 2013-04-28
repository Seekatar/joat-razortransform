using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.ComponentModel;
using System.IO;

namespace RazorTransform
{
    [System.Windows.Markup.ContentProperty("FolderName")]
    public partial class FolderInputBox : UserControl
    {
        String _prompt;
        bool _showNewFolderButton;

        public FolderInputBox(string prompt, bool showNewFolderButton )
        {
            _prompt = prompt;
            _showNewFolderButton = showNewFolderButton;

            InitializeComponent();
            theTextBox.TextChanged += new TextChangedEventHandler(OnTextChanged);
        }

        private void theButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fb = new System.Windows.Forms.FolderBrowserDialog();
            fb.Description = _prompt;
            fb.ShowNewFolderButton = _showNewFolderButton;
            if (!String.IsNullOrWhiteSpace(FolderName) && Directory.Exists(FolderName) )
                fb.SelectedPath = FolderName;

            if (fb.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.FolderName = fb.SelectedPath;
            }
        }
        
        public string FolderName
        {
            get { return (string)GetValue(FolderNameProperty); }
            set { SetValue(FolderNameProperty, value); }
        }
        
        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            e.Handled = true;
            RoutedEventArgs args = new RoutedEventArgs(FolderNameChangedEvent);
            RaiseEvent(args);
        }
        
        public event RoutedEventHandler FolderNameChanged
        {
            add { AddHandler(FolderNameChangedEvent, value); }
            remove { RemoveHandler(FolderNameChangedEvent, value); }
        }

        public bool IsReadOnly
        {
            get { return theTextBox.IsReadOnly; }
            set { theTextBox.IsReadOnly = value; theButton.IsEnabled = !value; }
        }

        public static readonly DependencyProperty FolderNameProperty =
           DependencyProperty.Register("FolderName", typeof(string), typeof(FolderInputBox));
        
        public static readonly RoutedEvent FolderNameChangedEvent =
            EventManager.RegisterRoutedEvent("FolderNameChanged",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(FolderInputBox));

    }
}