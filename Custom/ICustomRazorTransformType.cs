using System.Windows.Controls;
using System.Windows.Data;
using System.Xml.Linq;

namespace RazorTransform.Custom
{
    /// <summary>
    /// interface for custom type implemenations
    /// </summary>
    public interface ICustomRazorTransformType
    {
        /// <summary>
        /// called when the XML is parsed.  The implemenation creates the item that will load itself 
        /// via TransformModelItem.LoadFromXml
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="e">XML for this item being parsed to allow for custom attributes</param>
        /// <returns>an empty object that will load itself</returns>
        TransformModelItem CreateItem(TransformModelGroup parent, XElement e);

        /// <summary>
        /// create the control from the item
        /// </summary>
        /// <param name="info">an item created earlier when CreateItem was called</param>
        /// <param name="binding">the binding object for the control</param>
        /// <returns>the control to show on the screen</returns>
        Control CreateControl( TransformModelItem info, Binding binding);
    }
}
