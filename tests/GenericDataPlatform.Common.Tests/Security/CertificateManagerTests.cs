using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using GenericDataPlatform.Common.Security;
using Xunit;

namespace GenericDataPlatform.Common.Tests.Security
{
    public class CertificateManagerTests : IDisposable
    {
        private readonly ICertificateManager _certificateManager;
        private readonly string _testCertPath;
        private readonly string _testCertPassword;

        public CertificateManagerTests()
        {
            _certificateManager = new CertificateManager();
            _testCertPath = Path.Combine(Path.GetTempPath(), $"test-cert-{Guid.NewGuid()}.pfx");
            _testCertPassword = Guid.NewGuid().ToString();
            
            // Create a test certificate for file-based tests
            CreateTestCertificate();
        }

        public void Dispose()
        {
            // Clean up test certificate file
            if (File.Exists(_testCertPath))
            {
                try
                {
                    File.Delete(_testCertPath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        [Fact]
        public void CreateSelfSignedCertificate_ShouldCreateValidCertificate()
        {
            // Arrange
            string subjectName = "CN=TestCertificate";

            // Act
            var certificate = _certificateManager.CreateSelfSignedCertificate(subjectName);

            // Assert
            Assert.NotNull(certificate);
            Assert.Equal(subjectName, certificate.Subject);
            Assert.True(certificate.HasPrivateKey);
            
            // Verify certificate dates
            var now = DateTimeOffset.Now;
            Assert.True(certificate.NotBefore <= now);
            Assert.True(certificate.NotAfter > now);
        }

        [Fact]
        public void GetCertificateFromFile_ShouldLoadCertificateCorrectly()
        {
            // Act
            var certificate = _certificateManager.GetCertificateFromFile(_testCertPath, _testCertPassword);

            // Assert
            Assert.NotNull(certificate);
            Assert.Equal("CN=TestCertificate", certificate.Subject);
            Assert.True(certificate.HasPrivateKey);
        }

        [Fact]
        public void GetCertificateFromFile_ShouldThrowException_WhenFileDoesNotExist()
        {
            // Arrange
            string nonExistentPath = Path.Combine(Path.GetTempPath(), $"non-existent-{Guid.NewGuid()}.pfx");

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => 
                _certificateManager.GetCertificateFromFile(nonExistentPath, "password"));
        }

        [Fact]
        public void GetCertificateFromFile_ShouldThrowException_WhenPasswordIsIncorrect()
        {
            // Act & Assert
            Assert.Throws<CryptographicException>(() => 
                _certificateManager.GetCertificateFromFile(_testCertPath, "wrong-password"));
        }

        [Fact]
        public void GetCertificateByThumbprint_ShouldReturnNull_WhenCertificateNotFound()
        {
            // Arrange
            string nonExistentThumbprint = "0123456789ABCDEF0123456789ABCDEF01234567";

            // Act
            var certificate = _certificateManager.GetCertificateByThumbprint(nonExistentThumbprint);

            // Assert
            Assert.Null(certificate);
        }

        private void CreateTestCertificate()
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest(
                "CN=TestCertificate",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            var certificate = request.CreateSelfSigned(
                DateTimeOffset.Now.AddDays(-1),
                DateTimeOffset.Now.AddYears(1));

            File.WriteAllBytes(_testCertPath, certificate.Export(X509ContentType.Pfx, _testCertPassword));
        }
    }
}
