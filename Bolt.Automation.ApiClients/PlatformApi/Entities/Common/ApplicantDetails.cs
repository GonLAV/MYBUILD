namespace Bolt.Automation.ApiClients.PlatformApi.Entities.Common
{
    public class ApplicantDetails
    {
        public string GivenName { get; set; }
        public string OtherGivenName { get; set; }
        public string Surname { get; set; }
        public DateOnly? BirthDt { get; set; }
        public string MaritalStatusCd { get; set; }
        public string PhoneNumber { get; set; }
        public string EmailAddr { get; set; }
    }

}
