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

        public Model(IModel src)
        {
            throw new NotImplementedException();
        }

        #region MyRegion
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
        public IItemList GetList()
        {
            return Items.FirstOrDefault() as IItemList;
        }
        #endregion

        public void LoadFromXml(System.Xml.Linq.XElement xml, System.Xml.Linq.XElement values, IDictionary<string, string> overrides, int rtValuesVersion)
        {
            // get all the Items in the model
            var xmlGroups = xml.Elements(Constants.Group);

            foreach (var xmlGroup in xmlGroups)
            {
                IGroup group = new Group();
                group.LoadFromXml(xmlGroup);

                IItemBase item = null;

                if (xmlGroup.Attribute(Constants.ArrayValueName) != null && !String.IsNullOrEmpty(xmlGroup.Attribute(Constants.ArrayValueName).Value))
                {
                    item = new ItemList(this, group);
                    item.LoadFromXml(xmlGroup, values, overrides, rtValuesVersion);
                    Items.Add(item);
                }
                else
                {
                    foreach (var xmlItem in xmlGroup.Elements(Constants.Item))
                    {
                        item = new Item(this, group);
                        item.LoadFromXml(xmlItem, values, overrides, rtValuesVersion);
                        Items.Add(item);
                    }
                }
            }

            // get all the ItemLists in the model
            var lists = xml.Elements(Constants.Group).Where(o => o.Attribute(Constants.ArrayValueName) != null && !String.IsNullOrEmpty(o.Attribute(Constants.ArrayValueName).Value));

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
                ModelConfig.Instance.OnModelValidate();
            }
            catch (Exception)
            {
            }
            validate(Items, errors);
        }

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
                    if (!(item is IItem) || !(item as IItem).IsPassword)
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
        #endregion

        #region DynamicObject Implementation
        internal static bool TrySetMemberFn(SetMemberBinder binder, object value, List<IItemBase> items)
        {
            var list = items.Where(p => p.Name == binder.Name);
            if (list != null)
            {
                var result = list.FirstOrDefault();
                if (result is Item)
                {
                    var item = (result as IItem);
                    item.ExpandedValueStr = value.ToString();
                    return true;
                }
            }
            return false;
        }

        internal static bool TryGetMemberFn(GetMemberBinder binder, out object result, IModel model, List<IItemBase> items)
        {
            return TryGetMemberFn(binder.Name, out result, model, items);
        }

        internal static bool TryGetMemberFn(string name, out object result, IModel model, List<IItemBase> items)
        {
            bool ret = false;
            result = null;

            if (name == Constants.Root)
            {
                result = model.Root;
                return true;
            }

            if (name == Constants.Parent)
            {
                if (model.Parent != null)
                    result = model.Parent;
                else
                    result = model;
                return true;
            }

            // find in all the items with this name
            var list = items.Where(p => p.Name == name && p is Item);
            var arrays = items.Where(p => p.Name == name && p is ItemList);
            if (arrays != null)
            {
                ret = true;
                result = arrays;
            }
            else if (list != null)
            {
                result = list.FirstOrDefault();
                ret = true;
                if (result is IItem)
                {
                    var item = (result as IItem);
                    if (item.ExpandedValueStr != null)
                        result = item.ExpandedValueStr;
                    else
                        result = item.ValueStr;

                    if (item.Type == RtType.Bool)
                        result = item.GetValue<bool>();
                    else if (item.Type == RtType.Int16)
                        result = item.GetValue<Int16>();
                    else if (item.Type == RtType.Int32)
                        result = item.GetValue<Int32>();
                    else if (item.Type == RtType.Int64)
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
