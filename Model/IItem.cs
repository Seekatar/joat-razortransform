using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RazorTransform.Model
{
    public interface IItem : IItemBase
    {
        RtType Type { get; }
        string OriginalTypeStr { get; }
        string ExpandedValueStr { get; set; }
        string ValueStr { get; set; }
        Int64 Min { get; set; }
        Int64 Max { get; set; }
        bool ReadOnly { get; set; }
        string Regex { get; set; }
        bool IsPassword { get; set; }


        /// <summary>
        /// gets the value of ValueStr as Type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T GetValue<T>();

    }
}
