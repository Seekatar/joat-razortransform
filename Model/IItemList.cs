﻿using System.Collections.Generic;

namespace RazorTransform.Model
{
    /// <summary>
    /// Interface for lists of items
    /// </summary>
    public interface IItemList : IItemBase, IList<IModel>
    {
        /// <summary>
        /// Key format for the items in the list
        /// </summary>
        /// This includes name of IItems in the IModel in this list
        string KeyFormat { get; }

        /// <summary>
        /// If true, the values generated by KeyFormat must be unique
        /// </summary>
        bool Unique { get; }

        /// <summary>
        /// enum to indicate if the list is sorted
        /// </summary>
        RtSort Sort { get; }

        /// <summary>
        /// The prototoype model that has all the IItems with default values
        /// used when creating a new IItem for this list
        /// </summary>
        IModel Prototype { get; }

        /// <summary>
        /// Make the key name for a given model
        /// </summary>
        /// <param name="model">the model that is part of the list</param>
        /// <returns>the display name for the UI, or &lt;Unknown&gt; if an error occurred</returns>
        string ModelKeyName(IModel model);

        /// <summary>
        /// copy the values from another object to this one
        /// </summary>
        /// <param name="src"></param>
        /// <param name="parent"></param>
        void CopyValuesFrom(IItemList src, IModel parent = null);
    }
}