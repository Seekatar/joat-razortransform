using RazorTransform.Model;
using System;
using System.Xml.Linq;

namespace RazorTransform
{
    // types
    public enum RtType
    {
        Invalid,
        String,
        Int,
        Password,
        Folder,
        UncPath,
        Bool,
        Array,
        HiddenString, 
        Guid,
        Enum,
        HyperLink,
        Label,
        Custom
    }

    public enum RtSort
    {
        None,
        Ascending,
        Descending
    }

    /// <summary>
    /// various constants used in RTValues and RTObjects XML files
    /// </summary>
    public static class Constants
    {
        // elements and attributes
        internal const string Group = "group";
        internal const string NestedGroup = "nestedGroup";
        internal const string GroupName = "groupArrayValueName";
        internal const string Item = "item";
        internal const string Name = "name";
        internal const string DisplayName = "displayName";
        internal const string Description = "description";
        internal const string ArrayValueName = "arrayValueName";
        internal const string DefaultValue = "defaultValue";
        internal const string Max = "max";
        internal const string Min = "min";
        internal const string RegEx = "regex";
        internal const string Type = "type";
        internal const string Enum = "enum";
        internal const string Key = "key";
        internal const string Value = "value";
        internal const string ValueProvider = "valueProvider";
        internal const string Expanded = "expanded";
        internal const string Hidden = "hidden";
        internal const string Sort = "sort";
        internal const string Unique = "unique";
        internal const string ReadOnly  = "readOnly";
        internal const string Arguments = "arguments";
        internal const string Class = "classname";
        internal const string Custom = "custom";
        internal const string Parameter = "parameter";
        internal const string Model = "model";
        internal const string Original = "orig";
        internal const string Version = "version";
        internal const string CurrentSettings = "CurrentSettings";
        internal const string Root = "Root";
        internal const string Parent = "Parent";
        internal const string DestinationFolder = "DestinationFolder";

        // Version history
        // 2 = switch RtValues to use names instead of value for everything
        internal const int RtValuesVersion2 = 2;
        internal const int CurrentRtValuesVersion = RtValuesVersion2;

        /// <summary>
        /// map the type name to an RtType
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static RtType MapType(string typeName)
        {
            switch (typeName.ToLower())
            {
                case "string":
                    return RtType.String;
                case "int32": // older type
                case "int":
                    return RtType.Int;
                case "password":
                    return RtType.Password;
                case "label":
                    return RtType.Label;
                case "folder":
                    return RtType.Folder;
                case "uncpath":
                    return RtType.Folder;
                case "guid":
                    return RtType.Guid;
                case "bool":
                    return RtType.Bool;
                case "hidden":
                    return RtType.HiddenString;
                case "hyperlink":
                    return RtType.HyperLink;
                default:
                    if (ModelConfig.Instance.Enums.ContainsKey(typeName))
                        return RtType.Enum;
                    else if (ModelConfig.Instance.CustomTypes.ContainsKey(typeName))
                        return RtType.Custom;
                    else
                        return RtType.Invalid;
            }
        }

        /// <summary>
        /// for enum types, return the name, 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="enums"></param>
        /// <returns></returns>
        public static string GetEnumName(XElement x)
        {
            var s = (String)x.Attribute(Constants.Type) ?? String.Empty;
            if (ModelConfig.Instance.Enums.ContainsKey(s))
                return s;
            else
                return null;
        }
    }
}
