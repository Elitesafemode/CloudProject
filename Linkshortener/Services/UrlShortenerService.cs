using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Linkshortener.Data;

namespace Linkshortener.Services
{
    public class UrlShortenerService
    {
        private readonly AppDbContext _context;
        // Number And Word In Code 
        private const string Chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private readonly Random _random = new Random();

        public UrlShortenerService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateUniqueCodeAsync()
        {
            string code;
            bool exists;
            do
            {
                // Create Random Code 
                code = new string(Enumerable.Repeat(Chars, 6)
                    .Select(s => s[_random.Next(s.Length)]).ToArray());

                // If Not Exist in DataBase 
                exists = await _context.UrlRecords.AnyAsync(u => u.ShortCode == code);
            } while (exists); // If Exist Loop For Create New Code

            return code;
        }
    }
}
