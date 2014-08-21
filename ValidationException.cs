using RazorTransform.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace RazorTransform
{
    /// <summary>
    /// class for capturing multiple validation exceptions to display to user
    /// </summary>
    public class ValidationError
    {
        private List<Tuple<IGroup,string>> _groups = new List<Tuple<IGroup,string>>();

        public IList<Tuple<IGroup,string>> Group { get { return _groups; } }
        public IItem Item  { get; private set; }
        public string Message { get; private set; }

        public ValidationError(string p, IGroup g, String currentName = null ) 
        {
            Message = p;
            _groups.Add(new Tuple<IGroup,string>(g, currentName));
        }

        public ValidationError(string p, IItem i)
        {
            Message = p;
            this.Item = i;
        }

        public override string ToString()
        {
            var sb = new StringBuilder(Message+Environment.NewLine+"    ");
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

        internal void AddGroup(IGroup currentGroup, string currentName)
        {
            _groups.Add(new Tuple<IGroup, string>(currentGroup, currentName));
        }
    }
}
