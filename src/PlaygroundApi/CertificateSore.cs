using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;

namespace PlaygroundApi
{
    public class CertificateStore
    {
        private readonly ILogger<CertificateStore> _logger;
        private readonly ConcurrentDictionary<string, X509Certificate2> _certificateCache;

        public event EventHandler? DecryptionCertChange;

        public CertificateStore(ILogger<CertificateStore> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _certificateCache = new ConcurrentDictionary<string, X509Certificate2>(StringComparer.InvariantCultureIgnoreCase);
        }

        public X509Certificate2? GetCertificateByName(string name)
        {
            if (_certificateCache.TryGetValue(name, out var storeCert))
            {
                return storeCert;
            }

            _logger.LogError("Unable to find certificate '{CertificateName}' in store.", name);
            return null;
        }

        public void AddOrUpdateCert(string name, X509Certificate2 cert)
        {
            _ = _certificateCache.AddOrUpdate(name,
                (key, newCert) =>
                {
                    _logger.LogInformation("Adding certificate '{CertificateName}' with thumbprint '{CertificateThumbprint}'.", name, cert.Thumbprint);
                    return newCert;
                },
                (key, oldCert, newCert) =>
                {
                    if (oldCert.Thumbprint != newCert.Thumbprint)
                    {
                        _logger.LogInformation("Updating the certificate {CertificateName} from {OldCertificateThumbprint} to {CertificateThumbprint}", name, oldCert.Thumbprint, newCert.Thumbprint);
                        DecryptionCertChange?.Invoke(this, new EventArgs());
                    }
                    return newCert;
                },
                cert);
        }

        // !!! For demo purpose only. This should not be iterated on every get.
        public List<X509SecurityKey> GetAllDecryptionKeys()
        {
            return _certificateCache.Where(c => c.Value != null).Select(c => new X509SecurityKey(c.Value)).ToList();
        }


    }
}
