using ConsoleApp1.Models;
using ConsoleApp1.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.Observers
{
    public class ArticleStream : IObservable<Article>
    {
        private readonly Subject<Article> articleSubject = new Subject<Article>();
        private readonly NewsService newsService = new NewsService();

        public async Task GetArticles(string keyword)
        {
            try
            {
                var articles = await newsService.FetchArticlesAsync(keyword,0);
                foreach (var article in articles)
                {
                    articleSubject.OnNext(article);
                }
                articleSubject.OnCompleted();
            }

            catch (Exception ex)
            {
                articleSubject.OnError(ex);
            }

        }

        public IDisposable Subscribe(IObserver<Article> observer)
        {
            return articleSubject.Subscribe(observer);
        }
    }
}
