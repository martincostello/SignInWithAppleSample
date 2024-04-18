// Copyright (c) Martin Costello, 2019. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.IdentityModel.Tokens.Jwt;
using AspNet.Security.OAuth.Apple;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace MartinCostello.SignInWithApple;

public class AppleClientSecretGeneratorTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task GenerateAsync_Generates_Valid_Signed_Jwt()
    {
        // Arrange
        static void Configure(AppleAuthenticationOptions options)
        {
            options.ClientId = "my-client-id";
            options.ClientSecretExpiresAfter = TimeSpan.FromMinutes(1);
            options.KeyId = "my-key-id";
            options.TeamId = "my-team-id";
            options.PrivateKey = (_, cancellationToken) => GetPrivateKeyAsync(cancellationToken);
        }

        await GenerateTokenAsync(Configure, async (context) =>
        {
            var utcNow = DateTimeOffset.UtcNow;

            // Act
            string token = await context.Options.ClientSecretGenerator.GenerateAsync(context);

            // Assert
            token.ShouldNotBeNullOrWhiteSpace();
            token.Count((c) => c == '.').ShouldBe(2); // Format: "{header}.{body}.{signature}"

            // Act
            var validator = new JwtSecurityTokenHandler();
            var securityToken = validator.ReadJwtToken(token);

            // Assert - See https://developer.apple.com/documentation/signinwithapplerestapi/generate_and_validate_tokens
            securityToken.ShouldNotBeNull();

            securityToken.Header.ShouldNotBeNull();
            securityToken.Header.ShouldContainKeyAndValue("alg", "ES256");
            securityToken.Header.ShouldContainKeyAndValue("kid", "my-key-id");
            securityToken.Header.ShouldContainKeyAndValue("typ", "JWT");

            // See https://github.com/aspnet-contrib/AspNet.Security.OAuth.Providers/issues/684
            securityToken.Header.Keys.Order().ShouldBe(
                ["alg", "kid", "typ"],
                Case.Sensitive,
                "JWT header contains unexpected additional claims.");

            securityToken.Payload.ShouldNotBeNull();
            securityToken.Payload.ShouldContainKey("exp");
            securityToken.Payload.ShouldContainKey("iat");
            securityToken.Payload.ShouldContainKey("nbf");
            securityToken.Payload.ShouldContainKeyAndValue("aud", "https://appleid.apple.com");
            securityToken.Payload.ShouldContainKeyAndValue("iss", "my-team-id");
            securityToken.Payload.ShouldContainKeyAndValue("sub", "my-client-id");
            securityToken.Payload.Expiration.HasValue.ShouldBeTrue();

            securityToken.Payload.Keys.Order().ShouldBe(
                ["aud", "exp", "iat", "iss", "nbf", "sub"],
                Case.Sensitive,
                "JWT payload contains unexpected additional claims.");

            securityToken.Payload.IssuedAt.ShouldBeGreaterThanOrEqualTo(utcNow.UtcDateTime.AddSeconds(-1d * (utcNow.Second + utcNow.Millisecond)));
            securityToken.Payload.Expiration!.Value.ShouldBeGreaterThanOrEqualTo(utcNow.AddSeconds(60).ToUnixTimeSeconds());
            securityToken.Payload.Expiration.Value.ShouldBeLessThanOrEqualTo(utcNow.AddSeconds(70).ToUnixTimeSeconds());
        });
    }

    private static async Task<ReadOnlyMemory<char>> GetPrivateKeyAsync(CancellationToken cancellationToken = default)
        => (await File.ReadAllTextAsync("test.p8", cancellationToken)).AsMemory();

    private async Task GenerateTokenAsync(
        Action<AppleAuthenticationOptions> configureOptions,
        Func<AppleGenerateClientSecretContext, Task> actAndAssert)
    {
        // Arrange
        var builder = new WebHostBuilder()
            .ConfigureLogging((p) => p.AddXUnit(outputHelper).SetMinimumLevel(LogLevel.Debug))
            .Configure((app) => app.UseAuthentication())
            .ConfigureServices((services) =>
            {
                services.AddAuthentication()
                        .AddApple();
            });

        using var host = builder.Build();

        var httpContext = new DefaultHttpContext();
        var scheme = new AuthenticationScheme("Apple", "Apple", typeof(AppleAuthenticationHandler));

        var options = host.Services.GetRequiredService<IOptions<AppleAuthenticationOptions>>().Value;

        configureOptions(options);

        var context = new AppleGenerateClientSecretContext(httpContext, scheme, options);

        await actAndAssert(context);
    }
}
