using BtOperasyonTakip.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace BtOperasyonTakip.Controllers
{
    [Authorize(Roles = "Uyum")]
    public class UyumController : Controller
    {
        private readonly AppDbContext _context;

        public UyumController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index(string? durumFilter, string? searchWebsite)
        {
            var userId = int.TryParse(User.FindFirst("UserId")?.Value, out var uid) ? uid : 0;

            // Filtre: Tümü | Bekleyen | Onaylanan | Reddedilen
            var filter = string.IsNullOrWhiteSpace(durumFilter) ? "Bekleyen" : durumFilter.Trim();

            var query = _context.Tickets.AsQueryable();

            // Website arama
            if (!string.IsNullOrWhiteSpace(searchWebsite))
            {
                query = query.Where(t => t.MusteriWebSitesi.Contains(searchWebsite));
            }

            query = filter switch
            {
                "Tümü" => query.Where(t => t.Durum == "Uyum Onayı Bekleniyor"
                                        || t.Durum == "Operasyon Onayı Bekleniyor"
                                        || t.Durum == "Onaylandi"
                                        || t.Durum == "Reddedildi"),

                "Bekleyen" => query.Where(t => t.Durum == "Uyum Onayı Bekleniyor"),

                "Onaylanan" => query.Where(t => t.UyumOnaylayanUserId == userId
                                             && (t.Durum == "Operasyon Onayı Bekleniyor" || t.Durum == "Onaylandi")),

                "Reddedilen" => query.Where(t => t.UyumOnaylayanUserId == userId
                                              && t.Durum == "Reddedildi"),

                _ => query.Where(t => t.Durum == "Uyum Onayı Bekleniyor")
            };

            var tickets = query
                .OrderByDescending(t => t.UyumOnayTarihi ?? t.OlusturmaTarihi)
                .ToList();

            ViewBag.DurumFilter = filter;
            ViewBag.SearchWebsite = searchWebsite;

            return View(tickets);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Decide(int id, string karar, string? aciklama)
        {
            var ticket = _context.Tickets.FirstOrDefault(t => t.Id == id);
            if (ticket == null)
                return NotFound();

            if (ticket.Durum != "Uyum Onayı Bekleniyor")
                return BadRequest($"Ticket bu aşamada uyum kararına uygun değil. Durum: {ticket.Durum}");

            var userId = int.TryParse(User.FindFirst("UserId")?.Value, out var uid) ? uid : 0;

            ticket.UyumOnaylayanUserId = userId;
            ticket.UyumOnaylayanKullaniciAdi = User.Identity?.Name ?? "Bilinmiyor";
            ticket.UyumOnayTarihi = DateTime.UtcNow;
            ticket.UyumKararAciklamasi = string.IsNullOrWhiteSpace(aciklama) ? null : aciklama;

            if (string.Equals(karar, "Onay", StringComparison.OrdinalIgnoreCase))
            {
                ticket.Durum = "Operasyon Onayı Bekleniyor";
                _context.SaveChanges();
                TempData["Success"] = "✅ Ticket uyum tarafından onaylandı ve operasyon onayına gönderildi.";
                return RedirectToAction(nameof(Index), new { durumFilter = "Bekleyen" });
            }

            if (string.Equals(karar, "Red", StringComparison.OrdinalIgnoreCase))
            {
                ticket.Durum = "Reddedildi";
                _context.SaveChanges();
                TempData["Error"] = "❌ Ticket uyum tarafından reddedildi.";
                return RedirectToAction(nameof(Index), new { durumFilter = "Bekleyen" });
            }

            return BadRequest("Geçersiz karar. (Onay/Red)");
        }

        // Geriye dönük uyumluluk: mevcut Approve/Reject linkleri bozulmasın diye bırakılabilir.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Approve(int id, string? aciklama) => Decide(id, "Onay", aciklama);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reject(int id, string? aciklama) => Decide(id, "Red", aciklama);
    }
}