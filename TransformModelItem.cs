using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.Linq;
using System.Reflection;

namespace RazorTransform
{

    /// <summary>
    /// one piece of config info meta data
    /// </summary>
    public class TransformModelItem
    {
        private string makeName(string p)
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
        private XElement _element;

        /// <summary>
        /// constructor to load from Xml 
        /// </summary>
        /// <param name="x"></param>
        public TransformModelItem(XElement x, TransformModelItem parent = null)
        {
            LoadFromXml(x);
            Parent = parent;
        }

        /// <summary>
        /// constructor to clone one overriding the value
        /// </summary>
        /// <param name="src"></param>
        /// <param name="value"></param>
        public TransformModelItem(TransformModelItem src, string value)
        {
            DisplayName = src.DisplayName;
            PropertyName = src.PropertyName;
            Description = src.Description;
            Value = value;
            Type = src.Type;
            Parent = src.Parent;
            Children = new List<TransformModelItem>();
            Children.AddRange(src.Children.Select(o => { var ret = new TransformModelItem(o); ret.Parent = this; return ret; }));
            ArrayValueName = src.ArrayValueName;
            Min = src.Min;
            Max = src.Max;
            Expanded = src.Expanded;
        }

        /// <summary>
        /// copy constructor
        /// </summary>
        /// <param name="src"></param>
        public TransformModelItem(TransformModelItem src)
            : this(src, src.Value)
        {
        }

        public TransformModelItem() { }

        public IEnumerable<string> Arguments { get; set; }

        public string DisplayName { get; set; }
        public string Description { get; set; }
        public virtual string Value { get; set; }

        public string PropertyName { get; set; }

        public string Type { get; set; }

        public bool Hidden { get; set; }

        // group or parent if nested, null if group
        public TransformModelItem Parent { get; set; }

        // strings so type can figure out what min and max means.  e.g. int, decimal, date
        public string Min { get; set; }
        public string Max { get; set; }

        public int MinInt { get { int i; return int.TryParse(Min, out i) ? i : int.MinValue; } }
        public int MaxInt { get { int i; return int.TryParse(Max, out i) ? i : int.MaxValue; } }
        public Decimal MinDecimal { get { Decimal i; return Decimal.TryParse(Min, out i) ? i : Decimal.MinValue; } }
        public Decimal MaxDecimal { get { Decimal i; return Decimal.TryParse(Max, out i) ? i : Decimal.MaxValue; } }

        // mainly for parent-items
        public string ArrayValueName { get; set; }
        public bool IsArray { get { return !String.IsNullOrWhiteSpace(ArrayValueName); } }
        public bool Expanded { get; set; }

        /// <summary>
        /// gets the value of Value as Type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetValue<T>()
        {
            return (T)Convert.ChangeType(this.Value, typeof(T));
        }
        private List<TransformModelItem> _children = new List<TransformModelItem>();

        public List<TransformModelItem> Children
        {
            get { return _children; }
            private set { _children = value; }
        }



        public void LoadFromXml(XElement x)
        {
            _element = x;

            PropertyName = (string)x.Attribute("name");
            Type = (string)x.Attribute("type");
            if (Type != null && Type.ToLower() == "hidden")
            {
                Type = "string";
                Hidden = true;
            }
            else
                Hidden = (bool?)x.Attribute("hidden") ?? false;

            if (PropertyName == null)
                throw new ArgumentNullException("name");
            if (Type == null)
            {
                if (x.Name == "group")
                    Type = "group";
                else
                    throw new ArgumentNullException("type for property " + PropertyName);
            }

            // if no display name, use propertyname
            DisplayName = (string)x.Attribute("displayName") ?? PropertyName;
            ArrayValueName = (string)x.Attribute("arrayValueName");
            PropertyName =  IsArray ? ArrayValueName : makeName(PropertyName);

            Description = (string)x.Attribute("description") ?? DisplayName;

            if (x.Attribute("arguments") != null && !String.IsNullOrEmpty(x.Attribute("arguments").Value))
            {
                Arguments = x.Attribute("arguments").Value.Split(",".ToCharArray()).Select(y => y.Trim());
            }
            else
            {
                Arguments = new List<string>();
            }
            Min = (string)x.Attribute("min") ?? "";
            Max = (string)x.Attribute("max") ?? "";
            Expanded = (bool?)x.Attribute("expanded") ?? false;

            SetDefaultValue(x);
        }

        // grab the default value from the element and set it
        private void SetDefaultValue(XElement x)
        {
            if (x.Attribute("defaultValue") != null && !String.IsNullOrEmpty(x.Attribute("defaultValue").Value))
            {
                this.Value = x.Attribute("defaultValue").Value;
            }
            else if (x.Attribute("valueProvider") != null)
            {
                var type = Assembly.GetExecutingAssembly().GetTypes().Where(t => typeof(IValueProvider).IsAssignableFrom(t) &&
                                                                            t.Name == x.Attribute("valueProvider").Value).FirstOrDefault();
                var provider = Activator.CreateInstance(type) as IValueProvider;
                if (provider != null)
                {
                    var result = provider.GetValue(this);
                    if (result != null)
                        this.Value = String.Format("{0}", result);
                    else
                    {
                        if (x.Attribute("defaultValue") != null)
                        {
                            this.Value = x.Attribute("defaultValue").Value;
                        }
                    }

                }
            }
        }

        public void UpdateXml()
        {
            _element.SetAttributeValue("defaultValue", Value);

            // old files didn't have display name, save it off
            if (_element.Attribute("displayName") == null)
            {
                _element.SetAttributeValue("displayName", DisplayName);
                _element.SetAttributeValue("name", PropertyName);
            }
        }


        internal void CopyFrom(TransformModelItem src)
        {
            if (!Object.ReferenceEquals(this, src))
            {
                Parent = src.Parent;
                Children.Clear();
                foreach (var i in src.Children)
                {
                    Children.Add(new TransformModelItem(i));
                }
            }
        }
    }

}