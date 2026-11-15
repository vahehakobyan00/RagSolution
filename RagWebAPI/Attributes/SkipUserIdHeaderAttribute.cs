namespace RagWebAPI.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class SkipUserIdHeaderAttribute : Attribute
    {
    }
}
