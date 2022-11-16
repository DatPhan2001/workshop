using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using IdentityModel.Client;
using IdentityModel.OidcClient;
using System.Text.Json;

namespace MoviesNativeClientApp
{
    public class Program
    {
        const string searchUrl = "https://localhost:5009/movies/search?term={0}";
        const string authorityUrl = "https://localhost:5001/";

        public static void Main(string[] args) => MainAsync().GetAwaiter().GetResult();

        public static async Task MainAsync()
        {
            Console.WriteLine("Welcome to the movie search app!");
            Console.WriteLine("Search for a movie!");
            Console.WriteLine();

            //var accessToken = await PromptForPasswordAsync();
            var accessToken = await LaunchBrowserForTokenAsync();
            if (accessToken == null) return;

            await RunSearchLoopAsync(accessToken);
        }

        static async Task RunSearchLoopAsync(string accessToken)
        {
            while (true)
            {
                Console.WriteLine();
                Console.Write("Enter the search term (or just enter to exit the application): ");

                var term = Console.ReadLine();
                if (String.IsNullOrWhiteSpace(term)) return;

                var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var url = String.Format(searchUrl, WebUtility.UrlEncode(term));
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<MovieSearchResult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    ShowResult(result);
                }
                else
                {
                    Console.WriteLine($"Error: {(int)response.StatusCode}");
                    break;
                }
            }
        }

        const int TitleWidth = 30;
        const int YearWidth = 6;
        const int RatingWidth = 8;
        const int CountryWidth = 20;

        const string Separator = "  ";

        private static void ShowResult(MovieSearchResult result)
        {
            Console.WriteLine($"{result.Count} results");
            foreach(var item in result.Movies)
            {
                Console.Write("\t");

                Console.Write(Fit(item.Title, TitleWidth));
                Console.Write(Separator);

                Console.Write(Fit(item.Year.ToString(), YearWidth));
                Console.Write(Separator);

                Console.Write(Fit(item.Rating, RatingWidth));
                Console.Write(Separator);

                Console.Write(Fit(item.CountryName, CountryWidth));
                Console.WriteLine();
            }
        }

        static string Fit(string value, int length)
        {
            value = value ?? String.Empty;
            value = value.Trim();

            if (value.Length >= length)
            {
                value = value.Substring(0, length - 3);
                value += "...";
            }
            else if (value.Length < length)
            {
                value += new String(' ', length - value.Length);
            }

            return value;
        }

        private static async Task<string> PromptForPasswordAsync()
        {
            Console.WriteLine("Enter your credentials.");
            Console.Write("Username: ");
            var username = Console.ReadLine();
            Console.Write("Password: ");
            var password = Console.ReadLine();

			// TODO: create HttpClient to contact IdentityServer
            var client = new HttpClient();

            // TODO: use GetDiscoveryDocumentAsync to get the discovery document
			var discoveryResult = await client.GetDiscoveryDocumentAsync(authorityUrl);

            // TODO: use RequestPasswordTokenAsync to request resource owner password
			var tokenResult = await client.RequestPasswordTokenAsync(new PasswordTokenRequest {
                Address = discoveryResult.TokenEndpoint,
                ClientId = "native_client",
                UserName = username,
                Password = password,
                Scope = "movie_api"
            });

            // TODO: if token result IsError then show the error info and return null from this method
			if (tokenResult.IsError)
            {
                Console.WriteLine($"Error: {tokenResult.Error}");
                if (tokenResult.ErrorDescription != null)
                {
                    Console.WriteLine($"Description: {tokenResult.ErrorDescription}");
                }
            }

            Console.WriteLine();

            return tokenResult.AccessToken;
        }

        private static async Task<string> LaunchBrowserForTokenAsync()
        {
            var browser = new SystemBrowser();

            var opts = new OidcClientOptions
            {
                Authority = authorityUrl,
                Scope = "openid movie_api",
                RedirectUri = "http://127.0.0.1:" + browser.Port,
                ClientId = "native_client",
                Browser = browser,
            };
            // uncomment this line to enable logging
            //opts.LoggerFactory.AddConsole(LogLevel.Trace);

            var client = new OidcClient(opts);
            var request = new LoginRequest();
            var result = await client.LoginAsync(request);
            if (result.IsError)
            {
                Console.WriteLine($"Error: {result.Error}.");
                return null;
            }

            return result.AccessToken;
        }
    }
}
