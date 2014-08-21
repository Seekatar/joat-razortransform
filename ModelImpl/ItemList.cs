using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RazorTransform.Model
{
    class ItemList : System.Dynamic.DynamicObject, IItemList
    {
        private List<IModel> _models = new List<IModel>();

        public ItemList(IItemList src)
        {
            throw new NotImplementedException();
        }

        public ItemList(IModel parent, IGroup group)
        {
            Parent = parent;
            Group = group;
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

        public string KeyFormat
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

        public UInt16 Min
        {
            get;
            set;
        }

        public UInt16 Max
        {
            get;
            set;
        }

        public string DisplayName
        {
            get;
            set;
        }

        public bool Hidden
        {
            get;
            set;
        }

        public IList<string> VisibilityGroups
        {
            get;
            private set;
        }

        #endregion

        #region IItemList methods
        /// <summary>
        /// Make the key name for a given model
        /// </summary>
        /// <param name="model">the model that is part of the list</param>
        /// <returns>the display name for the UI, or &lt;Unknown&gt; if an error occurred</returns>
        public string ModelKeyName(IModel model)
        {
            // var ret = "<Unknown>";
            throw new NotImplementedException();
        }

        /// <summary>
        /// validate the model.
        /// </summary>
        /// <param name="errors">collection to be populated</param>
        public void Validate(ICollection<ValidationError> errors)
        {
            if (Count < Min)
            {
                errors.Add(new ValidationError(String.Format(Resource.MinCount, DisplayName, Min), Group));
            }
            else if (Count > Max)
            {
                errors.Add(new ValidationError(string.Format(Resource.MaxCount, DisplayName, Max), Group));
            }
            if (Unique)
            {
                for (int j = 0; j < (Count - 1); j++)
                {
                    if (this.Skip(j + 1).Any(o => ModelKeyName(o).Equals(ModelKeyName(this.ElementAt(j)))))
                    {
                        errors.Add(new ValidationError(String.Format(Resource.UniqueViolation, ModelKeyName(this.ElementAt(j))), this.Group));
                    }
                }
            }

        }

        /// <summary>
        /// Generate the RtValues XML adding it under the root XML passed in
        /// </summary>
        /// <param name="root"></param>
        public void GenerateXml(XElement root)
        {
            var x = new XElement(Name);
            root.Add(x);
            foreach( var m in this )
            {
                m.GenerateXml(root);
            }
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
            if (Sort != RtSort.None)
            {
                int i = 0;
                for (; i < _models.Count; i++)
                {
                    var key = ModelKeyName(item);
                    if ((Sort == RtSort.Ascending && String.Compare(ModelKeyName(_models[i]), key) >= 0) || // ascending
                                                      String.Compare(ModelKeyName(_models[i]), key) <= 0)    // descending
                        break;
                }
                _models.Insert(i, item);
            }
            else
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
            _models.CopyTo(array, arrayIndex);
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
            Name = (string)xml.Attribute(Constants.ArrayValueName) ?? String.Empty;
            KeyFormat = (string)xml.Attribute(Constants.Key) ?? String.Empty;
            Min = (UInt16)((UInt32?)xml.Attribute(Constants.Min) ?? (UInt32)UInt16.MinValue);
            Max = (UInt16)((UInt32?)xml.Attribute(Constants.Max) ?? (UInt32)UInt16.MaxValue);
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

        /// <summary>
        /// deep copy the array
        /// </summary>
        /// <param name="groupTo"></param>
        /// <param name="groupFrom"></param>
        public void CopyValueFrom(IItemList groupFrom)
        {
            if (!Type.ReferenceEquals(this, groupFrom))
            {
                Clear();
                foreach (var g in groupFrom)
                {
                    var newOne = (IModel)Activator.CreateInstance(g.GetType(), g);
                    Add(newOne);
                }
            }
        }


        private void loadValues(System.Xml.Linq.XElement values, IDictionary<string, string> overrides, int rtValuesVersion)
        {
            throw new NotImplementedException();
        }

    }
}
