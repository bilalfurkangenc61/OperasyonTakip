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
        public string MusteriWebSitesi { get; set; }

        [Required, StringLength(100)]
        public string YazilimciAdi { get; set; }

        [Required, StringLength(100)]
        public string YazilimciSoyadi { get; set; }

        [Required, StringLength(20)]
        public string IrtibatNumarasi { get; set; }

        [Required, EmailAddress, StringLength(100)]
        public string Mail { get; set; }

        [StringLength(100)]
        public string? TeknolojiBilgisi { get; set; }

        [StringLength(1000)]
        public string? Aciklama { get; set; }

        // Durum: "Uyum Onayı Bekleniyor", "Operasyon Onayı Bekleniyor", "Onaylandi", "Reddedildi"
        [Required, StringLength(50)]
        public string Durum { get; set; } = "Uyum Onayı Bekleniyor";

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
    }
}