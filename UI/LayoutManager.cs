using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace RazorTransform
{
    public static class LayoutManager
    {
        /// <summary>
        /// build a grid view for a group of simple items
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public static FrameworkElement BuildGridView(TransformModelGroup group)
        {
            var items = group.Items;

            Grid grid = new Grid();
            grid.Margin = new Thickness(5);
            grid.HorizontalAlignment = HorizontalAlignment.Stretch;
            grid.VerticalAlignment = VerticalAlignment.Stretch;
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var controls = items.Where(x => !x.Hidden).ToList();
            Enumerable.Range(0, controls.Count).ToList().ForEach(x =>
                {
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0, GridUnitType.Auto) });
                });

            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0, GridUnitType.Star) });


            int i = 0;
            foreach (var ci in controls)
            {

                var l = new Label() { Content = ci.DisplayName };

                l.ToolTip = ci.Description;
                l.SetValue(Grid.ColumnProperty, 0);
                l.SetValue(Grid.RowProperty, i);
                l.Style = Application.Current.Resources["CfgLabel"] as Style;

                var binding = new Binding();
                binding.Source = ci;
                binding.Path = new PropertyPath("Value");
                binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;

                Control t = CreateControl(ci, binding);
                t.ToolTip = ci.Description;
                t.SetValue(Grid.ColumnProperty, 1);
                t.SetValue(Grid.RowProperty, i);
                t.HorizontalAlignment = HorizontalAlignment.Left;
                t.HorizontalContentAlignment = HorizontalAlignment.Stretch;
                t.Style = Application.Current.Resources["CfgText"] as Style;

                if ((i & 1) == 0)
                    l.Background = new SolidColorBrush(Colors.WhiteSmoke);

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
            Enumerable.Range(0, items.Count()+1).ToList().ForEach(x =>
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            });

            // add a New button under the expander
            var add = new Button() { Content = "New "+parent.DisplayName, HorizontalAlignment = System.Windows.HorizontalAlignment.Right, Tag = parent.CreatePrototype };
            add.ToolTip = "Add a new item of type "+parent.DisplayName;
            add.HorizontalAlignment = HorizontalAlignment.Left;
            add.Click += (sender, args) =>
                {
                    addHandler(sender, args);
                };

            add.SetValue(Grid.ColumnSpanProperty, 2);
            add.SetValue(Grid.RowProperty, 0);

            grid.Children.Add(add);
            int i = 1;

            bool nameSet = false;
            foreach (var c in items)
            {
                var l = new Label() { Tag = c };
                if (!nameSet)
                {
                    nameSet = true;
                    l.Content = parent.DisplayName;
                }

                l.ToolTip = c.Description;
                l.SetValue(Grid.ColumnProperty, 0);
                l.SetValue(Grid.RowProperty, i);
                l.Style = Application.Current.Resources["CfgLabel"] as Style;


                var t = new EditItemBox(parent.Children.Count - 1 > parent.Min);
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
                t.SetValue(Grid.ColumnProperty, 1);
                t.SetValue(Grid.RowProperty, i);

                if ((i & 1) == 0)
                    l.Background = new SolidColorBrush(Colors.AliceBlue);

                i++;
                grid.Children.Add(l);
                grid.Children.Add(t);

            }
            return grid;
        }

        private static Control CreateControl(TransformModelItem info, Binding binding)
        {
            switch (info.Type)
            {
                case RtType.Folder:
                case RtType.UncPath: return _Folder(info, binding);
                case RtType.Guid: return _Guid(info, binding);
                case RtType.Bool: return _Bool(info, binding);
                case RtType.Int32: return _Int32(info, binding);
                case RtType.Password: return _Password(info, binding);
                case RtType.String: return _Default(info, binding);
                case RtType.Enum:
                    ComboBoxInput ret = _ComboBox(info, binding);
                    var listBinding = new Binding();
                    listBinding.Source = TransformModel.Enums[info.EnumName];
                    listBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                    ret.SetBinding(ComboBoxInput.ComboBoxListProperty, listBinding);
                    return ret;
                default:
                    return _Default(info, binding);
            }
        }



        private static Func<TransformModelItem, Binding, Control> _Folder = (ci, binding) =>
        {
            return _UncPath(ci, binding);
        };
        private static Func<TransformModelItem, Binding, Control> _UncPath = (ci, binding) =>
        {
            var t = new FolderInputBox(ci.DisplayName, true);

            binding.Mode = BindingMode.TwoWay;
            t.SetBinding(FolderInputBox.FolderNameProperty, binding);
            return t;
        };
        private static Func<TransformModelItem, Binding, Control> _Guid = (ci, binding) =>
        {
            var t = new GuidInput();
            binding.Mode = BindingMode.TwoWay;
            t.SetBinding(GuidInput.GuidStrProperty, binding);
            return t;
        };
        private static Func<TransformModelItem, Binding, Control> _Bool = (ci, binding) =>
            {
                var bib = new BoolInput();

                binding.Mode = BindingMode.TwoWay;
                bib.SetBinding(BoolInput.BoolProperty, binding);
                return bib;
            };
        private static Func<TransformModelItem, Binding, ComboBoxInput> _ComboBox = (ci, binding) =>
        {
            var bib = new ComboBoxInput();

            binding.Mode = BindingMode.TwoWay;
            bib.SetBinding(ComboBoxInput.ComboBoxProperty, binding);
            return bib;
        };
        private static Func<TransformModelItem, Binding, Control> _Default = (ci, binding) =>
        {
            var t = new TextBox();
            t.MinWidth = 150;
            t.SetBinding(TextBox.TextProperty, binding);
            return t;
        };
        private static Func<TransformModelItem, Binding, Control> _Password = (ci, binding) =>
        {
            var t = new PasswordBox();
            t.MinWidth = 150;
            (ci as PasswordTransformModelItem).PasswordBox = t;
            return t;
        };
        private static Func<TransformModelItem, Binding, Control> _Int32 = (ci, binding) =>
        {
            return _Default(ci, binding);
        };
    }
}
