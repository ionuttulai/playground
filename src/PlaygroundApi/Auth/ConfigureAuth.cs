using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.S2S;
using Microsoft.IdentityModel.S2S.Configuration;
using Microsoft.IdentityModel.Tokens;
using PlaygroundApi;

internal class ConfigureAuth : IS2SAuthenticationManagerPostConfigure
{
    private readonly ILogger<ConfigureAuth> _logger;
    private readonly CertificateStore _certificateStore;

    public ConfigureAuth(ILogger<ConfigureAuth> logger, CertificateStore certificateStore)
    {
        _logger = logger;
        _certificateStore = certificateStore;
    }

    public S2SAuthenticationManager PostConfigure(S2SAuthenticationManager s2sAuthenticationManager)
    {
       _logger.LogInformation("Postconfigure called");

        if (s2sAuthenticationManager.AuthenticationHandlers.Single() is not JwtAuthenticationHandler authHandler)
        {
            throw new InvalidOperationException("Failed to load JwtAuthenticationHandler");
        }

        var authPolicy = authHandler.InboundPolicies.FirstOrDefault(p => p.Label == "first-party-prod");

        if (authPolicy != null)
        {
            authPolicy.TokenValidationParameters.TokenDecryptionKeyResolver = CertificateStoreDecryptionResolver;
        }

        return s2sAuthenticationManager;
    }

    /// <summary>
    /// CertificateStoreDecryptionResolver - resolves the correct key or decryption using certs dynamically loaded in the CertificateStore.
    /// </summary>
    /// <param name="token"></param>
    /// <param name="securityToken"></param>
    /// <param name="kid"></param>
    /// <param name="validationParameters"></param>
    /// <returns></returns>
    private List<X509SecurityKey> CertificateStoreDecryptionResolver(string token, SecurityToken securityToken, string kid, TokenValidationParameters validationParameters)
    {

        var internalKeyId =  kid;
        if (string.IsNullOrEmpty(internalKeyId))
        {
            internalKeyId = ((JsonWebToken)securityToken)?.X5t;
        }

        _logger.LogInformation($"Key id used for token decryption is '{internalKeyId}' | Key passed in the decryption resolver: '{kid}'");
        var keys = _certificateStore.GetAllDecryptionKeys();
        var oneKey = keys.FirstOrDefault(k => k.X5t == internalKeyId);

        if ( oneKey != null )
        {
            _logger.LogInformation($"Decryption key found in the store for key id '{internalKeyId}'.");
            return new List<X509SecurityKey> { oneKey };
        }

        _logger.LogWarning($"Could not find an internal loaded key for key id '{internalKeyId}'. Returning all available decryption keys.");
        return  keys;
    }


}