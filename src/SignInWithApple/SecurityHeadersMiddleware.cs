// Copyright (c) Martin Costello, 2019. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using Microsoft.Net.Http.Headers;

namespace MartinCostello.SignInWithApple;

internal sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    private static readonly string ContentSecurityPolicy = string.Join(
        ';',
        "default-src 'self'",
        "script-src 'self' cdnjs.cloudflare.com",
        "script-src-elem 'self' cdnjs.cloudflare.com",
        "style-src 'self' cdnjs.cloudflare.com use.fontawesome.com",
        "style-src-elem 'self' cdnjs.cloudflare.com",
        "img-src 'self' data:",
        "font-src 'self' cdnjs.cloudflare.com",
        "connect-src 'self' cdnjs.cloudflare.com",
        "media-src 'none'",
        "object-src 'none'",
        "child-src 'none'",
        "frame-ancestors 'none'",
        "form-action 'self' appleid.apple.com",
        "block-all-mixed-content",
        "base-uri 'self'",
        "manifest-src 'self'",
        "upgrade-insecure-requests");

    public Task Invoke(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.Remove(HeaderNames.Server);
            context.Response.Headers.Remove(HeaderNames.XPoweredBy);

            context.Response.Headers.ContentSecurityPolicy = ContentSecurityPolicy;

            context.Response.Headers["Cross-Origin-Embedder-Policy"] = "unsafe-none";
            context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
            context.Response.Headers["Cross-Origin-Resource-Policy"] = "same-origin";

            if (context.Request.IsHttps)
            {
                context.Response.Headers["Expect-CT"] = "max-age=1800";
            }

            context.Response.Headers["Permissions-Policy"] = "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()";
            context.Response.Headers["Referrer-Policy"] = "no-referrer-when-downgrade";
            context.Response.Headers.XContentTypeOptions = "nosniff";
            context.Response.Headers["X-Download-Options"] = "noopen";

            if (!context.Response.Headers.ContainsKey(HeaderNames.XFrameOptions))
            {
                context.Response.Headers.XFrameOptions = "DENY";
            }

            context.Response.Headers.XXSSProtection = "1; mode=block";

            return Task.CompletedTask;
        });

        return next(context);
    }
}
