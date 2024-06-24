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
using System.Reactive.Threading.Tasks;
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
                .SubscribeOn(NewThreadScheduler.Default) // Obrada inicijalne pretplate na novoj niti
                .Do(_ => Console.WriteLine($"Subscribed on thread {Thread.CurrentThread.ManagedThreadId}"))
                .ObserveOn(TaskPoolScheduler.Default) // Obrada svakog zahteva na zasebnoj niti iz pool-a
                .SelectMany(context =>
                    Observable.Return(context)
                    .ObserveOn(TaskPoolScheduler.Default) // Dodeli novu nit pre asinhronog poziva
                    .SelectMany(ctx =>
                        Observable.FromAsync(async () =>
                        {
                            var stopwatch = Stopwatch.StartNew(); // Početak merenja vremena

                            try
                            {
                                // Pretpostavimo da keyword i sortOption dolaze iz URL-a
                                var queryString = HttpUtility.ParseQueryString(ctx.Request.Url.Query);
                                var keyword = queryString.Get("keyword");
                                var sortOptionStr = queryString.Get("sortOption");
                                Console.WriteLine($"Processing request on thread {Thread.CurrentThread.ManagedThreadId} for keyword '{keyword}' with sort option '{sortOptionStr}'");

                                if (string.IsNullOrEmpty(keyword))
                                {
                                    var response = ctx.Response;
                                    var errorMessage = "Nevalidan upit.";
                                    var buffer = Encoding.UTF8.GetBytes(errorMessage);
                                    response.ContentType = "text/plain; charset=utf-8"; // Postavljamo Content-Type sa UTF-8 kodiranjem
                                    response.StatusCode = 400; // Bad Request
                                    response.ContentLength64 = buffer.Length;
                                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                                    response.Close();
                                    return (null, null, 0); // Vraćamo null vrednosti kako bismo ih kasnije filtrirali i vreme obrade 0
                                }

                                // Parsiranje sortOption parametra
                                int sortOption = 0; // Default to relevancy
                                if (!string.IsNullOrEmpty(sortOptionStr) && int.TryParse(sortOptionStr, out int parsedSortOption))
                                {
                                    sortOption = parsedSortOption;
                                }

                                

                                var articles = await _NewsService.FetchArticlesAsync(keyword, sortOption);

                                

                                stopwatch.Stop();
                                var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                                if (articles == null || !articles.Any())
                                {
                                    var response = ctx.Response;
                                    var errorMessage = $"Članci sa ključnom rečju '{keyword}' ne postoje.";
                                    var buffer = Encoding.UTF8.GetBytes(errorMessage);
                                    response.ContentType = "text/plain; charset=utf-8"; // Postavljamo Content-Type sa UTF-8 kodiranjem
                                    response.StatusCode = 404; // Not Found
                                    response.ContentLength64 = buffer.Length;
                                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                                    response.Close();
                                    return (null, null, elapsedMilliseconds); // Vraćamo null vrednosti kako bismo ih kasnije filtrirali i vreme obrade
                                }

                                

                                return (ctx, articles, elapsedMilliseconds);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error processing request: {ex.Message} on thread {Thread.CurrentThread.ManagedThreadId}");
                                ctx.Response.StatusCode = 500; // Internal Server Error
                                ctx.Response.Close();
                                return (null, null, 0); // U slučaju greške, vraćamo null vrednosti i vreme obrade 0
                            }
                        })
                        .ObserveOn(TaskPoolScheduler.Default) // Dodajemo ObserveOn ovde kako bismo osigurali da se svaki korak obrade dešava na različitim nitima iz pool-a
                    )
                )
                .Where(result => result.Item1 != null && result.Item2 != null) // Filtriramo null vrednosti
                .Subscribe(result =>
                {
                    var (context, articles, elapsedMilliseconds) = result;
                    Console.WriteLine($"Sending response on thread {Thread.CurrentThread.ManagedThreadId}, processing time: {elapsedMilliseconds} ms");
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
            })
            .SubscribeOn(NewThreadScheduler.Default) // Obrada inicijalne pretplate na novoj niti
            .ObserveOn(TaskPoolScheduler.Default) // Obrada svakog zahteva na zasebnoj niti iz pool-a
            .Subscribe();
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
