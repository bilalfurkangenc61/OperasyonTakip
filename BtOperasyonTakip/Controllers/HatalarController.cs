using BtOperasyonTakip.Data;
using BtOperasyonTakip.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace BtOperasyonTakip.Controllers
{
    [Authorize]
    public class HatalarController : Controller
    {
        private readonly AppDbContext _context;

        public HatalarController(AppDbContext context)
        {
            _context = context;
        }

        // period format: "yyyy-MM" (örn: 2026-01)
        public async Task<IActionResult> Index(string q, string durum, string kategori, string? period)
        {
            IQueryable<Hata> query = _context.Hatalar.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(h =>
                    h.HataAdi.Contains(q) ||
                    h.HataAciklama.Contains(q) ||
                    h.OlusturanKullaniciAdi.Contains(q));
            }

            if (!string.IsNullOrWhiteSpace(durum))
                query = query.Where(h => h.Durum == durum);

            if (!string.IsNullOrWhiteSpace(kategori))
                query = query.Where(h => h.KategoriBilgisi == kategori);

            if (TryParsePeriod(period, out var start, out var end))
                query = query.Where(h => h.OlusturmaTarihi >= start && h.OlusturmaTarihi < end);

            var hatalar = await query
                .OrderByDescending(h => h.OlusturmaTarihi)
                .ToListAsync();

            var mevcutHatalar = await _context.Hatalar
                .Where(h => h.Durum == "Açık")
                .OrderBy(h => h.HataAdi)
                .ToListAsync();

            ViewBag.MevcutHatalar = mevcutHatalar;
            ViewBag.Q = q;
            ViewBag.Durum = durum;
            ViewBag.Kategori = kategori;
            ViewBag.Period = period;

            return View(hatalar);
        }

        // Excel (paketsiz): CSV indir (Excel açar)
        [HttpGet]
        public async Task<IActionResult> ExportExcel(string q, string durum, string kategori, string? period)
        {
            IQueryable<Hata> query = _context.Hatalar.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(h =>
                    h.HataAdi.Contains(q) ||
                    h.HataAciklama.Contains(q) ||
                    h.OlusturanKullaniciAdi.Contains(q));
            }

            if (!string.IsNullOrWhiteSpace(durum))
                query = query.Where(h => h.Durum == durum);

            if (!string.IsNullOrWhiteSpace(kategori))
                query = query.Where(h => h.KategoriBilgisi == kategori);

            if (TryParsePeriod(period, out var start, out var end))
                query = query.Where(h => h.OlusturmaTarihi >= start && h.OlusturmaTarihi < end);

            var data = await query
                .OrderByDescending(h => h.OlusturmaTarihi)
                .ToListAsync();

            var sb = new StringBuilder();

            // BOM: Excel'in UTF-8 Türkçe karakterleri doğru açması için
            sb.Append('\uFEFF');

            // Başlık
            sb.AppendLine(string.Join(';', new[]
            {
                "Hata",
                "Açıklama",
                "Kategori",
                "Bildiren",
                "Durum",
                "Tarih"
            }));

            foreach (var h in data)
            {
                sb.AppendLine(string.Join(';', new[]
                {
                    CsvEscape(h.HataAdi),
                    CsvEscape(h.HataAciklama),
                    CsvEscape(h.KategoriBilgisi),
                    CsvEscape(h.OlusturanKullaniciAdi),
                    CsvEscape(h.Durum),
                    CsvEscape(h.OlusturmaTarihi.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR")))
                }));
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var safePeriod = string.IsNullOrWhiteSpace(period) ? "tum-aylar" : period;
            var fileName = $"hatalar_{safePeriod}_{DateTime.Now:yyyyMMdd_HHmm}.csv";

            return File(bytes, "text/csv; charset=utf-8", fileName);
        }

        private static string CsvEscape(string? value)
        {
            value ??= "";
            // CSV ayırıcı ";" olduğundan; ;, " veya satır sonu varsa tırnakla
            var mustQuote = value.Contains(';') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
            value = value.Replace("\"", "\"\"");
            return mustQuote ? $"\"{value}\"" : value;
        }

        private static bool TryParsePeriod(string? period, out DateTime start, out DateTime end)
        {
            start = default;
            end = default;

            if (string.IsNullOrWhiteSpace(period))
                return false;

            if (!DateTime.TryParseExact(
                    period.Trim(),
                    "yyyy-MM",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var dt))
                return false;

            start = new DateTime(dt.Year, dt.Month, 1);
            end = start.AddMonths(1);
            return true;
        }

        [HttpGet]
        public IActionResult Yeni()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Yeni(Hata h)
        {
            ModelState.Remove("OperasyonCevabi");
            ModelState.Remove("CevaplayanKullaniciAdi");
            ModelState.Remove("CevaplaamaTarihi");
            ModelState.Remove("OlusturanUserId");
            ModelState.Remove("OlusturanKullaniciAdi");
            ModelState.Remove("OlusturmaTarihi");
            ModelState.Remove("SecilenHataId");

            if (!ModelState.IsValid)
                return View(h);

            h.OlusturanUserId = int.Parse(User.FindFirst("UserId")!.Value);
            h.OlusturanKullaniciAdi = User.Identity!.Name!;
            h.OlusturmaTarihi = DateTime.Now;
            h.OperasyonCevabi = "";
            h.CevaplayanKullaniciAdi = "";

            _context.Hatalar.Add(h);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Create(string hataAdi, string hataAciklama, string kategori, int? mevcutHataId)
        {
            var userId = int.Parse(User.FindFirst("UserId")!.Value);
            var userName = User.Identity!.Name;

            var hata = new Hata
            {
                HataAdi = hataAdi,
                HataAciklama = hataAciklama,
                KategoriBilgisi = kategori,
                Durum = "Açık",
                SecilenHataId = mevcutHataId,
                OlusturanUserId = userId,
                OlusturanKullaniciAdi = userName,
                OlusturmaTarihi = DateTime.Now,
                OperasyonCevabi = "",
                CevaplayanKullaniciAdi = ""
            };

            _context.Hatalar.Add(hata);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Detay(int id)
        {
            var hata = await _context.Hatalar.FindAsync(id);
            if (hata == null) return NotFound();
            return View(hata);
        }

        [HttpPost]
        public async Task<IActionResult> Cevapla(int id, string cevap, string durum)
        {
            var hata = await _context.Hatalar.FindAsync(id);
            if (hata == null) return NotFound();

            var userId = int.Parse(User.FindFirst("UserId")!.Value);
            var fullName = User.Identity!.Name;

            hata.OperasyonCevabi = cevap;
            hata.Durum = durum;
            hata.CevaplayanUserId = userId;
            hata.CevaplayanKullaniciAdi = fullName;
            hata.CevaplaamaTarihi = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["HataYanitiVar"] = "ok";

            return RedirectToAction("Detay", new { id });
        }
    }
}