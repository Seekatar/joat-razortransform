using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Xml.Linq;

namespace RazorTransform.Custom
{
    class ServerPortModelItem : TransformModelItem
    {
        public ServerPortModelItem()
        {
        }

        public ServerPortModelItem(ServerPortModelItem src)
            : base(src)
        {
        }

        public override void LoadFromXml(XElement xml, XElement values, IDictionary<string, string> overrides)
        {
            base.LoadFromXml(xml, values, overrides);
        }
    }

    public class ServerPort : ICustomRazorTransformType
    {
        public Control CreateControl(TransformModelItem ci, System.Windows.Data.Binding binding)
        {
            return ServerPort.CreatePortControl(ci, binding);
        }

        public static Control CreatePortControl(TransformModelItem ci, System.Windows.Data.Binding binding)
        {
            var t = new PortInput(ci);
            binding.Mode = BindingMode.TwoWay;
            binding.NotifyOnValidationError = true;

            binding.ValidationRules.Add(new PortValidation(ci));

            t.SetBinding(PortInput.PortProperty, binding);
            return t;
        }

        public TransformModelItem CreateItem(TransformModelGroup parent)
        {
            return new ServerPortModelItem();
        }
    }


}
