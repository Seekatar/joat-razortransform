using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Dynamic;
using System.Runtime.Serialization;
using System.IO;

namespace RazorTransform
{
    /// <summary>
    /// encapsulation of settings.
    /// </summary>
    /// use RT for managing the settings -- wheee!
    /// could have used more static class, but eat our own dog food
    public class Settings
    {
        private ExpandoObject _settings = null;
        private TransformModelItem _configInfo;
        private TransformModelItem _arrayConfigInfo;
        private string _objectFile;
        private string _valuesFile;

        // object definition for settings is really static since code relies on it
        static string _settingsXml =
@"<RtObject>
  <group name=""Settings"" description=""Settings that control how Artie works."" arrayValueName=""_settings"" hidden=""True"">
    <item name=""Title"" displayName=""Title"" description=""Title to show in the titlebar"" type=""" + Constants.String + @""" defaultValue=""RazorTransform""/>
    <item name=""LastPath"" displayName=""Save Path"" description=""Location used when saving."" type=""" + Constants.Folder + @""" defaultValue=""..""/>
    <item name=""LastTemplatePath"" displayName=""Template Path"" description=""Location for retrieving templates."" type=""" + Constants.Folder + @""" defaultValue=""Templates""/>
  </group>
</RtObject>";

        public void Load(IDictionary<string, string> overrideParms)
        {
            setOverrides(overrideParms);

            // load the settings
            var settingsDefinition = XDocument.Parse(_settingsXml).Root;
            TransformModel settings = new TransformModel();
            settings.LoadValuesFromXml(settingsDefinition, this);

            // the config info it the [1] value or [0] if first time. (for array 0th is template)
            _configInfo = settings.Groups[0].Children[settings.Groups[0].Children.Count - 1];

            // get the settings objects which is an array of one item so not in root namespace of values
            dynamic settingsArray = settings.GetProperties(false, false);
            if (settingsArray._settings.Count == 0)
            {
                // create a default one as template
                _settings = settings.BuildObject(settings.Groups[0].Children[0].Children, false, false);

                var newOne = new TransformModelItem(_configInfo);
                _configInfo.Parent.Children.Add(newOne);
                _configInfo = newOne;
            }
            else
                _settings = settingsArray._settings[0];

            _arrayConfigInfo = settings.Groups[0];
        }

        public Settings()
        {
            Overrides = null;
            ObjectFile = "RtObject.xml";
            ValuesFile = "RtValues.xml";
            Test = false;
            OverrideOutputFolder = null;
            OverrideTemplateFolder = null;
            LogFile = "RazorTransform.log";
        }

        // for saving
        public TransformModelItem ArrayConfigInfo { get { return _arrayConfigInfo;  } }

        // for editing
        public TransformModelItem ConfigInfo { get { return _configInfo;  } }

        // values saved in values file
        public string Title { get { return this["Title"]; } set { this["Title"] = value; } }
        public string OutputFolder 
        { 
            get 
            { 
                var temp = OverrideOutputFolder ?? this["LastPath"]; 
                return Path.GetFullPath(temp);
            }
            set { this["LastPath"] = value; }
        }
        public string TemplateFolder { get { return Path.GetFullPath(OverrideTemplateFolder ?? this["LastTemplatePath"]); } set { this["LastTemplatePath"] = value; } }

        // temp values
        public bool Test { get; set; }
        public bool NoSave { get; set; }
        public bool Debug { get; set; }

        // set with GetFullPath since XDocument read does odd things if not a full path
        public string ObjectFile { get { return _objectFile; } set { _objectFile = Path.GetFullPath(value); } }
        public string ValuesFile { get { return _valuesFile; } set { _valuesFile = Path.GetFullPath(value); } }

        /// <summary>
        /// XML content of a RTValues.xml file passed into the app
        /// </summary>
        public string ValuesContent { get; set; }
        [IgnoreDataMemberAttribute]
        public string OverrideOutputFolder { get; set; }
        [IgnoreDataMemberAttribute]
        public string OverrideTemplateFolder { get; set; }
        public bool Run { get; set; }
        public string LogFile { get; set; }
        public IDictionary<string, string> Overrides { get; set; }

        private void setOverrides(IDictionary<string, string> overrideParms)
        {
            Overrides = overrideParms;
        }

        static public IDictionary<string, string> SplitCommandLineOverrides(IEnumerable<string> overrideParms)
        {
            var ret = new Dictionary<string, string>();

            if (overrideParms != null && overrideParms.Count() > 0)
            {
                foreach (var o in overrideParms)
                {
                    var i = o.IndexOf("="); // use this instead of split in case have = in value
                    if (i > 0)
                    {
                        var s = o.Substring(0, i).Trim();
                        var v = o.Substring(i + 1).Trim();
                        if (s.Length > 0 && v.Length > 0)
                        {
                            ret.Add(s, v);
                        }
                    }
                }
            }
            return ret;
        }


        [IgnoreDataMemberAttribute]
        public string this[string key]
        {
            get
            {
                var dict = _settings as IDictionary<string,object>;
                object value = null;
                dict.TryGetValue(key, out value );
                return value.ToString();
            }
            set
            {
                var dict = _settings as IDictionary<string, object>;
                dict[key] = value;
            }
        }

        internal string ToString(TransformModel model)
        {
            var sb = new StringBuilder();
            var t = GetType();
            sb.AppendLine(t.Name+":");
            foreach ( var p in t.GetProperties() )
            {
                if ( p.GetCustomAttributes(false).Count() == 0 && (p.PropertyType.IsPrimitive || p.PropertyType == typeof(string)))
                    sb.AppendLine(String.Format("  {0}: {1}", p.Name, p.GetValue(this,null).ToString() ) );
            }
            if (Overrides != null && Overrides.Count() > 0)
            {
                sb.AppendLine("  Overrides:");
                foreach (var o in Overrides)
                {
                    var m = model.Groups.FindRecursive(p => p.PropertyName == o.Key).FirstOrDefault();
                    if ( m != null )
                    {
                        if ( String.Equals( m.Type, Constants.Password, StringComparison.InvariantCultureIgnoreCase) )
                            sb.AppendLine(String.Format("    {0}: {1}", o.Key, "******"));
                        else
                            sb.AppendLine(String.Format("    {0}: {1}", o.Key, o.Value));
                    }
                    else
                    {
                        // ignore ones not found
                    }
                }
            }
            return sb.ToString();
        }

        // set all the parameters from a dict passed in using reflection
        internal void SetParameters(IDictionary<string, object> parms)
        {
            var props = this.GetType().GetProperties();
            foreach ( var p in parms )
            {
                var prop = props.FirstOrDefault( o => o.Name == p.Key );
                if ( prop != null )
                {
                    prop.SetValue( this, p.Value );
                }
            }
        }

    }
}
