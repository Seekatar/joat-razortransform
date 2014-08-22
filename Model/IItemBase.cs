using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace RazorTransform.Model
{
    /// <summary>
    /// Base interface for items and item lists
    /// </summary>
    public interface IItemBase
    {
        /// <summary>
        /// Name of the item.  This is the property name on the object
        /// </summary>
        string  Name { get; set; }

        /// <summary>
        /// Description of the item.  Usually show in the tooltip of the UI
        /// </summary>
        string Description { get; set; }

        /// <summary>
        /// Group this item is in
        /// </summary>
        IGroup Group { get; set; }

        /// <summary>
        /// Parent this item belongs to
        /// </summary>
        IModel Parent { get; }

        /// <summary>
        /// Display name of this item.  May have spaces, special characters, etc.
        /// </summary>
        string DisplayName { get; set; }

        /// <summary>
        /// Visiblity groups used to restrict when the item is shown in the UI
        /// </summary>
        IList<string> VisibilityGroups { get; }

        /// <summary>
        /// Is this item hidden in UI by default.  Switch can show hidden
        /// </summary>
        bool Hidden { get; set; }

        /// <summary>
        /// min value for the item.  For arrays, it's the count, strings, the length
        /// </summary>
        Int64 Min { get; set; }

        /// <summary>
        /// max value for the item.  For arrays, it's the count, strings, the length
        /// </summary>
        Int64 Max { get; set; }

        /// <summary>
        /// load the model from XML
        /// </summary>
        /// <param name="xml">The model XML</param>
        /// <param name="values">The values XML to set on IItems in the model</param>
        /// <param name="overrides">Any override values to set</param>
        void LoadFromXml(XElement xml, XElement values, IDictionary<string, string> overrides);

        /// <summary>
        /// load the values only from XML.  The object has already been loaded
        /// </summary>
        /// <param name="values">The values XML to set on IItems in the model</param>
        /// <param name="overrides">Any override values to set</param>
        void LoadValuesFromXml(XElement values, IDictionary<string, string> overrides);

        /// <summary>
        /// validate the model.
        /// </summary>
        /// <param name="errors">collection to be populated</param>
        void Validate(ICollection<ValidationError> errors);

        /// <summary>
        /// Generate the RtValues XML adding it under the root XML passed in
        /// </summary>
        /// <param name="root">root element to add to</param>
        void GenerateXml(XElement root);

    }
}
