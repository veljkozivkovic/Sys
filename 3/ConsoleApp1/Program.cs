
﻿using ConsoleApp1.Observers;
using ConsoleApp1.Server;
using ConsoleApp1.Services;
using SentimentAnalyzer;
using System;
class Program
{
    public static async Task Main()
    {

        var newsService = new NewsService();

        var server = new Server(newsService);

        server.Init();

        Console.WriteLine("Press any key to exit");
        Console.ReadKey();

        server.Stop();

        




    }
}