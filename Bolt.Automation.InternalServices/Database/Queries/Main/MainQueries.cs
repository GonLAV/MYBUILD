using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.Common.PollyRetry;
using Bolt.Automation.InternalServices.Database.Contexts;
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

// logic

namespace Bolt.Automation.InternalServices.Database.Queries.Main
{
    public class MainQueries : IMainQueries
    {
        public MainQueries(MainDbContext context, IAutomationLogger logger, IPollyRetryService pollyRetryService)
        {
            Policy = new PolicyQueries(context, logger);
            Consumer = new ConsumerQueries(context, logger);
            Case = new CaseQueries(context, logger);
            CaseLogic = new CaseHelper(Case, logger, pollyRetryService);
            Renewals = new RenewalsQueries(context, logger);
            RenewalsLogic = new RenewalsHelper(Renewals, logger);
            Provisioning = new ProvisioningQueries(context, logger);
            ProvisioningLogic = new ProvisioningHelper(Provisioning, logger, pollyRetryService);
            ResultData = new ResultDataQueries(context, logger);
            ResultDataLogic = new ResultDataHelper(ResultData, logger);
            PolicyDataLogic = new PolicyDataHelper(Policy, logger, pollyRetryService);
        }

        public PolicyQueries Policy { get; }
        public RenewalsQueries Renewals { get; }
        public RenewalsHelper RenewalsLogic { get; }
        public ConsumerQueries Consumer { get; }
        public CaseQueries Case { get; }
        public CaseHelper CaseLogic { get; }
        public ProvisioningQueries Provisioning { get; }
        public ProvisioningHelper ProvisioningLogic { get; }
        public ResultDataQueries ResultData { get; }
        public ResultDataHelper ResultDataLogic { get; }
        public PolicyDataHelper PolicyDataLogic { get; }
    }
}