using RazorTransform.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RazorTransform.Custom
{
    /// <summary>
    /// custom class for adding/removing types from an array
    /// </summary>
    /// This custom class doesn't create items, but just hooks onto the
    /// transform model's events
    /// 
    class EnumFromArrayType : CustomRazorTransformTypeBase
    {
        string enumPrefix { get; set; } // prefix for items already in the enum in RtObject.xml
        string customPrefix { get; set; } // string to add in front of displayed value for items added on-the-fly

        string enumName { get; set; }       // name of <enum> in RTObject.xml to fill in
        string arrayName { get; set; }      // arrayValueName in RTObject we use to populate this
        string arrayItemName { get; set; }  // name of item in array to show in combo

        Dictionary<string, string> _original = new Dictionary<string, string>();

        #region EventHandlers
        void OnItemChanged(object sender, ItemChangedArgs arg)
        {
            var model = arg.Item;

            if (arg.List.Name == arrayName)
            {
                // re-add all of them
                OnModelLoaded(model, new ModelLoadedArgs(model.Root));
            }
        }

        void OnModelLoaded(object sender, ModelLoadedArgs e)
        {
            var model = e.Model;

            // add the custom and the enums to a type enum, if it exists
            if (ModelConfig.Instance.Enums.ContainsKey(enumName) && ModelConfig.Instance.Enums[enumName] != null)
            {
                var rtTypes = ModelConfig.Instance.Enums[enumName];
                rtTypes.Clear();

                // first add the original ones
                foreach( var i in _original )
                {
                    rtTypes[i.Key] = enumPrefix + i.Value;
                }

                // second add ones from the array in the model
                var arrayValues = model.Items.Where(o => o.Name == arrayName).OfType<IItemList>().SingleOrDefault();
                if (arrayValues != null)
                {
                    foreach (var r in arrayValues)
                    {
                        var key = (r.Items.SingleOrDefault(o => o.Name == arrayItemName) as Item).Value;
                        if (!rtTypes.ContainsKey(key))
                            rtTypes.Add(key, customPrefix + arrayValues.ModelKeyName(r));
                    }
                }
                else
                {
                    throw new Exception(string.Format(Resource.InvalidArrayName, arrayName, Name));
                }

            }
        }

        #endregion
        public EnumFromArrayType()
        {
        }

        public override void Initialize(IModelConfig config, IDictionary<string, string> parms)
        {
            config.ItemAdded += OnItemChanged;
            config.ItemDeleted += OnItemChanged;
            config.ItemChanged += OnItemChanged;
            config.ModelLoaded += OnModelLoaded;

            setParms(parms);

            if (ModelConfig.Instance.Enums.ContainsKey(enumName) && ModelConfig.Instance.Enums[enumName] != null)
            {
                var rtTypes = ModelConfig.Instance.Enums[enumName];
                foreach (var i in rtTypes)
                {
                    _original.Add(i.Key, i.Value);
                }
            }
            else
            {
                throw new Exception(string.Format(Resource.InvalidEnumName, enumName, Name));
            }
        }
    }
}
