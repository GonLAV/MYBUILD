using Bolt.Automation.ApiClients.CaseManagerApi.Entities.Organization.CreateOrganization;
using Bolt.Automation.ApiClients.CaseManagerApi.Entities.Organization.UpdateOrganization;
using Bolt.Automation.ApiClients.CaseManagerApi.Entities.Organization.CreateOrganizationLetter;
using Bolt.Automation.ApiClients.CaseManagerApi.Entities.Organization.EnOForm;
using Bolt.Automation.ApiClients.CaseManagerApi.Entities.Organization.W9Form;
using Bolt.Automation.ApiClients.CaseManagerApi.Entities.Organization._1099Form.Create;
using Bolt.Automation.ApiClients.CaseManagerApi.Entities.Organization._1099Form.Delete;
using Bolt.Automation.ApiClients.CaseManagerApi.Entities.Organization.License.CreateOrUpdate;
using Bolt.Automation.ApiClients.CaseManagerApi.Entities.Organization.License.Delete;
using Refit;

namespace Bolt.Automation.ApiClients.CaseManagerApi.Interfaces
{
    public partial interface ICaseManagerOrganizationsApi
    {
        [Post("/{tenant}/organizations")]
        Task<ApiResponse<CreateOrganizationResponse>> CreateOrganizationAsync([AliasAs("tenant")] string tenant, [Body] CreateOrganizationRequest request);

        [Put("/{tenant}/organizations/{externalId}")]
        Task<ApiResponse<UpdateOrganizationResponse>> UpdateOrganizationAsync(string externalId, [AliasAs("tenant")] string tenant, [Body] UpdateOrganiztionRequest request);

        [Post("/{tenant}/organizations/{externalId}/create-letter")]
        Task<ApiResponse<CreateOrganizationLetterResponse>> CreateOrganizationLetterAsync(string externalId, [AliasAs("tenant")] string tenant, [Body] CreateOrganizationLetterRequest request);

        [Post("/{tenant}/organizations/{externalId}/eno-form")]
        Task<ApiResponse<AddOrUpdateSubtenantEnOFormResponse>> AddOrUpdateSubtenatEnOFormAsync(string externalId, [AliasAs("tenant")] string tenant, [Body] AddOrUpdateSubtenantEnOFormRequest request);

        [Post("/{tenant}/organizations/{externalId}/w9-form")]
        Task<ApiResponse<AddOrUpdateSubtenatW9FormResponse>> AddOrUpdateSubtenatW9FormAsync(string externalId, [AliasAs("tenant")] string tenant, [Body] AddOrUpdateSubtenatW9FormRequest request);

        [Post("/{tenant}/organizations/{externalId}/1099-form")]
        Task<ApiResponse<AddSubtenat1099FormResponse>> AddSubtenat1099FormAsync(string externalId, [AliasAs("tenant")] string tenant, [Body] AddSubtenat1099FormRequest request);

        [Post("/{tenant}/organizations/{externalId}/delete-1099-form")]
        Task<ApiResponse<DeleteSubtenant1099FormResponse>> DeleteSubtenat1099FormAsync(string externalId, [AliasAs("tenant")] string tenant, [Body] DeleteSubtenant1099FormRequest request);

        [Post("/{tenant}/organizations/{externalId}/licenses")]
        Task<ApiResponse<AddUpdateLicenseResponse>> AddUpdateLicenseAsync(string externalId, [AliasAs("tenant")] string tenant, [Body] AddUpdateLicenseRequest request);

        [Delete("/{tenant}/organizations/{externalId}/licenses")]
        Task<ApiResponse<DeleteLicensesResponse>> DeleteLicensesAsync(string externalId, [AliasAs("tenant")] string tenant, [Body] DeleteLicensesRequest request);
    }
}
