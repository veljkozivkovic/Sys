using ConsoleApp1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.Observers
{
    public class ArticleObserver : IObserver<Article>
    {
        private readonly string name;
        public ArticleObserver(string name) 
        {
            this.name = name;
        }
        public void OnCompleted()
        {
            Console.WriteLine($"{name} completed");
        }

        public void OnError(Exception error)
        {
            Console.WriteLine($"ArticleObserver error: {error.Message}");
        }

        public void OnNext(Article article)
        {
            Console.WriteLine($"{name}: Naslov: {article.Title} , Content: {article.Content}, Source: {article.Source.Name}");
            Console.WriteLine("-------------------------------------------------");
        }
    }
}
