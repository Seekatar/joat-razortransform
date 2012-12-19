using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RazorTransform
{
    public class TransformModelArray : TransformModelGroup
    {
        private List<string> _keyReplacements = new List<string>();
        private List<TransformModelArrayItem> _items = new List<TransformModelArrayItem>();
        private List<TransformModelGroup> _prototypeGroups = new List<TransformModelGroup>();
        private XElement _element;

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

        public TransformModelArrayItem Prototype 
        {
            get { return new TransformModelArrayItem(PrototypeGroups) { Parent = this } ; } 
        }

        // get the items to show in the UI
        public override IEnumerable<TransformModelItem> Items
        {
            get { return _items;  }
        }

        public IList<TransformModelArrayItem> ArrayItems
        {
            get { return _items; }
        }

        public string ArrayValueName { get; set; }

        public string Key { get; set; }

        /// <summary>
        /// </summary>
        /// <param name="parent"></param>
        public TransformModelArray(TransformModelGroup parent)
            : base(parent)
        {
        }

        public TransformModelArray(TransformModelArray src)
        {
            _keyReplacements.AddRange(src._keyReplacements);
            _items.AddRange(src._items);
        }

        public TransformModelArray(TransformModelArray src, string value)
            : base(src)
        {
            if (src._keyReplacements == null)
                src._keyReplacements = new List<string>() { Children[0].PropertyName };

            _keyReplacements = new List<string>(src._keyReplacements);
            Key = src.Key;
            ArrayValueName = src.ArrayValueName;
        }

        public TransformModelArray(XElement x, TransformModelGroup parent = null)
            : base(parent)
        {
            loadFromXml(x);
        }

        public TransformModelArray()
        {
        }

        /// <summary>
        /// load this one item from the Xml
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="values"></param>
        /// <param name="overrides"></param>
        public override void LoadFromXml(XElement xml, XElement values, IDictionary<string, string> overrides)
        {
            _element = xml;

            // array-specific attributes
            ArrayValueName = (string)xml.Attribute(Constants.ArrayValueName);
            Min = (Int32?)xml.Attribute(Constants.Min) ?? 0;
            Max = (Int32?)xml.Attribute(Constants.Max) ?? Int32.MaxValue;

            // load the items into a new group
            var g = new TransformModelGroup();
            g.LoadFromXml(xml, null, null); // loads name, desc, the items that are our prototype
            PrototypeGroups.Clear();
            PrototypeGroups.Add(g);
            Children.Clear();

            // copy prototype group attribute to this
            DisplayName = g.DisplayName;
            Description = g.Description;
            Expanded = g.Expanded;

            // load nested arrays (groups)
            foreach (var x in xml.Elements(Constants.Group))
            {
                TransformModelGroup newOne = null;
                if (x.Attribute(Constants.ArrayValueName) != null)
                    newOne = new TransformModelArray();
                else
                    newOne = new TransformModelGroup();

                newOne.LoadFromXml(x, null, null);
                PrototypeGroups.Add(newOne);
            }

            makeKeyFormat(xml);

            // overrides not used in arrays -- for now
            loadArray(xml, values);
        }

        // load a group that is an array
        private void loadArray(XElement xml, XElement values)
        {
            // find all the values 
            foreach (var arrayItem in xml.Descendants(ArrayValueName))
            {
                var newOne = new TransformModelArrayItem();
                // TODO { Parent = this, DisplayName = DisplayName, Description = Description, Type = Constants.Array };

                newOne.LoadFromXml(_element, values, null);

                _items.Add(newOne);

                // nested arrays
                foreach (var e in xml.Elements().Where(n => n.Name == Constants.Group))
                {
                    var g = new TransformModelArray();
                    g.LoadFromXml(e, values, null);
                    newOne.Groups.Add(g);
                }

                setArrayValues(values, newOne);

                // figure out what the "key" is to show as the list
                newOne.DisplayName = makeKey(newOne);
            }

        }

        protected void setArrayValues(XElement values, TransformModelArrayItem arrayItem)
        {
            // load the values from the values file, if it exists
            if (values != null)
            {
                var myValues = values.Elements(Constants.Value).Where(o => o.Attribute(Constants.Name).Value == ArrayValueName);
                foreach (var mv in myValues)
                {
                    var nextOne = new TransformModelItem() { Parent = this, DisplayName = arrayItem.DisplayName };
                    Children.Add(nextOne);

                    foreach (var i in arrayItem.Items)
                    {
                    //    var childValues = mv.Elements().Where(n => n.Attribute(Constants.Name).Value == i.PropertyName);
                    //    if (!i.IsArray)
                    //    {
                    //        var v = childValues.SingleOrDefault();
                    //        if (v != null)
                    //        {
                    //            nextOne.Children.Add(new TransformModelItem(i, v.Attribute(Constants.Value).Value));
                    //        }
                    //        else
                    //        {
                    //            nextOne.Children.Add(new TransformModelItem(i, i.Value));
                    //        }
                    //    }
                    //    else
                    //    {
                    //        // array of items, get the array object off the arrayItem
                    //        var arrayNextOne = (arrayItem.Children.Where(o => o is TransformModelArray && (o as TransformModelArray).ArrayValueName == i.PropertyName).SingleOrDefault()) as TransformModelArray;
                    //        var nextArray = new TransformModelArray(arrayNextOne) { Parent = nextOne };
                    //        nextOne.Children.Add(nextArray);
                    //        nextArray.setArrayValues(mv, arrayNextOne.Children[0]);
                    //    }
                    }
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
                    values[j++] = i.Value;
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
            }
        }



    }
}
