
namespace RazorTransform
{
    /// <summary>
    /// event args for something changing, adding, deleting
    /// </summary>
    class ItemChangedArgs : System.EventArgs
    {
        /// <summary>
        /// the group of the item in question
        /// </summary>
        public TransformModelArray Group { get; set; }

        /// <summary>
        /// the item changed, added, deleted
        /// </summary>
        public TransformModelArrayItem Item { get; set; }
    }

    /// <summary>
    /// event args for global changes, load, validate, save, etc.
    /// </summary>
    class ModelChangedArgs : System.EventArgs
    {
    }

}