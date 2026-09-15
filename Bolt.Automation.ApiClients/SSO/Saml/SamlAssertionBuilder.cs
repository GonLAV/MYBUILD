using System.Xml;

namespace Bolt.Automation.ApiClients.SSO.Saml
{
    public class SamlAssertionBuilder(SamlSigner signer)
    {
        public XmlNode InsertData(XmlDocument saml, XmlNamespaceManager nsmgr)
        {
            saml.DocumentElement.Attributes["Destination"].Value = signer.Recipient?.Destination;
            saml.DocumentElement.Attributes["ID"].Value = signer.ResponseId;
            saml.DocumentElement.Attributes["IssueInstant"].Value = signer.IssueInstant;

            UpdateValue(saml, nsmgr, "/samlp:Response/saml:Assertion/saml:Subject/saml:NameID", signer.NameId);
            UpdateValue(saml, nsmgr, "/samlp:Response/saml:Issuer", signer.Issuer);
            UpdateValue(saml, nsmgr, "/samlp:Response/saml:Assertion/saml:Issuer", signer.Issuer);
            UpdateValue(saml, nsmgr, "/samlp:Response/saml:Assertion/@ID", signer.AssertionId);
            UpdateValue(saml, nsmgr, "/samlp:Response/saml:Assertion/@IssueInstant", signer.IssueInstant);
            UpdateValue(saml, nsmgr, "/samlp:Response/saml:Assertion/saml:Subject/saml:SubjectConfirmation/saml:SubjectConfirmationData/@NotOnOrAfter", signer.NotOnOrAfter);
            UpdateValue(saml, nsmgr, "/samlp:Response/saml:Assertion/saml:Subject/saml:SubjectConfirmation/saml:SubjectConfirmationData/@Recipient", signer.Recipient?.Recipient);
            UpdateValue(saml, nsmgr, "/samlp:Response/saml:Assertion/saml:Conditions/@NotBefore", signer.NotBefore);
            UpdateValue(saml, nsmgr, "/samlp:Response/saml:Assertion/saml:Conditions/@NotOnOrAfter", signer.NotOnOrAfter);
            UpdateValue(saml, nsmgr, "/samlp:Response/saml:Assertion/saml:Conditions/saml:AudienceRestriction/saml:Audience", signer.Recipient?.Audience);

            XmlNode attributeStatement = saml.SelectSingleNode("/samlp:Response/saml:Assertion/saml:AttributeStatement", nsmgr);
            return attributeStatement;
        }

        private static void UpdateValue(XmlDocument saml, XmlNamespaceManager nsmgr, string xpath, string val)
        {
            XmlNode node = saml.DocumentElement.SelectSingleNode(xpath, nsmgr);
            if (node is XmlAttribute)
                node.Value = val;
            else if (node is XmlElement)
                node.InnerText = val;
        }
    }
}

