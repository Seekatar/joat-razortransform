using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace RazorTransform.UI
{
	/// <summary>
	/// class for tracking array breadcrumbs to show @Model... context menu for 
	/// </summary>
	public class Breadcrumb
    {
        IList<string> _byName = new List<string>();
        IList<string> _byIndex = new List<string>();
        Regex _re = new Regex("^[A-Za-z_]\\w*$");
        const string _invalidKeyMarker = "<<invalidKey>>";

        /// <summary>
        /// Initializes a new instance of the <see cref="Breadcrumb"/> class.
        /// </summary>
        public Breadcrumb() 
        {
            _byIndex.Add("Model.Root");
            _byName.Add("Model.Root");
        }

        /// <summary>
        /// Pushes array values into the breadcrumb.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="key">The key.</param>
        /// <param name="index">The index.</param>
        public void PushArray(string name, string key, int index)
        {
            _byIndex.Add(string.Format("{0}[{1}]", name, index));
            if (_re.IsMatch(key))
                _byName.Add(string.Format("{0}[{1}]", name, key));
            else
                _byName.Add(string.Format("{0}[{1}]", name, index));
        }

        /// <summary>
        /// Pops the last value from the breadcrumb
        /// </summary>
        public void Pop()
        {
            if (_byIndex.Count < 2)
                return;
            _byIndex.RemoveAt(_byIndex.Count - 1);
            _byName.RemoveAt(_byName.Count - 1);
        }

        public int Depth 
        {
            get { return _byIndex.Count - 1; }
        }

        /// <summary>
        /// Sets the context menu for breadcrumb on the item
        /// </summary>
        /// <param name="l">The element.</param>
        /// <param name="itemName">Name of the item.</param>
        public void SetContextMenu(FrameworkElement l, string itemName)
        {
            ContextMenuService.SetShowOnDisabled(l, true);
            l.ContextMenu = new ContextMenu();

            addItem(l, _byName, itemName, Resource.CopyNameTooltip );

            if ( _byName.Count > 1 )
            {
                addItem(l, _byIndex, itemName, Resource.CopyIndexTooltip);
            }
        }

        private void addItem(FrameworkElement l, IList<string> list, string itemName, string tooltip )
        {
            var item = new MenuItem();

            string name = string.Format("@({0}.{1})", String.Join(".", list), itemName);
            if (!l.ContextMenu.HasItems || (string)(l.ContextMenu.Items[0] as MenuItem).CommandParameter != name)
            {
                item.Header = string.Format(Resource.CopyContextMenu, name);
                item.ToolTip = tooltip;
                item.Icon = new System.Windows.Controls.Image
                {
                    Source = new BitmapImage(new Uri(@"..\Resources\Clipboard.png", UriKind.Relative))
                };
                item.CommandParameter = name;
                item.Click += item_Click;
                l.ContextMenu.Items.Add(item);
            }
        }

        static void item_Click(object sender, RoutedEventArgs e)
        {
            var name = (sender as MenuItem).CommandParameter.ToString();
            if (!String.IsNullOrWhiteSpace(name))
                System.Windows.Forms.Clipboard.SetText(name, System.Windows.Forms.TextDataFormat.Text);
        }
    }
}
