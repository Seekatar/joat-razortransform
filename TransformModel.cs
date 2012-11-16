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

    /// <summary>
    /// class to read and contain the settings to be merged with all the other files
    /// </summary>
    class TransformModel
    {
        static IDictionary<string, Dictionary<string, string>> _enums = new Dictionary<string, Dictionary<string, string>>();

        public static IDictionary<string, Dictionary<string, string>> Enums { get { return _enums; } }

        public ExpandoObject GetProperties(bool updateXml, bool htmlEncode, string destinationFolder = null)
        {
            return BuildObject(_info, updateXml, htmlEncode, destinationFolder);
        }

        internal ExpandoObject BuildObject(IEnumerable<TransformModelItem> info, bool updateXml, bool htmlEncode, string destinationFolder = null, ExpandoObject parent = null )
        {
            var ret = new ExpandoObject();
            if ( destinationFolder != null )
                (ret as IDictionary<string, object>).Add("DestinationFolder", destinationFolder);

            if ( parent != null )
                (ret as IDictionary<string, object>).Add("Parent", parent);
            else
                (ret as IDictionary<string, object>).Add("CurrentSettings", _settings); // only add to root object

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
                            children.Add(BuildObject(c.Children, false, true, destinationFolder, ret));
                        }
                        (ret as IDictionary<string, object>).Add(i.PropertyName, children);
                    }
                    else if (String.Equals(Constants.Bool, i.Type, StringComparison.CurrentCultureIgnoreCase))
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
                        checkLimits(i);
                        string value = (i.Value ?? String.Empty).Trim();
                        if (htmlEncode)
                            (ret as IDictionary<string, object>).Add(i.PropertyName, HttpUtility.HtmlEncode(value));
                        else
                            (ret as IDictionary<string, object>).Add(i.PropertyName, value);
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

        protected void checkLimits(TransformModelItem i)
        {
            string value = (i.Value ?? String.Empty).Trim();
            switch ( i.Type.ToLower() )
            {
                case Constants.String:
                    if (i.MinInt != Int32.MinValue && value.Length < i.MinInt)
                    {
                        throw new Exception(String.Format(Resource.MinStrLen, i.DisplayName, i.MinInt));
                    }
                    if (i.MaxInt != Int32.MaxValue && value.Length > i.MaxInt)
                    {
                        throw new Exception(String.Format(Resource.MaxStrLen, i.DisplayName, i.MaxInt));
                    }
                    break;

                case Constants.Int32:
                    Int32 v;
                    if (Int32.TryParse(i.Value, out v))
                    {
                        if (i.MinInt != Int32.MinValue && v < i.MinInt)
                        {
                            throw new Exception(String.Format(Resource.MinInt, i.DisplayName, i.MinInt));
                        }
                        if (i.MaxInt != Int32.MaxValue && v > i.MaxInt)
                        {
                            throw new Exception(String.Format(Resource.MaxInt, i.DisplayName, i.MaxInt));
                        }
                    }
                    else
                    {
                        throw new Exception(String.Format(Resource.BadInteger,i.DisplayName,v));
                    }
                    break;
            }
        }

        public List<TransformModelItem> Groups { get; protected set; }
        protected List<TransformModelItem> _info { get; set; }

        public TransformModel()
        {
            _info = new List<TransformModelItem>();
            Groups = new List<TransformModelItem>();
        }

        // load both object def and values from files
        public bool LoadFromFile(Settings settings)
        {
            XDocument doc = null;
            using (var fs = new System.IO.FileStream(settings.ObjectFile, System.IO.FileMode.Open))
            {
                doc = XDocument.Load(fs);
            }
            return LoadValuesFromXml(doc.Root,settings);
        }

        // load object from xml and values from file (if exists)
        public bool LoadValuesFromXml(XElement objectRoot, Settings settings)
        {
            XDocument valueDoc = null;
            if (!String.IsNullOrEmpty( settings.ValuesContent))
            {
                valueDoc = XDocument.Parse(settings.ValuesContent);
            }
            else
            {
                if (File.Exists(settings.ValuesFile))
                {
                    using (var fs = new System.IO.FileStream(settings.ValuesFile, System.IO.FileMode.Open))
                    {
                        valueDoc = XDocument.Load(fs);
                    }
                }

            }
            return LoadFromXElement(objectRoot, valueDoc == null ? null : valueDoc.Root, settings.Overrides);
        }

        // load both object and values from XML
        public bool LoadFromXElement(XElement objectRoot, XElement values, IDictionary<string, string> overrides = null)
        {
            _enums.Clear();
            _info.Clear();
            Groups.Clear();

            foreach (var x in objectRoot.Elements().Where(n => n.Name == Constants.Enum))
            {
                var name = x.Attribute(Constants.Name);
                if (name == null)
                    throw new ArgumentNullException("name on enum");
                var dict = new Dictionary<string, string>();
                dict.LoadFromXml(x);
                _enums.Add(name.Value, dict);
            }

            foreach (var x in objectRoot.Elements().Where(n => n.Name == Constants.Group))
            {
                TransformModelItem g = null;
                if (x.Attribute(Constants.ArrayValueName) != null)
                {
                    g = new TransformModelArray(x);
                }
                else
                {
                    g = new TransformModelItem(x);
                }
                Groups.Add(g);

                g.Load(values, overrides, x);
                _info.AddRange(g.Children);
            }
            return true;
        }

        internal string Save(string _valueFileName)
        {
            var doc = XDocument.Parse("<RtValues/>");
            var root = doc.Root;

            foreach (var item in _info)
            {
                if ( !(item is PasswordTransformModelItem ))
                    saveItem(root, item);
            }

            doc.Save(_valueFileName);
            return File.ReadAllText(_valueFileName);
        }

        protected static void saveItem(XElement root, TransformModelItem item)
        {
            if (item.IsArray)
            {
                foreach (var child in item.Children.Skip(1))
                {
                    var x = new XElement(Constants.Value, new XAttribute(Constants.Name, (item as TransformModelArray).ArrayValueName));

                    root.Add(x);
                    foreach (var arrayChild in child.Children)
                    {
                        saveItem(x, arrayChild);
                    }
                }
            }
            else
            {
                var x = new XElement(Constants.Value, new XAttribute(Constants.Name, item.PropertyName), new XAttribute(Constants.Value, item.Value ?? ""));
                root.Add(x);
            }
        }

        /// <summary>
        /// settings object of the current run to allow Razor views to have access to it
        /// </summary>
        protected Settings _settings { get; set; }

        internal bool Load(Settings settings)
        {
            _settings = settings;
            string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var path = Path.Combine(directoryName, _settings.ValuesFile);
            XElement objectValues = null;
            if (File.Exists(path))
            {
                objectValues = XDocument.Load(path).Root;
            }
            _info.Add(_settings.ArrayConfigInfo); // add so we can save it when we save

            // load the main file
            path = Path.Combine(directoryName, _settings.ObjectFile);
            if (!File.Exists(path))
                throw new FileNotFoundException(String.Format(Resource.FileNotFound, _settings.ObjectFile));

            var definitionDoc = XDocument.Load(path);
            LoadFromXElement(definitionDoc.Root, objectValues, _settings.Overrides);

            return true;
        }

        /// <summary>
        /// get the suffix for the title, usually configured Title and destination folder
        /// </summary>
        public string TitleSuffix
        {
            get
            {
                string TitleSuffix = String.Empty;
                if (_settings != null && !String.IsNullOrWhiteSpace(_settings.Title))
                    TitleSuffix = " " + _settings.Title;

                TitleSuffix += " -> " + _settings.OutputFolder;

                return TitleSuffix;
            }
        }


    }
}
