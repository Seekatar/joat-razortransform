using System;
using System.Windows.Controls;
using System.Xml.Linq;

namespace RazorTransform
{
    /// <summary>
    /// class to use the Password box since can't easily to binding
    /// </summary>
    internal class PasswordTransformModelItem : TransformModelItem
    {
        string _override = null;
        PasswordBox _passwordBox = null;

        /// <summary>
        /// </summary>
        /// <param name="parent"></param>
        public PasswordTransformModelItem(TransformModelGroup parent) : base(parent)
        {
        }

        /// <summary>
        /// read from Xml
        /// </summary>
        /// <param name="e"></param>
        /// <param name="g"></param>
        public PasswordTransformModelItem(XElement e, TransformModelGroup g) : base(e,g)
        {
        }

        /// <summary>
        /// password box gui item
        /// </summary>
        public PasswordBox PasswordBox 
        {
            get { return _passwordBox; }
            set { _passwordBox = value; _passwordBox.Password = _override ?? String.Empty; } 
        }

        /// <summary>
        /// override the value, set used when overriding from the command line
        /// get used when getting the value
        /// </summary>
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
