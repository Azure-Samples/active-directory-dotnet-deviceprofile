using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Globalization;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace DirSearcherClient
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public const string resource = "00000002-0000-0000-c000-000000000000";
        public const string clientId = "b78054de-7478-45a6-be1c-09f696a91d64";

        public MainPage()
        {
            this.InitializeComponent();

            Search("laksberg@hotmail.com", "laksberghotmail.onmicrosoft.com");
        }

        static async Task Search(string searchterm, string tenant)
        {
            AuthenticationResult ar = await GetToken(tenant);
            if (ar != null)
            {
                JObject jResult = null;

                string graphResourceUri = "https://graph.windows.net";
                string graphApiVersion = "2013-11-08";

                try
                {
                    string graphRequest = String.Format(CultureInfo.InvariantCulture, "{0}/{1}/users?api-version={2}&$filter=mailNickname eq '{3}'", graphResourceUri, ar.TenantId, graphApiVersion, searchterm);
                    HttpClient client = new HttpClient();
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, graphRequest);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ar.AccessToken);
                    HttpResponseMessage response = await client.SendAsync(request);

                    string content = await response.Content.ReadAsStringAsync();
                    jResult = JObject.Parse(content);
                }
                catch (Exception ee)
                {
                    System.Diagnostics.Debug.WriteLine("Error on search");
                    System.Diagnostics.Debug.WriteLine(ee.Message);
                }

                if (jResult["odata.error"] != null || jResult["value"] == null)
                {
                    System.Diagnostics.Debug.WriteLine("Error on search");
                }
                else
                {
                    if (jResult.Count == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("No user with alias {0} found. (tenantID: {1})", searchterm, ar.TenantId);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Users found.");
                        foreach (JObject result in jResult["value"])
                        {
                            System.Diagnostics.Debug.WriteLine("-----");
                            System.Diagnostics.Debug.WriteLine("displayName: {0}", (string)result["displayName"]);
                            System.Diagnostics.Debug.WriteLine("givenName: {0}", (string)result["givenName"]);
                            System.Diagnostics.Debug.WriteLine("surname: {0}", (string)result["surname"]);
                            System.Diagnostics.Debug.WriteLine("userPrincipalName: {0}", (string)result["userPrincipalName"]);
                            System.Diagnostics.Debug.WriteLine("telephoneNumbe: {0}", (string)result["telephoneNumber"] == null ? "Not Listed." : (string)result["telephoneNumber"]);
                        }
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Failed to obtain a token.");
            }
        }

        static async Task<AuthenticationResult> GetToken(string tenant)
        {
            AuthenticationContext ctx = null;
            if (tenant != null)
                ctx = new AuthenticationContext("https://login.microsoftonline.com/" + tenant);
            else
            {
                ctx = new AuthenticationContext("https://login.microsoftonline.com/common");
                if (ctx.TokenCache.Count > 0)
                {
                    string homeTenant = ctx.TokenCache.ReadItems().First().TenantId;
                    ctx = new AuthenticationContext("https://login.microsoftonline.com/" + homeTenant);
                }
            }
            AuthenticationResult result = null;
            try
            {
                result = await ctx.AcquireTokenSilentAsync(resource, clientId);
            }
            catch (Exception exc)
            {
                if(exc is AdalException || exc.InnerException is AdalException)
                {
                    result = await GetTokenViaCode(ctx);
                }
                else
                {

                    System.Diagnostics.Debug.WriteLine("Something went wrong.");
                    System.Diagnostics.Debug.WriteLine("Message: " + exc.InnerException.Message + "\n");
                }
            }
            return result;
        }

        static async Task<AuthenticationResult> GetTokenViaCode(AuthenticationContext ctx)
        {
            AuthenticationResult result = null;
            try
            {
                DeviceCodeResult codeResult = await ctx.AcquireDeviceCodeAsync(resource, clientId);
                System.Diagnostics.Debug.WriteLine("You need to sign in.");
                System.Diagnostics.Debug.WriteLine("Message: " + codeResult.Message + "\n");
                result = await ctx.AcquireTokenByDeviceCodeAsync(codeResult);
            }
            catch (Exception exc)
            {
                System.Diagnostics.Debug.WriteLine("Something went wrong.");
                System.Diagnostics.Debug.WriteLine("Message: " + exc.Message + "\n");
            }
            return result;
        }
    }
}
