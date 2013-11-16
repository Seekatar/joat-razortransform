namespace RazorTransform
{
    public interface ITransformModelGroup
    {
        System.Collections.Generic.List<ITransformModelItem> Children { get; }
        string Description { get; set; }
        string DisplayName { get; set; }
        bool Expanded { get; set; }
        bool Hidden { get; set; }
        System.Collections.Generic.IEnumerable<ITransformModelItem> Items { get; }
    }
}
