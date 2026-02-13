using System.Globalization;
using System.Text;
using BtOperasyonTakip.Data;
using BtOperasyonTakip.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BtOperasyonTakip.Controllers;

[Authorize(Roles = "Operasyon,Admin")]
public sealed class MusteriController : Controller
{
    private readonly AppDbContext _context;

    public MusteriController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        int pageSize = 25,
        string? search = null,
        string? durum = null,
        string? teknoloji = null,
        string? talepSahibi = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 25;
        if (pageSize > 100) pageSize = 100;

        IQueryable<Musteri> query = _context.Musteriler.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Firma != null && x.Firma.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(durum))
        {
            var d = durum.Trim();
            query = query.Where(x => x.Durum == d);
        }

        if (!string.IsNullOrWhiteSpace(teknoloji))
        {
            var t = teknoloji.Trim();
            query = query.Where(x => x.Teknoloji == t);
        }

        if (!string.IsNullOrWhiteSpace(talepSahibi))
        {
            var ts = talepSahibi.Trim();
            query = query.Where(x => x.TalepSahibi == ts);
        }

        var totalCount = await query.CountAsync();

        var model = await query
            .OrderByDescending(x => x.KayitTarihi)
            .ThenByDescending(x => x.MusteriID)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewData["Page"] = page;
        ViewData["PageSize"] = pageSize;
        ViewData["TotalCount"] = totalCount;
        ViewData["Search"] = search;
        ViewData["SelectedDurum"] = durum;
        ViewData["SelectedTeknoloji"] = teknoloji;
        ViewData["SelectedTalepSahibi"] = talepSahibi;

        // Filtre dropdown'ları için (sadece Parametreler tablosundan)
        ViewData["Durumlar"] = await _context.Parametreler
            .AsNoTracking()
            .Where(p => p.Tur == "Durum" && p.ParAdi != null && p.ParAdi != "")
            .OrderBy(p => p.ParAdi)
            .Select(p => p.ParAdi!)
            .ToListAsync();

        ViewData["Teknolojiler"] = await _context.Parametreler
            .AsNoTracking()
            .Where(p => p.Tur == "Teknoloji" && p.ParAdi != null && p.ParAdi != "")
            .OrderBy(p => p.ParAdi)
            .Select(p => p.ParAdi!)
            .ToListAsync();

        ViewData["TalepSahipleri"] = await _context.Parametreler
            .AsNoTracking()
            .Where(p => p.Tur == "TalepEden" && p.ParAdi != null && p.ParAdi != "")
            .OrderBy(p => p.ParAdi)
            .Select(p => p.ParAdi!)
            .ToListAsync();

        return View(model);
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

    // AJAX'SIZ SAYFADA KULLANILMIYOR: İsterseniz kaldırabilirsiniz.
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
                kaynak = x.Kaynak,
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

    [HttpGet("/Musteri/ExportExcelByMonth")]
    public async Task<IActionResult> ExportExcelByMonth(string? month)
    {
        if (string.IsNullOrWhiteSpace(month))
            return BadRequest("Ay bilgisi zorunlu. Örn: 2026-02");

        if (!DateTime.TryParseExact(month.Trim(), "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var monthStart))
            return BadRequest("Geçersiz ay formatı. Örn: 2026-02");

        var start = new DateTime(monthStart.Year, monthStart.Month, 1, 0, 0, 0);
        var end = start.AddMonths(1);

        var rows = await _context.Musteriler
            .AsNoTracking()
            .Where(x => x.KayitTarihi != null &&
                        x.KayitTarihi.Value >= start &&
                        x.KayitTarihi.Value < end)
            .OrderByDescending(x => x.KayitTarihi)
            .ThenByDescending(x => x.MusteriID)
            .Select(x => new
            {
                x.MusteriID,
                x.Firma,
                x.FirmaYetkilisi,
                x.Telefon,
                x.SiteUrl,
                x.Teknoloji,
                x.Durum,
                x.TalepSahibi,
                x.KayitTarihi,
                x.Aciklama
            })
            .ToListAsync();

        const char sep = '\t';

        static string NormalizeCell(string? s)
        {
            var v = (s ?? "")
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Replace("\n", " ");

            if (v.Length > 0 && (v[0] == '=' || v[0] == '+' || v[0] == '-' || v[0] == '@'))
                v = "'" + v;

            v = v.Replace("\t", " ");
            return v;
        }

        var sb = new StringBuilder();

        sb.Append("ID").Append(sep)
          .Append("Firma").Append(sep)
          .Append("Yetkili").Append(sep)
          .Append("Telefon").Append(sep)
          .Append("SiteUrl").Append(sep)
          .Append("Teknoloji").Append(sep)
          .Append("Durum").Append(sep)
          .Append("TalepSahibi").Append(sep)
          .Append("KayitTarihi").Append(sep)
          .Append("Aciklama")
          .Append("\r\n");

        foreach (var r in rows)
        {
            sb.Append(NormalizeCell(r.MusteriID.ToString(CultureInfo.InvariantCulture))).Append(sep)
              .Append(NormalizeCell(r.Firma)).Append(sep)
              .Append(NormalizeCell(r.FirmaYetkilisi)).Append(sep)
              .Append(NormalizeCell(r.Telefon)).Append(sep)
              .Append(NormalizeCell(r.SiteUrl)).Append(sep)
              .Append(NormalizeCell(r.Teknoloji)).Append(sep)
              .Append(NormalizeCell(r.Durum)).Append(sep)
              .Append(NormalizeCell(r.TalepSahibi)).Append(sep)
              .Append(NormalizeCell(r.KayitTarihi?.ToString("dd.MM.yyyy") ?? "-")).Append(sep)
              .Append(NormalizeCell(r.Aciklama))
              .Append("\r\n");
        }

        var bytes = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/tab-separated-values", $"Musteriler_{month}.xls");
    }

    private static bool TryParseTrDate(string? s, out DateTime dt)
    {
        dt = default;
        if (string.IsNullOrWhiteSpace(s)) return false;

        return DateTime.TryParseExact(
            s.Trim(),
            new[] { "dd.MM.yyyy", "d.M.yyyy", "yyyy-MM-dd" },
            new CultureInfo("tr-TR"),
            DateTimeStyles.None,
            out dt);
    }
}