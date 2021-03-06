﻿
using RazorTransform.Model;
using System.Collections.Generic;
namespace RazorTransform
{
    /// <summary>
    /// event args for something changing, adding, deleting in a list
    /// </summary>
    public class ItemChangedArgs : System.EventArgs
    {
        /// <summary>
        /// the group of the item in question
        /// </summary>
        public IItemList List { get; set; }

        /// <summary>
        /// the item changed, added, deleted
        /// </summary>
        public IModel Item { get; set; }
    }

    /// <summary>
    /// event args for global changes, load, validate, save, etc.
    /// </summary>
    public class ModelChangedArgs : System.EventArgs
    {
        public ModelChangedArgs(IModel model) { Model = model; }
        public IModel Model { get; private set; }
    }

    /// <summary>
    /// event args for global changes, load, validate, save, etc.
    /// </summary>
    public class ModelLoadedArgs : System.EventArgs
    {
        public ModelLoadedArgs(IModel model) { Model = model; }
        public IModel Model { get; private set; }
    }

    /// <summary>
    /// class use when validating
    /// </summary>
    public class ModelValidateArgs
    {
        public ModelValidateArgs(IModel model, ICollection<ValidationError> errors) { Model = model; Errors = errors; }
        public IModel Model { get; private set; }
        public ICollection<ValidationError> Errors { get; private set; }
    }


}