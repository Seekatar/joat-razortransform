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
    /// Interaction logic for NameValueEdit.xaml
    /// </summary>
    public partial class NameValueEdit : UserControl
    {
        List<Expander> _expanders = new List<Expander>();
        IEnumerable<ConfigInfo> _groups;

        public NameValueEdit()
        {
            InitializeComponent();
            DataContext = this;
        }


        /// <summary>
        /// load a single configinfo with children
        /// </summary>
        /// <param name="parent"></param>
        public void Load(ConfigInfo parent)
        {
            _expanders.Clear();
            _stackPanel.Children.Clear();

            Expander expander = createExpander(parent);
            var lastExpanderStackPanel = expander.Content as StackPanel;

            lastExpanderStackPanel.Children.Add(LayoutManager.BuildGridView(parent.Children));
            expander.IsExpanded = true;
        }

        public void Load(IEnumerable<ConfigInfo> groups)
        {
            _groups = groups;

            _expanders.Clear();
            _stackPanel.Children.Clear();

            int i = 0;
            StackPanel lastExpanderStack = null;

            foreach (var item in groups)
            {
                Expander expander = createExpander(item);
                lastExpanderStack = expander.Content as StackPanel;

                var side = new StackPanel() { Orientation = Orientation.Horizontal };
                i = 0;

                if (item.IsArray)
                {
                    // add all the children 
                    if (item.Children.Count > 0 ) // must have 0th for adds at least
                    {
                        // add all the children 
                        if (item.Children.Count > 0) // must have 0th for adds at least
                        {
                            lastExpanderStack.Children.Add( LayoutManager.BuildGridView(item, add_Click, edit_Click, del_Click));
                        }
                        //var expander = createExpander(item);
                        //lastExpanderStack = expander.Content as StackPanel;

                        //// add a New button under the expander
                        //var add = new Button() { Content = "New", HorizontalAlignment = System.Windows.HorizontalAlignment.Right, Tag = item.Children[0] };
                        //add.ToolTip = "Add a new item";
                        //add.Click += new RoutedEventHandler(add_Click);
                        //add.Style = this.Resources["CfgButton"] as Style;
                        //side.Children.Add(add);
                        //side.Background = new SolidColorBrush(Colors.AliceBlue);
                        //lastExpanderStack.Children.Add(side);

                        //i++;

                        //bool nameSet = false;
                        //foreach (var child in item.Children.Skip(1))
                        //{
                        //    var c = child.Key;
                        //    side = new StackPanel() { Orientation = Orientation.Horizontal };
                        //    var l = new Label() { Tag = child };
                        //    if (!nameSet)
                        //    {
                        //        nameSet = true;
                        //        l.Content = c.Name;
                        //    }
                        //    side.Children.Add(l);
                        //    l.ToolTip = c.Description;
                        //    _controls.Add(l);
                        //    l.Style = this.Resources["CfgLabel"] as Style;


                        //    var t = new EditItemBox(item.Children.Count-1 > child.MinCount);
                        //    t.EditClicked += new RoutedEventHandler(edit_Click);
                        //    t.DelClicked += new RoutedEventHandler(del_Click);
                        //    t.Tag = child;
                        //    t.ItemName = child.Key.Value;
                        //    t.ToolTip = c.Description;
                        //    side.Children.Add(t);
                        //    _controls.Add(t);
                        //    if ((i & 1) == 0)
                        //        side.Background = new SolidColorBrush(Colors.AliceBlue);

                        //    i++;
                        //    lastExpanderStack.Children.Add(side);
                        //}
                    }
                }
                else
                {
                    lastExpanderStack.Children.Add(LayoutManager.BuildGridView(item.Children));
                    // createControl(i, lastExpanderStack, item, side);

                    i++;
                }
            }
        }

#if false
		        private void createControl(int i, StackPanel lastExpanderStack, ConfigInfo ci, StackPanel side)
        {
            var l = new Label() { Content = ci.DisplayName };
            side.Children.Add(l);
            l.ToolTip = ci.Description;
            _controls.Add(l);
            l.Style = this.Resources["CfgLabel"] as Style;

            var binding = new Binding();
            binding.Source = ci;
            binding.Path = new PropertyPath("Value");
            binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;

            Control t = null;
            if (String.Equals(ci.Type, "Folder", StringComparison.CurrentCultureIgnoreCase) ||
                String.Equals(ci.Type, "UncPath", StringComparison.CurrentCultureIgnoreCase))
            {
                t = createFolderControl(ci, binding);
            }
            else if (String.Equals(ci.Type, "Guid", StringComparison.CurrentCultureIgnoreCase))
            {
                t = createGuidControl(ci, binding);
            }
            else if (String.Equals(ci.Type, "Bool", StringComparison.CurrentCultureIgnoreCase))
            {
                t = createBoolControl(ci, binding);
            }
            else
            {
                t = createEnumControl(ci, binding);
                if (t == null)
                {
                    // default
                    t = new TextBox();
                    t.SetBinding(TextBox.TextProperty, binding);
                }
            }

            t.Style = this.Resources["CfgText"] as Style;
            t.ToolTip = ci.Description;
            side.Children.Add(t);
            _controls.Add(t);
            if ((i & 1) == 0)
                side.Background = new SolidColorBrush(Colors.AliceBlue);

            if (lastExpanderStack != null)
                lastExpanderStack.Children.Add(side);
            else
                _stackPanel.Children.Add(side);
        }

        private Control createEnumControl(ConfigInfo ci, Binding binding)
        {
            Control t = null;

            // is it an enum?
            if (ConfigSettings.Enums.ContainsKey(ci.Type))
            {
                var bib = new ComboBoxInput();
                t = bib;

                binding.Mode = BindingMode.TwoWay;
                bib.SetBinding(ComboBoxInput.ComboBoxProperty, binding);

                var listBinding = new Binding();
                listBinding.Source = ConfigSettings.Enums[ci.Type];
                listBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                bib.SetBinding(ComboBoxInput.ComboBoxListProperty, listBinding);
            }
            return t;
        }

        private Control createBoolControl(ConfigInfo ci, Binding binding)
        {
            var bib = new BoolInput();
            binding.Mode = BindingMode.TwoWay;
            bib.SetBinding(BoolInput.BoolProperty, binding);
            return bib;
        }

        private Control createGuidControl(ConfigInfo ci, Binding binding)
        {
            var t = new GuidInput();
            binding.Mode = BindingMode.TwoWay;
            t.SetBinding(GuidInput.GuidStrProperty, binding);
            return t;
        }

        private Control createFolderControl(ConfigInfo ci, Binding binding)
        {
            var fib = new FolderInputBox(ci.DisplayName, true);
            binding.Mode = BindingMode.TwoWay;
            fib.SetBinding(FolderInputBox.FolderNameProperty, binding);
            return fib;
        }

  
#endif  
        private Expander createExpander(ConfigInfo ci)
        {
            var expander = new Expander();
            expander.Style = this.Resources["CfgBigLabel"] as Style;
            expander.IsExpanded = ci.Expanded;
            expander.Header = ci.DisplayName;
            if (ci.Description != null)
                expander.ToolTip = ci.Description;
            _stackPanel.Children.Add(expander);
            var lastExpanderStack = new StackPanel();
            expander.Content = lastExpanderStack;
            _expanders.Add(expander);
            return expander;
        }

        void del_Click(object sender, RoutedEventArgs e)
        {
            // find it in the list
            var delMe = (sender as Control).Tag as ConfigInfo;
            if (delMe != null)
            {
                delMe.Parent.Children.Remove(delMe);
                ReLoad();
            }
        }

        /// <summary>
        /// edit an array, copying it during the edit in case of cancel
        /// </summary>
        /// <param name="orig"></param>
        /// <returns></returns>
        bool editArrayItem(ConfigInfo orig)
        {
            var nve = new ArrayItemEdit();
            return nve.ShowDialog(orig);
        }

        void edit_Click(object sender, RoutedEventArgs e)
        {
            if ( editArrayItem((sender as Control).Tag as ConfigInfo) )
                ReLoad();
        }

        private void ReLoad()
        {
            List<string> expandedOnes = new List<string>();
            foreach (var c in _expanders)
            {
                if (c.IsExpanded)
                    expandedOnes.Add(c.Header.ToString());
            }
            Load(_groups);
            foreach (var exp in _expanders.Where(o => expandedOnes.Contains(o.Header.ToString())))
            {
                exp.IsExpanded = true;
            }
            //resizeControls(_stackPanel.ActualWidth);
        }

        void add_Click(object sender, RoutedEventArgs e)
        {
            // create a new one from the 0th as a template
            var newOne = new ConfigInfo((sender as Control).Tag as ConfigInfo);
            if (editArrayItem(newOne))
            {
                // add it to the parent array
                newOne.Parent.Children.Add(newOne);
                ReLoad();
            }
        }


        private void stackPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //resizeControls(e.NewSize.Width);
        }

#if false
		        private void resizeControls(double width)
        {
            var lblWidth = width / 3 - SystemParameters.VerticalScrollBarWidth;
            var txtWidth = width - lblWidth - 10;
            foreach (var c in _controls)
            {
                if (c is Label)
                    c.Width = lblWidth;
                else
                    c.Width = txtWidth;
            }
        }
  
	#endif  
    }
}
