using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using Bolt.Microservice.Cryptography.Interfaces;
using Bolt.Microservices.Entities.Cryptography;

namespace Bolt.Automation.ApiClients.SSO.Saml
{
    public class SamlSigner
    {
        private X509Certificate2? _cert;
        private XmlDocument _samlTemplate = new();
        public const string CertFriendlyNameForSinging = "BoltPrivate";

        public SamlRecipient? Recipient { get; set; }
        public string? Issuer { get; set; }
        public string? NameId { get; set; }
        public string? GroupExternalId { get; set; }
        public string? Source { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }

        private const string SamlTimeFormat = "yyyy-MM-ddTHH:mm:ssZ";

        /// <summary>
        /// Minutes of clock-skew tolerance before <see cref="IssueInstant"/>. Kept
        /// at the long-standing 10 so an assertion is never rejected by a verifier
        /// whose clock runs slightly behind ours.
        /// </summary>
        private const int SkewToleranceMinutes = 10;

        private readonly Guid _responseId = Guid.NewGuid();
        private readonly DateTime _mintedAt = DateTime.UtcNow;
        public readonly string AssertionId = $"_{Guid.NewGuid()}";

        /// <summary>
        /// Minutes after <see cref="IssueInstant"/> that the assertion stays valid.
        /// Defaults to 10, keeping the original fixed +/-10 window for every
        /// existing caller. Raise it only for a caller that replays one assertion
        /// across many requests, such as a k6 load run — there is no renewal, so
        /// this is a hard ceiling on that run's length.
        /// </summary>
        public const int DefaultValidMinutes = 10;

        public int ValidMinutes { get; set; } = DefaultValidMinutes;

        private readonly SamlAssertionBuilder _assertionBuilder;

        public string ResponseId => _responseId.ToString();
        public string IssueInstant => _mintedAt.ToString(SamlTimeFormat);
        public string NotBefore => _mintedAt.AddMinutes(-SkewToleranceMinutes).ToString(SamlTimeFormat);
        public string NotOnOrAfter => _mintedAt.AddMinutes(ValidMinutes).ToString(SamlTimeFormat);

        public SamlSigner()
        {
            _assertionBuilder = new SamlAssertionBuilder(this);
        }

        private void SetCert(string subject)
        {
            if (subject != "Default")
            {
                _cert = CertificateLoader.LoadFromStore(subject);
            }
            else
            {
                var password = (Issuer == "Farmers" && Recipient?.Destination == "https://sts-kraftlake.boltinc.com/SSO/index")
                    ? "IKJH!Snc098iohv98uioevefv!" : "Lk8SE3y5g";
                _cert = CertificateLoader.LoadFromResource("Bolt.Automation.ApiClients.SSO.Saml.Resources.ams360test.boltinsurance.com.p12", password);
            }
        }

        public XmlDocument GetSaml()
        {
            XmlDocument saml = new();
            saml.LoadXml(_samlTemplate.OuterXml);
            XmlNamespaceManager nsmgr = new(saml.NameTable);
            nsmgr.AddNamespace("samlp", "urn:oasis:names:tc:SAML:2.0:protocol");
            nsmgr.AddNamespace("saml", "urn:oasis:names:tc:SAML:2.0:assertion");

            _assertionBuilder.InsertData(saml, nsmgr);
            XmlDocument signedSaml = new()
            {
                PreserveWhitespace = false
            };
            signedSaml.LoadXml(saml.OuterXml);

            SignSaml(signedSaml, nsmgr, AssertionId);

            return signedSaml;
        }

        public XmlDocument GetSaml(ICryptographyService cryptoService)
        {
            XmlDocument saml = new();
            saml.LoadXml(_samlTemplate.OuterXml);
            XmlNamespaceManager nsmgr = new(saml.NameTable);
            nsmgr.AddNamespace("samlp", "urn:oasis:names:tc:SAML:2.0:protocol");
            nsmgr.AddNamespace("saml", "urn:oasis:names:tc:SAML:2.0:assertion");
            _assertionBuilder.InsertData(saml, nsmgr);

            XmlNode assertion = saml.DocumentElement.SelectSingleNode("/samlp:Response/saml:Assertion", nsmgr);
            var assertionId = assertion.Attributes["ID"].Value;

            //var signedElementRes = cryptoService.SignSaml(new SignSamlRequest(saml.OuterXml, assertionId, CertFriendlyNameForSinging));
            var signedElementRes = Task.Run(() =>
                cryptoService.SignSaml(new SignSamlRequest(saml.OuterXml, assertionId, CertFriendlyNameForSinging))
                ).Result;
            var signedElement = signedElementRes.Value;
            var signedElementStr = signedElement.OuterXml.Replace("<Signature>", "<Signature xmlns=\"http://www.w3.org/2000/09/xmldsig#\">");

            var newSignedElement = new XmlDocument();
            newSignedElement.LoadXml(signedElementStr);
            var importedNode = assertion.OwnerDocument.ImportNode(newSignedElement.DocumentElement, true);

            XmlNode issuer = assertion.SelectSingleNode("./saml:Issuer", nsmgr);
            assertion.InsertAfter(importedNode, issuer);

            return saml;
        }

        private void SignSaml(XmlDocument saml, XmlNamespaceManager nsmgr, string assertionId)
        {
            SignedXml signedXml = new(saml)
            {
                SigningKey = _cert.GetRSAPrivateKey()    // *** The actual key for signing - MAKE SURE THIS ISN'T NULL!    
            };

            KeyInfo keyInfo = new();     // *** Create a KeyInfo structure  
            KeyInfoX509Data keyInfoData = new();    // *** Specifically use the issuer and serial number for the data rather than the default    
            keyInfoData.AddCertificate(_cert);
            keyInfo.AddClause(keyInfoData);
            signedXml.KeyInfo = keyInfo;      // *** provide the certficate info that gets embedded - note this is only for specific formatting of the message to provide the cert info    

            signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;     // *** Again unusual - meant to make the document match template    
            Reference reference = new()
            {
                Uri = "#" + assertionId
            };    // *** Now create reference to sign: Point at the Body element    
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            reference.AddTransform(new XmlDsigExcC14NTransform());  // reference id=body section in same doc    
            signedXml.AddReference(reference);     // required to match doc    
            signedXml.ComputeSignature();    // *** Finally create the signature    
            XmlElement signedElement = signedXml.GetXml();     // *** Result is an XML node with the signature detail below it. Now let's add the sucker into the SOAP-HEADER
            signedElement.Prefix = "ds";

            XmlNode assertion = saml.DocumentElement.SelectSingleNode("/samlp:Response/saml:Assertion", nsmgr);
            XmlNode issuer = assertion.SelectSingleNode("./saml:Issuer", nsmgr);
            assertion.InsertAfter(signedElement, issuer);
        }

        public string SendSamlProduct()
        {
            SetCert("Default");
            var saml = GetSaml();
            return SamlEncoder.Encode(saml);
        }

        public string SendSamlProduct(ICryptographyService cryptoService)
        {
            SetCert("Default");
            var saml = GetSaml(cryptoService);
            return SamlEncoder.Encode(saml);
        }


        public XmlDocument GetXmlTemplateFromResource(string templateName)
        {
            XmlDocument xmlTemplate = new();
            var stringTemplate = Resources.Resources.ResourceManager.GetString(templateName)
                ?? throw new ArgumentNullException(nameof(templateName), $@"The resource '{templateName}' could not be found.");
            xmlTemplate.LoadXml(stringTemplate);
            return xmlTemplate;
        }

        public void SetSamlTemplateFromResource(string templateName)
        {
            _samlTemplate = SamlTemplateManager.LoadTemplateFromResource(templateName);
        }

        public void SetSamlTemplate(XmlDocument samlTemplate)
        {
            _samlTemplate = samlTemplate;
        }
    }
}
