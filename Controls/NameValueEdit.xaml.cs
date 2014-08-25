using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RazorTransform.Controls;
using System.Threading.Tasks;
using RazorTransform.Model;

namespace RazorTransform
{
    /// <summary>
    /// Interaction logic for NameValueEdit.xaml
    /// </summary>
    public partial class NameValueEdit : UserControl
    {
        MyTabControl _tabCtrl;
        IModel _model;

        public NameValueEdit()
        {
            InitializeComponent();
            DataContext = this;
            SearchVisibility = System.Windows.Visibility.Collapsed;
            Dirty = false;
        }

        public System.Windows.Visibility SearchVisibility { get; set; }

        /// <summary>
        /// populate the control with items from the model
        /// </summary>
        /// <param name="model"></param>
        public void Load(IModel model, bool showHidden )
        {
            _model = model;

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

            var groups = model.Items.GroupBy( o => o.Group ).Where( o => showHidden || !o.Key.Hidden);
            foreach (var group in groups )
            {
                if (group.Count() == 0)
                    continue; // empty tab?

                var tabItem = createTab(group.Key);

                lastExpanderStack = tabItem.Content as StackPanel;

                var side = new StackPanel() { Orientation = Orientation.Horizontal };

                var first = group.First();
                if (first is IItemList) // it's an array
                {
                    lastExpanderStack.Children.Add( LayoutManager.BuildGridView(first as IItemList, add_Click, edit_Click, del_Click, copy_Click));
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

        /// <summary>
        /// Create the tab for a group
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        private MyTabItem createTab(IGroup group)
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
        async void del_Click(object sender, RoutedEventArgs e)
        {
            // find it in the list
            var delMe = (sender as Control).Tag as ArrayItem;
            if (delMe != null)
            {
                var list = delMe.List;

                if (list != null)
                {
                    list.Remove(delMe.Model);
                    ModelConfig.Instance.OnItemDeleted(new ItemChangedArgs() { List = list, Item = delMe.Model });
                    setDirty();
                    await RazorTransformer.Instance.RefreshModelAsync(false, true);
                    reload();
                }
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
            var copyMe = (sender as Control).Tag as ArrayItem;
            if (copyMe != null)
            {
                var list = copyMe.List;
                var copy = new RazorTransform.Model.Model(copyMe.Model,null);
                list.Add(copy);
                ModelConfig.Instance.OnItemAdded(new ItemChangedArgs() { List = list, Item = copy });
                setDirty();
                reload();
            }
        }

        /// <summary>
        /// edit an item in an array
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async void edit_Click(object sender, RoutedEventArgs e)
        {
            var existingOne = (sender as Control).Tag as ArrayItem;
            if (existingOne != null)
            {
                var list = existingOne.List;

                // set the parent
                var editedModel = editArrayItem(existingOne.Model);
                if (editedModel != null)
                {
                    // delete & add for sorting
                    list.Remove(existingOne.Model);
                    list.Add(editedModel);
                    ModelConfig.Instance.OnItemChanged(new ItemChangedArgs() { List = list, Item = editedModel });
                    await RazorTransformer.Instance.RefreshModelAsync(false, true);
                    reload();
                }
            }
        }

        private async void add_Click(object sender, RoutedEventArgs e)
        {
            // create a new one from the prototype, set on the tag
            var list = ((sender as Control).Tag as IItemList);
            var newOne = new RazorTransform.Model.Model(list.Prototype,null);
            var editedModel = editArrayItem(newOne, true);
            if (editedModel != null)
            {
                // add it to the parent array
                list.Add(newOne);
                ModelConfig.Instance.OnItemAdded(new ItemChangedArgs() { List = list, Item = editedModel });
                setDirty();
                await RazorTransformer.Instance.RefreshModelAsync(false, true);
                reload();
            }
        }
		
        #endregion        
        
        /// <summary>
        /// edit an array, copying it during the edit in case of cancel
        /// </summary>
        /// <param name="orig"></param>
        /// <returns>edited Model, or null if no changes</returns>
        IModel editArrayItem(IModel orig, bool isAdd = false)
        {
            var nve = new ArrayItemEdit();
            nve.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            nve.TrySetOwner(Window.GetWindow(this));
            var temp = nve.ShowDialog(orig, Settings.Instance.ShowHidden, isAdd );
            if (temp != null )
                setDirty();
            return temp;
        }

        private void reload()
        {
            Load(_model, Settings.Instance.ShowHidden);
        }


        private FrameworkElement CreateTabHeader(string text)
        {
            TextBlock block = new TextBlock();
            block.Text = text;
			block.Style = (Style)this.FindResource("tabHeader");
          
            return block;
        }
    }
}
