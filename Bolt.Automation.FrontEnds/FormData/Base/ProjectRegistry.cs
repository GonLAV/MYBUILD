using Bolt.Automation.Common.Enums;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.Flows;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.FormData;

namespace Bolt.Automation.FrontEnds.FormData.Base
{
    public class ProjectInfo
    {
        public FrontEndType Type { get; init; }
        public Type? FieldRegistryType { get; init; }
        public Type? FlowsType { get; init; }
    }

    public static class ProjectRegistry
    {
        private static readonly Dictionary<FrontEndType, ProjectInfo> _projects = new()
        {
            [FrontEndType.HQXConsumer] = new ProjectInfo
            {
                Type = FrontEndType.HQXConsumer,
                FieldRegistryType = typeof(Projects.HQXConsumer.FormData.FieldRegistryHQXConsumer),
                FlowsType = typeof(Projects.HQXConsumer.Flows.Flows)
            },
            [FrontEndType.HQXAgent] = new ProjectInfo
            {
                Type = FrontEndType.HQXAgent,
                FieldRegistryType = typeof(FieldRegistryHQXAgent),
                FlowsType = typeof(Flows)
            },
            [FrontEndType.D2C] = new ProjectInfo
            {
                Type = FrontEndType.D2C,
                FieldRegistryType = typeof(Projects.D2C.FormData.FieldRegistryD2C),
                FlowsType = typeof(Projects.D2C.Flows.Flows)
            },
            [FrontEndType.ADBX] = new ProjectInfo
            {
                Type = FrontEndType.ADBX,
                FieldRegistryType = typeof(Projects.ADBX.FormData.FieldRegistryADBX)
            },
            [FrontEndType.PartnerPortal] = new ProjectInfo
            {
                Type = FrontEndType.PartnerPortal,
                FieldRegistryType = typeof(Projects.PartnerPortal.FormData.FieldRegistryPartnerPortal)
            },
            [FrontEndType.Interview] = new ProjectInfo
            {
                Type = FrontEndType.Interview,
                FieldRegistryType = typeof(Projects.Interview.FormData.FieldRegistryInterview),
                FlowsType = typeof(Projects.Interview.Flows.Flows)
            }
        };

        public static ProjectInfo GetProject(FrontEndType type)
        {
            if (_projects.TryGetValue(type, out var project))
                return project;
            throw new ArgumentOutOfRangeException(nameof(type), $"Unknown project type: {type}");
        }

        public static IEnumerable<ProjectInfo> GetAllProjects() => _projects.Values;
    }
}