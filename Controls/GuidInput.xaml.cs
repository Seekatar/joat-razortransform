using System;
using System.Windows;
using System.Windows.Controls;

namespace RazorTransform
{
    [System.Windows.Markup.ContentProperty("GuidStr")]
    public partial class GuidInput : UserControl
    {
        public GuidInput()
        {
            InitializeComponent();
            theTextBox.TextChanged += new TextChangedEventHandler(OnTextChanged);
        }

        private void theButton_Click(object sender, RoutedEventArgs e)
        {
            this.GuidStr = Guid.NewGuid().ToString();
        }

        public string GuidStr
        {
            get { return (string)GetValue(GuidStrProperty); }
            set { SetValue(GuidStrProperty, value); }
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            e.Handled = true;
            RoutedEventArgs args = new RoutedEventArgs(GuidStrChangedEvent);
            RaiseEvent(args);
        }

        public event RoutedEventHandler GuidStrChanged
        {
            add { AddHandler(GuidStrChangedEvent, value); }
            remove { RemoveHandler(GuidStrChangedEvent, value); }
        }

        public static readonly DependencyProperty GuidStrProperty =
           DependencyProperty.Register("GuidStr", typeof(string), typeof(GuidInput));

        public static readonly RoutedEvent GuidStrChangedEvent =
            EventManager.RegisterRoutedEvent("GuidStrChanged",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(GuidInput));

    }
}
