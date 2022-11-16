using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace MoviesWebApp
{
    //public class MoviesController : ControllerBase
    //{
    //    private readonly HttpClient _client;

    //    public MoviesController(IHttpClientFactory httpClientFactory)
    //    {
    //        _client = httpClientFactory.CreateClient("api");
    //    }

    //    [HttpGet("/api/movies")]
    //    public async Task<IActionResult> GetMovies(int page)
    //    {
    //        var token = await HttpContext.GetUserAccessTokenAsync();

    //        var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost:5009/movies?page=" + page);
    //        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    //        var response = await _client.SendAsync(request);
            
    //        var data = await response.Content.ReadAsStringAsync();
            
    //        return new ContentResult() { 
    //            Content = data,
    //            ContentType = "application/json"
    //        };
    //    }
    //}
}
