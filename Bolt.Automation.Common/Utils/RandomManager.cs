namespace Bolt.Automation.Common.Utils
{
    public static class RandomManager
    {
        // Thread-safe: Each thread gets its own Random instance with unique seed
        private static readonly ThreadLocal<Random> threadLocalRandom = 
            new ThreadLocal<Random>(() => new Random(Guid.NewGuid().GetHashCode()));
        
        private static readonly string staticString = "okiutyrtredrtyjuktmdgteatyjgukixzcvbnqwp";

        public static string GetRandomEmail()
        {
            return $"Automation_{GetRandomString(6)}@epos.com";
        }

        public static string GetRandomString(int length)
        {
            return new string(Enumerable.Repeat(staticString, length)
                .Select(s => s[threadLocalRandom.Value!.Next(s.Length)])
                .ToArray());
        }

        public static string GetRandomDigits(int length)
        {
            return new string(Enumerable.Repeat("0123456789", length)
                .Select(s => s[threadLocalRandom.Value!.Next(s.Length)])
                .ToArray());
        }

        public static int GetRandomInt(int length)
        {
            int min = (int)Math.Pow(10, length - 1);
            int max = (int)Math.Pow(10, length) - 1;
            return threadLocalRandom.Value!.Next(min, max + 1);
        }
    }
}