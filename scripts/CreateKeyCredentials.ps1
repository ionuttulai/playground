# Create a new X.509 certificate object
$certificate1 = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2
$certificate1.Import("decrypt-test-1_f1cc112372aa48cf9fa7a921cefa27c7.cer") # set full path to cert file here

# Convert the certificate to base64 - no need in this case as CER files already contain base64
# $base64Value = [System.Convert]::ToBase64String($certificate.GetRawCertData())

# Create a KeyCredential object
$keyCredential1 = New-Object Microsoft.Open.AzureAD.Model.KeyCredential
$keyCredential1.KeyId = "6a7da62d-502c-4bed-964f-9f08a228d550"  # Set your desired key ID
$keyCredential1.Type = "AsymmetricX509Cert"
$keyCredential1.Usage = "Encrypt"
$keyCredential1.Value = $certificate1.GetRawCertData()


$certificate2 = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2
$certificate2.Import("decrypt-test-2_85ad94ddfc1d4327a86a0727d334ab9e.cer") # set full path to cert file here

# Create a KeyCredential object
$keyCredential2 = New-Object Microsoft.Open.AzureAD.Model.KeyCredential
$keyCredential2.KeyId = "6f56f5ae-7e75-462e-983e-a99b1f589916"  # Set your desired key ID
$keyCredential2.Type = "AsymmetricX509Cert"
$keyCredential2.Usage = "Encrypt"
$keyCredential2.Value = $certificate2.GetRawCertData()


$keyCredentials = @($keyCredential1, $keyCredential2)

//Uplaod decrypt certs to 3p app registration
# Set-AzureADApplication -ObjectId 73aaa947-b6c8-4099-a1de-7585bab03c61 -KeyCredentials $keyCredentials