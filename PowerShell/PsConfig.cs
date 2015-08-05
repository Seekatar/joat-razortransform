using RtPsHost;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RazorTransform
{
    /// <summary>
    /// implementation of PowerShell configuration
    /// </summary>
    internal class PsConfig : IPsConfig
    {
        public PsConfig(IDictionary<string, string> overrideParms)
        {
            if (overrideParms.ContainsKey("PsScriptSet"))
                ScriptSet = overrideParms["PsScriptSet"];

            if (overrideParms.ContainsKey("PsStep"))
            {
                bool temp;
                if ( bool.TryParse( overrideParms["PsStep"], out temp))
                    Step = temp;
            }

            if (overrideParms.ContainsKey("PsNoPrompt"))
            {
                bool temp;
                if (bool.TryParse(overrideParms["PsNoPrompt"], out temp))
                    NoPrompt = temp;
            }

            if (overrideParms.ContainsKey("PsStep"))
            {
                bool temp;
                if (bool.TryParse(overrideParms["PsStep"], out temp))
                    Step = temp;
            }

            if (overrideParms.ContainsKey("PsLogFileName"))
                LogFileName = overrideParms["PsLogFileName"];
            else
                LogFileName = "Deploy";

            LogFileName = String.Format("{0}_{1}.log", LogFileName, DateTime.Now.ToString("yyMMdd-HHmmss"));
            LogFileName = Path.GetFullPath(LogFileName);

            if (overrideParms.ContainsKey("PsScriptFileName"))
                ScriptFile = overrideParms["PsScriptFileName"];
            else
                ScriptFile = "PsScripts.xml";

            if (overrideParms.ContainsKey("PsWorkingDir"))
                WorkingDir = overrideParms["PsWorkingDir"];
            else
                WorkingDir = ".";

            WorkingDir = System.IO.Path.GetFullPath(WorkingDir); // convert .. to path

            if (overrideParms.ContainsKey("PsSkipUntil"))
                SkipUntil = overrideParms["PsSkipUntil"];

        }

        public bool NoPromptQuiet { get; set; }

        public bool NoPrompt { get; set; }

        public bool Quiet { get; set; }

        public bool Run { get; set; }

        public bool Step { get; set; }

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
            dict["PsLogFileName"] = LogFileName;
            dict["PsWorkingDir"] = WorkingDir;
            dict["PsStep"] = Step ? "$True" : "$False";
        }

        /// <summary>
        /// Gets the script sets in the script file, if any
        /// </summary>
        /// <returns>
        /// list of script set names and descriptions
        /// </returns>
        public IDictionary<string, string> GetScriptSets()
        {
            if (File.Exists(ScriptFile))
            {
                var doc = XDocument.Load(ScriptFile);
                return doc.Root.Descendants("scriptSet").ToDictionary(o => o.Attribute("name").Value, p => String.Format("{0} - {1}",p.Attribute("name").Value, p.Attribute("description").Value));
            }
            else
            {
                throw new FileNotFoundException(String.Format(Resource.FileNotFound, ScriptFile));
            }
        }

        /// <summary>
        /// Gets all steps.
        /// </summary>
        /// <returns>list of step names and descriptions</returns>
        public IDictionary<string, string> GetAllSteps()
        {
            if (File.Exists(ScriptFile))
            {
                var doc = XDocument.Load(ScriptFile);
                return doc.Root.Descendants("script").Where(o => o.Attribute("type") == null || String.Equals(o.Attribute("type").Value, "postRun") || String.Equals(o.Attribute("type").Value, "normal"))
                                                     .ToDictionary(o => o.Attribute("name").Value, p => String.Format("{0} - {1}", p.Attribute("name").Value, p.Attribute("description") != null ? p.Attribute("description").Value : String.Empty));
            }
            else
            {
                throw new FileNotFoundException(String.Format(Resource.FileNotFound, ScriptFile));
            }

        }


    }
}
