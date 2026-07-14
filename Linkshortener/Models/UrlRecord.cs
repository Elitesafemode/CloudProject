namespace Linkshortener.Models
{
    public class UrlRecord
    {
        public int Id { get; set; }
        public string OriginalUrl { get; set; } //Primary Ling Logn 
        public string ShortCode { get; set; } // Short Code 
        public int ClickCount { get; set; } = 0; // List Statistic
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}
