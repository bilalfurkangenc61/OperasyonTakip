using System;
using System.ComponentModel.DataAnnotations;

namespace BtOperasyonTakip.Models
{
    public class Ticket
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string FirmaAdi { get; set; } = string.Empty;

        [Required, StringLength(500)]
        public string MusteriWebSitesi { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string YazilimciAdi { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string YazilimciSoyadi { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string IrtibatNumarasi { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(100)]
        public string Mail { get; set; } = string.Empty;

        [StringLength(100)]
        public string? TeknolojiBilgisi { get; set; }

        [StringLength(1000)]
        public string? Aciklama { get; set; }

        // Durumlar:
        // - Operasyon 1 Onay Bekleniyor
        // - Uyum Onayı Bekleniyor
        // - Operasyon 2 Onay Bekleniyor
        // - Saha Canli Bekleniyor
        // - Musteri Kaydedildi
        // - Reddedildi
        [Required, StringLength(50)]
        public string Durum { get; set; } = "Operasyon 1 Onay Bekleniyor";

        [StringLength(500)]
        public string? KararAciklamasi { get; set; }

        [Required]
        public int OlusturanUserId { get; set; }

        [StringLength(100)]
        public string? OlusturanKullaniciAdi { get; set; }

        public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;

        public int? OnaylayanUserId { get; set; }

        [StringLength(100)]
        public string? OnaylayanKullaniciAdi { get; set; }

        public DateTime? OnaylamaTarihi { get; set; }

        public int? MusteriID { get; set; }

        // Uyum onayı bilgisi
        public int? UyumOnaylayanUserId { get; set; }

        [StringLength(100)]
        public string? UyumOnaylayanKullaniciAdi { get; set; }

        public DateTime? UyumOnayTarihi { get; set; }

        [StringLength(500)]
        public string? UyumKararAciklamasi { get; set; }

        // Operasyon 1 onay: Entegrasyon (Evet/Hayır)
        public bool? EntegreOlabilirMi { get; set; }

        [StringLength(250)]
        public string? EntegrasyonNotu { get; set; }

        public DateTime? Operasyon1OnayTarihi { get; set; }

        // Operasyon 2 onay: Mail gönderdim (Evet/Hayır)
        public bool? MailGonderildiMi { get; set; }

        [StringLength(250)]
        public string? MailNotu { get; set; }

        public DateTime? Operasyon2OnayTarihi { get; set; }

        // Saha: Canlı açıldı
        public DateTime? CanliAcildiTarihi { get; set; }

        [StringLength(250)]
        public string? CanliNotu { get; set; }
    }
}