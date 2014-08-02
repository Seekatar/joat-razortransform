using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace RazorTransform.Model
{
    class Model : IModel
    {
        List<IItemBase> _items = new List<IItemBase>();

        public IList<IItemBase> Items
        {
            get { return _items; }
        }

        public void LoadFromXml(System.Xml.Linq.XElement xml, System.Xml.Linq.XElement values, IDictionary<string, string> overrides, int rtValuesVersion)
        {
            // get all the Items in the model
            var items = xml.Elements(Constants.Group);

            foreach( var i in items )
            {
                Item item = null;
                if (i.Attribute(Constants.ArrayValueName) != null && !String.IsNullOrEmpty(i.Attribute(Constants.ArrayValueName).Value))
                {
                    item = new ItemList(this);
                }
                else
                {
                    item = new Item(this);
                }
                item.LoadFromXml(i, values, overrides, rtValuesVersion);
                Items.Add(item);
            }

            // get all the ItemLists in the model
            var lists = xml.Elements(Constants.Group).Where(o => o.Attribute(Constants.ArrayValueName) != null && !String.IsNullOrEmpty(o.Attribute(Constants.ArrayValueName).Value));

        }
    }
}
