using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RazorTransform.Custom
{
    /// <summary>
    /// custom class for adding/removing types for the RtThySelf Xml
    /// </summary>
    /// This custom class doesn't create imtes, but just hooks onto the
    /// transform model's events
    /// 
    class RtThySelfType : ICustomRazorTransformType
    {
        static string enumPrefix = "Enum: ";
        static string customPrefix = "Custom: ";
        static string rtType = "RtType";
        static string enumTag = "enum";
        static string customTag = "custom";

        #region EventHandlers
        void OnItemChanged(object sender, ItemChangedArgs arg)
        {
            var model = sender as TransformModel;

            if (arg.Group.ArrayValueName == enumTag || arg.Group.ArrayValueName == customTag)
            {
                // remove all the existing ones
                if (model.Enums.ContainsKey(rtType) && model.Enums[rtType] != null)
                {
                    var rtTypes = model.Enums[rtType];
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
            var model = sender as TransformModel;

            // add the custom and the enums to a type enum, if it exists
            if (model.Enums.ContainsKey(rtType) && model.Enums[rtType] != null)
            {
                var rtTypes = model.Enums[rtType];

                var enums = model.Groups.Where(o => o is TransformModelArray && (o as TransformModelArray).ArrayValueName == enumTag).SingleOrDefault();
                if (enums != null)
                {
                    foreach (var r in enums.Items.Select(o => o.DisplayName))
                    {
                        if (!rtTypes.ContainsKey(r))
                            rtTypes.Add(r, enumPrefix + r);
                    }
                }
                var customs = model.Groups.Where(o => o is TransformModelArray && (o as TransformModelArray).ArrayValueName == customTag).SingleOrDefault();
                if (customs != null)
                {
                    foreach (var r in customs.Items.Select(o => o.DisplayName))
                    {
                        if (!rtTypes.ContainsKey(r))
                            rtTypes.Add(r, customPrefix + r);
                    }
                }
            }
        }

        #endregion
        public RtThySelfType(TransformModel _model)
        {
            _model.ItemAdded += OnItemChanged;
            _model.ItemDeleted += OnItemChanged;
            _model.ItemChanged += OnItemChanged;
            _model.ModelLoaded += OnModelLoaded;
        }

        public TransformModelItem CreateItem(TransformModelGroup parent, System.Xml.Linq.XElement e)
        {
            // never needed to be called
            throw new NotImplementedException();
        }

        public System.Windows.Controls.Control CreateControl(TransformModelItem info, System.Windows.Data.Binding binding)
        {
            // never needed to be called
            throw new NotImplementedException();
        }
    }
}
