using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using RazorTransform.Controls;
using System.Windows.Documents;

namespace RazorTransform
{
    public static class LayoutManager
    {
        /// <summary>
        /// build a grid view for a group of simple items
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public static FrameworkElement BuildGridView(TransformModelGroup group, bool showHidden, Action itemChanged = null )
        {
            var items = group.Items;

            Grid grid = new Grid();
            grid.Margin = new Thickness(5);
            grid.HorizontalAlignment = HorizontalAlignment.Stretch;
            grid.VerticalAlignment = VerticalAlignment.Stretch;
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var controls = items.Where(x => showHidden || !x.Hidden).ToList();
            Enumerable.Range(0, controls.Count).ToList().ForEach(x =>
                {
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0, GridUnitType.Auto) });
                });

            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0, GridUnitType.Star) });


            int i = 0;
            foreach (var ci in controls)
            {

                var l = new MyLabel() { Content = ci.DisplayName };

                l.ToolTip = ci.Description;
                l.SetValue(Grid.ColumnProperty, 0);
                l.SetValue(Grid.RowProperty, i);
                l.Style = Application.Current.FindResource("CfgLabel") as Style;

                var binding = new Binding();
                binding.Source = ci;
                binding.Path = new PropertyPath("Value");
                binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;

                Control t = CreateControl(ci, binding, itemChanged);
                if (!String.IsNullOrWhiteSpace(ci.ExpandedValue))
                {
                    // build a stack panel for the tooltip to look like this:
                    // <description>
                    // Expanded value: <expandedvalue>
                    var ttBinding = new Binding()
                    {
                        Source = ci,
                        Path = new PropertyPath("ExpandedValue"),
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    };
                    var sp = new StackPanel() { Orientation = Orientation.Vertical };
                    var tt = new TextBlock() { Text = ci.Description };
                    sp.Children.Add(tt);
                    var horizSp = new StackPanel() { Orientation = Orientation.Horizontal };
                    sp.Children.Add(horizSp);
                    horizSp.Children.Add(new Label() { Content = Resource.ExpandedValue });
                    tt = new TextBlock();
                    tt.SetBinding(TextBlock.TextProperty, ttBinding);
                    tt.VerticalAlignment = VerticalAlignment.Center;
                    horizSp.Children.Add(tt);

                    t.ToolTip = sp;
                }
                else
                    t.ToolTip = ci.Description;
                t.SetValue(Grid.ColumnProperty, 1);
                t.SetValue(Grid.RowProperty, i);
                t.HorizontalAlignment = HorizontalAlignment.Left;
                t.HorizontalContentAlignment = HorizontalAlignment.Stretch;
                t.Style = Application.Current.FindResource("CfgText") as Style;

                if ((i & 1) == 0)
                {
                    var gb = GetAltBrush();

                    Rectangle re = new Rectangle();
                    re.Fill = gb;
                    re.SetValue(Grid.RowProperty, i);
                    re.SetValue(Grid.ColumnSpanProperty, 2);

                    grid.Children.Add(re);
                }

                l.Padding = t.Padding = new Thickness(5);

                t.Loaded += (s, e) =>
                    {
                        var cc = s as Control;
                        if (cc != null)
                        {
                            cc.HorizontalAlignment = HorizontalAlignment.Stretch;
                            cc.HorizontalContentAlignment = HorizontalAlignment.Stretch;

                        }
                    };
                t.LayoutUpdated += (s, e) =>
                {
                    var cc = s as Control;
                    if (cc != null)
                    {
                        // cc.Width = grid.ColumnDefinitions[1].ActualWidth;
                    }
                };


                grid.Children.Add(l);
                grid.Children.Add(t);
                i++;
            }
            return grid;

        }

        /// <summary>
        /// build a grid view for an array of items
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="addHandler"></param>
        /// <param name="editHandler"></param>
        /// <param name="deleteHandler"></param>
        /// <returns></returns>
        public static FrameworkElement BuildGridView(TransformModelArray parent,
            Action<object, RoutedEventArgs> addHandler,
            Action<object, RoutedEventArgs> editHandler,
            Action<object, RoutedEventArgs> deleteHandler,
            Action<object, RoutedEventArgs> copyHandler)
        {
            var items = parent.Items;

            Grid grid = new Grid();
            grid.Margin = new Thickness(5);
            grid.HorizontalAlignment = HorizontalAlignment.Stretch;
            grid.VerticalAlignment = VerticalAlignment.Stretch;
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // add 1 for the "New" button
            Enumerable.Range(0, items.Count() + 1).ToList().ForEach(x =>
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            });

            StackPanel p = new StackPanel();
            p.Orientation = Orientation.Horizontal;
            p.Background = GetButtonRowBrush();

            // add a New button under the expander
            var add = new MyButton()
                { 
                    Content = String.Format( Resource.NewItem, parent.DisplayName ), 
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Right, 
                    Tag = parent.CreatePrototype,
                    Style = Application.Current.FindResource("ArrayNewImageButton") as Style
                };
            var bitmap = new System.Windows.Media.Imaging.BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri( @"..\Resources\create.png", UriKind.Relative);
            bitmap.EndInit();
            add.Content = new Image() { Source = bitmap };
            add.ToolTip = "Add a new item of type "+parent.DisplayName;
            add.HorizontalAlignment = HorizontalAlignment.Left;
            add.Click += (sender, args) =>
                {
                    addHandler(sender, args);
                };

            p.SetValue(Grid.ColumnSpanProperty, 2);
            p.SetValue(Grid.RowProperty, 0);

            p.Children.Add(add);
            var description = new TextBlock()
            {
                Text = parent.Description,
                Style = Application.Current.FindResource("addDescText") as Style
            };
            p.Children.Add(description);
            grid.Children.Add(p);
            int i = 1;

            bool nameSet = false;
            foreach (var c in items)
            {
                var l = new MyLabel() { Tag = c };
                if (!nameSet)
                {
                    nameSet = true;
                    l.Content = parent.DisplayName;
                }

                l.ToolTip = c.Description;
                l.SetValue(Grid.ColumnProperty, 0);
                l.SetValue(Grid.RowProperty, i);
                l.Style = Application.Current.FindResource("CfgLabel") as Style;


                var t = new EditItemBox(parent.ArrayItems.Count > parent.Min);
                t.EditClicked += (sender, args) =>
                {
                    editHandler(sender, args);
                };
                t.DelClicked += (sender, args) =>
                {
                    deleteHandler(sender, args);
                };
                t.CopyClicked += (sender, args) =>
                {
                    copyHandler(sender, args);
                };
                t.Loaded += (s, e) =>
                {
                    var cc = s as Control;
                    if (cc != null)
                    {
                        cc.HorizontalAlignment = HorizontalAlignment.Stretch;
                        cc.HorizontalContentAlignment = HorizontalAlignment.Stretch;

                    }
                };
                t.SizeChanged += (s, e) =>
                    {
                        var cc = s as Control;
                        if (cc != null)
                        {
                            //cc.Width = grid.ColumnDefinitions[1].ActualWidth;
                        }
                    };

                t.Tag = c;
                t.ItemName = c.DisplayName;
                t.ToolTip = c.Description;
                t.SetValue(Grid.ColumnProperty, 1);
                t.SetValue(Grid.RowProperty, i);
                t.Style = Application.Current.FindResource("CfgText") as Style;
				

                if ((i & 1) == 0)
				{

                    var gb = GetAltBrush();

                    Rectangle re = new Rectangle();
                    re.Fill = gb;
                    re.SetValue(Grid.RowProperty, i);
                    re.SetValue(Grid.ColumnSpanProperty, 2);
                    grid.Children.Add(re);
                }
                i++;
                grid.Children.Add(l);
                grid.Children.Add(t);

            }
            return grid;
        }

        private static Brush GetAltBrush()
        {
            return new SolidColorBrush() { Color = (Color)Application.Current.FindResource("LightColor") };
        }

        private static Brush GetButtonRowBrush()
        {
            return new SolidColorBrush() { Color = (Color)Application.Current.FindResource("DarkColor") };
        }

        private static Control CreateControl(ITransformModelItem info, Binding binding, Action itemChanged )
        {
            switch (info.Type)
            {
                case RtType.Folder:
                case RtType.UncPath: 
                    return _Folder(info, binding, itemChanged);
                case RtType.Guid: 
                    return _Guid(info, binding, itemChanged);
                case RtType.Bool: 
                    return _Bool(info, binding, itemChanged);
                case RtType.Int32: 
                    return _Int32(info, binding, itemChanged);
                case RtType.Password: 
                    return _Password(info, binding, itemChanged);
                case RtType.String: 
                    return _Default(info, binding, itemChanged);
                case RtType.Enum: 
                    return _ComboBox(info, binding, itemChanged);
                case RtType.HyperLink: 
                    return _HyperLink(info, binding, itemChanged);
                default:
                    if (info.Type == RtType.Custom && TransformModel.Instance.Customs.ContainsKey(info.OriginalType))
                        return TransformModel.Instance.Customs[info.OriginalType].CreateControl(info, binding, itemChanged);
                    else
                        return _Default(info, binding, itemChanged);
            }
        }



        private static Func<ITransformModelItem, Binding, Action, Control> _Folder = (ci, binding, itemChanged) =>
        {
            return _UncPath(ci, binding, itemChanged);
        };

        private static Func<ITransformModelItem, Binding, Action, Control> _UncPath = (ci, binding, itemChanged) =>
        {
            var t = new FolderInputBox(ci.DisplayName, true);
            if (ci.ReadOnly)
                t.IsReadOnly = true;

            binding.Mode = BindingMode.TwoWay;
            t.SetBinding(FolderInputBox.FolderNameProperty, binding);
            t.FolderNameChanged += (o, e) => { itemChanged(); };
            return t;
        };

        private static Func<ITransformModelItem, Binding, Action, Control> _Guid = (ci, binding, itemChanged) =>
        {
            var t = new GuidInput();
            if (ci.ReadOnly)
                t.IsReadOnly = true;

            binding.Mode = BindingMode.TwoWay;
            t.SetBinding(GuidInput.GuidStrProperty, binding);
            t.GuidStrChanged += (o, e) => { itemChanged(); };
            return t;
        };

        private static Func<ITransformModelItem, Binding, Action, Control> _Bool = (ci, binding, itemChanged) =>
        {
            var bib = new BoolInput();
            if (ci.ReadOnly)
                bib.IsEnabled = false;

            binding.Mode = BindingMode.TwoWay;
            bib.SetBinding(BoolInput.BoolProperty, binding);
            bib.BoolChanged += (o, e) => { itemChanged(); };
            return bib;
        };

        private static Func<ITransformModelItem, Binding, Action, ComboBoxInput> _ComboBox = (ci, binding, itemChanged) =>
        {
            var bib = new ComboBoxInput();
            if (ci.ReadOnly)
                bib.IsEnabled = false;

            binding.Mode = BindingMode.TwoWay;
            bib.SetBinding(ComboBoxInput.ComboBoxProperty, binding);

            var listBinding = new Binding();
            listBinding.Source = TransformModel.Instance.Enums[ci.EnumName];
            listBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            bib.SetBinding(ComboBoxInput.ComboBoxListProperty, listBinding);

            bib.ComboBoxChanged += (o, e) => { itemChanged(); };
            return bib;
        };

        private static Func<ITransformModelItem, Binding, Action, Hyperlink> _HyperLink = (ci, binding, itemChanged) =>
        {
            var bib = new Hyperlink();

            try
            {
                bib.NavigateUri = new System.Uri(ci.Value);
                if ( String.IsNullOrWhiteSpace(ci.Description))
                    bib.Text = ci.Value;
                else
                    bib.Text = ci.Description;
            }
            catch
            {
                bib.Text = Resource.InvalidUri + ci.Value;
            }
            return bib;
        };

        private static Func<ITransformModelItem, Binding, Action, Control> _Default = (ci, binding, itemChanged) =>
        {
            var t = new TextBox();
            if (ci.ReadOnly)
                t.IsReadOnly = true;

            t.MinWidth = 150;
            t.SetBinding(TextBox.TextProperty, binding);
            t.TextChanged += (o, e) => { itemChanged(); }; 
            return t;
        };

        private static Func<ITransformModelItem, Binding, Action, Control> _Password = (ci, binding, itemChanged) =>
        {
            var t = new PasswordBox();
            if (ci.ReadOnly)
                t.IsEnabled = false;

            t.MinWidth = 150;
            var value = ci.Value;
            (ci as PasswordTransformModelItem).PasswordBox = t;
            t.Password = value;
            t.PasswordChanged += (o, e) => { itemChanged(); };
            return t;
        };

        private static Func<ITransformModelItem, Binding, Action, Control> _Int32 = (ci, binding, itemChanged ) =>
        {
            var t = new Xceed.Wpf.Toolkit.IntegerUpDown();
            if (ci.ReadOnly)
                t.IsEnabled = false;

            t.Minimum = ci.MinInt;
            t.Maximum = ci.MaxInt;

            t.TextAlignment = TextAlignment.Left;
            t.Value = Int32.Parse(ci.Value ?? "0");

            t.SetBinding(Xceed.Wpf.Toolkit.IntegerUpDown.TextProperty, binding);
            t.ValueChanged += (o, e) => { itemChanged();  };
            return t;
        };

    }
}
