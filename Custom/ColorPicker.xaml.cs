using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Xml.Linq;

namespace RazorTransform.Custom
{
    /// <summary>
    /// the model of the color
    /// </summary>
    class ColorItem : TransformModelItem
    {
        public ColorItem()
        {
        }

        public ColorItem(ColorItem src)
            : base(src)
        {
        }

    }

    /// <summary>
    /// custom type for representing a Color
    /// </summary>
    public class ColorType : ICustomRazorTransformType
    {
        bool _psColors = false;

        public ColorType() { }

        public void Initialize(ITransformModel model, IDictionary<string,string> parms) 
        {
            // only one parm now
            _psColors = parms.ContainsKey("psColors") && bool.Parse(parms["psColors"]);
        }

        public Control CreateControl(ITransformModelItem ci, System.Windows.Data.Binding binding)
        {

            var t = new ColorPicker(ci, _psColors);
            binding.Mode = BindingMode.TwoWay;

            t.SetBinding(ColorPicker.ColorProperty, binding);
            return t;
        }

        public TransformModelItem CreateItem(ITransformModelGroup parent, XElement e)
        {
            return new ColorItem();
        }
    }


    /// <summary>
    /// user contorl for getting a color, wraps the Xceed color picker
    /// </summary>
    [System.Windows.Markup.ContentProperty("Color")]
    public partial class ColorPicker : UserControl
    {
        public ColorPicker(ITransformModelItem ci, bool psColors)
        {
            InitializeComponent();
            colorPicker.SelectedColorChanged += OnColorChanged;
            if (psColors)
            {
                //colorPicker.AvailableColors = new System.Collections.ObjectModel.ObservableCollection<Xceed.Wpf.Toolkit.ColorItem>(
                //        PSHostGui.PsConsole.ColorMap.Select(o => new Xceed.Wpf.Toolkit.ColorItem(o.Value.Color, o.Key.ToString()))
                //    );
                colorPicker.ShowAdvancedButton = false;
                colorPicker.ShowStandardColors = false;
            }
        }

        public string ColorText
        {
            get { return (string)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }

        private void OnColorChanged(object sender, RoutedPropertyChangedEventArgs<Color> e)
        {
            RoutedEventArgs args = new RoutedEventArgs(ColorChangedEvent);
            RaiseEvent(args);
        }
        
        public event RoutedEventHandler ColorChanged
        {
            add { AddHandler(ColorChangedEvent, value); }
            remove { RemoveHandler(ColorChangedEvent, value); }
        }

        public bool IsReadOnly
        {
            get { return colorPicker.IsEnabled; }
            set { colorPicker.IsEnabled = !value; }
        }

        public static readonly DependencyProperty ColorProperty =
           DependencyProperty.Register("ColorText", typeof(string), typeof(ColorPicker));
        
        public static readonly RoutedEvent ColorChangedEvent =
            EventManager.RegisterRoutedEvent("ColorChanged",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ColorPicker));

    }
}