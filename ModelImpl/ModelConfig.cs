using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RazorTransform.Model
{
    class ModelConfig : IModelConfig
    {
        public ModelConfig()
        {
            Enums = new Dictionary<string, IDictionary<string, string>>();
            Regexes = new Dictionary<string, string>();
            CustomTypes = new Dictionary<string, Custom.ICustomRazorTransformType>();
            Instance = this;
        }

        #region Properties
        static public IModelConfig Instance
        {
            get;
            private set;
        }

        public IDictionary<string, IDictionary<string, string>> Enums
        {
            get;
            private set;
        }

        public IDictionary<string, string> Regexes
        {
            get;
            private set;
        }

        public IDictionary<string, Custom.ICustomRazorTransformType> CustomTypes
        {
            get;
            private set;
        }

        /// <summary>
        /// root XML element in model
        /// </summary>
        public XElement Root
        {
            get;
            private set;
        }

        /// <summary>
        /// root values XML element 
        /// </summary>
        public XElement ValuesRoot
        {
            get;
            private set;
        }

        /// <summary>
        /// the version of the RtValues.xml file
        /// </summary>
        public int RtValuesVersion
        {
            get;
            private set;
        }
        #endregion

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
        public void OnModelLoaded()
        {
            var temp = ModelLoaded;
            if (temp != null) temp(this, new ModelChangedArgs());
        }

        /// <summary>
        /// called before saving the model
        /// </summary>
        /// <param name="args"></param>
        public void OnModelValidate()
        {
            var temp = ModelValidate;
            if (temp != null) temp(this, new ModelChangedArgs());
        }

        /// <summary>
        /// called after the model has been saved
        /// </summary>
        /// <param name="args"></param>
        public void OnModelSaved()
        {
            var temp = ModelSaved;
            if (temp != null) temp(this, new ModelChangedArgs());
        }
        #endregion

        #region IModelConfig Methods
        /// <summary>
        /// load the config
        /// </summary>
        /// <param name="settings">Settings object with all the environment settings</param>
        /// <param name="loadValues">if true load values from the file in settings</param>
        /// <param name="objectRoot">override object model root node</param>
        /// <returns>true if loaded ok</returns>
        public bool Load(Settings settings, bool loadValues = true, XElement objectRoot = null)
        {
            var _settings = settings;

            XElement objectValues = null;
            if (loadValues && File.Exists(_settings.ValuesFile))
            {
                objectValues = XDocument.Load(_settings.ValuesFile).Root;
            }

            if (objectRoot != null)
            {
                Root = objectRoot;
            }
            else
            {
                Root = XDocument.Load(_settings.ObjectFile).Root;
                checkDestinationFolder(Root, _settings.OutputFolder);
            }

            ValuesRoot = objectValues;

            return loadFromXElement(Root, objectValues);
        }
        #endregion

        /// <summary>
        /// validate the destination folder in the XML so can be saved
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="outputFolder"></param>
        private void checkDestinationFolder(XElement root, string outputFolder)
        {
            // if don't have DestinationFolder, add it
            if (root.Elements().Count() > 0 &&
                root.Elements().First().Elements("item").Where(o => o.Attribute("name").Value == "DestinationFolder").SingleOrDefault() == null)
            {
                root.Elements().First().Add(new XElement("item", new XAttribute("name", Constants.DestinationFolder),
                                                   new XAttribute(Constants.Hidden, "true"),
                                                   new XAttribute(Constants.DisplayName, "Do not use"),
                                                   new XAttribute(Constants.Description, "do not use"),
                                                   new XAttribute(Constants.Type, "String"),
                                                   new XAttribute(Constants.DefaultValue, outputFolder)));
            }
        }

        /// <summary>
        /// load the enums, regexes, custom data from the object XML
        /// </summary>
        /// <param name="objectRoot">root node of RtObject.xml</param>
        /// <param name="values">root node of RtValues.xml, if it exists</param>
        /// <returns>true if loaded ok</returns>
        private bool loadFromXElement(XElement objectRoot, XElement values)
        {
            // check RtValues file version
            RtValuesVersion = Constants.CurrentRtValuesVersion; // if no values at all, default to current

            if (values != null)
            {
                RtValuesVersion = 1; // if not there, default to v1
                var verAttr = values.Attribute(Constants.Version);
                if (verAttr != null)
                {
                    int ver;
                    if (Int32.TryParse(verAttr.Value, out ver) && ver > Constants.CurrentRtValuesVersion)
                        throw new Exception(String.Format(Resource.BadRtValueVersion, verAttr.Value));
                    RtValuesVersion = ver;
                }
            }

            Enums.Clear();
            Regexes.Clear();

            foreach (var x in objectRoot.Elements().Where(n => n.Name.LocalName == Constants.Enum))
            {
                var name = x.Attribute(Constants.Name);
                if (name == null)
                    throw new ArgumentNullException(Resource.ErrorNullEnumName);
                var dict = new Dictionary<string, string>();
                dict.LoadFromXml(x);
                Enums[name.Value] = dict;
            }

            foreach (var x in objectRoot.Elements().Where(n => n.Name.LocalName == Constants.RegEx))
            {
                var name = x.Attribute(Constants.Name);
                var value = x.Attribute(Constants.Value);

                if (name == null || value == null)
                    throw new ArgumentNullException(Resource.ErrorNullRegEx);
                Regexes[name.Value] = value.Value;
            }

            foreach (var x in objectRoot.Elements().Where(n => n.Name.LocalName == Constants.Custom))
            {
                var parms = new Dictionary<string, string>();
                string name = null;

                string className = null;
                foreach (var a in x.Attributes())
                {
                    if (a.Name.LocalName == Constants.Name)
                        name = a.Value;
                    else if (a.Name.LocalName == Constants.Class)
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
                catch (Exception)
                { }

                if (t == null)
                    throw new ArgumentException(Resource.ErrorCustomType + className);

                custom = Activator.CreateInstance(t) as Custom.ICustomRazorTransformType;

                if (custom == null)
                    throw new ArgumentException(Resource.ErrorCreatingCustom + className);
                else
                    custom.Initialize(this, parms);

                CustomTypes[name] = custom;
            }

            var noparms = new Dictionary<string, string>();

            Root = objectRoot;

            return true;
        }
    }
}
