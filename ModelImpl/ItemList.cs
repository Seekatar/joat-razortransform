using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RazorTransform.Model
{
    class ItemList : IItemList
    {
        private List<IModel> _models = new List<IModel>();

        public ItemList(IModel parent)
        {
            Parent = parent;
        }

        #region IItemList/IItemBase properties
        public IModel Prototype
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }

        public IGroup Group
        {
            get;
            set;
        }

        public IModel Parent
        {
            get;
            private set;
        }

        public string Key
        {
            get;
            private set;
        }

        public bool Unique
        {
            get;
            private set;
        }

        public RtSort Sort
        {
            get;
            private set;
        }

        public uint Min
        {
            get;
            set;
        }

        public uint Max
        {
            get;
            set;
        }

        public string DisplayName
        {
            get;
            set;
        }
        #endregion

        #region IList Implementation
        public int IndexOf(IModel item)
        {
            return _models.IndexOf(item);
        }

        public void Insert(int index, IModel item)
        {
            _models.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _models.RemoveAt(index);
        }

        public IModel this[int index]
        {
            get
            {
                return _models[index];
            }
            set
            {
                _models[index] = value;
            }
        }

        public void Add(IModel item)
        {
            _models.Add(item);
        }

        public void Clear()
        {
            _models.Clear();
        }

        public bool Contains(IModel item)
        {
            return _models.Contains(item);
        }

        public void CopyTo(IModel[] array, int arrayIndex)
        {
            _models.CopyTo(array,arrayIndex);
        }

        public int Count
        {
            get { return _models.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(IModel item)
        {
            return _models.Remove(item);
        }

        public IEnumerator<IModel> GetEnumerator()
        {
            return _models.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _models.GetEnumerator();
        }
        #endregion

        public void LoadFromXml(System.Xml.Linq.XElement xml, System.Xml.Linq.XElement values, IDictionary<string, string> overrides, int rtValuesVersion)
        {
            // load the arrray meta data
            Name = (string)xml.Attribute(Constants.ArrayValueName) ?? "";
            Key = (string)xml.Attribute(Constants.Key) ?? "";
            Min = (uint?)xml.Attribute(Constants.Min) ?? UInt32.MinValue;
            Max = (uint?)xml.Attribute(Constants.Max) ?? UInt32.MaxValue;
            Unique = (bool?)xml.Attribute(Constants.Unique) ?? false;
            var sortStr = (string)xml.Attribute(Constants.Sort) ?? RtSort.None.ToString();

            bool ascending;
            // old RtObject took true/false
            if (bool.TryParse(sortStr, out ascending))
            {
                Sort = ascending ? RtSort.Ascending : RtSort.None;
            }
            else
            {
                RtSort sort;
                if (Enum.TryParse<RtSort>(sortStr, out sort))
                    Sort = sort;
                else
                    Sort = RtSort.None;
            }

            // load the prototype (no values)
            Prototype = new Model();
            Prototype.LoadFromXml(xml, null, null, rtValuesVersion);

            // load the values for each item in the array 
            loadValues(values, overrides, rtValuesVersion);
        }

        private void loadValues(System.Xml.Linq.XElement values, IDictionary<string, string> overrides, int rtValuesVersion)
        {
            throw new NotImplementedException();
        }

    }
}
