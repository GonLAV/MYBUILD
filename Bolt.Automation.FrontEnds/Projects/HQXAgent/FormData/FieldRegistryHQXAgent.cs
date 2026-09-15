using Bolt.Automation.Common.Enums;
using Bolt.Automation.FrontEnds.FormData;
using Bolt.Automation.FrontEnds.FormData.Base;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.FormData.FieldsRegistry;

namespace Bolt.Automation.FrontEnds.Projects.HQXAgent.FormData
{
    public static class FieldRegistryHQXAgent 
    {
        [FieldRegistry]
        public static readonly Dictionary<string, UIElement> Fields =
              CustomerIntroFields.Fields
              .Concat(DiscountsFields.Fields)
              .Concat(ExteriorFields.Fields)
              .Concat(FinalDetailsFields.Fields)
              .Concat(InteriorFields.Fields)
              .Concat(OverviewFields.Fields)
              .Concat(OwnerFields.Fields)
              .Concat(TriageFields.Fields)
              .Concat(SelectedCarrierFields.Fields)
              .Concat(CarrierQuestionsFields.Fields)
              .Concat(NoteFields.Fields)
              .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

       
        static FieldRegistryHQXAgent() => FieldRegistryProvider.Register(FrontEndType.HQXAgent, Fields);
    }
}