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

        #region Events
        public event EventHandler<ItemChangedArgs> ItemChanged;
        public event EventHandler<ItemChangedArgs> ItemAdded;
        public event EventHandler<ItemChangedArgs> ItemDeleted;
        public event EventHandler<ModelChangedArgs> ModelLoaded;
        public event EventHandler<ModelChangedArgs> ModelValidate;
        public event EventHandler<ModelChangedArgs> ModelSaved;

        /// <summary>
        /// Fired when an item is added to an array
        /// </summary>
        /// <param name="args"></param>
        public virtual void OnItemAdded(ItemChangedArgs args)
        {
            var temp = ItemAdded;
            if (temp != null) temp(this, args);
        }

        /// <summary>
        /// fired when an item is changed in an array
        /// </summary>
        /// <param name="args"></param>
        public virtual void OnItemChanged(ItemChangedArgs args)
        {
            var temp = ItemChanged;
            if (temp != null) temp(this, args);
        }

        /// <summary>
        /// fired when an item is deleted from an array
        /// </summary>
        /// <param name="args"></param>
        public virtual void OnItemDeleted(ItemChangedArgs args)
        {
            var temp = ItemDeleted;
            if (temp != null) temp(this, args);
        }

        /// <summary>
        /// fired after all XML is parsed and the model arrays have been loaded
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnModelLoaded()
        {
            var temp = ModelLoaded;
            if (temp != null) temp(this, new ModelChangedArgs());
        }

        /// <summary>
        /// called before saving the model
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnModelValidate()
        {
            var temp = ModelValidate;
            if (temp != null) temp(this, new ModelChangedArgs());
        }

        /// <summary>
        /// called after the model has been saved
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnModelSaved()
        {
            var temp = ModelSaved;
            if (temp != null) temp(this, new ModelChangedArgs());
        }

        #endregion

        // ugly for backward compat
        static IList<Type> _wellKnownCustoms = new List<Type>() { typeof(RazorTransform.Custom.ServerPort), typeof(RazorTransform.Custom.WebPort) };
        public static IList<Type> WellKnownCustoms { get { return _wellKnownCustoms; } }

        /// <summary>
        /// all the enums loaded from the Xml
        /// </summary>
        private IDictionary<string, Dictionary<string, string>> _enums = new Dictionary<string, Dictionary<string, string>>();

        /// <summary>
        /// all the custom items loaded from the Xml
        /// </summary>
        private IDictionary<string, Custom.ICustomRazorTransformType> _customs = new Dictionary<string, Custom.ICustomRazorTransformType>();

        /// <summary>
        ///  list of all arrays created so far for recursive use
        /// </summary>
        private IList<TransformModelArray> _arrays = new List<TransformModelArray>(); 

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
        public IDictionary<string, Dictionary<string, string>> Enums { get { return _enums; } }

        /// <summary>
        /// all the custom items loaded from the Xml
        /// </summary>
        public IDictionary<string, Custom.ICustomRazorTransformType> Customs { get { return _customs; } }

        /// <summary>
        /// list of all arrays created so far for recursive use
        /// </summary>
        public IList<TransformModelArray> Arrays { get { return _arrays; } }
 
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
        internal ExpandoObject BuildObject(IEnumerable<TransformModelGroup> groups, bool updateXml, bool htmlEncode, string destinationFolder = null)
        {
            return buildObject(groups, updateXml, htmlEncode, destinationFolder, null, null);
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
        private ExpandoObject buildObject(IEnumerable<TransformModelGroup> groups, bool updateXml, bool htmlEncode, string destinationFolder, ExpandoObject parent, ExpandoObject root)
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

            if (root == null)
                root = ret;

            dict.Add("Root", root);

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
                            children.Add(buildObject((c as TransformModelArrayItem).Groups, false, true, destinationFolder, ret, root));
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
                        if (v < i.MinInt)
                        {
                            throw new Exception(String.Format(Resource.MinInt, i.DisplayName, i.MinInt));
                        }
                        if (i.MaxInt != Int32.MaxValue && i.MaxInt != 0 && v > i.MaxInt)
                        {
                            throw new Exception(String.Format(Resource.MaxInt, i.DisplayName, i.MaxInt));
                        }
                    }
                    else
                    {
                        throw new Exception(String.Format(Resource.BadInteger, i.DisplayName, i.Value));
                    }
                    break;
            }
        }

        static public TransformModel Instance { get; set; }

        public TransformModel(bool setInstance = false)
        {
            Groups = new List<TransformModelGroup>();
            if ( setInstance )
                Instance = this;
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
            _customs.Clear();
            Arrays.Clear();
            Groups.Clear();

            foreach (var x in objectRoot.Elements().Where(n => n.Name == Constants.Enum))
            {
                var name = x.Attribute(Constants.Name);
                if (name == null)
                    throw new ArgumentNullException(Resource.ErrorNullEnumName);
                var dict = new Dictionary<string, string>();
                dict.LoadFromXml(x);
                _enums.Add(name.Value, dict);
            }

            foreach (var x in objectRoot.Elements().Where(n => n.Name == Constants.Custom))
            {
                var name = (String)x.Attribute(Constants.Name);
                if (name == null)
                    throw new ArgumentNullException(Resource.ErrorNullCustomName);
                var className = (String)x.Attribute(Constants.Class);
                if (className == null)
                    throw new ArgumentNullException(Resource.ErrorNullCustomClassName + name);
                var constructorParam = (String)x.Attribute(Constants.Parameter);

                Custom.ICustomRazorTransformType custom = null;
                Type t = Type.GetType(className);
                if (t == null)
                    throw new ArgumentException(Resource.ErrorCustomType + className);

                var constructors = t.GetConstructors();
                foreach (var c in constructors)
                {
                    var ps = c.GetParameters();
                    switch (ps.Count())
                    {
                        case 0:
                            custom = Activator.CreateInstance(t) as Custom.ICustomRazorTransformType;
                            break;
                        case 1:
                            if (ps[0].ParameterType == GetType())
                                custom = Activator.CreateInstance(t, new object[1] { this }) as Custom.ICustomRazorTransformType;
                            else if (ps[0].ParameterType == typeof(String))
                                custom = Activator.CreateInstance(t, new object[1] { constructorParam ?? String.Empty }) as Custom.ICustomRazorTransformType;
                            break;
                        case 2:
                            if (ps[0].ParameterType == GetType() && ps[1].ParameterType == typeof(String))
                                custom = Activator.CreateInstance(t, new object[2] { this, constructorParam ?? String.Empty }) as Custom.ICustomRazorTransformType;
                            break;
                        default:
                            break;
                    }
                }

                if (custom == null)
                    throw new ArgumentException(Resource.ErrorCreatingCustom  + className);

                _customs.Add(name, custom);
            }

            // get any ones known to us already
            foreach (var t in _wellKnownCustoms)
            {
                if (t.GetInterface(typeof(RazorTransform.Custom.ICustomRazorTransformType).FullName) != null)
                {
                    if (!_customs.Any(o => o.Key == t.Name))
                    {
                        var custom = Activator.CreateInstance(t) as Custom.ICustomRazorTransformType;
                        if (custom != null)
                            _customs.Add(t.Name, custom);
                    }
                }
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
            try
            {
                OnModelValidate();
            }
            catch (Exception)
            {
                return String.Empty;
            }

            var doc = XDocument.Parse("<RtValues/>");
            var root = doc.Root;

            var groups = new List<TransformModelGroup>(Groups);
            groups.Insert(0, _settings.Group);
            saveGroups(root, groups);

            doc.Save(_valueFileName);

            OnModelSaved();

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
            // load the main file
            path = Path.Combine(directoryName, _settings.ObjectFile);
            if (!File.Exists(path))
                throw new FileNotFoundException(String.Format(Resource.FileNotFound, _settings.ObjectFile));

            var definitionDoc = XDocument.Load(path);
            LoadFromXElement(definitionDoc.Root, objectValues, _settings.Overrides);

            OnModelLoaded();

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
                if (_settings != null)
                {
                    if (!String.IsNullOrWhiteSpace(_settings.Title))
                        TitleSuffix = " " + _settings.Title;

                    TitleSuffix += " -> " + _settings.OutputFolder;
                }

                return TitleSuffix;
            }
        }


    }
}
