using System;
using System.ComponentModel.DataAnnotations;

namespace BtOperasyonTakip.Models
{
    public class ToplantiNotu
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string MusteriAdi { get; set; } = string.Empty;

        [Required]
        public string NotIcerigi { get; set; } = string.Empty;

        public string EkleyenKisi { get; set; }

        public DateTime Tarih { get; set; } = DateTime.Now;
    }
}
