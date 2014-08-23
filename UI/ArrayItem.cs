using RazorTransform.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RazorTransform
{
    /// <summary>
    /// used to attach the list and model for editing list items
    /// </summary>
    internal class ArrayItem
    {
        internal IItemList List { get; private set; }
        internal IModel Model { get; private set; }

        internal ArrayItem( IItemList list, IModel model )
        {
            List = list;
            Model = model;
        }
    }
}
