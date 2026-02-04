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
                // Atama yapılan yapıda operasyon sadece kendine atanmış ticket'ları görsün
                var userId = int.TryParse(User.FindFirst("UserId")?.Value, out var id) ? id : 0;

                query = query.Where(t =>
                    t.AtananOperasyonUserId == userId &&
                    (t.Durum == "Operasyon 1 Onay Bekleniyor" ||
                     t.Durum == "Operasyon 2 Onay Bekleniyor" ||
                     t.Durum == "Saha Canli Bekleniyor" ||
                     t.Durum == "Musteri Kaydedildi" ||
                     t.Durum == "Reddedildi"));
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

                // Akış başlangıcı
                ticket.Durum = "Operasyon 1 Onay Bekleniyor";

                // Operasyon ekibine dağıtım (ilk 5 operasyon kullanıcısı, iş yoğunluğuna göre)
                var operasyonAdaylari = _context.Users
                    .Where(u => u.Role == "Operasyon")
                    .OrderBy(u => u.Id)
                    .Take(5)
                    .ToList();

                if (operasyonAdaylari.Count > 0)
                {
                    var adayIds = operasyonAdaylari.Select(x => x.Id).ToList();

                    // Aktif iş sayısı: kapanmış saydıklarımız hariç
                    var aktifIsSayilari = _context.Tickets
                        .Where(t => t.AtananOperasyonUserId.HasValue
                                    && adayIds.Contains(t.AtananOperasyonUserId.Value)
                                    && t.Durum != "Reddedildi"
                                    && t.Durum != "Musteri Kaydedildi")
                        .GroupBy(t => t.AtananOperasyonUserId!.Value)
                        .Select(g => new { UserId = g.Key, Count = g.Count() })
                        .ToDictionary(x => x.UserId, x => x.Count);

                    var secilen = operasyonAdaylari
                        .Select(u => new
                        {
                            User = u,
                            Count = aktifIsSayilari.TryGetValue(u.Id, out var c) ? c : 0
                        })
                        .OrderBy(x => x.Count)      // en az iş
                        .ThenBy(x => x.User.Id)     // eşitse sıra
                        .First()
                        .User;

                    ticket.AtananOperasyonUserId = secilen.Id;
                    ticket.AtananOperasyonKullaniciAdi = secilen.FullName ?? secilen.UserName;
                    ticket.AtanmaTarihi = DateTime.UtcNow;
                }

                _context.Tickets.Add(ticket);
                _context.SaveChanges();

                TempData["Success"] = "✅ Ticket oluşturuldu! Operasyon 1 onayı bekleniyor.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Hata: {ex.Message}");
                return View(ticket);
            }
        }

        // OPERASYON 1: Entegrasyon (Evet/Hayır) -> Uyum'a gönder / Reddet
        [HttpPost]
        [Authorize(Roles = "Operasyon")]
        public IActionResult Operasyon1Decide([FromBody] Operasyon1Request request)
        {
            if (request?.Id <= 0)
                return Json(new { success = false, message = "Geçersiz Ticket ID!" });

            var ticket = _context.Tickets.FirstOrDefault(t => t.Id == request.Id);
            if (ticket == null)
                return Json(new { success = false, message = "Ticket bulunamadı!" });

            if (ticket.Durum != "Operasyon 1 Onay Bekleniyor")
                return Json(new { success = false, message = $"Ticket bu aşamada operasyon 1 kararına uygun değil. Durum: {ticket.Durum}" });

            var userId = int.TryParse(User.FindFirst("UserId")?.Value, out var uid) ? uid : 0;

            if (ticket.AtananOperasyonUserId.HasValue && ticket.AtananOperasyonUserId.Value != userId)
                return Json(new { success = false, message = "Bu ticket size atanmadı." });

            ticket.EntegreOlabilirMi = request.EntegreOlabilirMi;
            ticket.EntegrasyonNotu = string.IsNullOrWhiteSpace(request.Not) ? null : request.Not.Trim();
            ticket.Operasyon1OnayTarihi = DateTime.UtcNow;

            // Karar veren kişi (EVET/HAYIR her iki durumda da)
            ticket.OnaylayanUserId = userId;
            ticket.OnaylayanKullaniciAdi = User.Identity?.Name ?? "Bilinmiyor";
            ticket.OnaylamaTarihi = DateTime.UtcNow;

            if (request.EntegreOlabilirMi == false)
            {
                ticket.Durum = "Reddedildi";
                ticket.KararAciklamasi = string.IsNullOrWhiteSpace(ticket.EntegrasyonNotu)
                    ? "Entegre olamıyoruz."
                    : ticket.EntegrasyonNotu;

                _context.SaveChanges();
                return Json(new { success = true, message = "❌ Entegre olamıyoruz. Ticket reddedildi." });
            }

            ticket.Durum = "Uyum Onayı Bekleniyor";
            _context.SaveChanges();

            return Json(new { success = true, message = "✅ Entegre olabiliyoruz. Ticket Uyum onayına gönderildi." });
        }

        // OPERASYON 2: Mail gönderdim (Evet/Hayır) -> her koşulda Saha canlı bekleniyor
        [HttpPost]
        [Authorize(Roles = "Operasyon")]
        public IActionResult Operasyon2Decide([FromBody] Operasyon2Request request)
        {
            if (request?.Id <= 0)
                return Json(new { success = false, message = "Geçersiz Ticket ID!" });

            var ticket = _context.Tickets.FirstOrDefault(t => t.Id == request.Id);
            if (ticket == null)
                return Json(new { success = false, message = "Ticket bulunamadı!" });

            if (ticket.Durum != "Operasyon 2 Onay Bekleniyor")
                return Json(new { success = false, message = $"Ticket bu aşamada operasyon 2 kararına uygun değil. Durum: {ticket.Durum}" });

            var userId = int.TryParse(User.FindFirst("UserId")?.Value, out var uid) ? uid : 0;

            if (ticket.AtananOperasyonUserId.HasValue && ticket.AtananOperasyonUserId.Value != userId)
                return Json(new { success = false, message = "Bu ticket size atanmadı." });

            ticket.MailGonderildiMi = request.MailGonderildiMi;
            ticket.MailNotu = string.IsNullOrWhiteSpace(request.Not) ? null : request.Not.Trim();
            ticket.Operasyon2OnayTarihi = DateTime.UtcNow;

            // Karar veren kişi
            ticket.OnaylayanUserId = userId;
            ticket.OnaylayanKullaniciAdi = User.Identity?.Name ?? "Bilinmiyor";
            ticket.OnaylamaTarihi = DateTime.UtcNow;

            ticket.Durum = "Saha Canli Bekleniyor";

            _context.SaveChanges();
            return Json(new { success = true, message = "✅ Operasyon 2 kaydedildi. Saha canlı açılışını bekliyor." });
        }

        // SAHA: Canlı ortamı açtım -> Musteriler tablosuna kayıt
        [HttpPost]
        [Authorize(Roles = "Saha")]
        public IActionResult SahaCanliAcildi([FromBody] SahaCanliRequest request)
        {
            if (request?.Id <= 0)
                return Json(new { success = false, message = "Geçersiz Ticket ID!" });

            var ticket = _context.Tickets.FirstOrDefault(t => t.Id == request.Id);
            if (ticket == null)
                return Json(new { success = false, message = "Ticket bulunamadı!" });

            var userId = int.TryParse(User.FindFirst("UserId")?.Value, out var uid) ? uid : 0;
            if (ticket.OlusturanUserId != userId)
                return Json(new { success = false, message = "Sadece ticketı açan saha kullanıcısı bu işlemi yapabilir." });

            if (ticket.Durum != "Saha Canli Bekleniyor")
                return Json(new { success = false, message = $"Ticket bu aşamada canlı açılışa uygun değil. Durum: {ticket.Durum}" });

            ticket.CanliAcildiTarihi = DateTime.UtcNow;
            ticket.CanliNotu = string.IsNullOrWhiteSpace(request.Not) ? null : request.Not.Trim();

            // Müşteri kaydı: URL bazlı upsert
            var mevcutMusteri = _context.Musteriler.FirstOrDefault(m => m.SiteUrl == ticket.MusteriWebSitesi);

            Musteri musteri;
            if (mevcutMusteri != null)
            {
                mevcutMusteri.Firma = string.IsNullOrWhiteSpace(ticket.FirmaAdi) ? mevcutMusteri.Firma : ticket.FirmaAdi;
                mevcutMusteri.FirmaYetkilisi = $"{ticket.YazilimciAdi} {ticket.YazilimciSoyadi}";
                mevcutMusteri.Telefon = ticket.IrtibatNumarasi;
                mevcutMusteri.SiteUrl = ticket.MusteriWebSitesi;
                mevcutMusteri.Teknoloji = ticket.TeknolojiBilgisi;
                mevcutMusteri.Durum = "Aktif";
                mevcutMusteri.TalepSahibi = ticket.OlusturanKullaniciAdi;
                mevcutMusteri.Aciklama = ticket.Aciklama;
                mevcutMusteri.KayitTarihi = DateTime.Now;

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
                    Teknoloji = ticket.TeknolojiBilgisi,
                    Durum = "Aktif",
                    TalepSahibi = ticket.OlusturanKullaniciAdi,
                    Aciklama = ticket.Aciklama,
                    KayitTarihi = DateTime.Now
                };

                _context.Musteriler.Add(musteri);
                _context.SaveChanges();
            }

            ticket.MusteriID = musteri.MusteriID;
            ticket.Durum = "Musteri Kaydedildi";

            _context.SaveChanges();

            return Json(new { success = true, message = "✅ Canlı açıldı. Müşteri kaydı oluşturuldu/güncellendi." });
        }

        // SAHA: Son aşamada reddet (Saha Canli Bekleniyor -> Reddedildi)
        [HttpPost]
        [Authorize(Roles = "Saha")]
        public IActionResult SahaReddet([FromBody] SahaRedRequest request)
        {
            if (request?.Id <= 0)
                return Json(new { success = false, message = "Geçersiz Ticket ID!" });

            var ticket = _context.Tickets.FirstOrDefault(t => t.Id == request.Id);
            if (ticket == null)
                return Json(new { success = false, message = "Ticket bulunamadı!" });

            var userId = int.TryParse(User.FindFirst("UserId")?.Value, out var uid) ? uid : 0;
            if (ticket.OlusturanUserId != userId)
                return Json(new { success = false, message = "Sadece ticketı açan saha kullanıcısı bu işlemi yapabilir." });

            if (ticket.Durum != "Saha Canli Bekleniyor")
                return Json(new { success = false, message = $"Ticket bu aşamada saha reddine uygun değil. Durum: {ticket.Durum}" });

            var aciklama = (request.Not ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(aciklama))
                return Json(new { success = false, message = "Reddetme açıklaması zorunlu!" });

            ticket.Durum = "Reddedildi";
            ticket.OnaylayanUserId = userId;
            ticket.OnaylayanKullaniciAdi = User.Identity?.Name ?? "Bilinmiyor";
            ticket.OnaylamaTarihi = DateTime.UtcNow;
            ticket.KararAciklamasi = aciklama;
            ticket.CanliNotu = aciklama;

            _context.SaveChanges();
            return Json(new { success = true, message = "❌ Ticket saha tarafından reddedildi." });
        }

        // --- Eski endpointler (geriye dönük) ---
        [HttpPost]
        [Authorize(Roles = "Operasyon")]
        public IActionResult ApproveWithTechnology([FromBody] ApproveWithTechRequest request)
        {
            if (request?.Id <= 0)
                return Json(new { success = false, message = "Geçersiz Ticket ID!" });

            var ticket = _context.Tickets.FirstOrDefault(t => t.Id == request.Id);
            if (ticket == null)
                return Json(new { success = false, message = "Ticket bulunamadı!" });

            if (ticket.Durum != "Operasyon 2 Onay Bekleniyor")
                return Json(new { success = false, message = $"Ticket bu aşamada onaya uygun değil. Durum: {ticket.Durum}" });

            var userId = int.TryParse(User.FindFirst("UserId")?.Value, out var uid) ? uid : 0;
            if (ticket.AtananOperasyonUserId.HasValue && ticket.AtananOperasyonUserId.Value != userId)
                return Json(new { success = false, message = "Bu ticket size atanmadı." });

            if (!string.IsNullOrWhiteSpace(request.TeknolojiBilgisi))
                ticket.TeknolojiBilgisi = request.TeknolojiBilgisi.Trim();

            ticket.MailGonderildiMi = true;
            ticket.MailNotu = string.IsNullOrWhiteSpace(request.KararAciklamasi) ? null : request.KararAciklamasi.Trim();
            ticket.Operasyon2OnayTarihi = DateTime.UtcNow;
            ticket.Durum = "Saha Canli Bekleniyor";

            ticket.OnaylayanUserId = userId;
            ticket.OnaylayanKullaniciAdi = User.Identity?.Name ?? "Bilinmiyor";
            ticket.OnaylamaTarihi = DateTime.UtcNow;

            _context.SaveChanges();
            return Json(new { success = true, message = "✅ Güncellendi. Saha canlı açılışını bekliyor." });
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

            var d = ticket.Durum?.Trim() ?? "";
            if (d != "Operasyon 1 Onay Bekleniyor" && d != "Operasyon 2 Onay Bekleniyor")
                return Json(new { success = false, message = $"Ticket bu aşamada operasyon reddine uygun değil. Durum: {ticket.Durum}" });

            var userId = int.TryParse(User.FindFirst("UserId")?.Value, out var uid) ? uid : 0;
            if (ticket.AtananOperasyonUserId.HasValue && ticket.AtananOperasyonUserId.Value != userId)
                return Json(new { success = false, message = "Bu ticket size atanmadı." });

            ticket.Durum = "Reddedildi";
            ticket.OnaylayanUserId = userId;
            ticket.OnaylayanKullaniciAdi = User.Identity?.Name ?? "Bilinmiyor";
            ticket.OnaylamaTarihi = DateTime.UtcNow;
            ticket.KararAciklamasi = string.IsNullOrWhiteSpace(request.KararAciklamasi) ? "Açıklama yapılmadı" : request.KararAciklamasi.Trim();

            _context.SaveChanges();
            return Json(new { success = true, message = "❌ Ticket reddedildi!" });
        }

        public sealed class Operasyon1Request
        {
            public int Id { get; set; }
            public bool EntegreOlabilirMi { get; set; }
            public string? Not { get; set; }
        }

        public sealed class Operasyon2Request
        {
            public int Id { get; set; }
            public bool MailGonderildiMi { get; set; }
            public string? Not { get; set; }
        }

        public sealed class SahaCanliRequest
        {
            public int Id { get; set; }
            public string? Not { get; set; }
        }

        public sealed class SahaRedRequest
        {
            public int Id { get; set; }
            public string? Not { get; set; }
        }

        public sealed class ApproveWithTechRequest
        {
            public int Id { get; set; }
            public string? TeknolojiBilgisi { get; set; }
            public string? KararAciklamasi { get; set; }
        }

        public sealed class ApproveRejectRequest
        {
            public int Id { get; set; }
            public string? KararAciklamasi { get; set; }
        }
    }
}