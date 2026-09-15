namespace Bolt.Automation.InternalServices.Extensions
{
    public static class EnumerableExtensions
    {
        public static bool IsNullOrEmpty<T>(this IEnumerable<T>? enumerable)
        {
            return enumerable is null || !enumerable.Any();
        }
    }
}
