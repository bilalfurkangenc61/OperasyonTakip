using System.Globalization;
using BtOperasyonTakip.Data;
using BtOperasyonTakip.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BtOperasyonTakip.Controllers;

[Authorize(Roles = "Operasyon")]
public sealed class MusteriController : Controller
{
    private readonly AppDbContext _context;

    public MusteriController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("/Musteri/Filters")]
    public async Task<IActionResult> Filters()
    {
        var teknolojiler = await _context.Parametreler
            .AsNoTracking()
            .Where(p => p.Tur == "Teknoloji" && p.ParAdi != null && p.ParAdi != "")
            .OrderBy(p => p.ParAdi)
            .Select(p => p.ParAdi!)
            .ToListAsync();

        var talepSahipleri = await _context.Parametreler
            .AsNoTracking()
            .Where(p => p.Tur == "TalepEden" && p.ParAdi != null && p.ParAdi != "")
            .OrderBy(p => p.ParAdi)
            .Select(p => p.ParAdi!)
            .ToListAsync();

        var durumlar = await _context.Parametreler
            .AsNoTracking()
            .Where(p => p.Tur == "Durum" && p.ParAdi != null && p.ParAdi != "")
            .OrderBy(p => p.ParAdi)
            .Select(p => p.ParAdi!)
            .ToListAsync();

        return Json(new
        {
            teknolojiler,
            talepSahipleri,
            durumlar
        });
    }

    [HttpGet("/Musteri/Data")]
    public async Task<IActionResult> Data(
        int draw,
        int start,
        int length,
        string? firma,
        string? durum,
        string? teknoloji,
        string? talepSahibi,
        string? minDate,
        string? maxDate)
    {
        IQueryable<Musteri> query = _context.Musteriler.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(firma))
        {
            var term = firma.Trim();
            query = query.Where(x => x.Firma != null && x.Firma.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(durum))
        {
            query = query.Where(x => x.Durum == durum);
        }

        if (!string.IsNullOrWhiteSpace(teknoloji))
        {
            query = query.Where(x => x.Teknoloji == teknoloji);
        }

        if (!string.IsNullOrWhiteSpace(talepSahibi))
        {
            query = query.Where(x => x.TalepSahibi == talepSahibi);
        }

        if (TryParseTrDate(minDate, out var min))
        {
            query = query.Where(x => x.KayitTarihi != null && x.KayitTarihi.Value.Date >= min.Date);
        }

        if (TryParseTrDate(maxDate, out var max))
        {
            query = query.Where(x => x.KayitTarihi != null && x.KayitTarihi.Value.Date <= max.Date);
        }

        var recordsTotal = await _context.Musteriler.AsNoTracking().CountAsync();
        var recordsFiltered = await query.CountAsync();

        query = query
            .OrderByDescending(x => x.KayitTarihi)
            .ThenByDescending(x => x.MusteriID);

        var page = await query
            .Skip(start)
            .Take(length <= 0 ? 25 : length)
            .Select(x => new
            {
                musteriID = x.MusteriID,
                firma = x.Firma,
                firmaYetkilisi = x.FirmaYetkilisi,
                telefon = x.Telefon,
                siteUrl = x.SiteUrl,
                teknoloji = x.Teknoloji,
                durum = x.Durum,
                talepSahibi = x.TalepSahibi,
                kayitTarihiText = x.KayitTarihi.HasValue ? x.KayitTarihi.Value.ToString("dd.MM.yyyy") : "-",
                aciklama = x.Aciklama
            })
            .ToListAsync();

        return Json(new
        {
            draw,
            recordsTotal,
            recordsFiltered,
            data = page
        });
    }

    private static bool TryParseTrDate(string? input, out DateTime result)
    {
        result = default;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        return DateTime.TryParseExact(
            input.Trim(),
            "dd.MM.yyyy",
            CultureInfo.GetCultureInfo("tr-TR"),
            DateTimeStyles.None,
            out result);
    }
}