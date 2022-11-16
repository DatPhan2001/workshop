using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace MoviesWebApp
{
    public class MoviesController : ControllerBase
    {
        private readonly HttpClient _client;

        public MoviesController(IHttpClientFactory httpClientFactory)
        {
            _client = httpClientFactory.CreateClient("api");
        }

        [HttpGet("/api/movies")]
        public async Task<IActionResult> GetMovies(int page)
        {
            // TODO: get the token from the current user's session

            var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost:5009/movies?page=" + page);
            
            // TODO: set the token as the authorization header on the request

            var response = await _client.SendAsync(request);

            var data = await response.Content.ReadAsStringAsync();

            return new ContentResult()
            {
                Content = data,
                ContentType = "application/json"
            };
        }
    }
}
