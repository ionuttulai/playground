// 3P iotulai
using Microsoft.Identity.Client;

const string applicationId = "ceccf247-9a0b-459a-8cb5-794e36efb0d4";
const string backendScope = "ceccf247-9a0b-459a-8cb5-794e36efb0d4/.default";
const string redirectUri = "http://localhost";

const string authority = "https://login.microsoftonline.com/common";
var client = PublicClientApplicationBuilder
                .Create(applicationId)
                .WithAuthority(authority)
                .WithRedirectUri(redirectUri)
                .Build();
try
{
    Console.WriteLine("Login using browser...");
    var backendToken = await client.AcquireTokenInteractive(new[] { backendScope }).ExecuteAsync();

    Console.WriteLine($"Authenticated user:{backendToken.Account.Username} for scope {string.Join("\n", backendToken.ClaimsPrincipal.Claims.Select((claim) => claim.ToString()))}");
    Console.WriteLine("AAD user token expires on: " + backendToken.ExpiresOn);
    Console.WriteLine("------------ TOKEN ------------------");
    Console.WriteLine(backendToken.AccessToken);
    Console.WriteLine("------------  END  ------------------");
}
catch (Exception ex)
{
    Console.WriteLine(ex.ToString());
}