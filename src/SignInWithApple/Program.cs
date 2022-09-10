// Copyright (c) Martin Costello, 2019. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using MartinCostello.SignInWithApple;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.IdentityModel.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.TryConfigureAzureKeyVault();

builder.Services.AddRazorPages();
builder.Services.AddSignInWithApple();

builder.WebHost.ConfigureKestrel((p) => p.AddServerHeader = false);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    IdentityModelEventSource.ShowPII = true;
}
else
{
    app.UseExceptionHandler("/error")
       .UseStatusCodePages();
}

app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseHsts();
app.UseHttpsRedirection();

var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".webmanifest"] = "application/manifest+json";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider,
    DefaultContentType = "application/json",
    ServeUnknownFileTypes = true // Required to serve the files in the .well-known folder
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthenticationRoutes();
app.MapRazorPages();

app.Run();

namespace MartinCostello.SignInWithApple
{
    public partial class Program
    {
    }
}
