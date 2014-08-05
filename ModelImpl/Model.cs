using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            var xmlGroups = xml.Elements(Constants.Group);

            foreach( var xmlGroup in xmlGroups )
            {
                IGroup group = new Group();
                group.LoadFromXml(xmlGroup);

                IItemBase item = null;

                if (xmlGroup.Attribute(Constants.ArrayValueName) != null && !String.IsNullOrEmpty(xmlGroup.Attribute(Constants.ArrayValueName).Value))
                {
                    item = new ItemList(this);
                    item.LoadFromXml(xmlGroup, values, overrides, rtValuesVersion);
                    item.Group = group;
                    Items.Add(item);
                }
                else
                {
                    foreach (var xmlItem in xmlGroup.Elements(Constants.Item))
                    {
                        item = new Item(this);
                        item.LoadFromXml(xmlItem, values, overrides, rtValuesVersion);
                        item.Group = group;
                        Items.Add(item);
                    }
                }
            }

            // get all the ItemLists in the model
            var lists = xml.Elements(Constants.Group).Where(o => o.Attribute(Constants.ArrayValueName) != null && !String.IsNullOrEmpty(o.Attribute(Constants.ArrayValueName).Value));

        }
    }
}
