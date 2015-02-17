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
            theTextBox.TextChanged += new TextChangedEventHandler(OnNumTextChanged);
            theUnits.Content = units;
        }

        public string NumText
        {
            get { return (string)GetValue(NumTextProperty); }
            set { SetValue(NumTextProperty, value); }
        }
        
        private void OnNumTextChanged(object sender, TextChangedEventArgs e)
        {
            e.Handled = true;
            RoutedEventArgs args = new RoutedEventArgs(NumTextChangedEvent);
            RaiseEvent(args);
        }

        public bool IsReadOnly
        {
            get { return theTextBox.IsReadOnly; }
            set { theTextBox.IsReadOnly = value; }
        }

        public event RoutedEventHandler NumTextChanged
        {
            add { AddHandler(NumTextChangedEvent, value); }
            remove { RemoveHandler(NumTextChangedEvent, value); }
        }
        
        public static readonly DependencyProperty NumTextProperty =
           DependencyProperty.Register("NumText", typeof(string), typeof(NumberWithUnits));
        
        public static readonly RoutedEvent NumTextChangedEvent =
            EventManager.RegisterRoutedEvent("NumTextChanged",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(NumberWithUnits));

    }
}