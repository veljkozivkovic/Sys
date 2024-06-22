using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ConsoleApp1.Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Enter a keyword to search for articles:");
            var keyword = Console.ReadLine();

            var articles = await FetchArticlesAsync(keyword);

            if (articles != null)
            {
                Console.WriteLine($"Found {articles.Count} articles for keyword '{keyword}':");
                foreach (var article in articles)
                {
                    Console.WriteLine($"- Title: {article.Title}\n {article.Content}\n Source: {article.Source.Name}\n Prediction: {article.Prediction} , Score: {article.Score}");
                    Console.WriteLine("-------------------------------------------------");
                }
            }
            else
            {
                Console.WriteLine("No articles found or an error occurred.");
            }

            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }

        static async Task<List<Article>> FetchArticlesAsync(string keyword)
        {
            using var client = new HttpClient();

            try
            {

                var response = await client.GetStringAsync($"http://localhost:5050/?keyword={Uri.EscapeDataString(keyword)}");

                return JsonConvert.DeserializeObject<List<Article>>(response);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
                return null;
            }
        }
    }

    public class Article
    {
        public Source Source { get; set; }
        public string? Author { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Url { get; set; }
        public string? UrlToImage { get; set; }
        public DateTime PublishedAt { get; set; }
        public string? Content { get; set; }
        public string? Prediction { get; set; }
        public double? Score { get; set; }
    }

    public class Source
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
    }
}