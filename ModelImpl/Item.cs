﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RazorTransform.Model
{
    internal class Item : IItem
    {
        List<string> _visibilityGroups = new List<string>();
        XElement _element;

        public Item(IModel parent, IGroup group)
        {
            Parent = parent;
            Group = group;
        }


        public Item(Item src, IModel parent)
        {
            copyValuesFrom(src, parent:parent);
        }

        public Item(Item src, IGroup group)
            : this(src, src.Value, group)
        {
        }

        public Item(Item src, string value, IGroup group)
        {
            copyValuesFrom(src, value, group);
        }

        void copyValuesFrom( Item src, string value = null, IGroup group = null, IModel parent = null )
        {
            Value = value ?? src.Value;
            ExpandedValue = src.ExpandedValue;
            Group = group ?? src.Group;
            Parent = parent ?? src.Parent;

            DisplayName = src.DisplayName;
            Name = src.Name;
            Description = src.Description;
            Type = src.Type;
            OriginalTypeStr = src.OriginalTypeStr;
            Min = src.Min;
            Max = src.Max;
            EnumName = src.EnumName;
            Regex = src.Regex;
            IsPassword = src.IsPassword;
        }

        #region IItem/IItemBase properties
        public IList<string> VisibilityGroups
        {
            get { return _visibilityGroups; }
        }

        public string Value
        {
            get;
            set;
        }

        public string ExpandedValue
        {
            get;
            set;
        }

        public void LoadFromXml(XElement xml, XElement values, IDictionary<string, string> overrides)
        {
            loadFromXml(xml);

            LoadValuesFromXml(values, overrides);
        }

        public string Name
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }

        /// <summary>
        /// Regular expression validation, may be null
        /// </summary>
        public string Regex
        {
            get;
            set;
        }

        public Int64 Min
        {
            get;
            set;
        }

        public Int64 Max
        {
            get;
            set;
        }

        public IGroup Group
        {
            get;
            set;
        }

        public IModel Parent
        {
            get;
            private set;
        }


        public RtType Type
        {
            get;
            private set;
        }


        public string DisplayName
        {
            get;
            set;
        }

        public bool ReadOnly
        {
            get;
            set;
        }

        public string OriginalTypeStr
        {
            get;
            private set;
        }

        public string EnumName
        {
            get;
            private set;
        }

        public bool Hidden
        {
            get;
            set;
        }

        public bool IsPassword
        {
            get;
            set;
        }
        #endregion

        #region IItem/IItemBase methods

        /// <summary>
        /// gets the value of Value as Type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetValue<T>()
        {
            return (T)Convert.ChangeType(this.Value, typeof(T));
        }

        /// <summary>
        /// validate the model.
        /// </summary>
        /// <param name="errors">collection to be populated</param>
        public void Validate(ICollection<ValidationError> errors)
        {
            string value = (Value ?? String.Empty).Trim();

            switch (Type)
            {
                case RtType.String:
                    if (!String.IsNullOrWhiteSpace(Regex) && !System.Text.RegularExpressions.Regex.IsMatch(value, ModelConfig.Instance.Regexes[Regex]))
                    {
                        errors.Add(new ValidationError(String.Format(Resource.RegExViolation, DisplayName, Regex), Group));
                    }
                    if (value.Length < Min)
                    {
                        errors.Add(new ValidationError(String.Format(Resource.MinStrLen, DisplayName, Min), Group));
                    }
                    if (value.Length > Max)
                    {
                        errors.Add(new ValidationError(String.Format(Resource.MaxStrLen, DisplayName, Max), Group));
                    }
                    break;

                case RtType.Int:
                    Int64 v;
                    if (Int64.TryParse(Value, out v))
                    {
                        if (v < Min)
                        {
                            errors.Add(new ValidationError(String.Format(Resource.MinInt, DisplayName, Min), Group));
                        }
                        if (v > Max)
                        {
                            errors.Add(new ValidationError(String.Format(Resource.MaxInt, DisplayName, Max), Group));
                        }
                    }
                    else
                    {
                        errors.Add(new ValidationError(String.Format(Resource.BadInteger, DisplayName, Value), Group));
                    }
                    break;
            }
        }


        /// <summary>
        /// Generate the RtValues XML adding it under the root XML passed in
        /// </summary>
        /// <param name="root"></param>
        public void GenerateXml(XElement root)
        {
            XElement x = null;
            if (!String.IsNullOrEmpty(ExpandedValue))
                x = new XElement(Name, ExpandedValue, new XAttribute(Constants.Original, Value ?? String.Empty));
            else
                x = new XElement(Name, Value ?? String.Empty, new XAttribute(Constants.Original, String.Empty));

            root.Add(x);
        }
        #endregion


        public void LoadValuesFromXml(XElement values, IDictionary<string, string> overrides)
        {
            if (overrides != null && overrides.ContainsKey(Name))
            {
                Value = overrides[Name];
            }
            else if (values != null)
            {
                string v = null;
                if (Settings.Instance.RtValuesVersion == 1)
                {
                    var childValues = values.Elements().Where(n =>
                    {
                        var a = n.Attribute(Constants.Name);
                        if (a != null)
                            return a.Value == Name;
                        else
                            throw new Exception("Missing name on element " + n.ToString());
                    });

                    var val = childValues.SingleOrDefault();
                    if (val != null)
                    {
                        var a = val.Attribute(Constants.Value);
                        if (a != null)
                            Value = val.Attribute(Constants.Value).Value;
                        else
                            throw new Exception("Missing value on element " + v.ToString());
                    }
                }
                else // must be v2
                {
                    var element = values.Elements(Name).SingleOrDefault();
                    if (element != null)
                    {
                        v = element.Value;

                        // show the original value as Value in the model
                        // and filled in one in tooltip
                        var attr = element.Attribute(Constants.Original);
                        if (attr != null && !String.IsNullOrWhiteSpace(attr.Value) && attr.Value.Contains('@'))
                        {
                            ExpandedValue = v;
                            v = attr.Value;
                        }
                    }
                }
                if (v != null)
                    Value = v;
            }
        }

        protected virtual void loadFromXml(XElement itemXml)
        {
            _element = itemXml;

            // attributes
            Name = (string)itemXml.Attribute(Constants.Name);
            if (Name == null)
                throw new ArgumentNullException(Constants.Name);

            // if no display name, use Name
            DisplayName = (string)itemXml.Attribute(Constants.DisplayName) ?? Name;
            Name = makeName(Name);

            Description = (string)itemXml.Attribute(Constants.Description) ?? DisplayName;

            OriginalTypeStr = (String)itemXml.Attribute(Constants.Type) ?? String.Empty;

            Type = Constants.MapType(OriginalTypeStr);

            if (Type == RtType.HiddenString) // old files have hidden as type to indicate hidden string
            {
                Type = RtType.String;
                Hidden = true;
            }
            else if (Type == RtType.Enum)
            {
                EnumName = Constants.GetEnumName(itemXml);
            }
            else if (Type == RtType.Password)
            {
                IsPassword = true;
            }
            else
            {
                Hidden = (bool?)itemXml.Attribute(Constants.Hidden) ?? false;
            }

            if (Type == RtType.Invalid)
            {
                throw new Exception("Invalid type for property " + Name);
            }

            ReadOnly = (bool?)itemXml.Attribute(Constants.ReadOnly) ?? false;

            Regex = (string)itemXml.Attribute(Constants.RegEx) ?? String.Empty;
            if (!String.IsNullOrWhiteSpace(Regex) && !ModelConfig.Instance.Regexes.ContainsKey(Regex))
            {
                throw new Exception(String.Format(Resource.RegExNotFound, Regex));
            }

            Min = (Int64?)itemXml.Attribute(Constants.Min) ?? Int64.MinValue;
            Max = (Int64?)itemXml.Attribute(Constants.Max) ?? Int64.MaxValue;

            setDefaultValue(itemXml);

            // no sub elements under <item>
        }

        /// <summary>
        /// grab the default value from the element and set it
        /// </summary>
        /// <param name="x"></param>
        protected virtual void setDefaultValue(XElement x)
        {
            if (x.Attribute(Constants.DefaultValue) != null && !String.IsNullOrEmpty(x.Attribute(Constants.DefaultValue).Value))
            {
                Value = x.Attribute(Constants.DefaultValue).Value;
            }
        }


        /// <summary>
        /// create a property name from the display name
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        protected static string makeName(string p)
        {
            p = p.Trim();
            StringBuilder sb = new StringBuilder();
            bool wasSpace = false;
            foreach (var c in p)
            {
                if (c == ' ')
                {
                    wasSpace = true;
                    continue;
                }
                if (wasSpace)
                    sb.Append(c.ToString().ToUpper());
                else
                    sb.Append(c);
                wasSpace = false;

            }
            return sb.ToString();
        }
    }
}