using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System;

namespace RazorTransform
{
    //[System.Windows.Markup.ContentProperty("Text")]
    public partial class ModelItemEdit : UserControl
    {
        RazorTransform.Model.IItem _item; 

        public ModelItemEdit(RazorTransform.Model.IItem item, string units = null )
        {
            InitializeComponent();
            // this.value.TextChanged += new TextChangedEventHandler(OnOrigChanged);
            this.value.LostFocus += new RoutedEventHandler(OnValueChanged);

            _item = item;
            if ( _item.Value != _item.ExpandedValue )
            {
                arrow.Visibility = expandedValue.Visibility = System.Windows.Visibility.Visible;
            }

            if ( !String.IsNullOrWhiteSpace(units))
            {
                this.units.Visibility = System.Windows.Visibility.Visible;
                this.units.Text = units;
            }
        }

        public string Value
        {
            get { return (string)GetValue(ValueProperty); }
            set 
            { 
                SetValue(ValueProperty, value); 
                if ( value != ExpandedValue )
                {
                    this.value.Visibility = System.Windows.Visibility.Visible;
                }
            }
        }

        public string ExpandedValue
        {
            get { return (string)GetValue(ExpandedValueProperty); }
            set { SetValue(ExpandedValueProperty, value); }
        }

        private void OnValueChanged(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            RoutedEventArgs args = new RoutedEventArgs(ValueChangedEvent);
            RaiseEvent(args);
            ExpandedValue = _item.ExpandedValue; // can't get the raise event to update ExpandedValue via binding
            if ( !String.IsNullOrWhiteSpace(ExpandedValue) && ExpandedValue != Value )
            {
                arrow.Visibility = expandedValue.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                arrow.Visibility = expandedValue.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void OnExpandedValueChanged(object sender, TextChangedEventArgs e)
        {
            e.Handled = true;
            RoutedEventArgs args = new RoutedEventArgs(ExpandedValueChangedEvent);
            RaiseEvent(args);
        }

        public bool IsReadOnly
        {
            get { return value.IsReadOnly; }
            set { this.value.IsReadOnly = value; }
        }

        public event RoutedEventHandler ValueChanged
        {
            add { AddHandler(ValueChangedEvent, value); }
            remove { RemoveHandler(ValueChangedEvent, value); }
        }

        public static readonly DependencyProperty ValueProperty =
           DependencyProperty.Register("Value", typeof(string), typeof(ModelItemEdit));

        public static readonly RoutedEvent ValueChangedEvent =
            EventManager.RegisterRoutedEvent("ValueChanged",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ModelItemEdit));

        public event RoutedEventHandler ExpandedValueChanged
        {
            add { AddHandler(ExpandedValueChangedEvent, value); }
            remove { RemoveHandler(ExpandedValueChangedEvent, value); }
        }

        public static readonly DependencyProperty ExpandedValueProperty =
           DependencyProperty.Register("ExpandedValue", typeof(string), typeof(ModelItemEdit));

        public static readonly RoutedEvent ExpandedValueChangedEvent =
            EventManager.RegisterRoutedEvent("ExpandedValueChanged",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ModelItemEdit));

    }
}