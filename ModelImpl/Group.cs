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

        public string DisplayName
        {
            get;
            set;
        }

        public string Description
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
            set;
        }


        public void LoadFromXml(System.Xml.Linq.XElement xml)
        {
            //   <group name="Strings" description="A few strings" hidden="false">
            Hidden = (bool?)xml.Attribute(Constants.Hidden) ?? false;
            DisplayName = (String)xml.Attribute(Constants.Name) ?? "<no name>";
            Description = (String)xml.Attribute(Constants.Description) ?? String.Empty;
        }
    }
}
