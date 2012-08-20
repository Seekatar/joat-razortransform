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
        public static void LoadFromXml( this Dictionary<string,string> dict, XElement element )
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

    public class ChildConfigInfo
    {
        public ChildConfigInfo(ChildConfigInfo src)
        {
            Children = new List<ConfigInfo>();
            CopyFrom(src);
        }

        public ChildConfigInfo(ConfigInfo configInfo)
        {
            Parent = configInfo;
            Children = new List<ConfigInfo>();
        }

        private int _minCount = 0;

        public int MinCount
        {
            get { return _minCount; }
            set { _minCount = value; }
        }

        public IList<ConfigInfo> Children { get; set; }
        public ConfigInfo Parent { get; set; }
        public ConfigInfo Key
        {
            get
            {
                if (Children != null && Children.Count() > 0)
                    return Children[0];
                else
                    return null;
            }
        }

        internal void CopyFrom(ChildConfigInfo src)
        {
            if (!Object.ReferenceEquals(this, src))
            {
                MinCount = src.MinCount;
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

        public ConfigInfo() { }

        /// <summary>
        /// constructor to load from Xml 
        /// </summary>
        /// <param name="x"></param>
        public ConfigInfo(XElement x, string destinationFolder)
        {
            LoadFromXml(x, destinationFolder);
        }

        /// <summary>
        /// constructor to clone one overriding the value
        /// </summary>
        /// <param name="i"></param>
        /// <param name="value"></param>
        public ConfigInfo(ConfigInfo i, string value)
        {
            Name = i.Name;
            PropName = i.PropName;
            Description = i.Description;
            Value = value;
            Type = i.Type;
        }

        /// <summary>
        /// copy constructor
        /// </summary>
        /// <param name="i"></param>
        public ConfigInfo(ConfigInfo i)
            : this(i, i.Value)
        {
        }

        public string Name { get; set; }
        public string PropName { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string Value { get; set; }
        public string Range { get; set; }
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
        private List<ChildConfigInfo> _children = new List<ChildConfigInfo>();

        public List<ChildConfigInfo> Children
        {
            get { return _children; }
            private set { _children = value; }
        }

        

        public void LoadFromXml(XElement x, String destinationFolder)
        {
            _element = x;

            PropName = x.Attribute("name").Value;
            if (x.Attribute("displayName") != null)
                Name = x.Attribute("displayName").Value;
            else
                Name = PropName;
            PropName = makeName(PropName);

            Type = x.Attribute("type").Value;
            if (x.Attribute("description") != null)
                Description = x.Attribute("description").Value;
            if (x.Attribute("range") != null && !String.IsNullOrEmpty(x.Attribute("range").Value))
            {
                Range = x.Attribute("range").Value;
            }
            
            if (x.Attribute("expanded") != null)
            {
                this.Expanded = Boolean.Parse(x.Attribute("expanded").Value);
            }
            else
            {
                this.Expanded = false;
            }
			
            SetValue(x);

            if (Type == "Array")
            {
                Children = new List<ChildConfigInfo>();
                var newOne = new ChildConfigInfo(this);
                Children.Add(newOne);

                int minCount = 0;
                if (x.Attribute("minCount") != null && Int32.TryParse(x.Attribute("minCount").Value, out minCount))
                {
                    newOne.MinCount = minCount;
                }

                // add a 0th one for a template for "New" ones
                foreach (var y in x.Nodes().Where(n => { if (n is XElement) return (n as XElement).Name == "item"; else return false; }))
                {
                    var i = new ConfigInfo(y as XElement, destinationFolder);
                    newOne.Children.Add(i);
                }
                if (x.Attribute("loadFrom") != null && x.Attribute("rootNode") != null)
                {
                    var fname = x.Attribute("loadFrom").Value;
                    if (!File.Exists(fname))
                    {
                        if (File.Exists(Path.Combine(destinationFolder, fname)))
                            fname = Path.Combine(destinationFolder, fname);
                        else
                            return; // if file doesn't exist, ok
                    }

                    try
                    {
                        var fs = new System.IO.FileStream(fname, System.IO.FileMode.Open);
                        XDocument kids = XDocument.Load(fs);
                        var nodes = (IEnumerable)kids.XPathEvaluate(x.Attribute("rootNode").Value);

                        // Add each one found in the loadFromXml, using the 0th one as the template
                        foreach (var n in nodes)
                        {
                            var nextOne = new ChildConfigInfo(this);
                            nextOne.MinCount = minCount;
                            Children.Add(nextOne);

                            foreach (var i in Children[0].Children)
                            {
                                if (n is XElement && (n as XElement).Attribute(i.PropName) != null)
                                {
                                    nextOne.Children.Add(new ConfigInfo(i, (n as XElement).Attribute(i.PropName).Value));
                                }
                                else
                                {
                                    nextOne.Children.Add(new ConfigInfo(i, String.Empty));
                                }
                            }
                        }
                        fs.Close();

                    }
                    catch
                    {
                        throw new Exception("Error loading child XML");
                    }
                }
            }

        }

        private void SetValue(XElement x)
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
        }

    }

   
    /// <summary>
    /// class to read and contain the settings to be merged with all the other files
    /// </summary>
    class ConfigSettings
    {
        string _destinationFolder;
        static IDictionary<string, Dictionary<string, string>> _enums = new Dictionary<string, Dictionary<string, string>>();

        public static IDictionary<string, Dictionary<string, string>> Enums { get { return _enums; } }

        public ExpandoObject GetProperties(bool updateXml, bool htmlEncode)
        {
            return buildObject(Info, updateXml, htmlEncode);
        }

        ExpandoObject buildObject(IEnumerable<ConfigInfo> info, bool updateXml, bool htmlEncode)
        {
            var ret = new ExpandoObject();
            (ret as IDictionary<string, object>).Add("DestinationFolder", _destinationFolder);


            foreach (var i in info)
            {
                try
                {
                    if (String.Equals("Label", i.Type, StringComparison.CurrentCultureIgnoreCase))
                        continue;

                    if (String.Equals("Array", i.Type, StringComparison.CurrentCultureIgnoreCase))
                    {
                        var children = new List<ExpandoObject>();
                        if (i.Children.Count - 1 < i.Children.First().MinCount)
                            throw new Exception(String.Format(Resource.MinCount, i.Name, i.Children.First().MinCount));
                        foreach (var c in i.Children.Skip(1))
                        {
                            children.Add(buildObject(c.Children, false, true));
                        }
                        (ret as IDictionary<string, object>).Add(i.PropName, children);
                    }
                    else if (String.Equals("Bool", i.Type, StringComparison.CurrentCultureIgnoreCase))
                    {
                        bool value = false;
                        if ( i.Value != null )
                        {
                            bool result;
                            if (Boolean.TryParse(i.Value, out result))
                                value = result;
                        }
                        (ret as IDictionary<string, object>).Add(i.PropName, value);
                    }
                    else
                    {
                        string value = i.Value ?? String.Empty;
                        if (htmlEncode)
                            (ret as IDictionary<string, object>).Add(i.PropName, HttpUtility.HtmlEncode(value.Trim()));
                        else
                            (ret as IDictionary<string, object>).Add(i.PropName, value.Trim());
                    }

                    if (updateXml)
                    {
                        i.UpdateXml();
                    }
                }
                catch (Exception e)
                {
                    string msg = String.Format(Resource.ProcessError, i.Name, e.Message);
                    throw new Exception(msg);

                }
            }
            return ret;
        }
        public List<ConfigInfo> Info { get; private set; }

        public ConfigSettings()
        {
            Info = new List<ConfigInfo>();
        }

        public bool LoadFromFile(string fileName, string destinationFolder, IEnumerable<string> overrideParms = null)
        {
            XDocument doc = XDocument.Load(new System.IO.FileStream(fileName, System.IO.FileMode.Open));

            return LoadFromXElement(doc.Root, destinationFolder, overrideParms);
        }

        public bool LoadFromXElement(XElement root, string destinationFolder, IEnumerable<string> overrideParms = null)
        {
            _destinationFolder = destinationFolder;

            var overrides = parseOverrides(overrideParms);


            foreach (var n in root.Nodes().Where(n => { if (n is XElement) return (n as XElement).Name == "enum"; else return false; }))
            {
                var x = n as XElement;
                var name = x.Attribute("name");
                if (name == null)
                    throw new ArgumentNullException("name on enum");
                var dict = new Dictionary<string,string>();
                dict.LoadFromXml(x);
                _enums.Add( name.Value, dict );
            }
            
            foreach (var x in root.Nodes().Where(n => { if (n is XElement) return (n as XElement).Name == "item"; else return false; }))
            {
                var i = new ConfigInfo(x as XElement, destinationFolder);
                if (overrides.ContainsKey(i.PropName))
                    i.Value = overrides[i.PropName];

                Info.Add(i);
            }
            return true;
        }

        private IDictionary<string,string> parseOverrides(IEnumerable<string> overrideParms)
        {
            var ret = new Dictionary<string, string>();

            if (overrideParms != null && overrideParms.Count() > 0)
            {
                foreach (var o in overrideParms )
                {
                    var i = o.IndexOf("="); // use this instead of split in case have = in value
                    if ( i > 0 )
                    {
                        var s = o.Substring(0,i).Trim();
                        var v = o.Substring(i+1).Trim();
                        if (s.Length > 0 && v.Length > 0)
                        {
                            ret.Add(s, v);
                        }
                    }
                }
            }
            return ret;
        }
    }
}
