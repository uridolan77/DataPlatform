namespace GenericDataPlatform.Common.Security
{
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Implementation of ICertificateManager for managing certificates
    /// </summary>
    public class CertificateManager : ICertificateManager
    {
        /// <summary>
        /// Creates a self-signed certificate for development purposes
        /// </summary>
        /// <param name="subjectName">The subject name for the certificate</param>
        /// <returns>A self-signed certificate</returns>
        public X509Certificate2 CreateSelfSignedCertificate(string subjectName)
        {
            // This is a simplified implementation for demonstration purposes
            // In a production environment, you would use a more robust approach
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest(
                $"CN={subjectName}",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            var certificate = request.CreateSelfSigned(
                DateTimeOffset.Now.AddDays(-1),
                DateTimeOffset.Now.AddYears(1));

            return new X509Certificate2(certificate.Export(X509ContentType.Pfx));
        }

        /// <summary>
        /// Gets a certificate by thumbprint from the certificate store
        /// </summary>
        /// <param name="thumbprint">The certificate thumbprint</param>
        /// <returns>The certificate if found, or null</returns>
        public X509Certificate2? GetCertificateByThumbprint(string thumbprint)
        {
            using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);

            var certificates = store.Certificates.Find(
                X509FindType.FindByThumbprint,
                thumbprint,
                validOnly: false);

            return certificates.Count > 0 ? certificates[0] : null;
        }

        /// <summary>
        /// Gets a certificate from a file
        /// </summary>
        /// <param name="filePath">Path to the certificate file</param>
        /// <param name="password">Password for the certificate</param>
        /// <returns>The certificate</returns>
        public X509Certificate2 GetCertificateFromFile(string filePath, string password)
        {
            return new X509Certificate2(filePath, password);
        }
    }
}