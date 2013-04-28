using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RazorTransform
{
    // types
    public enum RtType
    {
        Invalid,
        String,
        Int32,
        Password,
        Folder,
        UncPath,
        Bool,
        Array,
        HiddenString, 
        Guid,
        Enum,
        Custom
    }

    /// <summary>
    /// various constants used in RTValues and RTObjects XML files
    /// </summary>
    public static class Constants
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
        internal const string ReadOnly  = "readOnly";
        internal const string Arguments = "arguments";
        internal const string Class = "classname";
        internal const string Custom = "custom";
        internal const string Parameter = "parameter";

        /// <summary>
        /// extract the type from a XElement
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static RtType GetType(XElement x)
        {
            var s = (String)x.Attribute(Constants.Type) ?? String.Empty;
            return GetType(s);
        }

        /// <summary>
        /// map the type name to an RtType
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static RtType GetType(string typeName)
        {
            switch (typeName.ToLower())
            {
                case "string":
                    return RtType.String;
                case "int32":
                    return RtType.Int32;
                case "password":
                    return RtType.Password;
                case "folder":
                    return RtType.Folder;
                case "uncpath":
                    return RtType.Folder;
                case "guid":
                    return RtType.Guid;
                case "bool":
                    return RtType.Bool;
                case "Array":
                    return RtType.Array;
                case "hidden":
                    return RtType.HiddenString;
                default:
                    if (TransformModel.Instance.Enums.ContainsKey(typeName))
                        return RtType.Enum;
                    else if (TransformModel.Instance.Customs.ContainsKey(typeName))
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
            if (TransformModel.Instance.Enums.ContainsKey(s))
                return s;
            else
                return null;
        }
    }
}
