using System.Xml;
using Bolt.Automation.Common.Models.RelayStates;
using Bolt.Automation.Common.Models.Urls;
using Bolt.Automation.Common.Models.Users;
using Bolt.Microservices.Entities.Cryptography;

namespace Bolt.Automation.ApiClients.SSO.Saml
{
    public class SsoHelper
    {
        private readonly SamlSigner samlSigner = new();

        /// <param name="validMinutes">
        /// Optional validity-window override, in minutes. Null means the signer's
        /// default, so existing callers are unaffected.
        /// </param>
        public virtual string GetSsoSignedSaml(UserTestData user,UrlTestData url, RelayStateTestData relayState,string samlTemp, ICryptographyService? cryptographyService = null, int? validMinutes = null)
        {
            // Assigned unconditionally: samlSigner is a field shared by every call
            // on this helper, so a null must RESET the window. Setting it only when
            // a value is supplied would leak one caller's long window into the next
            // caller's assertion.
            samlSigner.ValidMinutes = validMinutes ?? SamlSigner.DefaultValidMinutes;

            var destination = url.EndpointSso ?? string.Empty;
            var recipient = url.Recipient ?? string.Empty;
            var audience = user.Sso.Audience ?? string.Empty;

            var samlRecipient = new SamlRecipient(destination, recipient, audience);
            samlSigner.GroupExternalId = user.GroupExternalId ?? string.Empty;
            samlSigner.NameId = user.UserExternalId ?? string.Empty;
            samlSigner.Recipient = samlRecipient;
            samlSigner.Issuer = user.Sso.Issuer ?? string.Empty;
            var samlXML = samlSigner.GetXmlTemplateFromResource(samlTemp);
            var elemlist = samlXML.GetElementsByTagName("saml:Attribute");
            foreach (XmlNode samlAttribute in elemlist)
            {
                var attributeName = samlAttribute.Attributes?[0]?.Value;
                if (attributeName is "GroupExternalId")
                {
                    var attributeValue = samlAttribute.ChildNodes?[0];
                    if (attributeValue != null)
                    {
                        attributeValue.InnerText = samlSigner.GroupExternalId;
                    }
                }
            }

            // Set the template directly from XmlDocument
            samlSigner.SetSamlTemplate(samlXML);

            string relayStateSaml;

            if (cryptographyService is not null)
            {
                samlSigner.Source = user.Source ?? string.Empty;
                samlSigner.FirstName = user.FirstName ?? string.Empty;
                samlSigner.LastName = user.LastName ?? string.Empty;
                samlSigner.Email = user.Email ?? string.Empty;
                samlSigner.Phone = user.Phone ?? string.Empty;
                relayStateSaml = $"RelayState={relayState?.Value}&SAMLResponse=" + samlSigner.SendSamlProduct(cryptographyService);
            }
            else
            {
                relayStateSaml = $"RelayState={relayState?.Value}&SAMLResponse=" + samlSigner.SendSamlProduct();
            }

            return relayStateSaml;
        }
    }
}
