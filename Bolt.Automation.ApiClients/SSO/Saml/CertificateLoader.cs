using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace Bolt.Automation.ApiClients.SSO.Saml
{
    public static class CertificateLoader
    {
        public static X509Certificate2? LoadFromStore(string subject)
        {
            using var store = new X509Store(StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            foreach (var cer in store.Certificates.Where(cer => cer.SubjectName.Name == subject))
            {
                var contentType = X509Certificate2.GetCertContentType(cer.RawData);
                if (contentType == X509ContentType.Cert)
                    return X509CertificateLoader.LoadCertificate(cer.RawData);
                if (contentType == X509ContentType.Pkcs12)
                    return X509CertificateLoader.LoadPkcs12(cer.RawData, null, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
#pragma warning disable SYSLIB0057
                return new X509Certificate2(cer.RawData, (string?)null, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
#pragma warning restore SYSLIB0057
            }
            return null;
        }

        public static X509Certificate2 LoadFromResource(string resourceName, string password)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var certStream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"The resource stream for '{resourceName}' could not be found.");
            var certBytes = new byte[certStream.Length];
            certStream.ReadExactly(certBytes, 0, certBytes.Length);
            var contentType = X509Certificate2.GetCertContentType(certBytes);
            if (contentType == X509ContentType.Pkcs12)
                return X509CertificateLoader.LoadPkcs12(certBytes, password, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
            if (contentType == X509ContentType.Cert)
                return X509CertificateLoader.LoadCertificate(certBytes);
#pragma warning disable SYSLIB0057
            return new X509Certificate2(certBytes, password, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
#pragma warning restore SYSLIB0057
        }
    }
}

