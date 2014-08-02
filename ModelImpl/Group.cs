using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RazorTransform.Model
{
    class Group : IGroup
    {
        List<string> _visibilityGroups = new List<string>();

        public string Name
        {
            get;
            set;
        }

        public IList<string> VisibilityGroups
        {
            get { return _visibilityGroups; }
        }


        public bool Hidden
        {
            get;
            private set;
        }
    }
}
