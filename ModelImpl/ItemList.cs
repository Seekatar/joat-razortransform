using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RazorTransform.Model
{
    class ItemList : IItemList
    {
        private System.Xml.Linq.XElement i;
        private Model model;
        private int rtValuesVersion;

        public ItemList(IModel parent)
        {
            Parent = model;
        }

        public IModel ProtoType
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
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

        public int Min
        {
            get;
            set;
        }

        public int Max
        {
            get;
            set;
        }

        public IGroup Group
        {
            get { throw new NotImplementedException(); }
        }

        public IModel Parent
        {
            get;
            private set;
        }

        public int IndexOf(IModel item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, IModel item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        #region IList Implementation
        public IModel this[int index]
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public void Add(IModel item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(IModel item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(IModel[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsReadOnly
        {
            get { throw new NotImplementedException(); }
        }

        public bool Remove(IModel item)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<IModel> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
        #endregion


        uint IItemList.Min
        {
            get { throw new NotImplementedException(); }
        }

        uint IItemList.Max
        {
            get { throw new NotImplementedException(); }
        }


        public string DisplayName
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public void LoadFromXml(System.Xml.Linq.XElement xml, System.Xml.Linq.XElement values, IDictionary<string, string> overrides, int rtValuesVersion)
        {
            throw new NotImplementedException();
        }
    }
}
