using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
        public bool ShowDialog( TransformModelItem orig )
        {
            var temp = new TransformModelItem(orig);
            Load(temp);
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            if (ShowDialog() ?? false)
            {
                orig.CopyFrom(temp);
                return true;
            }
            return false;

        }

        public void Load(IEnumerable<TransformModelItem> info )
        {
            nvEdit.Load(info);
        }

        public void Load(TransformModelItem parent)
        {
            nvEdit.Load(parent);
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
