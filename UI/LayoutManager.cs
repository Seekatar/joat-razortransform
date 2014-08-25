using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using RazorTransform.Controls;
using System.Windows.Documents;
using RazorTransform.Model;
using System.Collections.Generic;

namespace RazorTransform
{
    public static class LayoutManager
    {
        /// <summary>
        /// build a grid view for a model of only simple items, usually this is a tab of items
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static FrameworkElement BuildGridView(IEnumerable<IItemBase> items, bool showHidden, Action itemChanged = null )
        {
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
            foreach (var ci in controls.OfType<IItem>())
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
        /// <param name="list"></param>
        /// <param name="addHandler"></param>
        /// <param name="editHandler"></param>
        /// <param name="deleteHandler"></param>
        /// <returns></returns>
        public static FrameworkElement BuildGridView(IItemList list,
            Action<object, RoutedEventArgs> addHandler,
            Action<object, RoutedEventArgs> editHandler,
            Action<object, RoutedEventArgs> deleteHandler,
            Action<object, RoutedEventArgs> copyHandler)
        {
            var items = list;

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
                    Content = String.Format( Resource.NewItem, list.DisplayName ), 
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Right, 
                    Tag = list,
                    Style = Application.Current.FindResource("ArrayNewImageButton") as Style
                };
            var bitmap = new System.Windows.Media.Imaging.BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri( @"..\Resources\create.png", UriKind.Relative);
            bitmap.EndInit();
            add.Content = new Image() { Source = bitmap };
            add.ToolTip = "Add a new item of type "+list.DisplayName;
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
                Text = list.Description,
                Style = Application.Current.FindResource("addDescText") as Style
            };
            p.Children.Add(description);
            grid.Children.Add(p);
            int i = 1;

            bool nameSet = false;
            foreach (var c in items)
            {
                var l = new MyLabel() { Tag = new ArrayItem(list, c) };
                if (!nameSet)
                {
                    nameSet = true;
                    l.Content = list.DisplayName;
                }

                l.ToolTip = list.Description;
                l.SetValue(Grid.ColumnProperty, 0);
                l.SetValue(Grid.RowProperty, i);
                l.Style = Application.Current.FindResource("CfgLabel") as Style;


                var t = new EditItemBox(list.Count > list.Min);
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

                t.Tag = new ArrayItem(list, c);
                string rawKeyName;
                t.ItemName = list.ModelKeyName(c,out rawKeyName);
                t.ToolTip = rawKeyName;
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

        private static Control CreateControl(IItem item, Binding binding, Action itemChanged )
        {
            if (item.Type == RtType.Custom && ModelConfig.Instance.CustomTypes.ContainsKey(item.OriginalTypeStr))
            {
                return ModelConfig.Instance.CustomTypes[item.OriginalTypeStr].CreateControl(item, binding, itemChanged);
            }
            else if (item.ReadOnly)
            {
                return _Default(item, binding, itemChanged);
            }
            else
            {
                switch (item.Type)
                {
                    case RtType.Folder:
                    case RtType.UncPath:
                        return _Folder(item, binding, itemChanged);
                    case RtType.Guid:
                        return _Guid(item, binding, itemChanged);
                    case RtType.Label:
                        return _Label(item);
                    case RtType.Bool:
                        return _Bool(item, binding, itemChanged);
                    case RtType.Int:
                        return _Int(item, binding, itemChanged);
                    case RtType.Password:
                        return _Password(item, binding, itemChanged);
                    case RtType.String:
                        return _Default(item, binding, itemChanged);
                    case RtType.Enum:
                        return _ComboBox(item, binding, itemChanged);
                    case RtType.HyperLink:
                        return _HyperLink(item, binding, itemChanged);
                    default:
                        return _Default(item, binding, itemChanged);
                }
            }
        }



        private static Control _Folder ( IItem ci, Binding binding, Action itemChanged) 
        {
            return _UncPath(ci, binding, itemChanged);
        }

        private static Control _UncPath (IItem ci, Binding binding, Action itemChanged) 
        {
            var t = new FolderInputBox(ci.DisplayName, true);
            if (ci.ReadOnly)
                t.IsReadOnly = true;

            binding.Mode = BindingMode.TwoWay;
            t.SetBinding(FolderInputBox.FolderNameProperty, binding);
            t.FolderNameChanged += (o, e) => { itemChanged(); };
            return t;
        }

        private static Control _Guid (IItem ci, Binding binding, Action itemChanged) 
        {
            var t = new GuidInput();
            if (ci.ReadOnly)
                t.IsReadOnly = true;

            binding.Mode = BindingMode.TwoWay;
            t.SetBinding(GuidInput.GuidStrProperty, binding);
            t.GuidStrChanged += (o, e) => { itemChanged(); };
            return t;
        }

        private static Control _Bool (IItem ci, Binding binding, Action itemChanged) 
        {
            var bib = new BoolInput();
            if (ci.ReadOnly)
                bib.IsEnabled = false;

            binding.Mode = BindingMode.TwoWay;
            bib.SetBinding(BoolInput.BoolProperty, binding);
            bib.BoolChanged += (o, e) => { itemChanged(); };
            return bib;
        }

        private static ComboBoxInput _ComboBox (IItem ci, Binding binding, Action itemChanged) 
        {
            var bib = new ComboBoxInput();
            if (ci.ReadOnly)
                bib.IsEnabled = false;

            binding.Mode = BindingMode.TwoWay;
            bib.SetBinding(ComboBoxInput.ComboBoxProperty, binding);

            var listBinding = new Binding();
            listBinding.Source = ModelConfig.Instance.Enums[ci.OriginalTypeStr];
            listBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            bib.SetBinding(ComboBoxInput.ComboBoxListProperty, listBinding);

            bib.ComboBoxChanged += (o, e) => { itemChanged(); };
            return bib;
        }

        private static Hyperlink _HyperLink (IItem ci, Binding binding, Action itemChanged) 
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
        }


        private static Control _Label(IItem ci)
        {
            Control t = null;
            t = new Label() { Content = ci.ExpandedValue != null ? ci.ExpandedValue : ci.Value };
            t.MinWidth = 150;
            return t;
        }

        private static Control _Default (IItem ci, Binding binding, Action itemChanged) 
        {
            var t = new TextBox();
            if (ci.ReadOnly)
            {
                t.IsReadOnly = true;
                t.IsEnabled = false;
            }

            t.MinWidth = 150;
            t.SetBinding(TextBox.TextProperty, binding);
            t.TextChanged += (o, e) => { itemChanged(); }; 
            return t;
        }

        private static Control _Password (IItem ci, Binding binding, Action itemChanged) 
        {
            var t = new PasswordBox();
            if (ci.ReadOnly)
                t.IsEnabled = false;

            t.MinWidth = 150;
            var value = ci.Value;
            t.Password = value;
            t.PasswordChanged += (o, e) => { itemChanged(); };
            return t;
        }

        private static Control _Int (IItem ci, Binding binding, Action itemChanged )
        {
            var t = new Xceed.Wpf.Toolkit.LongUpDown();
            if (ci.ReadOnly)
                t.IsEnabled = false;

            t.Minimum = ci.Min;
            t.Maximum = ci.Max;

            t.TextAlignment = TextAlignment.Left;
            t.Value = Int64.Parse(ci.Value ?? "0");

            t.SetBinding(Xceed.Wpf.Toolkit.IntegerUpDown.TextProperty, binding);
            t.ValueChanged += (o, e) => { itemChanged();  };
            return t;
        }

    }
}
