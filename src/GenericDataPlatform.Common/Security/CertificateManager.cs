namespace GenericDataPlatform.Common.Security
{
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Implementation of ICertificateManager for managing certificates
    /// </summary>
    public class CertificateManager : ICertificateManager
    {
        private readonly ILogger<CertificateManager>? _logger;
        private X509Certificate2? _caCertificate;
        private X509Certificate2? _serverCertificate;
        private X509Certificate2? _clientCertificate;

        public CertificateManager(ILogger<CertificateManager>? logger = null)
        {
            _logger = logger;
            InitializeCertificates();
        }

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

        /// <summary>
        /// Gets the CA certificate
        /// </summary>
        /// <returns>The CA certificate</returns>
        public X509Certificate2 GetCACertificate()
        {
            if (_caCertificate == null)
            {
                _caCertificate = CreateSelfSignedCertificate("GenericDataPlatform.CA");
                _logger?.LogInformation("Created new CA certificate");
            }
            return _caCertificate;
        }

        /// <summary>
        /// Gets the server certificate
        /// </summary>
        /// <returns>The server certificate</returns>
        public X509Certificate2 GetServerCertificate()
        {
            if (_serverCertificate == null)
            {
                _serverCertificate = CreateSelfSignedCertificate("GenericDataPlatform.Server");
                _logger?.LogInformation("Created new server certificate");
            }
            return _serverCertificate;
        }

        /// <summary>
        /// Gets the client certificate
        /// </summary>
        /// <returns>The client certificate</returns>
        public X509Certificate2 GetClientCertificate()
        {
            if (_clientCertificate == null)
            {
                _clientCertificate = CreateSelfSignedCertificate("GenericDataPlatform.Client");
                _logger?.LogInformation("Created new client certificate");
            }
            return _clientCertificate;
        }

        /// <summary>
        /// Initialize certificates
        /// </summary>
        private void InitializeCertificates()
        {
            try
            {
                // In a real implementation, you would load certificates from a secure store
                // or create them if they don't exist
                _caCertificate = CreateSelfSignedCertificate("GenericDataPlatform.CA");
                _serverCertificate = CreateSelfSignedCertificate("GenericDataPlatform.Server");
                _clientCertificate = CreateSelfSignedCertificate("GenericDataPlatform.Client");

                _logger?.LogInformation("Initialized certificates");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize certificates");
                throw;
            }
        }
    }
}