using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RazorTransform.Model
{
    public interface IItemList : IItemBase, IList<IModel>
    {
        string Key { get; }
        bool Unique { get; }
        RtSort Sort { get; }
        IModel Prototype { get; }
        uint Min { get; }
        uint Max { get; }
    }
}
