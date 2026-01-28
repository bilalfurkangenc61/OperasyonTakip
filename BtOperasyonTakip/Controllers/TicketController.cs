using BtOperasyonTakip.Data;
using BtOperasyonTakip.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace BtOperasyonTakip.Controllers
{
    [Authorize(Roles = "Saha,Operasyon,Uyum")]
    public class TicketController : Controller
    {
        private readonly AppDbContext _context;

        public TicketController(AppDbContext context)
        {
            _context = context;
        }

        // Index:
        // - Saha: kendi ticketları
        // - Uyum: "Uyum Onayı Bekleniyor"
        // - Operasyon: "Operasyon Onayı Bekleniyor" + (istersen Onaylandi da gösterilebilir)
        public IActionResult Index(string? searchFirma)
        {
            IQueryable<Ticket> query = _context.Tickets;

            if (User.IsInRole("Saha"))
            {
                var userId = int.TryParse(User.FindFirst("UserId")?.Value, out var id) ? id : 0;
                query = query.Where(t => t.OlusturanUserId == userId);
            }
            else if (User.IsInRole("Uyum"))
            {
                query = query.Where(t => t.Durum == "Uyum Onayı Bekleniyor");
            }
            else if (User.IsInRole("Operasyon"))
            {
                query = query.Where(t => t.Durum == "Operasyon Onayı Bekleniyor" || t.Durum == "Onaylandi");
            }

            if (!string.IsNullOrWhiteSpace(searchFirma))
            {
                query = query.Where(t =>
                    t.FirmaAdi.Contains(searchFirma) ||
                    t.MusteriWebSitesi.Contains(searchFirma) ||
                    t.YazilimciAdi.Contains(searchFirma) ||
                    t.YazilimciSoyadi.Contains(searchFirma));
            }

            var tickets = query.OrderByDescending(t => t.OlusturmaTarihi).ToList();

            ViewBag.SearchFirma = searchFirma;

            return View(tickets);
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (!User.IsInRole("Saha"))
                return Unauthorized();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Ticket ticket)
        {
            if (!User.IsInRole("Saha"))
                return Unauthorized();

            if (!ModelState.IsValid)
                return View(ticket);

            try
            {
                var userId = int.TryParse(User.FindFirst("UserId")?.Value, out var id) ? id : 0;

                ticket.OlusturanUserId = userId;
                ticket.OlusturanKullaniciAdi = User.Identity?.Name ?? "Bilinmiyor";
                ticket.OlusturmaTarihi = DateTime.UtcNow;
                ticket.Durum = "Uyum Onayı Bekleniyor";

                _context.Tickets.Add(ticket);
                _context.SaveChanges();

                TempData["Success"] = "✅ Ticket başarıyla oluşturuldu! Uyum onayı bekleniyor.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Hata: {ex.Message}");
                return View(ticket);
            }
        }

        [HttpGet]
        public IActionResult Detail(int id)
        {
            var ticket = _context.Tickets.FirstOrDefault(t => t.Id == id);
            if (ticket == null)
                return NotFound();

            if (User.IsInRole("Saha"))
            {
                var userId = int.TryParse(User.FindFirst("UserId")?.Value, out var uid) ? uid : 0;
                if (ticket.OlusturanUserId != userId)
                    return Unauthorized();
            }

            return View(ticket);
        }

        // OPERASYON nihai onay (Uyum onayından sonra)
        [HttpPost]
        [Authorize(Roles = "Operasyon")]
        public IActionResult ApproveWithTechnology([FromBody] ApproveWithTechRequest request)
        {
            if (request?.Id <= 0)
                return Json(new { success = false, message = "Geçersiz Ticket ID!" });

            var ticket = _context.Tickets.FirstOrDefault(t => t.Id == request.Id);
            if (ticket == null)
                return Json(new { success = false, message = "Ticket bulunamadı!" });

            if (ticket.Durum != "Operasyon Onayı Bekleniyor")
                return Json(new { success = false, message = $"Ticket bu aşamada operasyon onayına uygun değil. Durum: {ticket.Durum}" });

            try
            {
                if (string.IsNullOrWhiteSpace(request.TeknolojiBilgisi))
                    return Json(new { success = false, message = "Lütfen bir teknoloji seçiniz!" });

                var userId = int.TryParse(User.FindFirst("UserId")?.Value, out var uid) ? uid : 0;

                ticket.TeknolojiBilgisi = request.TeknolojiBilgisi;

                var mevcutMusteri = _context.Musteriler
                    .FirstOrDefault(m => m.SiteUrl == ticket.MusteriWebSitesi);

                Musteri musteri;

                if (mevcutMusteri != null)
                {
                    mevcutMusteri.Firma = string.IsNullOrWhiteSpace(ticket.FirmaAdi) ? mevcutMusteri.Firma : ticket.FirmaAdi;
                    mevcutMusteri.FirmaYetkilisi = $"{ticket.YazilimciAdi} {ticket.YazilimciSoyadi}";
                    mevcutMusteri.Telefon = ticket.IrtibatNumarasi;
                    mevcutMusteri.Teknoloji = request.TeknolojiBilgisi;
                    mevcutMusteri.Durum = "Aktif";
                    mevcutMusteri.TalepSahibi = ticket.OlusturanKullaniciAdi;
                    mevcutMusteri.Aciklama = ticket.Aciklama;

                    musteri = mevcutMusteri;
                    _context.SaveChanges();
                }
                else
                {
                    musteri = new Musteri
                    {
                        Firma = ticket.FirmaAdi,
                        FirmaYetkilisi = $"{ticket.YazilimciAdi} {ticket.YazilimciSoyadi}",
                        Telefon = ticket.IrtibatNumarasi,
                        SiteUrl = ticket.MusteriWebSitesi,
                        Teknoloji = request.TeknolojiBilgisi,
                        Durum = "Aktif",
                        TalepSahibi = ticket.OlusturanKullaniciAdi,
                        Aciklama = ticket.Aciklama,
                        KayitTarihi = DateTime.Now
                    };

                    _context.Musteriler.Add(musteri);
                    _context.SaveChanges();
                }

                ticket.Durum = "Onaylandi";
                ticket.OnaylayanUserId = userId;
                ticket.OnaylayanKullaniciAdi = User.Identity?.Name ?? "Bilinmiyor";
                ticket.OnaylamaTarihi = DateTime.UtcNow;
                ticket.KararAciklamasi = request.KararAciklamasi;
                ticket.MusteriID = musteri.MusteriID;

                _context.SaveChanges();

                return Json(new { success = true, message = "✅ Ticket operasyon tarafından onaylandı ve müşteri kaydı oluşturuldu!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Hata: {ex.Message}" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Operasyon")]
        public IActionResult Reject([FromBody] ApproveRejectRequest request)
        {
            if (request?.Id <= 0)
                return Json(new { success = false, message = "Geçersiz Ticket ID!" });

            var ticket = _context.Tickets.FirstOrDefault(t => t.Id == request.Id);
            if (ticket == null)
                return Json(new { success = false, message = "Ticket bulunamadı!" });

            if (ticket.Durum != "Operasyon Onayı Bekleniyor")
                return Json(new { success = false, message = $"Ticket bu aşamada operasyon reddine uygun değil. Durum: {ticket.Durum}" });

            try
            {
                var userId = int.TryParse(User.FindFirst("UserId")?.Value, out var uid) ? uid : 0;

                ticket.Durum = "Reddedildi";
                ticket.OnaylayanUserId = userId;
                ticket.OnaylayanKullaniciAdi = User.Identity?.Name ?? "Bilinmiyor";
                ticket.OnaylamaTarihi = DateTime.UtcNow;
                ticket.KararAciklamasi = request.KararAciklamasi ?? "Açıklama yapılmadı";

                _context.SaveChanges();

                return Json(new { success = true, message = "❌ Ticket operasyon tarafından reddedildi!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Hata: {ex.Message}" });
            }
        }
    }
}