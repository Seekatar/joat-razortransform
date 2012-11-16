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
        IEnumerable<TransformModelItem> _groups;

        public NameValueEdit()
        {
            InitializeComponent();
            DataContext = this;
        }

        /// <summary>
        /// load a single configinfo with children
        /// </summary>
        /// <param name="parent"></param>
        public void Load(TransformModelItem parent)
        {
            var groups = new List<TransformModelItem>();
            var copy = new TransformModelItem(parent);
            copy.Children.Clear();
            copy.Children.AddRange(parent.Children.Where(o => !o.IsArray));
            copy.Expanded = true;
            groups.Add(copy);
            groups.AddRange(parent.Children.Where(o => o.IsArray)); //.Select( o => new TransformModelItem(o) ));

            Load(groups);
        }

        /// <summary>
        /// populate the control with items from the groups
        /// </summary>
        /// <param name="groups"></param>
        public void Load(IEnumerable<TransformModelItem> groups)
        {
            _groups = groups;

            _tabCtrl.Items.Clear();

            int i = 0;
            StackPanel lastExpanderStack = null;

            foreach (var item in groups)
            {
                var expander = createTab(item);

                lastExpanderStack = expander.Content as StackPanel;

                var side = new StackPanel() { Orientation = Orientation.Horizontal };

                if (item.IsArray)
                {
                    // add all the children 
                    if (item.Children.Count > 0 ) // must have 0th for adds at least
                    {
                        lastExpanderStack.Children.Add( LayoutManager.BuildGridView(item, add_Click, edit_Click, del_Click));
                    }
                }
                else
                {
                    lastExpanderStack.Children.Add(LayoutManager.BuildGridView(item.Children));

                    i++;
                }
            }
        }

        private TabItem createTab(TransformModelItem ci)
        {
            var tab = new TabItem();
            tab.Style = this.Resources["CfgBigLabel"] as Style;
            tab.Header = ci.DisplayName;
            if (ci.Description != null)
                tab.ToolTip = ci.Description;
            _tabCtrl.Items.Add(tab);
            var lastExpanderStack = new StackPanel();
            tab.Content = lastExpanderStack;
            return tab;
        }

        void del_Click(object sender, RoutedEventArgs e)
        {
            // find it in the list
            var delMe = (sender as Control).Tag as TransformModelItem;
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
        bool editArrayItem(TransformModelItem orig)
        {
            var nve = new ArrayItemEdit();
            return nve.ShowDialog(orig);
        }

        void edit_Click(object sender, RoutedEventArgs e)
        {
            if ( editArrayItem((sender as Control).Tag as TransformModelItem) )
                ReLoad();
        }

        private void ReLoad()
        {
            Load(_groups);
        }

        void add_Click(object sender, RoutedEventArgs e)
        {
            // create a new one from the 0th as a template
            var newOne = new TransformModelItem((sender as Control).Tag as TransformModelItem);
            if (editArrayItem(newOne))
            {
                // add it to the parent array
                newOne.Parent.Children.Add(newOne);
                ReLoad();
            }
        }
    }
}
