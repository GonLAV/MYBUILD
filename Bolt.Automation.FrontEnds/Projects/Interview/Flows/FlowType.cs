namespace Bolt.Automation.FrontEnds.Projects.Interview.Flows
{
    /// <summary>
    /// Defines the available flow types for Interview v3.
    /// </summary>
    public enum FlowType
    {
        /// <summary>
        /// HO3 Homeowners flow
        /// </summary>
        InterviewHO3Flow,

        /// <summary>
        /// Personal-lines property flow as an agent drives it from ADBX — the Start page carries the
        /// LOB tiles (no separate Lobs page) and the Markets page sits between Start and Home. The page
        /// sequence is the same for HO3 and HO6; the caller supplies the LOB.
        /// </summary>
        InterviewPLAgentFlow,

        /// <summary>
        /// HO4 Renters flow
        /// </summary>
        InterviewHO4Flow,

        /// <summary>
        /// Personal Auto flow
        /// </summary>
        InterviewAutoFlow,

        /// <summary>
        /// Personal Auto as an agent resumes it from a GetQuote-API-created application: the API
        /// already supplied applicant, operators, vehicles and policy, so the interview renders only
        /// Start → Markets → Results. Walking it with fillForms:false is deliberate — re-filling would
        /// overwrite the Rule-Engine-carried data this flow exists to verify.
        /// </summary>
        InterviewPLAgentAutoFlow,

        /// <summary>
        /// Bundle (Auto + Home) flow
        /// </summary>
        InterviewBundleFlow,

        /// <summary>
        /// Workers Compensation flow
        /// </summary>
        InterviewWCFlow,

        /// <summary>
        /// General Liability flow
        /// </summary>
        InterviewGLFlow,

   /// <summary>
        /// Business Owners Policy Old flow
        /// </summary>
        InterviewBOPOldFlow,

        /// <summary>
        /// Business Owners Policy flow
        /// </summary>
        InterviewBOPFlow,

        /// <summary>
        /// Flood insurance flow
        /// </summary>
        InterviewFloodFlow,

        /// <summary>
        /// Motorcycle flow
        /// </summary>
        InterviewMotorcycleFlow,

        /// <summary>
        /// Commercial Line Auto flow (KLX old interview)
        /// </summary>
        InterviewCLAutoFlow,

        /// <summary>
        /// Commercial Line Auto flow (boltaccess)
        /// </summary>
        InterviewBoltAccessCLAutoFlow,

        /// <summary>
        /// Commercial Line General Liability flow (boltaccess)
        /// </summary>
        InterviewBoltAccessGLFlow,

        /// <summary>
        /// Commercial Line Workers Compensation flow — the ProductBusinessProfilePageCL/
        /// ProductSelectionPageCL "CL new flow" page set (same shape as InterviewBOPFlow),
        /// with WorkersCompensation LOB defaults instead of BusinessOwners.
        /// </summary>
        InterviewWCFlowNew,

        /// <summary>
        /// Commercial Line Commercial Auto flow — the ProductBusinessProfilePageCL/
        /// ProductSelectionPageCL "CL new flow" page set (same shape as InterviewBOPFlow),
        /// with CommercialAuto LOB defaults instead of BusinessOwners.
        /// </summary>
        InterviewCLAutoFlowNew
    }
}
