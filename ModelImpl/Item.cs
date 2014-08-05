using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RazorTransform.Model
{
    internal class Item : IItem
    {
        List<string> _visibilityGroups = new List<string>();
        XElement _element;

        public Item(IModel parent)
        {
            Parent = parent;
        }

        public Item(Item src) : this(src, src.ValueStr)
        {
        }

        public Item(Item src, string value) : this(src)
        {
            ValueStr = value ?? src.ValueStr;
            ExpandedValueStr = src.ExpandedValueStr;
            DisplayName = src.DisplayName;
            Name = src.Name;
            Description = src.Description;
            Type = src.Type;
            OriginalType = src.OriginalType;
            Group = src.Group;
            MinStr = src.MinStr;
            MaxStr = src.MaxStr;
            EnumName = src.EnumName;
            Regex = src.Regex;
            Parent = src.Parent;
        }

        public IList<string> VisibilityGroups
        {
            get { return _visibilityGroups; }
        }

        public string ValueStr
        {
            get;
            set;
        }

        public string ExpandedValueStr
        {
            get;
            set;
        }

        public void LoadFromXml(System.Xml.Linq.XElement xml, System.Xml.Linq.XElement values, IDictionary<string, string> overrides, int rtValuesVersion)
        {
            loadFromXml(xml);

            SetItemValue(values, overrides, rtValuesVersion);
        }

        public string Name
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }

        /// <summary>
        /// Regular expression validation, may be null
        /// </summary>
        public string Regex 
        { 
            get; 
            set; 
        }

        public string MinStr
        {
            get;
            set;
        }

        public string MaxStr
        {
            get;
            set;
        }

        public IGroup Group
        {
            get;
            set;
        }

        public IModel Parent
        {
            get;
            private set;
        }


        public RtType Type
        {
            get; 
            private set;
        }


        public string DisplayName
        {
            get;
            set;
        }

        public bool ReadOnly
        {
            get;
            set;
        }

        public string OriginalType { get; private set; }
        public string EnumName { get; private set; }

        internal void SetItemValue(XElement values, IDictionary<string, string> overrides, int rtValuesVersion)
        {
            if (overrides != null && overrides.ContainsKey(Name))
            {
                ValueStr = overrides[Name];
            }
            else if (values != null)
            {
                string v = null;
                if (rtValuesVersion == 1)
                {
                    var childValues = values.Elements().Where(n =>
                    {
                        var a = n.Attribute(Constants.Name);
                        if (a != null)
                            return a.Value == Name;
                        else
                            throw new Exception("Missing name on element " + n.ToString());
                    });

                    var val = childValues.SingleOrDefault();
                    if (val != null)
                    {
                        var a = val.Attribute(Constants.Value);
                        if (a != null)
                            ValueStr = val.Attribute(Constants.Value).Value;
                        else
                            throw new Exception("Missing value on element " + v.ToString());
                    }
                }
                else // must be v2
                {
                    var element = values.Elements(Name).SingleOrDefault();
                    if (element != null)
                    {
                        v = element.Value;

                        // show the original value as Value in the model
                        // and filled in one in tooltip
                        var attr = element.Attribute(Constants.Original);
                        if (attr != null && !String.IsNullOrWhiteSpace(attr.Value) && attr.Value.Contains('@'))
                        {
                            ExpandedValueStr = v;
                            v = attr.Value;
                        }
                    }
                }
                if (v != null)
                    ValueStr = v;
            }
        }

        protected virtual void loadFromXml(XElement itemXml)
        {
            _element = itemXml;

            // attributes
            Name = (string)itemXml.Attribute(Constants.Name);
            if (Name == null)
                throw new ArgumentNullException(Constants.Name);

            // if no display name, use Name
            DisplayName = (string)itemXml.Attribute(Constants.DisplayName) ?? Name;
            Name = makeName(Name);

            Description = (string)itemXml.Attribute(Constants.Description) ?? DisplayName;

            OriginalType = (String)itemXml.Attribute(Constants.Type) ?? String.Empty;

            Type = Constants.MapType(OriginalType);

            if (Type == RtType.HiddenString) // old files have hidden as type to indicate hidden string
            {
                Type = RtType.String;
                Group.Hidden = true;
            }
            else if (Type == RtType.Enum)
            {
                EnumName = Constants.GetEnumName(itemXml);
            }
            else
                Group.Hidden = (bool?)itemXml.Attribute(Constants.Hidden) ?? false;

            if (Type == RtType.Invalid)
            {
                throw new Exception("Invalid type for property " + Name);
            }

            ReadOnly = (bool?)itemXml.Attribute(Constants.ReadOnly) ?? false;

            Regex = (string)itemXml.Attribute(Constants.RegEx) ?? String.Empty;
            if (!String.IsNullOrWhiteSpace(Regex) && !TransformModel.Instance.RegExes.ContainsKey(Regex))
            {
                throw new Exception(String.Format(Resource.RegExNotFound, Regex));
            }
            MinStr = (string)itemXml.Attribute(Constants.Min) ?? String.Empty;
            MaxStr = (string)itemXml.Attribute(Constants.Max) ?? String.Empty;

            setDefaultValue(itemXml);

            // no sub elements under <item>
        }

        /// <summary>
        /// grab the default value from the element and set it
        /// </summary>
        /// <param name="x"></param>
        protected virtual void setDefaultValue(XElement x)
        {
            if (x.Attribute(Constants.DefaultValue) != null && !String.IsNullOrEmpty(x.Attribute(Constants.DefaultValue).Value))
            {
                ValueStr = x.Attribute(Constants.DefaultValue).Value;
            }
        }


        /// <summary>
        /// create a property name from the display name
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        protected static string makeName(string p)
        {
            p = p.Trim();
            StringBuilder sb = new StringBuilder();
            bool wasSpace = false;
            foreach (var c in p)
            {
                if (c == ' ')
                {
                    wasSpace = true;
                    continue;
                }
                if (wasSpace)
                    sb.Append(c.ToString().ToUpper());
                else
                    sb.Append(c);
                wasSpace = false;

            }
            return sb.ToString();
        }

    }
}
