using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace RazorTransform
{

    /// <summary>
    /// one piece of config info meta data
    /// </summary>
    public class TransformModelItem : System.Dynamic.DynamicObject, ITransformModelItem
    {
        protected XElement _element;

        /// <summary>
        /// </summary>
        /// <param name="parent"></param>
        public TransformModelItem(TransformModelGroup parent )
        {
            Group = parent;
        }

        /// <summary>
        /// constructor to load from Xml 
        /// </summary>
        /// <param name="x"></param>
        public TransformModelItem(XElement x, TransformModelGroup parent = null)
        {
            loadFromXml(x);
            Group = parent;
        }

        /// <summary>
        /// constructor to clone one overriding the value
        /// </summary>
        /// <param name="src"></param>
        /// <param name=Constants.Value></param>
        public TransformModelItem(TransformModelItem src, string value)
        {
            Value = value ?? src.Value;
            DisplayName = src.DisplayName;
            PropertyName = src.PropertyName;
            Description = src.Description;
            Type = src.Type;
            OriginalType = src.OriginalType;
            Group = src.Group;
            Min = src.Min;
            Max = src.Max;
            EnumName = src.EnumName;
            Hidden = src.Hidden;
        }

        /// <summary>
        /// copy constructor
        /// </summary>
        /// <param name="src"></param>
        public TransformModelItem(TransformModelItem src)
            : this(src, src.Value)
        {
        }

        /// <summary>
        /// default constructor
        /// </summary>
        public TransformModelItem() { }

        /// <summary>
        /// name to show in the UI
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// tool tip
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// name of property for this item when on ExpandoObject
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// type of item see Contants.cs
        /// </summary>
        public RtType Type { get; set; }

        /// <summary>
        /// raw type from Xml
        /// </summary>
        public string OriginalType { get; set; }

        /// <summary>
        /// Is this item to be shown in the UI
        /// </summary>
        public bool Hidden { get; set; }

        /// <summary>
        /// Is this item readOnly in the UI
        /// </summary>
        public bool ReadOnly { get; set; }

        // strings so type can figure out what min and max means.  e.g. int, decimal, date

        /// <summary>
        /// Min value as string, empty of null if not used
        /// </summary>
        public string Min { get; set; }

        /// <summary>
        /// Max value as string, empty of null if not used
        /// </summary>
        public string Max { get; set; }

        /// <summary>
        /// Min value as an integer
        /// </summary>
        public int MinInt { get { int i; return int.TryParse(Min, out i) ? i : int.MinValue; } }

        /// <summary>
        /// Max value as an integer
        /// </summary>
        public int MaxInt { get { int i; return int.TryParse(Max, out i) && i != 0 ? i : int.MaxValue; } }

        /// <summary>
        /// Min value as an Decimal
        /// </summary>
        public Decimal MinDecimal { get { Decimal i; return Decimal.TryParse(Min, out i) ? i : Decimal.MinValue; } }

        /// <summary>
        /// Max value as an Decimal
        /// </summary>
        public Decimal MaxDecimal { get { Decimal i; return Decimal.TryParse(Max, out i) && i != 0 ? i : Decimal.MaxValue; } }

        /// <summary>
        /// RegEx validation
        /// </summary>
        public string RegEx { get; set; }

        /// <summary>
        /// group we're in 
        /// </summary>
        public ITransformModelGroup Group { get; set; }

        /// <summary>
        /// current value of the item
        /// </summary>
        public virtual string Value { get; set; }

        /// <summary>
        /// current value of the item, with macros e.g @Model.* in it
        /// </summary>
        public virtual string ExpandedValue { get; set; }

        /// <summary>
        /// gets the value of Value as Type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetValue<T>()
        {
            return (T)Convert.ChangeType(this.Value, typeof(T));
        }

        /// <summary>
        /// if an enum, this is the name
        /// </summary>
        public string EnumName { get; set; }

        /// <summary>
        /// is this item an array
        /// </summary>
        public bool IsArrayItem
        {
            get { return this is TransformModelArrayItem; }
        }

        /// <summary>
        /// load this one item from the Xml
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="values"></param>
        /// <param name="overrides"></param>
        public virtual void LoadFromXml(XElement xml, XElement values, IDictionary<string, string> overrides, int rtValuesVersion )
        {
            loadFromXml(xml);


            SetItemValue(values, overrides, rtValuesVersion);
        }

        internal void SetItemValue(XElement values, IDictionary<string, string> overrides, int rtValuesVersion)
        {
            if (overrides != null && overrides.ContainsKey(PropertyName))
            {
                Value = overrides[PropertyName];
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
                            return a.Value == PropertyName;
                        else
                            throw new Exception("Missing name on element " + n.ToString());
                    });

                    var val = childValues.SingleOrDefault();
                    if (val != null)
                    {
                        var a = val.Attribute(Constants.Value);
                        if (a != null)
                            Value = val.Attribute(Constants.Value).Value;
                        else
                            throw new Exception("Missing value on element " + v.ToString());
                    }
                }
                else // must be v2
                {
                    var element = values.Elements(PropertyName).SingleOrDefault();
                    if (element != null)
                    {
                        v = element.Value;

                        // show the original value as Value in the model
                        // and filled in one in tooltip
                        var attr = element.Attribute(Constants.Original);
                        if (attr != null && !String.IsNullOrWhiteSpace(attr.Value) && attr.Value.Contains('@') )
                        {
                            ExpandedValue = v;
                            v = attr.Value;
                        }
                    }
                }
                if (v != null)
                    Value = v;
            }
        }


        /// <summary>
        /// load the item from xml
        /// </summary>
        /// <param name="itemXml"></param>
        protected virtual void loadFromXml(XElement itemXml)
        {
            _element = itemXml;

            // attributes
            PropertyName = (string)itemXml.Attribute(Constants.Name);
            if (PropertyName == null)
                throw new ArgumentNullException(Constants.Name);

            // if no display name, use propertyname
            DisplayName = (string)itemXml.Attribute(Constants.DisplayName) ?? PropertyName;
            PropertyName = makeName(PropertyName);

            Description = (string)itemXml.Attribute(Constants.Description) ?? DisplayName;

            OriginalType = (String)itemXml.Attribute(Constants.Type) ?? String.Empty;

            Type = Constants.MapType(OriginalType);

            if (Type == RtType.HiddenString) // old files have hidden as type to indicate hidden string
            {
                Type = RtType.String;
                Hidden = true;
            }
            else if (Type == RtType.Enum)
            {
                EnumName = Constants.GetEnumName(itemXml);
            }
            else
                Hidden = (bool?)itemXml.Attribute(Constants.Hidden) ?? false;

            if (Type == RtType.Invalid)
            {
                throw new Exception("Invalid type for property " + PropertyName);
            }

            ReadOnly = (bool?)itemXml.Attribute(Constants.ReadOnly) ?? false;

            RegEx = (string)itemXml.Attribute(Constants.RegEx) ?? String.Empty;
            if (!String.IsNullOrWhiteSpace(RegEx) && !TransformModel.Instance.RegExes.ContainsKey(RegEx))
            {
                throw new Exception(String.Format(Resource.RegExNotFound, RegEx));
            }
            Min = (string)itemXml.Attribute(Constants.Min) ?? String.Empty;
            Max = (string)itemXml.Attribute(Constants.Max) ?? String.Empty;

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
                this.Value = x.Attribute(Constants.DefaultValue).Value;
            }
        }

        public virtual void UpdateXml()
        {
            _element.SetAttributeValue(Constants.DefaultValue, Value);

            // old files didn't have display name, save it off
            if (_element.Attribute(Constants.DisplayName) == null)
            {
                _element.SetAttributeValue(Constants.DisplayName, DisplayName);
                _element.SetAttributeValue(Constants.Name, PropertyName);
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