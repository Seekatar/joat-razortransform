using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace RazorTransform
{
    public class TransformModelGroup : System.Dynamic.DynamicObject, RazorTransform.ITransformModelGroup
    {
        protected List<ITransformModelItem> _children = new List<ITransformModelItem>();

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

        /// <summary>
        /// Current sort state for group
        /// </summary>
        public bool Sort { get; set; }

        /// <summary>
        /// Should display names be unique
        /// </summary>
        public bool Unique { get; set; }

        public List<ITransformModelItem> Children
        {
            get { return _children; }
            protected set { _children = value; }
        }

        public virtual IEnumerable<ITransformModelItem> Items
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
            Children = new List<ITransformModelItem>();
            Expanded = src.Expanded;
            Hidden = src.Hidden;
            Children.AddRange(src.Children.Select(o => 
                            { 
                                var ret = (TransformModelItem)Activator.CreateInstance(o.GetType(), o); 
                                ret.Group = this; 
                                return ret; 
                            }));
        }

        /// <summary>
        /// default constructor
        /// </summary>
        internal TransformModelGroup()
        {
        }

        /// <summary>
        /// given a group Element, load it from the Xml, and all the children
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="values"></param>
        /// <param name="overrides"></param>
        public virtual void LoadFromXml(XElement xml, XElement values, IDictionary<string, string> overrides, int rtValuesVersion )
        {
            // attributes
            DisplayName = (String)xml.Attribute(Constants.Name) ?? "<no name>";
            Description = (String)xml.Attribute(Constants.Description) ?? String.Empty;
            Expanded = (bool?)xml.Attribute(Constants.Expanded) ?? false;
            Hidden = (bool?)xml.Attribute(Constants.Hidden) ?? false;
            Sort = (bool?)xml.Attribute(Constants.Sort) ?? false;
            Unique = (bool?)xml.Attribute(Constants.Unique) ?? false;
            
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

                    i.LoadFromXml(e, values, overrides, rtValuesVersion);

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
