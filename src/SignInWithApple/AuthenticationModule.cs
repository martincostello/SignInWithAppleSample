// Copyright (c) Martin Costello, 2019. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using AspNet.Security.OAuth.Apple;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MartinCostello.SignInWithApple
{
    internal static class AuthenticationModule
    {
        private const string DeniedPath = "/denied";
        private const string RootPath = "/";
        private const string SignInPath = "/signin";
        private const string SignOutPath = "/signout";

        public static IServiceCollection AddSignInWithApple(this IServiceCollection builder)
        {
            return builder
                .AddAuthentication(options => options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = SignInPath;
                    options.LogoutPath = SignOutPath;
                })
                .AddApple()
                .Services
                .AddOptions<AppleAuthenticationOptions>(AppleAuthenticationDefaults.AuthenticationScheme)
                .Configure<IConfiguration, IHostEnvironment>((options, configuration, environment) =>
                {
                    options.AccessDeniedPath = DeniedPath;
                    options.ClientId = configuration["AppleClientId"];
                    options.KeyId = configuration["AppleKeyId"];
                    options.TeamId = configuration["AppleTeamId"];

                    options.UsePrivateKey(
                        keyId =>
                            environment.ContentRootFileProvider.GetFileInfo($"AuthKey_{keyId}.p8"));
                })
                .Services;
        }

        public static IEndpointRouteBuilder MapAuthenticationRoutes(this IEndpointRouteBuilder builder)
        {
            builder.MapGet(DeniedPath, context => context.RedirectAsync(RootPath + "?denied=true"));
            builder.MapGet(SignOutPath, context => context.RedirectAsync(RootPath));

            builder.MapPost(SignInPath, SignInAsync);
            builder.MapPost(SignOutPath, SignOutAsync);

            return builder;
        }

        private static async Task SignInAsync(HttpContext context)
        {
            await context.ChallengeAsync(
                AppleAuthenticationDefaults.AuthenticationScheme,
                new AuthenticationProperties { RedirectUri = RootPath });
        }

        private static async Task SignOutAsync(HttpContext context)
        {
            await context.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new AuthenticationProperties { RedirectUri = RootPath });
        }

        private static Task RedirectAsync(this HttpContext context, string location)
        {
            context.Response.Redirect(location);
            return Task.CompletedTask;
        }
    }
}
