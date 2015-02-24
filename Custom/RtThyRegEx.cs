﻿using RazorTransform.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RazorTransform.Custom
{
    /// <summary>
    /// custom class for adding/removing regular expressions in the enum for the RtThySelf Xml
    /// </summary>
    /// This custom class doesn't create items, but just hooks onto the
    /// transform model's events
    /// 
    class RtThySelfRegEx : ICustomRazorTransformType
    {
        static string rtRegEx = "RtRegEx"; // name of RegEx enum
        //static string enumTag = "enum";
        static string regexTag = "regex";

        #region EventHandlers
        void OnItemChanged(object sender, ItemChangedArgs arg)
        {
            var model = sender as IModel;

            if (arg.List.Name == regexTag) // changing the regex group?
            {
                // remove all the existing ones
                if (ModelConfig.Instance.Enums.ContainsKey(rtRegEx) && ModelConfig.Instance.Enums[rtRegEx] != null)
                {
                    var rtTypes = ModelConfig.Instance.Enums[rtRegEx];
                    var delMe = rtTypes.Where(o => !String.IsNullOrEmpty(o.Key)).Select(o => o.Key).ToList(); // <None> has empty key
                    foreach (var d in delMe)
                    {
                        rtTypes.Remove(d);
                    }
                }

                // re-add all of them
                OnModelLoaded(model, new ModelChangedArgs());
            }
        }

        void OnModelLoaded(object sender, ModelChangedArgs e)
        {
            var model = sender as IModel;

            // add the custom and the enums to a type enum, if it exists
            if (ModelConfig.Instance.Enums.ContainsKey(rtRegEx) && ModelConfig.Instance.Enums[rtRegEx] != null)
            {
                var rtTypes = ModelConfig.Instance.Enums[rtRegEx];

                var regexes = model.Items.Where(o => o.Group.DisplayName == regexTag);
                if (regexes != null)
                {
                    foreach (var r in regexes.Select(o => o.Name))
                    {
                        if (!rtTypes.ContainsKey(r))
                            rtTypes.Add(r, r);
                    }
                }
            }
        }

        #endregion
        public RtThySelfRegEx()
        { }

        public IItem CreateItem(IModel parent, IGroup group, System.Xml.Linq.XElement e)
        {
            // never needed to be called
            throw new NotImplementedException();
        }

        public System.Windows.Controls.Control CreateControl(IItem info, System.Windows.Data.Binding binding, System.Action<IItem> itemChanged)
        {
            // never needed to be called
            throw new NotImplementedException();
        }


        public void Initialize(IModelConfig _model, IDictionary<string, string> parms)
        {
            _model.ItemAdded += OnItemChanged;
            _model.ItemDeleted += OnItemChanged;
            _model.ItemChanged += OnItemChanged;
            _model.ModelLoaded += OnModelLoaded;
        }

    }
}
