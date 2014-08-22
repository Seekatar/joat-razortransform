using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace RazorTransform.Model
{
    class ItemList : System.Dynamic.DynamicObject, IItemList
    {
        private List<string> _keyReplacements = new List<string>();
        private List<IModel> _models = new List<IModel>();

        public ItemList(IItemList src, Model parent = null)
        {
            Name = src.Name;
            Description = src.Description;
            Prototype = src.Prototype;
            Group = src.Group;
            KeyFormat = src.KeyFormat;
            Sort = src.Sort;
            Group = src.Group;
            Unique = src.Unique;
            Min = src.Min;
            Max = src.Max;
            DisplayName = src.DisplayName;
            Hidden = src.Hidden;
            VisibilityGroups = src.VisibilityGroups;

            Parent = parent ?? src.Parent;
            
            CopyValueFrom(src, parent);
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
            var s = "<unknown>";

            var values = new string[_keyReplacements.Count];
            int j = 0;
            foreach (var k in _keyReplacements)
            {
                // find it in the item's values
                var i = model.Items.Where(o => o.Name == k).OfType<IItem>().SingleOrDefault();
                if (i == null)
                    return s;
                else
                {
                    if (String.IsNullOrEmpty(i.ExpandedValueStr))
                        values[j++] = i.ValueStr;
                    else
                        values[j++] = i.ExpandedValueStr;
                }
            }

            // don't have a specified format
            if (String.IsNullOrWhiteSpace(KeyFormat))
            {
                KeyFormat = String.Empty;
                for (int i = 0; i < values.Count(); i++)
                    KeyFormat += String.Format("{{{0}}}", i);
            }
            s = String.Format(KeyFormat, values);

            return s;
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


        public void LoadFromXml(System.Xml.Linq.XElement xml, System.Xml.Linq.XElement values, IDictionary<string, string> overrides)
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
            Prototype = new Model(Parent);
            Prototype.LoadFromXml(xml, null, null);

            makeKeyFormat(xml); // grab the key format from the XML

            // load the values for each item in the array 
            LoadValuesFromXml(values, overrides);
        }

        /// <summary>
        /// load the values (array items) from the XML
        /// </summary>
        /// <param name="values"></param>
        /// <param name="overrides"></param>
        public void LoadValuesFromXml(XElement values, IDictionary<string, string> overrides)
        {
            // load the values from the values file, if it exists
            if (values != null)
            {
                // search for nested value is something like ./value[@name="itemA"]
                string valuesXPath;
                if (Settings.Instance.RtValuesVersion == 1)
                    valuesXPath = String.Format("./value[@name=\"{0}\"]", Name);
                else
                    valuesXPath = String.Format("./{0}", Name);

                var myValues = values.XPathSelectElements(valuesXPath);
                foreach (var mv in myValues)
                {
                    var nextOne = new Model(Prototype, Parent);

                    foreach (var g in nextOne.Items)
                    {
                        g.LoadValuesFromXml(mv, overrides);
                    }
                    Add(nextOne);
                }
            }
        }

        /// <summary>
        /// deep copy the array
        /// </summary>
        /// <param name="fromList"></param>
        public void CopyValueFrom(IItemList fromList, IModel parent = null)
        {
            if (!Type.ReferenceEquals(this, fromList))
            {
                Clear();
                foreach (var g in fromList)
                {
                    var newOne = (IModel)Activator.CreateInstance(g.GetType(), g, parent);
                    Add(newOne);
                }
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

        /// <summary>
        /// read the Xml to figure out how to build the display name for each array item
        /// </summary>
        /// <param name="objectXml"></param>
        void makeKeyFormat(XElement objectXml)
        {
            string key = (string)objectXml.Attribute(Constants.Key);
            _keyReplacements.Clear();

            if (Count > 0)
            {
                if (key != null)
                {
                    // scan for each child name starting with the largest
                    foreach (var c in this.First().Items.Select(o => o.Name).OrderByDescending(o => o.Length))
                    {
                        if (key.Contains(c))
                        {
                            key = key.Replace(c, String.Format("{{{0}}}", _keyReplacements.Count));
                            _keyReplacements.Add(c);
                        }
                    }
                }
                else if (this.First().Items.Count() > 0)
                {
                    key = "{0}";
                    _keyReplacements.Add(this.First().Items.First().Name);
                }
                KeyFormat = key;
            }
        }
    }
}
