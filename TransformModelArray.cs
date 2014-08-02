using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace RazorTransform
{
    public class TransformModelArray : TransformModelGroup, IEnumerable<ITransformModelItem>
    {
        private List<string> _keyReplacements = new List<string>();
        private List<TransformModelArrayItem> _items = new List<TransformModelArrayItem>();
        private List<TransformModelGroup> _prototypeGroups = new List<TransformModelGroup>();
        private XElement _element;

        public override bool TryGetIndex(System.Dynamic.GetIndexBinder binder, object[] indexes, out object result)
        {
            result = null;
            int index = (int)indexes[0];
            if (index < 0 || index >= _items.Count)
                throw new IndexOutOfRangeException();
            else
            {
                result = _items[index];
                return true;
            }
        }

        /// <summary>
        /// min items in array
        /// </summary>
        public Int32 Min { get; set; }

        /// <summary>
        /// max items in array
        /// </summary>
        public Int32 Max { get; set; }

        /// <summary>
        /// list of the groups in the array
        /// </summary>
        public List<TransformModelGroup> PrototypeGroups
        {
            get { return _prototypeGroups; }
        }

        /// <summary>
        /// create a new array item object from the prototype object
        /// </summary>
        public TransformModelArrayItem CreatePrototype 
        {
            get 
            { 
                return new TransformModelArrayItem(PrototypeGroups) { Group = this, Parent = this.Parent } ; 
            } 
        }

        /// <summary>
        /// get the items to show in the UI 
        /// </summary>
        public override IEnumerable<ITransformModelItem> Items
        {
            get { return _items;  }
        }

        /// <summary>
        /// items in the array as ArrayItems
        /// </summary>
        public IList<TransformModelArrayItem> ArrayItems
        {
            get { return _items; }
        }

        /// <summary>
        /// member name of the array
        /// </summary>
        public string ArrayValueName { get; set; }

        /// <summary>
        /// the string template for displaying in the list
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// default constructor from group
        /// </summary>
        /// <param name="parent"></param>
        public TransformModelArray(TransformModelGroup parent)
            : base(parent)
        {
        }

        /// <summary>
        /// deep copy constructor
        /// </summary>
        /// <param name="src">copy from</param>
        public TransformModelArray(TransformModelArray src) : base( src )
        {
            _element = src._element;
            _keyReplacements.AddRange(src._keyReplacements);
            _items.AddRange(src._items.Select(o =>
            {
                var ret = (TransformModelArrayItem)Activator.CreateInstance(o.GetType(), o);
                ret.Group = this;
                return ret;
            }));
            _prototypeGroups = src._prototypeGroups;
            Min = src.Min;
            Max = src.Max;
            Key = src.Key;
            ArrayValueName = src.ArrayValueName;
        }

        /// <summary>
        /// constructor from XML
        /// </summary>
        /// <param name="x"></param>
        /// <param name="parent"></param>
        public TransformModelArray(XElement x, TransformModelGroup parent = null)
            : base(parent)
        {
            loadFromXml(x);
        }

        /// <summary>
        /// default constructor
        /// </summary>
        public TransformModelArray() 
        {
        }

        /// <summary>
        /// load this one item from the Xml
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="values"></param>
        /// <param name="overrides"></param>
        public override void LoadFromXml(XElement xml, XElement values, IDictionary<string, string> overrides, int rtValuesVersion)
        {
            _element = xml;

            // array-specific attributes
            ArrayValueName = (string)xml.Attribute(Constants.ArrayValueName);
            TransformModel.Instance.Arrays.Add(this);

            Min = (Int32?)xml.Attribute(Constants.Min) ?? 0;
            Max = (Int32?)xml.Attribute(Constants.Max) ?? Int32.MaxValue;

            PrototypeGroups.Clear();
            Children.Clear();

            // load the loose items directly under item into a new group, if any
            var g = new TransformModelGroup();
            g.LoadFromXml(xml, null, null,rtValuesVersion); // also loads name, desc, the items that are our prototype
            if ( g.Children.Count > 0 )
                PrototypeGroups.Add(g);

            // copy prototype group attributes to this
            DisplayName = g.DisplayName;
            Description = g.Description;
            Expanded = g.Expanded;
            Hidden = g.Hidden;
            Sort = g.Sort;
            Unique = g.Unique;

            // load nested arrays (groups)
            foreach (var x in xml.Elements(Constants.Group))
            {
                TransformModelGroup newOne = null;
                if (x.Attribute(Constants.ArrayValueName) != null)
                    newOne = new TransformModelArray();
                else
                    newOne = new TransformModelGroup();

                // this allows for recursive definitions
                var existingOne = TransformModel.Instance.Arrays.FirstOrDefault(o => o.ArrayValueName == (string)x.Attribute(Constants.ArrayValueName));

                newOne.LoadFromXml(x, null, null,rtValuesVersion);

                if (existingOne != null && newOne is TransformModelArray)
                {
                    // if reusing same prototype, set it and make the key
                    var nested = newOne as TransformModelArray;
                    nested._prototypeGroups = existingOne.PrototypeGroups;
                    nested.makeKeyFormat(x);
                }

                PrototypeGroups.Add(newOne);
            }

            foreach ( var n in xml.Elements(Constants.NestedGroup))
            {
                var ng = TransformModel.Instance.Groups.SingleOrDefault(o => (o is TransformModelArray) && (o as TransformModelArray).ArrayValueName == n.Attribute(Constants.GroupName).Value);
                if ( ng != null )
                {
                    PrototypeGroups.Add(ng);
                }
                else
                {
                    throw new Exception("Missing group named " + n.Attribute(Constants.NestedGroup).Value);
                }
            }

            makeKeyFormat(xml);

            // overrides not used in arrays -- for now
            setArrayValues(values,rtValuesVersion);
        }

        protected void setArrayValues(XElement values,int rtValuesVersion,TransformModelArrayItem parent = null)
        {
            // load the values from the values file, if it exists
            if (values != null)
            {
                // search for nested value is something like ./value[@name="itemA"]
                string valuesXPath;
                if ( rtValuesVersion == 1 )
                    valuesXPath = String.Format("./value[@name=\"{0}\"]", ArrayValueName);
                else
                    valuesXPath = String.Format("./{0}", ArrayValueName);

                var myValues = values.XPathSelectElements(valuesXPath); 
                foreach (var mv in myValues)
                {
                    var nextOne = new TransformModelArrayItem(CreatePrototype,parent);
                    foreach (var g in nextOne.Groups)
                    {
                        if (g is TransformModelArray)
                        {
                            (g as TransformModelArray).setArrayValues(mv,rtValuesVersion,nextOne);
                        }
                        else if (g is TransformModelGroup)
                        {
                            foreach (var i in nextOne.Items)
                            {
                                (i as TransformModelItem).SetItemValue(mv,null,rtValuesVersion);
                            }
                        }
                    }
                    nextOne.MakeKey();
                    AddArrayItem(nextOne);
                }
            }
        }

        /// <summary>
        ///  make the display name for each array item
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public string makeKey(TransformModelArrayItem item)
        {
            var s = "<unknown>";

            var values = new string[_keyReplacements.Count];
            int j = 0;
            foreach (var k in _keyReplacements)
            {
                // find it in the item's values
                var i = item.Items.Where(o => o.PropertyName == k).SingleOrDefault();
                if (i == null)
                    return s;
                else
                {
                    if ( String.IsNullOrEmpty(i.ExpandedValue))
                        values[j++] = i.Value;
                    else
                        values[j++] = i.ExpandedValue;
                }
            }
            if (String.IsNullOrWhiteSpace(Key))
            {
                Key = String.Empty;
                for (int i = 0; i < values.Count(); i++)
                    Key += String.Format("{{{0}}}", i);
            }
            s = String.Format(Key, values);
            
            return s;
        }

        /// <summary>
        /// read the Xml to figure out how to build the display name for each array item
        /// </summary>
        /// <param name="objectXml"></param>
        protected void makeKeyFormat(XElement objectXml)
        {
            string key = (string)objectXml.Attribute(Constants.Key);
            _keyReplacements.Clear();

            if (PrototypeGroups.Count > 0)
            {
                if (key != null)
                {
                    // scan for each child name starting with the largest
                    foreach (var c in PrototypeGroups.First().Children.Select(o => o.PropertyName).OrderByDescending(o => o.Length))
                    {
                        if (key.Contains(c))
                        {
                            key = key.Replace(c, String.Format("{{{0}}}", _keyReplacements.Count));
                            _keyReplacements.Add(c);
                        }
                    }
                }
                else if (PrototypeGroups.First().Items.Count() > 0)
                {
                    key = "{0}";
                    _keyReplacements.Add(PrototypeGroups.First().Items.First().PropertyName);
                }
                Key = key;
            }
        }

        internal void AddArrayItem(TransformModelArrayItem item)
        {
            if (Sort)
            {
                int i = 0;
                for (; i < ArrayItems.Count; i++)
                {
                    if (String.Compare(ArrayItems[i].DisplayName, item.DisplayName) >= 0)
                        break;
                }
                ArrayItems.Insert(i, item);
            }
            else
                ArrayItems.Add(item);
        }

        public int Count { get { return ArrayItems.Count;  } }

        public IEnumerator<ITransformModelItem> GetEnumerator()
        {
            return ArrayItems.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ArrayItems.GetEnumerator();
        }

        public TransformModelArrayItem Parent { get; set; }
    }
}
