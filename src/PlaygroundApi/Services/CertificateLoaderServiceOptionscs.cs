namespace PlaygroundApi.Services
{
    internal sealed class CertificateLoaderServiceOptions
    {
        public const string SectionName = "CertificateLoaderService";

        public required int RefreshTimeInMinutes { get; init; }

        public required CertificateOptionsDetails[] Certificates { get; init; }

        internal sealed class CertificateOptionsDetails
        {
            public required string CertificateName { get; init; }
            public required string Type { get; init; }

            // properties used when Type = kv-default-credentials
            public string? KeyVaultUri { get; init; }
            public string? KeyVaultCertName { get; init; }

            // property used for Type = local-store
            public string? Thumbprint { get; init; }
        }
    }
}
