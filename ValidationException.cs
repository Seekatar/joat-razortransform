using System;
using System.Collections.Generic;
using System.Text;

namespace RazorTransform
{
    internal class ValidationException : Exception
    {
        private List<Tuple<ITransformModelGroup,string>> _groups = new List<Tuple<ITransformModelGroup,string>>();

        public IList<Tuple<ITransformModelGroup,string>> Group { get { return _groups; } }
        public ITransformModelItem Item  { get; private set; }

        public ValidationException(string p, ITransformModelGroup g, String currentName = null ) : base(p)
        {
            _groups.Add(new Tuple<ITransformModelGroup,string>(g, currentName));
        }

        public ValidationException(string p, ITransformModelItem i)
            : base(p)
        {
            this.Item = i;
        }

        public override string ToString()
        {
            var sb = new StringBuilder("Validation exception "+Message+Environment.NewLine+"    ");
            _groups.Reverse();
            foreach (var g in _groups)
            {
                if ( String.IsNullOrWhiteSpace(g.Item2))
                    sb.Append(String.Format("-> {0}", g.Item1.DisplayName));
                else
                sb.Append(String.Format("-> {0}[{1}]", g.Item1.DisplayName, g.Item2 ));
            }
            if (Item != null)
            {
                sb.Append("-> " + Item.DisplayName);
            }
            return sb.ToString();
        }

        internal void AddGroup(ITransformModelGroup currentGroup, string currentName)
        {
            _groups.Add(new Tuple<ITransformModelGroup, string>(currentGroup, currentName));
        }
    }
}
