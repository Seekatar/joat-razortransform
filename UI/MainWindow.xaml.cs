using System.Collections.Generic;
using System.Windows;

namespace RazorTransform
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, ITransformParentWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        internal void Initialize(Dictionary<string, object> parms, IDictionary<string, string> overrides)
        {
            editControl.Initalize(this,parms, overrides);
        }

        public bool RanTransformOk
        {
            get { return editControl.RanTransformOk; }
        }

        public void SetTitle(string titleSuffix)
        {
            this.Title += titleSuffix;
        }

        public void ProcessingComplete(ProcessingResult results)
        {
            Close();
        }
        public void SendData(Dictionary<string, string> data)
        {
        }
    }
}
