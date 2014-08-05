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
        IList<string> VisibilityGroups { get; }

        RtType Type { get; }
        string ExpandedValueStr { get; set; }
        string ValueStr { get; set; }
        string MinStr { get; set; }
        string MaxStr { get; set; }
        bool ReadOnly { get; set; }
        string Regex { get; set; }

    }
}
