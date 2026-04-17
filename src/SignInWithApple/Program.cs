// Copyright (c) Martin Costello, 2019. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using MartinCostello.SignInWithApple;
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

app.MapStaticAssets()
   .ShortCircuit();

app.MapGet("apple-app-site-association", static (IWebHostEnvironment environment) =>
{
    var file = environment.WebRootFileProvider.GetFileInfo("apple-app-site-association.json");

    if (!file.Exists)
    {
        return Results.NotFound();
    }

    return Results.File(file.CreateReadStream(), contentType: "application/json");
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthenticationRoutes();
app.MapRazorPages();

app.Run();

namespace MartinCostello.SignInWithApple
{
    public partial class Program;
}
