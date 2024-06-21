using ConsoleApp1.Models;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Reactive.Concurrency;
using System.Reactive.Linq;


namespace ConsoleApp1.Services
{
    public class NewsService
    {
        private const string apiKey = "99b5f9e0a8014cc1811f616cda0380fb";
        private const string newsApiUrl = "https://newsapi.org/v2/everything";
        private HttpClient httpClient { get; } = new();


        //PROUCITI!!!
        //public IObservable<GithubInfo> GetRelatedRepositories(HashSet<string> topics)
        //{
        //    var observables = topics.Select(topic =>
        //        Observable.FromAsync(() => FetchReposAsync(topic)).SelectMany(repos => repos));
        //    return observables.Merge().SubscribeOn(new EventLoopScheduler()).ObserveOn(new EventLoopScheduler());
        //}


        public async Task<IEnumerable<Article>?> FetchArticlesAsync(string keyword)
        {
            var response = await httpClient.GetAsync($"{newsApiUrl}?q={keyword}&sortBy=publishedAt&apiKey={apiKey}");
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var jsonResponse = JObject.Parse(responseBody);

            var articles = jsonResponse["articles"];

            if(articles != null)
            {
                return articles.Select(article => new Article(
                    new Source(
                        article["source"]?["id"]?.ToString(),
                        article["source"]?["name"]?.ToString()
                    ),
                    article["author"]?.ToString(),
                    article["title"]?.ToString(),
                    article["description"]?.ToString(),
                    article["url"]?.ToString(),
                    article["urlToImage"]?.ToString(),
                    DateTime.Parse(article["publishedAt"]?.ToString() ?? DateTime.MinValue.ToString()), //ako nema publishedAt, vrati DateTime.MinValue
                    article["content"]?.ToString()
                ));
            }
            else
            {
                return Enumerable.Empty<Article>();
            }
        }
    }
}
