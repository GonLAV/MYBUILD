using Bolt.Automation.Common.Enums;
using Bolt.Automation.FrontEnds.FormData;
using Bolt.Automation.FrontEnds.FormData.Base;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.FormData.FieldsRegistry;

namespace Bolt.Automation.FrontEnds.Projects.HQXConsumer.FormData
{
    public static class FieldRegistryHQXConsumer
    {
        [FieldRegistry]
        public static readonly Dictionary<string, UIElement> Fields =
            PropertyFields.Fields
            .Concat(InteriorFields.Fields)
            .Concat(ExteriorFields.Fields)
            .Concat(PersonalInfoFields.Fields)
            .Concat(DetailsFields.Fields)
            .Concat(GeneralConsumerFields.Fields)
            .Concat(DiscountsFields.Fields)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        static FieldRegistryHQXConsumer() => FieldRegistryProvider.Register(FrontEndType.HQXConsumer, Fields);
    }
}
