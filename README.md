---
services: active-directory
platforms: dotnet, netcore, osx, linux
author: vibronet
---

Invoking an API protected by Azure AD from a text-only device 

This sample demonstrates how to leverage ADAL .NET to authenticate user calls to a web API (in this case, the directory Graph) from apps that do not have the capability of offering an interactive authentication experience.
The sample uses the OAuth2 device profile flow similar to the one described [here](https://developers.google.com/identity/protocols/OAuth2ForDevices?hl=en). The app is build entirely on .NET Core, hence it can be ran as-is on Windows (including Nano Server), OSX and Linux machines. To emulate a device not capable of showing UX, the sample is packaged as a console application.
The application signs users in with Azure Active Directory (Azure AD), using the Active Directory Authentication Library (ADAL) to obtain a JWT access token through the OAuth 2.0 protocol.  The access token is sent to Azure AD's Graph API to obtain information about other users in their organization.

### About The Sample
If you would like to get started immediately, skip this section and jump to *How To Run The Sample*.

This sample solution is a command line utility that can be used for looking up basic information for users in Azure AD tenants. The project targets .NET Core, hence it can run wherever DNX can run: it has been tested on Windows, OSX and Ubuntu Linux.

The application obtains tokens through a two steps process especially designed for devices and operating systems that cannot display any UX. The idea is that whenever a user authentication is required, the command line app asks the user to use another device (such as an internet-connected smartphone) to navigate to http://aka.ms/devicelogin, where the user will be prompted to enter the code so obtained. That done, the web page will lead the user through a normal authentication experience, including consent prompts and multi factor authentication if necessary. Upon successful authentication, the command line app will receive the required tokens through a back channel and will use it to perform the web API calls it needs.     

The code for handling the token acquisition process is extremely simple, as it boils down to one call for obtaining the code to display to the user (via `AcquireDeviceCodeAsync`) and one call to poll the service to retrieve the tokens when available (via `AcquireTokenByDeviceCodeAsync`). You can find both calls in the sample in the static method `GetTokenViaCode`, from the app root class `Program` in program.cs.
## How To Run The Sample

To run this entire sample you will need:
- An Internet connection
- A Windows machine (necessary if you want to run the app on Windows)
- An OS X machine (necessary if you want to run the app on OSX)
- A Linux machine (necessary if you want to run the app on OSX)
- Visual Studio 2015 (recommended) or DNX command line tools
- An Azure AD tenant
- Azure subscription (a free trial is sufficient. Necessary if you want to register the app in your own tenant)

Every Azure subscription has an associated Azure Active Directory tenant.  If you don't already have an Azure subscription, you can get a free subscription by signing up at [https://azure.microsoft.com](https://azure.microsoft.com).  All of the Azure AD features used by this sample are available free of charge.

### Step 1: Setup DNX