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

        public void Load(IEnumerable<ConfigInfo> info )
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
