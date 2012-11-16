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
        protected string makeName(string p)
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
        protected XElement _element;

        /// <summary>
        /// constructor to load from Xml 
        /// </summary>
        /// <param name="x"></param>
        public TransformModelItem(XElement x, TransformModelItem parent = null)
        {
            loadFromXml(x);
            Parent = parent;
        }

        /// <summary>
        /// constructor to clone one overriding the value
        /// </summary>
        /// <param name="src"></param>
        /// <param name=Constants.Value></param>
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
        protected List<TransformModelItem> _children = new List<TransformModelItem>();

        public List<TransformModelItem> Children
        {
            get { return _children; }
            protected set { _children = value; }
        }



        protected virtual void loadFromXml(XElement x)
        {
            _element = x;

            PropertyName = (string)x.Attribute(Constants.Name);
            Type = (string)x.Attribute(Constants.Type);
            if (Type != null && Type.ToLower() == Constants.Hidden)
            {
                Type = Constants.String;
                Hidden = true;
            }
            else
                Hidden = (bool?)x.Attribute(Constants.Hidden) ?? false;

            if (PropertyName == null)
                throw new ArgumentNullException(Constants.Name);
            if (Type == null)
            {
                if (x.Name == Constants.Group)
                    Type = Constants.Group;
                else
                    throw new ArgumentNullException("type for property " + PropertyName);
            }

            // if no display name, use propertyname
            DisplayName = (string)x.Attribute(Constants.DisplayName) ?? PropertyName;
            PropertyName =  makeName(PropertyName);

            Description = (string)x.Attribute(Constants.Description) ?? DisplayName;

            if (x.Attribute(Constants.Arguments) != null && !String.IsNullOrEmpty(x.Attribute(Constants.Arguments).Value))
            {
                Arguments = x.Attribute(Constants.Arguments).Value.Split(",".ToCharArray()).Select(y => y.Trim());
            }
            else
            {
                Arguments = new List<string>();
            }
            Min = (string)x.Attribute(Constants.Min) ?? "";
            Max = (string)x.Attribute(Constants.Max) ?? "";
            Expanded = (bool?)x.Attribute(Constants.Expanded) ?? false;

            SetDefaultValue(x);
        }

        // grab the default value from the element and set it
        protected void SetDefaultValue(XElement x)
        {
            if (x.Attribute(Constants.DefaultValue) != null && !String.IsNullOrEmpty(x.Attribute(Constants.DefaultValue).Value))
            {
                this.Value = x.Attribute(Constants.DefaultValue).Value;
            }
            else if (x.Attribute(Constants.ValueProvider) != null)
            {
                var type = Assembly.GetExecutingAssembly().GetTypes().Where(t => typeof(IValueProvider).IsAssignableFrom(t) &&
                                                                            t.Name == x.Attribute(Constants.ValueProvider).Value).FirstOrDefault();
                var provider = Activator.CreateInstance(type) as IValueProvider;
                if (provider != null)
                {
                    var result = provider.GetValue(this);
                    if (result != null)
                        this.Value = String.Format("{0}", result);
                    else
                    {
                        if (x.Attribute(Constants.DefaultValue) != null)
                        {
                            this.Value = x.Attribute(Constants.DefaultValue).Value;
                        }
                    }

                }
            }
        }

        public void UpdateXml()
        {
            _element.SetAttributeValue(Constants.DefaultValue, Value);

            // old files didn't have display name, save it off
            if (_element.Attribute(Constants.DisplayName) == null)
            {
                _element.SetAttributeValue(Constants.DisplayName, DisplayName);
                _element.SetAttributeValue(Constants.Name, PropertyName);
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

        public virtual void Load(XElement values, IDictionary<string, string> overrides, XElement x)
        {
            // add items in the group
            foreach (var e in x.Elements().Where(n => n.Name == Constants.Item))
            {
                TransformModelItem i = null;
                if (((string)e.Attribute(Constants.Type)) == Constants.Password)
                    i = new PasswordTransformModelItem(e, this);
                else
                    i = new TransformModelItem(e, this);
                if (overrides.ContainsKey(i.PropertyName))
                    i.Value = overrides[i.PropertyName];
                else if (values != null)
                {
                    var v = values.Elements(Constants.Value).Where(n => (string)n.Attribute(Constants.Name) == i.PropertyName).Select(n => (string)n.Attribute(Constants.Value)).SingleOrDefault();
                    if (v != null)
                        i.Value = v;
                }

                Children.Add(i);
            }
        }


        public bool IsArray 
        {
            get { return this is TransformModelArray; }
        }
    }

}