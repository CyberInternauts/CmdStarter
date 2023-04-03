namespace com.cyberinternauts.csharp.CmdStarter.Lib.Extensions
{
    internal static class IEnumerableExtensions
    {
        internal static object[] ToObjectArray<T>(this IEnumerable<T> enumerable)
        {
            return ToNullableObjectArray(enumerable)!;
        }

        internal static object?[] ToNullableObjectArray<T>(this IEnumerable<T> enumerable)
        {
            return enumerable.Select(item => (object?)item).ToArray();
        }
    }
}
