using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using System.Xml.Linq;
using System.Xml.XPath;

namespace RazorTransform
{
    public static class DictExtension
    {
        public static void LoadFromXml(this Dictionary<string, string> dict, XElement element)
        {
            foreach (var n in element.Nodes().Where(n => { if (n is XElement) return (n as XElement).Name == "value"; else return false; }))
            {
                var x = n as XElement;
                var key = x.Attribute("key");
                var value = x.Attribute("value");
                if (key == null || value == null)
                    throw new ArgumentNullException("Enums values must have key and value");
                dict.Add(key.Value, value.Value);
            }
        }
    }

    /// <summary>
    /// one piece of config info meta data
    /// </summary>
    public class ConfigInfo
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
        public ConfigInfo( XElement x, ConfigInfo parent = null)
        {
            LoadFromXml(x);
            Parent = parent;
        }

        /// <summary>
        /// constructor to clone one overriding the value
        /// </summary>
        /// <param name="i"></param>
        /// <param name="value"></param>
        public ConfigInfo(ConfigInfo i, string value)
        {
            DisplayName = i.DisplayName;
            PropertyName = i.PropertyName;
            Description = i.Description;
            Value = value;
            Type = i.Type;
            Parent = i.Parent;
            Children = new List<ConfigInfo>();
            Children.AddRange(i.Children.Select( o => new ConfigInfo(o) ));
        }

        /// <summary>
        /// copy constructor
        /// </summary>
        /// <param name="i"></param>
        public ConfigInfo(ConfigInfo i)
            : this(i, i.Value)
        {
        }

        public ConfigInfo() {}

        public IEnumerable<string> Arguments { get; set; }

        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Value { get; set; }

        public string PropertyName { get; set; }
        
        public string Type { get; set; }

        // group or parent if nested, null if group
        public ConfigInfo Parent { get; set; }

        // strings so type can figure out what min and max means.  e.g. int, decimal, date
        public string Min { get; set; }
        public string Max { get; set; }

        public int MinInt { get { int i = 0; int.TryParse(Min, out i ); return i;} }
        public int MaxInt { get { int i = int.MaxValue; int.TryParse(Max, out i ); return i;} }
        public Decimal MinDecimal { get { Decimal i = 0; Decimal.TryParse(Min, out i ); return i;} }
        public Decimal MaxDecimal { get { Decimal i = Decimal.MaxValue; Decimal.TryParse(Max, out i ); return i;} }

        // mainly for parent-items
        public  string ArrayValueName { get; set; }
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
        private List<ConfigInfo> _children = new List<ConfigInfo>();

        public List<ConfigInfo> Children
        {
            get { return _children; }
            private set { _children = value; }
        }



        public void LoadFromXml(XElement x)
        {
            _element = x;

            PropertyName = (string)x.Attribute("name");
            Type = (string)x.Attribute("type");
            if (PropertyName == null)
                throw new ArgumentNullException("name");
            if (Type == null)
            {
                if (x.Name == "group")
                    Type = "group";
                else
                    throw new ArgumentNullException("type for property "+PropertyName);
            }

            // if no display name, use propertyname
            DisplayName = (string)x.Attribute("displayName") ?? PropertyName;
            PropertyName = makeName(PropertyName);

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

            ArrayValueName = (string)x.Attribute("arrayValueName");

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


        internal void CopyFrom(ConfigInfo src)
        {
            if (!Object.ReferenceEquals(this, src))
            {
                Parent = src.Parent;
                Children.Clear();
                foreach (var i in src.Children)
                {
                    Children.Add(new ConfigInfo(i));
                }
            }
        }
    }
    

    /// <summary>
    /// class to read and contain the settings to be merged with all the other files
    /// </summary>
    class ConfigSettings
    {
        static IDictionary<string, Dictionary<string, string>> _enums = new Dictionary<string, Dictionary<string, string>>();

        public static IDictionary<string, Dictionary<string, string>> Enums { get { return _enums; } }

        public ExpandoObject GetProperties(bool updateXml, bool htmlEncode, string destinationFolder = null)
        {
            return BuildObject(Info, updateXml, htmlEncode, destinationFolder);
        }

        internal ExpandoObject BuildObject(IEnumerable<ConfigInfo> info, bool updateXml, bool htmlEncode, string destinationFolder = null)
        {
            var ret = new ExpandoObject();
            if ( destinationFolder != null )
                (ret as IDictionary<string, object>).Add("DestinationFolder", destinationFolder);

            foreach (var i in info)
            {
                try
                {
                    if (i.IsArray)
                    {
                        var children = new List<ExpandoObject>();
                        if (i.Children.Count - 1 < i.MinInt)
                            throw new Exception(String.Format(Resource.MinCount, i.DisplayName, i.MinInt));
                        foreach (var c in i.Children.Skip(1))
                        {
                            children.Add(BuildObject(c.Children, false, true, destinationFolder));
                        }
                        (ret as IDictionary<string, object>).Add(i.PropertyName, children);
                    }
                    else if (String.Equals("Bool", i.Type, StringComparison.CurrentCultureIgnoreCase))
                    {
                        bool value = false;
                        if (i.Value != null)
                        {
                            bool result;
                            if (Boolean.TryParse(i.Value, out result))
                                value = result;
                        }
                        (ret as IDictionary<string, object>).Add(i.PropertyName, value);
                    }
                    else
                    {
                        string value = i.Value ?? String.Empty;
                        if (htmlEncode)
                            (ret as IDictionary<string, object>).Add(i.PropertyName, HttpUtility.HtmlEncode(value.Trim()));
                        else
                            (ret as IDictionary<string, object>).Add(i.PropertyName, value.Trim());
                    }

                    if (updateXml)
                    {
                        i.UpdateXml();
                    }
                }
                catch (Exception e)
                {
                    string msg = String.Format(Resource.ProcessError, i.DisplayName, e.BuildMessage());
                    throw new Exception(msg);

                }
            }
            return ret;
        }
        public List<ConfigInfo> Groups { get; private set; }
        public List<ConfigInfo> Info { get; private set; }

        public ConfigSettings()
        {
            Info = new List<ConfigInfo>();
            Groups = new List<ConfigInfo>();
        }

        // load both object def and values from files
        public bool LoadFromFile(Settings settings)
        {
            XDocument doc = null;
            using (var fs = new System.IO.FileStream(settings.ObjectFile, System.IO.FileMode.Open))
            {
                doc = XDocument.Load(fs);
            }
            return LoadValuesFromFile(doc.Root,settings);
        }

        // load object from xml and values from file (if exists)
        public bool LoadValuesFromFile(XElement objectRoot, Settings settings)
        {
            XDocument valueDoc = null;
            if (File.Exists(settings.ValuesFile))
            {
                using (var fs = new System.IO.FileStream(settings.ValuesFile, System.IO.FileMode.Open))
                {
                    valueDoc = XDocument.Load(fs);
                }
            }

            return LoadFromXElement(objectRoot, valueDoc == null ? null : valueDoc.Root, settings.Overrides);
        }

        // load both from XML
        public bool LoadFromXElement(XElement objectRoot, XElement values, IDictionary<string, string> overrides = null)
        {
            foreach (var x in objectRoot.Elements().Where(n => n.Name == "enum"))
            {
                var name = x.Attribute("name");
                if (name == null)
                    throw new ArgumentNullException("name on enum");
                var dict = new Dictionary<string, string>();
                dict.LoadFromXml(x);
                _enums.Add(name.Value, dict);
            }

            foreach (var x in objectRoot.Elements().Where(n => n.Name == "group"))
            {
                var g = new ConfigInfo(x);
                Groups.Add(g);

                if (g.IsArray)
                {
                    loadArray(g, x, values);
                }
                else
                {
                    // add items in the group
                    foreach (var e in x.Elements().Where(n => n.Name == "item"))
                    {
                        var i = new ConfigInfo(e,g);
                        if (overrides.ContainsKey(i.PropertyName))
                            i.Value = overrides[i.PropertyName];
                        else if (values != null)
                        {
                            var v = values.Elements("value").Where(n => (string)n.Attribute("name") == i.PropertyName).Select(n => (string)n.Attribute("value")).SingleOrDefault();
                            if (v != null)
                                i.Value = v;
                        }

                        Info.Add(i);
                        g.Children.Add(i);
                    }
                }
            }
            return true;
        }

        // load a group that is an array
        private void loadArray(ConfigInfo parent, XElement x, XElement values)
        {
            var newOne = new ConfigInfo() { Parent = parent, DisplayName = parent.DisplayName, Description = parent.Description, Type = "Array" };
            Info.Add(parent);
            parent.Children.Add(newOne);

            // add a 0th one for a template for "New" ones
            foreach (var y in x.Elements().Where(n => n.Name == "item" ))
            {
                var i = new ConfigInfo(y,parent);
                newOne.Children.Add(i);
            }

            // load the values from the values file, if it exists
            if (values != null)
            {
                var myValues = values.Elements("value").Where( o => o.Attribute("name").Value == parent.ArrayValueName);
                foreach (var mv in myValues)
                {
                    var nextOne = new ConfigInfo() { Parent = parent };
                    //Info.Add(nextOne);
                    parent.Children.Add(nextOne);

                    foreach (var i in newOne.Children)
                    {
                        var v = mv.Elements().Where(n => n.Attribute("name").Value == i.PropertyName).SingleOrDefault();
                        if (v != null)
                        {
                            nextOne.Children.Add(new ConfigInfo(i, v.Attribute("value").Value));
                        }
                        else
                        {
                            nextOne.Children.Add(new ConfigInfo(i, i.Value));
                        }
                    }
                }
            }
        }

        internal void Save(string _valueFileName)
        {
            var doc = XDocument.Parse("<RtValues/>");
            var root = doc.Root;

            foreach (var item in Info)
            {
                if (item.IsArray)
                {
                    foreach ( var child in item.Children.Skip(1) )
                    {
                        var x = new XElement("value", new XAttribute("name", item.ArrayValueName));

                        root.Add(x);
                        foreach (var childElement in child.Children)
                        {
                            x.Add(new XElement("value", new XAttribute("name", childElement.PropertyName), new XAttribute("value", childElement.Value)));
                        }
                    }
                }
                else
                {
                    var x = new XElement("value", new XAttribute("name", item.PropertyName), new XAttribute("value",item.Value ?? ""));
                    root.Add(x);
                }
            }

            doc.Save(_valueFileName);
        }
    }
}
