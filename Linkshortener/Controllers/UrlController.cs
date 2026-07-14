using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed; 
using Linkshortener.Data;
using Linkshortener.Models;
using Linkshortener.Services;
using System;
using System.Threading.Tasks;
using Linkshortener.DTOs;

namespace Linkshortener.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UrlController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly UrlShortenerService _shortenerService;

        public UrlController(AppDbContext context, IDistributedCache cache, UrlShortenerService shortenerService)
        {
            _context = context;
            _cache = cache;
            _shortenerService = shortenerService;
        }

        [HttpPost("shorten")]
        public async Task<IActionResult> ShortenUrl([FromBody] UrlRequestDto request)
        {
            if (string.IsNullOrEmpty(request.OriginalUrl)) return BadRequest("آدرس نمی‌تواند خالی باشد.");
            var shortCode = await _shortenerService.GenerateUniqueCodeAsync();

            var urlRecord = new UrlRecord
            {
                OriginalUrl = request.OriginalUrl, 
                ShortCode = shortCode,
                CreatedAt = DateTime.UtcNow,
                ClickCount = 0
            };

            _context.UrlRecords.Add(urlRecord);
            await _context.SaveChangesAsync();

            var shortUrl = $"{Request.Scheme}://{Request.Host}/{shortCode}";
            return Ok(new { ShortUrl = shortUrl });
        }


        [HttpGet("/{shortCode}")]
        public async Task<IActionResult> RedirectToOriginal(string shortCode)
        {
            string originalUrl;
            var cachedUrl = await _cache.GetStringAsync(shortCode);

            var urlRecord = await _context.UrlRecords.FirstOrDefaultAsync(u => u.ShortCode == shortCode);
            if (urlRecord == null) return NotFound("لینک پیدا نشد!");

            if (!string.IsNullOrEmpty(cachedUrl))
            {
                originalUrl = cachedUrl;
            }
            else
            {
                originalUrl = urlRecord.OriginalUrl;
                var cacheOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) };
                await _cache.SetStringAsync(shortCode, originalUrl, cacheOptions);
            }

            urlRecord.ClickCount++;
            await _context.SaveChangesAsync();

            return Redirect(originalUrl);
        }

        [HttpGet("links")]
        public async Task<IActionResult> GetAllLinks()
        {
            var links = await _context.Urls
                .Select(u => new
                {
                    shortCode = u.ShortCode,
                    originalUrl = u.OriginalUrl,
                    clicks = u.ClickCount, // مپ کردن نام فیلد برای فرانت
                    createdAt = u.CreatedAt.ToString("yyyy/MM/dd") // تبدیل تاریخ به فرمت مناسب
                })
                .ToListAsync();

            return Ok(links);
        }


    }
}
