using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

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

        /// <summary>
        /// create a new array item object from the prototype object
        /// </summary>
        public TransformModelArrayItem CreatePrototype 
        {
            get { return new TransformModelArrayItem(PrototypeGroups) { Parent = this } ; } 
        }

        /// <summary>
        /// get the items to show in the UI 
        /// </summary>
        public override IEnumerable<TransformModelItem> Items
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
            _items.AddRange(src._items);
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
        public override void LoadFromXml(XElement xml, XElement values, IDictionary<string, string> overrides, string valuesXPath = "/RtValues")
        {
            _element = xml;

            // array-specific attributes
            ArrayValueName = (string)xml.Attribute(Constants.ArrayValueName);
            Min = (Int32?)xml.Attribute(Constants.Min) ?? 0;
            Max = (Int32?)xml.Attribute(Constants.Max) ?? Int32.MaxValue;

            PrototypeGroups.Clear();
            Children.Clear();

            // load the loose items directly under item into a new group, if any
            var g = new TransformModelGroup();
            g.LoadFromXml(xml, null, null); // also loads name, desc, the items that are our prototype
            if ( g.Children.Count > 0 )
                PrototypeGroups.Add(g);

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

                newOne.LoadFromXml(x, values, overrides, String.Join("/", valuesXPath, String.Format( "value[@name=\"{0}\"]",ArrayValueName )));
                PrototypeGroups.Add(newOne);
            }

            makeKeyFormat(xml);

            // overrides not used in arrays -- for now
            setArrayValues(values, valuesXPath);
        }

        protected void setArrayValues(XElement values, string valuesXPath)
        {
            // load the values from the values file, if it exists
            if (values != null)
            {
                // search for nested value is something like /RtValues/value[@name="itemA"]/value[@name="itemAB"]
                var myValues = values.XPathSelectElements(String.Join("/", valuesXPath, String.Format("value[@name=\"{0}\"]", ArrayValueName))); // Elements(Constants.Value).Where(o => o.Attribute(Constants.Name).Value == ArrayValueName);
                foreach (var mv in myValues)
                {
                    var nextOne = new TransformModelArrayItem(CreatePrototype);
                    ArrayItems.Add(nextOne);

                    foreach (var i in nextOne.Items)
                    {
                        var childValues = mv.Elements().Where(n => n.Attribute(Constants.Name).Value == i.PropertyName);

                        //if (!i.IsArray)
                        //{
                        var v = childValues.SingleOrDefault();
                        if (v != null)
                        {
                            i.Value = v.Attribute(Constants.Value).Value;
                        }
                        //}
                        //else
                        //{
                        //    // array of items, get the array object off the arrayItem
                        //    var arrayNextOne = (Children.Where(o => o is TransformModelArray && (o as TransformModelArray).ArrayValueName == i.PropertyName).SingleOrDefault()) as TransformModelArray;
                        //    var nextArray = new TransformModelArray(arrayNextOne) { Parent = nextOne };
                        //    nextOne.Children.Add(nextArray);
                        //    nextArray.setArrayValues(mv, arrayNextOne.Children[0]);
                        //}
                    }
                    nextOne.MakeKey();
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



    }
}
