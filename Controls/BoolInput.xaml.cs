using System;
using System.Windows;
using System.Windows.Controls;

namespace RazorTransform
{
    [System.Windows.Markup.ContentProperty("BoolStr")]
    public partial class BoolInput : UserControl
    {
        public BoolInput()
        {
            InitializeComponent();
            theCheckBox.Checked += new RoutedEventHandler(OnChecked);
        }

        void theCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        public bool Bool
        {
            get { return (bool)GetValue(BoolProperty); }
            set { SetValue(BoolProperty, value); }
        }

        private void OnChecked(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            RoutedEventArgs args = new RoutedEventArgs(BoolChangedEvent);
            RaiseEvent(args);
        }

        public event RoutedEventHandler BoolChanged
        {
            add { AddHandler(BoolChangedEvent, value); }
            remove { RemoveHandler(BoolChangedEvent, value); }
        }

        public static readonly DependencyProperty BoolProperty =
           DependencyProperty.Register("Bool", typeof(string), typeof(BoolInput));

        public static readonly RoutedEvent BoolChangedEvent =
            EventManager.RegisterRoutedEvent("BoolChanged",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(BoolInput));

    }
}
