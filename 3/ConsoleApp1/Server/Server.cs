using ConsoleApp1.Models;
using ConsoleApp1.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.Server
{
    public class Server
    {
        private const int Port = 10889;
        private readonly string[] _prefixes = { $"http://localhost:{Port}/", $"http://127.0.0.1:{Port}/" };

        private readonly HttpListener _listener = new();
        private readonly NewsService _NewsService;
        private IDisposable? _subscription;

        public Server(NewsService NewsService)
        {
            foreach (var prefix in _prefixes)
            {
                _listener.Prefixes.Add(prefix);
            }

            _NewsService = NewsService;
            _listener.Start();
            Console.WriteLine($"Listening at...\n{string.Join("\n", _listener.Prefixes)}");
        }

        public void Init()
        {
            if (_subscription != null)
                return;

            _subscription = GetRequestStream().Distinct().Subscribe(
                onNext: ProcessRequest,
                onError: (err) => Console.WriteLine($"Server shutting down due to {err.Message}")
            );
        }

        private void ProcessRequest(HttpListenerContext? context)
        {
            if (context == null)
                return;

            HttpListenerRequest request = context.Request;
            Console.WriteLine($"Processing {request.HttpMethod} request from {request.UserHostAddress} in thread {Thread.CurrentThread.ManagedThreadId}");

            HttpListenerResponse response = context.Response;
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "GET");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Accept");

            Stopwatch stopwatch = new();
            stopwatch.Start();
            string[]? segments = request.Url?.Segments;
            string[]? keywords = segments?[^1].Trim('/').Split('&');

            if (keywords == null || keywords.Length == 0 || keywords.Length > 10)
            {
                SendResponse(GenerateErrorResponse("Error occurred while parsing search words", "Illegal number of arguments"), response, stopwatch, request.HttpMethod, request.UserHostAddress);
                return;
            }

            Dictionary<string, List<Article>> articlesDict = keywords.ToDictionary(w => w, _ => new List<Article>());

            _NewsService.RetrieveArticlesByKeywords(keywords.ToHashSet()).Subscribe(
                article => {
                    Console.WriteLine($"Article '{article.Title}' processed in thread {Thread.CurrentThread.ManagedThreadId}");
                    articlesDict[article.Title].Add(article);
                },
                exception => {
                    Console.WriteLine($"Result sent in thread {Thread.CurrentThread.ManagedThreadId}");
                    SendResponse(GenerateErrorResponse("Error occurred while processing topics", exception.Message), response, stopwatch, request.HttpMethod, request.UserHostAddress);
                },
                () => {
                    Console.WriteLine($"Result sent in thread {Thread.CurrentThread.ManagedThreadId}");
                    SendResponse(GenerateOverviewResponse(articlesDict), response, stopwatch, request.HttpMethod, request.UserHostAddress);
                });
        }

        private void SendResponse(byte[] buffer, HttpListenerResponse response, Stopwatch stopwatch, string method, string userAddress)
        {
            response.ContentLength64 = buffer.Length;
            Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();
            stopwatch.Stop();
            Console.WriteLine($"{method} from {userAddress} successfully processed in {stopwatch.Elapsed.TotalSeconds} seconds");
        }

        private IObservable<HttpListenerContext?> GetRequestStream()
        {
            return Observable.Create<HttpListenerContext?>(async (observer) => {
                while (true)
                {
                    try
                    {
                        var context = await _listener.GetContextAsync();
                        Console.WriteLine($"Request accepted in thread {Thread.CurrentThread.ManagedThreadId}");
                        observer.OnNext(context);
                    }
                    catch (HttpListenerException ex)
                    {
                        observer.OnError(ex);
                        return;
                    }
                    catch (Exception)
                    {
                        observer.OnNext(null);
                    }
                }
            }).ObserveOn(TaskPoolScheduler.Default);
        }

        private byte[] GenerateErrorResponse(params string[] messages)
        {
            // Generates a simple HTML error response
            string html = "<html><body><h1>Error</h1><ul>" + string.Join("", messages.Select(m => $"<li>{m}</li>")) + "</ul></body></html>";
            return System.Text.Encoding.UTF8.GetBytes(html);
        }

        private byte[] GenerateOverviewResponse(Dictionary<string, List<Article>> articlesDict)
        {
            // Generates a simple HTML overview of articles
            string html = "<html><body><h1>Articles</h1><ul>";
            foreach (var keyword in articlesDict.Keys)
            {
                html += $"<h2>{keyword}</h2><ul>";
                foreach (var article in articlesDict[keyword])
                {
                    html += $"<li><a href='{article.Url}'>{article.Title}</a></li>";
                }
                html += "</ul>";
            }
            html += "</ul></body></html>";
            return System.Text.Encoding.UTF8.GetBytes(html);
        }
    }
}
