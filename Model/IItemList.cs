using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RazorTransform.Model
{
    public interface IItemList : IItemBase, IList<IModel>
    {
        IModel ProtoType { get; }
        uint Min { get; }
        uint Max { get; }
    }
}
