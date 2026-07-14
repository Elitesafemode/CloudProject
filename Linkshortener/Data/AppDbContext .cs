using Microsoft.EntityFrameworkCore;
using Linkshortener.Models;
namespace Linkshortener.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public AppDbContext() { }

        public DbSet<UrlRecord> UrlRecords { get; set; }
    }
}

