using RazorTransform.Model;
using System.Collections.Generic;
using System.Windows;

namespace RazorTransform
{
    /// <summary>
    /// Interaction logic for ArrayItemEdit.xaml
    /// </summary>
    public partial class ArrayItemEdit : Window
    {
        public ArrayItemEdit()
        {
            InitializeComponent();
        }

        /// <summary>
        /// show the dialog for editing the item
        /// </summary>
        /// <param name="info"></param>
        /// <returns>true if the item was saved</returns>
        public bool ShowDialog( IItemList orig, bool showHidden, bool isAdd = false )
        {
            var temp = new ItemList(orig);
            Load(temp, showHidden);
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if ((ShowDialog() ?? false) )
            {
                if ( nvEdit.Dirty)
                    orig.CopyValueFrom(temp);
                return isAdd || nvEdit.Dirty;
            }
            return false;

        }

        public void Load(IItemList info, bool showHidden)
        {
            // I don't think this is correct first parm
            nvEdit.Load(info.Parent, showHidden); 
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public bool Dirty { get { return nvEdit.Dirty; } }
    }
}
