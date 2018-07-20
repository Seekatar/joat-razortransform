using RazorTransform.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RazorTransform.Custom
{
	/// <summary>
	/// helper just to avoid boiler plate code
	/// </summary>
	public abstract class CustomRazorTransformTypeBase : ICustomRazorTransformType
    {
        public string Name { get; set; }

        public virtual IItem CreateItem(IModel parent, IGroup group, System.Xml.Linq.XElement e)
        {
            // never should be called
            throw new NotImplementedException();
        }

        public virtual System.Windows.Controls.Control CreateControl(IItem info, System.Windows.Data.Binding binding, System.Action<IItem> itemChanged)
        {
            // never should be called
            throw new NotImplementedException();
        }

        public abstract void Initialize(IModelConfig config, IDictionary<string, string> parms);

        /// <summary>
        /// Sets the string properties that match parms passes in 
        /// </summary>
        /// <param name="parms">The parms.</param>
        protected void setParms(IDictionary<string, string> parms)
        {
            if (parms != null)
            {
                var props = this.GetType().GetProperties().Where(o => o.PropertyType == typeof(string));
                foreach (var p in parms)
                {
                    // do we have a property for this
                    var prop = this.GetType().GetProperty(p.Key, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (prop != null && prop.PropertyType == typeof(string))
                    {
                        prop.SetValue(this, p.Value);
                    }
                }
            }


        }
    }
}
