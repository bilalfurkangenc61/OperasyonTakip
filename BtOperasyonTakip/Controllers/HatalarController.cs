using BtOperasyonTakip.Data;
using BtOperasyonTakip.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        // 📌 HATA LİSTE + ARAMA + FİLTRE
        public async Task<IActionResult> Index(string q, string durum, string kategori)
        {
            var query = _context.Hatalar.AsQueryable();

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

            return View(hatalar);
        }

        [HttpGet]
        public IActionResult Yeni()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Yeni(Hata h)
        {
            // Formdan GELMEYEN non-nullable alanları validate dışına al
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

        // 📌 HATA EKLE
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

        // 📌 HATA DETAY
        public async Task<IActionResult> Detay(int id)
        {
            var hata = await _context.Hatalar.FindAsync(id);
            if (hata == null) return NotFound();
            return View(hata);
        }

        // 📌 OPERASYON CEVAP
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