namespace Bolt.Automation.ApiClients.PlatformApi.Entities.Provision.BatchUserGroup
{
    public class BatchEntitiesResponse
    {
        public int NumSuccess
        {
            get
            {
                if (Successes == null)
                {
                    return 0;
                }

                return Successes.Count;
            }
        }

        public int NumFailure
        {
            get
            {
                if (Failures == null)
                {
                    return 0;
                }

                return Failures.Count;
            }
        }

        public List<ProvisioningEntityResponse> Successes { get; set; } = [];

        public List<ProvisioningEntityResponse> Failures { get; set; } = [];
    }
}
