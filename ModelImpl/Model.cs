using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RazorTransform.Model
{
    class Model : System.Dynamic.DynamicObject, IModel
    {
        List<IItemBase> _items = new List<IItemBase>();

        public IList<IItemBase> Items
        {
            get { return _items; }
        }

        public Model()
        {
        }

        public Model(IModel parent)
        {
            Parent = parent;
        }

        /// <summary>
        /// copy constructor
        /// </summary>
        /// <param name="src"></param>
        /// <param name="parent"></param>
        public Model(IModel src, IModel parent)
        {
            deepCopy(src);
            if (parent != null)
                Parent = parent;
            else
                Parent = src.Parent;
        }

        #region IModel Properties
        public IModel Parent
        {
            get;
            private set;
        }

        /// <summary>
        /// get the root model
        /// </summary>
        /// <returns></returns>
        public IModel Root
        {
            get
            {
                IModel ret = this;
                while (ret.Parent != null)
                    ret = ret.Parent;
                return ret;
            }
        }

        #endregion

        #region IModel methods
        /// <summary>
        /// Get the exported items.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, object> ExportedItems()
        {
            var dict = new Dictionary<string, object>();
            exportedItems(Items, dict);
            return dict;
        }

        /// <summary>
        /// Recursively get items to export.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="dict">The dictionary.</param>
        private void exportedItems(IEnumerable<IItemBase> items, Dictionary<string, object> dict)
        {
            foreach (var i in items.OfType<IItem>().Where( o => o.IsExported ))
            {
                dict[i.Name] = i.ExpandedValue;
            }
            foreach (var i in items.OfType<IItemList>())
            {
                foreach (var m in i)
                {
                    exportedItems(m.Items, dict);
                }
            }
        }

        public IItemList GetList()
        {
            return Items.FirstOrDefault() as IItemList;
        }

        /// <summary>
        /// copy all the values from an existing item
        /// </summary>
        /// <param name="temp"></param>
        void deepCopy(IModel src)
        {
            if (!Object.ReferenceEquals(this, src))
            {
                Parent = src.Parent;
                Items.Clear();

                foreach (var i in src.Items)
                {
                    // copy constructor, with parent
                    var newOne = (IItemBase)Activator.CreateInstance(i.GetType(), i, this);
                    Items.Add(newOne);
                }
            }
        }

        public void LoadFromXml(System.Xml.Linq.XElement xml, System.Xml.Linq.XElement values, IDictionary<string, string> overrides)
        {
            Items.Clear(); // reloading

            // get all the Items in the model
            var xmlGroups = xml.Elements(Constants.Group);
            var xmlUngroupedItems = xml.Elements(Constants.Item);

            if (xmlUngroupedItems.Any())
            {
                // fake out a group based on parent
                IGroup dummy = new Group();
                dummy.LoadFromXml(xml);
                var groupList = xmlGroups.ToList();
                var dummyXml = new XElement(Constants.Group,
                                        new XAttribute(Constants.Name, dummy.DisplayName),
                                        new XAttribute(Constants.Hidden, dummy.Hidden),
                                        new XAttribute(Constants.Description, dummy.Description));
                dummyXml.Add(xmlUngroupedItems);
                groupList.Insert(0, dummyXml);
                xmlGroups = groupList;
            }

            foreach (var xmlGroup in xmlGroups)
            {
                IGroup group = new Group();
                group.LoadFromXml(xmlGroup);

                IItemBase item = null;

                if (xmlGroup.Attribute(Constants.ArrayValueName) != null && !String.IsNullOrEmpty(xmlGroup.Attribute(Constants.ArrayValueName).Value))
                {
                    item = new ItemList(this, group);
                    item.LoadFromXml(xmlGroup, values, overrides);
                    Items.Add(item);
                }
                else
                {
                    foreach (var xmlItem in xmlGroup.Elements(Constants.Item))
                    {
                        item = new Item(this, group);
                        item.LoadFromXml(xmlItem, values, overrides);
                        if ( (item as IItem).IsPassword )
                        {
                            // what a hack
                            item = new PasswordItem(this, group);
                            item.LoadFromXml(xmlItem, values, overrides);
                        }
                        Items.Add(item);
                    }
                }
            }
        }

        /// <summary>
        /// generate the RtValues XML for the model
        /// </summary>
        /// <returns></returns>
        public void GenerateXml(XElement root)
        {
            var groups = Items.GroupBy(o => o.Group);

            saveGroups(root, groups);
        }

        /// <summary>
        /// validate the model. 
        /// </summary>
        public void Validate(ICollection<ValidationError> errors)
        {
            try
            {
                ModelConfig.Instance.OnModelValidate(new ModelValidateArgs(this, errors));
            }
            catch (Exception)
            {
            }
            validate(Items, errors);
        }
        #endregion

        private void validate(IEnumerable<IItemBase> items, ICollection<ValidationError> errors)
        {
            foreach (var g in items)
            {
                g.Validate(errors);
            }
        }

        private void saveGroups(XElement root, IEnumerable<IGrouping<IGroup, IItemBase>> groups)
        {
            foreach (var g in groups)
            {
                foreach (var item in g)
                {
                    if (!(item is IItem) || !(item as IItem).NoSaveValue) // never put passwords or other no save items in XML
                        item.GenerateXml(root);
                }
            }
        }

        #region DynamicObject Overrides
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            return TrySetMemberFn(binder, value, _items);
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            return TryGetMemberFn(binder, out result, this, _items);
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return _items.Select(o => o.Name);
        }
        #endregion

        #region DynamicObject Implementation
        internal static bool TrySetMemberFn(SetMemberBinder binder, object value, IEnumerable<IItemBase> items)
        {
            var list = items.Where(p => p.Name == binder.Name);
            if (list != null)
            {
                var result = list.FirstOrDefault();
                if (result is Item)
                {
                    var item = (result as IItem);
                    item.ExpandedValue = value.ToString();
                    return true;
                }
            }
            return false;
        }

        internal static bool TryGetMemberFn(GetMemberBinder binder, out object result, IModel model, IEnumerable<IItemBase> items)
        {
            return TryGetMemberFn(binder.Name, out result, model, items);
        }

        internal static bool TryGetMemberFn(string name, out object result, IModel model, IEnumerable<IItemBase> items)
        {
            bool ret = false;
            result = null;

            if (name == Constants.Root)
            {
                result = model.Root;
                return true;
            }
            else if (name == Constants.Parent)
            {
                if (model.Parent != null)
                    result = model.Parent;
                else
                    result = model;
                return true;
            }
            else if ( name == Constants.CurrentSettings)
            {
                result = Settings.Instance;
                return true;
            }

            // find in all the items with this name
            result = items.FirstOrDefault(p => p.Name == name);
            if (result != null)
            {
                ret = true;
                if (result is IItem)
                {
                    var item = (result as IItem);
                    if (item.ExpandedValue != null)
                        result = item.ExpandedValue;
                    else
                        result = item.Value;

                    if (item.Type == RtType.Bool)
                        result = item.GetValue<bool>();
                    else if (item.Type == RtType.Int)
                        result = item.GetValue<Int64>();
                }
                else if (!(result is IItemList))
                {
                    if (result != null)
                        throw new Exception("Bad type found for item " + name + " " + result.GetType().Name);
                    else
                        System.Diagnostics.Debug.WriteLine("Null result for " + name);
                }
            }
            return ret;
        }
        #endregion
    }
}
