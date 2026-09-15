
namespace Bolt.Automation.Common.Utils
{
    public static class EnvironmentUtils
    {
        public static DateTime GetCurrentDateTimeForEnvironment(string env)
        {
            var envEnum = Enum.Parse<Environment>(env);
            DateTime timeUtc = DateTime.UtcNow;
            TimeZoneInfo timeZone;
            DateTime cstTime;

            if (envEnum == Environment.Dev)
                timeZone = TimeZoneInfo.FindSystemTimeZoneById("Israel Standard Time");
            
            else
                timeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                
            cstTime = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, timeZone);
            return cstTime;
        }

    }
}
