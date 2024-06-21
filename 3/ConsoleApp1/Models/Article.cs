namespace ConsoleApp1.Models
{
    public class Article
    {
        public Source Source { get; set; }
        public string Author { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public string UrlToImage { get; set; }
        public DateTime PublishedAt { get; set; }
        public string Content { get; set; }

        public Article(Source s, string author, string title, string des, string url, string urlToImg, DateTime published, string content)
        {
            Source = s;
            Author = author;
            Title = title;
            Description = des;
            Url = url;
            UrlToImage = urlToImg;
            PublishedAt = published;
            Content = content;
        }
    }
}
