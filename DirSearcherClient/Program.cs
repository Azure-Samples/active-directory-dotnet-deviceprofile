using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DirSearcherClient
{
    public class Program
    {
        //public const string clientId = "04b07795-8ddb-461a-bbee-02f9e1bf7b46";
        public const string resource = "00000002-0000-0000-c000-000000000000";
        public const string clientId = "b78054de-7478-45a6-be1c-09f696a91d64";
        public void Main(string[] args)
        {
            string commandString = string.Empty;
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("***********************************************************");
            Console.WriteLine("*             Directory Searcher Text Client              *");
            Console.WriteLine("*                                                         *");
            Console.WriteLine("*             Type commands to search users               *");
            Console.WriteLine("*                                                         *");
            Console.WriteLine("***********************************************************");
            Console.WriteLine("");

            // main command cycle
            while (!commandString.Equals("Exit"))
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Enter command (search | clear | printcache | exit | help) >");
                commandString = Console.ReadLine();

                switch (commandString.ToUpper())
                {
                    case "CLEAR":
                        ClearCache();
                        break;
                    case "HELP":
                        Help();
                        break;
                    case "PRINTCACHE":
                        PrintCache();
                        break;
                    case "EXIT":
                        Console.WriteLine("Bye!");
                        return;
                        break;
                    default:
                        if (commandString.ToUpper().StartsWith("SEARCH") && commandString.Split(' ').Count() > 1)
                        {
                            string[] localArgs = commandString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (localArgs.Count() > 2)
                                Search(localArgs[1], localArgs[2]);
                            else
                                Search(localArgs[1], null);
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Invalid command.");
                        }
                        break;
                }
            }
        }
        static void Search(string searchterm, string tenant)
        {
            AuthenticationResult ar = GetToken(tenant);
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
                    HttpResponseMessage response = client.SendAsync(request).Result;

                    string content = response.Content.ReadAsStringAsync().Result;
                    jResult = JObject.Parse(content);
                }
                catch (Exception ee)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error on search");
                    Console.WriteLine(ee.Message);
                }

                if (jResult["odata.error"] != null || jResult["value"] == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error on search");
                }
                Console.ForegroundColor = ConsoleColor.Green;
                if (jResult.Count == 0)
                {
                    Console.WriteLine("No user with alias {0} found. (tenantID: {1})", searchterm, ar.TenantId);
                }
                else
                {
                    Console.WriteLine("Users found.");
                    foreach (JObject result in jResult["value"])
                    {
                        Console.WriteLine("-----");
                        Console.WriteLine("displayName: {0}", (string)result["displayName"]);
                        Console.WriteLine("givenName: {0}", (string)result["givenName"]);
                        Console.WriteLine("surname: {0}", (string)result["surname"]);
                        Console.WriteLine("userPrincipalName: {0}", (string)result["userPrincipalName"]);
                        Console.WriteLine("telephoneNumbe: {0}", (string)result["telephoneNumber"] == null ? "Not Listed." : (string)result["telephoneNumber"]);
                    }
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to obtain a token.");
            }
        }
        static void PrintCache()
        {
            AuthenticationContext ctx = new AuthenticationContext("https://login.microsoftonline.com/common");
            var cacheContent = ctx.TokenCache.ReadItems();
            Console.ForegroundColor = ConsoleColor.Green;
            if (cacheContent.Count() > 0)
            {
                Console.WriteLine("{0,-30} | {1,-15}", "UPN", "TenantId");
                Console.WriteLine("-----------------------------------------------------------------");
                foreach (TokenCacheItem tci in cacheContent)
                {
                    Console.WriteLine("{0,-30} | {1,-15}  ", tci.DisplayableId, tci.TenantId);
                }
                Console.WriteLine("-----------------------------------------------------------------");
            }
            else { Console.WriteLine("The cache is empty."); }
        }
        static void ClearCache()
        {
            AuthenticationContext ctx = new AuthenticationContext("https://login.microsoftonline.com/common");
            ctx.TokenCache.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Token cache cleared.");
        }
        static void Help()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("SEARCH  - searches for a user by alias. Requires sign in. \nUsage: \nsearch <useralias> [domain to search]");
            Console.WriteLine("CLEAR - empties the token cache, allowing you to sign in as a different user");
            Console.WriteLine("PRINTCACHE - displays the current cache content (basic fields only)");
            Console.WriteLine("HELP  - displays this page");
            Console.WriteLine("EXIT  - closes this program");
            Console.WriteLine("");
        }

        static AuthenticationResult GetToken(string tenant)
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
                result = ctx.AcquireTokenSilentAsync(resource, clientId).Result;
            }
            catch (Exception exc)
            {
                var adalEx = exc.InnerException as AdalException;
                if ((adalEx != null) && (adalEx.ErrorCode == "failed_to_acquire_token_silently"))
                {
                    result = GetTokenViaCode(ctx);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Something went wrong.");
                    Console.WriteLine("Message: " + exc.InnerException.Message + "\n");
                }
            }
            return result;

        }
        static AuthenticationResult GetTokenViaCode(AuthenticationContext ctx)
        {
            AuthenticationResult result = null;
            try
            {
                DeviceCodeResult codeResult = ctx.AcquireDeviceCodeAsync(resource, clientId).Result;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("You need to sign in.");
                Console.WriteLine("Message: " + codeResult.Message + "\n");
                result = ctx.AcquireTokenByDeviceCodeAsync(codeResult).Result;
            }
            catch (Exception exc)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Something went wrong.");
                Console.WriteLine("Message: " + exc.Message + "\n");
            }
            return result;
        }
    }
}
