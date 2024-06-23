using ConsoleApp1.Models;
using ConsoleApp1.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ConsoleApp1.Server
{
    public class Server
    {
        private const int Port = 5050;
        private readonly string _prefix =  $"http://localhost:{Port}/" ;

        private readonly HttpListener _listener = new();
        private readonly NewsService _NewsService;
        private IDisposable? _subscription;

        public Server(NewsService NewsService)
        {
            
               _listener.Prefixes.Add(_prefix);
            

            _NewsService = NewsService;
            _listener.Start();
            Console.WriteLine($"Listening at...\n{string.Join("\n", _listener.Prefixes)}");
        }

        public void Init()
        {
            if (_subscription != null)
                return;

            _subscription = Observable.Create<HttpListenerContext>(obs =>
            {
                return Observable.FromAsync(() =>
                {
                    Console.WriteLine($"GetContextAsync called on thread {Thread.CurrentThread.ManagedThreadId}");
                    return _listener.GetContextAsync();
                })
                .Repeat()
                .TakeWhile(_ => _listener.IsListening)
                .Subscribe(obs.OnNext, obs.OnError, obs.OnCompleted);
            })
            .SubscribeOn(NewThreadScheduler.Default) // Obrada inicijalne pretplate na novoj niti
            .Do(_ => Console.WriteLine($"Subscribed on thread {Thread.CurrentThread.ManagedThreadId}"))
            .ObserveOn(TaskPoolScheduler.Default) // Obrada svakog zahteva na zasebnoj niti iz pool-a
            .Do(_ => Console.WriteLine($"Observed on thread {Thread.CurrentThread.ManagedThreadId}"))
            .SelectMany(context =>
                Observable.FromAsync(async () =>
                {
                    try
                    {
                        // Pretpostavimo da keyword i sortOption dolaze iz URL-a
                        var queryString = HttpUtility.ParseQueryString(context.Request.Url.Query);
                        var keyword = queryString.Get("keyword");
                        var sortOptionStr = queryString.Get("sortOption");
                        Console.WriteLine($"Processing request on thread {Thread.CurrentThread.ManagedThreadId} for keyword '{keyword}' with sort option '{sortOptionStr}'");

                        if (string.IsNullOrEmpty(keyword))
                        {
                            var response = context.Response;
                            var errorMessage = "Nevalidan upit.";
                            var buffer = Encoding.UTF8.GetBytes(errorMessage);
                            response.ContentType = "text/plain; charset=utf-8"; // Postavljamo Content-Type sa UTF-8 kodiranjem
                            response.StatusCode = 400; // Bad Request
                            response.ContentLength64 = buffer.Length;
                            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                            response.Close();
                            return (null, null); // Vraćamo null vrednosti kako bismo ih kasnije filtrirali
                        }

                        // Parsiranje sortOption parametra
                        int sortOption = 0; // Default to relevancy
                        if (!string.IsNullOrEmpty(sortOptionStr) && int.TryParse(sortOptionStr, out int parsedSortOption))
                        {
                            sortOption = parsedSortOption;
                        }

                        var articles = await _NewsService.FetchArticlesAsync(keyword, sortOption);
                        if (articles == null || !articles.Any())
                        {
                            var response = context.Response;
                            var errorMessage = $"Članci sa ključnom rečju '{keyword}' ne postoje.";
                            var buffer = Encoding.UTF8.GetBytes(errorMessage);
                            response.ContentType = "text/plain; charset=utf-8"; // Postavljamo Content-Type sa UTF-8 kodiranjem
                            response.StatusCode = 404; // Not Found
                            response.ContentLength64 = buffer.Length;
                            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                            response.Close();
                            return (null, null); // Vraćamo null vrednosti kako bismo ih kasnije filtrirali
                        }

                        return (context, articles);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing request: {ex.Message} on thread {Thread.CurrentThread.ManagedThreadId}");
                        context.Response.StatusCode = 500; // Internal Server Error
                        context.Response.Close();
                        return (null, null); // U slučaju greške, vraćamo null vrednosti
                    }
                }))
            .Where(result => result.Item1 != null && result.Item2 != null) // Filtriramo null vrednosti
            .Subscribe(result =>
            {
                var (context, articles) = result;
                Console.WriteLine($"Sending response on thread {Thread.CurrentThread.ManagedThreadId}");
                var response = context.Response;
                var buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(articles));
                response.ContentType = "application/json; charset=utf-8"; // Postavljamo Content-Type sa UTF-8 kodiranjem
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.Close();
            }, error =>
            {
                Console.WriteLine($"Error: {error.Message} on thread {Thread.CurrentThread.ManagedThreadId}");
            });
        }





        public void Stop()
        {
            if (_subscription != null)
            {
                _subscription.Dispose(); // Otkazivanje pretplate
                _subscription = null; // Resetovanje _subscription na null
                _listener.Stop(); // Zaustavljanje HttpListener-a
                Console.WriteLine("\nServer stopped.");
            }
        }

    }
}
