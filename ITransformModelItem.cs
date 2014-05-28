namespace RazorTransform
{
    public interface ITransformModelItem
    {
        string Description { get; set; }
        string DisplayName { get; set; }
        string EnumName { get; set; }
        bool Hidden { get; set; }
        bool IsArrayItem { get; }
        string Max { get; set; }
        decimal MaxDecimal { get; }
        int MaxInt { get; }
        string Min { get; set; }
        decimal MinDecimal { get; }
        int MinInt { get; }
        string RegEx { get; }
        string OriginalType { get; set; }
        ITransformModelGroup Parent { get; set; }
        string PropertyName { get; set; }
        bool ReadOnly { get; set; }
        RtType Type { get; set; }
        string Value { get; set; }
        string ExpandedValue { get; set; }
    }
}
