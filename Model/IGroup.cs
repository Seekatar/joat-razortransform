using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RazorTransform.Model
{
    public interface IGroup
    {
        string Name { get; set; }

        IList<string> VisibilityGroups { get; }

        bool Hidden { get; set; }
    }
}
