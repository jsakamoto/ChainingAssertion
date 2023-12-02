namespace System.Diagnostics.CodeAnalysis
{
#if NETSTANDARD2_0 || NET462
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    internal sealed class MaybeNullAttribute : Attribute
    {
    }
#endif
}
