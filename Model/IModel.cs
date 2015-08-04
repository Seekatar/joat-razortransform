using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RazorTransform.Model
{
    public interface IModel
    {
        /// <summary>
        /// Gets the root-level items in this model.
        /// </summary>
        /// <value>
        /// The items.
        /// </value>
        IList<IItemBase> Items { get; }

        /// <summary>
        /// Get the exported items.
        /// </summary>
        /// <returns></returns>
        Dictionary<string, object> ExportedItems();

        /// <summary>
        /// get the parent model
        /// </summary>
        IModel Parent { get; }

        /// <summary>
        /// get the root model
        /// </summary>
        /// <returns></returns>
        IModel Root { get; }

        /// <summary>
        /// load the model from XML
        /// </summary>
        /// <param name="xml">The model XML</param>
        /// <param name="values">The values XML to set on IItems in the model</param>
        /// <param name="overrides">Any override values to set</param>
        void LoadFromXml(XElement xml, XElement values, IDictionary<string, string> overrides);

        /// <summary>
        /// if the Items has an IItemList (there will be only one), return it
        /// </summary>
        /// <returns>the list or null</returns>
        IItemList GetList();

        /// <summary>
        /// validate the model.
        /// </summary>
        /// <param name="errors">collection to be populated</param>
        void Validate(ICollection<ValidationError> errors);

        /// <summary>
        /// generate the RtValues XML for the model
        /// </summary>
        void GenerateXml(XElement root);

    }
}
