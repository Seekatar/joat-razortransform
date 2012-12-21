using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace RazorTransform
{
    public static class CollectionExtensions
    {
        public static void LoadFromXml(this Dictionary<string, string> dict, XElement element)
        {
            foreach (var n in element.Nodes().Where(n => { if (n is XElement) return (n as XElement).Name == Constants.Value; else return false; }))
            {
                var x = n as XElement;
                var key = x.Attribute(Constants.Key);
                var value = x.Attribute(Constants.Value);
                if (key == null || value == null)
                    throw new ArgumentNullException("Enums values must have key and value");
                dict.Add(key.Value, value.Value);
            }
        }

        public static IEnumerable<TransformModelItem> FindRecursive(this IEnumerable<TransformModelGroup> info, Predicate<TransformModelItem> criteria)
        {
            var results = new List<TransformModelItem>();
            findModelItems(info, criteria, results);
            return results;
        }

        private static void findModelItems(IEnumerable<TransformModelGroup> parent, Predicate<TransformModelItem> criteria, List<TransformModelItem> results)
        {
            foreach (var i in parent)
            {
                if (i.Children != null)
                {
                    foreach (var c in i.Children)
                    {
                        if (criteria(c))
                        {
                            results.Add(c);
                        }
                    }
                    // TODO
                    // findModelItems(i.Children, criteria, results );
                }
            }
        }


        public static List<KeyValuePair<TransformModelItem, List<TransformModelItem>>> GroupByType(this IEnumerable<TransformModelItem> info)
        {
            var list = info.ToList();

            List<KeyValuePair<TransformModelItem, List<TransformModelItem>>> grouping = new List<KeyValuePair<TransformModelItem, List<TransformModelItem>>>();
            var types = list.Where(x => x.Type == RtType.Array);

            List<int> indexList = new List<int>();
            foreach (var type in types)
            {
                KeyValuePair<TransformModelItem, List<TransformModelItem>> kvp = new KeyValuePair<TransformModelItem, List<TransformModelItem>>(type, new List<TransformModelItem>());
                //find the index in the collection of type and select all subsequent items until another "Label" is found or end of the collection is found.
                int index = list.FindIndex((ci) => ci.DisplayName == type.DisplayName);
                indexList.Add(index);

                grouping.Add(kvp);


            }
            for (int k = 0; k < grouping.Count; k++)
            {
                var kvp = grouping[k];
                int index = indexList[k];

                if (k + 1 < indexList.Count)
                {
                    TransformModelItem[] sub = new TransformModelItem[(indexList[k + 1] - index) - 1];
                    //remove the first item which should be the label.
                    list.CopyTo(index + 1, sub, 0, (indexList[k + 1] - index) - 1);
                    kvp.Value.AddRange(sub);
                }
                else
                {
                    TransformModelItem[] sub = new TransformModelItem[(list.Count - index) - 1];
                    //remove the first item which should be the label.
                    list.CopyTo(index + 1, sub, 0, (list.Count - index) - 1);
                    kvp.Value.AddRange(sub);
                }

            }
            if (!grouping.Any())
            {
                grouping.Add(new KeyValuePair<TransformModelItem, List<TransformModelItem>>(

                    new TransformModelItem() { DisplayName = "Details", Description = "Details", Type = RtType.String }, // TODO was "Label"
                    new List<TransformModelItem>(info)

                    ));
            }
            return grouping;
        }

        /// <summary>
        /// deep copy the array
        /// </summary>
        /// <param name="groupTo"></param>
        /// <param name="groupFrom"></param>
        public static void CopyValueFrom(this IList<TransformModelGroup> groupTo, IList<TransformModelGroup> groupFrom)
        {
            if (!Type.ReferenceEquals(groupTo, groupFrom))
            {
                groupTo.Clear();
                foreach (var g in groupFrom)
                {
                    TransformModelGroup newOne = (TransformModelGroup)Activator.CreateInstance(g.GetType(), g );
                    //newOne.CopyFrom(g);
                    groupTo.Add(newOne);
                }
            }
        }
    }
}
