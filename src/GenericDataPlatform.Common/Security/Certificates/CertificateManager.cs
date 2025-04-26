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
        /// Initializes the certificate manager
        /// </summary>
        private void Initialize()
        {
            try
            {
                // Create certificate directory if it doesn't exist
                if (!Directory.Exists(_options.CertificateDirectory))
                {
                    Directory.CreateDirectory(_options.CertificateDirectory);
                }

                // Load or generate CA certificate
                var caPath = Path.Combine(_options.CertificateDirectory, "ca.pfx");
                if (File.Exists(caPath))
                {
                    _caCertificate = CertificateGenerator.ImportFromPfx(caPath, _options.CertificatePassword);
                    _logger.LogInformation("Loaded CA certificate from {Path}", caPath);
                }
                else
                {
                    _caCertificate = CertificateGenerator.GenerateCACertificate(
                        $"GenericDataPlatform CA",
                        _options.CertificateValidityInDays);
                    CertificateGenerator.ExportToPfx(_caCertificate, caPath, _options.CertificatePassword);
                    _logger.LogInformation("Generated new CA certificate and saved to {Path}", caPath);
                }

                // Load or generate server certificate
                var serverPath = Path.Combine(_options.CertificateDirectory, $"{_options.ServiceName}.server.pfx");
                if (File.Exists(serverPath))
                {
                    _serverCertificate = CertificateGenerator.ImportFromPfx(serverPath, _options.CertificatePassword);
                    _logger.LogInformation("Loaded server certificate from {Path}", serverPath);
                }
                else
                {
                    _serverCertificate = CertificateGenerator.GenerateServerCertificate(
                        _caCertificate,
                        $"{_options.ServiceName}.server",
                        new[] { _options.ServiceName, "localhost" },
                        _options.CertificateValidityInDays);
                    CertificateGenerator.ExportToPfx(_serverCertificate, serverPath, _options.CertificatePassword);
                    _logger.LogInformation("Generated new server certificate and saved to {Path}", serverPath);
                }

                // Load or generate client certificate
                var clientPath = Path.Combine(_options.CertificateDirectory, $"{_options.ServiceName}.client.pfx");
                if (File.Exists(clientPath))
                {
                    _clientCertificate = CertificateGenerator.ImportFromPfx(clientPath, _options.CertificatePassword);
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
