using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.ComponentModel;

namespace RazorTransform
{
    [System.Windows.Markup.ContentProperty("ItemName")]
    public partial class EditItemBox : UserControl
    {
        public EditItemBox(bool enableDelete)
        {
            InitializeComponent();
            btnDel.IsEnabled = enableDelete;
        }

        public string ItemName
        {
            get { return (string)GetValue(ItemNameProperty); }
            set { SetValue(ItemNameProperty, value); }
        }

        public event RoutedEventHandler EditClicked
        {
            add { AddHandler(EditClickedEvent, value); }
            remove { RemoveHandler(EditClickedEvent, value); }
        }

        public event RoutedEventHandler DelClicked
        {
            add { AddHandler(DelClickedEvent, value); }
            remove { RemoveHandler(DelClickedEvent, value); }
        }

        public event RoutedEventHandler CopyClicked
        {
            add { AddHandler(CopyClickedEvent, value); }
            remove { RemoveHandler(CopyClickedEvent, value); }
        }

        public static readonly DependencyProperty ItemNameProperty =
           DependencyProperty.Register("ItemName", typeof(string), typeof(EditItemBox));

        public static readonly RoutedEvent EditClickedEvent =
            EventManager.RegisterRoutedEvent("EditClicked",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(EditItemBox));

        public static readonly RoutedEvent DelClickedEvent =
            EventManager.RegisterRoutedEvent("DelClicked",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(EditItemBox));

        public static readonly RoutedEvent CopyClickedEvent =
            EventManager.RegisterRoutedEvent("CopyClicked",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(EditItemBox));

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            RoutedEventArgs args = new RoutedEventArgs(EditClickedEvent);
            RaiseEvent(args);
        }

        private void btnDel_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            RoutedEventArgs args = new RoutedEventArgs(DelClickedEvent);
            RaiseEvent(args);
        }

        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            RoutedEventArgs args = new RoutedEventArgs(CopyClickedEvent);
            RaiseEvent(args);
        }

    }
}