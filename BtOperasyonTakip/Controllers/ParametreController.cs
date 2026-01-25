using BtOperasyonTakip.Data;
using BtOperasyonTakip.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BtOperasyonTakip.Controllers
{
    public class ParametreController : Controller
    {
        private readonly AppDbContext _context;

        public ParametreController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var parametreler = await _context.Parametreler
                .AsNoTracking()
                .OrderBy(p => p.Tur)
                .ThenBy(p => p.ParAdi)
                .ToListAsync();

            var turler = await _context.Parametreler
                .AsNoTracking()
                .Where(p => p.Tur != null && p.Tur != "")
                .Select(p => p.Tur!)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            ViewBag.Turler = turler;

            return View(parametreler);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(Parametre model)
        {
            if (ModelState.IsValid)
            {
                model.ParAdi = (model.ParAdi ?? "").Trim();
                model.Tur = (model.Tur ?? "").Trim();

                _context.Parametreler.Add(model);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }

            return View(_context.Parametreler.ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var param = _context.Parametreler.FirstOrDefault(p => p.Id == id);
            if (param != null)
            {
                _context.Parametreler.Remove(param);
                _context.SaveChanges();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TurEkle(string tur)
        {
            tur = (tur ?? "").Trim();
            if (string.IsNullOrWhiteSpace(tur))
            {
                return RedirectToAction(nameof(Index));
            }

            var exists = await _context.Parametreler.AnyAsync(p => p.Tur == tur);
            if (!exists)
            {
                // Sadece tür oluşturmak için boş bir kayıt atıyoruz (ParAdi null).
                _context.Parametreler.Add(new Parametre { Tur = tur, ParAdi = null });
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet("/Parametre/Durumlar")]
        public async Task<IActionResult> Durumlar()
        {
            var durumlar = await _context.Parametreler
                .AsNoTracking()
                .Where(p => p.Tur == "Durum" && p.ParAdi != null && p.ParAdi != "")
                .OrderBy(p => p.ParAdi)
                .Select(p => p.ParAdi!)
                .ToListAsync();

            return Json(durumlar);
        }
    }
}