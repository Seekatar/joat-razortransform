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
        public bool ShowDialog( IList<TransformModelGroup> orig, bool showHidden )
        {
            var temp = new List<TransformModelGroup>(orig);
            Load(temp, showHidden);
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if ((ShowDialog() ?? false) && nvEdit.Dirty)
            {
                orig.CopyValueFrom(temp);
                return true;
            }
            return false;

        }

        public void Load(IEnumerable<TransformModelGroup> info, bool showHidden)
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
    }
}
