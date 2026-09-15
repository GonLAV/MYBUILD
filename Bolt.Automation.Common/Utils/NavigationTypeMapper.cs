using Bolt.Automation.Common.Enums;

namespace Bolt.Automation.Common.Utils
{
    public static class NavigationTypeMapper
    {
        public static string ToUiText(this NavigationType type)
        {
            return type switch
            {
                NavigationType.LogOut => "Log Out",
                NavigationType.MarketFinder => "Market Finder",
                NavigationType.SendEmail => "Send Email",
                NavigationType.SendSms => "Send SMS",
                NavigationType.DefaultsManagement => "Defaults Management",
                NavigationType.TextMessaging => "Text Messaging",
                _ => type.ToString()
            };
        }
    }
}
