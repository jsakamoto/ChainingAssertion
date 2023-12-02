namespace System.Diagnostics.CodeAnalysis
{
#if NETSTANDARD2_0 || NET462
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue, Inherited = false)]
    internal sealed class NotNull : Attribute
    {
    }
#endif
}
