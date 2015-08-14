using RazorTransform.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RazorTransform.Custom
{
    /// <summary>
    /// custom class for popuating dropdown with script sets
    /// </summary>
    class ScriptSetEnum : CustomRazorTransformTypeBase
    {

        string scriptSetName { get; set; }       // name of <enum> in RTObject.xml to fill in
        string skipUntilName { get; set; }       // name of <enum> in RTObject.xml to fill in
        IItem scriptSet { get; set; }
        IItem skipUntil { get; set; }
        IItem step { get; set; }
        IItem test { get; set; }

        #region EventHandlers

        void OnModelLoaded(object sender, ModelLoadedArgs e)
        {
            scriptSet = e.Model.Items.FirstOrDefault(o => o is IItem && (o as IItem).Type == RtType.Enum && (o as IItem).Name == scriptSetName) as IItem;
            if (System.IO.File.Exists(Settings.Instance.PowerShellConfig.ScriptFile))
            {
                skipUntil = e.Model.Items.FirstOrDefault(o => o is IItem && (o as IItem).Type == RtType.Enum && (o as IItem).Name == skipUntilName) as IItem;
                step = e.Model.Items.FirstOrDefault(o => o is IItem && (o as IItem).Type == RtType.Bool && (o as IItem).Name == PsConfig.PsStepName) as IItem;
                test = e.Model.Items.FirstOrDefault(o => o is IItem && (o as IItem).Type == RtType.Bool && (o as IItem).Name == PsConfig.PsTestName) as IItem;

                var model = e.Model;
                // only fill if we have items matching
                if (scriptSet != null)
                {
                    fillEnum(scriptSet.OriginalTypeStr, Settings.Instance.PowerShellConfig.GetScriptSets(), scriptSet, "Default - Run all configured steps", Settings.Instance.PowerShellConfig.ScriptSet);
                }
                if (skipUntil != null)
                {
                    fillEnum(skipUntil.OriginalTypeStr, Settings.Instance.PowerShellConfig.GetAllSteps(), skipUntil, "Default - Don't skip any steps", Settings.Instance.PowerShellConfig.SkipUntil);
                }

                scriptSet.NoSaveValue = true;
                if (skipUntil != null)
                {
                    skipUntil.NoSaveValue = true;
                }
                if (step != null)
                {
                    step.NoSaveValue = true;
                }
                if (test != null)
                {
                    test.NoSaveValue = true;
                }
            }
            else if (scriptSet != null)
            {
                // hide the group with this
                scriptSet.Group.Hidden = true;
            }
            
        }

        void OnModelSaved( object sender, ModelChangedArgs e )
        {
            // set the values in the settings
            if ( scriptSet != null )
            {
                Settings.Instance.PowerShellConfig.ScriptSet = scriptSet.Value;
            }
            if ( skipUntil != null )
            {
                Settings.Instance.PowerShellConfig.SkipUntil = skipUntil.Value;
            }
            if ( step != null )
            {
                Settings.Instance.PowerShellConfig.Step = Convert.ToBoolean(step.Value);
            }
            if ( test != null )
            {
                Settings.Instance.PowerShellConfig.Test = Convert.ToBoolean(test.Value);
            }
        }

        void OnModelValidate(object sender, ModelValidateArgs e)
        {
            if (e.Model.Parent == null) // only validate root
            {
                // make sure the step is in the script set, if set
                if (skipUntil != null && skipUntil.Value != string.Empty && scriptSet != null && scriptSet.Value != string.Empty)
                {
                    if (!Settings.Instance.PowerShellConfig.IsStepInScriptSet(scriptSet.Value, skipUntil.Value))
                    {
                        e.Errors.Add(new ValidationError(String.Format("SkipUntil value of {0} is not in scriptSet {1}", skipUntil.Value, scriptSet.Value), skipUntil));
                    }
                }
            }
        }

        private void fillEnum( string name, IDictionary<string,string> values, IItem item, string defaultName, string valueToMatch )
        {
            // add the custom and the enums to a type enum, if it exists
            if (ModelConfig.Instance.Enums.ContainsKey(name) && ModelConfig.Instance.Enums[name] != null)
            {
                var rtTypes = ModelConfig.Instance.Enums[name];
                rtTypes.Clear();
                rtTypes.Add(new KeyValuePair<string, string>(String.Empty, defaultName));

                foreach (var s in values)
                {
                    rtTypes.Add(new KeyValuePair<string, string>(s.Key, checkLen(s.Value,100)));
                }

                if (item != null)
                {
                    var value = string.Empty;
                    var match = rtTypes.FirstOrDefault(o => String.Equals(o.Key, valueToMatch, StringComparison.CurrentCultureIgnoreCase));
                    if (match.Key != null)
                        value = match.Key;
                    (item as IItem).Value = value;
                }
            }

        }

        private string checkLen(string p1, int p2)
        {
            if ( p1.Length > p2 )
                return p1.Substring(0,p2)+"...";
            else 
                return p1;
        }
        #endregion

        public ScriptSetEnum()
        {
        }

        public override void Initialize(IModelConfig config, IDictionary<string, string> parms)
        {
            config.ModelLoaded += OnModelLoaded;
            config.ModelSaved += OnModelSaved;
            config.ModelValidate += OnModelValidate;

            setParms(parms);
        }
    }
}
