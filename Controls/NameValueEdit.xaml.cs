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
        List<Expander> _expanders = new List<Expander>();
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

            _expanders.Clear();
            _stackPanel.Children.Clear();

            int i = 0;
            StackPanel lastExpanderStack = null;

            foreach (var item in groups)
            {
                Expander expander = createExpander(item);
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

        private Expander createExpander(TransformModelItem ci)
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
