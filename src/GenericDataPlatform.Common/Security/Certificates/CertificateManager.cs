using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GenericDataPlatform.Common.Security.Certificates
{
    /// <summary>
    /// Manages certificates for mTLS
    /// </summary>
    public class CertificateManager : ICertificateManager
    {
        private readonly CertificateOptions _options;
        private readonly ILogger<CertificateManager> _logger;
        private X509Certificate2 _caCertificate;
        private X509Certificate2 _serverCertificate;
        private X509Certificate2 _clientCertificate;

        public CertificateManager(IOptions<CertificateOptions> options, ILogger<CertificateManager> logger)
        {
            _options = options.Value;
            _logger = logger;
            Initialize();
        }

        /// <summary>
        /// Gets the CA certificate
        /// </summary>
        public X509Certificate2 GetCACertificate() => _caCertificate;

        /// <summary>
        /// Gets the server certificate
        /// </summary>
        public X509Certificate2 GetServerCertificate() => _serverCertificate;

        /// <summary>
        /// Gets the client certificate
        /// </summary>
        public X509Certificate2 GetClientCertificate() => _clientCertificate;

        /// <summary>
        /// Gets a certificate by thumbprint from the certificate store
        /// </summary>
        public X509Certificate2 GetCertificateByThumbprint(string thumbprint)
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
        public X509Certificate2 GetCertificateFromFile(string filePath, string password)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Certificate file not found: {filePath}");
            }

            try
            {
                return new X509Certificate2(filePath, password);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load certificate from file: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// Creates a self-signed certificate
        /// </summary>
        public X509Certificate2 CreateSelfSignedCertificate(string subjectName)
        {
            return CertificateGenerator.GenerateClientCertificate(
                _caCertificate, 
                subjectName, 
                _options.CertificateValidityInDays);
        }

        /// <summary>
        /// Initializes the certificate manager
        /// </summary>
        private void Initialize()
        {
            try
            {
                // Create directory if it doesn't exist
                Directory.CreateDirectory(_options.CertificateDirectory);

                // CA certificate
                var caPath = Path.Combine(_options.CertificateDirectory, "ca.pfx");
                if (File.Exists(caPath))
                {
                    _caCertificate = new X509Certificate2(caPath, _options.CertificatePassword);
                    _logger.LogInformation("Loaded CA certificate from {Path}", caPath);
                }
                else
                {
                    _caCertificate = CertificateGenerator.GenerateCA($"{_options.ServiceName}.ca", _options.CertificateValidityInDays);
                    CertificateGenerator.ExportToPfx(_caCertificate, caPath, _options.CertificatePassword);
                    _logger.LogInformation("Generated new CA certificate and saved to {Path}", caPath);
                }

                // Server certificate
                var serverPath = Path.Combine(_options.CertificateDirectory, "server.pfx");
                if (File.Exists(serverPath))
                {
                    _serverCertificate = new X509Certificate2(serverPath, _options.CertificatePassword);
                    _logger.LogInformation("Loaded server certificate from {Path}", serverPath);
                }
                else
                {
                    _serverCertificate = CertificateGenerator.GenerateServerCertificate(
                        _caCertificate,
                        $"{_options.ServiceName}.server",
                        _options.CertificateValidityInDays);
                    CertificateGenerator.ExportToPfx(_serverCertificate, serverPath, _options.CertificatePassword);
                    _logger.LogInformation("Generated new server certificate and saved to {Path}", serverPath);
                }

                // Client certificate
                var clientPath = Path.Combine(_options.CertificateDirectory, "client.pfx");
                if (File.Exists(clientPath))
                {
                    _clientCertificate = new X509Certificate2(clientPath, _options.CertificatePassword);
                    _logger.LogInformation("Loaded client certificate from {Path}", clientPath);
                }
                else
                {
                    _clientCertificate = CertificateGenerator.GenerateClientCertificate(
                        _caCertificate,
                        $"{_options.ServiceName}.client",
                        _options.CertificateValidityInDays);
                    CertificateGenerator.ExportToPfx(_clientCertificate, clientPath, _options.CertificatePassword);
                    _logger.LogInformation("Generated new client certificate and saved to {Path}", clientPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing certificate manager");
                throw;
            }
        }
    }

    /// <summary>
    /// Interface for certificate manager
    /// </summary>
    public interface ICertificateManager
    {
        X509Certificate2 GetCACertificate();
        X509Certificate2 GetServerCertificate();
        X509Certificate2 GetClientCertificate();
        X509Certificate2 GetCertificateByThumbprint(string thumbprint);
        X509Certificate2 GetCertificateFromFile(string filePath, string password);
        X509Certificate2 CreateSelfSignedCertificate(string subjectName);
    }

    /// <summary>
    /// Options for certificate manager
    /// </summary>
    public class CertificateOptions
    {
        public string CertificateDirectory { get; set; } = "certificates";
        public string CertificatePassword { get; set; } = "changeme";
        public int CertificateValidityInDays { get; set; } = 365;
        public string ServiceName { get; set; } = "service";
    }
}
