namespace Bolt.Automation.ApiClients.PlatformApi.Entities.Common
{
    public enum ChannelIndicators
    {
        Agency = 0,
        Direct
    }

    public enum ConsumerLine : short
    {
        Commercial,
        Personal,
        NA
    }

    public enum DeviceIndicator
    {
        Desktop,
        Tablet,
        Mobile,
        NotSet
    }

    public enum TransitionType
    {
        DeepLink = 0,
        CRMNote,
        QuoteStatus,
        QuoteStart,
        QuoteRetrieve,
        AddEvent,
        FindQuote,
        SFTPQuoteStart,
        FindEntities,
        GotoApplication
    }

    public enum SourceNames
    {
        MPQ3,
        MPX,
        Condominium,
        Homeowners,
    }

    public enum Lobs
    {
        Home,
        HO3,
        HO6,
        DF,
        MFH,
        Renters
    }

    public enum SPNames
    {
        BOLTAPI,
    }

    public enum Orgs
    {
        ProgressivePL,
    }

    /// <summary>
    /// Primary quote statuses listed by priority (highest priority takes precedence).
    /// </summary>
    public enum QuoteStatus
    {
        NotFound,
        Sold,
        Bridged,
        ConsumerBridged,
        NoAppetite,
        Processing,
        Expired,
        Complete,
        DNQ,
        Incomplete
    }

    /// <summary>
    /// Secondary quote statuses providing additional context to the primary status.
    /// </summary>
    public enum QuoteSecondaryStatus
    {
        LockedByAgent,
        NoAppetite,
        ReferToAgent,
        UUD,
        DNQ,
        ManufacturedHome,
        TechnicalDifficulty,
        TimeOut,
        ResultDeliveryFailure,
        NoVisibleResults,
        None
    }
}
