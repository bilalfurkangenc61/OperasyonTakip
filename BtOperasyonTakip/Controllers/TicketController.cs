using BtOperasyonTakip.Data;
using BtOperasyonTakip.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace BtOperasyonTakip.Controllers
{
    [Authorize(Roles = "Saha,KurumsalSaha,Operasyon,Uyum,Admin")]
    public class TicketController : Controller
    {
        private readonly AppDbContext _context;

        private const string TicketDurumOperasyon1 = "Operasyon 1 Onay Bekleniyor";
        private const string TicketDurumUyum = "Uyum Onayı Bekleniyor";
        private const string TicketDurumOperasyon2 = "Operasyon 2 Onay Bekleniyor";
        private const string TicketDurumEntegrasyonBekliyor = "Entegrasyon Bekleniyor";
        private const string TicketDurumSahaCanliBekleniyor = "Saha Canli Bekleniyor";
        private const string TicketDurumMusteriKaydedildi = "Musteri Kaydedildi";
        private const string TicketDurumReddedildi = "Reddedildi";

        public TicketController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(string? searchFirma)
        {
            IQueryable<Ticket> query = _context.Tickets;

            if (User.IsInRole("Saha") || User.IsInRole("KurumsalSaha"))
            {
                var userId = int.TryParse(User.FindFirst("UserId")?.Value, out var id) ? id : 0;
                query = query.Where(t => t.OlusturanUserId == userId);
            }
            else if (User.IsInRole("Uyum"))
            {
                query = query.Where(t => t.Durum == TicketDurumUyum);
            }
            else if (User.IsInRole("Operasyon"))
            {
                var userId = int.TryParse(User.FindFirst("UserId")?.Value, out var id) ? id : 0;

                // Operasyon kendi atanmış ticketlarını görsün (entegrasyon adımı da dahil)
                query = query.Where(t =>
                    t.AtananOperasyonUserId == userId &&
                    (t.Durum == TicketDurumOperasyon1 ||
                     t.Durum == TicketDurumOperasyon2 ||
                     t.Durum == TicketDurumEntegrasyonBekliyor ||
                     t.Durum == TicketDurumSahaCanliBekleniyor ||
                     t.Durum == TicketDurumMusteriKaydedildi ||
                     t.Durum == TicketDurumReddedildi));
            }

            if (!string.IsNullOrWhiteSpace(searchFirma))
            {
                query = query.Where(t =>
                    t.FirmaAdi.Contains(searchFirma) ||
                    t.MusteriWebSitesi.Contains(searchFirma) ||
                    t.YazilimciAdi.Contains(searchFirma) ||
                    t.YazilimciSoyadi.Contains(searchFirma));
            }

            if (User.IsInRole("Admin"))
            {
                var operasyonUsers = _context.Users
                    .Where(u => u.Role == "Operasyon")
                    .OrderBy(u => u.Id)
                    .Select(u => new
                    {
                        u.Id,
                        Name = (u.FullName ?? u.UserName)
                    })
                    .ToList();

                ViewBag.OperasyonUsers = operasyonUsers;
            }

            var tickets = query.OrderByDescending(t => t.OlusturmaTarihi).ToList();
            ViewBag.SearchFirma = searchFirma;

            return View(tickets);
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (!(User.IsInRole("Saha") || User.IsInRole("KurumsalSaha")))
                return Unauthorized();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Ticket ticket)
        {
            if (!(User.IsInRole("Saha") || User.IsInRole("KurumsalSaha")))
                return Unauthorized();

            if (!ModelState.IsValid)
                return View(ticket);

            var firmaAdi = (ticket.FirmaAdi ?? string.Empty).Trim();
            var siteAdi = (ticket.MusteriWebSitesi ?? string.Empty).Trim();

            if (!string.IsNullOrWhiteSpace(firmaAdi) && !string.IsNullOrWhiteSpace(siteAdi))
            {
                var firmaKey = firmaAdi.ToLowerInvariant();
                var siteKey = siteAdi.ToLowerInvariant();

                var aynisiVarMi = _context.Tickets.Any(t =>
                    ((t.FirmaAdi ?? string.Empty).Trim().ToLower()) == firmaKey &&
                    ((t.MusteriWebSitesi ?? string.Empty).Trim().ToLower()) == siteKey &&
                    t.Durum != TicketDurumReddedildi &&
                    t.Durum != TicketDurumMusteriKaydedildi);

                if (aynisiVarMi)
                {
                    ModelState.AddModelError("", "Bu firma ve site için zaten açık bir ticket mevcut. Yeni ticket oluşturulmadı.");
                    return View(ticket);
                }
            }

            try
            {
                var userId = int.TryParse(User.FindFirst("UserId")?.Value, out var id) ? id : 0;

                ticket.OlusturanUserId = userId;
                ticket.OlusturanKullaniciAdi = User.Identity?.Name ?? "Bilinmiyor";
                ticket.OlusturmaTarihi = DateTime.UtcNow;

                ticket.Durum = TicketDurumOperasyon1;

                // KURAL:
                // - Kurumsal satış (KurumsalSaha) -> sadece Furkan'a atansın
                // - Saha -> Furkan dahil tüm Operasyon kullanıcıları arasından rastgele atansın
                if (User.IsInRole("KurumsalSaha"))
                {
                    var furkan = _context.Users.FirstOrDefault(u =>
                        u.Role == "Operasyon" &&
                        (u.UserName ?? string.Empty).Trim().ToLower() == "furkan");

                    if (furkan == null)
                    {
                        ModelState.AddModelError("", "Kurumsal ticket ataması için 'furkan' kullanıcısı bulunamadı (Operasyon rolünde olmalı, UserName = 'furkan').");
                        return View(ticket);
                    }

                    ticket.AtananOperasyonUserId = furkan.Id;
                    ticket.AtananOperasyonKullaniciAdi = furkan.FullName ?? furkan.UserName;
                    ticket.AtanmaTarihi = DateTime.UtcNow;
                }
                else
                {
                    var operasyonAdaylari = _context.Users
                        .Where(u => u.Role == "Operasyon")
                        .OrderBy(u => u.Id)
                        .Take(5)
                        .ToList();

                    if (operasyonAdaylari.Count > 0)
                    {
                        var index = Random.Shared.Next(operasyonAdaylari.Count);
                        var secilen = operasyonAdaylari[index];

                        ticket.AtananOperasyonUserId = secilen.Id;
                        ticket.AtananOperasyonKullaniciAdi = secilen.FullName ?? secilen.UserName;
                        ticket.AtanmaTarihi = DateTime.UtcNow;
                    }
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

        // =========================
        // OPERASYON 1 ONAY / RED
        // =========================
        [HttpPost]
        [Authorize(Roles = "Operasyon")]
        public IActionResult Operasyon1Decide([FromBody] Operasyon1DecideRequest request)
        {
            if (request?.Id <= 0)
                return Json(new { success = false, message = "Geçersiz Ticket ID!" });

            var ticket = _context.Tickets.FirstOrDefault(t => t.Id == request.Id);
            if (ticket == null)
                return Json(new { success = false, message = "Ticket bulunamadı!" });

            if (ticket.Durum != TicketDurumOperasyon1)
                return Json(new { success = false, message = $"Ticket bu aşamada Operasyon 1 kararına uygun değil. Durum: {ticket.Durum}" });

            var userId = int.TryParse(User.FindFirst("UserId")?.Value, out var uid) ? uid : 0;
            if (ticket.AtananOperasyonUserId != userId)
                return Json(new { success = false, message = "Bu ticket size atanmadığı için işlem yapamazsınız." });

            ticket.EntegreOlabilirMi = request.EntegreOlabilirMi;
            ticket.EntegrasyonNotu = string.IsNullOrWhiteSpace(request.Not) ? null : request.Not.Trim();
            ticket.OnaylayanUserId = userId;
            ticket.OnaylayanKullaniciAdi = User.Identity?.Name ?? "Bilinmiyor";
            ticket.Operasyon1OnayTarihi = DateTime.UtcNow;

            ticket.Durum = TicketDurumUyum;

            _context.SaveChanges();
            return Json(new { success = true, message = "✅ Operasyon 1 kararı kaydedildi. Uyum onayı bekleniyor." });
        }

        // =========================
        // OPERASYON 2 ONAY / RED
        // =========================
        [HttpPost]
        [Authorize(Roles = "Operasyon")]
        public IActionResult Operasyon2Decide([FromBody] Operasyon2DecideRequest request)
        {
            if (request?.Id <= 0)
                return Json(new { success = false, message = "Geçersiz Ticket ID!" });

            var ticket = _context.Tickets.FirstOrDefault(t => t.Id == request.Id);
            if (ticket == null)
                return Json(new { success = false, message = "Ticket bulunamadı!" });

            if (ticket.Durum != TicketDurumOperasyon2)
                return Json(new { success = false, message = $"Ticket bu aşamada Operasyon 2 kararına uygun değil. Durum: {ticket.Durum}" });

            var userId = int.TryParse(User.FindFirst("UserId")?.Value, out var uid) ? uid : 0;
            if (ticket.AtananOperasyonUserId != userId)
                return Json(new { success = false, message = "Bu ticket size atanmadığı için işlem yapamazsınız." });

            ticket.MailGonderildiMi = request.MailGonderildiMi;
            ticket.MailNotu = string.IsNullOrWhiteSpace(request.Not) ? null : request.Not.Trim();
            ticket.Operasyon2OnayTarihi = DateTime.UtcNow;

            // Proje kuralı korunur:
            // - MailGonderildiMi = Hayır olsa bile durum "Saha Canli Bekleniyor" devam eder (eski davranış).
            // Yeni akış yalnızca mail = EVET iken araya girer.
            if (request.MailGonderildiMi)
            {
                ticket.Durum = TicketDurumEntegrasyonBekliyor;

                // İş Takip'e otomatik kayıt (DB'ye yaz, aranabilir olsun)
                var jiraId = $"TICKET-{ticket.Id}";
                var talepKonusu = $"Entegrasyon: {ticket.FirmaAdi}".Trim();
                var talepAcan = (ticket.AtananOperasyonKullaniciAdi ?? User.Identity?.Name ?? "Sistem").Trim();

                var existing = _context.JiraTasks.FirstOrDefault(x => x.JiraId == jiraId);
                if (existing == null)
                {
                    _context.JiraTasks.Add(new JiraTask
                    {
                        JiraId = jiraId,
                        TalepKonusu = talepKonusu,
                        TalepAcan = talepAcan,
                        Durum = "Beklemede",
                        TakipEden = ticket.AtananOperasyonKullaniciAdi
                    });
                }
                else
                {
                    existing.Durum = "Beklemede";
                    if (string.IsNullOrWhiteSpace(existing.TalepKonusu))
                        existing.TalepKonusu = talepKonusu;
                    if (string.IsNullOrWhiteSpace(existing.TalepAcan))
                        existing.TalepAcan = talepAcan;
                }
            }

            _context.SaveChanges();
            return Json(new { success = true, message = "✅ Operasyon 2 kararı kaydedildi." });
        }

        // ✅ Entegrasyon bitti -> tekrar canlı beklemeye dönsün
        [HttpPost]
        [Authorize(Roles = "Operasyon")]
        public IActionResult EntegrasyonTamamlandi([FromBody] EntegrasyonTamamlandiRequest request)
        {
            if (request?.Id <= 0)
                return Json(new { success = false, message = "Geçersiz Ticket ID!" });

            var ticket = _context.Tickets.FirstOrDefault(t => t.Id == request.Id);
            if (ticket == null)
                return Json(new { success = false, message = "Ticket bulunamadı!" });

            if (ticket.Durum != TicketDurumEntegrasyonBekliyor)
                return Json(new { success = false, message = $"Ticket bu aşamada entegrasyon tamamlamaya uygun değil. Durum: {ticket.Durum}" });

            var userId = int.TryParse(User.FindFirst("UserId")?.Value, out var uid) ? uid : 0;
            if (ticket.AtananOperasyonUserId != userId)
                return Json(new { success = false, message = "Bu ticket size atanmadığı için işlem yapamazsınız." });

            ticket.Durum = TicketDurumSahaCanliBekleniyor;

            // Takip kaydı varsa durumu güncelle (istersen "Tamamlandı" da yapabiliriz)
            var jiraId = $"TICKET-{ticket.Id}";
            var task = _context.JiraTasks.FirstOrDefault(x => x.JiraId == jiraId);
            if (task != null)
                task.Durum = "Aktif";

            _context.SaveChanges();
            return Json(new { success = true, message = "✅ Entegrasyon tamamlandı. Ticket tekrar Saha Canli Bekleniyor durumuna alındı." });
        }

        [HttpPost]
        [Authorize(Roles = "Saha,KurumsalSaha")]
        public IActionResult SahaCanliAcildi([FromBody] SahaCanliRequest request)
        {
            if (request?.Id <= 0)
                return Json(new { success = false, message = "Geçersiz Ticket ID!" });

            var ticket = _context.Tickets.FirstOrDefault(t => t.Id == request.Id);
            if (ticket == null)
                return Json(new { success = false, message = "Ticket bulunamadı!" });

            var userId = int.TryParse(User.FindFirst("UserId")?.Value, out var uid) ? uid : 0;
            if (ticket.OlusturanUserId != userId)
                return Json(new { success = false, message = "Sadece ticket'ı açan saha kullanıcısı bu işlemi yapabilir." });

            if (ticket.Durum != TicketDurumSahaCanliBekleniyor)
                return Json(new { success = false, message = $"Ticket bu aşamada canlı açılışa uygun değil. Durum: {ticket.Durum}" });

            ticket.CanliAcildiTarihi = DateTime.UtcNow;
            ticket.CanliNotu = string.IsNullOrWhiteSpace(request.Not) ? null : request.Not.Trim();

            var firmaAdi = (ticket.FirmaAdi ?? string.Empty).Trim();
            var siteUrl = (ticket.MusteriWebSitesi ?? string.Empty).Trim();

            var firmaKey = firmaAdi.ToLowerInvariant();
            var siteKey = siteUrl.ToLowerInvariant();

            var mevcutMusteri = _context.Musteriler.FirstOrDefault(m =>
                ((m.Firma ?? string.Empty).Trim().ToLower()) == firmaKey &&
                ((m.SiteUrl ?? string.Empty).Trim().ToLower()) == siteKey);

            var isKurumsal = User.IsInRole("KurumsalSaha");
            var kaynak = isKurumsal ? "Kurumsal" : null;

            Musteri musteri;
            if (mevcutMusteri != null)
            {
                mevcutMusteri.Firma = string.IsNullOrWhiteSpace(firmaAdi) ? mevcutMusteri.Firma : firmaAdi;
                mevcutMusteri.FirmaYetkilisi = $"{ticket.YazilimciAdi} {ticket.YazilimciSoyadi}";
                mevcutMusteri.Telefon = ticket.IrtibatNumarasi;
                mevcutMusteri.SiteUrl = siteUrl;
                mevcutMusteri.Teknoloji = ticket.TeknolojiBilgisi;
                mevcutMusteri.Durum = "Aktif";
                mevcutMusteri.TalepSahibi = ticket.OlusturanKullaniciAdi;
                mevcutMusteri.Aciklama = ticket.Aciklama;
                mevcutMusteri.KayitTarihi = DateTime.Now;

                if (isKurumsal)
                    mevcutMusteri.Kaynak = kaynak;

                musteri = mevcutMusteri;
                _context.SaveChanges();
            }
            else
            {
                musteri = new Musteri
                {
                    Firma = firmaAdi,
                    FirmaYetkilisi = $"{ticket.YazilimciAdi} {ticket.YazilimciSoyadi}",
                    Telefon = ticket.IrtibatNumarasi,
                    SiteUrl = siteUrl,
                    Teknoloji = ticket.TeknolojiBilgisi,
                    Durum = "Aktif",
                    TalepSahibi = ticket.OlusturanKullaniciAdi,
                    Aciklama = ticket.Aciklama,
                    KayitTarihi = DateTime.Now,
                    Kaynak = kaynak
                };

                _context.Musteriler.Add(musteri);
                _context.SaveChanges();
            }

            ticket.MusteriID = musteri.MusteriID;
            ticket.Durum = TicketDurumMusteriKaydedildi;

            _context.SaveChanges();
            return Json(new { success = true, message = "✅ Canlı açılış kaydedildi. Müşteri kaydı oluşturuldu/güncellendi." });
        }

        [HttpPost]
        [Authorize(Roles = "Saha,KurumsalSaha")]
        public IActionResult SahaReddet([FromBody] SahaRedRequest request)
        {
            if (request?.Id <= 0)
                return Json(new { success = false, message = "Geçersiz Ticket ID!" });

            var ticket = _context.Tickets.FirstOrDefault(t => t.Id == request.Id);
            if (ticket == null)
                return Json(new { success = false, message = "Ticket bulunamadı!" });

            var userId = int.TryParse(User.FindFirst("UserId")?.Value, out var uid) ? uid : 0;
            if (ticket.OlusturanUserId != userId)
                return Json(new { success = false, message = "Sadece ticket'ı açan saha kullanıcısı bu işlemi yapabilir." });

            var not = (request.Not ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(not))
                return Json(new { success = false, message = "Açıklama zorunlu." });

            ticket.Durum = TicketDurumReddedildi;
            ticket.KararAciklamasi = not;

            _context.SaveChanges();
            return Json(new { success = true, message = "✅ Ticket reddedildi." });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult ChangeAssignment([FromBody] ChangeAssignmentRequest request)
        {
            if (request == null)
                return BadRequest(new { success = false, message = "Geçersiz istek." });

            if (request.TicketId <= 0)
                return BadRequest(new { success = false, message = "Geçersiz Ticket ID." });

            if (request.YeniOperasyonUserId <= 0)
                return BadRequest(new { success = false, message = "Yeni operasyon seçiniz." });

            var neden = (request.Neden ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(neden))
                return BadRequest(new { success = false, message = "Değişiklik nedeni zorunlu." });

            if (neden.Length > 500)
                return BadRequest(new { success = false, message = "Değişiklik nedeni maksimum 500 karakter olmalıdır." });

            var ticket = _context.Tickets.FirstOrDefault(t => t.Id == request.TicketId);
            if (ticket == null)
                return NotFound(new { success = false, message = "Ticket bulunamadı." });

            var newUser = _context.Users.FirstOrDefault(u => u.Id == request.YeniOperasyonUserId && u.Role == "Operasyon");
            if (newUser == null)
                return BadRequest(new { success = false, message = "Seçilen kullanıcı operasyon rolünde değil veya bulunamadı." });

            var oldAssigneeName = ticket.AtananOperasyonKullaniciAdi;
            var oldAssigneeId = ticket.AtananOperasyonUserId;

            ticket.AtananOperasyonUserId = newUser.Id;
            ticket.AtananOperasyonKullaniciAdi = newUser.FullName ?? newUser.UserName;
            ticket.AtanmaTarihi = DateTime.UtcNow;

            var adminId = int.TryParse(User.FindFirst("UserId")?.Value, out var adminUid) ? adminUid : 0;
            var adminName = User.Identity?.Name ?? "Bilinmiyor";

            _context.TicketAtamaLoglari.Add(new TicketAtamaLog
            {
                TicketId = ticket.Id,
                EskiOperasyonUserId = oldAssigneeId,
                EskiOperasyonKullaniciAdi = oldAssigneeName,
                YeniOperasyonUserId = newUser.Id,
                YeniOperasyonKullaniciAdi = ticket.AtananOperasyonKullaniciAdi,
                DegisiklikNedeni = neden,
                DegistirenUserId = adminId,
                DegistirenKullaniciAdi = adminName,
                DegisiklikTarihi = DateTime.UtcNow
            });

            _context.SaveChanges();
            return Json(new { success = true, message = "✅ Atama güncellendi." });
        }

        public sealed class Operasyon1DecideRequest
        {
            public int Id { get; set; }
            public bool EntegreOlabilirMi { get; set; }
            public string? Not { get; set; }
        }

        public sealed class Operasyon2DecideRequest
        {
            public int Id { get; set; }
            public bool MailGonderildiMi { get; set; }
            public string? Not { get; set; }
        }

        public sealed class EntegrasyonTamamlandiRequest
        {
            public int Id { get; set; }
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

        public sealed class ChangeAssignmentRequest
        {
            public int TicketId { get; set; }
            public int YeniOperasyonUserId { get; set; }
            public string? Neden { get; set; }
        }
    }
}