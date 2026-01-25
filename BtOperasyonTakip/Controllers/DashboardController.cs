using BtOperasyonTakip.Data;
using BtOperasyonTakip.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BtOperasyonTakip.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var musteriler = _context.Musteriler?.ToList() ?? new List<Musteri>();

            var jiraTasks = _context.JiraTasks?.ToList() ?? new List<JiraTask>();
            var jiraBeklemede = jiraTasks.Count(t => !string.IsNullOrWhiteSpace(t.Durum) && t.Durum.Trim().ToLower() == "beklemede");
            var jiraAktif = jiraTasks.Count(t => !string.IsNullOrWhiteSpace(t.Durum) && t.Durum.Trim().ToLower() == "aktif");
            var jiraTamam = jiraTasks.Count(t => !string.IsNullOrWhiteSpace(t.Durum) && t.Durum.Trim().ToLower() == "tamamlandı");

            var toplantiNotlari = _context.ToplantiNotlari?.ToList() ?? new List<ToplantiNotu>();

            var tickets = _context.Tickets?.ToList() ?? new List<Ticket>();
            var ticketOnaybekleniyor = tickets.Count(t => t.Durum == "Onay Bekleniyor");
            var ticketOnaylandi = tickets.Count(t => t.Durum == "Onaylandi");
            var ticketReddedildi = tickets.Count(t => t.Durum == "Reddedildi");

            var aktifMusteri = musteriler.Count(m => !string.IsNullOrWhiteSpace(m.Durum) && m.Durum.Trim().ToLower() == "aktif");
            var pasifMusteri = musteriler.Count(m => !string.IsNullOrWhiteSpace(m.Durum) && m.Durum.Trim().ToLower() == "pasif");

            var bekleyenIs = jiraTasks.Count(t => !string.IsNullOrWhiteSpace(t.Durum) && t.Durum.Trim().ToLower() == "beklemede");

            var buAyEklenen = musteriler.Count(m =>
                m.KayitTarihi.HasValue &&
                m.KayitTarihi.Value.Month == DateTime.Now.Month &&
                m.KayitTarihi.Value.Year == DateTime.Now.Year);

            var aylar = Enumerable.Range(0, 6)
                .Select(i => DateTime.Now.AddMonths(-i))
                .OrderBy(x => x)
                .ToList();

            var aylikSayilar = new List<int>();
            var ayEtiketleri = new List<string>();

            foreach (var ay in aylar)
            {
                int sayi = _context.Musteriler.Count(m =>
                    m.KayitTarihi.HasValue &&
                    m.KayitTarihi.Value.Month == ay.Month &&
                    m.KayitTarihi.Value.Year == ay.Year);

                aylikSayilar.Add(sayi);
                ayEtiketleri.Add(ay.ToString("MMMM", new CultureInfo("tr-TR")));
            }

            var musteriDurumGruplari = musteriler
                .GroupBy(m => (m.Durum ?? "Bilinmiyor").Trim())
                .Select(g => new { Durum = g.Key, Sayi = g.Count() })
                .OrderByDescending(x => x.Sayi)
                .ToList();

            var model = new DashboardViewModel
            {
                ToplamMusteri = musteriler.Count,
                AktifMusteri = aktifMusteri,
                PasifMusteri = pasifMusteri,
                Bekleyen = bekleyenIs,
                BuAyEklenen = buAyEklenen,
                AylikMusteriSayilari = aylikSayilar,
                AyEtiketleri = ayEtiketleri,

                JiraBeklemede = jiraBeklemede,
                JiraAktif = jiraAktif,
                JiraTamamlandi = jiraTamam,

                TicketOnaybekleniyor = ticketOnaybekleniyor,
                TicketOnaylandi = ticketOnaylandi,
                TicketReddedildi = ticketReddedildi,
                ToplamTicket = tickets.Count,

                // YENİ: tüm müşteri durumları (Aktif/Pasif/Beklemede/...)
                MusteriDurumEtiketleri = musteriDurumGruplari.Select(x => x.Durum).ToList(),
                MusteriDurumSayilari = musteriDurumGruplari.Select(x => x.Sayi).ToList(),

                Musteriler = musteriler
                    .OrderByDescending(m => m.KayitTarihi ?? DateTime.MinValue)
                    .Take(10)
                    .ToList(),

                JiraTasks = jiraTasks
                    .OrderByDescending(t => t.OlusturmaTarihi)
                    .Take(10)
                    .ToList(),

                Tickets = tickets
                    .OrderByDescending(t => t.OlusturmaTarihi)
                    .ToList(),

                ToplantiNotlari = toplantiNotlari
                    .OrderByDescending(n => n.Tarih)
                    .Take(10)
                    .ToList()
            };

            return View(model);
        }
    }
}