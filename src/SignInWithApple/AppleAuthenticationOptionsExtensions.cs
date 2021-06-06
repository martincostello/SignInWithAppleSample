// Copyright (c) Martin Costello, 2019. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using AspNet.Security.OAuth.Apple;
using Azure;
using Azure.Security.KeyVault.Secrets;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods to configure Sign in with Apple authentication capabilities for an HTTP application pipeline.
    /// </summary>
    internal static class AppleAuthenticationOptionsExtensions
    {
        /// <summary>
        /// Configures the application to use a specified Azure Key Vault secret to generate a client secret for the provider.
        /// </summary>
        /// <param name="options">The Apple authentication options to configure.</param>
        /// <param name="secretProvider">
        /// A delegate to an asynchronous method to return the <see cref="KeyVaultSecret"/> for the
        /// private key which is passed the value of <see cref="AppleAuthenticationOptions.KeyId"/>.
        /// </param>
        /// <returns>
        /// The value of the <paramref name="options"/> argument.
        /// </returns>
        public static AppleAuthenticationOptions UseAzureKeyVaultSecret(
            this AppleAuthenticationOptions options,
            Func<string, Task<Response<KeyVaultSecret>>> secretProvider)
        {
            options.GenerateClientSecret = true;
            options.PrivateKeyBytes = async (keyId) =>
            {
                var secret = await secretProvider(keyId);

                string privateKey = secret.Value.Value;

                if (privateKey.StartsWith("-----BEGIN PRIVATE KEY-----", StringComparison.Ordinal))
                {
                    string[] lines = privateKey.Split('\n');
                    privateKey = string.Join(string.Empty, lines[1..^1]);
                }

                return Convert.FromBase64String(privateKey);
            };

            return options;
        }
    }
}
