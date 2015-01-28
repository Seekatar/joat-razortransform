using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace RazorTransform
{
    [System.Windows.Markup.ContentProperty("Text")]
    public partial class NumberWithUnits : UserControl
    {
        public NumberWithUnits(string units)
        {
            InitializeComponent();
            theTextBox.TextChanged += new TextChangedEventHandler(OnTextChanged);
            theUnits.Content = units;
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }
        
        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            e.Handled = true;
            RoutedEventArgs args = new RoutedEventArgs(TextChangedEvent);
            RaiseEvent(args);
        }

        public bool IsReadOnly
        {
            get { return theTextBox.IsReadOnly; }
            set { theTextBox.IsReadOnly = value; }
        }

        public event RoutedEventHandler TextChanged
        {
            add { AddHandler(TextChangedEvent, value); }
            remove { RemoveHandler(TextChangedEvent, value); }
        }
        
        public static readonly DependencyProperty TextProperty =
           DependencyProperty.Register("Text", typeof(string), typeof(NumberWithUnits));
        
        public static readonly RoutedEvent TextChangedEvent =
            EventManager.RegisterRoutedEvent("TextChanged",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(NumberWithUnits));

    }
}