using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RazorTransform
{
    public class TransformModelArrayItem : TransformModelItem
    {
        List<TransformModelGroup> _groups = new List<TransformModelGroup>();
        private TransformModelArrayItem orig;

        public TransformModelArrayItem(TransformModelArrayItem src) : base(src)
        {
            this.orig = src.orig;
            _groups.CopyValueFrom(src.Groups);
        }

        public TransformModelArrayItem()
        {
        }

        public TransformModelArrayItem( IList<TransformModelGroup> groups )
        {
            _groups.CopyValueFrom(groups);
        }

        /// <summary>
        /// get the parent object cast as TransformModelArray
        /// </summary>
        public TransformModelArray ArrayParent 
        {
            get { return Parent as TransformModelArray; } 
        }

        public List<TransformModelGroup> Groups { get { return _groups; } }

        /// <summary>
        /// all the items in the groups, flattened out to make it 
        /// easier to get to them 
        /// </summary>
        public IEnumerable<ITransformModelItem> Items 
        { 
            get 
            {
                var ret = new List<ITransformModelItem>();
                foreach (var g in _groups)
                {
                    ret.AddRange(g.Items);
                }
                return ret;
            } 
        }

        public override void LoadFromXml(XElement xml, XElement values, IDictionary<string, string> overrides)
        {
            
        }

        internal void MakeKey()
        {
            DisplayName = ArrayParent.makeKey(this);
        }
    }
}
