using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RazorTransform
{
    public class TransformModelArray : TransformModelItem
    {
        private List<string> _keyReplacements;

        public TransformModelArray(TransformModelArray src) : this(src, src.Value)
        {
        }

        public TransformModelArray(TransformModelArray src, string value ) : base(src,value)
        {
            KeyReplacements = new List<string>(src.KeyReplacements);
            Key = src.Key;
            ArrayValueName = src.ArrayValueName;
        }

        public TransformModelArray(XElement x, TransformModelItem parent = null)
        {
            loadFromXml(x);
            Parent = parent;
        }

        protected override void loadFromXml(XElement x)
        {
            base.loadFromXml(x);
            ArrayValueName = (string)x.Attribute(Constants.ArrayValueName);
            PropertyName = ArrayValueName;
        }

        public override void Load(XElement values, IDictionary<string, string> overrides, XElement objectXml)
        {
            loadArray(objectXml, values );
        }

        // load a group that is an array
        private void loadArray(XElement objectXml, XElement values)
        {
            var newOne = new TransformModelItem() { Parent = this, DisplayName = DisplayName, Description = Description, Type = Constants.Array };
            Children.Add(newOne);

            // add a 0th one for a template for "New" ones
            foreach (var y in objectXml.Elements().Where(n => n.Name == Constants.Item))
            {
                var i = new TransformModelItem(y, this);
                newOne.Children.Add(i);
            }
            // nested arrays
            foreach (var e in objectXml.Elements().Where(n => n.Name == Constants.Group))
            {
                var g = new TransformModelArray(e, Children[0]);
                newOne.Children.Add(g);
                g.loadArray( e, null);
            }

            setArrayValues(values, newOne);

            // figure out what the "key" is to show as the list
            makeKey(newOne, objectXml);
        }

        protected void makeKey(TransformModelItem newOne, XElement objectXml)
        {
            string key = (string)objectXml.Attribute(Constants.Key);
            var replacements = new List<string>();

            if (key != null)
            {
                // scan for each child name starting with the largest
                foreach (var c in newOne.Children.Select(o => o.PropertyName).OrderByDescending(o => o.Length))
                {
                    if (key.Contains(c))
                    {
                        key = key.Replace(c, String.Format("{{{0}}}", replacements.Count));
                        replacements.Add(c);
                    }
                }
            }
            else if (newOne.Children.Count > 0)
            {
                key = "{0}";
                replacements.Add(newOne.Children.First().PropertyName);
            }
        }


        protected void setArrayValues( XElement values, TransformModelItem arrayItem)
        {
            // load the values from the values file, if it exists
            if (values != null)
            {
                var myValues = values.Elements(Constants.Value).Where(o => o.Attribute(Constants.Name).Value == ArrayValueName);
                foreach (var mv in myValues)
                {
                    var nextOne = new TransformModelItem() { Parent = this, DisplayName = arrayItem.DisplayName };
                    Children.Add(nextOne);

                    foreach (var i in arrayItem.Children)
                    {
                        var childValues = mv.Elements().Where(n => n.Attribute(Constants.Name).Value == i.PropertyName);
                        if (!i.IsArray)
                        {
                            var v = childValues.SingleOrDefault();
                            if (v != null)
                            {
                                nextOne.Children.Add(new TransformModelItem(i, v.Attribute(Constants.Value).Value));
                            }
                            else
                            {
                                nextOne.Children.Add(new TransformModelItem(i, i.Value));
                            }
                        }
                        else
                        {
                            // array of items, get the array object off the arrayItem
                            var arrayNextOne = (arrayItem.Children.Where(o => o is TransformModelArray && (o as TransformModelArray).ArrayValueName == i.PropertyName).SingleOrDefault()) as TransformModelArray;
                            var nextArray = new TransformModelArray(arrayNextOne) { Parent = nextOne };
                            nextOne.Children.Add(nextArray);
                            nextArray.setArrayValues(mv, arrayNextOne.Children[0]);
                        }
                    }
                }
            }
        }

        public string ArrayValueName { get; set; }
        public string Key { get; set; }

        public List<string> KeyReplacements
        {
            get { return _keyReplacements; }
            set { _keyReplacements = value; }
        }
        
    }
}
