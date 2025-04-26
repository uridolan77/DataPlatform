namespace GenericDataPlatform.Common.Security
{
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Interface for managing certificates for secure communication
    /// </summary>
    public interface ICertificateManager
    {
        /// <summary>
        /// Gets a certificate by thumbprint from the certificate store
        /// </summary>
        /// <param name="thumbprint">The certificate thumbprint</param>
        /// <returns>The certificate if found, or null</returns>
        X509Certificate2? GetCertificateByThumbprint(string thumbprint);

        /// <summary>
        /// Gets a certificate from a file
        /// </summary>
        /// <param name="filePath">Path to the certificate file</param>
        /// <param name="password">Password for the certificate</param>
        /// <returns>The certificate</returns>
        X509Certificate2 GetCertificateFromFile(string filePath, string password);

        /// <summary>
        /// Creates a self-signed certificate for development
        /// </summary>
        /// <param name="subjectName">The subject name for the certificate</param>
        /// <returns>A self-signed certificate</returns>
        X509Certificate2 CreateSelfSignedCertificate(string subjectName);
    }
}