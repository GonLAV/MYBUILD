using System.Text;
using System.Web;
using System.Xml;

namespace Bolt.Automation.ApiClients.SSO.Saml
{
    public static class SamlEncoder
    {
        public static string Encode(XmlDocument saml)
        {
            var strAscii = saml.InnerXml;
            var bytes = Encoding.ASCII.GetBytes(strAscii);
            var result = Convert.ToBase64String(bytes);
            return HttpUtility.UrlEncode(result);
        }
    }
}

