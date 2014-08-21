using RazorTransform.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RazorTransform.Custom
{
    /// <summary>
    /// custom class for adding/removing types for the RtThySelf Xml
    /// </summary>
    /// This custom class doesn't create items, but just hooks onto the
    /// transform model's events
    /// 
    class RtThySelfType : ICustomRazorTransformType
    {
        static string enumPrefix = "Enum: ";
        static string customPrefix = "Custom: ";
        static string rtType = "RtType"; // name of type enum
        static string enumTag = "enum";
        static string customTag = "custom";

        #region EventHandlers
        void OnItemChanged(object sender, ItemChangedArgs arg)
        {
            var model = sender as IModel;

            if (arg.List.Name == enumTag || arg.List.Name == customTag)
            {
                // remove all the existing ones
                if (ModelConfig.Instance.Enums.ContainsKey(rtType) && ModelConfig.Instance.Enums[rtType] != null)
                {
                    var rtTypes = ModelConfig.Instance.Enums[rtType];
                    var delMe = rtTypes.Where(o => o.Value.StartsWith(enumPrefix) || o.Value.StartsWith(customPrefix)).Select( o => o.Key ).ToList();
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
            if (ModelConfig.Instance.Enums.ContainsKey(rtType) && ModelConfig.Instance.Enums[rtType] != null)
            {
                var rtTypes = ModelConfig.Instance.Enums[rtType];

                var enums = model.Items.Where(o => o.Group.DisplayName == enumTag);
                if (enums != null)
                {
                    foreach (var r in enums.Select(o => o.DisplayName))
                    {
                        if (!rtTypes.ContainsKey(r))
                            rtTypes.Add(r, enumPrefix + r);
                    }
                }
                var customs = model.Items.Where(o => o.Name == customTag ).OfType<IItemList>().SingleOrDefault();
                if (customs != null)
                {
                    foreach (var r in customs.Select(o => customs.ModelKeyName(o)))
                    {
                        if (!rtTypes.ContainsKey(r))
                            rtTypes.Add(r, customPrefix + r);
                    }
                }
            }
        }

        #endregion
        public RtThySelfType()
        { }

        public IItem CreateItem(IModel parent, IGroup group, System.Xml.Linq.XElement e)
        {
            // never needed to be called
            throw new NotImplementedException();
        }

        public System.Windows.Controls.Control CreateControl(IItem info, System.Windows.Data.Binding binding, System.Action itemChanged)
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
