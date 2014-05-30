using System.Collections.Generic;
using System.Dynamic;
using System.Xml.Linq;
using System.Linq;
using System;

namespace RazorTransform
{
    public class TransformModelArrayItem : TransformModelItem
    {
        List<TransformModelGroup> _groups = new List<TransformModelGroup>();
        private TransformModelArrayItem orig;

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            var list = Groups.SelectMany(o => o.Children.Where(p => p.PropertyName == binder.Name));
            if (list != null)
            {
                var result = list.FirstOrDefault();
                if (result is TransformModelItem)
                {
                    var item = (result as TransformModelItem);
                    item.ExpandedValue = value.ToString();
                    return true;
                }
            }
            return false;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            bool ret = false;
            result = null;

            if ( binder.Name == "Root")
            {
                result = TransformModel.Instance; 
                return true;
            }
            if (binder.Name == "Parent")
            {
                if (Parent != null)
                    result = Parent;
                else
                    result = TransformModel.Instance;
                return true;
            }

            // find in all the groups, an item or array with this name
            var list = Groups.SelectMany(o => o.Children.Where(p => p.PropertyName == binder.Name));
            var arrays = Groups.Where(o => o is TransformModelArray && (o as TransformModelArray).ArrayValueName == binder.Name).SingleOrDefault();
            if (arrays != null)
            {
                ret = true;
                result = arrays;
            }
            else if (list != null)
            {
                result = list.FirstOrDefault();
                ret = true;
                if (result is TransformModelItem)
                {
                    var item = (result as TransformModelItem);
                    if (item.ExpandedValue != null)
                        result = item.ExpandedValue;
                    else 
                        result = item.Value;

                    if (item.Type == RtType.Bool)
                        result = item.GetValue<bool>();
                    else if ( item.Type == RtType.Int32)
                        result = item.GetValue<Int32>();
                }
                else if (!(result is TransformModelArray))
                {
                    if (result != null)
                        throw new Exception("Bad type found for item " + binder.Name + " " + result.GetType().Name);
                    else
                        System.Diagnostics.Debug.WriteLine("Null result for " + binder.Name);
                }
            }
            return ret;
        }

        object _parent;
        public object Parent { get { return _parent ?? TransformModel.Instance; } set { _parent = value; } }

        public TransformModelArrayItem(TransformModelArrayItem src) : this(src,null)
        {
            
        }

        public TransformModelArrayItem(TransformModelArrayItem src, TransformModelArrayItem parent = null ) : base(src)
        {
            if (parent != null)
                this.Parent = parent;
            else
                this.Parent = src.Parent;
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
        public TransformModelArray ArrayGroup 
        {
            get { return Group as TransformModelArray; } 
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

        internal void MakeKey()
        {
            DisplayName = ArrayGroup.makeKey(this);
        }
    }
}
