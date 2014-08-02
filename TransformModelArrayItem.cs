using System.Collections.Generic;
using System.Dynamic;
using System.Xml.Linq;
using System.Linq;
using System;

namespace RazorTransform
{
    public class TransformModelArrayItem : TransformModelItem
    {
        private List<TransformModelGroup> _groups = new List<TransformModelGroup>();
        private TransformModelArrayItem orig;
        private object _parent;

        #region DynamicObject Overrides
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            return TrySetMemberFn(binder, value, Groups);
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            return TryGetMemberFn(binder, out result, Parent, Groups);
        }
    
        internal static bool TrySetMemberFn(SetMemberBinder binder, object value, IList<TransformModelGroup> groups)
        {
            var list = groups.SelectMany(o => o.Children.Where(p => p.PropertyName == binder.Name));
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

        internal static bool TryGetMemberFn(GetMemberBinder binder, out object result, object parent, IList<TransformModelGroup> groups)
        {
            return TryGetMemberFn(binder.Name, out result, parent, groups);
        }

        internal static bool TryGetMemberFn(string name, out object result, object parent, IList<TransformModelGroup> groups)
        {
            bool ret = false;
            result = null;

            if ( name == Constants.Root)
            {
                result = TransformModel.Instance; 
                return true;
            }
            if (name == Constants.Parent)
            {
                if (parent != null)
                    result = parent;
                else
                    result = TransformModel.Instance;
                return true;
            }

            // find in all the groups, an item or array with this name
            var list = groups.SelectMany(o => o.Children.Where(p => p.PropertyName == name));
            var arrays = groups.Where(o => o is TransformModelArray && (o as TransformModelArray).ArrayValueName == name).SingleOrDefault();
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
                        throw new Exception("Bad type found for item " + name + " " + result.GetType().Name);
                    else
                        System.Diagnostics.Debug.WriteLine("Null result for " + name);
                }
            }
            return ret;
        }
        #endregion

        /// <summary>
        /// Parent property so can be used in Razor.  e.g. @Model.Parent.blah
        /// </summary>
        public object Parent { get { return _parent ?? TransformModel.Instance; } set { _parent = value; } }

        #region constructors
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
            foreach ( var a in _groups.Where( o => o is TransformModelArray ).Select( o => o as TransformModelArray))
            {
                a.Parent = this;
            }
        }

        public TransformModelArrayItem()
        {
        }

        public TransformModelArrayItem( IList<TransformModelGroup> groups )
        {
            _groups.CopyValueFrom(groups);
        }
        #endregion

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

        /// <summary>
        /// create the "key" for the item, used in list display and uniquness
        /// </summary>
        internal void MakeKey()
        {
            DisplayName = ArrayGroup.makeKey(this);
        }
    }
}
