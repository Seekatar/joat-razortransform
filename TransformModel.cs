using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Xml.Linq;

namespace RazorTransform
{

    /// <summary>
    /// class to read and contain the settings to be merged with all the other files
    /// </summary>
    class TransformModel
    {
        /// <summary>
        /// all the enums loaded from the Xml
        /// </summary>
        static IDictionary<string, Dictionary<string, string>> _enums = new Dictionary<string, Dictionary<string, string>>();

        /// <summary>
        /// settings object of the current run to allow Razor views to have access to it
        /// </summary>
        protected Settings _settings { get; set; }

        /// <summary>
        /// all the groups that make up this object
        /// </summary>
        public List<TransformModelGroup> Groups { get; protected set; }

        /// <summary>
        /// all the enums loaded from the Xml
        /// </summary>
        public static IDictionary<string, Dictionary<string, string>> Enums { get { return _enums; } }

        /// <summary>
        /// get the object with all the properties set on it
        /// </summary>
        /// <param name="updateXml"></param>
        /// <param name="htmlEncode"></param>
        /// <param name="destinationFolder"></param>
        /// <returns></returns>
        public ExpandoObject GetProperties(bool updateXml, bool htmlEncode, string destinationFolder = null)
        {
            return BuildObject(Groups, updateXml, htmlEncode, destinationFolder);
        }

        /// <summary>
        /// get the object with all the properties set on it
        /// </summary>
        /// <param name="items"></param>
        /// <param name="updateXml"></param>
        /// <param name="htmlEncode"></param>
        /// <param name="destinationFolder"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        internal ExpandoObject BuildObject(IEnumerable<TransformModelGroup> groups, bool updateXml, bool htmlEncode, string destinationFolder = null, ExpandoObject parent = null)
        {
            var ret = new ExpandoObject();
            var dict = ret as IDictionary<string, object>;

            // add some of the global values to the object to be available in the transform
            if (destinationFolder != null)
                dict.Add("DestinationFolder", destinationFolder);

            if (parent != null)
                dict.Add("Parent", parent);
            else
                dict.Add("CurrentSettings", _settings); // only add to root object

            String currentName = "<unknown>";

            try
            {
                foreach (var g in groups)
                {
                    currentName = g.DisplayName;

                    if (g is TransformModelArray)
                    {
                        var i = g as TransformModelArray;
                        var children = new List<ExpandoObject>();
                        if (i.Items.Count() < i.Min)
                            throw new Exception(String.Format(Resource.MinCount, i.DisplayName, i.Min));
                        foreach (var c in i.Items)
                        {
                            children.Add(BuildObject((c as TransformModelArrayItem).Groups, false, true, destinationFolder, ret));
                        }
                        dict.Add(i.ArrayValueName, children);
                    }
                    else
                    {
                        foreach (var i in g.Children)
                        {
                            currentName = i.DisplayName;

                            if (i.Type == RtType.Bool)
                            {
                                bool value = false;
                                if (i.Value != null)
                                {
                                    bool result;
                                    if (Boolean.TryParse(i.Value, out result))
                                        value = result;
                                }
                                dict.Add(i.PropertyName, value);
                            }
                            else
                            {
                                checkLimits(i);
                                string value = (i.Value ?? String.Empty).Trim();
                                if (htmlEncode)
                                    dict.Add(i.PropertyName, HttpUtility.HtmlEncode(value));
                                else
                                    dict.Add(i.PropertyName, value);
                            }

                            if (updateXml)
                            {
                                i.UpdateXml();
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                string msg = String.Format(Resource.ProcessError, currentName, e.BuildMessage());
                throw new Exception(msg);

            }
            return ret;
        }

        protected void checkLimits(TransformModelItem i)
        {
            string value = (i.Value ?? String.Empty).Trim();
            switch (i.Type)
            {
                case RtType.String:
                    if (i.MinInt != Int32.MinValue && value.Length < i.MinInt)
                    {
                        throw new Exception(String.Format(Resource.MinStrLen, i.DisplayName, i.MinInt));
                    }
                    if (i.MaxInt != Int32.MaxValue && value.Length > i.MaxInt)
                    {
                        throw new Exception(String.Format(Resource.MaxStrLen, i.DisplayName, i.MaxInt));
                    }
                    break;

                case RtType.Int32:
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
                        throw new Exception(String.Format(Resource.BadInteger, i.DisplayName, v));
                    }
                    break;
            }
        }

        public TransformModel()
        {
            Groups = new List<TransformModelGroup>();
        }

        // load both object def and values from files
        public bool LoadFromFile(Settings settings)
        {
            XDocument doc = null;
            using (var fs = new System.IO.FileStream(settings.ObjectFile, System.IO.FileMode.Open))
            {
                doc = XDocument.Load(fs);
            }
            return LoadValuesFromXml(doc.Root, settings);
        }

        // load object from xml and values from file (if exists)
        public bool LoadValuesFromXml(XElement objectRoot, Settings settings)
        {
            XDocument valueDoc = null;
            if (!String.IsNullOrEmpty(settings.ValuesContent))
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
                TransformModelGroup g = null;
                if (x.Attribute(Constants.ArrayValueName) != null)
                {
                    g = new TransformModelArray();
                }
                else
                {
                    g = new TransformModelGroup();
                }
                Groups.Add(g);

                g.LoadFromXml(x, values, overrides);
            }
            return true;
        }

        internal string Save(string _valueFileName)
        {
            var doc = XDocument.Parse("<RtValues/>");
            var root = doc.Root;

            saveGroups(root, Groups);

            doc.Save(_valueFileName);
            return File.ReadAllText(_valueFileName);
        }

        private void saveGroups(XElement root, List<TransformModelGroup> groups)
        {
            foreach (var g in groups)
            {
                foreach (var item in g.Items)
                {
                    if (!(item is PasswordTransformModelItem))
                        saveItem(root, item);
                }
            }
        }

        protected void saveItem(XElement root, TransformModelItem item)
        {
            if (item is TransformModelArrayItem)
            {
                var arrayItem = item as TransformModelArrayItem;
                var x = new XElement(Constants.Value, new XAttribute(Constants.Name, arrayItem.ArrayParent.ArrayValueName));
                root.Add(x);
                saveGroups(x, arrayItem.Groups);
            }
            else
            {
                var x = new XElement(Constants.Value, new XAttribute(Constants.Name, item.PropertyName), new XAttribute(Constants.Value, item.Value ?? ""));
                root.Add(x);
            }
        }

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
            Groups.Add(_settings.ArrayConfigInfo); // add so we can save it when we save

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
