---
services: active-directory
platforms: dotnet, netcore, osx, linux
author: vibronet
---

# Invoking an API protected by Azure AD from a text-only device

This sample demonstrates how to leverage ADAL .NET to authenticate user calls to a web API (in this case, the directory Graph) from apps that do not have the capability of offering an interactive authentication experience.
The sample uses the OAuth2 device profile flow similar to the one described [here](https://developers.google.com/identity/protocols/OAuth2ForDevices?hl=en). The app is build entirely on .NET Core, hence it can be ran as-is on Windows (including Nano Server), OSX and Linux machines. To emulate a device not capable of showing UX, the sample is packaged as a console application.
The application signs users in with Azure Active Directory (Azure AD), using the Active Directory Authentication Library (ADAL) to obtain a JWT access token through the OAuth 2.0 protocol.  The access token is sent to Azure AD's Graph API to obtain information about other users in their organization.

### About The Sample
If you would like to get started immediately, skip this section and jump to *How To Run The Sample*.

This sample solution is a command line utility that can be used for looking up basic information for users in Azure AD tenants. The project targets .NET Core, hence it can run wherever DNX can run: it has been tested on Windows, OSX and Ubuntu Linux.

The application obtains tokens through a two steps process especially designed for devices and operating systems that cannot display any UX. The idea is that whenever a user authentication is required, the command line app asks the user to use another device (such as an internet-connected smartphone) to navigate to http://aka.ms/devicelogin, where the user will be prompted to enter the code so obtained. That done, the web page will lead the user through a normal authentication experience, including consent prompts and multi factor authentication if necessary. Upon successful authentication, the command line app will receive the required tokens through a back channel and will use it to perform the web API calls it needs.     

The code for handling the token acquisition process is extremely simple, as it boils down to one call for obtaining the code to display to the user (via `AcquireDeviceCodeAsync`) and one call to poll the service to retrieve the tokens when available (via `AcquireTokenByDeviceCodeAsync`). You can find both calls in the sample in the static method `GetTokenViaCode`, from the app root class `Program` in program.cs.

## How To Run The Sample

To run this sample you will need:
- An Internet connection
- A Windows machine (necessary if you want to run the app on Windows)
- An OS X machine (necessary if you want to run the app on OSX)
- A Linux machine (necessary if you want to run the app on OSX)
- Visual Studio 2015 (recommended) or DNX command line tools
- An Azure AD tenant
- Azure subscription (a free trial is sufficient. Necessary if you want to register the app in your own tenant)

Every Azure subscription has an associated Azure Active Directory tenant.  If you don't already have an Azure subscription, you can get a free subscription by signing up at [https://azure.microsoft.com](https://azure.microsoft.com).  All of the Azure AD features used by this sample are available free of charge.

### Step 1: Setup DNX

The ASP.NET documentation pages provide step by step instructions for installing ASP.NET 5 and DNX (the .NET Execution Environment) for your platform of choice.
#### Windows
If you are targeting Windows, see [this document](https://docs.asp.net/en/latest/getting-started/installing-on-windows.html).
(this readme assumes you will follow the instructions for installing ASP.NET 5 with Visual Studio, but you can also go for the standalone option).
#### Mac OS X
If you are targeting Mac OS X, please follow the instructions [here](https://docs.asp.net/en/latest/getting-started/installing-on-mac.html). 
#### Linux
If you are targeting Linux, please follow the instructions [here](https://docs.asp.net/en/latest/getting-started/installing-on-linux.html). This sample has been tested on Ubuntu 

### Step 2: Clone or download this repository

Once you have completed the DNX setup, from your shell or command line run:

git clone https://github.com/Azure-Samples/active-directory-dotnet-deviceprofile.git 

or download and exact the repository .zip file.


### Step 3: Run the sample

Starting up the sample requires slightly different steps on different platforms, but once it runs it presents the exact same surface across all OSes.   

#### Starting up the sample on Windows
Open the solution in Visual Studio, restore the NuGet packages, select the project and start it in the debugger.

#### Starting up the sample on Mac OS X & Linux
Open a terminal and navigate to the project folder (DirSearcherClient).
Restore the packages with the following command:
    dnu restore
Launch the app by entering the following command:
    dnx run

#### Operating the sample

As soon as you start the sample, you will be presented with the following prompt.

>  Enter command (search | clear | printcache | exit | help) >

To see the device code authentication experience in action, enter a command for searching a user in one of your tenants. In my case, I want to search a user with alias `mario` in my tenant `developertenant.onmicrosoft.com`. Hence, the command I'll enter will be:

> search mario developertenant.onmicrosoft.com

The app will respond with the following prompt.

> You need to sign in.
> Message: To sign in, use a web browser to open the page https://aka.ms/devicelogin. Enter the code B7D3SVXHV to authenticate. If you're signing in as an Azure AD application, use the --username and --password parameters.

Open a browser on any device (common choices are the computer on which you are running the sample, or even your smartphone) and navigate to [https://aka.ms/devicelogin](https://aka.ms/devicelogin). Once there, type in the code provided by the app (in this sample, I am typing `B7D3SVXHV`) and hit enter.
The web page will proceed to prompt you for authentication: please authenticate as a user (native or guest) in the tenant that you specified in the search command. Note that, thanks t the fact that you are using an external browser or a different, browser capable device, you can authenticate without restrictions: for example, if your tenant requires you to authenticate using MFA, you are able to do so. That would not have been possible if you would have had to drive the authentication operations exclusively in the console
Once you successfully authenticate, go back to the console app. You'll see that the app has now access to the token it needs to query the Directory Graph API. In my case, a mario does exist in my tenant: hence I'll receive the following result.

> Users found.
> displayName: Mario Rossi
> givenName: Mario
> surname: Rossi
> userPrincipalName: mario@developertenant.onmicrosoft.com
> telephoneNumbe: Not Listed.
> Enter command (search | clear | printcache | exit | help) >
> > 

As a next step, you can search for other users. If you search for users in the same tenant, you'll be able to perform the query without extra prompts. If you indicate a new tenant, you will be prompted again. Note that, if your authenticated user is provisioned in the new tenant and your sample app already received consent in that tenant, you'll be able to get tokens for the new tenant without extra prompts as well.   

### Optional: configure the sample as an app in your directory tenant

The instructions so far leveraged the Azure AD entry for the app in our test tenant: given that the app is multitenant, anybody can run the sample against that app entry.
Below you'll find instructions to provision the sample in your own tenant, so that you can exercise complete control on the app settings and behavior. 

#### Register the app in your tenant

1. Sign in to the Azure management portal.
2. Click on Active Directory in the left hand nav.
3. Click the directory tenant where you wish to register the sample application.
4. Click the Applications tab.
5. In the drawer, click Add.
6. Click "Add an application my organization is developing".
7. Enter a friendly name for the application, for example "DirSearcherClient", select "Native Client Application", and click next.
8. Enter a Redirect Uri value of your choosing and of form http://MyDirSearcherApp. However note that for the flow in this sample, it will not be used.
9. While still in the Azure portal, click the Configure tab of your application.
10. Find the Client ID value and copy it aside, you will need this later when configuring your application.
11. In the Permissions to Other Applications configuration section, ensure that "Access your organization's directory" and "Enable sign-on and read user's profiles" are selected under "Delegated permissions" for Azure Active Directory. Save the configuration.


#### Update the sample code to point to the app entry in your tenant

1.Open the solution in Visual Studio 2015.
2.Open the Program.cs file in the DirSearcherClient project.
3.Find the clientId member variable and replace its value with the Client Id you copied from the Azure portal.

