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

            var urlToSave = request.OriginalUrl;
            if (!urlToSave.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !urlToSave.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                urlToSave = "https://" + urlToSave;
            }

            var shortCode = await _shortenerService.GenerateUniqueCodeAsync();

            var urlRecord = new UrlRecord
            {
                OriginalUrl = urlToSave, 
                ShortCode = shortCode,
                CreatedAt = DateTime.UtcNow,
                ClickCount = 0
            };

            _context.UrlRecords.Add(urlRecord);
            await _context.SaveChangesAsync();

            var shortUrl = $"{Request.Scheme}://{Request.Host}/{shortCode}";
            return Ok(new { ShortUrl = shortUrl });
        }





        [HttpGet("/{shortCode:regex(^[[a-zA-Z0-9]]+$)}")]
        public async Task<IActionResult> RedirectToOriginal(string shortCode)
        {
            string cachedUrl = null;

            try
            {
                cachedUrl = await _cache.GetStringAsync(shortCode);
            }
            catch (Exception)
            {
            }

            if (!string.IsNullOrEmpty(cachedUrl))
            {
                var record = await _context.UrlRecords.FirstOrDefaultAsync(u => u.ShortCode == shortCode);
                if (record != null)
                {
                    record.ClickCount++;
                    await _context.SaveChangesAsync();
                }
                return Redirect(cachedUrl);
            }

            var urlRecord = await _context.UrlRecords.FirstOrDefaultAsync(u => u.ShortCode == shortCode);
            if (urlRecord == null) return NotFound("لینک پیدا نشد!");

            try
            {
                var cacheOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) };
                await _cache.SetStringAsync(shortCode, urlRecord.OriginalUrl, cacheOptions);
            }
            catch (Exception)
            {
            }

            urlRecord.ClickCount++;
            await _context.SaveChangesAsync();

            return Redirect(urlRecord.OriginalUrl);
        }


        [HttpGet("links")]
        public async Task<IActionResult> GetAllLinks()
        {
            var links = await _context.UrlRecords 
                .Select(u => new
                {
                    shortCode = u.ShortCode,
                    originalUrl = u.OriginalUrl,
                    clicks = u.ClickCount, 
                    createdAt = u.CreatedAt.ToString("yyyy/MM/dd") 
                })
                .ToListAsync();

            return Ok(links);
        }



    }
}
