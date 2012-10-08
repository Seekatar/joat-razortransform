using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RazorTransform
{
    /// <summary>
    /// Defines the abstraction for acquiring a value for a ConfigItem
    /// </summary>
    public interface IValueProvider
    {
        object GetValue(TransformModelItem input);
    }
}
