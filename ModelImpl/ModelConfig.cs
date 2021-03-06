﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public event EventHandler<ModelLoadedArgs> ModelLoaded;
        public event EventHandler<ModelValidateArgs> ModelValidate;
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
        public void OnModelLoaded(ModelLoadedArgs args)
        {
            var temp = ModelLoaded;
            if (temp != null) temp(this, args);
        }

        /// <summary>
        /// called before saving the model
        /// </summary>
        /// <param name="args"></param>
        public void OnModelValidate(ModelValidateArgs args)
        {
            var temp = ModelValidate;
            if (temp != null) temp(this, args);
        }

        /// <summary>
        /// called after the model has been saved
        /// </summary>
        /// <param name="args"></param>
        public void OnModelSaved(ModelChangedArgs args)
        {
            var temp = ModelSaved;
            if (temp != null) temp(this, args);
        }
        #endregion

        #region IModelConfig Methods
        /// <summary>
        /// load the config
        /// </summary>
        /// <param name="settings">Settings object with all the environment settings</param>
        /// <param name="loadValues">if true load values from the file in settings</param>
        /// <param name="objectRoot">override object model root node</param>
        /// <param name="warnings">The warnings.</param>
        /// <returns>
        /// true if loaded ok
        /// </returns>
        public bool Load(Settings settings, bool loadValues = true, XElement objectRoot = null, IList<Exception> warnings = null)
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

            var ret = loadFromXElement(Root, objectValues, warnings );
            return ret;
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
        private bool loadFromXElement(XElement objectRoot, XElement values, IList<Exception> exceptions )
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
                bool loaded = false;
                var dict = new Dictionary<string, string>();

                var name = x.Attribute(Constants.Name);
                if (name == null)
                    throw new ArgumentNullException(Resource.ErrorNullEnumName);
                var scriptAttr = x.Attribute(Constants.Script);
                if (scriptAttr != null && !String.IsNullOrWhiteSpace(scriptAttr.Value))
                {
                    var timeout = 1000;
                    int timeoutAttr = (Int32?)x.Attribute(Constants.Timeout) ?? Int32.MinValue;
                    if (timeoutAttr > 0)
                    {
                        timeout = 1000 * timeoutAttr;
                    }
                    var path = scriptAttr.Value;
                    if (!File.Exists(path))
                    {
                        // look above output
                        path = Path.Combine(Settings.Instance.OutputFolder, scriptAttr.Value);
                        if (!File.Exists(path))
                        {
                            // not fully qualified or in current dir, is it with the object file?
                            path = Path.Combine(Path.GetDirectoryName(Settings.Instance.ObjectFile), path);
                        }
                    }
                    if (!File.Exists(path))
                    {
                        exceptions.Add(new ArgumentException(String.Format(Resource.InvalidEnumScript, name.Value, scriptAttr.Value)));
                    }
                    else
                    {
                        path = System.IO.Path.GetFullPath(path);

                        Dictionary<string, object> parms = parseParms((String)x.Attribute(Constants.Parameters) ?? null);

                        // run the script to generate the enums
                        var ps = RtPsHost.PsHostFactory.CreateHost();
                        var task = ps.InvokeScriptAsync(path, (System.Collections.IDictionary o) =>
                        {
                            foreach (var i in o.Keys)
                            {
                                dict[o[i].ToString()] = i.ToString();
                            }
                        }, parms);
                        try
                        {
                            task.Wait(timeout);
                        }
                        catch (Exception e )
                        {
                            exceptions.Add(e);
                        }
                        if (task.Status != TaskStatus.RanToCompletion)
                        {
                            exceptions.Add(new ArgumentException(String.Format(Resource.EnumScriptTimeout, name.Value, path)));
                        }
                        if (dict.Count == 0)
                        {
                            exceptions.Add(new ArgumentException(String.Format(Resource.NoEnumContent, name.Value, path)));
                        }
                    }
                    loaded = true;
                }
                if (!loaded)
                {
                    dict.LoadFromXml(x);
                }
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
                {
                    custom.Name = name;
                    custom.Initialize(this, parms);
                }

                CustomTypes[name] = custom;
            }

            var noparms = new Dictionary<string, string>();

            Root = objectRoot;

            return true;
        }

        private Dictionary<string, object> parseParms(string p)
        {
            Dictionary<string, object> ret = new Dictionary<string, object>();
            if (p != null)
            {
                // format is name=value;name2=value
                // allow escaping with \  name\=1=valu\;;
                p = p.Replace("\\;", "\t").Replace("\\=", "\b");

                foreach (var parm in p.Split(";".ToCharArray()))
                {
                    var nameValue = parm.Split("=".ToCharArray());
                    if (nameValue.Length == 2)
                    {
                        ret[nameValue[0].Replace('\t', ';').Replace('\b', '=')] = nameValue[1].Replace('\t', ';').Replace('\b', '=');
                    }
                }
            }

            return ret;
        }
    }
}
