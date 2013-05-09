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
    class WebPortModelItem : TransformModelItem
    {
        public WebPortModelItem()
        {
        }

        public WebPortModelItem(WebPortModelItem src)
            : base(src)
        {
        }

        public override void LoadFromXml(XElement xml, XElement values, IDictionary<string, string> overrides)
        {
            base.LoadFromXml(xml, values, overrides);
        }
    }

    public class WebPort : ICustomRazorTransformType
    {
        public Control CreateControl(ITransformModelItem ci, System.Windows.Data.Binding binding)
        {
            return ServerPort.CreatePortControl(ci, binding);
        }

        public TransformModelItem CreateItem(ITransformModelGroup parent, XElement e)
        {
            return new WebPortModelItem();
        }

        public void Initialize(ITransformModel model, IDictionary<string, string> parms)
        {
        }
    }
}
