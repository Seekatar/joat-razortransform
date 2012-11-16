using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RazorTransform
{
    /// <summary>
    /// various constants used in RTValues and RTObjects XML files
    /// </summary>
    internal static class Constants
    {
        // elements and attributes
        internal const string Group = "group";
        internal const string Item = "item";
        internal const string Name = "name";
        internal const string DisplayName = "displayName";
        internal const string Description = "description";
        internal const string ArrayValueName = "arrayValueName";
        internal const string DefaultValue = "defaultValue";
        internal const string Max = "max";
        internal const string Min = "min";
        internal const string Type = "type";
        internal const string Enum = "enum";
        internal const string Key = "key";
        internal const string Value = "value";
        internal const string ValueProvider = "valueProvider";
        internal const string Expanded = "expanded";
        internal const string Hidden = "hidden";
        internal const string Arguments = "arguments";

        // types
        internal const string String = "string";
        internal const string Int32 = "int32";
        internal const string Password = "password";
        internal const string Folder = "folder";
        internal const string UncPath = "uncpath";
        internal const string Guid = "guid";
        internal const string Bool = "bool";
        internal const string Array = "Array";

    }
}
