using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace GenericDataPlatform.Common.Security.Certificates
{
    /// <summary>
    /// Utility for generating certificates for mTLS
    /// </summary>
    public class CertificateGenerator
    {
        /// <summary>
        /// Generates a self-signed CA certificate
        /// </summary>
        public static X509Certificate2 GenerateCACertificate(string subjectName, int validityInDays = 365)
        {
            using var rsa = RSA.Create(4096);
            var request = new CertificateRequest(
                $"CN={subjectName}",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            request.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(true, false, 0, true));

            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign,
                    true));

            var certificate = request.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(validityInDays));

            return certificate;
        }

        /// <summary>
        /// Generates a server certificate signed by the CA
        /// </summary>
        public static X509Certificate2 GenerateServerCertificate(
            X509Certificate2 caCertificate,
            string subjectName,
            string[] dnsNames,
            int validityInDays = 365)
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest(
                $"CN={subjectName}",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            // Add DNS names as subject alternative names
            var sanBuilder = new SubjectAlternativeNameBuilder();
            foreach (var dnsName in dnsNames)
            {
                sanBuilder.AddDnsName(dnsName);
            }
            request.CertificateExtensions.Add(sanBuilder.Build());

            // Add server authentication extended key usage
            request.CertificateExtensions.Add(
                new X509EnhancedKeyUsageExtension(
                    new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, // Server Authentication
                    true));

            // Add key usage
            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                    true));

            // Create certificate signed by the CA
            var notBefore = DateTimeOffset.UtcNow.AddDays(-1);
            var notAfter = DateTimeOffset.UtcNow.AddDays(validityInDays);

            using var caPrivateKey = caCertificate.GetRSAPrivateKey();
            var serialNumber = new byte[16];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(serialNumber);

            var certificate = request.Create(
                caCertificate,
                notBefore,
                notAfter,
                serialNumber);

            // Create a certificate with private key
            return certificate.CopyWithPrivateKey(rsa);
        }

        /// <summary>
        /// Generates a client certificate signed by the CA
        /// </summary>
        public static X509Certificate2 GenerateClientCertificate(
            X509Certificate2 caCertificate,
            string subjectName,
            int validityInDays = 365)
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest(
                $"CN={subjectName}",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            // Add client authentication extended key usage
            request.CertificateExtensions.Add(
                new X509EnhancedKeyUsageExtension(
                    new OidCollection { new Oid("1.3.6.1.5.5.7.3.2") }, // Client Authentication
                    true));

            // Add key usage
            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature,
                    true));

            // Create certificate signed by the CA
            var notBefore = DateTimeOffset.UtcNow.AddDays(-1);
            var notAfter = DateTimeOffset.UtcNow.AddDays(validityInDays);

            using var caPrivateKey = caCertificate.GetRSAPrivateKey();
            var serialNumber = new byte[16];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(serialNumber);

            var certificate = request.Create(
                caCertificate,
                notBefore,
                notAfter,
                serialNumber);

            // Create a certificate with private key
            return certificate.CopyWithPrivateKey(rsa);
        }

        /// <summary>
        /// Exports a certificate to a PFX file
        /// </summary>
        public static void ExportToPfx(X509Certificate2 certificate, string filePath, string password)
        {
            var pfxBytes = certificate.Export(X509ContentType.Pfx, password);
            File.WriteAllBytes(filePath, pfxBytes);
        }

        /// <summary>
        /// Exports a certificate to a PEM file
        /// </summary>
        public static void ExportToPem(X509Certificate2 certificate, string filePath)
        {
            var pemCertificate = PemEncoding.Write("CERTIFICATE", certificate.RawData);
            File.WriteAllText(filePath, pemCertificate);

            if (certificate.HasPrivateKey)
            {
                var privateKeyPath = Path.ChangeExtension(filePath, ".key.pem");
                var privateKey = certificate.GetRSAPrivateKey();
                var pemPrivateKey = PemEncoding.Write("RSA PRIVATE KEY", privateKey.ExportRSAPrivateKey());
                File.WriteAllText(privateKeyPath, pemPrivateKey);
            }
        }

        /// <summary>
        /// Imports a certificate from a PFX file
        /// </summary>
        public static X509Certificate2 ImportFromPfx(string filePath, string password)
        {
            return new X509Certificate2(filePath, password, X509KeyStorageFlags.Exportable);
        }
    }
}
