using RazorTransform.Model;
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
        /// <returns>updated model if the item was saved, null if not</returns>
        public IModel ShowDialog( IModel orig, bool showHidden, bool isAdd = false )
        {
            // if add, no need to copy it
            var temp = isAdd ? orig : new RazorTransform.Model.Model(orig,null);
            Load(temp, showHidden);
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if ((ShowDialog() ?? false) )
            {
                if ( isAdd || nvEdit.Dirty )
                    return temp;
            }
            return null;

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
