using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RazorTransform
{
    public class TransformModelGroup
    {
        protected List<TransformModelItem> _children = new List<TransformModelItem>();

        /// <summary>
        /// name to show in the UI
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// tool tip
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Current expanded state
        /// </summary>
        public bool Expanded { get; set; }

        /// <summary>
        /// Current hidden state
        /// </summary>
        public bool Hidden { get; set; }

        public List<TransformModelItem> Children
        {
            get { return _children; }
            protected set { _children = value; }
        }

        public virtual IEnumerable<TransformModelItem> Items
        {
            get { return _children; }
        }

        /// <summary>
        /// deep copy constructor
        /// </summary>
        /// <param name="src"></param>
        public TransformModelGroup(TransformModelGroup src)
        {
            DisplayName = src.DisplayName;
            Description = src.Description;
            Children = new List<TransformModelItem>();
            Expanded = src.Expanded;
            Children.AddRange(src.Children.Select(o => 
                            { 
                                var ret = (TransformModelItem)Activator.CreateInstance(o.GetType(), o); 
                                ret.Parent = this; 
                                return ret; 
                            }));
        }

        /// <summary>
        /// default constructor
        /// </summary>
        public TransformModelGroup()
        {
        }

        /// <summary>
        /// given a group Element, load it from the Xml, and all the children
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="values"></param>
        /// <param name="overrides"></param>
        public virtual void LoadFromXml(XElement xml, XElement values, IDictionary<string, string> overrides)
        {
            // attributes
            DisplayName = (String)xml.Attribute(Constants.Name) ?? "<no name>";
            Description = (String)xml.Attribute(Constants.Description) ?? String.Empty;
            Expanded = (bool?)xml.Attribute(Constants.Expanded) ?? false;
            Hidden = (bool?)xml.Attribute(Constants.Hidden) ?? false;

            // elements
            // regular group can only have items
            foreach (var e in xml.Elements())
            {
                if (e.Name == Constants.Item)
                {
                    TransformModelItem i = null;

                    var typeName = (String)e.Attribute(Constants.Type) ?? String.Empty;
                    var type = Constants.MapType(typeName);

                    if (type == RtType.Password)
                        i = new PasswordTransformModelItem(this);
                    else if (type == RtType.Custom)
                        i = TransformModel.Instance.Customs[typeName].CreateItem(this,e);
                    else
                        i = new TransformModelItem(this);

                    i.LoadFromXml(e, values, overrides);

                    Children.Add(i);
                }
            }
        }

        /// <summary>
        /// load the group from Xml
        /// </summary>
        /// <param name="x"></param>
        protected virtual void loadFromXml(XElement x)
        {
            // if no display name, use propertyname
            DisplayName = (string)x.Attribute(Constants.DisplayName) ?? "<no name>";

            Description = (string)x.Attribute(Constants.Description) ?? DisplayName;

            Expanded = (bool?)x.Attribute(Constants.Expanded) ?? false;


        }



    }
}
