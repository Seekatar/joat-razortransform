using RazorTransform.Model;
using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// given a IModel.Items, find matches of IItems
        /// </summary>
        /// <param name="info"></param>
        /// <param name="criteria"></param>
        /// <returns></returns>
        public static IEnumerable<IItem> FindRecursive(this IEnumerable<IItemBase> info, Predicate<IItem> criteria)
        {
            var results = new List<IItem>();
            findModelItems(info, criteria, results);
            return results;
        }

        private static void findModelItems(IEnumerable<IItemBase> parent, Predicate<IItem> criteria, IList<IItem> results)
        {
            foreach (var i in parent.OfType<IItem>())
            {
                if (criteria(i))
                {
                    results.Add(i);
                }
                // TODO
                // findModelItems(i.Children, criteria, results );
            }
        }
    }
}
