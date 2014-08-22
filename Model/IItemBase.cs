using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RazorTransform.Model
{
    public interface IItemBase
    {
        string  Name { get; set; }
        string Description { get; set; }
        IGroup Group { get; set; }
        IModel Parent { get; }
        string DisplayName { get; set; }
        IList<string> VisibilityGroups { get; }
        bool Hidden { get; set; }

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
