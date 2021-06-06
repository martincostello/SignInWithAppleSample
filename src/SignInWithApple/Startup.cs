// Copyright (c) Martin Costello, 2019. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System;
using AspNet.Security.OAuth.Apple;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Logging;

namespace MartinCostello.SignInWithApple
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddAuthentication(options => options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/signin";
                    options.LogoutPath = "/signout";
                })
                .AddApple();

            // Configuration for the Sign in with Apple provider is applied separately so
            // that any configuration loaded externally, such as from Azure Key Vault, is
            // available at the point that the authentication configuration is validated.
            services
                .AddOptions<AppleAuthenticationOptions>(AppleAuthenticationDefaults.AuthenticationScheme)
                .Configure<IConfiguration, IServiceProvider>((options, configuration, serviceProvider) =>
                {
                    options.AccessDeniedPath = "/denied";
                    options.ClientId = configuration["Apple:ClientId"];
                    options.KeyId = configuration["Apple:KeyId"];
                    options.TeamId = configuration["Apple:TeamId"];

                    var client = serviceProvider.GetService<SecretClient>();

                    if (client is not null)
                    {
                        // Load the private key from Azure Key Vault if available
                        options.UseAzureKeyVaultSecret(
                            (keyId) => client.GetSecretAsync($"AuthKey-{keyId}"));
                    }
                    else
                    {
                        // Otherwise assume the private key is stored locally on disk
                        var environment = serviceProvider.GetRequiredService<IHostEnvironment>();

                        options.UsePrivateKey(
                            (keyId) =>
                                environment.ContentRootFileProvider.GetFileInfo($"AuthKey_{keyId}.p8"));
                    }
                });

            services.AddMvc(options => options.Filters.Add(new RequireHttpsAttribute()));
        }

        public void Configure(IApplicationBuilder app, IHostEnvironment environment)
        {
            if (environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                IdentityModelEventSource.ShowPII = true;
            }
            else
            {
                app.UseExceptionHandler("/Home/Error")
                   .UseStatusCodePages();
            }

            if (environment.IsProduction())
            {
                app.UseHsts()
                   .UseHttpsRedirection();
            }

            var provider = new FileExtensionContentTypeProvider();
            provider.Mappings[".webmanifest"] = "application/manifest+json";

            app.UseStaticFiles(new StaticFileOptions()
            {
                ContentTypeProvider = provider,
                DefaultContentType = "application/json",
                ServeUnknownFileTypes = true, // Required to serve the files in the .well-known folder
            });

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints => endpoints.MapDefaultControllerRoute());
        }
    }
}
