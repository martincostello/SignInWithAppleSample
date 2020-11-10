// Copyright (c) Martin Costello, 2019. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using AspNet.Security.OAuth.Apple;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace MartinCostello.SignInWithApple.Controllers
{
    public class AuthenticationController : Controller
    {
        [HttpGet("~/denied")]
        public IActionResult Denied()
            => Redirect("/?denied=true");

        [HttpPost("~/signin")]
        public IActionResult SignIn()
            => Challenge(new AuthenticationProperties { RedirectUri = "/" }, AppleAuthenticationDefaults.AuthenticationScheme);

        [HttpGet("~/signout")]
        [HttpPost("~/signout")]
        public IActionResult SignOutCurrentUser()
            => SignOut(new AuthenticationProperties { RedirectUri = "/" }, CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
