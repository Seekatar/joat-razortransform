using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RazorTransform
{
    /// <summary>
    /// Interaction logic for PortInput.xaml
    /// </summary>
    [System.Windows.Markup.ContentProperty("Port")]
    public partial class PortInput : UserControl
    {
        private TransformModelItem info;
        public PortInput(TransformModelItem info)
        {

            this.info = info;


            InitializeComponent();

            // portList.DataContext = PortUtility.GetAssignmentsFromInstalledServices();
        }


        public string Port
        {
            get { return (string)GetValue(PortProperty); }
            set { SetValue(PortProperty, value); }
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {

            var r = new PortValidation(this.info);
            if (r.Validate(((TextBox)sender).Text, null).IsValid)
            {



                e.Handled = true;
                RoutedEventArgs args = new RoutedEventArgs(PortChangedEvent);
                RaiseEvent(args);
            }
            else
            {
                e.Handled = true;
            }

        }



        public static readonly DependencyProperty PortProperty =
           DependencyProperty.Register("Port", typeof(string), typeof(PortInput));

        public static readonly RoutedEvent PortChangedEvent =
            EventManager.RegisterRoutedEvent("PortChanged",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(PortInput));

        private void theTextBox_Error(object sender, ValidationErrorEventArgs e)
        {
            if (e.Action == ValidationErrorEventAction.Added)    // Validation Error Occurred
            {
                ((Control)sender).ToolTip = e.Error.ErrorContent.ToString();
            }
            else                        // No Error
            {
                ((Control)sender).ToolTip = "";
            }
        }

        private void RadButton_Click_1(object sender, RoutedEventArgs e)
        {
            //  this.ValidationPopup.IsOpen = true;
        }

        private void Grid_MouseLeave_1(object sender, MouseEventArgs e)
        {
            //  this.ValidationPopup.IsOpen = false;
        }
    }

    public class PortValidation : ValidationRule
    {
        private TransformModelItem info;
        public PortValidation(TransformModelItem info)
        {
            this.info = info;
        }
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            int result;

            if (Int32.TryParse("" + value, out result))
            {
                return new ValidationResult(true, null);
            }
            return new ValidationResult(false, "Port must be an integer");
        }
    }
}
