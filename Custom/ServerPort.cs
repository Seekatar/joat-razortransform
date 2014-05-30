using System.Collections.Generic;
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
    }

    public class ServerPort : ICustomRazorTransformType
    {
        public Control CreateControl(ITransformModelItem ci, System.Windows.Data.Binding binding, System.Action itemChanged)
        {
            return ServerPort.CreatePortControl(ci, binding, itemChanged);
        }

        public static Control CreatePortControl(ITransformModelItem ci, System.Windows.Data.Binding binding, System.Action itemChanged)
        {
            var t = new PortInput(ci);
            binding.Mode = BindingMode.TwoWay;
            binding.NotifyOnValidationError = true;

            binding.ValidationRules.Add(new PortValidation(ci));

            t.SetBinding(PortInput.PortProperty, binding);
            return t;
        }

        public TransformModelItem CreateItem(ITransformModelGroup parent, XElement e)
        {
            return new ServerPortModelItem();
        }


        public void Initialize(ITransformModel model, IDictionary<string, string> parms)
        {
        }
    }


}
