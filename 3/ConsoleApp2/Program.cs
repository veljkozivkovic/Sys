using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ConsoleApp1.Client
{

public class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Enter a keyword to search for articles:");
            var keyword = Console.ReadLine();

            Console.WriteLine("Enter sort option (0: relevancy, 1: popularity, 2: publishedAt):");
            if (!int.TryParse(Console.ReadLine(), out int sortOption))
            {
                sortOption = 0; // Default to relevancy if input is invalid
            }

            Thread.Sleep(2000);
            // Keywords to search for concurrently
            var keywords = new List<string> { "technology", "sports", "politics", "health", "entertainment" };

            // List of threads for concurrent execution
            List<Thread> threads = new List<Thread>();

            foreach (var kw in keywords)
            {
                var thread = new Thread(() => FetchAndPrintArticlesAsync(kw, sortOption).GetAwaiter().GetResult());
                thread.Start();
                threads.Add(thread);
            }

            // Wait for all threads to complete
            foreach (var thread in threads)
            {
                thread.Join();
            }

            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }

        static async Task FetchAndPrintArticlesAsync(string keyword, int sortOption)
        {
            var articles = await FetchArticlesAsync(keyword, sortOption);

            if (articles != null)
            {
                Console.WriteLine($"Found {articles.Count} articles for keyword '{keyword}':");
                foreach (var article in articles)
                {
                    Console.WriteLine($"- Title: {article.Title}\n  Description: {article.Description}\n  URL: {article.Url}\n  URL to Image: {article.UrlToImage}\n  Published At: {article.PublishedAt}\n  Content: {article.Content}\n  Source: {article.Source.Name}\n  Prediction: {article.Prediction}, Score: {article.Score}");
                    Console.WriteLine("-------------------------------------------------");
                }
                Console.WriteLine("====================================");
            }
            else
            {
                Console.WriteLine($"No articles found or an error occurred for keyword '{keyword}'.");
                Console.WriteLine("====================================");
            }
        }

        static async Task<List<Article>> FetchArticlesAsync(string keyword, int sortOption)
        {
            using var client = new HttpClient();

            try
            {
                var response = await client.GetStringAsync($"http://localhost:5050/?keyword={Uri.EscapeDataString(keyword)}&sortOption={sortOption}");

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