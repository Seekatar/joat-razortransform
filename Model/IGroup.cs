using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RazorTransform.Model
{
    public interface IGroup
    {
        string DisplayName { get; set; }

        string Description { get; set; }

        IList<string> VisibilityGroups { get; }

        bool Hidden { get; set; }

        void LoadFromXml(XElement xmlGroup);
    }
}
