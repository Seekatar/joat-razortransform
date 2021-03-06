﻿using RazorTransform.Model;
using System;
using System.Collections.Generic;
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
        IItem CreateItem(IModel parent, IGroup group, XElement e);

        /// <summary>
        /// create the control from the item
        /// </summary>
        /// <param name="info">an item created earlier when CreateItem was called</param>
        /// <param name="binding">the binding object for the control</param>
        /// <param name="itemChanged"> call when the item has changed to indicate dirty</param>
        /// <returns>the control to show on the screen</returns>
        Control CreateControl(IItem info, Binding binding, Action<IItem> itemChanged);

        /// <summary>
        /// called after the object is constructed
        /// </summary>
        /// <param name="config">The model's configuration.</param>
        /// <param name="parms">any parameters from your XML</param>
        void Initialize(IModelConfig config, IDictionary<string, string> parms);

        /// <summary>
        /// Set with name from XML on load.
        /// </summary>
        string Name { get; set; }

    }
}
