using RazorTransform.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
