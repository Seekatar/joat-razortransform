using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RazorTransform.Model
{
    public interface IItem : IItemBase, INotifyPropertyChanged
    {
        /// <summary>
        /// Gets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        RtType Type { get; }

        /// <summary>
        /// Gets the original type string, which may be mapped to an internal RtType
        /// </summary>
        /// <value>
        /// The original type string.
        /// </value>
        string OriginalTypeStr { get; }


        /// <summary>
        /// Gets or sets the expanded value after any @Model values expanded
        /// </summary>
        string ExpandedValue { get; set; }

        /// <summary>
        /// Gets or sets the raw value, with @Model unexpanded
        /// </summary>
        string Value { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [read only].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [read only]; otherwise, <c>false</c>.
        /// </value>
        bool ReadOnly { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to save the value to RtValues.  Passwords always return true.
        /// </summary>
        bool NoSaveValue { get; set; }

        /// <summary>
        /// Gets or sets the optional regex for validation
        /// </summary>
        string Regex { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is exported to PowerShell.
        /// </summary>
        bool IsExported { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is password.
        /// </summary>
        bool IsPassword { get; set; }

        /// <summary>
        /// Gets an attributes from the orignal RtObject XML
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>null if doesn't exist</returns>
        string Attribute(string name);

        /// <summary>
        /// gets the value of Value as Type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T GetValue<T>();

    }
}
