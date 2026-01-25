using System;
using System.ComponentModel.DataAnnotations;

namespace BtOperasyonTakip.Models
{
    public class Ticket
    {
        public int Id { get; set; }

        // Firma bilgisi
        [Required, StringLength(200)]
        public string FirmaAdi { get; set; } = string.Empty;

        // Müşteri bilgileri
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

        // Durum: "Onay Bekleniyor", "Onaylandi", "Reddedildi"
        [Required, StringLength(50)]
        public string Durum { get; set; } = "Onay Bekleniyor";

        // Operasyon tarafından red/onay kararı açıklaması (opsiyonel)
        [StringLength(500)]
        public string? KararAciklamasi { get; set; }

        // Sahanın oluşturduğu ticket bilgisi
        [Required]
        public int OlusturanUserId { get; set; }

        [StringLength(100)]
        public string? OlusturanKullaniciAdi { get; set; }

        public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;

        // Operasyon tarafından onay/red veren kişi
        public int? OnaylayanUserId { get; set; }

        [StringLength(100)]
        public string? OnaylayanKullaniciAdi { get; set; }

        public DateTime? OnaylamaTarihi { get; set; }

        // Onaylanan ticket'ın müşteri olarak kaydedildiği müşteri ID
        public int? MusteriID { get; set; }
    }
}