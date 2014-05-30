using System.Collections.Generic;
using System.Windows.Controls;
using System.Xml.Linq;

namespace RazorTransform.Custom
{
    class WebPortModelItem : TransformModelItem
    {
        public WebPortModelItem()
        {
        }

        public WebPortModelItem(WebPortModelItem src)
            : base(src)
        {
        }
    }

    public class WebPort : ICustomRazorTransformType
    {
        public Control CreateControl(ITransformModelItem ci, System.Windows.Data.Binding binding, System.Action itemChanged)
        {
            return ServerPort.CreatePortControl(ci, binding,itemChanged);
        }

        public TransformModelItem CreateItem(ITransformModelGroup parent, XElement e )
        {
            return new WebPortModelItem();
        }

        public void Initialize(ITransformModel model, IDictionary<string, string> parms)
        {
        }
    }
}
