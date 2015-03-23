using System;
using System.Windows.Controls;
using System.Xml.Linq;

namespace RazorTransform.Model
{
    /// <summary>
    /// class to use the Password box since can't easily do binding
    /// </summary>
    internal class PasswordItem : Item
    {
        public PasswordItem(IModel parent, IGroup group) : base(parent, group)
        {
        }


        public PasswordItem(PasswordItem src, IModel parent)
            : base(src, parent)
        {
            GetPassword = src.GetPassword;
            SetPassword = src.SetPassword;
        }

        public PasswordItem(PasswordItem src, IGroup group)
            : this(src, src.Value, group)
        {
        }

        public PasswordItem(PasswordItem src, string value, IGroup group)
            : base(src, value, group)
        {
            GetPassword = src.GetPassword;
            SetPassword = src.SetPassword;
        }

        /// <summary>
        /// The get password
        /// </summary>
        public Func<string> GetPassword;

        /// <summary>
        /// The set password
        /// </summary>
        public Action<string> SetPassword;

        /// <summary>
        /// override the value, set used when overriding from the command line
        /// get used when getting the value
        /// </summary>
        public override string Value
        {
            get
            {
                if (GetPassword != null)
                    return GetPassword();
                else
                    return base.Value;
            }
            set
            {
                if (SetPassword != null)
                    SetPassword(value);
                else
                    base.Value = value;
            }
        }
    }
}
