﻿using ConsoleApp1.Models;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Reactive.Concurrency;
using System.Reactive.Linq;


namespace ConsoleApp1.Services
{
    public class NewsService
    {
        private const string apiKey = "0796d3f80fa147d1beb194cbd0cbc6e5";

        //0796d3f80fa147d1beb194cbd0cbc6e5
        //99b5f9e0a8014cc1811f616cda0380fb
        private const string newsApiUrl = "https://newsapi.org/v2/everything";
        private HttpClient httpClient { get; } = new();


        //PROUCITI!!!
        //public IObservable<GithubInfo> GetRelatedRepositories(HashSet<string> topics)
        //{
        //    var observables = topics.Select(topic =>
        //        Observable.FromAsync(() => FetchReposAsync(topic)).SelectMany(repos => repos));
        //    return observables.Merge().SubscribeOn(new EventLoopScheduler()).ObserveOn(new EventLoopScheduler());
        //}
        //public IObservable<Article> GetRelatedArticles(string keywords)
        //{
        //    var observables = keywords.Select(keyword =>
        //        Observable.FromAsync(() => FetchArticlesAsync(keywords)).SelectMany(articles => articles));

        //    return observables.Merge().SubscribeOn(new EventLoopScheduler()).ObserveOn(new EventLoopScheduler());
        //}


        public async Task<IEnumerable<Article>?> FetchArticlesAsync(string keyword, int sortOption)
        {
            // Mapiranje sortOption vrednosti na sortBy parametar
            string sortBy = sortOption switch
            {
                // i 0 ukazuje na relevancy
                1 => "popularity",  // Sortiranje po popularnosti
                2 => "publishedAt", // Sortiranje po datumu objave
                _ => "relevancy"  // Default : Sortiranje po relevantnosti
            };

            var encodedKeyword = Uri.EscapeDataString(keyword);
            var url = $"{newsApiUrl}?q={encodedKeyword}&language=en&sortBy={sortBy}&apiKey={apiKey}";

            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.Add("User-Agent", "FaksProba");

                var response = await httpClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    // Log or handle the error as needed
                    return null;
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                var jsonResponse = JObject.Parse(responseBody);

                var articles = jsonResponse["articles"];

                if (articles != null)
                {
                    return articles
                        .Where(article => article["source"]?["name"]?.ToString() != "[Removed]")
                        .Select(article => new Article(
                            new Source(
                                article["source"]?["id"]?.ToString(),
                                article["source"]?["name"]?.ToString()
                            ),
                            article["author"]?.ToString(),
                            article["title"]?.ToString(),
                            article["description"]?.ToString(),
                            article["url"]?.ToString(),
                            article["urlToImage"]?.ToString(),
                            DateTime.Parse(article["publishedAt"]?.ToString() ?? DateTime.MinValue.ToString()), // ako nema publishedAt, vrati DateTime.MinValue
                            article["content"]?.ToString()
                        ));
                }
                else
                {
                    return Enumerable.Empty<Article>();
                }
            }
        }





        //    public IObservable<Article> RetrieveArticlesByKeywords(string keywords)
        //{
        //    // Pretvaranje skupa ključnih reči u Observable sekvencu
        //    var keywordObservable = keywords.ToObservable();

        //    // Dohvaćanje članaka za svaku ključnu reč i ravnanje rezultata u jednu sekvencu
        //    var articleObservable = keywordObservable.SelectMany(keyword =>
        //        Observable.FromAsync(async () =>
        //        {
        //            var response = await httpClient.GetAsync($"{newsApiUrl}?q={keyword}&sortBy=publishedAt&apiKey={apiKey}");
        //            response.EnsureSuccessStatusCode();

        //            var responseBody = await response.Content.ReadAsStringAsync();
        //            var jsonResponse = JObject.Parse(responseBody);

        //            var articles = jsonResponse["articles"];
        //            return articles != null
        //                ? articles.Select(article => new Article(
        //                    new Source(
        //                        article["source"]?["id"]?.ToString(),
        //                        article["source"]?["name"]?.ToString()
        //                    ),
        //                    article["author"]?.ToString(),
        //                    article["title"]?.ToString(),
        //                    article["description"]?.ToString(),
        //                    article["url"]?.ToString(),
        //                    article["urlToImage"]?.ToString(),
        //                    DateTime.TryParse(article["publishedAt"]?.ToString(), out var publishedAt) ? publishedAt : DateTime.MinValue,
        //                    article["content"]?.ToString()
        //                )).ToList()
        //                : new List<Article>();
        //        }).SelectMany(articles => articles.ToObservable())
        //    );

        //    // Vraćanje Observable sekvence članaka sa specificiranim SubscribeOn i ObserveOn schedulerima
        //    return articleObservable.SubscribeOn(TaskPoolScheduler.Default).ObserveOn(TaskPoolScheduler.Default);
        //}

















    }
}
