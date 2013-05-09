using System;
namespace RazorTransform
{
    public interface ITransformModel
    {
        System.Collections.Generic.IList<TransformModelArray> Arrays { get; }
        System.Collections.Generic.IDictionary<string, RazorTransform.Custom.ICustomRazorTransformType> Customs { get; }
        System.Collections.Generic.IDictionary<string, System.Collections.Generic.Dictionary<string, string>> Enums { get; }
        System.Collections.Generic.List<TransformModelGroup> Groups { get; }
        event EventHandler<ItemChangedArgs> ItemAdded;
        event EventHandler<ItemChangedArgs> ItemChanged;
        event EventHandler<ItemChangedArgs> ItemDeleted;
        event EventHandler<ModelChangedArgs> ModelLoaded;
        event EventHandler<ModelChangedArgs> ModelSaved;
        event EventHandler<ModelChangedArgs> ModelValidate;
        string TitleSuffix { get; }
    }
}
