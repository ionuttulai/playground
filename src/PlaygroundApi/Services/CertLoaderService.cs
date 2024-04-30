using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;

namespace PlaygroundApi.Services
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "The class is being dependency injected.")]
    internal class CertificateLoaderService : TimedBackgroundService
    {
        private readonly ILogger<CertificateLoaderService> _logger;
        private readonly int refreshTimeInMinutes = 15;
        private readonly DefaultAzureCredential _credential;
        private readonly CertificateStore _certificateStore;

        private readonly IOptions<CertificateLoaderServiceOptions> _options;
        private readonly CertificateLoaderServiceOptions.CertificateOptionsDetails[] _certificates;

        public CertificateLoaderService(
            ILogger<CertificateLoaderService> logger,
            IOptions<CertificateLoaderServiceOptions> options,
            DefaultAzureCredential credential,
            CertificateStore certificateStore) : base(logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _credential = credential ?? throw new ArgumentNullException(nameof(credential));
            _certificateStore = certificateStore ?? throw new ArgumentNullException(nameof(certificateStore));


            _options = options;
            
            refreshTimeInMinutes = _options.Value.RefreshTimeInMinutes;
            _certificates = _options.Value.Certificates;

            _certificateStore = certificateStore ?? throw new ArgumentNullException(nameof(certificateStore));
        }

        /// <summary>
        /// Attempts to load all certificates defined in options
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "By design.")]
        public async virtual Task<(int LoadedCount, bool AllCertsLoaded)> LoadAllCertificates(CancellationToken stoppingToken)
        {
            var certsLoaded = 0;
            bool allCertsLoaded = true;
            foreach (var certInfo in _certificates)
            {
                _logger.LogInformation("{ClassName}.{MethodName}: Loading certificate '{CertificateName}'", nameof(CertificateLoaderService), nameof(InvokeAsync), certInfo.CertificateName);

                try
                {
                    X509Certificate2? certificate = null;

                    switch (certInfo.Type)
                    {
                        case "kv-default-credentials":
                            _logger.LogInformation("{ClassName}.{MethodName}: Loading certificate '{CertificateName}' with type '{CertificateType}' and KeyVault info '({KeyVaultCertificateName}|{KeyVaultUri})'.", nameof(CertificateLoaderService), nameof(InvokeAsync), certInfo.CertificateName, certInfo.Type, certInfo.KeyVaultCertName, certInfo.KeyVaultUri);

                            if (certInfo.KeyVaultCertName == null)
                            {
                                allCertsLoaded = false;
                                _logger.LogError("{ClassName}.{MethodName}: Loading certificate '{CertificateName}' failed because the KeyVaultCertName was null.", nameof(CertificateLoaderService), nameof(InvokeAsync), certInfo.CertificateName);
                                break;
                            }

                            if (certInfo.KeyVaultUri == null)
                            {
                                allCertsLoaded = false;
                                _logger.LogError("{ClassName}.{MethodName}: Loading certificate '{CertificateName}' failed because the KeyVaultUri was null.", nameof(CertificateLoaderService), nameof(InvokeAsync), certInfo.CertificateName);
                                break;
                            }

                            certificate = await LoadFromKeyVaultAsync(certInfo.KeyVaultCertName, certInfo.KeyVaultUri, stoppingToken);
                            break;

                        case "local-store":
                            _logger.LogInformation("{ClassName}.{MethodName}: Loading certificate '{CertificateName}' with type '{CertificateType}' and thumbprint '{CertificateThumbprint}'.", nameof(CertificateLoaderService), nameof(InvokeAsync), certInfo.CertificateName, certInfo.Type, certInfo.Thumbprint);

                            if (certInfo.Thumbprint == null)
                            {
                                allCertsLoaded = false;
                                _logger.LogError("{ClassName}.{MethodName}: Loading certificate '{CertificateName}' failed because the Thumbprint was null.", nameof(CertificateLoaderService), nameof(InvokeAsync), certInfo.CertificateName);
                                break;
                            }

                            certificate = LoadFromLocalStore(certInfo.Thumbprint);
                            break;

                        default:
                            _logger.LogWarning("{ClassName}.{MethodName}: Unknown certificate type found in configuration: '{CertificateType}'.", nameof(CertificateLoaderService), nameof(InvokeAsync), certInfo.Type);
                            break;
                    }

                    if (certificate != null)
                    {
                        _certificateStore.AddOrUpdateCert(certInfo.CertificateName, certificate);
                        certsLoaded++;
                    }
                    else
                    {
                        allCertsLoaded = false;
                        _logger.LogWarning("{ClassName}.{MethodName}: Loading certificate name '{CertificateName}' failed because the certificate was null.", nameof(CertificateLoaderService), nameof(InvokeAsync), certInfo.CertificateName);
                    }
                }
                catch (Exception ex)
                {
                    allCertsLoaded = false;
                    // log any exception here and move to the next cert.
                    _logger.LogWarning(ex, "{ClassName}.{MethodName}: Unable to load certificate '{CertificateName}'.", nameof(CertificateLoaderService), nameof(InvokeAsync), certInfo.CertificateName);
                }
            }
            return (certsLoaded, allCertsLoaded);
        }

        /// <inheritdoc />
        protected override async Task<BackgroundTaskResult> InvokeAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("{ClassName}.{MethodName}: Begin CertificateLoaderService Refresh.", nameof(CertificateLoaderService), nameof(InvokeAsync));
            var certsLoaded = await LoadAllCertificates(stoppingToken);

            _logger.LogInformation("{ClassName}.{MethodName}: Finish CertificateLoaderService Refresh. Certificates refreshed: '{CertificateLoadedCount}'", nameof(CertificateLoaderService), nameof(InvokeAsync), certsLoaded);

            return BackgroundTaskResult.Succeed(TimeSpan.FromMinutes(refreshTimeInMinutes));
        }

        private X509Certificate2? LoadFromLocalStore(string thumbprint)
        {
            _logger.LogInformation("{ClassName}.{MethodName}: Begin loading certificate.", nameof(CertificateLoaderService), nameof(LoadFromLocalStore));

            // TODO: The location doesn't have to be hardcoded and could later be passed in the certificate configuration.
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            {
                store.Open(OpenFlags.ReadOnly);
                var certificates = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);

                return certificates.Count > 0 ? certificates[0] : null;
            }
        }

        private async Task<X509Certificate2?> LoadFromKeyVaultAsync(string name, string kvUri, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{ClassName}.{MethodName}: Begin loading certificate.", nameof(CertificateLoaderService), nameof(LoadFromKeyVaultAsync));

            // Loading a certificate with the private key requires using the secret client. The certificate client retrieves the certificate data without the PK. Ref: https://docs.microsoft.com/en-us/samples/azure/azure-sdk-for-net/get-certificate-private-key/
            var client = new SecretClient(new Uri(kvUri), _credential);
            Response<KeyVaultSecret> secretResponse;
            var timer = Stopwatch.StartNew();
            try
            {
                secretResponse = await client.GetSecretAsync(name, cancellationToken: cancellationToken);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ClassName}.{MethodName}: Failed to load certificate name '{CertificateName}' on exception '{ExceptionMessage}'.", nameof(CertificateLoaderService), nameof(LoadFromKeyVaultAsync), name, ex.Message);
                throw;
            }

            if (string.IsNullOrEmpty(secretResponse.Value.Value))
            {
                _logger.LogWarning("{ClassName}.{MethodName}: Loading certificate name '{CertificateName}' from Key Vault failed because secret was null or empty.", nameof(CertificateLoaderService), nameof(LoadFromKeyVaultAsync), name);
                return null;
            }

            return new X509Certificate2(
                rawData: Convert.FromBase64String(secretResponse.Value.Value),
                password: (string?)null,
                keyStorageFlags: X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable);
        }
    }

    internal sealed class BackgroundTaskResult
    {
        public bool TaskSucceeded { get; }

        public TimeSpan Delay { get; }

        private BackgroundTaskResult(bool succeeded, TimeSpan delay)
        {
            TaskSucceeded = succeeded;
            Delay = delay;
        }

        public static BackgroundTaskResult Succeed(TimeSpan delay)
        {
            return new BackgroundTaskResult(true, delay);
        }

        public static BackgroundTaskResult Fail(TimeSpan delay)
        {
            return new BackgroundTaskResult(false, delay);
        }
    }
}


