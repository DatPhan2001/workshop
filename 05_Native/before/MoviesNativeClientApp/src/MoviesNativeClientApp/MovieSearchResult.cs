using System.Collections.Generic;

namespace MoviesNativeClientApp
{
    public class MovieSearchResult
    {
        public string SearchTerm { get; set; }
        public int Count { get; set; }
        public IEnumerable<Movie> Movies { get; set; }
    }
}
