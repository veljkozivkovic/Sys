
﻿using ConsoleApp1.Observers;
using ConsoleApp1.Services;
using System;
class Program
{
    public static async Task Main()
    {

        var articleStream = new ArticleStream();

        var observer1 = new ArticleObserver("Observer 1");
        var observer2 = new ArticleObserver("Observer 2");

        var subscription1 = articleStream.Subscribe(observer1);
        var subscription2 = articleStream.Subscribe(observer2);

        await articleStream.GetArticles("bitcoin");

        Console.ReadLine();

        subscription1.Dispose();
        subscription2.Dispose();








=======
﻿using ConsoleApp1.Services;
using System;
class Program
{
    static void Main(string[] args)
    {
        NewsService newsService = new NewsService();

        
>>>>>>> 7e6d244d83fa109b18cd8f192bc660704687246a
    }
}