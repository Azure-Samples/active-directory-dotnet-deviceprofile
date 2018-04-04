using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace DirSearcherClient
{
    public class Program
    {
        public const string resource = "https://graph.microsoft.com";
        public const string clientId = "f5b5b9c9-c68b-45c5-8f57-bcf3f2f15f26";
        public static void Main(string[] args)
        {
            string commandString = string.Empty;
            Console.ForegroundColor = ConsoleColor.White;

            // You might want to enable logging (by setting this boolean to true)
            LoggerCallbackHandler.UseDefaultLogging = false;

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
                Console.ResetColor();
                Console.WriteLine("Enter command (search <upn> <tenantname> | clear | printcache | exit | help) >");
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
                    default:
                        if (commandString.ToUpper().StartsWith("SEARCH") && commandString.Split(' ').Count() > 1)
                        {
                            string[] localArgs = commandString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (localArgs.Count() > 2)
                                Search(localArgs[1], localArgs[2]).Wait();
                            else
                                Search(localArgs[1], null).Wait();
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
        static async Task Search(string searchterm, string tenant)
        {
            AuthenticationResult ar = await GetToken(tenant);
            if (ar != null)
            {
                JObject jResult = null;
                try
                {
                    string graphRequest = $"{resource}/v1.0/users?$filter=mailNickname eq '{searchterm}'";
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

                if (jResult == null || jResult["odata.error"] != null || jResult["value"] == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error on search");
                }
                Console.ForegroundColor = ConsoleColor.Green;
                if (jResult != null)
                {
                    if (jResult.Count == 0)
                    {
                        Console.WriteLine("No user with alias {0} found. (tenantID: {1})", searchterm, ar.TenantId);
                    }
                    else
                    {
                        Console.WriteLine("Users found.");
                        foreach (JToken result in jResult["value"])
                        {
                            Console.WriteLine("-----");
                            Console.WriteLine("displayName: {0}", (string)result["displayName"]);
                            Console.WriteLine("givenName: {0}", (string)result["givenName"]);
                            Console.WriteLine("surname: {0}", (string)result["surname"]);
                            Console.WriteLine("userPrincipalName: {0}", (string)result["userPrincipalName"]);
                            Console.WriteLine("telephoneNumber: {0}", (string)result["telephoneNumber"] ?? "Not Listed.");
                        }
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
            if (cacheContent.Any())
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
            catch (AdalSilentTokenAcquisitionException)
            {
                result = await GetTokenViaCode(ctx);
            }
            catch (AdalException exc)
            {
                PrintError(exc);
            }
            return result;

        }

        private static void PrintError(Exception exc)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Something went wrong.");
            Console.WriteLine("Message: " + exc.Message + "\n");
        }

        static async Task<AuthenticationResult> GetTokenViaCode(AuthenticationContext ctx)
        {
            AuthenticationResult result = null;
            try
            {
                DeviceCodeResult codeResult = await ctx.AcquireDeviceCodeAsync(resource, clientId);
                Console.ResetColor();
                Console.WriteLine("You need to sign in.");
                Console.WriteLine("Message: " + codeResult.Message + "\n");
                result = await ctx.AcquireTokenByDeviceCodeAsync(codeResult);
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
