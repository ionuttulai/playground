# playground
Ropo for playing around with API projects


## Playground API
Example of API endpoint authorized with S2S library using dynamicaly loaded certs for key decryption. 

The 3P Entra app used as identity: https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/~/Overview/appId/ceccf247-9a0b-459a-8cb5-794e36efb0d4/isMSAApp~/false

Certs used from DEV KV:

https://ms.portal.azure.com/#@microsoft.onmicrosoft.com/asset/Microsoft_Azure_KeyVault/Certificate/https://kv-spacesbatch.vault.azure.net/certificates/decrypt-test-1
https://ms.portal.azure.com/#@microsoft.onmicrosoft.com/asset/Microsoft_Azure_KeyVault/Certificate/https://kv-spacesbatch.vault.azure.net/certificates/decrypt-test-2


### How does this work

**CertLoader** background service loads certs from a KV on a predefined time interval and saves them in an in-memory CertificateStore.

**ConfigureAuth** class defines PostConfiguration for S2S auth library and uses a Decryption Key Resolver delegate rather than configuring the keys statically in the Auth Manager.

When a key is needed for a token decryption, the key resolvers will try to match the **key id** in one of the loaded certs. If the key id hint is not passed along, it will look at the security token and use the header property x5t to determine the corresponding key and, if a key is not used it'll return all available keys in the cert store.

Encryption Key Rotation Logic

-------------------------------------------------------------------------------------------
**Step 1**

- Key 1 (cert 1) loaded in the app registration as encryption cert and marked as **active**
- Key 2 (cert 2) loaded in the app registration as encryption cert.

***API has both keys loaded and always matching incoming encrypted tokens on key 1 - active for encryption***

-------------------------------------------------------------------------------------------
**Step 2**

- Key 2 is marked as **active** in preparation for cert 1 rotation.
- Key 1 remains in the ap registration but doesn't get used for encrypting new tokens.

***API starts seeing a mix of old tokens encrypted with key 1 and new ones encrypted with key 2 - all works.***

-------------------------------------------------------------------------------------------
**Step 3** 

- Cert 1 gets rotated and uplaoded to the app registration as key 1.
- Key 2 remains **active** for all token encryption.

***API picks up the new cert for key 1 and continues to see tokens encrypted with key 2 - all works.***

-------------------------------------------------------------------------------------------
**Step 4**

- Key 1 (new) is marked as **active** in preparation for cert 1 rotation.
- Key 2 remains available as encryption key, marked **not active**.

***API starts seeyng tokens encrypted with the new cert for Key 1 - all works.***

-------------------------------------------------------------------------------------------
**Future**

All prepared for a key rotation for Key 2 with the same steps as performed for Key 1 above).

-------------------------------------------------------------------------------------------
