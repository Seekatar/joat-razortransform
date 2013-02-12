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
        private TransformModelGroup _settingsGroup;
        private string _objectFile;
        private string _valuesFile;

        // object definition for settings is really static since code relies on it
        static string _settingsXml =
@"<RtObject>
  <group name=""Settings"" description=""Settings that control how Artie works."" hidden=""False"">
    <item name=""RTSettings_Title"" displayName=""Title"" description=""Title to show in the titlebar"" type=""" + RtType.String + @""" defaultValue=""RazorTransform""/>
    <item name=""RTSettings_LastPath"" displayName=""Save Path"" description=""Location used when saving."" type=""" + RtType.Folder + @""" defaultValue=""..""/>
    <item name=""RTSettings_LastTemplatePath"" displayName=""Template Path"" description=""Location for retrieving templates."" type=""" + RtType.Folder + @""" defaultValue=""Templates""/>
  </group>
</RtObject>";

        public void Load(IDictionary<string, string> overrideParms)
        {
            setOverrides(overrideParms);

            // load the settings
            var settingsDefinition = XDocument.Parse(_settingsXml).Root;
            var settings = new TransformModel();
            settings.LoadValuesFromXml(settingsDefinition, this);

            _settingsGroup = settings.Groups[0];
            _settings = settings.BuildObject(settings.Groups, false, false);
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
        public TransformModelGroup Group { get { return _settingsGroup;  } }

        // for editing
        public IList<TransformModelGroup> ConfigInfo 
        {
            get { return new List<TransformModelGroup>() { _settingsGroup }; } 
        }

        // values saved in values file
        public string Title { get { return this["RTSettings_Title"]; } set { this["RTSettings_Title"] = value; } }
        public string OutputFolder 
        { 
            get 
            { 
                var temp = OverrideOutputFolder ?? this["RTSettings_LastPath"]; 
                return Path.GetFullPath(temp);
            }
            set { this["RTSettings_LastPath"] = value; }
        }
        public string TemplateFolder { get { return Path.GetFullPath(OverrideTemplateFolder ?? this["RTSettings_LastTemplatePath"]); } set { this["RTSettings_LastTemplatePath"] = value; } }

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

        /// <summary>
        /// override output folder, if passed in from command line
        /// </summary>
        [IgnoreDataMemberAttribute]
        public string OverrideOutputFolder { get; set; }

        /// <summary>
        /// override template folder, if passed in from command line
        /// </summary>
        [IgnoreDataMemberAttribute]
        public string OverrideTemplateFolder { get; set; }

        /// <summary>
        /// -run switch to indicated not to show UI, and just run transformation
        /// </summary>
        public bool Run { get; set; }

        /// <summary>
        /// log file where to write output
        /// </summary>
        public string LogFile { get; set; }

        /// <summary>
        /// list of override value passed in from the command line to override
        /// values in RtValues.xml
        /// </summary>
        public IDictionary<string, string> Overrides { get; set; }

        private void setOverrides(IDictionary<string, string> overrideParms)
        {
            Overrides = overrideParms;
        }

        /// <summary>
        /// helper function to parse overrides passed in from command line 
        /// </summary>
        /// <param name="overrideParms"></param>
        /// <returns></returns>
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

        /// <summary>
        /// indexing override to allow access to setting via string index
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
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
                if (p.GetCustomAttributes(false).Count() == 0 && (p.PropertyType.IsPrimitive || p.PropertyType == typeof(string)))
                {
                    String value = "<null>";
                    var v = p.GetValue(this, null);
                    if ( v != null)
                        value = v.ToString();
                    sb.AppendLine(String.Format("  {0}: {1}", p.Name, value));
                }
            }
            if (Overrides != null && Overrides.Count() > 0)
            {
                sb.AppendLine("  Overrides:");
                foreach (var o in Overrides)
                {
                    var m = model.Groups.FindRecursive(p => p.PropertyName == o.Key).FirstOrDefault();
                    if ( m != null )
                    {
                        if ( m.Type == RtType.Password )
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
