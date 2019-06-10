# _Sign In with Apple_ Example Integration

| | Linux/macOS | Windows |
|:-:|:-:|:-:|
| **Build Status** | [![Build status](https://img.shields.io/travis/martincostello/SignInWithAppleSample/master.svg)](https://travis-ci.org/martincostello/SignInWithAppleSample) | [![Build status](https://img.shields.io/appveyor/ci/martincostello/signinwithapplesample/master.svg)](https://ci.appveyor.com/project/martincostello/signinwithapplesample) |

This repository contains a sample implementation of [_Sign In with Apple_](https://developer.apple.com/sign-in-with-apple/) for ASP.NET Core 2.2 written in C#.

## Overview

_Sign In with Apple_ is a way of allowing users of websites to sign in using their Apple ID.

This example integration shows a minimal sample of how to integrate _Sign In with Apple_ a website to authenticate a user using their Apple ID and retrieve their email address (or a relay address to it) and their name*.

_*Retrieving the user's email and name is not yet implemented._

## Setup

To setup the repository to run the sample, perform the steps below:

  1. Install the [.NET Core SDK](https://www.microsoft.com/net/download/core), Visual Studio or Visual Studio Code version compatible with .NET Core 2.2.
  1. Fork this repository.
  1. Clone the repository from your fork to your local machine: `git clone https://github.com/{username}/SignInWithAppleSample.git`
  1. [Follow the steps](https://developer.okta.com/blog/2019/06/04/what-the-heck-is-sign-in-with-apple#how-sign-in-with-apple-works-hint-it-uses-oauth-and-oidc) to obtain your _Client ID_, _Private Key_, _Key ID_ and Domain Verification file, if you do not already have them.
  1. Place the Domain Verification file (`apple-developer-domain-association.txt`) in the `src\SignInWithApple\wwwroot\.well-known` folder.
  1. Either add your `.p8` file contining the private key to generate the client secret to the root of the application in `src\SignInWithApple` (but **not** in the `wwwroot` folder), or use some other mechanism, such as loading it from a blob storage account.
  1. Update the favicon (`src\SignInWithApple\wwwroot\favicon.ico`) to your own design.
  1. Configure the following settings as appropriate in either your environment variables or in `src\SignInWithApple\appsettings.json`:
    * `AppleClientId`
    * `AppleKeyId`
    * `AppleTeamId`
  1. Deploy the application to the hosting environment for the domain where you wish to use _Sign In with Apple_.
  1. Verify the domain in the [Apple Developer Portal](https://developer.apple.com/account/).

You should now be able to sign in with your Apple ID in the deployed application.

## Local Debugging

You should be able to debug the application in [Visual Studio Code](https://code.visualstudio.com/) or [Visual Studio 2019 (16.1 or later)](https://www.visualstudio.com/downloads/).

## Azure App Service Deployment

If you are deploying the sample application to a Microsoft Azure App Service Web App, you will need to make the following configuration changes to your Web App for the sample application to run correctly:

  * Navigate to the _Application settings_ tab of your Web App and add the following settings:
    * `WEBSITE_LOAD_USER_PROFILE` to a value of `1`.
    * Save the changes.
  * Ensure the hostname you are using (either `{yourappname}.azurewebsites.net` or a custom hostname that you have set up) has been added in the Apple Developer portal to your service id and you've added the Apple Developer domain validation file as described in the _Setup_ section above.

## Feedback

Any feedback or issues can be added to the [issues](https://github.com/martincostello/SignInWithAppleSample/issues) for this project in GitHub.

## License

This project is licensed under the [Apache 2.0](https://github.com/martincostello/SignInWithAppleSample/blob/master/LICENSE) license.

## External Links

  * [Sign In with Apple](https://developer.apple.com/sign-in-with-apple/)
  * [Apple Developer Portal](https://developer.apple.com/account/)
  * [Prototyping Sign In with Apple for ASP.NET Core](https://blog.martincostello.com/sign-in-with-apple-prototype-for-aspnet-core/)
