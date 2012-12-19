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
        public bool ShowDialog( IList<TransformModelGroup> orig )
        {
            var temp = new List<TransformModelGroup>(orig);
            Load(temp);
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            if (ShowDialog() ?? false)
            {
                orig.CopyValueFrom(temp);
                return true;
            }
            return false;

        }

        public void Load(IEnumerable<TransformModelGroup> info)
        {
            nvEdit.Load(info);
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
