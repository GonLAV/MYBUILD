using Bolt.Automation.ApiClients.PlatformApi.Entities.Provision;
using Bolt.Automation.ApiClients.PlatformApi.Entities.Provision.BatchUserGroup;
using Bolt.Automation.ApiClients.PlatformApi.Entities.Provision.CreateUser;
using Bolt.Automation.Common.Utils;
using Bolt.Automation.TestDataProvider.TestData.PlatformAPITestData;

namespace Bolt.Automation.TestDataProvider.Providers.PlatformAPIDataProvider
{
    public static class ProvisioningDataProvider
    {
        public static BatchUserGroupRequestModel CreateKraftlakeXProvisioningRequestModel(double? batchId = null, string? rqUID = null)
        {
            var requestId = rqUID ?? "7309759e-25a4-4a69-8afe-2bdc786bd497";
            var batch = batchId ?? 70457.0;

            // Generate agency data once to ensure consistency across groups and users
            var agencyName = "AH" + RandomManager.GetRandomDigits(5);
            var agencyExternalId = "AGENCY_" + agencyName;
            var aor1ExternalId = "AOR1" + agencyName;
            var aor2ExternalId = "AOR2" + agencyName;

            return new BatchUserGroupRequestModel
            {
                BatchId = batch,
                Groups = CreateHierarchicalGroups(requestId, agencyName, agencyExternalId, aor1ExternalId, aor2ExternalId),
                Users = CreateUsers(requestId, agencyName, agencyExternalId, aor1ExternalId, aor2ExternalId)
            };
        }

        public static CreateUserRequestModel CreateProgressiveUserModel()
        {
            var userId = RandomManager.GetRandomDigits(6);
          
            return new CreateUserRequestModel
            {
                Id = userId,
                ExternalId = userId,
                UserName = $"AHtest{userId}@progressive.com",
                Title = "",
                Name = new NameInfo
                {
                    FamilyName = "AHPGR" + RandomManager.GetRandomString(6),
                    GivenName = "Test" + RandomManager.GetRandomString(6)
                },
                Emails = new List<EmailInfo>
                {
                    new EmailInfo
                    {
                        Value = $"AHtest{userId}@progressive.com",
                        Type = "Work",
                        Primary = true
                    }
                },
                UserType = "Supervisor",
                Entitlements = new List<Entitlement>()
            };
        }

        private static List<GroupProvisioningItem> CreateHierarchicalGroups(string rqUID, string agencyName, string agencyExternalId, string aor1ExternalId, string aor2ExternalId)
        {
            var groups = new List<GroupProvisioningItem>();

            // Zone Level
            var zone = CreateGroupItem(rqUID, "ZONE_EA", "EA", "Structure", "Zone", "Active", 
                [new ParentGroup { ParentGroupId = "KLInternalAgency", HierarchyType = "Structure", Status = "Active" }]);
            groups.Add(zone);

            // Territory Level
            var territory = CreateGroupItem(rqUID, "TERRITORY_Central", "Central", "Structure", "Territory", "Active",
                [new ParentGroup { ParentGroupId = "ZONE_EA", HierarchyType = "Structure", Status = "Active" }]);
            groups.Add(territory);

            // Market Level
            var market = CreateGroupItem(rqUID, "MARKET_04A-NorthernTexas", "04A-NorthernTexas", "Structure", "Market", "Active",
                [new ParentGroup { ParentGroupId = "TERRITORY_Central", HierarchyType = "Structure", Status = "Active" }]);
            groups.Add(market);

            // Area Level
            var area = CreateGroupItem(rqUID, "AREA_C07", "C07", "Structure", "Area", "Active",
                [new ParentGroup { ParentGroupId = "MARKET_04A-NorthernTexas", HierarchyType = "Structure", Status = "Active" }]);
            groups.Add(area);

            // Division Level
            var division = CreateGroupItem(rqUID, "DIVISION_27", "27", "Structure", "division", "Active",
                [new ParentGroup { ParentGroupId = "AREA_C07", HierarchyType = "Structure", Status = "Active" }]);
            groups.Add(division);

            // State Level
            var state = CreateGroupItem(rqUID, "18", "AR", "Structure", "State", "Active",
                [new ParentGroup { ParentGroupId = "DIVISION_27", HierarchyType = "Structure", Status = "Active" }]);
            groups.Add(state);

            // Districts
            var district1 = CreateGroupItem(rqUID, "D1001", "D1001", "Structure", "District", "Active",
                [new ParentGroup { ParentGroupId = "18", HierarchyType = "Structure", Status = "Active" }]);
            groups.Add(district1);

            var district2 = CreateGroupItem(rqUID, "D2001", "D2001", "Structure", "District", "Active",
                [new ParentGroup { ParentGroupId = "18", HierarchyType = "Structure", Status = "Active" }]);
            groups.Add(district2);

            // Agency - using dynamic data
            var agency = CreateGroupItem(rqUID, agencyExternalId, agencyName, "Agency", "Agency", "Active",
                [new ParentGroup { ParentGroupId = "KLInternalAgency", HierarchyType = "Agency", Status = "Active" }]);
            groups.Add(agency);

            // AORs (Area of Responsibility) - using dynamic data
            var address = Addresses.Provisioning_MO_Ozark;
            var contact = new Contact { MailingAddress = address };

            var aor1 = CreateGroupItem(rqUID, aor1ExternalId, aor1ExternalId, "Agency", "AOR", "Active",
                [
                    new ParentGroup { ParentGroupId = agencyExternalId, HierarchyType = "Agency", Status = "Active", EndDate = "" },
                    new ParentGroup { ParentGroupId = "D1001", HierarchyType = "Agency", Status = "Active", EndDate = "" }
                ], address, contact, false);
            groups.Add(aor1);

            var aor2 = CreateGroupItem(rqUID, aor2ExternalId, aor2ExternalId, "Agency", "AOR", "Active",
                [
                    new ParentGroup { ParentGroupId = agencyExternalId, HierarchyType = "Agency", Status = "Active", EndDate = "" },
                    new ParentGroup { ParentGroupId = "D2001", HierarchyType = "Agency", Status = "Active", EndDate = "" }
                ], address, contact, false);
            groups.Add(aor2);

            return groups;
        }

        private static List<UserProvisioningItem> CreateUsers(string rqUID, string agencyName, string agencyExternalId, string aor1ExternalId, string aor2ExternalId)
        {
            var users = new List<UserProvisioningItem>();

            // District Managers
            var dm1 = CreateUserItem(rqUID, "DM1001", "District", null, "Manager", "Licensed", "District Manager", 
                "Bucket 1", "Active", "D1001", "ExternalManager", "District Manager", "dm1001@mail.com", "5556482006");
            users.Add(dm1);

            var dm2 = CreateUserItem(rqUID, "DM2001", "District", null, "Manager", "Licensed", "District Manager",
                "Bucket 1", "Active", "D2001", "ExternalManager", "District Manager", "dm2001@mail.com", "5556482006");
            users.Add(dm2);

            // Agency Principal - using dynamic data
            var principal = CreateUserItem(rqUID, agencyName, RandomManager.GetRandomString(6), null, RandomManager.GetRandomString(6), 
                "Licensed", "Agency Principal", "Bucket 1", "Active", agencyExternalId, "ExternalAgent", "Agency Principal", 
                $"AH{RandomManager.GetRandomString(6)}@test.com", "5556482005");
            users.Add(principal);

            // Licensed Producers - using dynamic data
            var lsp1ExternalId = aor1ExternalId + "user";
            var lsp2ExternalId = aor2ExternalId + "user";

            var lsp1 = CreateLicensedProducer(rqUID, lsp1ExternalId, aor1ExternalId, ["AL", "FL"], "LSPONE");
            users.Add(lsp1);

            var lsp2 = CreateLicensedProducer(rqUID, lsp2ExternalId, aor2ExternalId, ["CA", "TX"], "LSPTWO");
            users.Add(lsp2);

            return users;
        }

        private static UserProvisioningItem CreateLicensedProducer(string rqUID, string userId, string groupId, string[] states, string firstNamePrefix = "LSP")
        {               
            var user = new User
            {
                Type = "Licensed",
                ExternalUserType = "Licensed Producer",
                UserId = userId,
                ChoiceBucket = "Bucket 1",
                FirstName = firstNamePrefix + RandomManager.GetRandomString(6),
                MiddleName = null,
                LastName = RandomManager.GetRandomString(6),
                Status = "Active",
                AffiliatedGroups =
                [
                    new AffiliatedGroup
                    {
                        GroupId = groupId,
                        Status = "Active",
                        Roles = [new Role { AgentRole = "ExternalLicensed", ExternalRole = "Licensed Producer" }],
                        ContactDetails = new ContactDetails
                        {
                            Email = $"{firstNamePrefix}{(firstNamePrefix == "LSPONE" ? "1" : "2")}{RandomManager.GetRandomString(6)}@test.com",
                            WorkPhone = "5558635805",
                            Fax = "5552853005"
                        },
                        Licenses = states.Select(state => new License
                        {
                            State = state,
                            ExpirationDate = DateTime.Today.AddYears(1).ToString("yyyy-MM-dd")
                        }).ToList()
                    }
                ]
            };

            return new UserProvisioningItem
            {
                RqUID = rqUID,
                User = user,
                Parameters = new ProvisioningParameters { OverrideExistingLinks = "False" }
            };
        }

        private static GroupProvisioningItem CreateGroupItem(string rqUID, string groupId, string name, string type, 
            string externalGroupType, string status, List<ParentGroup> parentGroups, Address? physicalAddress = null, 
            Contact? contact = null, bool overrideLinks = true)
        {
            return new GroupProvisioningItem
            {
                RqUID = rqUID,
                Group = new Group
                {
                    GroupId = groupId,
                    Name = name,
                    Type = type,
                    ExternalGroupType = externalGroupType,
                    Status = status,
                    ParentGroups = parentGroups,
                    PhysicalAddress = physicalAddress,
                    Contact = contact
                },
                Parameters = new ProvisioningParameters { OverrideExistingLinks = overrideLinks.ToString() }
            };
        }

        private static UserProvisioningItem CreateUserItem(string rqUID, string userId, string firstName, string? middleName, 
            string lastName, string type, string externalUserType, string choiceBucket, string status, string groupId, 
            string agentRole, string externalRole, string email, string workPhone)
        {
            return new UserProvisioningItem
            {
                RqUID = rqUID,
                User = new User
                {
                    Type = type,
                    ExternalUserType = externalUserType,
                    UserId = userId,
                    ChoiceBucket = choiceBucket,
                    FirstName = firstName,
                    MiddleName = middleName,
                    LastName = lastName,
                    Status = status,
                    AffiliatedGroups =
                    [
                        new AffiliatedGroup
                        {
                            GroupId = groupId,
                            Status = "Active",
                            Roles = [new Role { AgentRole = agentRole, ExternalRole = externalRole }],
                            ContactDetails = new ContactDetails
                            {
                                Email = email,
                                WorkPhone = workPhone,
                                Fax = "0000000000"
                            }
                        }
                    ]
                },
                Parameters = new ProvisioningParameters { OverrideExistingLinks = "False" }
            };
        }
    }
}