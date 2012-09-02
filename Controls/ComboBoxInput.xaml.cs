using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RazorTransform
{
    /// <summary>
    /// Interaction logic for ComboBoxInput.xaml
    /// </summary>
    [System.Windows.Markup.ContentProperty("ComboStr")]
    public partial class ComboBoxInput : UserControl
    {
        public ComboBoxInput()
        {
            InitializeComponent();
            theComboBox.SelectionChanged += new SelectionChangedEventHandler(OnSelectionChanged);
        }

        public string ComboBoxStr
        {
            get { return (string)GetValue(ComboBoxProperty); }
            set { SetValue(ComboBoxProperty, value); }
        }

        void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            e.Handled = true;
            RoutedEventArgs args = new RoutedEventArgs(ComboBoxChangedEvent);
            RaiseEvent(args);
        }

        public event RoutedEventHandler ComboBoxChanged
        {
            add { AddHandler(ComboBoxChangedEvent, value); }
            remove { RemoveHandler(ComboBoxChangedEvent, value); }
        }

        public static readonly DependencyProperty ComboBoxProperty =
           DependencyProperty.Register("ComboBoxStr", typeof(string), typeof(ComboBoxInput));

        public static readonly DependencyProperty ComboBoxListProperty =
           DependencyProperty.Register("ComboBoxList", typeof(IDictionary<string,string>), typeof(ComboBoxInput));

        public static readonly RoutedEvent ComboBoxChangedEvent =
            EventManager.RegisterRoutedEvent("ComboBoxChanged",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ComboBoxInput));

    }
}
