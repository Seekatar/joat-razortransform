﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RazorTransform.Model
{
    public interface IItemBase
    {
        string  Name { get; set; }
        string Description { get; set; }
        IGroup Group { get; set; }
        IModel Parent { get; }
        string DisplayName { get; set; }
        IList<string> VisibilityGroups { get; }
        bool Hidden { get; set; }

        void LoadFromXml(XElement xml, XElement values, IDictionary<string, string> overrides, int rtValuesVersion);
    }
}
