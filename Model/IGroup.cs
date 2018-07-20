using System.Collections.Generic;
using System.Xml.Linq;

namespace RazorTransform.Model
{
	/// <summary>
	/// interface for a group.  All items and list have a group
	/// </summary>
	public interface IGroup
    {
        /// <summary>
        /// Display name of this item.  May have spaces, special characters, etc.
        /// </summary>
        string DisplayName { get; set; }

        /// <summary>
        /// Visiblity groups used to restrict when the item is shown in the UI
        /// </summary>
        IList<string> VisibilityGroups { get; }

        /// <summary>
        /// Description of the item.  Usually show in the tooltip of the UI
        /// </summary>
        string Description { get; set; }

        /// <summary>
        /// Is this item hidden in UI by default.  Switch can show hidden
        /// </summary>
        bool Hidden { get; set; }

        /// <summary>
        /// load the group from values in XML
        /// </summary>
        /// <param name="xmlGroup"></param>
        void LoadFromXml(XElement xmlGroup);
    }
}
