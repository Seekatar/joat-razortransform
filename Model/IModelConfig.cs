using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RazorTransform.Model
{
    /// <summary>
    /// model configuration data from the RtObject.xml and RtValues files
    /// </summary>
    public interface IModelConfig
    {
        IDictionary<string, IDictionary<string, string>> Enums { get; }

        IDictionary<string, string> Regexes { get; }

        IDictionary<string, Custom.ICustomRazorTransformType> CustomTypes { get; }

        /// <summary>
        /// root XML element in model
        /// </summary>
        XElement Root { get; }

        /// <summary>
        /// root values XML element 
        /// </summary>
        XElement ValuesRoot { get; }

        /// <summary>
        /// the version of the RtValues.xml file
        /// </summary>
        int RtValuesVersion { get; }

        /// <summary>
        /// load the config
        /// </summary>
        /// <param name="settings">Settings object with all the environment settings</param>
        /// <param name="loadValues">if true load values from the file in settings</param>
        /// <param name="objectRoot">override object model root node</param>
        /// <returns>true if loaded ok</returns>
        bool Load(Settings settings, bool loadValues = true, XElement objectRoot = null);

        event EventHandler<ItemChangedArgs> ItemAdded;
        event EventHandler<ItemChangedArgs> ItemChanged;
        event EventHandler<ItemChangedArgs> ItemDeleted;
        event EventHandler<ModelChangedArgs> ModelLoaded;
        event EventHandler<ModelChangedArgs> ModelSaved;
        event EventHandler<ModelChangedArgs> ModelValidate;

        /// <summary>
        /// Fired when an item is added to an array
        /// </summary>
        /// <param name="args"></param>
        void OnItemAdded(ItemChangedArgs args);

        /// <summary>
        /// fired when an item is changed in an array
        /// </summary>
        /// <param name="args"></param>
        void OnItemChanged(ItemChangedArgs args);

        /// <summary>
        /// fired when an item is deleted from an array
        /// </summary>
        /// <param name="args"></param>
        void OnItemDeleted(ItemChangedArgs args);

        /// <summary>
        /// fired after all XML is parsed and the model arrays have been loaded
        /// </summary>
        /// <param name="args"></param>
        void OnModelLoaded();

        /// <summary>
        /// called before saving the model
        /// </summary>
        /// <param name="args"></param>
        void OnModelValidate();

        /// <summary>
        /// called after the model has been saved
        /// </summary>
        /// <param name="args"></param>
        void OnModelSaved();


    }
}
