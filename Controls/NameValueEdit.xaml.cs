using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace RazorTransform
{
    /// <summary>
    /// Interaction logic for NameValueEdit.xaml
    /// </summary>
    public partial class NameValueEdit : UserControl
    {
        IEnumerable<TransformModelGroup> _groups;

        public NameValueEdit()
        {
            InitializeComponent();
            DataContext = this;
        }

        /// <summary>
        /// populate the control with items from the groups
        /// </summary>
        /// <param name="groups"></param>
        public void Load(IEnumerable<TransformModelGroup> groups)
        {
            _groups = groups;

            var selectedIndex = _tabCtrl.SelectedIndex;
            _tabCtrl.Items.Clear();

            int i = 0;
            StackPanel lastExpanderStack = null;

            foreach (var group in groups.Where( o => !o.Hidden))
            {
                var expander = createTab(group);

                lastExpanderStack = expander.Content as StackPanel;

                var side = new StackPanel() { Orientation = Orientation.Horizontal };

                if (group is TransformModelArray)
                {
                    lastExpanderStack.Children.Add( LayoutManager.BuildGridView(group as TransformModelArray, add_Click, edit_Click, del_Click, copy_Click));
                }
                else
                {
                    lastExpanderStack.Children.Add(LayoutManager.BuildGridView(group));

                    i++;
                }
            }
            _tabCtrl.SelectedIndex = selectedIndex;
        }

        private TabItem createTab(TransformModelGroup group)
        {
            var tab = new TabItem();
            tab.Style = this.Resources["CfgBigLabel"] as Style;
            tab.Header = group.DisplayName;
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
            if (delMe != null)
            {
                delMe.Parent.Children.Remove(delMe);
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
                copy.ArrayParent.ArrayItems.Add(copy);
                copy.MakeKey();
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
            return nve.ShowDialog(orig);
        }

        private void Reload()
        {
            Load(_groups);
        }

        void add_Click(object sender, RoutedEventArgs e)
        {
            // create a new one from the prototype, set on the tag
            var parent = ((sender as Control).Tag as TransformModelArrayItem);
            var newOne = new TransformModelArrayItem(parent);
            if (editArrayItem(newOne.Groups))
            {
                // add it to the parent array
                parent.ArrayParent.ArrayItems.Add(newOne);
                newOne.MakeKey();
                Reload();
            }
        }
    }
}
