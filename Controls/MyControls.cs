using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RazorTransform.Controls
{
#if TELERIK
    public class MyButton : Telerik.Windows.Controls.RadButton
    {
    }
    public class MyTabControl : Telerik.Windows.Controls.RadTabControl
    {
    }
    public class MyTabItem : Telerik.Windows.Controls.RadTabItem
    {
    }
    public class MyLabel : Telerik.Windows.Controls.Label
    {

    }
    public class MyNumericUpDown : Telerik.Windows.Controls.RadNumericUpDown
    {

    }
#else
    public class MyButton : System.Windows.Controls.Button
    {
    }
    public class MyTabControl : System.Windows.Controls.TabControl
    {
    }
    public class MyTabItem : System.Windows.Controls.TabItem
    {
    }
    public class MyLabel : System.Windows.Controls.Label
    {

    }
    public class MyNumericUpDown : System.Windows.Controls.TextBox
    {

    }
#endif
}
