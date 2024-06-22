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
                    return Observable.FromAsync(() => _listener.GetContextAsync())
                        .Repeat()
                        .TakeWhile(_ => _listener.IsListening)
                        .Subscribe(obs.OnNext, obs.OnError, obs.OnCompleted);
                })
                .SubscribeOn(NewThreadScheduler.Default) // Obrada inicijalne pretplate na novoj niti
                .ObserveOn(TaskPoolScheduler.Default) // Obrada svakog zahteva na zasebnoj niti iz pool-a
                .SelectMany(context =>
                    Observable.FromAsync(async () =>
                    {
                        try
                        {
                            // Pretpostavimo da keyword dolazi iz URL-a
                            var keyword = HttpUtility.ParseQueryString(context.Request.Url.Query).Get("keyword");
                            if (string.IsNullOrEmpty(keyword))
                            {
                                context.Response.StatusCode = 400; // Bad Request
                                context.Response.Close();
                                return (null, null); // Vraćamo null vrednosti kako bismo ih kasnije filtrirali
                            }

                            var articles = await _NewsService.FetchArticlesAsync(keyword);
                            return (context, articles);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing request: {ex.Message}");
                            context.Response.StatusCode = 500; // Internal Server Error
                            context.Response.Close();
                            return (null, null); // U slučaju greške, vraćamo null vrednosti
                        }
                    }))
                .Where(result => result.Item1 != null && result.Item2 != null) // Filtriramo null vrednosti
                .Subscribe(result =>
                {
                    var (context, articles) = result;
                    var response = context.Response;
                    var buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(articles));
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                    response.Close();
                }, error =>
                {
                    Console.WriteLine($"Error: {error.Message}");
                });
        }

        public void Stop()
        {
            if (_subscription != null)
            {
                _subscription.Dispose(); // Otkazivanje pretplate
                _subscription = null; // Resetovanje _subscription na null
                _listener.Stop(); // Zaustavljanje HttpListener-a
                Console.WriteLine("Server stopped.");
            }
        }

    }
}
