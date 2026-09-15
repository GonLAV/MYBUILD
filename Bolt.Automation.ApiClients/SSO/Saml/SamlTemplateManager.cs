using System.Xml;

namespace Bolt.Automation.ApiClients.SSO.Saml
{
    public static class SamlTemplateManager
    {
        public static XmlDocument LoadTemplateFromResource(string templateName)
        {
            var stringTemplate = Resources.Resources.ResourceManager.GetString(templateName)
                ?? throw new ArgumentNullException(nameof(templateName), $@"The resource '{templateName}' could not be found.");
            var xmlTemplate = new XmlDocument();
            xmlTemplate.LoadXml(stringTemplate);
            return xmlTemplate;
        }
    }
}

