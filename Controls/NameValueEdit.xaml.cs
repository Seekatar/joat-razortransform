using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RazorTransform.Controls;

namespace RazorTransform
{
    /// <summary>
    /// Interaction logic for NameValueEdit.xaml
    /// </summary>
    public partial class NameValueEdit : UserControl
    {
        MyTabControl _tabCtrl;
        IEnumerable<TransformModelGroup> _groups;

        public NameValueEdit()
        {
            InitializeComponent();
            DataContext = this;
            SearchVisibility = System.Windows.Visibility.Collapsed;
            Dirty = false;
        }

        public System.Windows.Visibility SearchVisibility { get; set; }

        /// <summary>
        /// populate the control with items from the groups
        /// </summary>
        /// <param name="groups"></param>
        public void Load(IEnumerable<TransformModelGroup> groups, bool showHidden )
        {
            _groups = groups;

            stackPanel.Children.Clear();

            var selectedIndex = -1;
            if (_tabCtrl != null)
            {
                selectedIndex = _tabCtrl.SelectedIndex;
                _tabCtrl.Items.Clear();
            }

            StackPanel lastExpanderStack = null;
            _tabCtrl = new MyTabControl();
            _tabCtrl.Style = (Style)this.FindResource("tabControlStyle");
            _tabCtrl.Background = Brushes.White;

            foreach (var group in groups.Where( o => showHidden || !o.Hidden))
            {
                var tabItem = createTab(group);

                lastExpanderStack = tabItem.Content as StackPanel;

                var side = new StackPanel() { Orientation = Orientation.Horizontal };

                if (group is TransformModelArray)
                {
                    lastExpanderStack.Children.Add( LayoutManager.BuildGridView(group as TransformModelArray, add_Click, edit_Click, del_Click, copy_Click));
                }
                else
                {
                    lastExpanderStack.Children.Add(LayoutManager.BuildGridView(group, showHidden, setDirty));
                }
            }
            stackPanel.Children.Add(_tabCtrl);
            _tabCtrl.SelectedIndex = selectedIndex;
        }

        public bool Dirty { get; set; }

        private void setDirty()
        {
            Dirty = true;
        }

        private MyTabItem createTab(TransformModelGroup group)
        {
            System.Diagnostics.Debug.WriteLine("Dirty is " + Dirty);
            var tab = new MyTabItem();
            tab.Style = this.FindResource("tabStyle") as Style;
            tab.Header = CreateTabHeader(group.DisplayName);
            if (group.Description != null)
                tab.ToolTip = group.Description;
            _tabCtrl.Items.Add(tab);
            var lastExpanderStack = new StackPanel();
            tab.Content = lastExpanderStack;
            return tab;
        }

        #region Array Button Click handlers

        /// <summary>
        /// delete an item from an array
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void del_Click(object sender, RoutedEventArgs e)
        {
            // find it in the list
            var delMe = (sender as Control).Tag as TransformModelArrayItem;
            if (delMe != null && delMe.Group is TransformModelArray)
            {
                (delMe.Group as TransformModelArray).ArrayItems.Remove(delMe);
                TransformModel.Instance.OnItemDeleted(new ItemChangedArgs() { Group = delMe.Group as TransformModelArray, Item = delMe } );
                setDirty();
                Reload();
            }
        }

        /// <summary>
        /// copy an item in an array
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void copy_Click(object sender, RoutedEventArgs e)
        {
            // find it in the list
            var copyMe = (sender as Control).Tag as TransformModelArrayItem;
            if (copyMe != null)
            {
                var copy = new TransformModelArrayItem(copyMe);
                copy.ArrayGroup.AddArrayItem(copy);
                copy.MakeKey();
                TransformModel.Instance.OnItemAdded(new ItemChangedArgs() { Group = copy.ArrayGroup, Item = copy });
                setDirty();
                Reload();
            }
        }

        /// <summary>
        /// edit an item in an array
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void edit_Click(object sender, RoutedEventArgs e)
        {
            var existingOne = (sender as Control).Tag as TransformModelArrayItem;
            if (editArrayItem(existingOne.Groups))
            {
                existingOne.MakeKey();
                // delete & add for sorting
                (existingOne.Group as TransformModelArray).ArrayItems.Remove(existingOne);
                (existingOne.Group as TransformModelArray).AddArrayItem(existingOne);
                TransformModel.Instance.OnItemChanged(new ItemChangedArgs() { Group = existingOne.ArrayGroup, Item = existingOne });
                Reload();
            }
        }

        void add_Click(object sender, RoutedEventArgs e)
        {
            // create a new one from the prototype, set on the tag
            var parent = ((sender as Control).Tag as TransformModelArrayItem);
            var newOne = new TransformModelArrayItem(parent);
            if (editArrayItem(newOne.Groups))
            {
                // add it to the parent array
                parent.ArrayGroup.AddArrayItem(newOne);
                newOne.MakeKey();
                TransformModel.Instance.OnItemAdded(new ItemChangedArgs() { Group = newOne.ArrayGroup, Item = newOne });
                setDirty();
                Reload();
            }
        }
		
        #endregion        
        
        /// <summary>
        /// edit an array, copying it during the edit in case of cancel
        /// </summary>
        /// <param name="orig"></param>
        /// <returns></returns>
        bool editArrayItem(IList<TransformModelGroup> orig)
        {
            var nve = new ArrayItemEdit();
            nve.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            nve.TrySetOwner(Window.GetWindow(this));
            bool ret = nve.ShowDialog(orig, TransformModel.Instance.Settings.ShowHidden);
            if (ret)
                setDirty();
            return ret;
        }

        private void Reload()
        {
            Load(_groups, TransformModel.Instance.Settings.ShowHidden);
        }


        private FrameworkElement CreateTabHeader(string text)
        {
            TextBlock block = new TextBlock();
            block.Text = text;
			block.Style = (Style)this.FindResource("tabHeader");
          
            return block;
        }

        private bool CompareTabHeader(object left, object right)
        {
            if (left is TextBlock && right is TextBlock)
            {
                TextBlock t1 = left as TextBlock;
                TextBlock t2 = right as TextBlock;

                return String.Compare(t1.Text, t2.Text, true) == 0;
            }
            else
            {
                return false;
            }
        }
    }
}
