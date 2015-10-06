using RtPsHost;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace RazorTransform
{
    /// <summary>
    /// implementation of PowerShell configuration
    /// </summary>
    internal class PsConfig : IPsConfig
    {
        const string PsScriptSetName = "RtPsScriptSet";
        public const string PsStepName = "RtPsStep";
        public const string PsTestName = "RtPsTest";
        const string PsNoPromptName = "RtPsNoPrompt";
        const string PsSkipUntilName = "RtPsSkipUntil";
        const string PsWorkingDirName = "RtPsWorkingDir";
        const string PsLogFileName = "RtPsLogFileName";
        const string PsScriptFileName = "RtPsScriptFileName";

        // script file cached
        private XDocument _scriptFile;
        private DateTime _scriptFileWriteTime;

        public PsConfig(IDictionary<string, string> overrideParms)
        {
            if (overrideParms.ContainsKey(PsScriptSetName))
                ScriptSet = overrideParms[PsScriptSetName];

            if (overrideParms.ContainsKey(PsStepName))
            {
                bool temp;
                if ( bool.TryParse( overrideParms[PsStepName], out temp))
                    Step = temp;
            }

            if (overrideParms.ContainsKey(PsNoPromptName))
            {
                bool temp;
                if (bool.TryParse(overrideParms[PsNoPromptName], out temp))
                    NoPrompt = temp;
            }

            if (overrideParms.ContainsKey(PsLogFileName))
                LogFileName = overrideParms[PsLogFileName];
            else
                LogFileName = "Deploy";

            LogFileName = String.Format("{0}_{1}.log", LogFileName, DateTime.Now.ToString("yyMMdd-HHmmss"));
            LogFileName = Path.GetFullPath(LogFileName);

            if (overrideParms.ContainsKey(PsScriptFileName))
                ScriptFile = overrideParms[PsScriptFileName];
            else
                ScriptFile = "PsScripts.xml";

            if (overrideParms.ContainsKey(PsWorkingDirName))
                WorkingDir = overrideParms[PsWorkingDirName];
            else
                WorkingDir = ".";

            WorkingDir = System.IO.Path.GetFullPath(WorkingDir); // convert .. to path

            if (overrideParms.ContainsKey(PsSkipUntilName))
                SkipUntil = overrideParms[PsSkipUntilName];

        }

        public bool NoPromptQuiet { get; set; }

        public bool NoPrompt { get; set; }

        public bool Quiet { get; set; }

        public bool Run { get; set; }

        public bool Step { get; set; }

        public bool Test { get; set; }

        public string ScriptFile { get; set; }

        public string SkipUntil { get; set; }

        public string ScriptSet { get; set; }

        public string LogFileName { get; set; }

        public string WorkingDir { get; set; }

        /// <summary>
        /// Gets the name of the log file base on the type of scripts to run
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public string GetLogFileName(ScriptInfo.ScriptType type)
        {
            if (type == ScriptInfo.ScriptType.preRun)
                return Path.Combine(Path.GetDirectoryName(LogFileName), "Pre_" + Path.GetFileName(LogFileName));
            else
                return LogFileName;
        }

        /// <summary>
        /// Appends the exports to the dict for PowerShell.
        /// </summary>
        /// <param name="dict">The dictionary.</param>
        public void AppendExports(Dictionary<string, object> dict)
        {
            dict[PsLogFileName] = LogFileName;
            dict[PsWorkingDirName] = WorkingDir;
            dict[PsStepName] = Step ? "$True" : "$False";
            dict[PsSkipUntilName] = SkipUntil;
            dict[PsScriptSetName] = ScriptSet;
        }

        /// <summary>
        /// Gets the script sets in the script file, if any
        /// </summary>
        /// <returns>
        /// list of script set names and descriptions
        /// </returns>
        public IDictionary<string, string> GetScriptSets()
        {
            loadScriptFile();

            return _scriptFile.Root.Descendants("scriptSet").ToDictionary(o => o.Attribute("name").Value, p => String.Format("{0} - {1}", p.Attribute("name").Value, p.Attribute("description").Value));
        }

        /// <summary>
        /// Gets all steps.
        /// </summary>
        /// <returns>list of step names and descriptions</returns>
        public IDictionary<string, string> GetAllSteps()
        {
            loadScriptFile();

            return _scriptFile.Root.Descendants("script").Where(o => o.Attribute("id") != null && (o.Attribute("type") == null || String.Equals(o.Attribute("type").Value, "postRun") || String.Equals(o.Attribute("type").Value, "normal")))
                                                     .ToDictionary(o => o.Attribute("id").Value, p => String.Format("{0} - {1}", p.Attribute("name").Value, p.Attribute("description") != null ? p.Attribute("description").Value : String.Empty));

        }

        private void loadScriptFile()
        {
            if (File.Exists(ScriptFile))
            {
                var writeTime = File.GetLastWriteTimeUtc(ScriptFile);
                if (_scriptFile == null || writeTime != _scriptFileWriteTime)
                {
                    _scriptFile = XDocument.Load(ScriptFile);
                    _scriptFileWriteTime = writeTime;
                }
            }
            else
            {
                throw new FileNotFoundException(String.Format(Resource.FileNotFound, ScriptFile));
            }

        }

        public bool IsStepInScriptSet( string scriptSet, string stepId )
        {
            if (String.IsNullOrEmpty(scriptSet) || String.IsNullOrEmpty(stepId))
                return true;
            else 
            {
                loadScriptFile();

                // get all the steps for the set
                var set = _scriptFile.Root.XPathSelectElement(String.Format("/scripts/scriptSet[@name=\"{0}\"]", scriptSet));
                var color = (string)set.Attribute("listType");

                if ( string.Equals(color, "black", StringComparison.CurrentCultureIgnoreCase) ) // black list step shouldn't be in list, otherwise in the list
                    return _scriptFile.Root.XPathSelectElement(String.Format("/scripts/scriptSet[@name=\"{0}\"]/step[@id=\"{1}\"]", scriptSet, stepId)) == null;
                else 
                    return _scriptFile.Root.XPathSelectElement(String.Format("/scripts/scriptSet[@name=\"{0}\"]/step[@id=\"{1}\"]", scriptSet, stepId )) != null;
            }
        }


    }
}
