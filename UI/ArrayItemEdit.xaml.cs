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
        public bool ShowDialog( IModel orig, bool showHidden, bool isAdd = false )
        {
            var temp = new RazorTransform.Model.Model(orig,null);
            Load(temp, showHidden);
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if ((ShowDialog() ?? false) )
            {
                if ( nvEdit.Dirty)
                    orig.CopyValuesFrom(temp);
                return isAdd || nvEdit.Dirty;
            }
            return false;

        }

        public void Load(IModel info, bool showHidden)
        {
            nvEdit.Load(info, showHidden); 
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
