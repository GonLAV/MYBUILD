namespace Bolt.Automation.FrontEnds.FormData
{
    public class LocatorSet : List<string>
    {
        public LocatorSet() { }
        public LocatorSet(IEnumerable<string> locators) : base(locators) { }

        public static implicit operator LocatorSet(string singleLocator)
        {
            return [singleLocator];
        }

        public static implicit operator LocatorSet(string[] multipleLocators)
        {
            return new LocatorSet(multipleLocators);
        }

        public static implicit operator string(LocatorSet locatorSet)
        {
            return locatorSet.FirstOrDefault() ?? "";
        }
    }
}
