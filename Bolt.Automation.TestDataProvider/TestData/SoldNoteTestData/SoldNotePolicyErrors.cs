namespace Bolt.Automation.TestDataProvider.TestData.SoldNoteTestData;

/// <summary>
/// Expected carrier-specific and general validation error messages for the Sold Note
/// policy number field. These are domain-level expected values with no NUnit dependency.
/// </summary>
public static class SoldNotePolicyErrors
{
    public const string MaxLength           = "Policy number cannot exceed 25 characters";

    public const string AmericanIntegrity   = "American Integrity\u2019s home policy numbers start with AGH followed by seven digits. A policy that has three different letters at the beginning is not an HO3 policy.";
    public const string AMIG                = "American Modern\u2019s policy number should be 9 digits long (e.g. 123456789).";
    public const string ASI                 = "ASI\u2019s policy number should be at least 7 characters long, consisting of 3 letters followed by numbers (e.g. ABC12345).";
    public const string Assurant            = "Assurant\u2019s policy number should be 10 characters long, with the prefix \"PSM\" followed by 7 numbers. (e.g. PSM1234567).";
    public const string Foremost            = "The Policy Note Format for a Foremost Signature HO3 policy should be a length of 11 characters, with the first character being a letter, and the remaining 10 characters being numbers.";
    public const string Hippo               = "Hippo policy numbers are 13 capital characters long, starting with three letters representing the policy type and state, followed by a dash, an 8-digit policy ID, another dash, and ending with 00.";    
    public const string HomesiteFormat      = "Homesite's policy number should be 8 digits long (e.g. 12345678)";    
    public const string Homesite            = "Homesite’s policy number should be 8 digits long (e.g. 12345678).";
    public const string NationalGeneral     = "National General\u2019s policy number should be 12 digits long. If you\u2019ve received a policy number with only 10 digits please add 00 at the end.";
    public const string Nationwide          = "Nationwide\u2019s policy number should be 12 characters long (e.g. 1238AZ576422).";
    public const string Openly              = "Openly's policy number should begin with the prefixes BQ01, TCPN, or TRPN, followed by 5-7 letters after the dash (e.g. BQ01-ABCDEFG).";
    public const string Progressive         = "Progressive\u2019s policy number should be 9 characters long (e.g. 123456789)";
    public const string ProgressiveUmbrella = "Progressive\u2019s policy number should be 8 numeric digits long and start with the number 6.";
    public const string Pure                = "PURE’s policy number should be 11 characters long, beginning with 2 letters followed by 9 numbers.";
    public const string RLI                 = "All issued policy numbers are 10 alphanumeric characters in length, starting with the letters \"PUP\" followed by seven numeric characters.";
    public const string TowerHill           = "Tower Hill policy numbers should be one letter and then nine digits long.";
    public const string True                = "TRUE Policy numbers should be 14 characters long, beginning with 3 letters representing the state, followed by 1 character for the LOB, followed by 10 digits.";
    public const string ASIUmbrella         = "Policy number Example TXU123456: The first two letters will be the policy state\u2019s abbreviation and the 3rd letter is the product (U) Umbrella";
}
