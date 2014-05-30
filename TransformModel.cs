using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using RazorTransform.Custom;

namespace RazorTransform
{
    /// <summary>
    /// class to read and contain the settings to be merged with all the other files
    /// </summary>
    class TransformModel : System.Dynamic.DynamicObject, RazorTransform.ITransformModel
    {

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            bool ret = false;
            result = null;

            if (binder.Name == "Root")
            {
                result = this; // needs to b
                return true;
            }
            if (binder.Name == "Parent")
            {
                result = this;
                return true;
            }
            if ( binder.Name == "CurrentSettings")
            {
                result = _settings;
                return true;
            }

            // find in all the groups, an item or array with this name
            // var list = Groups.Where( o => o.Children.Where(p => p.PropertyName == binder.Name));
            var list = Groups.SelectMany( o => o.Children.Where(p => p.PropertyName == binder.Name));
            var arrays = Groups.Where(o => o is TransformModelArray && (o as TransformModelArray).ArrayValueName == binder.Name).SingleOrDefault();
            if ( arrays != null )
            {
                ret = true;
                result = arrays;
            }
            else if ( list != null )
            {
                result = list.FirstOrDefault();
                    ret = true;
                    if ( result is TransformModelItem )
                    {
                        var item = (result as TransformModelItem);
                        if (item.ExpandedValue != null)
                            result = item.ExpandedValue;
                        else
                            result = item.Value;

                        if (item.Type == RtType.Bool)
                            result = item.GetValue<bool>();
                        else if (item.Type == RtType.Int32)
                            result = item.GetValue<Int32>();
                    }
                    else if ( !(result is TransformModelArray))
                    {
                        if (result != null)
                            throw new Exception("Bad type found for item " + binder.Name + " " + result.GetType().Name);
                        else
                            System.Diagnostics.Debug.WriteLine("Null result for " + binder.Name);
                    }
            }
            return ret;
        }

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

        /// <summary>
        /// all the enums loaded from the Xml
        /// </summary>
        private IDictionary<string, Dictionary<string, string>> _enums = new Dictionary<string, Dictionary<string, string>>();

        /// <summary>
        /// all the enums loaded from the Xml
        /// </summary>
        private IDictionary<string, string> _regexes = new Dictionary<string,string>();

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
        public Settings Settings { get { return _settings;  } }
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
        /// all the regular expressions load from the Xml
        /// </summary>
        public IDictionary<string, string> RegExes { get { return _regexes; } }

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
        public object GetProperties(bool updateXml, bool htmlEncode, string destinationFolder = null, bool validateModel = true)
        {
            return this;
            //return BuildObject(Groups, updateXml, htmlEncode, destinationFolder, validateModel);
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
        internal ExpandoObject BuildObject(IEnumerable<TransformModelGroup> groups, bool updateXml, bool htmlEncode, string destinationFolder = null, bool validateModel = true )
        {
            return buildObject(groups, updateXml, htmlEncode, destinationFolder, null, null, validateModel);
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
        private ExpandoObject buildObject(IEnumerable<TransformModelGroup> groups, bool updateXml, bool htmlEncode, string destinationFolder, ExpandoObject parent, ExpandoObject root, bool validateModel )
        {
            var ret = new ExpandoObject();
            var dict = ret as IDictionary<string, object>;

            if (parent != null)
                dict.Add("Parent", parent);
            else
                dict.Add("CurrentSettings", _settings); // only add to root object

            if (root == null)
                root = ret;

            dict.Add("Root", root);

            String currentName = "<unknown>";
            ITransformModelGroup currentGroup = null;

            try
            {
                foreach (var g in groups)
                {
                    currentGroup = g;
                    currentName = g.DisplayName;

                    if (g is TransformModelArray)
                    {
                        var i = g as TransformModelArray;
                        var children = new List<ExpandoObject>();
                        if (validateModel)
                        {
                            if (i.Items.Count() < i.Min)
                                throw new ValidationException(String.Format(Resource.MinCount, i.DisplayName, i.Min), g);

                            if (i.Unique)
                            {
                                for (int j = 0; j < (i.Items.Count() - 1); j++)
                                {
                                    if (i.Items.Skip(j + 1).Any(o => o.DisplayName.Equals(i.Items.ElementAt(j).DisplayName)))
                                    {
                                        throw new ValidationException(String.Format(Resource.UniqueViolation, i.Items.ElementAt(j).DisplayName), g);
                                    }
                                }
                            }
                        }
                        foreach (var c in i.Items)
                        {
                            currentName = (c as TransformModelArrayItem).DisplayName;
                            children.Add(buildObject((c as TransformModelArrayItem).Groups, false, htmlEncode, destinationFolder, ret, root,validateModel));
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
                                if ( validateModel )
                                    checkLimits(i);
                                string value = (i.Value ?? String.Empty).Trim();
                                if (htmlEncode)
                                    dict.Add(i.PropertyName, HttpUtility.HtmlEncode(value));
                                else
                                    dict.Add(i.PropertyName, value);
                            }

                            if (updateXml)
                            {
                                (i as TransformModelItem).UpdateXml();
                            }
                        }
                    }
                }
            }
            catch (ValidationException ve)
            {
                if (ve.Group.Count() == 0 || ve.Group.Last().Item1 != currentGroup)
                    ve.AddGroup(currentGroup,currentName);
                throw ve;
            }
            catch (Exception e)
            {
                string msg = String.Format(Resource.ProcessError, currentName, e.BuildMessage());
                throw new Exception(msg);

            }
            return ret;
        }

        protected void checkLimits(ITransformModelItem i)
        {
            string value = (i.Value ?? String.Empty).Trim();
            switch (i.Type)
            {
                case RtType.String:
                    if (!String.IsNullOrWhiteSpace(i.RegEx) && !Regex.IsMatch( value, TransformModel.Instance.RegExes[i.RegEx] ))
                    {
                        throw new ValidationException(String.Format(Resource.RegExViolation, i.DisplayName, i.RegEx), i);
                    }
                    if (i.MinInt != Int32.MinValue && value.Length < i.MinInt)
                    {
                        throw new ValidationException(String.Format(Resource.MinStrLen, i.DisplayName, i.MinInt),i);
                    }
                    if (i.MaxInt != Int32.MaxValue && value.Length > i.MaxInt)
                    {
                        throw new ValidationException(String.Format(Resource.MaxStrLen, i.DisplayName, i.MaxInt),i);
                    }
                    break;

                case RtType.Int32:
                    Int32 v;
                    if (Int32.TryParse(i.Value, out v))
                    {
                        if (v < i.MinInt)
                        {
                            throw new ValidationException(String.Format(Resource.MinInt, i.DisplayName, i.MinInt),i);
                        }
                        if (i.MaxInt != Int32.MaxValue && i.MaxInt != 0 && v > i.MaxInt)
                        {
                            throw new ValidationException(String.Format(Resource.MaxInt, i.DisplayName, i.MaxInt),i);
                        }
                    }
                    else
                    {
                        throw new ValidationException(String.Format(Resource.BadInteger, i.DisplayName, i.Value),i);
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
            checkDestinationFolder(doc, settings.OutputFolder);
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
            // check RtValues file version
            int rtValuesVersion = 1;
            if (values != null)
            {
                var verAttr = values.Attribute(Constants.Version);
                if (verAttr != null)
                {
                    if (Int32.TryParse(verAttr.Value, out rtValuesVersion) && rtValuesVersion > Constants.CurrentRtValuesVersion )
                        throw new Exception(String.Format(Resource.BadRtValueVersion, verAttr.Value));
                }
            }

            _enums.Clear();
            _regexes.Clear();
            //TransformModel.Instance.Customs.Clear();
            Arrays.Clear();
            Groups.Clear();

            foreach (var x in objectRoot.Elements().Where(n => n.Name == Constants.Enum))
            {
                var name = x.Attribute(Constants.Name);
                if (name == null)
                    throw new ArgumentNullException(Resource.ErrorNullEnumName);
                var dict = new Dictionary<string, string>();
                dict.LoadFromXml(x);
                _enums[name.Value] = dict;
            }

            foreach (var x in objectRoot.Elements().Where(n => n.Name == Constants.RegEx))
            {
                var name = x.Attribute(Constants.Name);
                var value = x.Attribute(Constants.Value);

                if (name == null || value == null )
                    throw new ArgumentNullException(Resource.ErrorNullRegEx);
                _regexes[name.Value] = value.Value;
            }

            foreach (var x in objectRoot.Elements().Where(n => n.Name == Constants.Custom))
            {
                var parms = new Dictionary<string, string>();
                string name = null;

                string className = null;
                foreach (var a in x.Attributes())
                {
                    if (a.Name == Constants.Name)
                        name = a.Value;
                    else if (a.Name == Constants.Class)
                        className = a.Value;
                    else
                        parms[a.Name.LocalName] = a.Value;
                }
                if (name == null)
                    throw new ArgumentNullException(Resource.ErrorNullCustomName);
                if (className == null)
                    throw new ArgumentNullException(Resource.ErrorNullCustomClassName + name);

                Custom.ICustomRazorTransformType custom = null;
                Type t = null;
                try
                {
                    t = Type.GetType(className);
                }
                catch( Exception )
                { }

                if (t == null)
                    throw new ArgumentException(Resource.ErrorCustomType + className);

                custom = Activator.CreateInstance(t) as Custom.ICustomRazorTransformType;

                if (custom == null)
                    throw new ArgumentException(Resource.ErrorCreatingCustom + className);
                else
                    custom.Initialize(this, parms);

                TransformModel.Instance.Customs[name] = custom;
            }

            var noparms = new Dictionary<string, string>();

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

                g.LoadFromXml(x, values, overrides, rtValuesVersion);
            }
            return true;
        }

        internal string GenerateXml()
        {
            try
            {
                OnModelValidate();
            }
            catch (Exception)
            {
                return String.Empty;
            }

            var doc = XDocument.Parse(String.Format("<RtValues {0}=\"{1}\"/>", Constants.Version, Constants.CurrentRtValuesVersion));
            var root = doc.Root;

            var groups = new List<TransformModelGroup>(Groups);
            groups.Insert(0, _settings.Group);
            saveGroups(root, groups);

            OnModelSaved();

            return doc.ToString();
        }

        private void saveGroups(XElement root, List<TransformModelGroup> groups)
        {
            foreach (var g in groups)
            {
                foreach (var item in g.Items)
                {
                    if (!(item is PasswordTransformModelItem))
                        saveItem(root, item as TransformModelItem);
                }
            }
        }

        protected void saveItem(XElement root, TransformModelItem item)
        {
            if (item is TransformModelArrayItem)
            {
                var arrayItem = item as TransformModelArrayItem;
                var x = new XElement(arrayItem.ArrayGroup.ArrayValueName);
                root.Add(x);
                saveGroups(x, arrayItem.Groups);
            }
            else
            {
                XElement x = null;
                if ( !String.IsNullOrEmpty(item.ExpandedValue))
                    x = new XElement(item.PropertyName, item.ExpandedValue, new XAttribute(Constants.Original,  item.Value ?? ""));
                else
                    x = new XElement(item.PropertyName, item.Value ?? "");

                root.Add(x);
            }
        }

        internal bool Load(Settings settings, XElement objectValues = null )
        {
            _settings = settings;
            string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var path = Path.Combine(directoryName, _settings.ValuesFile);
            if (objectValues == null && File.Exists(path))
            {
                objectValues = XDocument.Load(path).Root;
            }
            // load the main file
            path = Path.Combine(directoryName, _settings.ObjectFile);
            if (!File.Exists(path))
                throw new FileNotFoundException(String.Format(Resource.FileNotFound, _settings.ObjectFile));

            var definitionDoc = XDocument.Load(path);
            checkDestinationFolder(definitionDoc, _settings.OutputFolder);
            LoadFromXElement(definitionDoc.Root, objectValues, _settings.Overrides);

            OnModelLoaded();

            return true;
        }

        private void checkDestinationFolder(XDocument doc, string p)
        {
            if (doc.Root.Elements().Count() > 0 && 
                doc.Root.Elements().First().Elements("item").Where(o => o.Attribute("name").Value == "DestinationFolder").SingleOrDefault() == null)
            {
                doc.Root.Elements().First().Add(new XElement("item", new XAttribute("name", "DestinationFolder"),
                                                   new XAttribute("hidden", "true"),
                                                   new XAttribute("displayName", "Do not use"),
                                                   new XAttribute("description", "do not use"),
                                                   new XAttribute("type", "String"),
                                                   new XAttribute("defaultValue",p)));
            }
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
