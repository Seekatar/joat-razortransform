using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace RazorTransform
{
    class PasswordTransformModelItem : TransformModelItem
    {
        string _override = null;
        PasswordBox _passwordBox = null;

        public PasswordTransformModelItem(System.Xml.Linq.XElement e, TransformModelItem g) : base(e,g)
        {
        }

        public PasswordBox PasswordBox 
        {
            get { return _passwordBox; }
            set { _passwordBox = value; _passwordBox.Password = _override ?? String.Empty; } 
        }

        public override string Value
        {
            get
            {
                if ( PasswordBox != null )
                    return PasswordBox.Password;
                else
                    return _override ?? String.Empty; 
            }
            set
            {
                if (PasswordBox != null)
                    PasswordBox.Password = value;
                else
                    _override = value;
            }
        }
    }
}
