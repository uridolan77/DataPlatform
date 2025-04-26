using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace GenericDataPlatform.Common.Security.Certificates
{
    /// <summary>
    /// Utility for generating X.509 certificates
    /// </summary>
    public static class CertificateGenerator
    {
        /// <summary>
        /// Generates a Certificate Authority (CA) certificate
        /// </summary>
        public static X509Certificate2 GenerateCA(string subjectName, int validityInDays)
        {
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var req = new CertificateRequest(
                $"CN={subjectName}",
                ecdsa,
                HashAlgorithmName.SHA256);

            // Add CA extensions
            req.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(true, true, 12, true));
            
            req.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.CrlSign | X509KeyUsageFlags.KeyCertSign,
                    true));

            // Generate self-signed CA cert
            var certificate = req.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(validityInDays));

            // Ensure the certificate has a private key
            return new X509Certificate2(certificate.Export(X509ContentType.Pfx), (string)null, 
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
        }

        /// <summary>
        /// Generates a server certificate signed by the CA
        /// </summary>
        public static X509Certificate2 GenerateServerCertificate(
            X509Certificate2 caCertificate,
            string subjectName,
            int validityInDays,
            string[] subjectAlternativeNames = null)
        {
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var req = new CertificateRequest(
                $"CN={subjectName}",
                ecdsa,
                HashAlgorithmName.SHA256);

            // Add server certificate extensions
            req.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                    true));

            req.CertificateExtensions.Add(
                new X509EnhancedKeyUsageExtension(
                    new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") },
                    true));

            // Add subject alternative names if provided
            if (subjectAlternativeNames != null && subjectAlternativeNames.Length > 0)
            {
                var sanBuilder = new SubjectAlternativeNameBuilder();
                foreach (var name in subjectAlternativeNames)
                {
                    sanBuilder.AddDnsName(name);
                }
                req.CertificateExtensions.Add(sanBuilder.Build());
            }
            else
            {
                // Add default SAN for the subject name
                var sanBuilder = new SubjectAlternativeNameBuilder();
                sanBuilder.AddDnsName(subjectName);
                req.CertificateExtensions.Add(sanBuilder.Build());
            }

            // Extract CA's private key for signing
            using var caPrivateKey = caCertificate.GetECDsaPrivateKey();
            if (caPrivateKey == null)
            {
                throw new InvalidOperationException("CA certificate does not have a private key or it's not accessible.");
            }

            // Create a certificate signed by the CA
            var notBefore = DateTimeOffset.UtcNow.AddDays(-1);
            var notAfter = DateTimeOffset.UtcNow.AddDays(validityInDays);
            var serialNumber = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(serialNumber);
            }

            var certificate = req.Create(caCertificate, notBefore, notAfter, serialNumber);

            // Create certificate with private key
            return new X509Certificate2(
                certificate.CopyWithPrivateKey(ecdsa).Export(X509ContentType.Pfx),
                (string)null,
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
        }

        /// <summary>
        /// Generates a client certificate signed by the CA
        /// </summary>
        public static X509Certificate2 GenerateClientCertificate(
            X509Certificate2 caCertificate,
            string subjectName,
            int validityInDays)
        {
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var req = new CertificateRequest(
                $"CN={subjectName}",
                ecdsa,
                HashAlgorithmName.SHA256);

            // Add client certificate extensions
            req.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature,
                    true));

            req.CertificateExtensions.Add(
                new X509EnhancedKeyUsageExtension(
                    new OidCollection { new Oid("1.3.6.1.5.5.7.3.2") },
                    true));

            // Extract CA's private key for signing
            using var caPrivateKey = caCertificate.GetECDsaPrivateKey();
            if (caPrivateKey == null)
            {
                throw new InvalidOperationException("CA certificate does not have a private key or it's not accessible.");
            }

            // Create a certificate signed by the CA
            var notBefore = DateTimeOffset.UtcNow.AddDays(-1);
            var notAfter = DateTimeOffset.UtcNow.AddDays(validityInDays);
            var serialNumber = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(serialNumber);
            }

            var certificate = req.Create(caCertificate, notBefore, notAfter, serialNumber);

            // Create certificate with private key
            return new X509Certificate2(
                certificate.CopyWithPrivateKey(ecdsa).Export(X509ContentType.Pfx),
                (string)null,
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
        }

        /// <summary>
        /// Exports a certificate to a PFX file
        /// </summary>
        public static void ExportToPfx(X509Certificate2 certificate, string path, string password)
        {
            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            // Export to PFX with password
            File.WriteAllBytes(path, certificate.Export(X509ContentType.Pfx, password));
        }
    }
}
