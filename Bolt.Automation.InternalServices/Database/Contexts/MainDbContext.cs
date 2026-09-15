using Bolt.Automation.InternalServices.Database.Entities.Main;
using LinqToDB;
using LinqToDB.Data;
using static Bolt.Automation.InternalServices.Database.Entities.Main.ResultData;

namespace Bolt.Automation.InternalServices.Database.Contexts
{
    public class MainDbContext(DataOptions options) : DataConnection(options)
    {
        public ITable<Case> Cases => this.GetTable<Case>();
        public ITable<ExternalSource> ExternalSources => this.GetTable<ExternalSource>();
        public ITable<Journey> Journeys => this.GetTable<Journey>();
        public ITable<PolicyBinder> PolicyBinders => this.GetTable<PolicyBinder>();
        public ITable<RenewalsRequoteDetails> RenewalsRequoteDetails => this.GetTable<RenewalsRequoteDetails>();
        public ITable<ProvisioningAction> ProvisioningActions => this.GetTable<ProvisioningAction>();
        public ITable<ResultData> ResultData => this.GetTable<ResultData>();
        public ITable<ResultAttachment> ResultAttachments => this.GetTable<ResultAttachment>();
        public ITable<ResultAttachmentContent> ResultAttachmentContents => this.GetTable<ResultAttachmentContent>();
        public ITable<Users> Users => this.GetTable<Users>();
        public ITable<Consumer> Consumers => this.GetTable<Consumer>();
        public ITable<Deeplink> Deeplinks => this.GetTable<Deeplink>();
        public ITable<EmailTemplate> EmailTemplates => this.GetTable<EmailTemplate>();
        public ITable<GroupRestrictions> GroupRestrictions => this.GetTable<GroupRestrictions>();
        public ITable<Policy> Policies => this.GetTable<Policy>();
        public ITable<PolicyAttachment> PolicyAttachments => this.GetTable<PolicyAttachment>();
        public ITable<ActivityAgent> ActivityAgents => this.GetTable<ActivityAgent>();
        public ITable<UserGroupRoles> UserGroupRoles => this.GetTable<UserGroupRoles>();
        public ITable<Groups> Groups => this.GetTable<Groups>();
        public ITable<Roles> Roles => this.GetTable<Roles>();
        public ITable<GroupTypes> GroupTypes => this.GetTable<GroupTypes>();
        public ITable<GroupRelations> GroupRelations => this.GetTable<GroupRelations>();
        public ITable<ProvisioningAction> ProvisioningAction => this.GetTable<ProvisioningAction>();
        public ITable<vGroupTree> vGroupTree => this.GetTable<vGroupTree>();

    }
}