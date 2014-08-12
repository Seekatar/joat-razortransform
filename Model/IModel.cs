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
        IList<IItemBase> Items { get; }

        IModel Parent { get; }

        void LoadFromXml(XElement xml, XElement values, IDictionary<string, string> overrides, int rtValuesVersion);

    }
}
