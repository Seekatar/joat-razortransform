using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RazorTransform
{
    public static class ConfigItemExtensions
    {
        public static List<KeyValuePair<ConfigInfo, List<ConfigInfo>>> GroupByType(this IEnumerable<ConfigInfo> info)
        {
            var list = info.ToList();

            List<KeyValuePair<ConfigInfo, List<ConfigInfo>>> grouping = new List<KeyValuePair<ConfigInfo, List<ConfigInfo>>>();
            var types = list.Where(x => x.Type == "Label" || x.Type == "Array");

            List<int> indexList = new List<int>();
            foreach (var type in types)
            {
                KeyValuePair<ConfigInfo, List<ConfigInfo>> kvp = new KeyValuePair<ConfigInfo, List<ConfigInfo>>(type, new List<ConfigInfo>());
                //find the index in the collection of type and select all subsequent items until another "Label" is found or end of the collection is found.
                int index = list.FindIndex((ci) => ci.Name == type.Name);
                indexList.Add(index);

                grouping.Add(kvp);


            }
            for (int k = 0; k < grouping.Count; k++)
            {
                var kvp = grouping[k];
                int index = indexList[k];

                if (k + 1 < indexList.Count)
                {
                    ConfigInfo[] sub = new ConfigInfo[(indexList[k + 1] - index) - 1];
                    //remove the first item which should be the label.
                    list.CopyTo(index + 1, sub, 0, (indexList[k + 1] - index) - 1);
                    kvp.Value.AddRange(sub);
                }
                else
                {
                    ConfigInfo[] sub = new ConfigInfo[(list.Count - index) - 1];
                    //remove the first item which should be the label.
                    list.CopyTo(index + 1, sub, 0, (list.Count - index) - 1);
                    kvp.Value.AddRange(sub);
                }

            }
            if (!grouping.Any())
            {
                grouping.Add(new KeyValuePair<ConfigInfo, List<ConfigInfo>>(

                    new ConfigInfo { Name = "Details", Description = "Details", Type = "Label", Expanded = true },
                    new List<ConfigInfo>(info)

                    ));
            }
            return grouping;
        }
    }
}
