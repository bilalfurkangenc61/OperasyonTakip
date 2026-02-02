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

        // period format: "yyyy-MM" (örn: 2025-12)
        public IActionResult Index(string? period)
        {
            var tr = CultureInfo.GetCultureInfo("tr-TR");
            var now = DateTime.Now;

            var seciliYil = now.Year;
            var seciliAy = now.Month;

            if (!string.IsNullOrWhiteSpace(period))
            {
                var parts = period.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out var y) &&
                    int.TryParse(parts[1], out var m) &&
                    m is >= 1 and <= 12)
                {
                    seciliYil = y;
                    seciliAy = m;
                }
            }

            var seciliAyBaslangic = new DateTime(seciliYil, seciliAy, 1);
            var seciliAyBitis = seciliAyBaslangic.AddMonths(1);

            // Son 12 ay dropdown (seçim listesi)
            var aySecenekleri = Enumerable.Range(0, 12)
                .Select(i =>
                {
                    var d = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                    return (Year: d.Year, Month: d.Month, Label: d.ToString("MMMM yyyy", tr));
                })
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .ToList();

            // Sayfa geneli
            var musteriler = _context.Musteriler?.ToList() ?? new List<Musteri>();
            var jiraTasksAll = _context.JiraTasks?.ToList() ?? new List<JiraTask>();
            var toplantiNotlari = _context.ToplantiNotlari?.ToList() ?? new List<ToplantiNotu>();
            var ticketsAll = _context.Tickets?.ToList() ?? new List<Ticket>();
            var hatalar = _context.Hatalar?.ToList() ?? new List<Hata>();

            // === Jira: seçili aya göre filtre ===
            var jiraTasks = jiraTasksAll
                .Where(t => t.OlusturmaTarihi >= seciliAyBaslangic && t.OlusturmaTarihi < seciliAyBitis)
                .ToList();

            var jiraBeklemede = jiraTasks.Count(t => !string.IsNullOrWhiteSpace(t.Durum) && t.Durum.Trim().ToLower() == "beklemede");
            var jiraAktif = jiraTasks.Count(t => !string.IsNullOrWhiteSpace(t.Durum) && t.Durum.Trim().ToLower() == "aktif");
            var jiraTamam = jiraTasks.Count(t => !string.IsNullOrWhiteSpace(t.Durum) && t.Durum.Trim().ToLower() == "tamamlandı");

            // === Ticket: seçili aya göre filtre ===
            var tickets = ticketsAll
                .Where(t => t.OlusturmaTarihi >= seciliAyBaslangic && t.OlusturmaTarihi < seciliAyBitis)
                .ToList();

            var ticketOnaybekleniyor = tickets.Count(t => t.Durum == "Onay Bekleniyor");
            var ticketOnaylandi = tickets.Count(t => t.Durum == "Onaylandi");
            var ticketReddedildi = tickets.Count(t => t.Durum == "Reddedildi");

            // Müşteri sayıları (genel)
            var aktifMusteri = musteriler.Count(m => !string.IsNullOrWhiteSpace(m.Durum) && m.Durum.Trim().ToLower() == "aktif");
            var pasifMusteri = musteriler.Count(m => !string.IsNullOrWhiteSpace(m.Durum) && m.Durum.Trim().ToLower() == "pasif");
            var bekleyenIs = jiraTasks.Count(t => !string.IsNullOrWhiteSpace(t.Durum) && t.Durum.Trim().ToLower() == "beklemede");

            // Bu ay eklenen müşteri (seçili aya göre)
            var buAyEklenen = musteriler.Count(m =>
                m.KayitTarihi.HasValue &&
                m.KayitTarihi.Value >= seciliAyBaslangic &&
                m.KayitTarihi.Value < seciliAyBitis);

            // Aylık Müşteri Artışı: seçili ay baz alınarak son 6 ayın AYLIK toplamı
            var aylar = Enumerable.Range(0, 6)
                .Select(i => seciliAyBaslangic.AddMonths(-i))
                .OrderBy(x => x)
                .ToList();

            var aylikSayilar = new List<int>();
            var ayEtiketleri = new List<string>();

            foreach (var ay in aylar)
            {
                var ayStart = new DateTime(ay.Year, ay.Month, 1);
                var ayEnd = ayStart.AddMonths(1);

                int sayi = _context.Musteriler.Count(m =>
                    m.KayitTarihi.HasValue &&
                    m.KayitTarihi.Value >= ayStart &&
                    m.KayitTarihi.Value < ayEnd);

                aylikSayilar.Add(sayi);
                ayEtiketleri.Add(ay.ToString("MMMM yyyy", tr));
            }

            // === Müşteri Durumu: Parametreler(Tur="Durum") etiketleri + seçili ayda eklenen müşterilerden sayım ===
            var paramDurumlar = _context.Parametreler
                .Where(p => p.Tur == "Durum" && p.ParAdi != null && p.ParAdi != "")
                .OrderBy(p => p.ParAdi)
                .Select(p => p.ParAdi!)
                .ToList();

            // Seçili ayda kayıt olan müşteriler
            var musterilerSeciliAy = musteriler
                .Where(m => m.KayitTarihi.HasValue &&
                            m.KayitTarihi.Value >= seciliAyBaslangic &&
                            m.KayitTarihi.Value < seciliAyBitis)
                .ToList();

            // Seçili ay müşteri durum sayacı (db’de ne varsa)
            var durumSayac = musterilerSeciliAy
                .GroupBy(m => string.IsNullOrWhiteSpace(m.Durum) ? "" : m.Durum.Trim())
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            // Her parametre durumu tek tek göster (0 olsa bile)
            var musteriDurumEtiketleri = new List<string>();
            var musteriDurumSayilari = new List<int>();

            foreach (var d in paramDurumlar)
            {
                musteriDurumEtiketleri.Add(d);
                musteriDurumSayilari.Add(durumSayac.TryGetValue(d, out var c) ? c : 0);
            }

            // Parametrelerde olmayan durumlar varsa "Diğer" olarak ekle (seçili ay için)
            var otherCount = durumSayac
                .Where(kvp =>
                    !string.IsNullOrWhiteSpace(kvp.Key) &&
                    !paramDurumlar.Contains(kvp.Key, StringComparer.OrdinalIgnoreCase))
                .Sum(kvp => kvp.Value);

            if (otherCount > 0)
            {
                musteriDurumEtiketleri.Add("Diğer");
                musteriDurumSayilari.Add(otherCount);
            }

            // Hatalar: seçili aya göre
            var hatalarSeciliAy = hatalar
                .Where(h => h.OlusturmaTarihi.Year == seciliYil && h.OlusturmaTarihi.Month == seciliAy)
                .ToList();

            int HataDurumSay(string durum) =>
                hatalarSeciliAy.Count(h => string.Equals((h.Durum ?? "").Trim(), durum, StringComparison.OrdinalIgnoreCase));

            var model = new DashboardViewModel
            {
                ToplamMusteri = musteriler.Count,
                AktifMusteri = aktifMusteri,
                PasifMusteri = pasifMusteri,
                Bekleyen = bekleyenIs,
                BuAyEklenen = buAyEklenen,
                AylikMusteriSayilari = aylikSayilar,
                AyEtiketleri = ayEtiketleri,

                // Jira (seçili ay)
                JiraBeklemede = jiraBeklemede,
                JiraAktif = jiraAktif,
                JiraTamamlandi = jiraTamam,

                // Ticket (seçili ay)
                TicketOnaybekleniyor = ticketOnaybekleniyor,
                TicketOnaylandi = ticketOnaylandi,
                TicketReddedildi = ticketReddedildi,
                ToplamTicket = tickets.Count,

                // Müşteri Durumu (aylık + parametre bazlı, her durum ayrı)
                MusteriDurumEtiketleri = musteriDurumEtiketleri,
                MusteriDurumSayilari = musteriDurumSayilari,

                Musteriler = musteriler
                    .OrderByDescending(m => m.KayitTarihi ?? DateTime.MinValue)
                    .Take(10)
                    .ToList(),

                JiraTasks = jiraTasksAll
                    .OrderByDescending(t => t.OlusturmaTarihi)
                    .Take(10)
                    .ToList(),

                Tickets = ticketsAll
                    .OrderByDescending(t => t.OlusturmaTarihi)
                    .ToList(),

                ToplantiNotlari = toplantiNotlari
                    .OrderByDescending(n => n.Tarih)
                    .Take(10)
                    .ToList(),

                SeciliAy = seciliAy,
                SeciliYil = seciliYil,
                AySecenekleri = aySecenekleri,

                HataToplamSeciliAy = hatalarSeciliAy.Count,
                HataAcikSeciliAy = HataDurumSay("Açık"),
                HataBeklemedeSeciliAy = HataDurumSay("Beklemede"),
                HataKapaliSeciliAy = HataDurumSay("Kapalı")
            };

            return View(model);
        }
    }
}