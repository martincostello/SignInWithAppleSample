/*
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
 * See https://github.com/aspnet-contrib/AspNet.Security.OAuth.Providers
 * for more information concerning the license and the contributors participating to this project.
 */

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace AspNet.Security.OAuth.Apple
{
    /// <summary>
    /// Defines a handler for authentication using Apple.
    /// </summary>
    public class AppleAuthenticationHandler : OAuthHandler<AppleAuthenticationOptions>
    {
        private readonly JwtSecurityTokenHandler _tokenHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppleAuthenticationHandler"/> class.
        /// </summary>
        /// <param name="options">The authentication options.</param>
        /// <param name="logger">The logger to use.</param>
        /// <param name="encoder">The URL encoder to use.</param>
        /// <param name="clock">The system clock to use.</param>
        /// <param name="tokenHandler">The JWT security token handler to use.</param>
        public AppleAuthenticationHandler(
            IOptionsMonitor<AppleAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            JwtSecurityTokenHandler tokenHandler)
            : base(options, logger, encoder, clock)
        {
            _tokenHandler = tokenHandler;
        }

        /// <summary>
        /// The handler calls methods on the events which give the application control at certain points where processing is occurring.
        /// If it is not provided a default instance is supplied which does nothing when the methods are called.
        /// </summary>
        protected new AppleAuthenticationEvents Events
        {
            get { return (AppleAuthenticationEvents)base.Events; }
            set { base.Events = value; }
        }

        /// <inheritdoc />
        protected override string BuildChallengeUrl(AuthenticationProperties properties, string redirectUri)
        {
            string challengeUrl = base.BuildChallengeUrl(properties, redirectUri);

            // Apple requires the response mode to be form_post when the email or name scopes are requested
            return QueryHelpers.AddQueryString(challengeUrl, "response_mode", "form_post");
        }

        /// <inheritdoc />
        protected override Task<object> CreateEventsAsync() => Task.FromResult<object>(new AppleAuthenticationEvents());

        /// <inheritdoc />
        protected override async Task<AuthenticationTicket> CreateTicketAsync(
            ClaimsIdentity identity,
            AuthenticationProperties properties,
            OAuthTokenResponse tokens)
        {
            string idToken = tokens.Response.Value<string>("id_token");

            // TODO These can probably be removed once Sign In with Apple is finalized
            Logger.LogInformation("Creating ticket for Sign In with Apple.");
            Logger.LogTrace("Access Token: {AccessToken}", tokens.AccessToken);
            Logger.LogTrace("Refresh Token: {RefreshToken}", tokens.RefreshToken);
            Logger.LogTrace("Token Type: {TokenType}", tokens.TokenType);
            Logger.LogTrace("Expires In: {ExpiresIn}", tokens.ExpiresIn);
            Logger.LogTrace("Response: {TokenResponse}", tokens.Response);
            Logger.LogTrace("ID Token: {IdToken}", idToken);

            if (string.IsNullOrWhiteSpace(idToken))
            {
                throw new InvalidOperationException("No Apple ID token was returned in the OAuth token response.");
            }

            if (Options.ValidateTokens)
            {
                var validateIdContext = new AppleValidateIdTokenContext(Context, Scheme, Options, idToken);
                await Events.ValidateIdToken(validateIdContext);
            }

            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, GetNameIdentifier(idToken)));

            var principal = new ClaimsPrincipal(identity);

            var context = new OAuthCreatingTicketContext(principal, properties, Context, Scheme, Options, Backchannel, tokens);
            context.RunClaimActions(tokens.Response);

            await Options.Events.CreatingTicket(context);
            return new AuthenticationTicket(context.Principal, context.Properties, Scheme.Name);
        }

        /// <inheritdoc />
        protected override async Task<OAuthTokenResponse> ExchangeCodeAsync(string code, string redirectUri)
        {
            if (Options.GenerateClientSecret)
            {
                var context = new AppleGenerateClientSecretContext(Context, Scheme, Options);
                await Events.GenerateClientSecret(context);
            }

            return await base.ExchangeCodeAsync(code, redirectUri);
        }

        /// <inheritdoc />
        protected override Task<HandleRequestResult> HandleRemoteAuthenticateAsync()
        {
            // HACK If form_post was used, then copy the parameters from the form into the
            // query string so we can re-use the implementation in the base class to handle
            // the authentication, rather than re-implement it to support using the form.
            if (string.Equals(Request.Method, HttpMethod.Post.Method, StringComparison.OrdinalIgnoreCase))
            {
                var queryCopy = new Dictionary<string, StringValues>(Request.Query);

                foreach (var parameter in Request.Form)
                {
                    if (!queryCopy.ContainsKey(parameter.Key))
                    {
                        queryCopy[parameter.Key] = parameter.Value;
                    }
                }

                Request.Query = new QueryCollection(queryCopy);
            }

            return base.HandleRemoteAuthenticateAsync();
        }

        private string GetNameIdentifier(string token)
        {
            try
            {
                var userToken = _tokenHandler.ReadJwtToken(token);
                return userToken.Subject;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to parse JWT from Apple ID token.", ex);
            }
        }
    }
}
