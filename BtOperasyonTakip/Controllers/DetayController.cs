using BtOperasyonTakip.Data;
using BtOperasyonTakip.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BtOperasyonTakip.Controllers
{
    [Authorize(Roles = "Operasyon")]
    public class DetayController : Controller
    {
        private readonly AppDbContext _context;
        public DetayController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var musteriler = _context.Musteriler.ToList();
            return View(musteriler);
        }

        // MÜŞTERİ DÜZENLEME - GET
        public IActionResult GetMusteri(int id)
        {
            var musteri = _context.Musteriler.Find(id);
            if (musteri == null)
                return NotFound();

            return Json(new
            {
                musteriID = musteri.MusteriID,
                firma = musteri.Firma,
                firmaYetkilisi = musteri.FirmaYetkilisi,
                telefon = musteri.Telefon,
                siteUrl = musteri.SiteUrl,
                teknoloji = musteri.Teknoloji,
                durum = musteri.Durum,
                talepSahibi = musteri.TalepSahibi,
                aciklama = musteri.Aciklama,
                kayitTarihi = musteri.KayitTarihi?.ToString("yyyy-MM-dd")
            });
        }

        // MÜŞTERİ DÜZENLEME - POST
        [HttpPost]
        public IActionResult UpdateMusteri(Musteri musteri)
        {
            try
            {
                Console.WriteLine($"📝 Müşteri güncelleniyor: ID={musteri.MusteriID}");

                var existingMusteri = _context.Musteriler.Find(musteri.MusteriID);
                if (existingMusteri == null)
                {
                    return NotFound($"Müşteri ID {musteri.MusteriID} bulunamadı");
                }

                // Alanları güncelle
                existingMusteri.Firma = musteri.Firma;
                existingMusteri.FirmaYetkilisi = musteri.FirmaYetkilisi;
                existingMusteri.Telefon = musteri.Telefon;
                existingMusteri.SiteUrl = musteri.SiteUrl;
                existingMusteri.Teknoloji = musteri.Teknoloji;
                existingMusteri.Durum = musteri.Durum;
                existingMusteri.TalepSahibi = musteri.TalepSahibi;
                existingMusteri.Aciklama = musteri.Aciklama;

                _context.SaveChanges();
                Console.WriteLine("✅ Müşteri güncellendi!");

                return Json(new { success = true, message = "Müşteri başarıyla güncellendi!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Müşteri güncelleme hatası: {ex.Message}");
                return Json(new { success = false, message = "Güncelleme sırasında hata: " + ex.Message });
            }
        }

        // MÜŞTERİ SİLME
        [HttpDelete]
        public IActionResult DeleteMusteri(int id)
        {
            try
            {
                var musteri = _context.Musteriler
                    .Include(m => m.Detaylar)
                    .FirstOrDefault(m => m.MusteriID == id);

                if (musteri == null)
                    return NotFound();

                // İlişkili detayları sil
                if (musteri.Detaylar != null && musteri.Detaylar.Any())
                {
                    _context.Detaylar.RemoveRange(musteri.Detaylar);
                }

                _context.Musteriler.Remove(musteri);
                _context.SaveChanges();

                return Json(new { success = true, message = "Müşteri başarıyla silindi!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Silme sırasında hata: " + ex.Message });
            }
        }

        // DETAY İŞLEMLERİ (mevcut kodunuz)
        public IActionResult GetDetaylar(int musteriId)
        {
            var detaylar = _context.Detaylar
                .Where(d => d.MusteriID == musteriId)
                .OrderByDescending(d => d.Tarih)
                .ToList();

            ViewBag.MusteriID = musteriId;
            return PartialView("_DetayListesi", detaylar);
        }

        [HttpPost]
        public IActionResult AddDetay(Detay detay)
        {
            try
            {
                Console.WriteLine($"📩 AddDetay tetiklendi: MusteriID={detay.MusteriID}, Tarih={detay.Tarih}, Gorusulen={detay.Gorusulen}");

                if (!_context.Musteriler.Any(m => m.MusteriID == detay.MusteriID))
                {
                    Console.WriteLine($"❌ HATA: MusteriID {detay.MusteriID} bulunamadı!");
                    return BadRequest($"Müşteri ID {detay.MusteriID} bulunamadı");
                }

                _context.Detaylar.Add(detay);
                _context.SaveChanges();
                Console.WriteLine("✅ Kayıt eklendi!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("💥 HATA: " + ex.Message);
            }

            var detaylar = _context.Detaylar
                .Where(d => d.MusteriID == detay.MusteriID)
                .OrderByDescending(d => d.Tarih)
                .ToList();

            ViewBag.MusteriID = detay.MusteriID;
            return PartialView("_DetayListesi", detaylar);
        }

        [HttpPost]
        public IActionResult UpdateDetay(Detay detay)
        {
            var existing = _context.Detaylar.FirstOrDefault(d => d.DetayID == detay.DetayID);
            if (existing == null) return NotFound();

            existing.Tarih = detay.Tarih;
            existing.Gorusulen = detay.Gorusulen;
            existing.Aciklama = detay.Aciklama;
            existing.Kekleyen = detay.Kekleyen;
            _context.SaveChanges();

            var detaylar = _context.Detaylar
                .Where(d => d.MusteriID == detay.MusteriID)
                .OrderByDescending(d => d.Tarih)
                .ToList();

            ViewBag.MusteriID = detay.MusteriID;
            return PartialView("_DetayListesi", detaylar);
        }

        [HttpGet]
        public IActionResult GetDetay(int id)
        {
            var detay = _context.Detaylar.FirstOrDefault(d => d.DetayID == id);
            if (detay == null)
                return NotFound();

            return Json(new
            {
                detayID = detay.DetayID,
                musteriID = detay.MusteriID,
                tarih = detay.Tarih.ToString("yyyy-MM-dd"),
                gorusulen = detay.Gorusulen,
                aciklama = detay.Aciklama,
                kekleyen = detay.Kekleyen
            });
        }


        [HttpDelete]
        public IActionResult DeleteDetay(int id)
        {
            var detay = _context.Detaylar.Find(id);
            if (detay == null)
                return NotFound();

            _context.Detaylar.Remove(detay);
            _context.SaveChanges();
            return Ok();
        }
    }
}