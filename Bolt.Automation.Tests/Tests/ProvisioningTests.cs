using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.ApiClients.PlatformApi;
using Bolt.Automation.Common;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.Common.Utils;
using Bolt.Automation.InternalServices.Database.Queries.Main;
using Bolt.Automation.TestDataProvider.Providers.PlatformAPIDataProvider;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;

namespace Bolt.Automation.Tests.Tests
{
    public class ProvisioningTests : TestBase
    {
        private IMainQueries? _mainQueries;
        private IPlatformApiClientFactory _platformApiFactory = null!;

        protected override void ResolveServices()
        {
            var refitApiLocator = _testScope.ServiceProvider.GetRequiredService<RefitApiServiceLocator>();
            _mainQueries = _testScope.ServiceProvider.GetService<IMainQueries>();
            _platformApiFactory = refitApiLocator.GetRequiredService<IPlatformApiClientFactory>();
        }

        [Test]
        [Tenant(Tenant.KRAFTLAKEX)]
        [Category("Provision")]
        [Category("CRM")]
        [Author(Author.Andrii)]
        [TestCaseId(184705)]
        [Description("Validates provisioning of AOR groups with users linked to them. It sends batch create request to Platform API and then validates that all provisioning actions are successful.")]
        public async Task KraftlakeX_Provisioning_Test()
        {

                var provisioningRequest = await _logger.ExecuteStepAsync("Prepare dynamic test data", async () =>
                {
                    var request = ProvisioningDataProvider.CreateKraftlakeXProvisioningRequestModel();
                    return await Task.FromResult(request);
                });

                var agencyGroup = provisioningRequest.Groups
                    .First(g => g.Group.ExternalGroupType == "Agency");
                var aor1Group = provisioningRequest.Groups
                    .First(g => g.Group.ExternalGroupType == "AOR" && g.Group.GroupId.Contains("AOR1"));
                var aor2Group = provisioningRequest.Groups
                    .First(g => g.Group.ExternalGroupType == "AOR" && g.Group.GroupId.Contains("AOR2"));
                var districtGroups = provisioningRequest.Groups
                    .Where(g => g.Group.ExternalGroupType == "District").ToList();

                var agencyName = agencyGroup?.Group.Name;
                var agencyExternalId = agencyGroup?.Group.GroupId;
                var aor1ExternalId = aor1Group?.Group.GroupId;
                var aor2ExternalId = aor2Group?.Group.GroupId;

                var lspUser1ExternalId = aor1ExternalId + "user";
                var lspUser2ExternalId = aor2ExternalId + "user";
                var districtGroup1ExternalId = districtGroups.First().Group.GroupId;
                var districtGroup2ExternalId = districtGroups.Skip(1).First().Group.GroupId;

                var user = TestContextAccessor.CurrentUserCollection.RootAdmin;
                ScopeContext.Set(ctx => ctx.CurrentUser, user);

                await _logger.ExecuteStepAsync("Send batch create request to Platform API", async () =>
                {
                    var platformApi = await _platformApiFactory.CreateApiClientAsync();
                    var response = await platformApi.CreateUserGroupBatch(provisioningRequest);

                    Assert.That(response.IsSuccessStatusCode, Is.True,
                        $"Failed, status code should be 200/OK, actual {response.StatusCode}");

                    var userFailuresCount = response.Content.UserResponses.NumFailure;
                    var groupFailuresCount = response.Content.GroupResponses.NumFailure;

                    Assert.Multiple(() =>
                    {
                        Assert.That(userFailuresCount == 0, Is.True,
                            $"Failed, user failures count should be 0, actual {userFailuresCount}");
                        Assert.That(groupFailuresCount == 0, Is.True,
                            $"Failed, group failures count should be 0, actual {groupFailuresCount}");
                    });
                });

                await _logger.ExecuteStepAsync("Validate AOR2 group in database", async () =>
                {
                    var aor2GroupData = await _mainQueries.Provisioning.GetDataAfterProvisionByGroupExternalId(aor2ExternalId, districtGroup2ExternalId);
                    var aor2GroupName = aor2GroupData.Value.groupName;
                    var aor2GroupExternalIdFromDb = aor2GroupData.Value.groupExternalId;

                    Assert.Multiple(() =>
                    {
                        Assert.That(aor2GroupName == aor2ExternalId, Is.True,
                            $"Failed, aor2 group name should be {aor2ExternalId}, actual: {aor2GroupName}");
                        Assert.That(aor2GroupExternalIdFromDb == aor2ExternalId, Is.True,
                            $"Failed, aor2 group external Id should be {aor2ExternalId}, actual: {aor2GroupExternalIdFromDb}");
                    });
                });

                await _logger.ExecuteStepAsync("Validate AOR1 group in database", async () =>
                {
                    var aor1GroupData = await _mainQueries.Provisioning.GetDataAfterProvisionByGroupExternalId(aor1ExternalId, districtGroup1ExternalId);
                    var aor1GroupName = aor1GroupData.Value.groupName;
                    var aor1GroupExternalIdFromDb = aor1GroupData.Value.groupExternalId;
                    Assert.Multiple(() =>
                    {
                        Assert.That(aor1GroupName == aor1ExternalId, Is.True,
                            $"Failed, aor1 group name should be {aor1ExternalId}, actual: {aor1GroupName}");
                        Assert.That(aor1GroupExternalIdFromDb == aor1ExternalId, Is.True,
                            $"Failed, aor1 group external Id should be {aor1ExternalId}, actual: {aor1GroupExternalIdFromDb}");
                    });
                });

                await _logger.ExecuteStepAsync("Validate group paths in database", async () =>
                {
                    var aor2GroupPath = await _mainQueries.Provisioning.GetGroupTreeByGroupName(aor2ExternalId);
                    var expectedAor2Path = $"/Root/Agencies/Bucket 1/{agencyName}/{aor2ExternalId}/";

                    var aor1GroupPath = await _mainQueries.Provisioning.GetGroupTreeByGroupName(aor1ExternalId);
                    var expectedAor1Path = $"/Root/Agencies/Bucket 1/{agencyName}/{aor1ExternalId}/";

                    Assert.Multiple(() =>
                    {
                        Assert.That(aor1GroupPath == expectedAor1Path, Is.True,
                            $"Failed, aor1 group path should be {expectedAor1Path}, actual: {aor1GroupPath}");
                        Assert.That(aor2GroupPath == expectedAor2Path, Is.True,
                            $"Failed, aor2 group path should be {expectedAor2Path}, actual: {aor2GroupPath}");
                    });
                });

                await _logger.ExecuteStepAsync("Validate user-group links for AOR1", async () =>
                {
                    var aor1UserData = await _mainQueries.Provisioning.GetUserAccessGroup(aor1ExternalId);
                    var aor1UserIds = aor1UserData.userExternalIds.ToList();
                    var aor1GroupIds = aor1UserData.groupExternalIds;

                    Assert.Multiple(() =>
                    {
                        Assert.That(aor1UserIds.Count == 2, Is.True,
                            $"Failed, only 2 users should be linked to the group, actual: {aor1UserIds.Count}");
                        Assert.That(aor1UserIds.Contains(lspUser1ExternalId) || aor1UserIds.Contains(agencyName), Is.True,
                            $"Failed, {lspUser1ExternalId} or {agencyName} should be linked to the group, actual: {string.Join(", ", aor1UserIds)}");
                        Assert.That(aor1GroupIds.All(e => e == aor1ExternalId), Is.True,
                            $"Failed, all group ids should be {aor1ExternalId}");
                    });
                });

                await _logger.ExecuteStepAsync("Validate user-group links for AOR2", async () =>
                {
                    var aor2UserData = await _mainQueries.Provisioning.GetUserAccessGroup(aor2ExternalId);
                    var aor2UserIds = aor2UserData.userExternalIds.ToList();
                    var aor2GroupIds = aor2UserData.groupExternalIds;

                    Assert.Multiple(() =>
                    {
                        Assert.That(aor2UserIds.Count == 2, Is.True,
                            $"Failed, only 2 users should be linked to the group, actual: {aor2UserIds.Count}");
                        Assert.That(aor2UserIds.Contains(lspUser2ExternalId) || aor2UserIds.Contains(agencyName), Is.True,
                            $"Failed, {lspUser2ExternalId} or {agencyName} should be linked to the group, actual: {string.Join(", ", aor2UserIds)}");
                        Assert.That(aor2GroupIds.All(e => e == aor2ExternalId), Is.True,
                            $"Failed, all group ids should be {aor2ExternalId}");
                    });
                });

                // Replace the Task.Delay with the new ProvisioningHelper
                var provisioningResults = await _logger.ExecuteStepAsync("Wait for provisioning actions to complete", async () =>
                {
                    var groupIds = new[] { aor1ExternalId, aor2ExternalId };
                    var userIds = new[] { agencyName, lspUser1ExternalId, lspUser2ExternalId };
                    
                    return await _mainQueries.ProvisioningLogic.WaitForProvisioningActionsAsync(
                        groupIds, userIds, timeoutSeconds: 420); // 7 minutes total timeout
                }, "Expected result: All provisioning actions completed successfully");

                // Simplified validation - the helper already verified success
                await _logger.ExecuteStepAsync("Validate provisioning action statuses", async () =>
                {
                    // Log the results for transparency
                    foreach (var result in provisioningResults)
                    {
                        _logger.Info($"{result.EntityType} {result.EntityId}: Status = {result.Status}");
                        
                        Assert.That(result.IsSuccessful, Is.True,
                            $"Failed, provision action status should be success for {result.EntityType.ToLower()} {result.EntityId}, error: {result.RetryData}, status: {result.Status}");
                    }
                }, "Expected result: All provisioning actions have 'Success' status");
            
        }

        [Test]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [Category("Provision")]
        [TestCaseId(230100)]
        [Description("Validates provisioning of Progressive users. It creates a user via Platform API, updates the user's information, verifies the update, and then deletes the user.")]
        public async Task PGR_Provisioning_Test()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            var userModel = ProvisioningDataProvider.CreateProgressiveUserModel();
            var platformApi = await _platformApiFactory.CreateApiClientAsync();

            await _logger.ExecuteStepAsync("Create user via Platform API", async () =>
            {
                var createUserResponse = await platformApi.CreateUser(userModel);
                Assert.That(createUserResponse.IsSuccessStatusCode, Is.True,
                    $"Failed, status code should be 200/OK, actual {createUserResponse.StatusCode}");
            });

            await _logger.ExecuteStepAsync("Update user via Platform API", async () =>
            {
                userModel.Name.FamilyName = "update" + RandomManager.GetRandomString(6);
                var updateUserResponse = await platformApi.UpdateUser(userModel.Id, userModel);
                Assert.That(updateUserResponse.IsSuccessStatusCode, Is.True,
                    $"Failed, status code should be 200/OK, actual {updateUserResponse.StatusCode}");
            });

            await _logger.ExecuteStepAsync("Verify user update via GetUsers", async () =>
            {
                var getUsersResponse = await platformApi.GetUsers();
                Assert.That(getUsersResponse.IsSuccessStatusCode, Is.True,
                    $"Failed, status code should be 200/OK, actual {getUsersResponse.StatusCode}");

                var actualUser = getUsersResponse.Content!.Resources!.FirstOrDefault(e => e.Id == userModel.Id);

                Assert.That(actualUser is not null, Is.True,
                    "created user is not found after getUsers request");
                Assert.That(actualUser.Name.FamilyName == userModel.Name.FamilyName, Is.True,
                    $"Failed,  user's name is not updated. actual {actualUser.Name.FamilyName}, expected {userModel.Name.FamilyName}");
            });

            await _logger.ExecuteStepAsync("Delete user via Platform API", async () =>
            {
                var deleteUserResponse = await platformApi.DeleteUser(userModel.Id);
                Assert.That(deleteUserResponse.IsSuccessStatusCode, Is.True,
                    $"Failed, status code should be 200/OK, actual {deleteUserResponse.StatusCode}");
            });
        }
    }
}
