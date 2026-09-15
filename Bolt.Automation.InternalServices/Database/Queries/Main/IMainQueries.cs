using Bolt.Automation.InternalServices.Database.DBHelpers.Case;
using Bolt.Automation.InternalServices.Database.DBHelpers.Policy;
using Bolt.Automation.InternalServices.Database.DBHelpers.Provisioning;
using Bolt.Automation.InternalServices.Database.DBHelpers.Renewals;
using Bolt.Automation.InternalServices.Database.DBHelpers.ResultData;
using Bolt.Automation.InternalServices.Database.Queries.Main.Case;
using Bolt.Automation.InternalServices.Database.Queries.Main.Consumer;
using Bolt.Automation.InternalServices.Database.Queries.Main.Policy;
using Bolt.Automation.InternalServices.Database.Queries.Main.Provisioning;
using Bolt.Automation.InternalServices.Database.Queries.Main.Renewals;
using Bolt.Automation.InternalServices.Database.Queries.Main.ResultData;

namespace Bolt.Automation.InternalServices.Database.Queries.Main
{
    public interface IMainQueries
    {
        PolicyQueries Policy { get; }
        ConsumerQueries Consumer { get; }
        CaseQueries Case { get; }
        CaseHelper CaseLogic { get; }
        RenewalsQueries Renewals { get; }
        RenewalsHelper RenewalsLogic { get; } // Use helper, not interface
        ProvisioningQueries Provisioning { get; }
        ProvisioningHelper ProvisioningLogic { get; } // Use helper, not interface
        ResultDataQueries ResultData { get; } // Raw
        ResultDataHelper ResultDataLogic { get; } // Use helper, not interface
        PolicyDataHelper PolicyDataLogic { get; }
    }
}